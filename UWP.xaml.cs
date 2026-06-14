using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Packaging;
using System.Linq;
using System.Media;
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
using Microsoft.Win32;
using iNKORE.UI.WPF.Modern.Controls;
using ModernWpf.Media.Animation;
using Windows.UI.Composition.Desktop;

namespace MakuTweakerNew
{
    public partial class UWP : System.Windows.Controls.Page
    {
        MainWindow mw = (MainWindow)Application.Current.MainWindow;
        private bool _isChecking;
        private bool _progressShown;

        public UWP()
        {
            InitializeComponent();
            LoadLang();
            Loaded += async (_, __) =>
            {
                _isChecking = true;
                _progressShown = true;

                await CheckInstalledUWPAppsAsync(true);

                _isChecking = false;
            };

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

        private async Task HideProgressSmoothAsync()
        {
            FadeOut(loadingPanel, 300);
            await Task.Delay(300);
            loadingPanel.Visibility = Visibility.Collapsed;
            ring.IsActive = false;

            FadeIn(MainScrollViewer, 400);
            FadeIn(b, 400);
            FadeIn(info2, 400);

            _progressShown = false;
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

            FadeOut(info2, 300);
            FadeOut(MainScrollViewer, 300);

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

                    FadeIn(info2, 300);
                    FadeIn(MainScrollViewer, 300);
                };
                timer.Start();
            }
        }

        private void ShowProgressSmooth()
        {
            _progressShown = true;

            FadeOut(MainScrollViewer, 300);
            FadeOut(b, 300);
            FadeOut(info2, 300);

            ring.IsActive = true;
            loadingPanel.Visibility = Visibility.Visible;
            loadingPanel.Opacity = 0;
            FadeIn(loadingPanel, 400);
        }

        private async Task CheckInstalledUWPAppsAsync(bool anim)
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var uwp = MainWindow.Localization.LoadLocalization(languageCode, "uwp");

            t.Text = uwp["main"]["chk"];
            if (!_progressShown) ShowProgressSmooth();

            string[] appIds =
            {
        "Microsoft.ZuneVideo",
        "Microsoft.ZuneMusic",
        "Microsoft.MicrosoftStickyNotes",
        "Microsoft.MixedReality.Portal",
        "Microsoft.MicrosoftSolitaireCollection",
        "Microsoft.Messaging",
        "Microsoft.WindowsFeedbackHub",
        "microsoft.windowscommunicationsapps",
        "Microsoft.BingNews",
        "Microsoft.Microsoft3DViewer",
        "Microsoft.BingWeather",
        "Microsoft.549981C3F5F10",
        "Microsoft.XboxApp",
        "Microsoft.GetHelp",
        "Microsoft.WindowsCamera",
        "Microsoft.WindowsMaps",
        "Microsoft.Office.OneNote",
        "Microsoft.YourPhone",
        "Microsoft.Windows.DevHome",
        "Clipchamp.Clipchamp",
        "Microsoft.PowerAutomateDesktop",
        "Microsoft.Getstarted",
        "Microsoft.WindowsSoundRecorder",
        "Microsoft.WindowsStore",
        "Microsoft.People",
        "Microsoft.SkypeApp",
        "Microsoft.WindowsAlarms",
        "Microsoft.OutlookForWindows",
        "MSTeams",
        "Microsoft.Todos",
        "Microsoft.Copilot",
        "Microsoft.MicrosoftOfficeHub"
    };
            int installedCount = 0;

            var installedApps = await GetInstalledUWPAppsAsync();
            foreach (var appId in appIds)
            {
                bool isInstalled = installedApps.Contains(appId);

                if (appId == "Microsoft.XboxApp")
                {
                    isInstalled = installedApps.Contains("Microsoft.XboxApp") ||
                                  installedApps.Contains("Microsoft.GamingApp");
                }

                if (appId == "Microsoft.Microsoft3DViewer")
                {
                    isInstalled = installedApps.Contains("Microsoft.Microsoft3DViewer") ||
                                  installedApps.Contains("Microsoft.MSPaint") ||
                                  installedApps.Contains("Microsoft.3DBuilder");
                }

                if (appId == "Microsoft.Copilot" || appId == "Microsoft.MicrosoftOfficeHub")
                {
                    isInstalled = installedApps.Contains("Microsoft.Copilot") ||
                                  installedApps.Contains("Microsoft.MicrosoftOfficeHub");
                }

                if (SettingsCardMap.TryGetValue(appId, out var mappedSettingsCard))
                {
                    mappedSettingsCard.Visibility = isInstalled ? Visibility.Visible : Visibility.Collapsed;
                    mappedSettingsCard.IsEnabled = isInstalled;
                    if (isInstalled) installedCount++;
                }

                if (ToggleMap.TryGetValue(appId, out var mappedToggle))
                {
                    mappedToggle.IsEnabled = isInstalled;
                    if (mappedToggle != u9t && mappedToggle != u15t && mappedToggle != u16t 
                        && mappedToggle != u17t && mappedToggle != u23t && mappedToggle != u24t)
                        mappedToggle.IsOn = isInstalled;
                    else
                        mappedToggle.IsOn = false;
                }
            }
            
