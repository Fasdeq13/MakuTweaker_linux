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
using System.ServiceProcess;
using System;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Windows.UI.Composition.Desktop;

namespace MakuTweakerNew
{
    public partial class Advanced : Page
    {
        bool isLoaded = false;
        MainWindow mw = (MainWindow)System.Windows.Application.Current.MainWindow;
        private void OpenUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

        public Advanced()
        {
            InitializeComponent();
            checkReg();
            LoadLang();
            isLoaded = true;
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

        private void LoadLang()
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var adv = MainWindow.Localization.LoadLocalization(languageCode, "adv");
            var main = MainWindow.Localization.LoadLocalization(languageCode, "base");
            var compon = MainWindow.Localization.LoadLocalization(languageCode, "compon");
            var tooltips = MainWindow.Localization.LoadLocalization(languageCode, "tooltips");

            label.Text = main["catname"]["adv"];

            edgelabel.Text = adv["main"]["deledge_title"];
            edgeBtn.Content = adv["main"]["deledge_btn"];

            disindex.Header = adv["main"]["index_title"];
            oldbootloader.Header = adv["main"]["oldbootloader"];
            advancedboot.Header = adv["main"]["advancedboot"];
            swap.Header = adv["main"]["swap"];
            vbs.Header = adv["main"]["vbs"];
            ttl.Header = adv["main"]["ttl"];

            sys_tooltip_vbs.Content = tooltips["main"]["coreisol"];
            sys_tooltip_swap.Content = tooltips["main"]["swap"];
            sys_tooltip_oldbootloader.Content = tooltips["main"]["oldloader"];
            sys_tooltip_advancedboot.Content = tooltips["main"]["additional"];
            sys_tooltip_ttl.Content = tooltips["main"]["ttl"];
            edge_tooltip.Content = adv["main"]["deledge_tooltip"];
            index_tooltip.Content = adv["main"]["index_tooltip"];

            foreach (var toggle in AllToggles)
            {
                toggle.OnContent = main["def"]["on"];
                toggle.OffContent = main["def"]["off"];
            }
        }

        private List<ModernWpf.Controls.ToggleSwitch> AllToggles => new()
        {
            oldbootloader,
            advancedboot,
            swap,
            vbs,
            ttl,
            disindex
        };
        private void checkReg()
        {
            vbs.IsOn = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard")?.GetValue("EnableVirtualizationBasedSecurity")?.Equals(0) ?? false;
            ttl.IsOn = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters")?.GetValue("DefaultTTL")?.Equals(65) ?? false;
            string bcdCurrent = GetCmdOutput("bcdedit", "/enum {current}");
            oldbootloader.IsOn = bcdCurrent.Contains("bootmenupolicy") && bcdCurrent.Contains("legacy");
            string bcdGlobal = GetCmdOutput("bcdedit", "/enum {globalsettings}");
            advancedboot.IsOn = Regex.IsMatch(bcdGlobal, @"advancedoptions\s+yes", RegexOptions.IgnoreCase);
            disindex.IsOn = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WSearch")?.GetValue("Start")?.Equals(4) ?? false;
            var pagingFiles = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management")?.GetValue("PagingFiles") as string[];
            swap.IsOn = pagingFiles == null || pagingFiles.Length == 0 || (pagingFiles.Length == 1 && string.IsNullOrWhiteSpace(pagingFiles[0]));
        }

        private void ttl_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                try
                {
                    using (var keyIPv4 = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"))
                    using (var keyIPv6 = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\TCPIP6\Parameters"))
                    {
                        if (ttl.IsOn)
                        {
                            keyIPv4?.SetValue("DefaultTTL", 65, Microsoft.Win32.RegistryValueKind.DWord);
                            keyIPv6?.SetValue("DefaultTTL", 65, Microsoft.Win32.RegistryValueKind.DWord);
                        }
                        else
                        {
                            keyIPv4?.DeleteValue("DefaultTTL", false);
                            keyIPv6?.DeleteValue("DefaultTTL", false);
                        }
                    }
                }
                catch { }
            }
        }

