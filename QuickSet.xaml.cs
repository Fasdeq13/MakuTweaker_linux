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
using MicaWPF.Controls;
using Microsoft.Win32;
using Windows.UI.Composition.Desktop;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MakuTweakerNew
{
    public partial class QuickSet : Page
    {
        MainWindow mw = (MainWindow)Application.Current.MainWindow;
        bool togglesState = true;
        string uncheckText;
        string checkAllText;
        public QuickSet()
        {
            InitializeComponent();
            LoadLang();
            HideAlreadyAppliedTweaks();


            if (checkWinVer() < 22621)
            {
                quick_oldcont.Visibility = Visibility.Collapsed;
                quick_endtask.Visibility = Visibility.Collapsed;
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

        private void CheckIfTweaksFinished()
        {
            var visibleToggles = GetAllToggles(ToggleContainer).Where(t => IsToggleEffectivelyVisible(t)).ToList();

            int visibleCount = visibleToggles.Count;
            bool anyVisible = false;

            foreach (var toggle in GetAllToggles(ToggleContainer))
            {
                if (IsToggleEffectivelyVisible(toggle))
                {
                    anyVisible = true;
                    break;
                }
            }

            var languageCode = Properties.Settings.Default.lang ?? "en";
            var quick = MainWindow.Localization.LoadLocalization(languageCode, "quick");

            if (!anyVisible)
            {
                info.Text = quick["main"]["infodone"];
                start.Visibility = Visibility.Collapsed;
                uncheck.Visibility = Visibility.Collapsed;
            }
            else
            {
                info.Text = quick["main"]["info"];
                start.Visibility = Visibility.Visible;
                uncheck.Visibility = Visibility.Visible;

                if (visibleCount < 6)
                {
                    uncheck.Visibility = Visibility.Collapsed;
                }
                else
                {
                    uncheck.Visibility = Visibility.Visible;
                }
            }
        }

        private bool IsToggleEffectivelyVisible(FrameworkElement toggle)
        {
            bool isSelfVisible = toggle.Visibility == Visibility.Visible;
            bool isParentVisible = toggle.Parent is FrameworkElement parent ? parent.Visibility == Visibility.Visible : true;

            return isSelfVisible && isParentVisible;
        }

        private bool CheckRegValue(RegistryKey root, string path, string name, object expected)
        {
            try
            {
                using (var key = root.OpenSubKey(path))
                {
                    if (key == null)
                        return false;

                    var value = key.GetValue(name);
                    if (value == null)
                        return false;

                    return value.ToString() == expected.ToString();
                }
            }
            catch
            {
                return false;
            }
        }

        private void AnimateHide(UIElement element)
        {
            CloseTooltips(element);
            var fade = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new System.Windows.Media.Animation.CubicEase
                {
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut
                }
            };

            fade.Completed += (s, e) =>
            {
                element.Visibility = Visibility.Collapsed;
                element.Opacity = 1;
                CheckIfTweaksFinished();
            };

            element.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private void CloseTooltips(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is FrameworkElement fe && fe.ToolTip is ToolTip tt)
                {
                    tt.IsOpen = false;
                }

                CloseTooltips(child);
            }
        }

        private void HideAppliedToggle(FrameworkElement element)
        {
            if (element == null)
                return;

            AnimateHide(element);
        }

        private void HideAlreadyAppliedTweaks()
        {
            if (CheckRegValue(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", 1)) quick_hidden.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 0)) quick_ext.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 1)) quick_pchome.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0)) quick_showpc.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates", "ShortcutNameTemplate", "%s.lnk")) quick_desktopend.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 0)) quick_hidewidget.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDynamicSearchBoxEnabled", 0)) quick_removeads.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1)) quick_bingoff.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "506")) ((FrameworkElement)quick_sticky.Parent).Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"SOFTWARE\Microsoft\Clipboard", "EnableClipboardHistory", 1)) quick_clipboard.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"Control Panel\Desktop", "MenuShowDelay", "50")) quick_contdelay.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager", "AutoChkTimeout", 60)) ((FrameworkElement)quick_chkdsk.Parent).Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\BitLocker", "PreventDeviceEncryption", 1)) ((FrameworkElement)quick_bitlockeroff.Parent).Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"SOFTWARE\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}", "System.IsPinnedToNameSpaceTree", 0)) quick_gallery.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios", "HypervisorEnforcedCodeIntegrity", 0)) ((FrameworkElement)quick_coreisol.Parent).Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", 0)) ((FrameworkElement)quick_uac.Parent).Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", "Off")) ((FrameworkElement)quick_smartscreen.Parent).Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 0)) quick_telemetry.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings", "TaskbarEndTask", 1)) quick_endtask.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "verbosestatus", 1)) ((FrameworkElement)quick_verbose.Parent).Visibility = Visibility.Collapsed;
            if (Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}") != null) quick_oldcont.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled", 0)) ((FrameworkElement)quick_hybern.Parent).Visibility = Visibility.Collapsed;
            if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\DelegateFolders\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}") == null) quick_expfix.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine,@"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings","PauseUpdatesExpiryTime","2077-01-01T00:00:00Z")) quick_winupd.Visibility = Visibility.Collapsed;
            if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\Packages") ?.GetSubKeyNames().Any(x => x.Contains("DirectPlay")) == true) ((FrameworkElement)quick_directplay.Parent).Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 0))
                ((FrameworkElement)quick_vbs.Parent).Visibility = Visibility.Collapsed;

            foreach (var toggle in GetAllToggles(ToggleContainer))
            {
                if (!IsToggleEffectivelyVisible(toggle))
                {
                    toggle.IsOn = false;
                }
            }
            CheckIfTweaksFinished();
        }

        private void start_Click(object sender, RoutedEventArgs e)
        {
            if (quick_hidden.IsOn == true && quick_hidden.IsVisible)
            {
                Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("Hidden", 1);
                HideAppliedToggle((FrameworkElement)quick_hidden);
            }
            if (quick_ext.IsOn == true && quick_ext.IsVisible)
            {
                Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("HideFileExt", 0);
                HideAppliedToggle((FrameworkElement)quick_ext);
            }
            if (quick_pchome.IsOn == true && quick_pchome.IsVisible)
            {
                Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("LaunchTo", 1);
                HideAppliedToggle((FrameworkElement)quick_pchome);
            }
            if (quick_expfix.IsOn == true && quick_expfix.IsVisible)
            {
                try
                {
                    Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\DelegateFolders\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}");
                    Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\DelegateFolders\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}");
                    HideAppliedToggle((FrameworkElement)quick_expfix);
                }
                catch
                {

                }
            }
            if (quick_showpc.IsOn == true && quick_showpc.IsVisible)
            {
                Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel").SetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0);
                HideAppliedToggle((FrameworkElement)quick_showpc);
            }
            if (quick_desktopend.IsOn == true && quick_desktopend.IsVisible)
            {
                try
                {
                    Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel").SetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0);
                    Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates").SetValue("ShortcutNameTemplate", "%s.lnk");
                    HideAppliedToggle((FrameworkElement)quick_desktopend);
                }
                catch
                {

                }
            }
            if (quick_hidewidget.IsOn == true && quick_hidewidget.IsVisible)
            {
                HideAppliedToggle((FrameworkElement)quick_hidewidget);
                try
                {
                    Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("ShowTaskViewButton", 0);
                    Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("TaskbarDa", 0);
                    Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("TaskbarMn", 0);
                }
                catch
                {

                }
            }
            if (quick_removeads.IsOn == true && quick_removeads.IsVisible)
            {
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\SearchSettings").SetValue("IsDynamicSearchBoxEnabled", 0);
                HideAppliedToggle((FrameworkElement)quick_removeads);
            }
            if (quick_bingoff.IsOn == true && quick_bingoff.IsVisible)
            {
                Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer").SetValue("DisableSearchBoxSuggestions", 1);
                HideAppliedToggle((FrameworkElement)quick_bingoff);
            }
            if (quick_winupd.IsOn == true && quick_winupd.IsVisible)
            {
                Process.Start("cmd.exe", "/c \"reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v ActiveHoursStart /t REG_DWORD /d 9 /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v ActiveHoursEnd /t REG_DWORD /d 2 /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseFeatureUpdatesStartTime /t REG_SZ /d \"2015-01-01T00:00:00Z\" /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseQualityUpdatesStartTime /t REG_SZ /d \"2015-01-01T00:00:00Z\" /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseUpdatesExpiryTime /t REG_SZ /d \"2077-01-01T00:00:00Z\" /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseFeatureUpdatesEndTime /t REG_SZ /d \"2077-01-01T00:00:00Z\" /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseQualityUpdatesEndTime /t REG_SZ /d \"2077-01-01T00:00:00Z\" /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseUpdatesStartTime /t REG_SZ /d \"2015-01-01T00:00:00Z\" /f\"");
                HideAppliedToggle((FrameworkElement)quick_winupd);
            }
            if (quick_sticky.IsOn == true && quick_sticky.IsVisible)
            {
                Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\StickyKeys").SetValue("Flags", "506");
                Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\Keyboard Response").SetValue("Flags", "122");
                Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\ToggleKeys").SetValue("Flags", "58");
                HideAppliedToggle((FrameworkElement)quick_sticky.Parent);
            }

            if (quick_clipboard.IsOn == true && quick_clipboard.IsVisible)
            {
                Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Clipboard").SetValue("EnableClipboardHistory", 1);
                HideAppliedToggle((FrameworkElement)quick_clipboard);
            }
            if (quick_contdelay.IsOn == true && quick_contdelay.IsVisible)
            {
                Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop").SetValue("MenuShowDelay", "50");
                HideAppliedToggle((FrameworkElement)quick_contdelay);
            }
            if (quick_chkdsk.IsOn == true && quick_chkdsk.IsVisible)
            {
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager").SetValue("AutoChkTimeout", 60);
                HideAppliedToggle((FrameworkElement)quick_chkdsk.Parent);
            }
            if (quick_directplay.IsOn && quick_directplay.IsVisible)
            {
                Process.Start("powershell.exe", "-Command \"& dism /online /Enable-Feature /FeatureName:DirectPlay /All\"");
                HideAppliedToggle((FrameworkElement)quick_directplay.Parent);
            }
            if (quick_bitlockeroff.IsOn == true && quick_bitlockeroff.IsVisible)
            {
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\BitLocker").SetValue("PreventDeviceEncryption", 1, RegistryValueKind.DWord);
                HideAppliedToggle((FrameworkElement)quick_bitlockeroff.Parent);
            }
            if(quick_gallery.IsOn == true && quick_gallery.IsVisible)
            {
                Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}").SetValue("System.IsPinnedToNameSpaceTree", 0);
                HideAppliedToggle((FrameworkElement)quick_gallery);
            }
            if (quick_coreisol.IsOn == true && quick_coreisol.IsVisible)
            {
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios").SetValue("HypervisorEnforcedCodeIntegrity", 0);
                HideAppliedToggle((FrameworkElement)quick_coreisol.Parent);

            }
            if (quick_uac.IsOn == true && quick_uac.IsVisible)
            {
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("EnableLUA", 0);
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments")?.SetValue("SaveZoneInformation", 1, RegistryValueKind.DWord);
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Associations")?.SetValue("LowRiskFileTypes", ".exe;.msi;.bat;", RegistryValueKind.String);
                HideAppliedToggle((FrameworkElement)quick_uac.Parent);
            }
            if (quick_smartscreen.IsOn == true && quick_smartscreen.IsVisible)
            {
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("EnableSmartScreen", 0);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer").SetValue("SmartScreenEnabled", "Off", RegistryValueKind.String);
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments").SetValue("SaveZoneInformation", 1, RegistryValueKind.DWord);
                HideAppliedToggle((FrameworkElement)quick_smartscreen.Parent);
            }
            if (quick_hybern.IsOn == true && quick_hybern.IsVisible)
            {
                Process.Start("cmd.exe", "/C powercfg /h off");
                HideAppliedToggle((FrameworkElement)quick_hybern.Parent);
            }
            if (quick_telemetry.IsOn == true && quick_telemetry.IsVisible)
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
                HideAppliedToggle((FrameworkElement)quick_telemetry);
            }
            if (quick_endtask.IsOn == true && quick_endtask.IsVisible)
            {
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings").SetValue("TaskbarEndTask", 1);
                HideAppliedToggle((FrameworkElement)quick_endtask);

            }
            if (quick_verbose.IsOn == true && quick_verbose.IsVisible)
            {
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("verbosestatus", 1);
                HideAppliedToggle((FrameworkElement)quick_verbose.Parent);
            }
            if (quick_oldcont.IsOn == true && quick_oldcont.IsVisible)
            {
                Process.Start("cmd.exe", "/c \"reg.exe add \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32\" /f /ve\"");
                Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel").SetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0);
                HideAppliedToggle((FrameworkElement)quick_oldcont);
            }
            if (quick_vbs.IsOn == true && quick_vbs.IsVisible)
            {
                try
                {
                    Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord);
                    Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("RequirePlatformSecurityFeatures", 0, RegistryValueKind.DWord);
                    Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa").SetValue("LsaCfgFlags", 0, RegistryValueKind.DWord);
                    Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity").SetValue("Enabled", 0, RegistryValueKind.DWord);
                    Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("RequireMicrosoftSignedBootChain", 0, RegistryValueKind.DWord);
                    Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("KernelDMAProtection", 0, RegistryValueKind.DWord);
                    Process.Start("cmd.exe", "/c bcdedit /set hypervisorlaunchtype off");
                    HideAppliedToggle((FrameworkElement)quick_vbs.Parent);
                }
                catch { }
            }
        }
        private void LoadLang()
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var basel = MainWindow.Localization.LoadLocalization(languageCode, "base");
            var quick = MainWindow.Localization.LoadLocalization(languageCode, "quick");
            var tooltips = MainWindow.Localization.LoadLocalization(languageCode, "tooltips");
            var expl = MainWindow.Localization.LoadLocalization(languageCode, "expl");
            var wu = MainWindow.Localization.LoadLocalization(languageCode, "wu");
            var sr = MainWindow.Localization.LoadLocalization(languageCode, "sr");
            var adv = MainWindow.Localization.LoadLocalization(languageCode, "adv");
            var per = MainWindow.Localization.LoadLocalization(languageCode, "per");
            var compon = MainWindow.Localization.LoadLocalization(languageCode, "compon");

            quick_hidewidget.Header = quick["main"]["hidewidget"];
            quick_removeads.Header = quick["main"]["removeads"];
            quick_clipboard.Header = quick["main"]["clipboard"];

            quick_hidden.Header = expl["main"]["hidden"];
            quick_ext.Header = expl["main"]["ext"];
            quick_pchome.Header = expl["main"]["pchome"];
            quick_gallery.Header = expl["main"]["gallery"];
            quick_showpc.Header = expl["main"]["showpc"];
            quick_desktopend.Header = expl["main"]["shortcut"];
            quick_expfix.Header = expl["main"]["fixlabel"];

            quick_winupd.Header = wu["main"]["wu5"];
            quick_contdelay.Header = per["main"]["delcont"];
            quick_oldcont.Header = per["main"]["oldcont"];
            quick_verbose.Header = per["main"]["verbose"];
            quick_endtask.Header = per["main"]["etask"];
            quick_directplay.Header = compon["main"]["directplay"];

            quick_bitlockeroff.Header = sr["main"]["bitlocker"];
            quick_bingoff.Header = sr["main"]["bing"];
            quick_sticky.Header = sr["main"]["sticky"];
            quick_chkdsk.Header = sr["main"]["chkdsk"];
            quick_coreisol.Header = sr["main"]["coreisol"];
            quick_uac.Header = sr["main"]["uac"];
            quick_smartscreen.Header = sr["main"]["smartscreen"];
            quick_hybern.Header = sr["main"]["hybern"];
            quick_vbs.Header = adv["main"]["vbs"];
            quick_telemetry.Header = sr["main"]["telemetry"];

            label.Text = quick["main"]["label"];
            info.Text = quick["main"]["info"];
            start.Content = quick["main"]["b"];
            uncheckText = quick["main"]["uncheck"];
            checkAllText = quick["main"]["checkall"];
            uncheck.Content = uncheckText;

            var onText = basel["def"]["on"];
            var offText = basel["def"]["off"];

            foreach (var toggle in GetAllToggles(ToggleContainer))
            {
                toggle.OnContent = onText;
                toggle.OffContent = offText;
            }

            sys_tooltip_sticky.Content = tooltips["main"]["sticky"];
            sys_tooltip_coreisol.Content = tooltips["main"]["coreisol"];
            sys_tooltip_uac.Content = tooltips["main"]["duac"];
            sys_tooltip_smartscreen.Content = tooltips["main"]["smartscr"];
            sys_tooltip_hyber.Content = tooltips["main"]["hybern"];
            sys_tooltip_vbs.Content = tooltips["main"]["coreisol"];
            sys_tooltip_chkdsk.Content = tooltips["main"]["chkdsk"];
            sys_tooltip_bitlocker.Content = tooltips["main"]["bitlocker"];
            sys_tooltip_verbose.Content = tooltips["main"]["advanced"];
            sys_tooltip_directplay.Content = tooltips["main"]["directplay"];
        }

        private IEnumerable<ModernWpf.Controls.ToggleSwitch> GetAllToggles(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is ModernWpf.Controls.ToggleSwitch toggle)
                    yield return toggle;

                foreach (var t in GetAllToggles(child))
                    yield return t;
            }
        }

        private void SetAllToggles(bool state)
        {
            foreach (var toggle in GetAllToggles(ToggleContainer))
            {
                if (IsToggleEffectivelyVisible(toggle))
                    toggle.IsOn = state;
            }
        }

        private void uncheck_Click(object sender, RoutedEventArgs e)
        {
            togglesState = !togglesState;
            SetAllToggles(togglesState);
            uncheck.Content = togglesState ? uncheckText : checkAllText;
        }
    }
}