            UpdateCategorySelectAllState(e1, selectAllE1);
            UpdateCategorySelectAllState(e2, selectAllE2);
            UpdateCategoryVisibility();

            b.IsEnabled = AllToggles.Any(t => t.IsOn);
            _isChecking = false;

            bool shouldHide = installedCount < 5;
            if (mw != null)
            {
                mw.c5.Visibility = shouldHide ? Visibility.Collapsed : Visibility.Visible;
            }

            Properties.Settings.Default.UwpHidden = shouldHide;
            Properties.Settings.Default.UwpChecked = true;
            Properties.Settings.Default.Save();

            if (installedCount == 0)
            {
                if (_progressShown)
                {
                    FadeOut(loadingPanel, 300);
                    await Task.Delay(300);
                    loadingPanel.Visibility = Visibility.Collapsed;
                    ring.IsActive = false;
                    _progressShown = false;
                }
                ShowSuccessCheckmark(true);
            }
            else
            {
                if (_progressShown)
                {
                    await HideProgressSmoothAsync();
                }
            }
        }

        private void UpdateCategorySelectAllState(SettingsExpander expander, CheckBox mainCheckBox)
        {
            int totalApps = 0;
            int selectedApps = 0;

            foreach (var item in expander.Items)
            {
                if (item is SettingsCard card && card.IsEnabled && card.Visibility == Visibility.Visible)
                {
                    if (card.Content is ToggleSwitch appToggle)
                    {
                        totalApps++;
                        if (appToggle.IsOn) selectedApps++;
                    }
                }
            }

            if (totalApps == 0)
            {
                mainCheckBox.IsChecked = false;
            }
            else
            {
                mainCheckBox.IsEnabled = true;
                if (selectedApps == 0)
                {
                    mainCheckBox.IsChecked = false;
                }
                else if (selectedApps == totalApps)
                {
                    mainCheckBox.IsChecked = true;
                }
                else
                {
                    mainCheckBox.IsChecked = null;
                }
            }
        }

        private void UpdateCategoryVisibility()
        {
            var bloatCards = new[] { u1, u2, u3, u4, u5, u6, u7, u8, u10, u11, u12, u13, u14, u18, u19, u20, u21, u22, u25, u26, u27, u28, u29, u30, u31 };
            var popularCards = new[] { u15, u16, u17, u9 };
            var necessaryCards = new[] { u23, u24 };

            bool bloatHasVisible = bloatCards.Any(c => c.Visibility == Visibility.Visible);
            bool popularHasVisible = popularCards.Any(c => c.Visibility == Visibility.Visible);
            bool necessaryHasVisible = necessaryCards.Any(c => c.Visibility == Visibility.Visible);

            e1.Visibility = bloatHasVisible ? Visibility.Visible : Visibility.Collapsed;
            e2.Visibility = popularHasVisible ? Visibility.Visible : Visibility.Collapsed;
            e3.Visibility = necessaryHasVisible ? Visibility.Visible : Visibility.Collapsed;

            b.Visibility = (bloatHasVisible || popularHasVisible || necessaryHasVisible) ? Visibility.Visible : Visibility.Collapsed;
        }

        private List<iNKORE.UI.WPF.Modern.Controls.SettingsCard> AllSettingsCards => new()
        {
            u1,u2,u3,u4,u5,u6,u7,u8,u9,u10,
            u11,u12,u13,u14,u15,u16,u17,u18,
            u19,u20,u21,u22,u23,u24,u25,u26,u27,u28,
            u29, u30, u31
        };

        private List<iNKORE.UI.WPF.Modern.Controls.ToggleSwitch> AllToggles => new()
        {
            u1t,u2t,u3t,u4t,u5t,u6t,u7t,u8t,u9t,u10t,
            u11t,u12t,u13t,u14t,u15t,u16t,u17t,u18t,
            u19t,u20t,u21t,u22t,u23t,u24t,u25t,u26t,u27t,u28t,
            u29t, u30t, u31t
        };

        private Dictionary<string, iNKORE.UI.WPF.Modern.Controls.SettingsCard> SettingsCardMap => new()
        {
            ["Microsoft.MixedReality.Portal"] = u1,
            ["Microsoft.MicrosoftSolitaireCollection"] = u2,
            ["Microsoft.Messaging"] = u3,
            ["Microsoft.549981C3F5F10"] = u4,
            ["Microsoft.GetHelp"] = u5,
            ["Microsoft.WindowsFeedbackHub"] = u6,
            ["Microsoft.Windows.DevHome"] = u7,
            ["Microsoft.Microsoft3DViewer"] = u8,
            ["Microsoft.YourPhone"] = u9,
            ["Microsoft.WindowsMaps"] = u10,
            ["Microsoft.PowerAutomateDesktop"] = u11,
            ["Clipchamp.Clipchamp"] = u12,
            ["microsoft.windowscommunicationsapps"] = u13,
            ["Microsoft.Office.OneNote"] = u14,
            ["Microsoft.ZuneMusic"] = u15,
            ["Microsoft.ZuneVideo"] = u16,
            ["Microsoft.WindowsCamera"] = u17,
            ["Microsoft.BingNews"] = u18,
            ["Microsoft.BingWeather"] = u19,
            ["Microsoft.MicrosoftStickyNotes"] = u20,
            ["Microsoft.Getstarted"] = u21,
            ["Microsoft.WindowsSoundRecorder"] = u22,
            ["Microsoft.WindowsStore"] = u23,
            ["Microsoft.XboxApp"] = u24,
            ["Microsoft.People"] = u25,
            ["Microsoft.SkypeApp"] = u26,
            ["Microsoft.WindowsAlarms"] = u27,
            ["Microsoft.OutlookForWindows"] = u28,
            ["MSTeams"] = u29,
            ["Microsoft.Todos"] = u30,
            ["Microsoft.Copilot"] = u31,
            ["Microsoft.MicrosoftOfficeHub"] = u31
        };

        private Dictionary<string, iNKORE.UI.WPF.Modern.Controls.ToggleSwitch> ToggleMap => new()
        {
            ["Microsoft.MixedReality.Portal"] = u1t,
            ["Microsoft.MicrosoftSolitaireCollection"] = u2t,
            ["Microsoft.Messaging"] = u3t,
            ["Microsoft.549981C3F5F10"] = u4t,
            ["Microsoft.GetHelp"] = u5t,
            ["Microsoft.WindowsFeedbackHub"] = u6t,
            ["Microsoft.Windows.DevHome"] = u7t,
            ["Microsoft.Microsoft3DViewer"] = u8t,
            ["Microsoft.YourPhone"] = u9t,
            ["Microsoft.WindowsMaps"] = u10t,
            ["Microsoft.PowerAutomateDesktop"] = u11t,
            ["Clipchamp.Clipchamp"] = u12t,
            ["microsoft.windowscommunicationsapps"] = u13t,
            ["Microsoft.Office.OneNote"] = u14t,
            ["Microsoft.ZuneMusic"] = u15t,
            ["Microsoft.ZuneVideo"] = u16t,
            ["Microsoft.WindowsCamera"] = u17t,
            ["Microsoft.BingNews"] = u18t,
            ["Microsoft.BingWeather"] = u19t,
            ["Microsoft.MicrosoftStickyNotes"] = u20t,
            ["Microsoft.Getstarted"] = u21t,
            ["Microsoft.WindowsSoundRecorder"] = u22t,
            ["Microsoft.WindowsStore"] = u23t,
            ["Microsoft.XboxApp"] = u24t,
            ["Microsoft.People"] = u25t,
            ["Microsoft.SkypeApp"] = u26t,
            ["Microsoft.WindowsAlarms"] = u27t,
            ["Microsoft.OutlookForWindows"] = u28t,
            ["MSTeams"] = u29t,
            ["Microsoft.Todos"] = u30t,
            ["Microsoft.Copilot"] = u31t,
            ["Microsoft.MicrosoftOfficeHub"] = u31t
        };
        private async Task<HashSet<string>> GetInstalledUWPAppsAsync()
        {
            return await Task.Run(() =>
            {
                var result = new HashSet<string>();

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-Command \"Get-AppxPackage | Select -ExpandProperty Name\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        result.Add(line.Trim());
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        System.Windows.MessageBox.Show($"PowerShell error: {ex.Message}")
                    );
                }

                return result;
            });
        }

        private void FadeIn(UIElement element, double durationSeconds)
        {
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(durationSeconds),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            element.BeginAnimation(OpacityProperty, fadeInAnimation);
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

        private async void b_Click(object sender, RoutedEventArgs e)
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var uwp = MainWindow.Localization.LoadLocalization(languageCode, "uwp");

            int count = 0;

            foreach (var toggle in AllToggles)
            {
                if (toggle.IsOn)
                {
                    if (toggle == u8t)
                        count += 3;
                    else if (toggle == u23t)
                    {
                        ILOVEMAKUTWEAKERDialog dialog = new ILOVEMAKUTWEAKERDialog("Microsoft Store");
                        var result = await dialog.ShowAsync();
                        int resulty = await dialog.TaskCompletionSource.Task;
                        if (resulty == 0)
                        {
                            return;
                        }
                        else
                        {
                            count++;
                        }
                    }
                    else if (toggle == u24t)
                    {
                        ILOVEMAKUTWEAKERDialog dialog = new ILOVEMAKUTWEAKERDialog("Xbox");
                        var result = await dialog.ShowAsync();
                        int resulty = await dialog.TaskCompletionSource.Task;
                        if (resulty == 0)
                        {
                            return;
                        }
                        else
                        {
                            count += 5;
                        }
                    }
                    else if (toggle == u25t)
                        count += 2;
                    else if (toggle == u31t)
                        count += 2;
                    else
                        count++;       
                }
            }

            if (!_progressShown)
            {
                ShowProgressSmooth();
            }
            t.Text = $"{uwp["status"]["started"]} 0/{count}";
            mw.NavigationView_Root.IsEnabled = false;
            var apps = 0;
            mw.TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            mw.TaskbarInfo.ProgressValue = 0;
            await Task.Delay(300);
            b.IsEnabled = false;

            var appPackages = new (iNKORE.UI.WPF.Modern.Controls.ToggleSwitch toggle, string packageName)[]
            {
        (u1t, "Microsoft.MixedReality.Portal"),
        (u2t, "Microsoft.MicrosoftSolitaireCollection"),
        (u3t, "Microsoft.Messaging"),
        (u4t, "Microsoft.549981C3F5F10"),
        (u5t, "Microsoft.GetHelp"),
        (u6t, "Microsoft.WindowsFeedbackHub"),
        (u7t, "Microsoft.Windows.DevHome"),
        (u8t, "Microsoft.MSPaint"),
        (u8t, "Microsoft.3DBuilder"),
        (u8t, "Microsoft.Microsoft3DViewer"),
        (u9t, "Microsoft.YourPhone"),
        (u10t, "Microsoft.WindowsMaps"),
        (u11t, "Microsoft.PowerAutomateDesktop"),
        (u12t, "Clipchamp.Clipchamp"),
        (u13t, "microsoft.windowscommunicationsapps"),
        (u14t, "Microsoft.Office.OneNote"),
        (u15t, "Microsoft.ZuneMusic"),
        (u16t, "Microsoft.ZuneVideo"),
        (u17t, "Microsoft.WindowsCamera"),
        (u18t, "Microsoft.BingNews"),
        (u19t, "Microsoft.BingWeather"),
        (u20t, "Microsoft.MicrosoftStickyNotes"),
        (u21t, "Microsoft.Getstarted"),
        (u22t, "Microsoft.WindowsSoundRecorder"),
        (u23t, "Microsoft.WindowsStore"),
        (u24t, "Microsoft.XboxApp"),
        (u24t, "Microsoft.GamingApp"),
        (u24t, "Microsoft.Xbox.TCUI"),
        (u24t, "Microsoft.XboxSpeechToTextOverlay"),
        (u24t, "Microsoft.XboxGameCallableUI"),
        (u25t, "Microsoft.People"),
        (u25t, "Microsoft.WindowsPeopleExperienceHost"),
        (u26t, "Microsoft.SkypeApp"),
        (u27t, "Microsoft.WindowsAlarms"),
        (u28t, "Microsoft.OutlookForWindows"),
        (u29t, "MSTeams"),
        (u30t, "Microsoft.Todos"),
        (u31t, "Microsoft.Copilot"),
        (u31t, "Microsoft.MicrosoftOfficeHub")
            };

            var toRemove = appPackages
                .Where(x => x.toggle.IsOn && x.toggle.Visibility == Visibility.Visible)
                .Select(x => x.packageName)
                .ToList();

            if (toRemove.Count > 0)
            {
                var togglesToRemove = appPackages
                    .Where(x => x.toggle.IsOn && x.toggle.Visibility == Visibility.Visible)
                    .Select(x => x.toggle)
                    .Distinct();

                var script = string.Join("\n", toRemove.Select(pkg =>
                    $"Get-AppxPackage -Name '{pkg}' | Remove-AppxPackage"));

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{script.Replace("\"", "\\\"")}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(psi);
                while (!process.HasExited)
                {
                    await Task.Delay(300);
                    if (apps < count - 1)
                    {
                        apps++;
                        mw.TaskbarInfo.ProgressValue = apps / (double)count;
                        t.Text = $"{uwp["status"]["started"]} {(int)apps}/{count}";
                    }
                }
                apps = count;
                mw.TaskbarInfo.ProgressValue = 1;
            }
            apps = 0;
            SystemSounds.Asterisk.Play();
            mw.NavigationView_Root.IsEnabled = true;
            mw.TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;

            foreach (var toggle in AllToggles)
            {
                toggle.IsOn = false;
            }
            b.IsEnabled = false;

            FadeOut(loadingPanel, 300);
            await Task.Delay(300);
            loadingPanel.Visibility = Visibility.Collapsed;
            ring.IsActive = false;
            _progressShown = false;
            ShowSuccessCheckmark(true);
            await Task.Delay(2000);
            var fadeOut = new DoubleAnimation { From = 1, To = 0, Duration = TimeSpan.FromMilliseconds(400) };
            SuccessOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            await Task.Delay(400);
            SuccessOverlay.Visibility = Visibility.Collapsed;
            SuccessOverlay.Opacity = 1;
            await CheckInstalledUWPAppsAsync(true);
        }

        private void LoadLang()
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var uwp = MainWindow.Localization.LoadLocalization(languageCode, "uwp");
            var quick = MainWindow.Localization.LoadLocalization(languageCode, "quick");
            var main = MainWindow.Localization.LoadLocalization(languageCode, "base");

            label.Text = uwp["main"]["label"];
            info2.Message = uwp["main"]["info1"] + "\n" + uwp["main"]["info2"];

            selectAllE1.Content = quick["main"]["checkall"];
            selectAllE2.Content = quick["main"]["checkall"];

            u3.Header = uwp["main"]["u3"];
            u5.Header = uwp["main"]["u5"];
            u6.Header = uwp["main"]["u6"];
            u9.Header = uwp["main"]["u9"];
            u10.Header = uwp["main"]["u10"];
            u13.Header = uwp["main"]["u13"];
            u15.Header = uwp["main"]["u15"];
            u16.Header = uwp["main"]["u16"];
            u17.Header = uwp["main"]["u17"];
            u18.Header = uwp["main"]["u18"];
            u19.Header = uwp["main"]["u19"];
            u20.Header = uwp["main"]["u20"];
            u22.Header = uwp["main"]["u22"];
            u27.Header = uwp["main"]["u27"];

            e1.Header = uwp["main"]["mode1"];
            e2.Header = uwp["main"]["mode2"];
            e3.Header = uwp["main"]["mode3"];

            b.Content = uwp["main"]["b"];
            t.Text = uwp["main"]["chk"];

            foreach (var toggle in AllToggles)
            {
                toggle.OnContent = main["def"]["on"];
                toggle.OffContent = main["def"]["off"];
            }
        }

        private void SelectAll_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is CheckBox mainToggle)
            {
                bool targetState = mainToggle.IsChecked != true;

                mainToggle.IsChecked = targetState;

                UpdateAllTogglesInCategory(mainToggle, targetState);

                e.Handled = true;
            }
        }

        private void UpdateAllTogglesInCategory(CheckBox mainToggle, bool targetState)
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
                    if (item is SettingsCard card && card.Content is ToggleSwitch childToggle)
                    {
                        if (childToggle.IsEnabled && card.Visibility == Visibility.Visible)
                        {
                            childToggle.IsOn = targetState;
                        }
                    }
                }
            }
        }

        private void AppToggle_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateCategorySelectAllState(e1, selectAllE1);
            UpdateCategorySelectAllState(e2, selectAllE2);

            bool anyOn = AllToggles.Any(t => t.IsOn);
            b.IsEnabled = anyOn;
        }
    }
}
