using System;
using System.Collections.Generic;
using System.Configuration;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MakuTweakerNew.Properties;
using Microsoft.Win32;
using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;

namespace MakuTweakerNew
{
    public partial class Explorer : Page
    {
        private MainWindow mw = (MainWindow)System.Windows.Application.Current.MainWindow;
        bool isLoaded = false;
        public Explorer()
        {
            InitializeComponent();
            checkReg();
            if (checkWinVer() >= 22621)
            {
                nonremovable.Visibility = Visibility.Collapsed;
            }
            else
            {
                nonremovable.Visibility = Visibility.Visible;
            }
            LoadLang(Settings.Default.lang);
            isLoaded = true;
        }

        private void fix_Click(object sender, RoutedEventArgs e)
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var expl = MainWindow.Localization.LoadLocalization(languageCode, "expl");
            try
            {
                Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\DelegateFolders\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}");
                Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\DelegateFolders\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}");
                FadeOut(fixlabel, 300);
                FadeOut(fix, 300);
                fixlabel.IsEnabled = false;
                fix.IsEnabled = false;
            }
            catch
            {
                FadeOut(fixlabel, 300);
                FadeOut(fix, 300);
                fixlabel.IsEnabled = false;
                fix.IsEnabled = false;
            }
        }
        private void FadeOut(UIElement element, double durationSeconds)
        {
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(durationSeconds),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            element.BeginAnimation(OpacityProperty, fadeOutAnimation);

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
        private void LoadLang(string lang)
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var expl = MainWindow.Localization.LoadLocalization(languageCode, "expl");
            var main = MainWindow.Localization.LoadLocalization(languageCode, "base");

            lab.Text = expl["main"]["label"];
            nonremovable.Header = expl["main"]["nonremovable"];
            hidden.Header = expl["main"]["hidden"];
            ext.Header = expl["main"]["ext"];
            pchome.Header = expl["main"]["pchome"];
            gallery.Header = expl["main"]["gallery"];
            showpc.Header = expl["main"]["showpc"];
            shortcut.Header = expl["main"]["shortcut"];
            fixlabel.Text = expl["main"]["fixlabel"];
            fix.Content = expl["main"]["e8b"];
            driveslabel.Text = expl["main"]["driveslabel"];
            hide.Content = expl["main"]["choose"];
            showall.Content = expl["main"]["showall"];

            foreach (var toggle in AllToggles)
            {
                toggle.OnContent = main["def"]["on"];
                toggle.OffContent = main["def"]["off"];
            }
        }

        private List<ModernWpf.Controls.ToggleSwitch> AllToggles => new()
        {
            nonremovable,
            hidden,
            ext,
            pchome,
            gallery,
            showpc,
            shortcut
        };
        private void checkReg()
        {
            nonremovable.IsOn =
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{A0953C92-50DC-43bf-BE83-3742FED03C9C}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{A0953C92-50DC-43bf-BE83-3742FED03C9C}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{A8CDFF1C-4878-43be-B5FD-F8091C1C60D0}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{d3162b92-9365-467a-956b-92703aca08af}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{A8CDFF1C-4878-43be-B5FD-F8091C1C60D0}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{d3162b92-9365-467a-956b-92703aca08af}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{374DE290-123F-4565-9164-39C4925E467B}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{088e3905-0323-4b02-9826-5d99428e115f}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{374DE290-123F-4565-9164-39C4925E467B}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{088e3905-0323-4b02-9826-5d99428e115f}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3ADD1653-EB32-4cb0-BBD7-DFA0ABB5ACCA}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{24ad3ad4-a569-4530-98e1-ab02f9417aa8}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3ADD1653-EB32-4cb0-BBD7-DFA0ABB5ACCA}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{24ad3ad4-a569-4530-98e1-ab02f9417aa8}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{1CF1260C-4DD0-4ebb-811F-33C572699FDE}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{1CF1260C-4DD0-4ebb-811F-33C572699FDE}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}") == null;

            hidden.IsOn = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true)?.GetValue("Hidden")?.Equals(1) ?? false;
            ext.IsOn = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true)?.GetValue("HideFileExt")?.Equals(0) ?? false;
            pchome.IsOn = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true)?.GetValue("LaunchTo")?.Equals(1) ?? false;
            gallery.IsOn = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}", true)?.GetValue("System.IsPinnedToNameSpaceTree")?.Equals(0) ?? false;
            showpc.IsOn = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", true)?.GetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}")?.Equals(0) ?? false;
            shortcut.IsOn = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates", true)?.GetValue("ShortcutNameTemplate")?.Equals("%s.lnk") ?? false;

            if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\DelegateFolders\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}") == null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\DelegateFolders\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}") == null)
            {
                fixlabel.Visibility = Visibility.Collapsed;
                fix.Visibility = Visibility.Collapsed;
            }

            var noDrivesValue = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer")?.GetValue("NoDrives");
            if (noDrivesValue != null && noDrivesValue.ToString() != "0")
            {
                showall.IsEnabled = true;
            }
            else
            {
                showall.IsEnabled = false;
            }
        }

        private async void hide_Click(object sender, RoutedEventArgs e)
        {
            HidePart dialog = new HidePart();
            var result = await dialog.ShowAsync();
            decimal resulty = await dialog.TaskCompletionSource.Task;
            if(resulty != -1)
            {
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer").SetValue("NoDrives", resulty, RegistryValueKind.DWord);
                mw.RebootNotify(2);
            }
            showall.IsEnabled = true;
        }

        private void showall_Click(object sender, RoutedEventArgs e)
        {
            Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer").SetValue("NoDrives", 0, RegistryValueKind.DWord);
            mw.RebootNotify(2);
            showall.IsEnabled = false;
        }

        private void nonremovable_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (nonremovable.IsOn)
                {
                    case true:
                        try
                        {
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{A0953C92-50DC-43bf-BE83-3742FED03C9C}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{A0953C92-50DC-43bf-BE83-3742FED03C9C}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}");

                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{A8CDFF1C-4878-43be-B5FD-F8091C1C60D0}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{d3162b92-9365-467a-956b-92703aca08af}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{A8CDFF1C-4878-43be-B5FD-F8091C1C60D0}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{d3162b92-9365-467a-956b-92703aca08af}");

                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{374DE290-123F-4565-9164-39C4925E467B}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{088e3905-0323-4b02-9826-5d99428e115f}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{374DE290-123F-4565-9164-39C4925E467B}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{088e3905-0323-4b02-9826-5d99428e115f}");

                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3ADD1653-EB32-4cb0-BBD7-DFA0ABB5ACCA}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{24ad3ad4-a569-4530-98e1-ab02f9417aa8}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3ADD1653-EB32-4cb0-BBD7-DFA0ABB5ACCA}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{24ad3ad4-a569-4530-98e1-ab02f9417aa8}");

                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{1CF1260C-4DD0-4ebb-811F-33C572699FDE}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{1CF1260C-4DD0-4ebb-811F-33C572699FDE}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}");

                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}");

                            try
                            {
                                Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}");
                                Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}");
                            }
                            catch
                            {

                            }
                        }
                        catch
                        {

                        }
                        break;
                    case false:
                        try
                        {
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{A0953C92-50DC-43bf-BE83-3742FED03C9C}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{A0953C92-50DC-43bf-BE83-3742FED03C9C}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}");

                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{A8CDFF1C-4878-43be-B5FD-F8091C1C60D0}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{d3162b92-9365-467a-956b-92703aca08af}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{A8CDFF1C-4878-43be-B5FD-F8091C1C60D0}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{d3162b92-9365-467a-956b-92703aca08af}");

                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{374DE290-123F-4565-9164-39C4925E467B}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{088e3905-0323-4b02-9826-5d99428e115f}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{374DE290-123F-4565-9164-39C4925E467B}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{088e3905-0323-4b02-9826-5d99428e115f}");

                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3ADD1653-EB32-4cb0-BBD7-DFA0ABB5ACCA}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{24ad3ad4-a569-4530-98e1-ab02f9417aa8}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3ADD1653-EB32-4cb0-BBD7-DFA0ABB5ACCA}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{24ad3ad4-a569-4530-98e1-ab02f9417aa8}");

                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{1CF1260C-4DD0-4ebb-811F-33C572699FDE}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{1CF1260C-4DD0-4ebb-811F-33C572699FDE}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}");

                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}");

                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}");
                            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}");
                        }
                        catch
                        {

                        }
                        break;
                }
            }
        }

        private void hidden_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (hidden.IsOn)
                {
                    case true:
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("Hidden", 1);
                        break;
                    case false:
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("Hidden", 0);
                        break;
                }
            }
        }

        private void ext_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (ext.IsOn)
                {
                    case true:
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("HideFileExt", 0);
                        break;
                    case false:
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("HideFileExt", 1);
                        break;
                }
            }
        }

        private void pchome_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (pchome.IsOn)
                {
                    case true:
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("LaunchTo", 1);
                        break;
                    case false:
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("LaunchTo", 2);
                        break;
                }
            }
        }

        private void gallery_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (gallery.IsOn)
                {
                    case true:
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}").SetValue("System.IsPinnedToNameSpaceTree", 0);
                        break;
                    case false:
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}").SetValue("System.IsPinnedToNameSpaceTree", 1);
                        break;
                }
            }
        }

        private void showpc_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (showpc.IsOn)
                {
                    case true:
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel").SetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0);
                        break;
                    case false:
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel").SetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 1);
                        break;
                }
            }
        }

        private void shortcut_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (shortcut.IsOn)
                {
                    case true:
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates").SetValue("ShortcutNameTemplate", "%s.lnk");
                        break;
                    case false:
                            Registry.CurrentUser.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates");
                        break;
                }
            }
        }
    }
}

