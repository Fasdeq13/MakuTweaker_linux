using Hardcodet.Wpf.TaskbarNotification;
using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Media.Animation;
using MakuTweakerNew.Properties;
using MicaWPF.Controls;
using MicaWPF.Core.Enums;
using MicaWPF.Core.Helpers;
using MicaWPF.Core.Services;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Reflection;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Windows.Data;
using Windows.Data.Xml.Dom;
using Windows.Globalization.Fonts;
using Windows.UI;
using Windows.UI.Notifications;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace MakuTweakerNew
{
    public partial class MainWindow : MicaWindow
    {
		private NavigationTransitionInfo _transitionInfo = null;
        private DispatcherTimer ExpRestart;
        public static bool HasAutoStartedExclusive = false;

        public static class Localization
        {
            public static Dictionary<string, Dictionary<string, string>> LoadLocalization(string language, string category)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"MakuTweakerNew.loc.{language}.json";

                using Stream stream = assembly.GetManifestResourceStream(resourceName);

                if (stream == null)
                {
                    Settings.Default.lang = "en";
                    throw new FileNotFoundException($"Cannot find embedded localization {resourceName}.\nLanguage has been changed to English.");
                }

                using StreamReader reader = new StreamReader(stream);
                var jsonContent = reader.ReadToEnd();

                var localizationData =
                    JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>>(jsonContent);

                if (localizationData.ContainsKey("categories"))
                {
                    var categories = localizationData["categories"];

                    if (categories.ContainsKey(category))
                    {
                        return categories[category];
                    }
                }

                Settings.Default.lang = "en";
                throw new KeyNotFoundException($"Cannot find category '{category}' in localization {resourceName}");
            }
            
            public static List<TweakSuggestion> GetAllTweaksForSearch(string language)
            {
                var tweaks = new List<TweakSuggestion>();
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"MakuTweakerNew.loc.{language}.json";

                using Stream stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) return tweaks;

                using StreamReader reader = new StreamReader(stream);
                var jsonContent = reader.ReadToEnd();

                var fullData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonContent);
                var categoriesRoot = fullData["categories"];
    
                var catNames = categoriesRoot["base"]["catname"].ToObject<Dictionary<string, string>>();

                var ignoredKeys = new HashSet<string>
                {
                    "label", "choose", "showall", "info1", "info2", "info3", "chk", "comp", "b",
                    "mode1", "mode2", "mode3", "tpmy", "tpmn", "tooltip", "test1multi",
                    "running_multicore", "running", "test1", "test2", "test3", "benchtip",
                    "info", "flyout", "reportbutton", "b2", "b4", "deledge_done", "deledge_error",
                    "deledge_tooltip", "deledge_sure", "deledge_before", "deledge_btn",
                    "makuos_tooltip", "infodone", "title", "title2", "os", "oned", "tenM",
                    "thirtyM", "oneH", "twoH", "fourH", "sixH", "suredialogT1", "suredialogT2",
                    "suredialogT3", "suredialogT4", "suredialogNS", "isnt", "is", "wu5b", "wu6b",
                    "install", "reset", "enable", "e8b"
                };

                foreach (var category in categoriesRoot)
                {
                    if (category.Name == "base" || category.Name == "pmgr" || category.Name == "perfor" || category.Name == "settings" || category.Name == "ab" || category.Name == "quick") continue;
                    if (category.Value["main"] == null) continue;
                    if (category.Value["main"]["label"] == null) continue;

                    string internalTag = category.Name;
                    string displayCategoryName = catNames.ContainsKey(internalTag) 
                        ? catNames[internalTag] 
                        : internalTag;

                    if (category.Value["main"] != null)
                    {
                        foreach (var tweak in category.Value["main"])
                        {
                            string key = tweak.Name;

                            if (ignoredKeys.Contains(key) || (key == "label" || key == "choose" || key == "showall" ||
                                key == "info1" || key == "info2" || key == "info3" ||
                                key == "chk" || key == "comp" || key == "b" ||
                                key == "mode1" || key == "mode2" || key == "mode3" ||
                                key.StartsWith("tooltip") || key.StartsWith("desc") ||
                                key.StartsWith("status") || key.StartsWith("info") ||
                                key.StartsWith("tip") || key.StartsWith("note") ||
                                key.StartsWith("warn") || key.StartsWith("msg")))
                                continue;

                            string displayValue = tweak.Value.ToString();
    
                            if (displayValue.Length > 90) continue;
    
                            tweaks.Add(new TweakSuggestion
                            {
                                Id = key,
                                DisplayName = displayValue,
                                CategoryKey = displayCategoryName,
                                InternalCategoryTag = internalTag
                            });
                        }
                    }
                }

                try
                {
                    if (categoriesRoot["myan"] != null && categoriesRoot["myan"]["main"] != null && categoriesRoot["myan"]["main"]["excltitle"] != null)
                    {
                        string displayCategoryName = catNames.ContainsKey("procmgr")
                            ? catNames["procmgr"]
                            : "ProcessMGR";

                        tweaks.Add(new TweakSuggestion
                        {
                            Id = "makuyan_appblock",
                            DisplayName = categoriesRoot["myan"]["main"]["excltitle"].ToString(),
                            CategoryKey = displayCategoryName,
                            InternalCategoryTag = "makuyan_window"
                        });
                    }
                }
                catch { }

                return tweaks;
            }
        }
        
        public class TweakSuggestion
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public string CategoryKey { get; set; }
            public string InternalCategoryTag { get; set; }
    
            public override string ToString() => DisplayName;
        }
        
        private List<TweakSuggestion> _searchLibrary = new List<TweakSuggestion>();

        private void InitializeSearch()
        {
            _searchLibrary = Localization.GetAllTweaksForSearch(Settings.Default.lang);
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var query = sender.Text.ToLower();
                if (string.IsNullOrWhiteSpace(query))
                {
                    sender.ItemsSource = null;
                    return;
                }

                sender.ItemsSource = _searchLibrary
                    .Where(t => t.DisplayName.ToLower().Contains(query))
                    .Take(10)
                    .ToList();
            }
        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.IsSuggestionListOpen = false;
            Keyboard.ClearFocus();
            if (args.SelectedItem is TweakSuggestion selected)
            {
                if (selected.InternalCategoryTag == "makuyan_window")
                {
                    if (this.Topmost)
                    {
                        this.Topmost = false;
                    }
                    MakuYan mkyan = new MakuYan();
                    mkyan.Owner = this;
                    mkyan.ShowDialog();
                    sender.Text = string.Empty;
                    return;
                }

                string xamlTag = selected.InternalCategoryTag switch
                {
                    "expl" => "exp",
                    "wu" => "wu",
                    "sr" => "sys",
                    "per" => "per",
                    "uwp" => "uwp",
                    "quick" => "quick",
                    "adv" => "adv",
                    "compon" => "compon",
                    "act" => "act",
                    "perf" => "perf",
                    "sat" => "sat",
                    "procmgr" => "pmgr",
                    "pci" => "pci",
                    _ => selected.InternalCategoryTag
                };

                var targetItem = NavigationView_Root.MenuItems
                    .OfType<NavigationViewItem>()
                    .FirstOrDefault(i => i.Tag?.ToString() == xamlTag);

                if (targetItem != null)
                {
                    NavigationView_Root.SelectedItem = targetItem;
                }
                sender.Text = string.Empty;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            string[] args = Environment.GetCommandLineArgs();
            bool launchedViaTaskmgr = args.Length > 1 && args.Any(arg => arg.IndexOf("taskmgr.exe", StringComparison.OrdinalIgnoreCase) >= 0);
            if (checkWinVer() < 14393)
            {
                System.Windows.Forms.DialogResult old = System.Windows.Forms.MessageBox.Show("Your version of Windows is not supported. To use MakuTweaker, update your system to Windows 10 1607 or higher. Do you want to download MakuTweaker Legacy Windows Edition?\n\nВаша версия Windows неподдерживается. Для использования MakuTweaker, обновитесь до Windows 10 1607 или выше. Вы хотите скачать MakuTweaker для старых Windows?", "MakuTweaker", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Error);
                if(old == System.Windows.Forms.DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo("https://adderly.top/mt") { UseShellExecute = true });
                }
                Application.Current.Shutdown();
            }

            if (Properties.Settings.Default.AutoStartExclusive || launchedViaTaskmgr)
            {
                if (launchedViaTaskmgr)
                {
                    Properties.Settings.Default.AutoStartExclusive = true;
                }

                this.MinWidth = 580;
                this.MinHeight = 380;
                this.MaxWidth = double.PositiveInfinity;
                this.MaxHeight = double.PositiveInfinity;
                this.ResizeMode = ResizeMode.CanResize;

                var chrome = System.Windows.Shell.WindowChrome.GetWindowChrome(this);
                if (chrome != null) chrome.ResizeBorderThickness = new Thickness(6);

                if (Properties.Settings.Default.ExclusiveWindowLeft != -10000)
                {
                    this.WindowStartupLocation = WindowStartupLocation.Manual;
                    this.Width = Properties.Settings.Default.ExclusiveWindowWidth >= 580 ? Properties.Settings.Default.ExclusiveWindowWidth : 1062;
                    this.Height = Properties.Settings.Default.ExclusiveWindowHeight >= 380 ? Properties.Settings.Default.ExclusiveWindowHeight : 675;
                    this.Left = Properties.Settings.Default.ExclusiveWindowLeft;
                    this.Top = Properties.Settings.Default.ExclusiveWindowTop;
                }
                else
                {
                    this.Width = 1062;
                    this.Height = 675;
                    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                NavigationView_Root.OpenPaneLength = 0;
                NavigationView_Root.IsPaneVisible = false;
                SearchBox.Opacity = 0;
                SearchBox.Visibility = Visibility.Collapsed;
            }

            ExpTimer();
            if (Properties.Settings.Default.firRun)
            {
                string systemLang = CultureInfo.CurrentUICulture.Name.ToLower();
                string isoLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
                string detectedTag = systemLang switch
                {
                    "zh-tw" or "zh-hk" or "zh-mo" => "tw",
                    "zh-cn" or "zh-sg" or "zh-chs" => "zh",
                    _ => isoLang switch
                    {
                        "uk" or "cs" or "ru" or "az" or "es" or "tl" or "tr" or "ko" or
                        "zh" or "it" or "de" or "fr" or "be" or "vi" or "id" or "hi" or
                        "ja" or "kk" or "pt" or "lv" or "fi" or "et" or "pl" or "th" => isoLang,
                        "fil" => "tl",
                        _ => "en"
                    }
                };

                Settings.Default.lang = detectedTag;
                var tagsOrder = new List<string>
                {
                    "en", "ru", "uk", "be", "kk", "cs", "de", "fr", "es", "it",
                    "pt", "fi", "et", "lv", "pl", "az", "tr", "zh", "tw", "ja",
                    "ko", "vi", "th", "id", "tl", "hi"
                };
                int index = tagsOrder.IndexOf(detectedTag);
                Settings.Default.langSI = index != -1 ? index : 0;

                var currentSystemTheme = MicaWPFServiceUtility.ThemeService.CurrentTheme;
                Settings.Default.theme = currentSystemTheme == WindowsTheme.Dark ? "Dark" : "Light";

                Settings.Default.firRun = false;
                Settings.Default.Save();
                ApplyTheme(currentSystemTheme);
            }
            else
            {
                string themeString = Properties.Settings.Default.theme;
                if (string.IsNullOrEmpty(themeString) || themeString == "Auto")
                {
                    var systemTheme = MicaWPFServiceUtility.ThemeService.CurrentTheme;
                    ApplyTheme(systemTheme);
                    Properties.Settings.Default.theme = systemTheme == WindowsTheme.Dark ? "Dark" : "Light";
                }
                else if (Enum.TryParse<WindowsTheme>(themeString, out var parsedTheme))
                {
                    ApplyTheme(parsedTheme);
                }
                else
                {
                    ApplyTheme(MicaWPFServiceUtility.ThemeService.CurrentTheme);
                }
            }

            LoadLang(Properties.Settings.Default.lang);
            _ = CheckForUpd();
            InitializeSearch();
        }

        private void ApplyTheme(WindowsTheme theme)
        {
            MicaWPFServiceUtility.ThemeService.ChangeTheme(theme);

            if (theme == WindowsTheme.Dark)
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                this.Foreground = System.Windows.Media.Brushes.White;
            }
            else
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                this.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void ExpTimer()
        {
            ExpRestart = new DispatcherTimer();
            ExpRestart.Interval = TimeSpan.FromMilliseconds(2000);
            ExpRestart.Tick += ExpRestart_Tick;
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem && selectedItem.Tag != null)
            {
                string tag = selectedItem.Tag.ToString();
                Type pageType = tag switch
                {
                    "exp" => typeof(Explorer),
                    "wu" => typeof(WindowsUpdate),
                    "sys" => typeof(SysAndRec),
                    "per" => typeof(Personalization),
                    "uwp" => typeof(UWP),
                    "quick" => typeof(QuickSet),
                    "adv" => typeof(Advanced),
                    "compon" => typeof(WindowsComponents),
                    "act" => typeof(Act),
                    "perf" => typeof(Perf),
                    "sat" => typeof(SAT),
                    "pmgr" => typeof(ProcessMGR),
                    "pci" => typeof(PCI),
                    _ => null
                };

                if (pageType != null)
                {
                    MainFrame.Navigate(pageType, null, _transitionInfo);
                    Properties.Settings.Default.lastPageTag = tag;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void MicaWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
            bool launchedViaTaskmgr = false;
            if (args.Length > 1 && args.Any(arg => arg.IndexOf("taskmgr.exe", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                launchedViaTaskmgr = true;
                Properties.Settings.Default.AutoStartExclusive = true;
            }

            if (args.Length > 1 && args[1].EndsWith(".mktw", StringComparison.OrdinalIgnoreCase))
            {
                string presetPath = args[1];

                if (File.Exists(presetPath))
                {
                    var settingsPage = new SettingsAbout();
                    NavigationView_Root.SelectedItem = null;
                    MainFrame.Navigate(settingsPage, null, _transitionInfo);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        settingsPage.ProcessPresetImport(presetPath);
                    }), DispatcherPriority.ContextIdle);

                    return;
                }
            }

            string lastTag = Properties.Settings.Default.lastPageTag;
            if (args.Length > 1)
            {
                string param = args[1].ToLower().TrimStart('/', '-');

                switch (param)
                {
                    case "u":
                        lastTag = "wu";
                        break;
                    case "p":
                        lastTag = "perf";
                        break;
                    case "s":
                        lastTag = "sat";
                        break;
                    case "mgr":
                        lastTag = "pmgr";
                        break;
                    case "pc":
                        lastTag = "pci";
                        break;
                }
            }


            if (Properties.Settings.Default.AutoStartExclusive || launchedViaTaskmgr)
            {
                lastTag = "pmgr";
                NavigationView_Root.OpenPaneLength = 0;
                NavigationView_Root.IsPaneVisible = false;
                SearchBox.Opacity = 0;
                SearchBox.Visibility = Visibility.Collapsed;
            }

            if (IsWindowsActivated())
            {
                c9.Visibility = Visibility.Collapsed;
                if (lastTag == "act")
                {
                    lastTag = "exp";
                }
            }

            var checkQs = new QuickSet();
            if (checkQs.VisibleTweaksCount < 5)
            {
                c6.Visibility = Visibility.Collapsed;
                if (lastTag == "quick")
                {
                    lastTag = "exp";
                }
            }

            if (Properties.Settings.Default.UwpHidden)
            {
                c5.Visibility = Visibility.Collapsed;
                if (lastTag == "uwp") lastTag = "exp";
            }

            if (!string.IsNullOrEmpty(lastTag))
            {
                var itemToSelect = NavigationView_Root.MenuItems
                    .OfType<NavigationViewItem>()
                    .FirstOrDefault(i => i.Tag?.ToString() == lastTag);

                if (itemToSelect != null)
                {
                    NavigationView_Root.SelectedItem = itemToSelect;
                }
                else
                {
                    NavigationView_Root.SelectedItem = c1;
                }
            }
            else
            {
                NavigationView_Root.SelectedItem = c1;
            }

            Enum.TryParse(Settings.Default.style, out BackdropType bd);
            MicaWPFServiceUtility.ThemeService.EnableBackdrop(this, bd);
            this.SizeChanged += MainWindow_SizeChanged;
            UpdateMainWindowResponsiveUI(this.ActualWidth);
        }

        private bool IsWindowsActivated()
        {
            try
            {
                ManagementScope scope = new ManagementScope(@"\\" + Environment.MachineName + @"\root\cimv2");
                scope.Connect();
                SelectQuery searchQuery = new SelectQuery("SELECT LicenseStatus FROM SoftwareLicensingProduct WHERE ApplicationID = '55c92734-d682-4d71-983e-d6ec3f16059f' AND PartialProductKey IS NOT NULL");

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, searchQuery))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (Convert.ToInt32(obj["LicenseStatus"]) == 1)
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
            }
            return false;
        }
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateMainWindowResponsiveUI(e.NewSize.Width);
        }

        private void UpdateMainWindowResponsiveUI(double width)
        {
            if (width < 900)
            {
                if (rexplorer != null) rexplorer.Visibility = Visibility.Collapsed;
                if (rexplorerSeparator != null) rexplorerSeparator.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (rexplorer != null) rexplorer.Visibility = Visibility.Visible;
                if (rexplorerSeparator != null) rexplorerSeparator.Visibility = Visibility.Visible;
            }
            if (width < 740)
            {
                if (settingsText != null) settingsText.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (settingsText != null) settingsText.Visibility = Visibility.Visible;
            }
        }

        public void LoadLang(string lang)
        {
            try
            {
                var languageCode = Properties.Settings.Default.lang ?? "en";
                var basel = Localization.LoadLocalization(languageCode, "base");
                c1.Content = basel["catname"]["expl"];
                c2.Content = basel["catname"]["wu"];
                c3.Content = basel["catname"]["sr"];
                c4.Content = basel["catname"]["per"];
                c5.Content = basel["catname"]["uwp"];
                c6.Content = basel["catname"]["quick"];
                c7.Content = basel["catname"]["adv"];
                c8.Content = basel["catname"]["compon"];
                c9.Content = basel["catname"]["act"];
                c10.Content = basel["catname"]["perf"];
                c11.Content = basel["catname"]["sat"];
                c12.Content = basel["catname"]["procmgr"];
                c13.Content = basel["catname"]["pci"];
                rexplorerText.Text = basel["lowtabs"]["rexp"];
                settingsText.Text = basel["lowtabs"]["set"];
                SearchBox.PlaceholderText = basel["def"]["search"];
                InitializeSearch();
                SearchBox.Text = string.Empty;
            }
            catch(Exception ex)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(ex.Message, "MakuTweaker Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                System.Windows.Forms.Application.Restart();
                System.Windows.Application.Current.Shutdown();
            }
        }

        public void RebootNotify(int mode)
        {
            string message = string.Empty;
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var basel = MainWindow.Localization.LoadLocalization(languageCode, "base");
            Icon trayIcon = new Icon(GetResourceStream("assets/icons/MakuT.ico"));

            TaskbarIcon _trayIcon = new TaskbarIcon
            {
                ToolTipText = "MakuTweaker",
                Icon = trayIcon
            };

            if (mode == 1)
            {
                message = basel["def"]["rebnotify"];
            }
            else if (mode == 2)
            {
                message = basel["def"]["rebnotifyexplorer"];
            }
            else if (mode == 3)
            {
                message = basel["def"]["rebnotifysfc"];
            }

            _trayIcon.ShowBalloonTip("MakuTweaker", message, BalloonIcon.Warning);

            Task.Delay(8000).ContinueWith(t =>
            {
                _trayIcon.Dispatcher.Invoke(() => _trayIcon.Dispose());
            });
        }
        private Stream GetResourceStream(string relativePath)
        {
            var uri = new Uri($"pack://application:,,,/{relativePath}", UriKind.Absolute);
            var resourceInfo = Application.GetResourceStream(uri);

            if (resourceInfo == null)
                throw new FileNotFoundException($"Ресурс {relativePath} не найден.");

            return resourceInfo.Stream;
        }

        private void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content is ProcessMGR pmgr)
            {
                pmgr.ToggleExclusiveMode();
                return;
            }

            NavigationView_Root.SelectedItem = null;
            MainFrame.Navigate(typeof(SettingsAbout), null, _transitionInfo);
        }

        private void MainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (NavigationView_Root.SelectedItem == null)
            {
                settingsButton.IsEnabled = false;
            }
            else
            {
                settingsButton.IsEnabled = true;
            }

            if (e.Content is ProcessMGR pmgr)
            {
                UpdateSettingsButtonForExclusive(true, pmgr.IsExclusiveMode);
            }
            else
            {
                UpdateSettingsButtonForExclusive(false, false);
            }
        }

        public void expk()
        {
            Process proc = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "taskkill";
            startInfo.Arguments = "/F /IM explorer.exe";
            proc.StartInfo = startInfo;
            proc.Start();
        }
        private void ExpRestart_Tick(object sender, EventArgs e)
        {
            Process.Start("explorer.exe");
            ExpRestart.Stop();
        }

        private void rexplorer_Click(object sender, RoutedEventArgs e)
        {
            expk();
            ExpRestart.Start();
        }

        private async Task CheckForUpd()
        {
            if (Properties.Settings.Default.disableUpdateNotify) return;

            int ThisBuild = int.Parse(new StreamReader(Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("MakuTweakerNew.BuildNumber.txt")!)
                .ReadToEnd()
                .Trim());

            string url = "https://raw.githubusercontent.com/AdderlyMark/MakuTweaker/refs/heads/main/ver.json";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var jsonData = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

                    if (jsonData != null && jsonData.ContainsKey("build"))
                    {
                        int latestBuild = int.Parse(jsonData["build"]);
                        if (latestBuild > ThisBuild)
                        {
                            Properties.Settings.Default.updIgnoredCount++;
                            Properties.Settings.Default.Save();
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var languageCode = Properties.Settings.Default.lang ?? "en";
                                var basel = Localization.LoadLocalization(languageCode, "base");
                                string title = "MakuTweaker Update";
                                string msg = basel["def"]["updatedialog"];
                                string trayMsg = basel["def"]["updatenotify"];
                                string dontShowText = basel["def"]["updatecheckb"];

                                if (Properties.Settings.Default.updIgnoredCount >= 5)
                                {
                                    var content = new StackPanel();
                                    var messageText = new TextBlock
                                    {
                                        Text = msg,
                                        TextWrapping = TextWrapping.Wrap,
                                        Margin = new Thickness(0, 0, 0, 15),
                                        FontSize = 16,
                                        FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable Display")
                                    };

                                    content.Children.Add(messageText);
                                    var dontShowCheckBox = new CheckBox
                                    {
                                        Content = dontShowText,
                                        FontSize = 14
                                    };
                                    content.Children.Add(dontShowCheckBox);

                                    var dialog = new iNKORE.UI.WPF.Modern.Controls.ContentDialog
                                    {
                                        Title = title,
                                        Content = content,
                                        PrimaryButtonText = basel["def"]["updatebutton"],
                                        CloseButtonText = basel["def"]["updatecancel"],
                                        DefaultButton = iNKORE.UI.WPF.Modern.Controls.ContentDialogButton.Primary
                                    };

                                    _ = dialog.ShowAsync().ContinueWith(t => {
                                        if (t.Result == iNKORE.UI.WPF.Modern.Controls.ContentDialogResult.Primary)
                                        {
                                            Application.Current.Dispatcher.Invoke(() => {
                                                if (dontShowCheckBox.IsChecked == true)
                                                {
                                                    Properties.Settings.Default.disableUpdateNotify = true;
                                                }
                                                Properties.Settings.Default.Save();
                                                Process.Start(new ProcessStartInfo("https://adderly.top/makutweaker") { UseShellExecute = true });
                                            });
                                        }
                                    });
                                }
                                else
                                {
                                    Icon trayIcon = new Icon(GetResourceStream("assets/icons/MakuT.ico"));
                                    TaskbarIcon _trayIcon = new TaskbarIcon { ToolTipText = "MakuTweaker", Icon = trayIcon };
                                    _trayIcon.ShowBalloonTip("MakuTweaker", trayMsg, BalloonIcon.Info);

                                    _trayIcon.TrayBalloonTipClicked += (sender, args) =>
                                    {
                                        Process.Start(new ProcessStartInfo("https://adderly.top/makutweaker") { UseShellExecute = true });
                                    };

                                    Task.Delay(8000).ContinueWith(t => _trayIcon.Dispatcher.Invoke(() => _trayIcon.Dispose()));
                                }
                            });
                        }
                        else
                        {
                            Properties.Settings.Default.updIgnoredCount = 0;
                            Properties.Settings.Default.Save();
                        }
                    }
                }
                catch { }
            }
        }

        private int checkWinVer()
        {
            string keyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            string valueName = "CurrentBuild";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
            {
                if (key != null)
                {
                    object value = key.GetValue(valueName);

                    if (value != null && int.TryParse(value.ToString(), out int build))
                    {
                        return build;
                    }
                }
            }
            return 19045;
        }
        public void UpdateSettingsButtonForExclusive(bool isProcessMgrActive, bool isExclusive)
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";

            if (isProcessMgrActive)
            {
                var pmgrLoc = Localization.LoadLocalization(languageCode, "pmgr");

                if (isExclusive)
                {
                    settingsText.Text = pmgrLoc.ContainsKey("main") && pmgrLoc["main"].ContainsKey("getback")
                        ? pmgrLoc["main"]["getback"]
                        : "Back";

                    settingsIcon.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Back;
                }
                else
                {
                    settingsText.Text = pmgrLoc.ContainsKey("main") && pmgrLoc["main"].ContainsKey("getfullscr")
                        ? pmgrLoc["main"]["getfullscr"]
                        : "Full Screen";
                    settingsIcon.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Forward;
                }
            }
            else
            {
                var basel = Localization.LoadLocalization(languageCode, "base");
                settingsText.Text = basel["lowtabs"]["set"];
                settingsIcon.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Setting;
            }
        }

        public void AnimateExclusiveModeTransition(bool isExclusive, bool animate = true)
        {
            if (!animate)
            {
                NavigationView_Root.BeginAnimation(iNKORE.UI.WPF.Modern.Controls.NavigationView.OpenPaneLengthProperty, null);
                SearchBox.BeginAnimation(UIElement.OpacityProperty, null);

                NavigationView_Root.OpenPaneLength = isExclusive ? 0 : 290;
                SearchBox.Opacity = isExclusive ? 0 : 1;
                SearchBox.IsHitTestVisible = !isExclusive;
                SearchBox.Visibility = isExclusive ? Visibility.Collapsed : Visibility.Visible;
                NavigationView_Root.IsPaneVisible = !isExclusive;
                return;
            }

            var ease = new CubicEase() { EasingMode = EasingMode.EaseInOut };
            var paneDuration = TimeSpan.FromMilliseconds(300);
            var searchDuration = TimeSpan.FromMilliseconds(200);

            DoubleAnimation paneAnimation = new DoubleAnimation()
            {
                To = isExclusive ? 0 : 290,
                Duration = paneDuration,
                EasingFunction = ease
            };
            DoubleAnimation searchAnimation = new DoubleAnimation()
            {
                To = isExclusive ? 0 : 1,
                Duration = searchDuration,
                EasingFunction = ease
            };

            SearchBox.IsHitTestVisible = !isExclusive;
            if (!isExclusive)
            {
                NavigationView_Root.IsPaneVisible = true;
                SearchBox.Visibility = Visibility.Visible;
            }
            NavigationView_Root.BeginAnimation(iNKORE.UI.WPF.Modern.Controls.NavigationView.OpenPaneLengthProperty, paneAnimation);
            SearchBox.BeginAnimation(UIElement.OpacityProperty, searchAnimation);
        }
    }
}