using MakuTweakerNew.Properties;
using MicaWPF.Core.Enums;
using MicaWPF.Core.Services;
using Microsoft.Win32;
using ModernWpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MakuTweakerNew
{
    public partial class SettingsAbout : Page
    {
        private const string AppPathsRoot = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";
        private readonly string[] _aliases = { "makut.exe", "maku.exe", "mt.exe" };
        private MainWindow mw = (MainWindow)System.Windows.Application.Current.MainWindow;
        bool isLoaded = false;

        public SettingsAbout()
        {
            InitializeComponent();
            WinRAliasCheck.IsChecked = CheckWinRAliasStatus();
            disableAnalytics.IsChecked = Properties.Settings.Default.disableTelemetry;
            string buildSuffix = GetBuildNumberFromResources();
            credN.Text = $"5.7.3 ({buildSuffix})\nMark Adderly\nNikitori\nMaksimCeleron, Bruh_SomeBody, Massgrave";
            if (string.IsNullOrEmpty(Settings.Default.lang))
            {
                string systemLang = CultureInfo.CurrentUICulture.Name.ToLower();
                string isoLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
                Settings.Default.lang = systemLang switch
                {
                    "zh-tw" or "zh-hk" or "zh-mo" => "tw",
                    "zh-cn" or "zh-sg" or "zh-chs" => "zh",

                    _ => isoLang switch
                    {
                        "uk" => "uk",   // Украинский
                        "cs" => "cs",   // Чешский
                        "ru" => "ru",   // Русский
                        "az" => "az",   // Азербайджанский
                        "es" => "es",   // Испанский
                        "tl" => "tl",   // Тагальский
                        "fil" => "tl",  // Филиппинский
                        "tr" => "tr",   // Турецкий
                        "ko" => "ko",   // Корейский
                        "zh" => "zh",   // Китайский
                        "it" => "it",   // Итальянский
                        "de" => "de",   // Немецкий
                        "fr" => "fr",   // Французский
                        "be" => "be",   // Белорусский
                        "vi" => "vi",   // Вьетнамский
                        "id" => "id",   // Индонезийский
                        "hi" => "hi",   // Хинди
                        "ja" => "ja",   // Японский
                        "kk" => "kk",   // Казахский
                        "pt" => "pt",   // Португальский
                        "lv" => "lv",   // Латвийский
                        "fi" => "fi",   // Финский
                        "et" => "et",   // Эстонский
                        "pl" => "pl",   // Польский
                        "th" => "th",   // Тайский
                        _ => "en"       // Стандартный (Английский)
                    }
                };

                Settings.Default.Save();
            }

            foreach (ComboBoxItem item in lang.Items)
            {
                if (item.Tag?.ToString() == Settings.Default.lang)
                {
                    lang.SelectedItem = item;
                    break;
                }
            }

            if (lang.SelectedIndex == -1)
            {
                lang.SelectedIndex = 0;
            }

            Settings.Default.langSI = lang.SelectedIndex;

            if (checkWinVer() < 22000)
            {
                style.Visibility = Visibility.Collapsed;
                styleL.Visibility = Visibility.Collapsed;
            }

            var currentTheme = MicaWPFServiceUtility.ThemeService.CurrentTheme;
            theme.SelectedIndex = currentTheme == WindowsTheme.Dark ? 1 : 0;

            style.SelectedIndex = Settings.Default.style switch
            {
                "Mica" => 0,
                "Tabbed" => 1,
                "Acrylic" => 2,
                "None" => 3,
                _ => 0
            };

            relang();
            isLoaded = true;
        }

        private void DisableAnalytics_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("In the GitHub version, this function is a stub. This checkbox controls the blocking of Google Analytics methods.", "MakuTweaker Github",MessageBoxButton.OK,MessageBoxImage.Information);
            Properties.Settings.Default.disableTelemetry = (disableAnalytics.IsChecked == true);
            Properties.Settings.Default.Save();
        }

        private bool CheckWinRAliasStatus()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey($@"{AppPathsRoot}\mt.exe"))
                {
                    return key?.GetValue("") != null;
                }
            }
            catch { return false; }
        }

        private void WinRAliasCheck_Click(object sender, RoutedEventArgs e)
        {
            bool shouldCreate = WinRAliasCheck.IsChecked ?? false;

            try
            {
                string currentExePath = Assembly.GetExecutingAssembly().Location;
                if (currentExePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    currentExePath = Path.ChangeExtension(currentExePath, ".exe");
                }
                string currentDir = Path.GetDirectoryName(currentExePath);

                if (shouldCreate)
                {
                    foreach (var alias in _aliases)
                    {
                        using (RegistryKey key = Registry.LocalMachine.CreateSubKey($@"{AppPathsRoot}\{alias}", true))
                        {
                            key.SetValue("", currentExePath);
                            key.SetValue("Path", currentDir);
                        }
                    }
                }
                else
                {
                    foreach (var alias in _aliases)
                    {
                        Registry.LocalMachine.DeleteSubKeyTree($@"{AppPathsRoot}\{alias}", false);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("MakuTweaker Registry Error", "MakuTweaker Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                WinRAliasCheck.IsChecked = !shouldCreate;
            }
            catch (Exception ex)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(ex.Message, "MakuTweaker Registry Error", MessageBoxButton.OK, MessageBoxImage.Error);
                WinRAliasCheck.IsChecked = !shouldCreate;
            }
        }
        private int checkWinVer()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                var value = key?.GetValue("CurrentBuild");
                return (value != null && int.TryParse(value.ToString(), out int build)) ? build : 19045;
            }
            catch { return 19045; }
        }

        private void OpenUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

        private void Button_Click(object sender, RoutedEventArgs e) => OpenUrl("https://adderly.top");

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => OpenUrl("https://boosty.to/adderly");

        private void Image_MouseLeftButtonUp_2(object sender, MouseButtonEventArgs e) => OpenUrl("https://t.me/adderly324");

        private void Image_MouseLeftButtonUp_3(object sender, MouseButtonEventArgs e) => OpenUrl("https://youtube.com/@MakuAdarii");

        private void theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoaded) return;

            bool isDark = theme.SelectedIndex == 1;
            Settings.Default.theme = isDark ? "Dark" : "Light";
            Settings.Default.Save();

            var executablePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(executablePath))
            {
                Process.Start(executablePath);
            }
            Application.Current.Shutdown();
        }

        private void lang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoaded) return;
            if (lang.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                Settings.Default.lang = selectedItem.Tag.ToString();
            }

            Settings.Default.langSI = lang.SelectedIndex;
            Settings.Default.Save();

            mw.LoadLang(Settings.Default.lang);
            relang();
        }

        private void style_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoaded) return;
            var styleData = style.SelectedIndex switch
            {
                0 => (Type: BackdropType.Mica, Name: "Mica"),
                1 => (Type: BackdropType.Tabbed, Name: "Tabbed"),
                2 => (Type: BackdropType.Acrylic, Name: "Acrylic"),
                _ => (Type: BackdropType.None, Name: "None")
            };

            MicaWPFServiceUtility.ThemeService.EnableBackdrop(mw, styleData.Type);
            Settings.Default.style = styleData.Name;
            Settings.Default.Save();
        }

        private void relang()
        {
            var languageCode = Settings.Default.lang ?? "en";
            var ab = MainWindow.Localization.LoadLocalization(languageCode, "ab");
            var abEn = languageCode != "en" ? MainWindow.Localization.LoadLocalization("en", "ab") : ab;
            var b = MainWindow.Localization.LoadLocalization(languageCode, "base");
            var bEn = languageCode != "en" ? MainWindow.Localization.LoadLocalization("en", "base") : b;

            string GetText(dynamic dict, dynamic dictEn, string category, string key)
            {
                try { return dict[category][key]; }
                catch { return dictEn[category][key]; }
            }

            credL.Text = ab["main"]["credL"];
            label.Text = ab["main"]["label"];
            web.Content = ab["main"]["atop"];
            langL.Text = ab["main"]["lang"];
            styleL.Text = ab["main"]["st"];
            themeL.Text = ab["main"]["th"];
            l.Content = " " + ab["main"]["l"];
            d.Content = " " + ab["main"]["d"];
            off.Content = " " + b["def"]["off"];
            configsL.Text = ab["main"]["cfg_title"];
            savePresetBtn.Content = ab["main"]["cfg_save"];
            importPresetBtn.Content = ab["main"]["cfg_import"];
            tooltip.Content = ab["main"]["cfg_info"];
            WinRAliasCheck.Content = ab["main"]["winr"];
            tooltipalias.Content = ab["main"]["winrinfo"];
            disableAnalytics.Content = GetText(ab, abEn, "main", "disabletelemetry");
            teltooltip.Content = GetText(ab, abEn, "main", "telemetryabt");
        }

        //CONFIG
        //CONFIG
        //CONFIG
        //CONFIG
        //CONFIG

        private string GetCmdOutput(string command, string arguments)
        {
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = command;
                    p.StartInfo.Arguments = arguments;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    return output.ToLower();
                }
            }
            catch { return ""; }
        }

        private void RunCmdCommand(string fileName, string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using (Process p = new Process()) { p.StartInfo = psi; p.Start(); p.WaitForExit(); }
        }

        private bool IsPowerSettingZero(string output)
        {
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length >= 2)
            {
                return lines[lines.Length - 1].Contains("0x00000000") && lines[lines.Length - 2].Contains("0x00000000");
            }
            return false;
        }

        public class MakuPreset
        {
            public bool Exp_Hidden { get; set; }
            public bool Exp_Ext { get; set; }
            public bool Exp_PcHome { get; set; }
            public bool Exp_Gallery { get; set; }
            public bool Exp_ShowPc { get; set; }
            public bool Exp_Shortcut { get; set; }
            public bool Wpd_AutoUpdate { get; set; }
            public bool Wpd_QualityDrivers { get; set; }
            public bool Wpd_Reserves { get; set; }
            public bool Sys_Bitlocker { get; set; }
            public bool Sys_Chkdsk { get; set; }
            public bool Sys_CoreIsol { get; set; }
            public bool Sys_Hybern { get; set; }
            public bool Sys_Telemetry { get; set; }
            public bool Sys_SmartScreen { get; set; }
            public bool Sys_Uac { get; set; }
            public bool Sys_Sticky { get; set; }
            public bool Sys_Bing { get; set; }
            public bool Sys_SleepTimeout { get; set; }
            public string Per_RenameTemplate { get; set; }
            public int Per_ColorIndex { get; set; }
            public bool Per_ClassicMenu { get; set; }
            public bool Per_MenuDelay { get; set; }
            public bool Per_SmallWindows { get; set; }
            public bool Per_Blur { get; set; }
            public bool Per_Transparency { get; set; }
            public bool Per_DarkTheme { get; set; }
            public bool Per_Verbose { get; set; }
            public bool Per_EndTask { get; set; }
            public bool Per_DisableLogo { get; set; }
            public bool Per_DisableAnim { get; set; }
            public bool Adv_Vbs { get; set; }
            public bool Adv_Swap { get; set; }
            public bool Adv_Ttl { get; set; }
            public bool Adv_OldBootloader { get; set; }
            public bool Adv_AdvancedBoot { get; set; }
            public bool Adv_DisIndex { get; set; }
        }

        private void SavePreset_Click(object sender, RoutedEventArgs e)
        {
            var languageCode = Settings.Default.lang ?? "en";
            var ab = MainWindow.Localization.LoadLocalization(languageCode, "ab");
            try
            {
                string bcdCurrent = GetCmdOutput("bcdedit", "/enum {current}");
                string bcdGlobal = GetCmdOutput("bcdedit", "/enum {globalsettings}");
                string powerVideo = GetCmdOutput("powercfg", "/q SCHEME_CURRENT SUB_VIDEO VIDEOIDLE");
                string powerSleep = GetCmdOutput("powercfg", "/q SCHEME_CURRENT SUB_SLEEP STANDBYIDLE");

                var highlightColor = Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors")?.GetValue("HightLight")?.ToString();
                int colorIndex = highlightColor switch
                {
                    "51 153 255" => 0,
                    "0 100 100" => 1,
                    "180 0 180" => 2,
                    "0 90 30" => 3,
                    "100 40 0" => 4,
                    "135 0 0" => 5,
                    "15 0 120" => 6,
                    "40 40 40" => 7,
                    _ => 0
                };

                MakuPreset preset = new MakuPreset
                {
                    //explorer
                    Exp_Hidden = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", false)?.GetValue("Hidden")?.Equals(1) ?? false,
                    Exp_Ext = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", false)?.GetValue("HideFileExt")?.Equals(0) ?? false,
                    Exp_PcHome = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", false)?.GetValue("LaunchTo")?.Equals(1) ?? false,
                    Exp_Gallery = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}", false)?.GetValue("System.IsPinnedToNameSpaceTree")?.Equals(0) ?? false,
                    Exp_ShowPc = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", false)?.GetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}")?.Equals(0) ?? false,
                    Exp_Shortcut = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates", false)?.GetValue("ShortcutNameTemplate")?.Equals("%s.lnk") ?? false,
                    //wu
                    Wpd_AutoUpdate = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU")?.GetValue("NoAutoUpdate")?.Equals(1) ?? false,
                    Wpd_QualityDrivers = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate")?.GetValue("ExcludeWUDriversInQualityUpdate")?.Equals(1) ?? false,
                    Wpd_Reserves = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ReserveManager")?.GetValue("ShippedWithReserves")?.Equals(0) ?? false,
                    //systemAndRec
                    Sys_Bitlocker = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\BitLocker")?.GetValue("PreventDeviceEncryption")?.Equals(1) ?? false,
                    Sys_Chkdsk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager")?.GetValue("AutoChkTimeout")?.Equals(60) ?? false,
                    Sys_CoreIsol = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios")?.GetValue("HypervisorEnforcedCodeIntegrity")?.Equals(0) ?? false,
                    Sys_Hybern = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power")?.GetValue("HibernateEnabled")?.Equals(0) ?? false,
                    Sys_Telemetry = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Policies\DataCollection")?.GetValue("AllowTelemetry")?.Equals(0) ?? false,
                    Sys_SmartScreen = (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", false)?.GetValue("EnableSmartScreen")?.Equals(0) ?? false) || (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", false)?.GetValue("SmartScreenEnabled")?.Equals("Off") ?? false),
                    Sys_Uac = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", false)?.GetValue("EnableLUA")?.Equals(0) ?? false,
                    Sys_Sticky = (Registry.CurrentUser.OpenSubKey(@"Control Panel\Accessibility\StickyKeys", false)?.GetValue("Flags")?.Equals("506") ?? false) || (Registry.CurrentUser.OpenSubKey(@"Control Panel\Accessibility\ToggleKeys", false)?.GetValue("Flags")?.Equals("58") ?? false),
                    Sys_Bing = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\Explorer", false)?.GetValue("DisableSearchBoxSuggestions")?.Equals(1) ?? false,
                    Sys_SleepTimeout = IsPowerSettingZero(powerVideo) && IsPowerSettingZero(powerSleep),
                    //adv
                    Adv_Swap = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management")?.GetValue("PagingFiles") is string[] arr ? arr.All(s => string.IsNullOrWhiteSpace(s)) : true,
                    Adv_Vbs = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard")?.GetValue("EnableVirtualizationBasedSecurity")?.Equals(0) ?? false,
                    Adv_Ttl = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters")?.GetValue("DefaultTTL")?.Equals(65) ?? false,
                    Adv_OldBootloader = bcdCurrent.Contains("bootmenupolicy") && bcdCurrent.Contains("legacy"),
                    Adv_AdvancedBoot = Regex.IsMatch(bcdGlobal, @"advancedoptions\s+yes", RegexOptions.IgnoreCase),
                    Adv_DisIndex = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WSearch")?.GetValue("Start")?.Equals(4) ?? false,
                    //personalization
                    Per_RenameTemplate = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates")?.GetValue("RenameNameTemplate")?.ToString() ?? "",
                    Per_ColorIndex = colorIndex,
                    Per_SmallWindows = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop\WindowMetrics")?.GetValue("CaptionHeight")?.ToString() == "-270",
                    Per_Blur = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System")?.GetValue("DisableAcrylicBackgroundOnLogon")?.Equals(1) ?? false,
                    Per_Transparency = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")?.GetValue("EnableTransparency")?.Equals(0) ?? false,
                    Per_DarkTheme = (Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")?.GetValue("AppsUseLightTheme") is int a && a == 0) && (Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")?.GetValue("SystemUsesLightTheme") is int b && b == 0),
                    Per_Verbose = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System")?.GetValue("verbosestatus")?.Equals(1) ?? false,
                    Per_EndTask = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings")?.GetValue("TaskbarEndTask") is int v && v == 1,
                    Per_ClassicMenu = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32") != null,
                    Per_MenuDelay = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop")?.GetValue("MenuShowDelay")?.ToString() == "50",
                    Per_DisableLogo = IsBcdValueEnabled(bcdGlobal, "custom:16000067", "bootlogo", "nobootlogo"),
                    Per_DisableAnim = IsBcdValueEnabled(bcdGlobal, "custom:16000069", "nobootuxprogress")
                };

                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "MakuTweaker Config (*.mktw)|*.mktw",
                    FileName = $"{Environment.UserName}.mktw",
                    Title = ab["main"]["cfg_save"]
                };

                if (sfd.ShowDialog() == true)
                {
                    string json = JsonSerializer.Serialize(preset, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(sfd.FileName, json);
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(ab["main"]["cfg_svsuccess"], "MakuTweaker", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show($"{ab["main"]["cfg_error"]}\n\n{ex.Message}", "MakuTweaker", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void ProcessPresetImport(string filePath)
        {
            var languageCode = Settings.Default.lang ?? "en";
            var ab = MainWindow.Localization.LoadLocalization(languageCode, "ab");
            try
            {
                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                MakuPreset preset = JsonSerializer.Deserialize<MakuPreset>(json);

                if (preset != null)
                {
                    importProgress.Value = 0;
                    importProgress.Visibility = Visibility.Visible;
                    importPresetBtn.IsEnabled = false;
                    savePresetBtn.IsEnabled = false;

                    var progress = new Progress<int>(value => importProgress.Value = value);
                    await System.Threading.Tasks.Task.Run(() => ApplyPresetBackground(preset, progress));

                    importProgress.Visibility = Visibility.Collapsed;
                    importPresetBtn.IsEnabled = true;
                    savePresetBtn.IsEnabled = true;
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(ab["main"]["cfg_ldsuccess"], "MakuTweaker", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                importProgress.Visibility = Visibility.Collapsed;
                importPresetBtn.IsEnabled = true;
                savePresetBtn.IsEnabled = true;
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show($"{ab["main"]["cfg_error1"]}\n\n{ex.Message}", "MakuTweaker", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportPreset_Click(object sender, RoutedEventArgs e)
        {
            var languageCode = Settings.Default.lang ?? "en";
            var ab = MainWindow.Localization.LoadLocalization(languageCode, "ab");

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "MakuTweaker Config (*.mktw)|*.mktw",
                Title = ab["main"]["cfg_import"]
            };

            if (ofd.ShowDialog() == true)
            {
                ProcessPresetImport(ofd.FileName);
            }
        }

        private void ApplyPresetBackground(MakuPreset preset, IProgress<int> progress)
        {
            int totalSteps = 37;
            int currentStep = 0;

            Action ReportProgress = () =>
            {
                currentStep++;
                progress.Report((currentStep * 100) / totalSteps);
            };

            // explorer

            if (preset.Exp_Hidden) Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("Hidden", 1);
            else Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("Hidden", 0);
            ReportProgress();

            if (preset.Exp_Ext) Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("HideFileExt", 0);
            else Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("HideFileExt", 1);
            ReportProgress();

            if (preset.Exp_PcHome) Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("LaunchTo", 1);
            else Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("LaunchTo", 2);
            ReportProgress();

            if (preset.Exp_Gallery) Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}").SetValue("System.IsPinnedToNameSpaceTree", 0);
            else Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}").SetValue("System.IsPinnedToNameSpaceTree", 1);
            ReportProgress();

            if (preset.Exp_ShowPc) Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel").SetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0);
            else Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel").SetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 1);
            ReportProgress();

            if (preset.Exp_Shortcut)
            {
                Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates").SetValue("ShortcutNameTemplate", "%s.lnk");
            }
            else
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates", true))
                {
                    key?.DeleteValue("ShortcutNameTemplate", false);
                }
            }
            ReportProgress();

            // sysAndRec

            if (preset.Sys_Bitlocker) Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\BitLocker").SetValue("PreventDeviceEncryption", 1, RegistryValueKind.DWord);
            else Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\BitLocker").SetValue("PreventDeviceEncryption", 0, RegistryValueKind.DWord);
            ReportProgress();

            if (preset.Sys_Chkdsk) Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager").SetValue("AutoChkTimeout", 60);
            else Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager").SetValue("AutoChkTimeout", 8);
            ReportProgress();

            if (preset.Sys_CoreIsol) Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios").SetValue("HypervisorEnforcedCodeIntegrity", 0);
            else Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios").SetValue("HypervisorEnforcedCodeIntegrity", 1);
            ReportProgress();

            if (preset.Sys_Hybern) RunCmdCommand("cmd.exe", "/C powercfg /h off");
            else RunCmdCommand("cmd.exe", "/C powercfg /h on");
            ReportProgress();

            if (preset.Sys_Uac)
            {
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("EnableLUA", 0);
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments")?.SetValue("SaveZoneInformation", 1, RegistryValueKind.DWord);
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Associations")?.SetValue("LowRiskFileTypes", ".exe;.msi;.bat;", RegistryValueKind.String);
            }
            else Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("EnableLUA", 1);
            ReportProgress();

            if (preset.Sys_SmartScreen)
            {
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("EnableSmartScreen", 0);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer").SetValue("SmartScreenEnabled", "Off", RegistryValueKind.String);
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments").SetValue("SaveZoneInformation", 1, RegistryValueKind.DWord);
            }
            else
            {
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("EnableSmartScreen", 1);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer").SetValue("SmartScreenEnabled", "Warn", RegistryValueKind.String);
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments").SetValue("SaveZoneInformation", 0, RegistryValueKind.DWord);
            }
            ReportProgress();

            if (preset.Sys_Sticky)
            {
                Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\StickyKeys").SetValue("Flags", "506");
                Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\Keyboard Response").SetValue("Flags", "122");
                Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\ToggleKeys").SetValue("Flags", "58");
            }
            else
            {
                Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\StickyKeys").SetValue("Flags", "510");
                Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\Keyboard Response").SetValue("Flags", "126");
                Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\ToggleKeys").SetValue("Flags", "62");
            }
            ReportProgress();

            if (preset.Sys_Bing) Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer").SetValue("DisableSearchBoxSuggestions", 1);
            else Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer").SetValue("DisableSearchBoxSuggestions", 0);
            ReportProgress();

            if (preset.Sys_SleepTimeout)
            {
                RunCmdCommand("powercfg", "-change -monitor-timeout-ac 0"); RunCmdCommand("powercfg", "-change -monitor-timeout-dc 0");
                RunCmdCommand("powercfg", "-change -standby-timeout-ac 0"); RunCmdCommand("powercfg", "-change -standby-timeout-dc 0");
            }
            else
            {
                RunCmdCommand("powercfg", "-change -monitor-timeout-ac 10"); RunCmdCommand("powercfg", "-change -monitor-timeout-dc 5");
                RunCmdCommand("powercfg", "-change -standby-timeout-ac 30"); RunCmdCommand("powercfg", "-change -standby-timeout-dc 15");
            }
            ReportProgress();

            if (preset.Sys_Telemetry)
            {
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Policies\DataCollection").SetValue("AllowTelemetry", 0);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection").SetValue("AllowTelemetry", 0);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection").SetValue("MaxTelemetryAllowed", 0);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform").SetValue("NoGenTicket", 1);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection").SetValue("DoNotShowFeedbackNotifications", 1);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat").SetValue("AITEnable", 0);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat").SetValue("AllowTelemetry", 0);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat").SetValue("DisableEngine", 1);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat").SetValue("DisableInventory", 1);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat").SetValue("DisablePCA", 1);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat").SetValue("DisableUAR", 1);
            }
            else
            {
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Policies\DataCollection").SetValue("AllowTelemetry", 1);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection").SetValue("AllowTelemetry", 1);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection").SetValue("MaxTelemetryAllowed", 1);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform").SetValue("NoGenTicket", 0);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection").SetValue("DoNotShowFeedbackNotifications", 0);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat").SetValue("AITEnable", 1);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat").SetValue("AllowTelemetry", 1);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat").SetValue("DisableEngine", 0);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat").SetValue("DisableInventory", 0);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat").SetValue("DisablePCA", 0);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat").SetValue("DisableUAR", 0);
            }
            ReportProgress();

            // wu
            if (preset.Wpd_AutoUpdate)
            {
                var wuKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate");
                wuKey.SetValue("DoNotConnectToWindowsUpdateInternetLocations", 1, RegistryValueKind.DWord);
                wuKey.SetValue("DisableWindowsUpdateAccess", 1, RegistryValueKind.DWord);
                wuKey.SetValue("DisableDualScan", 1, RegistryValueKind.DWord);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU").SetValue("NoAutoUpdate", 1, RegistryValueKind.DWord);
                try
                {
                    Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\wuauserv").SetValue("Start", 4);
                    Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\UsoSvc").SetValue("Start", 4);
                    Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\WaaSMedicSvc").SetValue("Start", 4);
                }
                catch { }
                RunCmdCommand("schtasks", "/change /tn \"\\Microsoft\\Windows\\WindowsUpdate\\Scheduled Start\" /disable");
                RunCmdCommand("schtasks", "/change /tn \"\\Microsoft\\Windows\\UpdateOrchestrator\\Universal Orchestrator Start\" /disable");
                RunCmdCommand("cmd.exe", "/c \"echo 127.0.0.1 index.wp.microsoft.com >> %windir%\\system32\\drivers\\etc\\hosts\"");
                RunCmdCommand("cmd.exe", "/c \"echo 127.0.0.1 update.microsoft.com >> %windir%\\system32\\drivers\\etc\\hosts\"");
                RunCmdCommand("cmd.exe", "/c \"echo 127.0.0.1 slscr.update.microsoft.com >> %windir%\\system32\\drivers\\etc\\hosts\"");
                RunCmdCommand("cmd.exe", "/c \"echo 127.0.0.1 fe2.update.microsoft.com >> %windir%\\system32\\drivers\\etc\\hosts\"");
                try { RunCmdCommand("taskkill", "/f /im wuauclt.exe"); RunCmdCommand("taskkill", "/f /im updatenotificationmgr.exe"); RunCmdCommand("net", "stop wuauserv /y"); RunCmdCommand("net", "stop bits /y"); RunCmdCommand("net", "stop UsoSvc /y"); } catch { }
                RunCmdCommand("cmd.exe", "/c ipconfig /flushdns");
            }
            else
            {
                var auKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", true);
                auKey?.SetValue("NoAutoUpdate", 0, RegistryValueKind.DWord);
                var wuKeyRestore = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", true);
                if (wuKeyRestore != null)
                {
                    wuKeyRestore.SetValue("DoNotConnectToWindowsUpdateInternetLocations", 0, RegistryValueKind.DWord);
                    wuKeyRestore.SetValue("DisableWindowsUpdateAccess", 0, RegistryValueKind.DWord);
                    wuKeyRestore.SetValue("DisableDualScan", 0, RegistryValueKind.DWord);
                }
                try
                {
                    Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\wuauserv").SetValue("Start", 3);
                    Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\UsoSvc").SetValue("Start", 2);
                    Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\WaaSMedicSvc").SetValue("Start", 3);
                    RunCmdCommand("net", "start UsoSvc");
                }
                catch { }
                RunCmdCommand("schtasks", "/change /tn \"\\Microsoft\\Windows\\WindowsUpdate\\Scheduled Start\" /enable");
                RunCmdCommand("schtasks", "/change /tn \"\\Microsoft\\Windows\\UpdateOrchestrator\\Universal Orchestrator Start\" /enable");
                RunCmdCommand("powershell.exe", "-Command \"(Get-Content $env:windir\\system32\\drivers\\etc\\hosts) | Where-Object { $_ -notmatch 'microsoft.com' } | Set-Content $env:windir\\system32\\drivers\\etc\\hosts\"");
                RunCmdCommand("cmd.exe", "/c ipconfig /flushdns");
            }
            ReportProgress();

            if (preset.Wpd_QualityDrivers)
            {
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate").SetValue("ExcludeWUDriversInQualityUpdate", 1);
            }
            else Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate").SetValue("ExcludeWUDriversInQualityUpdate", 0);
            ReportProgress();

            if (preset.Wpd_Reserves) Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ReserveManager").SetValue("ShippedWithReserves", 0);
            else Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ReserveManager").SetValue("ShippedWithReserves", 1);
            ReportProgress();

            //adv
            if (preset.Adv_Swap) Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management").SetValue("PagingFiles", new string[] { }, RegistryValueKind.MultiString);
            else Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management").SetValue("PagingFiles", new string[] { @"?:\pagefile.sys" }, RegistryValueKind.MultiString);
            ReportProgress();

            if (preset.Adv_Vbs)
            {
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord);
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("RequirePlatformSecurityFeatures", 0, RegistryValueKind.DWord);
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa").SetValue("LsaCfgFlags", 0, RegistryValueKind.DWord);
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity").SetValue("Enabled", 0, RegistryValueKind.DWord);
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("RequireMicrosoftSignedBootChain", 0, RegistryValueKind.DWord);
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("KernelDMAProtection", 0, RegistryValueKind.DWord);
                RunCmdCommand("cmd.exe", "/c bcdedit /set hypervisorlaunchtype off");
            }
            else
            {
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("EnableVirtualizationBasedSecurity", 1, RegistryValueKind.DWord);
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("RequirePlatformSecurityFeatures", 3, RegistryValueKind.DWord);
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa").SetValue("LsaCfgFlags", 1, RegistryValueKind.DWord);
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity").SetValue("Enabled", 1, RegistryValueKind.DWord);
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("RequireMicrosoftSignedBootChain", 1, RegistryValueKind.DWord);
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("KernelDMAProtection", 1, RegistryValueKind.DWord);
                RunCmdCommand("cmd.exe", "/c bcdedit /set hypervisorlaunchtype auto");
            }
            ReportProgress();

            using (var keyIPv4 = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"))
            using (var keyIPv6 = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\TCPIP6\Parameters"))
            {
                if (preset.Adv_Ttl)
                {
                    keyIPv4?.SetValue("DefaultTTL", 65, RegistryValueKind.DWord);
                    keyIPv6?.SetValue("DefaultTTL", 65, RegistryValueKind.DWord);
                }
                else
                {
                    keyIPv4?.DeleteValue("DefaultTTL", false);
                    keyIPv6?.DeleteValue("DefaultTTL", false);
                }
            }
            ReportProgress();

            if (preset.Adv_OldBootloader)
                RunCmdCommand("bcdedit.exe", "/set \"{current}\" bootmenupolicy legacy");
            else
                RunCmdCommand("bcdedit.exe", "/set \"{current}\" bootmenupolicy standard");
            ReportProgress();

            if (preset.Adv_AdvancedBoot)
                RunCmdCommand("bcdedit.exe", "/set \"{globalsettings}\" advancedoptions true");
            else
                RunCmdCommand("bcdedit.exe", "/set \"{globalsettings}\" advancedoptions false");
            ReportProgress();

            if (preset.Adv_DisIndex)
            {
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WSearch", "Start", 4, RegistryValueKind.DWord);
                RunCmdCommand("sc.exe", "stop WSearch");
            }
            else
            {
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WSearch", "Start", 2, RegistryValueKind.DWord);
                RunCmdCommand("sc.exe", "start WSearch");
            }
            ReportProgress();

            // personalization

            if (!string.IsNullOrEmpty(preset.Per_RenameTemplate))
            {
                RunCmdCommand("reg.exe", "add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\NamingTemplates /v RenameNameTemplate /t REG_SZ /d \"" + preset.Per_RenameTemplate + "\" /f");
            }
            else
            {
                RunCmdCommand("reg.exe", "delete HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\NamingTemplates /v RenameNameTemplate /f");
            }
            ReportProgress();

            string highlightValue = "";
            string hotTrackingColorValue = "";
            switch (preset.Per_ColorIndex)
            {
                case 0: highlightValue = "51 153 255"; hotTrackingColorValue = "0 102 204"; break;
                case 1: highlightValue = "0 100 100"; hotTrackingColorValue = "0 100 100"; break;
                case 2: highlightValue = "180 0 180"; hotTrackingColorValue = "110 0 110"; break;
                case 3: highlightValue = "0 90 30"; hotTrackingColorValue = "0 90 30"; break;
                case 4: highlightValue = "100 40 0"; hotTrackingColorValue = "100 40 0"; break;
                case 5: highlightValue = "135 0 0"; hotTrackingColorValue = "135 0 0"; break;
                case 6: highlightValue = "15 0 120"; hotTrackingColorValue = "15 0 120"; break;
                case 7: highlightValue = "40 40 40"; hotTrackingColorValue = "40 40 40"; break;
                default: highlightValue = "51 153 255"; hotTrackingColorValue = "0 102 204"; break;
            }

            var colorKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors", true);
            if (colorKey != null)
            {
                colorKey.SetValue("HightLight", highlightValue, RegistryValueKind.String);
                colorKey.SetValue("Hilight", highlightValue, RegistryValueKind.String);
                colorKey.SetValue("HotTrackingColor", hotTrackingColorValue, RegistryValueKind.String);
            }
            ReportProgress();

            if (preset.Per_SmallWindows)
            {
                RunCmdCommand("reg.exe", "add \"HKEY_CURRENT_USER\\Control Panel\\Desktop\\WindowMetrics\" /v CaptionHeight /t REG_SZ /d -270 /f");
                RunCmdCommand("reg.exe", "add \"HKEY_CURRENT_USER\\Control Panel\\Desktop\\WindowMetrics\" /v CaptionWidth /t REG_SZ /d -270 /f");
            }
            else
            {
                RunCmdCommand("reg.exe", "add \"HKEY_CURRENT_USER\\Control Panel\\Desktop\\WindowMetrics\" /v CaptionHeight /t REG_SZ /d -330 /f");
                RunCmdCommand("reg.exe", "add \"HKEY_CURRENT_USER\\Control Panel\\Desktop\\WindowMetrics\" /v CaptionWidth /t REG_SZ /d -330 /f");
            }
            ReportProgress();

            if (preset.Per_Blur) Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System").SetValue("DisableAcrylicBackgroundOnLogon", 1);
            else Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System").SetValue("DisableAcrylicBackgroundOnLogon", 0);
            ReportProgress();

            if (preset.Per_Transparency) Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").SetValue("EnableTransparency", 0);
            else Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").SetValue("EnableTransparency", 1);
            ReportProgress();

            if (preset.Per_DarkTheme)
            {
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").SetValue("AppsUseLightTheme", 0);
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").SetValue("SystemUsesLightTheme", 0);
            }
            else
            {
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").SetValue("AppsUseLightTheme", 1);
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").SetValue("SystemUsesLightTheme", 1);
            }
            ReportProgress();

            if (preset.Per_Verbose) Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("verbosestatus", 1);
            else Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("verbosestatus", 0);
            ReportProgress();

            if (preset.Per_EndTask) Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings").SetValue("TaskbarEndTask", 1);
            else Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings").SetValue("TaskbarEndTask", 0);
            ReportProgress();

            if (preset.Per_ClassicMenu)
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"))
                {
                    key?.SetValue("", "");
                }
            }
            else
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}", false);
            }
            ReportProgress();

            if (preset.Per_MenuDelay) Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop").SetValue("MenuShowDelay", "50");
            else Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop").SetValue("MenuShowDelay", "400");
            ReportProgress();

            if (preset.Per_DisableLogo)
                RunCmdCommand("bcdedit.exe", "/set \"{globalsettings}\" custom:16000067 true");
            else
                RunCmdCommand("bcdedit.exe", "/deletevalue \"{globalsettings}\" custom:16000067");
            ReportProgress();

            if (preset.Per_DisableAnim)
                RunCmdCommand("bcdedit.exe", "/set \"{globalsettings}\" custom:16000069 true");
            else
                RunCmdCommand("bcdedit.exe", "/deletevalue \"{globalsettings}\" custom:16000069");
            ReportProgress();

            System.Threading.Thread.Sleep(300);
        }

        private bool IsBcdValueEnabled(string output, params string[] keys)
        {
            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string line = lines.FirstOrDefault(l => keys.Any(k => l.Contains(k)));

            if (string.IsNullOrEmpty(line)) return false;

            string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                string val = parts.Last().ToLower().Trim();
                if (val.StartsWith("n") || val.StartsWith("н") || val == "0" || val == "false")
                    return false;
                return true;
            }
            return false;
        }
        private void OpenLangOverlay_Click(object sender, RoutedEventArgs e)
        {
            var order = new Dictionary<string, int>
            {
                {"en", 1}, {"ru", 2}, {"uk", 3},
                {"be", 4}, {"kk", 5}, {"cs", 6}, 
                {"de", 7}, {"fr", 8}, {"es", 9},
                {"pl", 10}, {"it", 11}, {"pt", 12},
                {"fi", 13}, {"et", 14}, {"lv", 15},
                {"az", 16}, {"tr", 17}, {"hi", 18},
                {"id", 19}, {"vi", 20}, {"th", 21},
                {"ja", 22}, {"ko", 23}, {"tl", 24},
                {"zh", 25}, {"tw", 26}
            };

            FullLangList.ItemsSource = lang.Items.Cast<ComboBoxItem>()
                .Select(x => new
                {
                    Content = x.Content,
                    Tag = x.Tag?.ToString() ?? "",
                    Priority = order.ContainsKey(x.Tag?.ToString() ?? "") ? order[x.Tag.ToString()] : 99
                })
                .OrderBy(x => x.Priority)
                .ToList();

            LanguageOverlay.Visibility = Visibility.Visible;
        }

        private void FullLangItem_Click(object sender, RoutedEventArgs e)
        {
            LanguageOverlay.Visibility = Visibility.Collapsed;
            var btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                string selectedTag = btn.Tag.ToString();
                foreach (ComboBoxItem item in lang.Items)
                {
                    if (item.Tag?.ToString() == selectedTag)
                    {
                        lang.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void CloseLangOverlay_Click(object sender, RoutedEventArgs e)
        {
            LanguageOverlay.Visibility = Visibility.Collapsed;
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private string GetBuildNumberFromResources()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = "MakuTweakerNew.BuildNumber.txt";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return "Unknown";
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd().Trim();
                    }
                }
            }
            catch
            {
                return "N/A";
            }
        }
    }
}