using MakuTweakerNew.Properties;
using Microsoft.Win32;
using NvAPIWrapper.Native.GPU;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.UI.Composition.Desktop;
using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;

namespace MakuTweakerNew
{
    public partial class Personalization : Page
    {
        bool isLoaded = false;
        MainWindow mw = (MainWindow)Application.Current.MainWindow;
        public Personalization()
        {
            InitializeComponent();
            checkReg();
            LoadLang();
            if (checkWinVer() < 22000)
            {
                endtask.Visibility = Visibility.Collapsed;
                oldcont.Visibility = Visibility.Collapsed;
            }
            isLoaded = true;
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

        private void apN_Click(object sender, RoutedEventArgs e)
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var per = MainWindow.Localization.LoadLocalization(languageCode, "per");
            string folderName = newname.Text;
            RunCmdCommand("reg.exe", "add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\NamingTemplates /v RenameNameTemplate /t REG_SZ /d \"" + folderName + "\" /f");
            mw.ChSt(per["status"]["apN"]);
        }

        private void stN_Click(object sender, RoutedEventArgs e)
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var per = MainWindow.Localization.LoadLocalization(languageCode, "per");
            newname.Text = string.Empty;
            RunCmdCommand("reg.exe", "delete HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\NamingTemplates /v RenameNameTemplate /f");
            mw.ChSt(per["status"]["stN"]);
        }

        private void apC_Click(object sender, RoutedEventArgs e)
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var per = MainWindow.Localization.LoadLocalization(languageCode, "per");
            string regPath = @"Control Panel\Colors";

            string highlightValue = "";
            string hotTrackingColorValue = "";

            switch (color.SelectedIndex)
            {
                case 0:
                    highlightValue = "51 153 255";
                    hotTrackingColorValue = "0 102 204";
                    break;
                case 1:
                    highlightValue = "0 100 100";
                    hotTrackingColorValue = "0 100 100";
                    break;
                case 2:
                    highlightValue = "180 0 180";
                    hotTrackingColorValue = "110 0 110";
                    break;
                case 3:
                    highlightValue = "0 90 30";
                    hotTrackingColorValue = "0 90 30";
                    break;
                case 4:
                    highlightValue = "100 40 0";
                    hotTrackingColorValue = "100 40 0";
                    break;
                case 5:
                    highlightValue = "135 0 0";
                    hotTrackingColorValue = "135 0 0";
                    break;
                case 6:
                    highlightValue = "15 0 120";
                    hotTrackingColorValue = "15 0 120";
                    break;
                case 7:
                    highlightValue = "40 40 40";
                    hotTrackingColorValue = "40 40 40";
                    break;
                default:
                    highlightValue = "51 153 255";
                    hotTrackingColorValue = "0 102 204";
                    return;
            }

            RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath, true);

            if (key != null)
            {
                key.SetValue("HightLight", highlightValue, RegistryValueKind.String);
                key.SetValue("Hilight", highlightValue, RegistryValueKind.String);
                key.SetValue("HotTrackingColor", hotTrackingColorValue, RegistryValueKind.String);
            }
            else
            {

            }
            mw.ChSt(per["status"]["apC"]);
            mw.RebootNotify(1);
        }

        private void LoadLang()
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var per = MainWindow.Localization.LoadLocalization(languageCode, "per");
            var basel = MainWindow.Localization.LoadLocalization(languageCode, "base");
            var tooltips = MainWindow.Localization.LoadLocalization(languageCode, "tooltips");

            label.Text = per["main"]["label"];
            defaultnamelabel.Text = per["main"]["defaultnamelabel"];
            colorlabel.Text = per["main"]["colorlabel"];
            newname.Watermark = per["main"]["newname"];
            apN.Content = basel["def"]["apply"];
            apC.Content = basel["def"]["apply"];
            stN.Content = per["main"]["b2"];

            c1.Content = per["main"]["c1"];
            c2.Content = per["main"]["c2"];
            c3.Content = per["main"]["c3"];
            c4.Content = per["main"]["c4"];
            c5.Content = per["main"]["c5"];
            c6.Content = per["main"]["c6"];
            c7.Content = per["main"]["c7"];
            c8.Content = per["main"]["c8"];

            smallwindows.Header = per["main"]["smallwindows"];
            blur.Header = per["main"]["blur"];
            transparency.Header = per["main"]["transparency"];
            darktheme.Header = per["main"]["darktheme"];
            verbose.Header = per["main"]["verbose"];
            endtask.Header = per["main"]["etask"];
            disablelogo.Header = per["main"]["disablelogo"];
            disableanim.Header = per["main"]["disableanim"];
            oldcont.Header = per["main"]["oldcont"];
            contdel.Header = per["main"]["delcont"];

            smallwindows.OffContent = basel["def"]["off"];
            blur.OffContent = basel["def"]["off"];
            transparency.OffContent = basel["def"]["off"];
            darktheme.OffContent = basel["def"]["off"];
            disablelogo.OffContent = basel["def"]["off"];
            disableanim.OffContent = basel["def"]["off"];
            verbose.OffContent = basel["def"]["off"];
            endtask.OffContent = basel["def"]["off"];
            oldcont.OffContent = basel["def"]["off"];
            contdel.OffContent = basel["def"]["off"];

            smallwindows.OnContent = basel["def"]["on"];
            blur.OnContent = basel["def"]["on"];
            transparency.OnContent = basel["def"]["on"];
            darktheme.OnContent = basel["def"]["on"];
            disablelogo.OnContent = basel["def"]["on"];
            disableanim.OnContent = basel["def"]["on"];
            verbose.OnContent = basel["def"]["on"];
            endtask.OnContent = basel["def"]["on"];
            oldcont.OnContent = basel["def"]["on"];
            contdel.OnContent = basel["def"]["on"];

            sys_tooltip_verbose.Content = tooltips["main"]["advanced"];
        }
        private void checkReg()
        {
            this.newname.Text = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates")?.GetValue("RenameNameTemplate")?.ToString();

            string captionHeight = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop\WindowMetrics")?.GetValue("CaptionHeight")?.ToString();
            smallwindows.IsOn = (captionHeight == "-270");
            blur.IsOn = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System")?.GetValue("DisableAcrylicBackgroundOnLogon")?.Equals(1) == true;
            transparency.IsOn = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")?.GetValue("EnableTransparency")?.Equals(0) == true;
            darktheme.IsOn = (Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")?.GetValue("AppsUseLightTheme") is int a && a == 0)&& (Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")?.GetValue("SystemUsesLightTheme") is int b && b == 0);
            verbose.IsOn = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System")?.GetValue("verbosestatus")?.Equals(1) ?? false;
            endtask.IsOn = (Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings")?.GetValue("TaskbarEndTask") is int v && v == 1);

            contdel.IsOn = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop")?.GetValue("MenuShowDelay")?.Equals("50") ?? false;
            oldcont.IsOn = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32")?.GetValue("")?.Equals("") ?? false;

            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = "bcdedit";
                    p.StartInfo.Arguments = "/enum {globalsettings}";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();

                    string output = p.StandardOutput.ReadToEnd().ToLower();
                    p.WaitForExit();
                    disablelogo.IsOn = IsBcdValueEnabled(output, "custom:16000067", "bootlogo", "nobootlogo");
                    disableanim.IsOn = IsBcdValueEnabled(output, "custom:16000069", "nobootuxprogress");
                }
            }
            catch { }

            var highlightColor = Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors")?.GetValue("HightLight")?.ToString();
            switch (highlightColor)
            {
                case "51 153 255": color.SelectedIndex = 0; break;
                case "0 100 100": color.SelectedIndex = 1; break;
                case "180 0 180": color.SelectedIndex = 2; break;
                case "0 90 30": color.SelectedIndex = 3; break;
                case "100 40 0": color.SelectedIndex = 4; break;
                case "135 0 0": color.SelectedIndex = 5; break;
                case "15 0 120": color.SelectedIndex = 6; break;
                case "40 40 40": color.SelectedIndex = 7; break;
                default: color.SelectedIndex = 0; break;
            }
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

        private void endtask_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (endtask.IsOn)
                {
                    case true:
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings").SetValue("TaskbarEndTask", 1);
                        break;
                    case false:
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings").SetValue("TaskbarEndTask", 0);
                        break;
                }
            }
        }

        private void smallwindows_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (smallwindows.IsOn)
                {
                    case true:
                        RunCmdCommand("reg.exe", "add \"HKEY_CURRENT_USER\\Control Panel\\Desktop\\WindowMetrics\" /v CaptionHeight /t REG_SZ /d -270 /f");
                        RunCmdCommand("reg.exe", "add \"HKEY_CURRENT_USER\\Control Panel\\Desktop\\WindowMetrics\" /v CaptionWidth /t REG_SZ /d -270 /f");
                        break;
                    case false:
                        RunCmdCommand("reg.exe", "add \"HKEY_CURRENT_USER\\Control Panel\\Desktop\\WindowMetrics\" /v CaptionHeight /t REG_SZ /d -330 /f");
                        RunCmdCommand("reg.exe", "add \"HKEY_CURRENT_USER\\Control Panel\\Desktop\\WindowMetrics\" /v CaptionWidth /t REG_SZ /d -330 /f");
                        break;
                }
                mw.RebootNotify(1);
            }
        }

        private void blur_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (blur.IsOn)
                {
                    case true:
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System").SetValue("DisableAcrylicBackgroundOnLogon", 1);
                        break;
                    case false:
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System").SetValue("DisableAcrylicBackgroundOnLogon", 0);
                        break;
                }
            }
        }

        private void transparency_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (transparency.IsOn)
                {
                    case true:
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").SetValue("EnableTransparency", 0);
                        break;
                    case false:
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").SetValue("EnableTransparency", 1);
                        break;
                }
            }
        }

        private void darktheme_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (darktheme.IsOn)
                {
                    case true:
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").SetValue("AppsUseLightTheme", 0);
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").SetValue("SystemUsesLightTheme", 0);
                        break;
                    case false:
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").SetValue("AppsUseLightTheme", 1);
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").SetValue("SystemUsesLightTheme", 1);
                        break;
                }
                mw.RebootNotify(2);
            }
        }

        private void verbose_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (verbose.IsOn)
                {
                    case true:
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("verbosestatus", 1);
                        break;
                    case false:
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("verbosestatus", 0);
                        break;
                }
            }
        }

        private void disablelogo_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (disablelogo.IsOn)
                {
                    case true:
                        RunCmdCommand("bcdedit", "/set \"{globalsettings}\" custom:16000067 true");
                        mw.RebootNotify(1);
                        break;
                    case false:
                        RunCmdCommand("bcdedit", "/deletevalue \"{globalsettings}\" custom:16000067");
                        mw.RebootNotify(1);
                        break;
                }
            }
        }

        private void disableanim_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (disableanim.IsOn)
                {
                    case true:
                        RunCmdCommand("bcdedit", "/set \"{globalsettings}\" custom:16000069 true");
                        mw.RebootNotify(1);
                        break;
                    case false:
                        RunCmdCommand("bcdedit", "/deletevalue \"{globalsettings}\" custom:16000069");
                        mw.RebootNotify(1);
                        break;
                }
            }
        }

        private void oldcont_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (oldcont.IsOn)
                {
                    case true:
                        Process.Start("cmd.exe", "/c \"reg.exe add \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32\" /f /ve\"");
                        Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel").SetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0);
                        break;
                    case false:
                        {
                            Process.Start("cmd.exe", "/c \"reg delete \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32\" /f\"");
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel").SetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0);
                            break;
                        }
                }
                mw.RebootNotify(2);
            }
        }

        private void contdel_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (contdel.IsOn)
                {
                    case true:
                        Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop").SetValue("MenuShowDelay", "50");
                        break;
                    case false:
                        Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop").SetValue("MenuShowDelay", "400");
                        break;
                }
                mw.RebootNotify(2);
            }
        }
    }
}
