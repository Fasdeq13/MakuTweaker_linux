using MakuTweakerNew.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.UI.Composition.Desktop;

namespace MakuTweakerNew
{
    public partial class WindowsUpdate : Page
    {
        bool isLoaded = false;
        bool isUpdatesBlocked = false;
        MainWindow mw = (MainWindow)Application.Current.MainWindow;
        public WindowsUpdate()
        {
            InitializeComponent();
            checkReg();
            if (wu4.SelectedIndex == -1)
            {
                int currentBuild = checkWinVer();
                if (currentBuild >= 26300) wu4.SelectedIndex = 11;
                else if (currentBuild >= 26200) wu4.SelectedIndex = 10;
                else if (currentBuild >= 26100) wu4.SelectedIndex = 9;
                else if (currentBuild >= 22631) wu4.SelectedIndex = 8;
                else if (currentBuild >= 22621 || currentBuild == 19045) wu4.SelectedIndex = 7;
                else if (currentBuild >= 22000 || currentBuild == 19044) wu4.SelectedIndex = 6;
                else if (currentBuild == 19042) wu4.SelectedIndex = 5;
                else if (currentBuild == 19041) wu4.SelectedIndex = 4;
                else if (currentBuild == 18363) wu4.SelectedIndex = 3;
                else if (currentBuild == 17763) wu4.SelectedIndex = 2;
                else if (currentBuild == 16299) wu4.SelectedIndex = 1;
                else wu4.SelectedIndex = 0;
            }
            var build = checkWinVer();

            var rules = new (Func<int, bool> Condition, UIElement Element)[]
            {
                (b => b > 14393, u1607),
                (b => b > 16299, u1709),
                (b => b > 17763, u1809),
                (b => b > 18363, u1909),
                (b => b > 19041, u2004),
                (b => b > 19042, u20H2),
                (b => (b > 19044 && b < 22000) || b > 22000, u21H2),
                (b => (b > 19045 && b < 22621) || b > 22621, u22H2),
                (b => b > 22631, u23H2),
                (b => b > 26100, u24H2),
                (b => b > 26200, u25H2),
                (b => b > 26300, u26H2)
            };

            foreach (var (condition, element) in rules)
            {
                if (condition(build))
                {
                    element.Visibility = Visibility.Collapsed;
                    element.IsEnabled = false;
                }
            }

            LoadLang(Properties.Settings.Default.lang);
            isLoaded = true;
        }

        private void RunCmdCommand(string fileName, string arguments)
        {
            using (Process p = new Process())
            {
                p.StartInfo.FileName = fileName;
                p.StartInfo.Arguments = arguments;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.Start();
                p.WaitForExit();
            }
        }

        private async void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                var languageCode = Properties.Settings.Default.lang ?? "en";
                var wu = MainWindow.Localization.LoadLocalization(languageCode, "wu");

                switch (wu1.IsOn)
                {
                    case true:
                        t.Text = wu["main"]["statusdis"];
                        if (mw != null) mw.NavigationView_Root.IsEnabled = false;
                        wu1.IsEnabled = false;
                        ring.IsActive = true;
                        FadeIn(loadingPanel, 300);
                        await Task.Delay(300);

                        await Task.Run(() =>
                        {
                            try
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
                                try
                                {
                                    RunCmdCommand("taskkill", "/f /im wuauclt.exe");
                                    RunCmdCommand("taskkill", "/f /im updatenotificationmgr.exe");
                                    RunCmdCommand("net", "stop wuauserv /y");
                                    RunCmdCommand("net", "stop bits /y");
                                    RunCmdCommand("net", "stop UsoSvc /y");
                                }
                                catch { }

                                RunCmdCommand("cmd.exe", "/c ipconfig /flushdns");
                            }
                            catch { }
                        });

                        FadeOut(loadingPanel, 300);
                        await Task.Delay(300);
                        ring.IsActive = false;
                        if (mw != null) mw.NavigationView_Root.IsEnabled = true;
                        wu1.IsEnabled = true;

                        mw.RebootNotify(1);
                        break;

                    case false:
                        t.Text = wu["main"]["statusdis"];

                        if (mw != null) mw.NavigationView_Root.IsEnabled = false;
                        wu1.IsEnabled = false;
                        ring.IsActive = true;
                        FadeIn(loadingPanel, 300);
                        await Task.Delay(300);

                        await Task.Run(() =>
                        {
                            try
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
                            catch { }
                        });

                        FadeOut(loadingPanel, 300);
                        await Task.Delay(300);
                        ring.IsActive = false;
                        if (mw != null) mw.NavigationView_Root.IsEnabled = true;
                        wu1.IsEnabled = true;