        private void vbs_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (vbs.IsOn)
                {
                    case true:
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord);
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("RequirePlatformSecurityFeatures", 0, RegistryValueKind.DWord);
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa").SetValue("LsaCfgFlags", 0, RegistryValueKind.DWord);
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity").SetValue("Enabled", 0, RegistryValueKind.DWord);
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("RequireMicrosoftSignedBootChain", 0, RegistryValueKind.DWord);
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("KernelDMAProtection", 0, RegistryValueKind.DWord);
                        Process.Start("cmd.exe", "/c bcdedit /set hypervisorlaunchtype off");
                        break;
                    case false:
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("EnableVirtualizationBasedSecurity", 1, RegistryValueKind.DWord);
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("RequirePlatformSecurityFeatures", 3, RegistryValueKind.DWord);
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa").SetValue("LsaCfgFlags", 1, RegistryValueKind.DWord);
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity").SetValue("Enabled", 1, RegistryValueKind.DWord);
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("RequireMicrosoftSignedBootChain", 1, RegistryValueKind.DWord);
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard").SetValue("KernelDMAProtection", 1, RegistryValueKind.DWord);
                        Process.Start("cmd.exe", "/c bcdedit /set hypervisorlaunchtype auto");
                        break;
                }
                mw.RebootNotify(1);
            }
        }

        private void oldbootloader_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (oldbootloader.IsOn)
                {
                    case true:
                        Process.Start("cmd.exe", "/c \"bcdedit /set \"{current}\" bootmenupolicy legacy\"");
                        break;
                    case false:
                        Process.Start("cmd.exe", "/c \"bcdedit /set \"{current}\" bootmenupolicy standard\"");
                        break;
                }
            }
        }

        private void advancedboot_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (advancedboot.IsOn)
                {
                    case true:
                        Process.Start("cmd.exe", "/c \"bcdedit /set \"{globalsettings}\" advancedoptions true\"");
                        break;
                    case false:
                        Process.Start("cmd.exe", "/c \"bcdedit /set \"{globalsettings}\" advancedoptions false\"");
                        break;
                }
            }
        }
        private void swap_Toggled(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                switch (swap.IsOn)
                {
                    case true:
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management").SetValue("PagingFiles", new string[] { }, RegistryValueKind.MultiString);
                        break;
                    case false:
                        Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management").SetValue("PagingFiles", new string[] { @"?:\pagefile.sys" }, RegistryValueKind.MultiString);
                        break;
                }
                mw.RebootNotify(1);
            }
        }

        private async void edgeBtn_Click(object sender, RoutedEventArgs e)
        {
            var languageCode = Settings.Default.lang ?? "en";
            var adv = MainWindow.Localization.LoadLocalization(languageCode, "adv");
            var b = MainWindow.Localization.LoadLocalization(languageCode, "base");

            string title = adv["main"]["deledge_title"];

            var resultBefore = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                adv["main"]["deledge_before"],
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultBefore == MessageBoxResult.Yes)
            {
                OpenUrl("https://www.google.com/chrome/");
                return;
            }

            var resultSure = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                adv["main"]["deledge_sure"],
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultSure != MessageBoxResult.Yes) return;
            try
            {
                string[] processes = { "msedge", "msedgewebview2", "MicrosoftEdgeUpdate" };
                foreach (var name in processes)
                {
                    foreach (var p in Process.GetProcessesByName(name))
                    {
                        try { p.Kill(); } catch { }
                    }
                }

                await Task.Run(() => {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c sc stop edgeupdate & sc delete edgeupdate & sc stop edgeupdatem & sc delete edgeupdatem",
                            Verb = "runas",
                            CreateNoWindow = true,
                            UseShellExecute = true
                        })?.WaitForExit();

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c schtasks /delete /tn \"MicrosoftEdgeUpdateTaskMachineCore\" /f & schtasks /delete /tn \"MicrosoftEdgeUpdateTaskMachineUA\" /f",
                            Verb = "runas",
                            CreateNoWindow = true,
                            UseShellExecute = true
                        })?.WaitForExit();
                    }
                    catch { }
                });

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\EdgeUpdate"))
                {
                    key?.SetValue("DoNotUpdateToEdgeWithChromium", 1, RegistryValueKind.DWord);
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\EdgeUpdate"))
                {
                    key?.SetValue("InstallDefault", 0, RegistryValueKind.DWord);
                    key?.SetValue("UpdateDefault", 0, RegistryValueKind.DWord);
                }

                string[] basePaths = { @"C:\Program Files (x86)\Microsoft\Edge\Application", @"C:\Program Files\Microsoft\Edge\Application" };
                foreach (var edgePath in basePaths)
                {
                    if (!Directory.Exists(edgePath)) continue;
                    var version = Directory.GetDirectories(edgePath).Select(Path.GetFileName).OrderByDescending(v => v).FirstOrDefault();
                    if (version == null) continue;

                    string setupPath = Path.Combine(edgePath, version, "Installer", "setup.exe");
                    if (File.Exists(setupPath))
                    {
                        await Task.Run(() => {
                            var proc = Process.Start(new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c \"{setupPath}\" --uninstall --system-level --force-uninstall --verbose-logging --msedge",
                                Verb = "runas",
                                CreateNoWindow = true,
                                UseShellExecute = true
                            });
                            proc?.WaitForExit();
                        });
                    }
                }

                await Task.Delay(3000);

                await Task.Run(() => {
                    string[] allPaths = {
                @"C:\Program Files (x86)\Microsoft\Edge", @"C:\Program Files (x86)\Microsoft\EdgeCore", @"C:\Program Files (x86)\Microsoft\EdgeUpdate",
                @"C:\Program Files\Microsoft\Edge", @"C:\Program Files\Microsoft\EdgeCore", @"C:\Program Files\Microsoft\EdgeUpdate"
            };

                    foreach (var path in allPaths)
                    {
                        try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
                    }

                    string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string[] userPaths = { Path.Combine(local, "Microsoft\\Edge"), Path.Combine(local, "Microsoft\\EdgeUpdate"), Path.Combine(roaming, "Microsoft\\Edge") };

                    foreach (var path in userPaths)
                    {
                        try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
                    }
                });

                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(adv["main"]["deledge_done"], "MakuTweaker", MessageBoxButton.OK, MessageBoxImage.Information);
                mw.RebootNotify(1);
            }
            catch (Exception)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(adv["main"]["deledge_error"], "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void disindex_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            try
            {
                Registry.LocalMachine.CreateSubKey(
                    @"SYSTEM\CurrentControlSet\Services\WSearch")
                    .SetValue("Start", disindex.IsOn ? 4 : 2, RegistryValueKind.DWord);

                if (disindex.IsOn)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c sc stop WSearch",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c sc start WSearch",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }

                mw.RebootNotify(1);
            }
            catch
            {
            }
        }
    }
}
