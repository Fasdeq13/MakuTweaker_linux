using iNKORE.UI.WPF.Modern.Controls;
using MicaWPF.Controls;
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
    public partial class QuickSet : System.Windows.Controls.Page
    {
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

            var accentColor = SystemParameters.WindowGlassColor;
            var lightColor = System.Windows.Media.Color.FromArgb(255,
                (byte)Math.Min(accentColor.R + 60, 255),
                (byte)Math.Min(accentColor.G + 60, 255),
                (byte)Math.Min(accentColor.B + 60, 255));

            SuccessIcon.Stroke = new System.Windows.Media.LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(1, 1),
                GradientStops = new System.Windows.Media.GradientStopCollection
        {
            new System.Windows.Media.GradientStop(lightColor, 0.0),
            new System.Windows.Media.GradientStop(accentColor, 1.0)
        }
            };
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

        private void CheckIfTweaksFinished(bool showCheckmark = false)
        {
            var expanders = new[] { expander, expander2 };
            foreach (var exp in expanders)
            {
                bool hasVisibleItems = false;
                foreach (var item in exp.Items)
                {
                    if (item is FrameworkElement element && element.Visibility == Visibility.Visible)
                    {
                        hasVisibleItems = true;
                        break;
                    }
                }

                if (!hasVisibleItems)
                {
                    exp.Visibility = Visibility.Collapsed;
                }
            }

            bool anyVisible = false;

            foreach (var toggle in GetAllToggles())
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
                info.Message = quick["main"]["infodone"];
                start.Visibility = Visibility.Collapsed;
                expander.Visibility = Visibility.Collapsed;
                expander2.Visibility = Visibility.Collapsed;
                BottomPanel.Visibility = Visibility.Collapsed;

                if (showCheckmark && SuccessOverlay.Visibility != Visibility.Visible)
                    ShowSuccessCheckmark(true);

                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.c6.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                info.Message = quick["main"]["info"];
                start.Visibility = Visibility.Visible;
                BottomPanel.Visibility = Visibility.Visible;
            }
        }

        private void ShowSuccessCheckmark(bool permanent = false)
        {
            SuccessOverlay.Visibility = Visibility.Visible;

            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new System.Windows.Media.Animation.CubicEase
                {
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
                }
            };

            var scaleAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.3,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new System.Windows.Media.Animation.ElasticEase
                {
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut,
                    Oscillations = 1,
                    Springiness = 5
                }
            };

            SuccessOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            SuccessIconTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            SuccessIconTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

            if (!permanent)
            {
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(2000)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();

                    var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(400),
                        EasingFunction = new System.Windows.Media.Animation.CubicEase
                        {
                            EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut
                        }
                    };
                    fadeOut.Completed += (s2, e2) =>
                    {
                        SuccessOverlay.Visibility = Visibility.Collapsed;
                        SuccessOverlay.Opacity = 1;
                    };
                    SuccessOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                };
                timer.Start();
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
                CheckIfTweaksFinished(true);
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
            if (CheckRegValue(Registry.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "506")) ((FrameworkElement)quick_sticky_t.Parent).Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"SOFTWARE\Microsoft\Clipboard", "EnableClipboardHistory", 1)) quick_clipboard.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"Control Panel\Desktop", "MenuShowDelay", "50")) quick_contdelay.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager", "AutoChkTimeout", 60)) ((FrameworkElement)quick_chkdsk_t.Parent).Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\BitLocker", "PreventDeviceEncryption", 1)) ((FrameworkElement)quick_bitlockeroff_t.Parent).Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"SOFTWARE\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}", "System.IsPinnedToNameSpaceTree", 0)) quick_gallery.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios", "HypervisorEnforcedCodeIntegrity", 0)) ((FrameworkElement)quick_coreisol_t.Parent).Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", 0)) ((FrameworkElement)quick_uac_t.Parent).Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", "Off")) ((FrameworkElement)quick_smartscreen_t.Parent).Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 0)) quick_telemetry.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings", "TaskbarEndTask", 1)) quick_endtask.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "verbosestatus", 1)) ((FrameworkElement)quick_verbose_t.Parent).Visibility = Visibility.Collapsed;
            if (Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}") != null) quick_oldcont.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled", 0)) ((FrameworkElement)quick_hybern_t.Parent).Visibility = Visibility.Collapsed;
            if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\DelegateFolders\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}") == null) quick_expfix.Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseUpdatesExpiryTime", "2077-01-01T00:00:00Z")) quick_winupd.Visibility = Visibility.Collapsed;
            if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\Packages")?.GetSubKeyNames().Any(x => x.Contains("DirectPlay")) == true) ((FrameworkElement)quick_directplay_t.Parent).Visibility = Visibility.Collapsed;
            if (CheckRegValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 0))
                ((FrameworkElement)quick_vbs_t.Parent).Visibility = Visibility.Collapsed;

            foreach (var toggle in GetAllToggles())
            {
                if (!IsToggleEffectivelyVisible(toggle))
                {
                    toggle.IsOn = false;
                }
            }
            CheckIfTweaksFinished();
        }

        private async void start_Click(object sender, RoutedEventArgs e)
        {
            bool anySelected = false;
            foreach (var toggle in GetAllToggles())
            {
                if (toggle.IsVisible && toggle.IsOn)
                {
                    anySelected = true;
                    break;
                }
            }

            if (!anySelected)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                    "Please select at least one tweak to apply.",
                    "No Tweaks Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            start.IsEnabled = false;

            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (quick_hidden_t.IsOn == true && quick_hidden.IsVisible)
                    {
                        Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("Hidden", 1);
                        HideAppliedToggle((FrameworkElement)quick_hidden);
                    }
                    if (quick_ext_t.IsOn == true && quick_ext.IsVisible)
                    {
                        Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("HideFileExt", 0);
                        HideAppliedToggle((FrameworkElement)quick_ext);
                    }
                    if (quick_pchome_t.IsOn == true && quick_pchome.IsVisible)
                    {
                        Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("LaunchTo", 1);
                        HideAppliedToggle((FrameworkElement)quick_pchome);
                    }
                    if (quick_expfix_t.IsOn == true && quick_expfix.IsVisible)
                    {
                        try
                        {
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\DelegateFolders\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}");
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\DelegateFolders\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}");
                            HideAppliedToggle((FrameworkElement)quick_expfix);
                        }
                        catch { }
                    }
                    if (quick_showpc_t.IsOn == true && quick_showpc.IsVisible)
                    {
                        Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel").SetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0);
                        HideAppliedToggle((FrameworkElement)quick_showpc);
                    }
                    if (quick_desktopend_t.IsOn == true && quick_desktopend.IsVisible)
                    {
                        try
                        {
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel").SetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0);
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates").SetValue("ShortcutNameTemplate", "%s.lnk");
                            HideAppliedToggle((FrameworkElement)quick_desktopend);
                        }
                        catch { }
                    }
                    if (quick_hidewidget_t.IsOn == true && quick_hidewidget.IsVisible)
                    {
                        try
                        {
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("ShowTaskViewButton", 0);
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("TaskbarDa", 0);
                            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced").SetValue("TaskbarMn", 0);
                            HideAppliedToggle((FrameworkElement)quick_hidewidget);
                        }
                        catch { }
                    }
                    if (quick_removeads_t.IsOn == true && quick_removeads.IsVisible)
                    {
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\SearchSettings").SetValue("IsDynamicSearchBoxEnabled", 0);
                        HideAppliedToggle((FrameworkElement)quick_removeads);
                    }
                    if (quick_bingoff_t.IsOn == true && quick_bingoff.IsVisible)
                    {
                        Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer").SetValue("DisableSearchBoxSuggestions", 1);
                        HideAppliedToggle((FrameworkElement)quick_bingoff);
                    }
                    if (quick_winupd_t.IsOn == true && quick_winupd.IsVisible)
                    {
                        Process.Start("cmd.exe", "/c \"reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v ActiveHoursStart /t REG_DWORD /d 9 /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v ActiveHoursEnd /t REG_DWORD /d 2 /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseFeatureUpdatesStartTime /t REG_SZ /d \"2015-01-01T00:00:00Z\" /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseQualityUpdatesStartTime /t REG_SZ /d \"2015-01-01T00:00:00Z\" /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseUpdatesExpiryTime /t REG_SZ /d \"2077-01-01T00:00:00Z\" /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseFeatureUpdatesEndTime /t REG_SZ /d \"2077-01-01T00:00:00Z\" /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseQualityUpdatesEndTime /t REG_SZ /d \"2077-01-01T00:00:00Z\" /f && reg add HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings /v PauseUpdatesStartTime /t REG_SZ /d \"2015-01-01T00:00:00Z\" /f\"");
                        HideAppliedToggle((FrameworkElement)quick_winupd);
                    }
                    if (quick_sticky_t.IsOn == true && quick_sticky.IsVisible)
                    {
                        Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\StickyKeys").SetValue("Flags", "506");
                        Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\Keyboard Response").SetValue("Flags", "122");
                        Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\ToggleKeys").SetValue("Flags", "58");
                        HideAppliedToggle((FrameworkElement)quick_sticky);
                    }
                    if (quick_clipboard_t.IsOn == true && quick_clipboard.IsVisible)
                    {
                        Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Clipboard").SetValue("EnableClipboardHistory", 1);
                        HideAppliedToggle((FrameworkElement)quick_clipboard);
                    }
                    if (quick_contdelay_t.IsOn == true && quick_contdelay.IsVisible)
                    {
                        Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop").SetValue("MenuShowDelay", "50");
                        HideAppliedToggle((FrameworkElement)quick_contdelay);
                    }
                    if (quick_chkdsk_t.IsOn == true && quick_chkdsk.IsVisible)
                    {
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager").SetValue("AutoChkTimeout", 60);
                        HideAppliedToggle((FrameworkElement)quick_chkdsk);
                    }
                    if (quick_directplay_t.IsOn && quick_directplay.IsVisible)
                    {
                        Process.Start("powershell.exe", "-Command \"& dism /online /Enable-Feature /FeatureName:DirectPlay /All\"");
                        HideAppliedToggle((FrameworkElement)quick_directplay);
                    }
                    if (quick_bitlockeroff_t.IsOn == true && quick_bitlockeroff.IsVisible)
                    {
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\BitLocker").SetValue("PreventDeviceEncryption", 1, RegistryValueKind.DWord);
                        HideAppliedToggle((FrameworkElement)quick_bitlockeroff);
                    }
                    if (quick_gallery_t.IsOn == true && quick_gallery.IsVisible)
                    {
                        Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}").SetValue("System.IsPinnedToNameSpaceTree", 0);
                        HideAppliedToggle((FrameworkElement)quick_gallery);
                    }
                    if (quick_coreisol_t.IsOn == true && quick_coreisol.IsVisible)
                    {
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios").SetValue("HypervisorEnforcedCodeIntegrity", 0);
                        HideAppliedToggle((FrameworkElement)quick_coreisol);
                    }
                    if (quick_uac_t.IsOn == true && quick_uac.IsVisible)
                    {
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("EnableLUA", 0);
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments")?.SetValue("SaveZoneInformation", 1, RegistryValueKind.DWord);
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Associations")?.SetValue("LowRiskFileTypes", ".exe;.msi;.bat;", RegistryValueKind.String);
                        HideAppliedToggle((FrameworkElement)quick_uac);
                    }
                    if (quick_smartscreen_t.IsOn == true && quick_smartscreen.IsVisible)
                    {
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("EnableSmartScreen", 0);
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer").SetValue("SmartScreenEnabled", "Off", RegistryValueKind.String);
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments").SetValue("SaveZoneInformation", 1, RegistryValueKind.DWord);
                        HideAppliedToggle((FrameworkElement)quick_smartscreen);
                    }
                    if (quick_hybern_t.IsOn == true && quick_hybern.IsVisible)
                    {
                        Process.Start("cmd.exe", "/C powercfg /h off");
                        HideAppliedToggle((FrameworkElement)quick_hybern);
                    }
                    if (quick_telemetry_t.IsOn == true && quick_telemetry.IsVisible)
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
                    if (quick_endtask_t.IsOn == true && quick_endtask.IsVisible)
                    {
                        Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings").SetValue("TaskbarEndTask", 1);
                        HideAppliedToggle((FrameworkElement)quick_endtask);
                    }
                    if (quick_verbose_t.IsOn == true && quick_verbose.IsVisible)
                    {
                        Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System").SetValue("verbosestatus", 1);
                        HideAppliedToggle((FrameworkElement)quick_verbose);
                    }
                    if (quick_oldcont_t.IsOn == true && quick_oldcont.IsVisible)
                    {
                        Process.Start("cmd.exe", "/c \"reg.exe add \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32\" /f /ve\"");
                        Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel").SetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0);
                        HideAppliedToggle((FrameworkElement)quick_oldcont);
                    }
                    if (quick_vbs_t.IsOn == true && quick_vbs.IsVisible)
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
                            HideAppliedToggle((FrameworkElement)quick_vbs);
                        }
                        catch { }
                    }
                });
            });

            start.IsEnabled = true;
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

            expander.Header = quick["main"]["title"];
            expander2.Header = quick["main"]["title2"];
            makuos_tooltip.Content = quick["main"]["makuos_tooltip"];
            label.Text = quick["main"]["label"];
            info.Message = quick["main"]["info"];
            start.Content = quick["main"]["b"];
            makuos.Content = quick["main"]["makuos"];
            selectAll.Content = quick["main"]["checkall"];
            selectAll2.Content = quick["main"]["checkall"];
            var onText = basel["def"]["on"];
            var offText = basel["def"]["off"];

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
            quick_endtask.Header = per["main"]["etask"];
            quick_bingoff.Header = sr["main"]["bing"];
            quick_telemetry.Header = sr["main"]["telemetry"];

            quick_directplay_txt.Text = compon["main"]["directplay"];
            quick_bitlockeroff_txt.Text = sr["main"]["bitlocker"];
            quick_sticky_txt.Text = sr["main"]["sticky"];
            quick_chkdsk_txt.Text = sr["main"]["chkdsk"];
            quick_coreisol_txt.Text = sr["main"]["coreisol"];
            quick_uac_txt.Text = sr["main"]["uac"];
            quick_smartscreen_txt.Text = sr["main"]["smartscreen"];
            quick_hybern_txt.Text = sr["main"]["hybern"];
            quick_vbs_txt.Text = adv["main"]["vbs"];
            quick_verbose_txt.Text = per["main"]["verbose"];

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

            foreach (var toggle in GetAllToggles())
            {
                toggle.OnContent = onText;
                toggle.OffContent = offText;
            }
        }

        private IEnumerable<iNKORE.UI.WPF.Modern.Controls.ToggleSwitch> GetAllToggles()
        {
            var expanders = new[] { expander, expander2 };

            foreach (var exp in expanders)
            {
                foreach (var item in exp.Items)
                {
                    if (item is SettingsCard card && card.Content is iNKORE.UI.WPF.Modern.Controls.ToggleSwitch toggle)
                    {
                        yield return toggle;
                    }
                }
            }
        }

        private async void selectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox mainToggle)
            {
                bool targetState = mainToggle.IsChecked == true;
                UpdateAllTogglesInCategory(mainToggle, targetState);
            }
        }

        private async Task UpdateAllTogglesInCategory(CheckBox mainToggle, bool targetState)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(mainToggle);
            while (parent != null && !(parent is SettingsExpander))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent is SettingsExpander expander)
            {
                foreach (var item in expander.Items)
                {
                    if (item is SettingsCard card && card.Content is iNKORE.UI.WPF.Modern.Controls.ToggleSwitch childToggle)
                    {
                        if (childToggle.IsEnabled && card.Visibility == Visibility.Visible)
                        {
                            childToggle.IsOn = targetState;
                        }
                    }
                }
            }
        }

        private async void makuos_Click(object sender, RoutedEventArgs e)
        {
            foreach (var toggle in GetAllToggles())
            {
                if (toggle.IsEnabled && IsToggleEffectivelyVisible(toggle))
                {
                    toggle.IsOn = false;
                }
            }

            if (IsToggleEffectivelyVisible(quick_pchome_t)) quick_pchome_t.IsOn = true;
            if (IsToggleEffectivelyVisible(quick_hidden_t)) quick_hidden_t.IsOn = true;
            if (IsToggleEffectivelyVisible(quick_ext_t)) quick_ext_t.IsOn = true;
            if (IsToggleEffectivelyVisible(quick_showpc_t)) quick_showpc_t.IsOn = true;
            if (IsToggleEffectivelyVisible(quick_removeads_t)) quick_removeads_t.IsOn = true;
            if (IsToggleEffectivelyVisible(quick_bitlockeroff_t)) quick_bitlockeroff_t.IsOn = true;
            if (IsToggleEffectivelyVisible(quick_sticky_t)) quick_sticky_t.IsOn = true;
            if (IsToggleEffectivelyVisible(quick_contdelay_t)) quick_contdelay_t.IsOn = true;
            if (IsToggleEffectivelyVisible(quick_telemetry_t)) quick_telemetry_t.IsOn = true;
        }
    }
}
