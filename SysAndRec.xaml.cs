using MakuTweakerNew.Properties;
using MicaWPF.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Mail;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Windows.UI.Composition.Desktop;
using static System.Net.Mime.MediaTypeNames;

namespace MakuTweakerNew
{
    public partial class SysAndRec : Page
    {
        bool isLoaded = false;
        MainWindow mw = (MainWindow)System.Windows.Application.Current.MainWindow;
        public SysAndRec()
        {
            InitializeComponent();
            checkReg();
            LoadLang();
            isLoaded = true;

            if (!HasBattery())
            {
                batterylabel.Visibility = Visibility.Collapsed;
                report.Visibility = Visibility.Collapsed;
            }
        }

        private bool HasBattery()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery"))
                using (var results = searcher.Get())
                {
                    return results.Count > 0;
                }
            }
            catch
            {
                return false;
            }
        }
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
            catch
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("CMD Error", "MakuTweaker", MessageBoxButton.OK, MessageBoxImage.Error);
                return "";
            }
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

            using (Process p = new Process())
            {
                p.StartInfo = psi;
                p.Start();
            }
        }

        private bool IsPowerSettingZero(string output)
        {
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length >= 2)
            {
                return lines[lines.Length - 1].Contains("0x00000000") &&
                       lines[lines.Length - 2].Contains("0x00000000");
            }
            return false;
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

        private void sfc_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("cmd.exe", "/k sfc /scannow");
            mw.RebootNotify(3);
            MarkApplied(sfc);
        }

        private void dism_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("cmd.exe", "/k DISM /Online /Cleanup-Image /RestoreHealth");
            mw.RebootNotify(3);
            MarkApplied(dism);
        }

        private void temp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("cmd.exe", "/c del /q /f %temp%");
            MarkApplied(temp);
        }

        private void MarkApplied(MicaWPF.Controls.Button btn)
        {
            btn.IsEnabled = false;
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var basel = MainWindow.Localization.LoadLocalization(languageCode, "base");
            btn.Content = basel["def"]["applied"];
        }

        private void SetBtn(MicaWPF.Controls.Button btn, string normalText, string appliedText)
        {
            btn.Content = btn.IsEnabled ? normalText : appliedText;
        }

        private void LoadLang()
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var sr = MainWindow.Localization.LoadLocalization(languageCode, "sr");
            var main = MainWindow.Localization.LoadLocalization(languageCode, "base");
            var compon = MainWindow.Localization.LoadLocalization(languageCode, "compon");
            var tooltips = MainWindow.Localization.LoadLocalization(languageCode, "tooltips");
            string applied = main["def"]["applied"];

            label.Text = sr["main"]["label"];
            sfclabel.Text = sr["main"]["sfclabel"];
            dismlabel.Text = sr["main"]["dismlabel"];
            templabel.Text = sr["main"]["templabel"];
            batterylabel.Text = sr["main"]["batterylabel"];

            SetBtn(sfc, sr["main"]["b2"], applied);
            SetBtn(dism, sr["main"]["b2"], applied);
            SetBtn(temp, sr["main"]["b4"], applied);
            SetBtn(report, sr["main"]["reportbutton"], applied);

            bitlocker.Header = sr["main"]["bitlocker"];
            chkdsk.Header = sr["main"]["chkdsk"];
            coreisol.Header = sr["main"]["coreisol"];
            hybern.Header = sr["main"]["hybern"];
            sleeptimeout.Header = sr["main"]["sleeptimeout"];
            smartscreen.Header = sr["main"]["smartscreen"];
            uac.Header = sr["main"]["uac"];
            sticky.Header = sr["main"]["sticky"];
            bing.Header = sr["main"]["bing"];
            telemetry.Header = sr["main"]["telemetry"];

            sys_tooltip_sfc.Content = tooltips["main"]["sfc"];
            sys_tooltip_dism.Content = tooltips["main"]["dism"];
            sys_tooltip_sticky.Content = tooltips["main"]["sticky"];
            sys_tooltip_coreisol.Content = tooltips["main"]["coreisol"];
            sys_tooltip_uac.Content = tooltips["main"]["duac"];
            sys_tooltip_smartscreen.Content = tooltips["main"]["smartscr"];
            sys_tooltip_hyber.Content = tooltips["main"]["hybern"];
            sys_tooltip_chkdsk.Content = tooltips["main"]["chkdsk"];
            sys_tooltip_bitlocker.Content = tooltips["main"]["bitlocker"];
            sys_tooltip_bing.Content = tooltips["main"]["bing"];

            foreach (var toggle in AllToggles)
            {
                toggle.OnContent = main["def"]["on"];
                toggle.OffContent = main["def"]["off"];
            }
        }

        private List<ModernWpf.Controls.ToggleSwitch> AllToggles => new()
        {
            bitlocker,
            chkdsk,
            coreisol,
            hybern,
            sleeptimeout,
            smartscreen,
            uac,
            sticky,
            bing,
            telemetry
        };
        private void checkReg()
        {
            try
            {
                bitlocker.IsOn = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\BitLocker")?.GetValue("PreventDeviceEncryption")?.Equals(1) ?? false;
                chkdsk.IsOn = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager")?.GetValue("AutoChkTimeout")?.Equals(60) ?? false;
                coreisol.IsOn = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios")?.GetValue("HypervisorEnforcedCodeIntegrity")?.Equals(0) ?? false;
                hybern.IsOn = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power")?.GetValue("HibernateEnabled")?.Equals(0) ?? false;
                telemetry.IsOn = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Policies\DataCollection")?.GetValue("AllowTelemetry")?.Equals(0) ?? false;
                smartscreen.IsOn = (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System")?.GetValue("EnableSmartScreen")?.Equals(0) ?? false) ||
              (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer")?.GetValue("SmartScreenEnabled")?.Equals("Off") ?? false);
                uac.IsOn = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System")?.GetValue("EnableLUA")?.Equals(0) ?? false;

                sticky.IsOn = (Registry.CurrentUser.OpenSubKey(@"Control Panel\Accessibility\StickyKeys")?.GetValue("Flags")?.Equals("506") ?? false)
                          || (Registry.CurrentUser.OpenSubKey(@"Control Panel\Accessibility\ToggleKeys")?.GetValue("Flags")?.Equals("58") ?? false)
                          || (Registry.CurrentUser.OpenSubKey(@"Control Panel\Accessibility\Keyboard Response")?.GetValue("Flags")?.Equals("122") ?? false);

                bing.IsOn = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\Explorer")?.GetValue("DisableSearchBoxSuggestions")?.Equals(1) ?? false;

                string powerVideo = GetCmdOutput("powercfg", "/q SCHEME_CURRENT SUB_VIDEO VIDEOIDLE");
                string powerSleep = GetCmdOutput("powercfg", "/q SCHEME_CURRENT SUB_SLEEP STANDBYIDLE");
                sleeptimeout.IsOn = IsPowerSettingZero(powerVideo) && IsPowerSettingZero(powerSleep);
            }
            catch (Exception ex)
            {
            }
        }

        private void report_Click(object sender, RoutedEventArgs e)
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var sr = MainWindow.Localization.LoadLocalization(languageCode, "sr");
            Microsoft.Win32.SaveFileDialog saveFileDialog1 = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog1.Filter = "HTML (*.html)|*.html";
            saveFileDialog1.Title = "Microsoft Battery Report";
            saveFileDialog1.FileName = "battery-report.html";
            if (saveFileDialog1.ShowDialog() == true)
            {
                string reportPath = saveFileDialog1.FileName;
                Process.Start("cmd.exe", $"/c powercfg /batteryreport /output \"{reportPath}\"");
                MarkApplied(report);
            }
        }

        private void sticky_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (sticky.IsOn)
                {
                    case true:
                        Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\StickyKeys").SetValue("Flags", "506");
                        Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\Keyboard Response").SetValue("Flags", "122");
                        Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\ToggleKeys").SetValue("Flags", "58");
                        break;
                    case false:
                        Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\StickyKeys").SetValue("Flags", "510");
                        Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\Keyboard Response").SetValue("Flags", "126");
                        Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\ToggleKeys").SetValue("Flags", "62");
                        break;
                }
                mw.RebootNotify(1);
            }
        }

        private void coreisol_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (coreisol.IsOn)
                {
                    case true:
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios").SetValue("HypervisorEnforcedCodeIntegrity", 0);
                        break;
                    case false:
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios").SetValue("HypervisorEnforcedCodeIntegrity", 1);
                        break;
                }
                mw.RebootNotify(1);
            }
        }

        private void uac_Toggled(object sender, RoutedEventArgs e)
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var sr = MainWindow.Localization.LoadLocalization(languageCode, "sr");


            if (isLoaded)
            {
                if (checkWinVer() >= 22621 && uac.IsOn)
                {
                    var res = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(sr["status"]["uacwarn"],"MakuTweaker", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (res == MessageBoxResult.No)
                    {
                        uac.IsOn = false;
                        return;
                    }
                }
                switch (uac.IsOn)
                {
                    case true:
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("EnableLUA", 0);
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments")?.SetValue("SaveZoneInformation", 1, RegistryValueKind.DWord);
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Associations")?.SetValue("LowRiskFileTypes", ".exe;.msi;.bat;", RegistryValueKind.String);
                        break;
                    case false:
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("EnableLUA", 1);
                        break;
                }
            }
        }

        private void smartscreen_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (smartscreen.IsOn)
                {
                    case true:
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("EnableSmartScreen", 0);
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer").SetValue("SmartScreenEnabled", "Off", RegistryValueKind.String);
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments").SetValue("SaveZoneInformation", 1, RegistryValueKind.DWord);
                        break;
                    case false:
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("EnableSmartScreen", 1);
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer").SetValue("SmartScreenEnabled", "Warn", RegistryValueKind.String);
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments").SetValue("SaveZoneInformation", 0, RegistryValueKind.DWord);
                        break;
                }
            }
        }

        private void hybern_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (hybern.IsOn)
                {
                    case true:
                        Process.Start("cmd.exe", "/C powercfg /h off");
                        break;
                    case false:
                        Process.Start("cmd.exe", "/C powercfg /h on");
                        break;
                }
                mw.RebootNotify(1);
            }
        }

        private void sleeptimeout_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (sleeptimeout.IsOn)
                {
                    case true:
                        RunCmdCommand("powercfg", "-change -monitor-timeout-ac 0");
                        RunCmdCommand("powercfg", "-change -monitor-timeout-dc 0");
                        RunCmdCommand("powercfg", "-change -standby-timeout-ac 0");
                        RunCmdCommand("powercfg", "-change -standby-timeout-dc 0");
                        break;
                    case false:
                        RunCmdCommand("powercfg", "-change -monitor-timeout-ac 10");
                        RunCmdCommand("powercfg", "-change -monitor-timeout-dc 5");
                        RunCmdCommand("powercfg", "-change -standby-timeout-ac 30");
                        RunCmdCommand("powercfg", "-change -standby-timeout-dc 15");
                        break;
                }
            }
        }

        private void bing_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (bing.IsOn)
                {
                    case true:
                            Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer").SetValue("DisableSearchBoxSuggestions", 1);
                        break;
                    case false:
                            Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer").SetValue("DisableSearchBoxSuggestions", 0);
                        break;
                }
            }
        }

        private void chkdsk_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (chkdsk.IsOn)
                {
                    case true:
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager").SetValue("AutoChkTimeout", 60);
                        break;
                    case false:
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager").SetValue("AutoChkTimeout", 8);
                        break;
                }
            }
        }

        private void bitlocker_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (bitlocker.IsOn)
                {
                    case true:
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\BitLocker").SetValue("PreventDeviceEncryption", 1, RegistryValueKind.DWord);
                        break;
                    case false:
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\BitLocker").SetValue("PreventDeviceEncryption", 0, RegistryValueKind.DWord);
                        break;
                }
            }
        }

        private void telemetry_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (telemetry.IsOn)
                {
                    case true:
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
                        break;
                    case false:
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
                        break;
                }
            }
        }
    }
}