                        mw.RebootNotify(1);
                        break;
                }
            }
        }

        private void ToggleSwitch_Toggled_1(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (wu2.IsOn)
                {
                    case true:
                        try
                        {
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate").SetValue("ExcludeWUDriversInQualityUpdate", 1);
                        }
                        catch
                        {

                        }
                        break;
                    case false:
                        try
                        {
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate").SetValue("ExcludeWUDriversInQualityUpdate", 0);
                        }
                        catch
                        {

                        }
                        break;
                }
            }
        }

        private void wu4_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var languageCode = Settings.Default.lang ?? "en";
            var wul = MainWindow.Localization.LoadLocalization(languageCode, "wu");

            if (!isUpdatesBlocked)
            {
                string targetVersion = "unknown";
                switch (wu4.SelectedIndex)
                {
                    case 0: targetVersion = "1607"; break;
                    case 1: targetVersion = "1709"; break;
                    case 2: targetVersion = "1809"; break;
                    case 3: targetVersion = "1909"; break;
                    case 4: targetVersion = "2004"; break;
                    case 5: targetVersion = "20H2"; break;
                    case 6: targetVersion = "21H2"; break;
                    case 7: targetVersion = "22H2"; break;
                    case 8: targetVersion = "23H2"; break;
                    case 9: targetVersion = "24H2"; break;
                    case 10: targetVersion = "25H2"; break;
                    case 11: targetVersion = "26H2"; break;
                }

                try
                {
                    var wuKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate");
                    switch (wu4.SelectedIndex)
                    {
                        case 0: wuKey.SetValue("TargetReleaseVersionInfo", "1607"); break;
                        case 1: wuKey.SetValue("TargetReleaseVersionInfo", "1709"); break;
                        case 2: wuKey.SetValue("TargetReleaseVersionInfo", "1809"); break;
                        case 3: wuKey.SetValue("TargetReleaseVersionInfo", "1909"); break;
                        case 4: wuKey.SetValue("TargetReleaseVersionInfo", "2004"); break;
                        case 5: wuKey.SetValue("TargetReleaseVersionInfo", "20H2"); break;
                        case 6: wuKey.SetValue("TargetReleaseVersionInfo", "21H2"); break;
                        case 7: wuKey.SetValue("TargetReleaseVersionInfo", "22H2"); break;
                        case 8: wuKey.SetValue("TargetReleaseVersionInfo", "23H2"); break;
                        case 9: wuKey.SetValue("TargetReleaseVersionInfo", "24H2"); break;
                        case 10: wuKey.SetValue("TargetReleaseVersionInfo", "25H2"); break;
                        case 11: wuKey.SetValue("TargetReleaseVersionInfo", "26H2"); break;
                    }
                    string productVersion = checkWinVer() >= 22000 ? "Windows 11" : "Windows 10";
                    wuKey.SetValue("ProductVersion", productVersion);
                }
                catch { }

                isUpdatesBlocked = true;
                block.Content = wul["main"]["wu6u"];
            }
            else
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue("TargetReleaseVersionInfo", false);
                            key.DeleteValue("ProductVersion", false);
                        }
                    }
                }
                catch { }

                isUpdatesBlocked = false;
                block.Content = wul["main"]["wu6b"];
            }
            mw.RebootNotify(1);
        }

        private void pause_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunCmdCommand("cmd.exe", "/c \"reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v ActiveHoursStart /t REG_DWORD /d 9 /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v ActiveHoursEnd /t REG_DWORD /d 2 /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseFeatureUpdatesStartTime /t REG_SZ /d \"2015-01-01T00:00:00Z\" /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseQualityUpdatesStartTime /t REG_SZ /d \"2015-01-01T00:00:00Z\" /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseUpdatesExpiryTime /t REG_SZ /d \"2077-01-01T00:00:00Z\" /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseFeatureUpdatesEndTime /t REG_SZ /d \"2077-01-01T00:00:00Z\" /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseQualityUpdatesEndTime /t REG_SZ /d \"2077-01-01T00:00:00Z\" /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseUpdatesStartTime /t REG_SZ /d \"2015-01-01T00:00:00Z\" /f\"");
                var languageCode = Settings.Default.lang ?? "en";
                var wul = MainWindow.Localization.LoadLocalization(languageCode, "wu");
                MarkApplied(pause);
            }
            catch { }
        }

        private void wu6_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (wu6.IsOn)
                {
                    case true:
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ReserveManager").SetValue("ShippedWithReserves", 0);
                        break;
                    case false:
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ReserveManager").SetValue("ShippedWithReserves", 1);
                        break;
                }
            }
        }
        private void LoadLang(string lang)
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var wu = MainWindow.Localization.LoadLocalization(languageCode, "wu");
            var sr = MainWindow.Localization.LoadLocalization(languageCode, "sr");
            var main = MainWindow.Localization.LoadLocalization(languageCode, "base");
            string applied = main["def"]["applied"];

            wu1.Header = wu["main"]["wu1"];
            wu2.Header = wu["main"]["wu3"];
            wu6.Header = wu["main"]["wu6"];
            pausel.Text = wu["main"]["wu5"];
            blockL.Text = wu["main"]["wu2"];
            l7.Text = wu["main"]["wu4"];

            SetBtn(pause, wu["main"]["wu5b"], applied);
            block.Content = isUpdatesBlocked ? wu["main"]["wu6u"] : wu["main"]["wu6b"];
            SetBtn(wupd, sr["main"]["b4"], applied);

            foreach (var toggle in AllToggles)
            {
                toggle.OnContent = main["def"]["on"];
                toggle.OffContent = main["def"]["off"];
            }
        }

        private List<ModernWpf.Controls.ToggleSwitch> AllToggles => new()
        {
            wu1,
            wu2,
            wu6,
        };

        private void MarkApplied(Button btn)
        {
            btn.IsEnabled = false;
            var languageCode = Settings.Default.lang ?? "en";
            var basel = MainWindow.Localization.LoadLocalization(languageCode, "base");
            btn.Content = basel["def"]["applied"];
        }
        private void SetBtn(Button btn, string normalText, string appliedText)
        {
            btn.Content = btn.IsEnabled ? normalText : appliedText;
        }
        private void checkReg()
        {
            wu1.IsOn = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU")?.GetValue("NoAutoUpdate")?.Equals(1) ?? false;
            wu2.IsOn = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate")?.GetValue("ExcludeWUDriversInQualityUpdate")?.Equals(1) ?? false;
            wu6.IsOn = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ReserveManager")?.GetValue("ShippedWithReserves")?.Equals(0) ?? false;

            string targetVersion = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate")?.GetValue("TargetReleaseVersionInfo")?.ToString();
            isUpdatesBlocked = !string.IsNullOrEmpty(targetVersion);
            switch (targetVersion)
            {
                case "1607": wu4.SelectedIndex = 0; break;
                case "1709": wu4.SelectedIndex = 1; break;
                case "1809": wu4.SelectedIndex = 2; break;
                case "1909": wu4.SelectedIndex = 3; break;
                case "2004": wu4.SelectedIndex = 4; break;
                case "20H2": wu4.SelectedIndex = 5; break;
                case "21H2": wu4.SelectedIndex = 6; break;
                case "22H2": wu4.SelectedIndex = 7; break;
                case "23H2": wu4.SelectedIndex = 8; break;
                case "24H2": wu4.SelectedIndex = 9; break;
                case "25H2": wu4.SelectedIndex = 10; break;
                case "26H2": wu4.SelectedIndex = 11; break;
                default: wu4.SelectedIndex = -1; break;
            }

            string pauseTime = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings")?.GetValue("PauseUpdatesExpiryTime")?.ToString();
            pause.IsEnabled = string.IsNullOrEmpty(pauseTime) || !pauseTime.Contains("2077");
            wupd.IsEnabled = true;
        }
        private async void wupd_Click(object sender, RoutedEventArgs e)
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var wu = MainWindow.Localization.LoadLocalization(languageCode, "wu");
            t.Text = wu["main"]["status"];
            var mw = (MainWindow)Application.Current.MainWindow;
            if (mw != null) mw.NavigationView_Root.IsEnabled = false;
            wupd.IsEnabled = false;
            ring.IsActive = true;
            FadeIn(loadingPanel, 300);
            await Task.Delay(300);
            await Task.Run(() =>
            {
                RunCmdCommand("cmd.exe", "/c del /f /s /q %windir%\\SoftwareDistribution\\Download\\*");
            });

            FadeOut(loadingPanel, 300);
            await Task.Delay(300);
            ring.IsActive = false;

            if (mw != null) mw.NavigationView_Root.IsEnabled = true;
            MarkApplied(wupd);
        }

        private void FadeIn(UIElement element, double durationSeconds)
        {
            var fadeInAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(durationSeconds),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
            };
            element.BeginAnimation(OpacityProperty, fadeInAnimation);
        }

        private void FadeOut(UIElement element, double durationSeconds)
        {
            var fadeOutAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(durationSeconds),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            element.BeginAnimation(OpacityProperty, fadeOutAnimation);
        }
    }
}
