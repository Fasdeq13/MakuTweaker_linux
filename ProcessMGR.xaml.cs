using MakuTweakerNew.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Windows.UI.Composition.Desktop;
using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;

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
        public ProcessMGR()
        {
            InitializeComponent();
            LoadLang();
            isLoaded = true;
        }
        public class ProcessItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private string memoryUsage;
            public string MemoryUsage
            {
                get => memoryUsage;
                set
                {
                    memoryUsage = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MemoryUsage)));
                }
            }

            private string name;
            public string Name
            {
                get => name;
                set
                {
                    name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }

            public int Id { get; set; }

            public override string ToString()
            {
                return $"{Name} ({MemoryUsage})";
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ProcessListView.ItemsSource = _items;
            this.Focus();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(0.1);
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
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            RefreshProcessList();
        }

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
                bool showOnlyHung = false;
                long threshold = _dynamicMemoryThreshold;

                showOnlyHung = OnlyNotRespondingCheck.IsChecked ?? false;

                if (MemoryLimitCombo?.SelectedItem is ComboBoxItem comboItem)
                    threshold = long.Parse(comboItem.Tag.ToString());

                var heavyProcesses = Process.GetProcesses()
                    .Where(p =>
                    {
                        try
                        {
                            if (p.Id <= 4 || p.SessionId == 0) return false;
                            if (finalExclusions.Contains(p.ProcessName.ToLower())) return false;
                            if (p.WorkingSet64 <= threshold) return false;
                            if (showOnlyHung && p.Responding) return false;
                            return true;
                        }
                        catch { return false; }
                    })
                    .OrderByDescending(p => p.WorkingSet64)
                    .Select(p => new ProcessItem
                    {
                        Id = p.Id,
                        Name = p.ProcessName,
                        MemoryUsage = $"{Math.Round(p.WorkingSet64 / 1024.0 / 1024.0, 2)} MB"
                    })
                    .ToList();

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    var existing = _items.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First());
                    foreach (var p in heavyProcesses)
                    {
                        if (existing.TryGetValue(p.Id, out var item))
                        {
                            item.Name = p.Name;
                            item.MemoryUsage = p.MemoryUsage;
                        }
                        else
                        {
                            _items.Add(p);
                        }
                    }

                    for (int i = _items.Count - 1; i >= 0; i--)
                    {
                        if (!heavyProcesses.Any(p => p.Id == _items[i].Id))
                            _items.RemoveAt(i);
                    }
                    for (int i = 0; i < heavyProcesses.Count; i++)
                    {
                        var item = _items.FirstOrDefault(x => x.Id == heavyProcesses[i].Id);
                        if (item != null)
                        {
                            int index = _items.IndexOf(item);
                            if (index != i)
                                _items.Move(index, i);
                        }
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
            if (ProcessListView.SelectedItem is ProcessItem selected)
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

                    if (_timer != null && !_timer.IsEnabled)
                        _timer.Start();

                    PauseIcon.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Pause;

                    RefreshProcessList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "MakuTweaker Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            if (isLoaded) RefreshProcessList();
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
            mgr2_tooltip.Content = myan["main"]["excltitle"];
            label.Text = pmgr["main"]["label"];
            if (KillBtn != null) KillBtn.Content = pmgr["main"]["endprocess"];
            if (OnlyNotRespondingCheck != null) OnlyNotRespondingCheck.Content = pmgr["main"]["onlyfrozen"];

            if (MemoryLimitCombo != null && MemoryLimitCombo.Items.Count >= 7)
            {
                string[] keys = { "showall", "from50mb", "from100mb", "from300mb", "from500mb", "from1000mb", "from2000mb" };

                for (int i = 0; i < keys.Length; i++)
                {
                    if (i < MemoryLimitCombo.Items.Count && MemoryLimitCombo.Items[i] is ComboBoxItem item)
                    {
                        item.Content = pmgr["main"][keys[i]];
                    }
                }
            }

            var contextMenu = this.Resources["ItemContextMenu"] as System.Windows.Controls.ContextMenu;
            if (contextMenu != null)
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

                buttontooltip.Content = "←";
            }
            else
            {
                AnimatePages(false);

                buttontooltip.Content = "?";
            }

            await Task.Delay(200);
            buttontooltip.IsEnabled = true;
        }

        private void AnimatePages(bool showHelp)
        {
            double offset = ContentHost.ActualHeight;
            double duration = 0.25;

            var ease = new System.Windows.Media.Animation.CubicEase
            {
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
            };

            if (showHelp)
            {
                HelpContent.Visibility = Visibility.Visible;
                MainContent.IsHitTestVisible = false;

                ControlPanel.Visibility = Visibility.Collapsed;

                var mainAnim = new DoubleAnimation
                {
                    To = -offset,
                    Duration = TimeSpan.FromSeconds(duration),
                    EasingFunction = ease
                };

                var helpAnim = new DoubleAnimation
                {
                    From = offset,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(duration),
                    EasingFunction = ease
                };

                var fadeOut = new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromSeconds(duration * 0.8),
                    EasingFunction = ease
                };

                MainTransform.BeginAnimation(TranslateTransform.YProperty, mainAnim);
                MainContent.BeginAnimation(UIElement.OpacityProperty, fadeOut);

                HelpTransform.BeginAnimation(TranslateTransform.YProperty, helpAnim);
            }
            else
            {
                MainContent.IsHitTestVisible = true;
                ControlPanel.Visibility = Visibility.Visible;

                MainContent.BeginAnimation(UIElement.OpacityProperty, null);
                MainContent.Opacity = 0;

                var mainAnim = new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromSeconds(duration),
                    EasingFunction = ease
                };

                var helpAnim = new DoubleAnimation
                {
                    To = offset,
                    Duration = TimeSpan.FromSeconds(duration),
                    EasingFunction = ease
                };

                helpAnim.Completed += (s, e) =>
                {
                    HelpContent.Visibility = Visibility.Collapsed;

                    var fadeIn = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(0.25),
                        EasingFunction = ease
                    };

                    MainContent.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                };

                MainTransform.BeginAnimation(TranslateTransform.YProperty, mainAnim);
                HelpTransform.BeginAnimation(TranslateTransform.YProperty, helpAnim);
            }
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            _isUpdatingPaused = true;
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            _isUpdatingPaused = false;
            RefreshProcessList();
            PauseIcon.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Pause;
        }

        private void MemoryLimitCombo_DropDownOpened(object sender, EventArgs e)
        {
            _isUpdatingPaused = true;
        }

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
                        ? savedExclusions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(x => x.Trim().ToLower())
                                         .ToList()
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

        private void SettingsBtn_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void restartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                _items.Clear();
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
    }
}
