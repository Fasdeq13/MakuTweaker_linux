using MakuTweakerNew.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Interop;

namespace MakuTweakerNew
{
    public partial class ProcessMGR : Page
    {
        private ObservableCollection<ProcessItem> _items = new ObservableCollection<ProcessItem>();
        private bool _isUpdatingPaused = false;
        private DispatcherTimer _timer;
        private long _dynamicMemoryThreshold = 524288000;
        bool isLoaded = false;
        private bool helpVisible = false;
        MainWindow mw = (MainWindow)Application.Current.MainWindow;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] MEMORYSTATUSEX lpBuffer);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
        private const uint PROCESS_TERMINATE = 0x0001;

        private bool CanTerminateProcess(int processId)
        {
            try
            {
                IntPtr handle = OpenProcess(PROCESS_TERMINATE, false, processId);
                if (handle != IntPtr.Zero)
                {
                    CloseHandle(handle);
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        private string FormatMemory(long bytes)
        {
            double megabytes = bytes / 1024.0 / 1024.0;
            if (megabytes >= 1024)
            {
                return $"{Math.Round(megabytes / 1024.0, 2)} GB";
            }
            return $"{Math.Round(megabytes, 2)} MB";
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);
        private Dictionary<string, ImageSource> _iconCache = new Dictionary<string, ImageSource>();

        private ImageSource GetProcessIcon(Process p)
        {
            string name = p.ProcessName;
            if (_iconCache.TryGetValue(name, out ImageSource cachedIcon))
                return cachedIcon;

            ImageSource imgSource = null;
            try
            {
                string path = p.MainModule.FileName;
                using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(path))
                {
                    if (icon != null)
                    {
                        using (System.Drawing.Bitmap bmp = icon.ToBitmap())
                        {
                            IntPtr hBitmap = bmp.GetHbitmap();
                            try
                            {
                                imgSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                    hBitmap, IntPtr.Zero, Int32Rect.Empty,
                                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                                imgSource.Freeze();
                            }
                            finally { DeleteObject(hBitmap); }
                        }
                    }
                }
            }
            catch { }

            _iconCache[name] = imgSource;
            return imgSource;
        }

        public ProcessMGR()
        {
            InitializeComponent();

            if (MemoryLimitCombo != null)
                MemoryLimitCombo.SelectedIndex = Properties.Settings.Default.LastProcessFilterIndex;

            if (CompactViewCheck != null)
                CompactViewCheck.IsChecked = Properties.Settings.Default.compact;

            if (GroupProcessesCheck != null)
                GroupProcessesCheck.IsChecked = Properties.Settings.Default.group;

            LoadLang();

            isLoaded = true;
        }

        public class ProcessItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            public string Identifier { get; set; } = "";
            public int Id { get; set; }
            public long RawMemory { get; set; }

            private string memoryUsage;
            public string MemoryUsage
            {
                get => memoryUsage;
                set { if (memoryUsage != value) { memoryUsage = value; OnPropertyChanged(nameof(MemoryUsage)); } }
            }

            private double memoryPercentage;
            public double MemoryPercentage
            {
                get => memoryPercentage;
                set { if (memoryPercentage != value) { memoryPercentage = value; OnPropertyChanged(nameof(MemoryPercentage)); } }
            }

            private string name;
            public string Name
            {
                get => name;
                set { if (name != value) { name = value; OnPropertyChanged(nameof(Name)); } }
            }

            private ImageSource icon;
            public ImageSource Icon
            {
                get => icon;
                set { if (icon != value) { icon = value; OnPropertyChanged(nameof(Icon)); } }
            }

            public override string ToString() => $"{Name} ({MemoryUsage})";
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ProcessListView.ItemsSource = _items;
            this.Focus();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += Timer_Tick;

            RefreshProcessList();
            _timer.Start();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
            }

            if (mw != null && mw.Topmost)
            {
                mw.Topmost = false;
                if (TopmostIcon != null)
                {
                    TopmostIcon.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Pin;
                }
            }
        }

        private void Timer_Tick(object sender, EventArgs e) => RefreshProcessList();

        private void RefreshProcessList()
        {
            if (_isUpdatingPaused) return;
            try
            {
                string[] hardcodedExclusions = { "dwm", "msedgewebview2", "startmenuexperiencehost", "taskmgr", "explorer", "system", "idle", "dllhost", "smss", "csrss", "wininit", "services", "lsass", "winlogon", "svchost", "fontdrvhost", "sihost", "shellexperiencehost", "ctfmon", "runtimebroker", "searchindexer", "msbuild", "crossdeviceservice", "bioenrollmenthost", "acergaicameraw", "vmtoolsd",
                "searchapp", "wpfsurface", "searchhost", "phoneexperiencehost", "textinputhost", "nvidia overlay", "vscodium", "lockapp", "shellhost", "systemsettings", "crossdeviceresume", "applicationframehost", "searchui", "gamebar", "xboxgamebarwidgets", "xboxpcappft", "icloudservices", "nvdisplay.container", "widgets", "xboxgamebarspotify", "backgroundtaskhost", "perfwatson2",
                "onedrive", "onedrive.sync.service", "igcctray", "igcc", "microsoft.cmdpal.ui", "wwahost", "onedrive.setup", "rtkuwp", "makutweaker", "msedge", "nvcontainer", "snippingtool", "softlandingtask", "unsecapp", "gameinputredistservice", "accuserps", "useroobebroker", "smartscreen", "nvsphelper64", "acerhardwareservice", "rtkauduservice64", "acersyshardwareservice", "openrgb", "widgetservice",
                "applemobiledeviceprocess", "aqauserps", "windowspackagemanagerserver", "dataexchangehost", "inputpersonalization", "bootcamp", "settingsynchost", "igfxtray", "igfxhk", "securityhealthsystray","filecoauth", "storedesktopextension", "vm3dservice", "rundll32", "searchprotocolhost", "backgroundtransferhost", "xgamehelper", "comppkgsrv", "onedrivestandaloneupdater", "gamebarftserver", "appactions", "systemsettingsbroker"};
                string savedExclusions = Properties.Settings.Default.ProcessExclusions;

                var userExclusions = !string.IsNullOrWhiteSpace(savedExclusions)
                    ? savedExclusions.Split(',').Select(x => x.Trim().ToLower())
                    : Enumerable.Empty<string>();

                var finalExclusions = hardcodedExclusions.Concat(userExclusions).Distinct().ToList();
                long threshold = _dynamicMemoryThreshold;

                bool showOnlyHung = OnlyNotRespondingCheck?.IsChecked ?? false;
                bool groupProcesses = GroupProcessesCheck?.IsChecked ?? false;

                if (MemoryLimitCombo?.SelectedItem is ComboBoxItem comboItem)
                    threshold = long.Parse(comboItem.Tag.ToString());

                var allValidProcesses = Process.GetProcesses()
                    .Where(p =>
                    {
                        try
                        {
                            if (p.Id <= 4 || p.SessionId == 0) return false;
                            if (finalExclusions.Contains(p.ProcessName.ToLower())) return false;
                            if (showOnlyHung && p.Responding) return false;
                            if (!CanTerminateProcess(p.Id)) return false;
                            return true;
                        }
                        catch { return false; }
                    });

                List<ProcessItem> targetList;

                if (groupProcesses)
                {
                    targetList = allValidProcesses
                        .GroupBy(p => p.ProcessName)
                        .Select(g => new
                        {
                            Name = g.Key,
                            TotalMemory = g.Sum(p => { try { return p.WorkingSet64; } catch { return 0L; } }),
                            FirstProc = g.First()
                        })
                        .Where(g => g.TotalMemory > threshold)
                        .OrderByDescending(g => g.TotalMemory)
                        .Select(g => new ProcessItem
                        {
                            Identifier = "GROUP_" + g.Name,
                            Id = g.FirstProc.Id,
                            Name = g.Name,
                            RawMemory = g.TotalMemory,
                            MemoryUsage = FormatMemory(g.TotalMemory),
                            Icon = GetProcessIcon(g.FirstProc)
                        })
                        .ToList();
                }
                else
                {
                    targetList = allValidProcesses
                        .Where(p => { try { return p.WorkingSet64 > threshold; } catch { return false; } })
                        .OrderByDescending(p => { try { return p.WorkingSet64; } catch { return 0L; } })
                        .Select(p => new ProcessItem
                        {
                            Identifier = p.Id.ToString(),
                            Id = p.Id,
                            Name = p.ProcessName,
                            RawMemory = p.WorkingSet64,
                            MemoryUsage = FormatMemory(p.WorkingSet64),
                            Icon = GetProcessIcon(p)
                        })
                        .ToList();
                }

                long totalVisibleMemory = targetList.Sum(x => x.RawMemory);
                foreach (var item in targetList)
                {
                    item.MemoryPercentage = totalVisibleMemory > 0 ? ((double)item.RawMemory / totalVisibleMemory * 100) : 0;
                }

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        var existing = _items.Where(x => !string.IsNullOrEmpty(x.Identifier))
                                             .GroupBy(x => x.Identifier)
                                             .ToDictionary(g => g.Key, g => g.First());

                        foreach (var p in targetList)
                        {
                            if (existing.TryGetValue(p.Identifier, out var item))
                            {
                                item.Name = p.Name;
                                item.MemoryUsage = p.MemoryUsage;
                                item.MemoryPercentage = p.MemoryPercentage;
                                item.Id = p.Id;
                                item.Icon = p.Icon;
                            }
                            else
                            {
                                _items.Add(p);
                            }
                        }

                        for (int i = _items.Count - 1; i >= 0; i--)
                        {
                            if (!targetList.Any(p => p.Identifier == _items[i].Identifier))
                                _items.RemoveAt(i);
                        }

                        for (int i = 0; i < targetList.Count; i++)
                        {
                            var item = _items.FirstOrDefault(x => x.Identifier == targetList[i].Identifier);
                            if (item != null)
                            {
                                int index = _items.IndexOf(item);
                                if (index != i) _items.Move(index, i);
                            }
                        }
                    }
                    catch (Exception uiEx)
                    {
                        Debug.WriteLine($"UI Update Exception: {uiEx.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void KillProcess_Click(object sender, RoutedEventArgs e)
        {
            ProcessItem selected = null;

            if (sender is Button btn && btn.DataContext is ProcessItem item)
                selected = item;
            else if (ProcessListView.SelectedItem is ProcessItem listSelectedItem)
                selected = listSelectedItem;

            if (selected != null)
            {
                try
                {
                    var processesToKill = Process.GetProcessesByName(selected.Name);
                    foreach (var proc in processesToKill)
                    {
                        try { proc.Kill(); } catch { }
                    }

                    await Task.Delay(150);
                    _isUpdatingPaused = false;
                    if (_timer != null && !_timer.IsEnabled) _timer.Start();

                    PauseIcon.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Pause;
                    RefreshProcessList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "MakuTweaker", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                Properties.Settings.Default.LastProcessFilterIndex = MemoryLimitCombo.SelectedIndex;
                Properties.Settings.Default.compact = CompactViewCheck?.IsChecked ?? false;
                Properties.Settings.Default.group = GroupProcessesCheck?.IsChecked ?? false;
                Properties.Settings.Default.Save();
                RefreshProcessList();
            }
        }

        private void ProcessListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && ProcessListView.SelectedItem != null)
            {
                KillProcess_Click(sender, e);
            }

            if (e.Key == Key.F5)
            {
                _items.Clear();
                RefreshProcessList();
            }
        }

        private void LoadLang()
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var pmgr = MainWindow.Localization.LoadLocalization(languageCode, "pmgr");
            var myan = MainWindow.Localization.LoadLocalization(languageCode, "myan");
            var tooltips = MainWindow.Localization.LoadLocalization(languageCode, "tooltips");

            mgr_tooltip.Content = tooltips["main"]["MakuTweakerProcessMGR1"];
            mgr1_tooltip.Content = pmgr["main"]["excltitle"];
            MakuYanText.Text = myan["main"]["excltitle"];
            label.Text = pmgr["main"]["label"];
            groupproc.Text = pmgr["main"]["process"];
            groupmem.Text = pmgr["main"]["memuse"];

            if (CompactViewCheck != null) CompactViewCheck.Content = pmgr["main"]["compact"];
            if (GroupProcessesCheck != null) GroupProcessesCheck.Content = pmgr["main"]["group"];
            if (OnlyNotRespondingCheck != null) OnlyNotRespondingCheck.Content = pmgr["main"]["onlyfrozen"];

            if (MemoryLimitCombo != null && MemoryLimitCombo.Items.Count >= 7)
            {
                string[] keys = { "showall", "from50mb", "from100mb", "from300mb", "from500mb", "from1000mb", "from2000mb" };

                for (int i = 0; i < keys.Length; i++)
                {
                    if (i < MemoryLimitCombo.Items.Count && MemoryLimitCombo.Items[i] is ComboBoxItem comboItem)
                        comboItem.Content = pmgr["main"][keys[i]];
                }
            }

            if (this.Resources["ItemContextMenu"] is System.Windows.Controls.ContextMenu contextMenu)
            {
                var items = (contextMenu as System.Windows.Controls.ItemsControl).Items;

                if (items.Count >= 3)
                {
                    if (items[0] is MenuItem itemKill) itemKill.Header = pmgr["main"]["endprocess"];
                    if (items[1] is MenuItem itemAddExcl) itemAddExcl.Header = pmgr["main"]["excl"];
                    if (items[2] is MenuItem itemLoc) itemLoc.Header = pmgr["main"]["location"];
                }
            }
        }

        private void OpenLocation_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessListView.SelectedItem is ProcessItem selected)
            {
                try
                {
                    var proc = Process.GetProcessById(selected.Id);
                    string filePath = proc.MainModule.FileName;
                    Process.Start("explorer.exe", $"/select, \"{filePath}\"");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "MakuTweaker Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void InfoBtn_Click(object sender, RoutedEventArgs e)
        {
            buttontooltip.IsEnabled = false;
            helpVisible = !helpVisible;

            if (helpVisible)
            {
                var languageCode = Properties.Settings.Default.lang ?? "en";
                var tooltips = MainWindow.Localization.LoadLocalization(languageCode, "tooltips");
                HelpText.Text = tooltips["main"]["MakuTweakerProcessMGR"];
                AnimatePages(true);
                InfoIcon.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Back;
            }
            else
            {
                AnimatePages(false);
                InfoIcon.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Help;
            }

            await Task.Delay(200);
            buttontooltip.IsEnabled = true;
        }

        private void AnimatePages(bool showHelp)
        {
            double offset = ContentHost.ActualHeight;
            double duration = 0.25;
            var ease = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };

            if (showHelp)
            {
                HelpContent.Visibility = Visibility.Visible;
                MainContent.IsHitTestVisible = false;

                var mainAnim = new DoubleAnimation { To = -offset, Duration = TimeSpan.FromSeconds(duration), EasingFunction = ease };
                var helpAnim = new DoubleAnimation { From = offset, To = 0, Duration = TimeSpan.FromSeconds(duration), EasingFunction = ease };
                var fadeOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(duration * 0.8), EasingFunction = ease };

                MainTransform.BeginAnimation(TranslateTransform.YProperty, mainAnim);
                MainContent.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                HelpTransform.BeginAnimation(TranslateTransform.YProperty, helpAnim);
            }
            else
            {
                MainContent.IsHitTestVisible = true;
                MainContent.BeginAnimation(UIElement.OpacityProperty, null);
                MainContent.Opacity = 0;

                var mainAnim = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(duration), EasingFunction = ease };
                var helpAnim = new DoubleAnimation { To = offset, Duration = TimeSpan.FromSeconds(duration), EasingFunction = ease };

                helpAnim.Completed += (s, e) =>
                {
                    HelpContent.Visibility = Visibility.Collapsed;
                    var fadeIn = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.25), EasingFunction = ease };
                    MainContent.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                };

                MainTransform.BeginAnimation(TranslateTransform.YProperty, mainAnim);
                HelpTransform.BeginAnimation(TranslateTransform.YProperty, helpAnim);
            }
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e) => _isUpdatingPaused = true;
        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            _isUpdatingPaused = false;
            RefreshProcessList();
            PauseIcon.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Pause;
        }

        private void MemoryLimitCombo_DropDownOpened(object sender, EventArgs e) => _isUpdatingPaused = true;
        private void MemoryLimitCombo_DropDownClosed(object sender, EventArgs e)
        {
            _isUpdatingPaused = false;
            RefreshProcessList();
            PauseIcon.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Pause;
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            ExclusionWindow win = new ExclusionWindow();
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
            RefreshProcessList();
        }

        private void AddToExclusions_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessListView.SelectedItem is ProcessItem selected)
            {
                try
                {
                    string currentName = selected.Name.ToLower();
                    string savedExclusions = Properties.Settings.Default.ProcessExclusions;
                    var currentExclusions = !string.IsNullOrWhiteSpace(savedExclusions)
                        ? savedExclusions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim().ToLower()).ToList()
                        : new List<string>();

                    if (!currentExclusions.Contains(currentName))
                    {
                        currentExclusions.Add(currentName);
                        Properties.Settings.Default.ProcessExclusions = string.Join(", ", currentExclusions);
                        Properties.Settings.Default.Save();
                        RefreshProcessList();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "MakuTweaker Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PauseBtn_Click(object sender, RoutedEventArgs e)
        {
            _isUpdatingPaused = !_isUpdatingPaused;
            if (_isUpdatingPaused)
            {
                _timer.Stop();
                PauseIcon.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Play;
            }
            else
            {
                _timer.Start();
                _items.Clear();
                PauseIcon.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Pause;
                RefreshProcessList();
            }
        }

        private void restartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                _items.Clear();
                _iconCache.Clear();
                RefreshProcessList();
            }
        }

        private void MakuYan_Click(object sender, RoutedEventArgs e)
        {
            MakuYan mkyan = new MakuYan();
            mkyan.Owner = Application.Current.MainWindow;
            mkyan.ShowDialog();
            RefreshProcessList();
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                _items.Clear();
                RefreshProcessList();
            }
        }

        private void ProcessListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProcessListView.SelectedItem != null)
                OpenLocation_Click(sender, e);
        }

        private void TopmostBtn_Click(object sender, RoutedEventArgs e)
        {
            if (mw != null)
            {
                mw.Topmost = !mw.Topmost;

                if (mw.Topmost)
                {
                    TopmostIcon.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.UnPin;
                }
                else
                {
                    TopmostIcon.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Pin;
                }
            }
        }
    }
}