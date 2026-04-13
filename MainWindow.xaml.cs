using Hardcodet.Wpf.TaskbarNotification;
using MakuTweakerNew.Properties;
using MicaWPF.Controls;
using MicaWPF.Core.Enums;
using MicaWPF.Core.Helpers;
using MicaWPF.Core.Services;
using Microsoft.Win32;
using ModernWpf;
using ModernWpf.Media.Animation;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
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
        }

        public MainWindow()
        {
            InitializeComponent();
            if(checkWinVer() < 14393)
            {
                System.Windows.Forms.DialogResult old = System.Windows.Forms.MessageBox.Show("Your version of Windows is not supported. To use MakuTweaker, update your system to Windows 10 1607 or higher. Do you want to download MakuTweaker Legacy Windows Edition?\n\nВаша версия Windows неподдерживается. Для использования MakuTweaker, обновитесь до Windows 10 1607 или выше. Вы хотите скачать MakuTweaker для старых Windows?", "MakuTweaker", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Error);
                if(old == System.Windows.Forms.DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo("https://adderly.top/mt") { UseShellExecute = true });
                }
                Application.Current.Shutdown();
            }
            string buildVersion;
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("MakuTweakerNew.BuildLab.txt"))
            using (StreamReader reader = new StreamReader(stream))
            {
                buildVersion = reader.ReadToEnd();
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
        }

        private void ApplyTheme(WindowsTheme theme)
        {
            MicaWPFServiceUtility.ThemeService.ChangeTheme(theme);

            if (theme == WindowsTheme.Dark)
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                this.Foreground = System.Windows.Media.Brushes.White;
                this.Separator.Stroke = System.Windows.Media.Brushes.White;
            }
            else
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                this.Foreground = System.Windows.Media.Brushes.Black;
                this.Separator.Stroke = System.Windows.Media.Brushes.Black;
            }
        }

        private void ExpTimer()
        {
            ExpRestart = new DispatcherTimer();
            ExpRestart.Interval = TimeSpan.FromMilliseconds(2000);
            ExpRestart.Tick += ExpRestart_Tick;
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Category.SelectedIndex == -1) return;

            _transitionInfo = new EntranceNavigationTransitionInfo();
            switch (Category.SelectedIndex)
            {
                case 0: MainFrame.Navigate(typeof(Explorer), null, _transitionInfo); break;
                case 1: MainFrame.Navigate(typeof(WindowsUpdate), null, _transitionInfo); break;
                case 2: MainFrame.Navigate(typeof(SysAndRec), null, _transitionInfo); break;
                case 3: MainFrame.Navigate(typeof(UWP), null, _transitionInfo); break;
                case 4: MainFrame.Navigate(typeof(Personalization), null, _transitionInfo); break;
                case 5: MainFrame.Navigate(typeof(ContextMenu), null, _transitionInfo); break;
                case 6: MainFrame.Navigate(typeof(QuickSet), null, _transitionInfo); break;
                case 7: MainFrame.Navigate(typeof(WindowsComponents), null, _transitionInfo); break;
                case 8: MainFrame.Navigate(typeof(Act), null, _transitionInfo); break;
                case 9: MainFrame.Navigate(typeof(Perf), null, _transitionInfo); break;
                case 10: MainFrame.Navigate(typeof(SAT), null, _transitionInfo); break;
                case 11: MainFrame.Navigate(typeof(ProcessMGR), null, _transitionInfo); break;
                case 12: MainFrame.Navigate(typeof(PCI), null, _transitionInfo); break;
            }

            Properties.Settings.Default.lastPage = Category.SelectedIndex;
            Settings.Default.Save();
        }

		private void MicaWindow_Loaded(object sender, RoutedEventArgs e)
		{
            Category.SelectedIndex = Properties.Settings.Default.lastPage;

            Enum.TryParse(Settings.Default.style, out BackdropType bd);
            MicaWPFServiceUtility.ThemeService.EnableBackdrop(this, bd);
        }
        private bool isAnimating = false;

        public async void ChSt(string st)
        {
            if (isAnimating) return;

            try
            {
                isAnimating = true;

                AnimY(status, 300, 26, 0);    
                status.Text = st;

                await Task.Delay(5000);      

                AnimY(status, 300, 0, 33);    
            }
            finally
            {
                isAnimating = false;      
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
                c4.Content = basel["catname"]["uwp"];
                c5.Content = basel["catname"]["per"];
                c6.Content = basel["catname"]["cm"];
                c7.Content = basel["catname"]["quick"];
                c8.Content = basel["catname"]["compon"];
                c9.Content = basel["catname"]["act"];
                c10.Content = basel["catname"]["perf"];
                c11.Content = basel["catname"]["sat"];
                c12.Content = basel["catname"]["procmgr"];
                c13.Content = basel["catname"]["pci"];
                rexplorer.Label = basel["lowtabs"]["rexp"];
                settingsButton.Label = basel["lowtabs"]["set"];
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "MakuTweaker Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                System.Windows.Forms.Application.Restart();
                System.Windows.Application.Current.Shutdown();
            }
        }
        private void AnimY(UIElement element, double durationMilliseconds, double from, double to)
        {
            double currentY = 16;

            if (element.RenderTransform != null && element.RenderTransform is TranslateTransform transform)
            {
                currentY = transform.Y;
            }
            else if (element.RenderTransform != null && element.RenderTransform is MatrixTransform matrixTransform)
            {
                currentY = matrixTransform.Matrix.OffsetY;
            }

            var moveDownAnimation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMilliseconds),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut },
            };
            if (element.RenderTransform == null || element.RenderTransform is not TranslateTransform)
            {
                element.RenderTransform = new TranslateTransform();
            }

            var translateTransform = (TranslateTransform)element.RenderTransform;
            translateTransform.BeginAnimation(TranslateTransform.YProperty, moveDownAnimation);

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
            Category.SelectedIndex = -1;
            MainFrame.Navigate(typeof(SettingsAbout), null, _transitionInfo);
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (Category.SelectedIndex == -1)
            {
                settingsButton.IsEnabled = false;
            }
            else
            {
                settingsButton.IsEnabled = true;
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

                    try
                    {
                        var jsonData = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

                        if (jsonData.ContainsKey("build"))
                        {
                            string lb = jsonData["build"];
                            if (int.Parse(lb) > ThisBuild)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    Icon trayIcon = new Icon(GetResourceStream("assets/icons/MakuT.ico"));
                                    TaskbarIcon _trayIcon = new TaskbarIcon
                                    {
                                        ToolTipText = "MakuTweaker",
                                        Icon = trayIcon
                                    };

                                    string currentLang = Properties.Settings.Default.lang;
                                    if (currentLang is "ru" or "uk" or "be" or "kk" or "az")
                                    {
                                        _trayIcon.ShowBalloonTip("MakuTweaker", "Доступно обновление MakuTweaker!\nНажмите на уведомление, чтобы перейти на страницу загрузки.", BalloonIcon.Info);
                                    }
                                    else
                                    {
                                        _trayIcon.ShowBalloonTip("MakuTweaker", "An update for MakuTweaker is available!\nClick the notification to go to the download page.", BalloonIcon.Info);
                                    }

                                    _trayIcon.TrayBalloonTipClicked += (sender, args) =>
                                    {
                                        Process.Start(new ProcessStartInfo("https://adderly.top/makutweaker") { UseShellExecute = true });
                                    };

                                    Task.Delay(8000).ContinueWith(t =>
                                    {
                                        _trayIcon.Dispatcher.Invoke(() => _trayIcon.Dispose());
                                    });
                                });
                            }
                            else
                            {
                            }
                        }
                        else
                        {
                        }
                    }
                    catch (JsonException ex)
                    {
                    }
                }
                catch (HttpRequestException e)
                {
                }
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
    }
}