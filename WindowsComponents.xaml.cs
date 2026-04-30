using MakuTweakerNew.Properties;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace MakuTweakerNew
{
    public partial class WindowsComponents : Page
    {
        bool isLoaded = false;
        MainWindow mw = (MainWindow)Application.Current.MainWindow;
        public WindowsComponents()
        {
            InitializeComponent();
            if (checkWinEd() == "Core" || checkWinEd() == "CoreSingleLanguage" || checkWinEd() == "CoreCountrySpecific" || checkWinEd() == "CoreN")
            {
                gpedit.Visibility = Visibility.Visible;
                lgp.Visibility = Visibility.Visible;
            }
            checkReg();
            LoadLang(Properties.Settings.Default.lang);
            isLoaded = true;
        }

        private void checkReg()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR"))
            {
                dvr.IsEnabled = (key?.GetValue("AllowGameDVR")?.Equals(0) != true);
            }

            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell"))
            {
                pwsh.IsEnabled = (key?.GetValue("ExecutionPolicy")?.ToString() ?? "") != "RemoteSigned";
            }

            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Photo Viewer\Capabilities\FileAssociations"))
            {
                pv.IsEnabled = (key?.GetValue(".jpg")?.ToString() != "PhotoViewer.FileAssoc.Tiff");
            }

            try
            {
                string bcdOutput = GetCmdOutput("bcdedit", "");
                hypervdis.IsEnabled = !bcdOutput.Contains("hypervisorlaunchtype    Off");
            }
            catch
            {
                hypervdis.IsEnabled = true;
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
                    return output;
                }
            }
            catch { return ""; }
        }

        private string checkWinEd()
        {
            string keyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            string valueName = "EditionID";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
            {
                object value = key.GetValue(valueName);
                return value.ToString();
            }
        }

        private void SetBtn(Button btn, string normalText, string appliedText)
        {
            btn.Content = btn.IsEnabled ? normalText : appliedText;
        }

        private void LoadLang(string lang)
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var basel = MainWindow.Localization.LoadLocalization(languageCode, "base");
            var compon = MainWindow.Localization.LoadLocalization(languageCode, "compon");
            var tooltips = MainWindow.Localization.LoadLocalization(languageCode, "tooltips");
            string applied = basel["def"]["applied"];

            label.Text = compon["main"]["label"];
            directplay.Text = compon["main"]["directplay"];
            framework.Text = compon["main"]["framework"];
            photoviewer.Text = compon["main"]["photoviewer"];
            powershellscr.Text = compon["main"]["powershellscr"];
            xboxdvr.Text = compon["main"]["xboxdvr"];
            forcedis.Text = compon["main"]["forcedis"];
            winsxs.Text = compon["main"]["winsxs"];
            gpedit.Text = compon["main"]["gpedit"];

            SetBtn(dp, compon["main"]["install"], applied);
            SetBtn(dnet, compon["main"]["install"], applied);
            SetBtn(sxs, compon["main"]["reset"], applied);
            SetBtn(lgp, compon["main"]["enable"], applied);
            SetBtn(pv, compon["main"]["enable"], applied);
            SetBtn(pwsh, compon["main"]["enable"], applied);
            SetBtn(dvr, basel["def"]["apply"], applied);
            SetBtn(hypervdis, basel["def"]["apply"], applied);

            sys_tooltip_photo.Content = tooltips["main"]["photow"];
            sys_tooltip_powershell.Content = tooltips["main"]["powershell"];
            sys_tooltip_xbox.Content = tooltips["main"]["xbox"];
            sys_tooltip_hyperv.Content = tooltips["main"]["hyperv"];
            sys_tooltip_directplay.Content = tooltips["main"]["directplay"];
        }
        private void pwsh_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("powershell", "-Command Set-ExecutionPolicy RemoteSigned -Force") { CreateNoWindow = true, UseShellExecute = false });
            mw.RebootNotify(1);
            MarkApplied(pwsh);
        }
        private void dp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("cmd.exe", "/C dism /online /Enable-Feature /FeatureName:DirectPlay /All");
            MarkApplied(dp);
        }

        private void dnet_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("powershell.exe", "/C Add-WindowsCapability -Online -Name NetFx3~~~~"));
            MarkApplied(dnet);
        }
        private void sxs_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("cmd.exe", "/C dism /Online /Cleanup-Image /StartComponentCleanup /ResetBase");
            mw.RebootNotify(3);
            MarkApplied(sxs);
        }
        private void lgp_Click(object sender, RoutedEventArgs e)
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var sr = MainWindow.Localization.LoadLocalization(languageCode, "sr");
            string batContent = @"
            pushd ""%~dp0""

            dir /b %SystemRoot%\servicing\Packages\Microsoft-Windows-GroupPolicy-ClientExtensions-Package~3*.mum >List.txt 
            dir /b %SystemRoot%\servicing\Packages\Microsoft-Windows-GroupPolicy-ClientTools-Package~3*.mum >>List.txt 

            for /f %%i in ('findstr /i . List.txt 2^>nul') do dism /online /norestart /add-package:""%SystemRoot%\servicing\Packages\%%i""";
            string tempBatFilePath = Path.Combine(Path.GetTempPath(), "script.bat");
            File.WriteAllText(tempBatFilePath, batContent);
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c \"" + tempBatFilePath + "\"";
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = false;
            try
            {
                process.Start();

            }
            catch
            {

            }
            MarkApplied(lgp);
        }
        private void pv_Click(object sender, RoutedEventArgs e)
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            try
            {
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"Applications\photoviewer.dll\shell\open"))
                {
                    key.SetValue("MuiVerb", "@photoviewer.dll,-3043");
                }

                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"Applications\photoviewer.dll\shell\open\command"))
                {
                    key.SetValue("", @"%SystemRoot%\System32\rundll32.exe ""%ProgramFiles%\Windows Photo Viewer\PhotoViewer.dll"", ImageViewer_Fullscreen %1", RegistryValueKind.String);
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows Photo Viewer\Capabilities\FileAssociations"))
                {
                    key.SetValue(".bmp", "PhotoViewer.FileAssoc.Tiff");
                    key.SetValue(".gif", "PhotoViewer.FileAssoc.Tiff");
                    key.SetValue(".jpeg", "PhotoViewer.FileAssoc.Tiff");
                    key.SetValue(".jpg", "PhotoViewer.FileAssoc.Tiff");
                    key.SetValue(".png", "PhotoViewer.FileAssoc.Tiff");
                }
                MarkApplied(pv);
            }
            catch (Exception ex)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show($"{ex.Message}");
            }
        }

        private void dvr_Click(object sender, RoutedEventArgs e)
        {
            Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore").SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);
            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR").SetValue("AllowGameDVR", 0, RegistryValueKind.DWord);
            MarkApplied(dvr);
        }

        private void hypervdis_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("cmd.exe", "/c \"bcdedit /set hypervisorlaunchtype off\"");
            MarkApplied(hypervdis);
        }

        private void MarkApplied(Button btn)
        {
            btn.IsEnabled = false;
            var basel = MainWindow.Localization.LoadLocalization(Properties.Settings.Default.lang, "base");
            btn.Content = basel["def"]["applied"];
        }
    }
}
