using MicaWPF.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Windows.Devices.Geolocation;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MakuTweakerNew
{
    public partial class ExclusionWindow : MicaWindow
    {
        private const string IfeoPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\taskmgr.exe";
        public ExclusionWindow()
        {
            InitializeComponent();
            LoadLang();
            ExclusionsTxt.Text = Properties.Settings.Default.ProcessExclusions;
            OnlyMbCheck.IsChecked = Properties.Settings.Default.onlyMB_processMGR;
            int mode = Properties.Settings.Default.ProcessNameDisplayMode;
            if (mode == 0) RadioNameFriendly.IsChecked = true;
            else if (mode == 1) RadioNameSystem.IsChecked = true;
            else RadioNameBoth.IsChecked = true;
            ReplaceTaskmgrCheck.IsChecked = CheckIfTaskmgrIsReplaced();
        }

        private bool CheckIfTaskmgrIsReplaced()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(IfeoPath))
                {
                    return key?.GetValue("Debugger") != null;
                }
            }
            catch { return false; }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ProcessExclusions = ExclusionsTxt.Text.Replace(" ", "").ToLower();
            Properties.Settings.Default.onlyMB_processMGR = OnlyMbCheck.IsChecked ?? false;
            if (RadioNameFriendly.IsChecked == true) Properties.Settings.Default.ProcessNameDisplayMode = 0;
            else if (RadioNameSystem.IsChecked == true) Properties.Settings.Default.ProcessNameDisplayMode = 1;
            else Properties.Settings.Default.ProcessNameDisplayMode = 2;
            Properties.Settings.Default.Save();

            bool shouldReplace = ReplaceTaskmgrCheck.IsChecked ?? false;
            ApplyRegistryChanges(shouldReplace);

            this.Close();
        }



        private void ApplyRegistryChanges(bool replace)
        {
            try
            {
                string currentExePath = Assembly.GetExecutingAssembly().Location;
                if (currentExePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    currentExePath = System.IO.Path.ChangeExtension(currentExePath, ".exe");
                }
                string currentDir = System.IO.Path.GetDirectoryName(currentExePath);

                if (replace)
                {
                    using (RegistryKey key = Registry.LocalMachine.CreateSubKey(IfeoPath, true))
                    {
                        key.SetValue("Debugger", $"\"{currentExePath}\"");
                    }
                }
                else
                {
                    Registry.LocalMachine.DeleteSubKeyTree(IfeoPath, false);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (Exception ex)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(ex.Message, "MakuTweaker Registry Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadLang()
        {
            try
            {
                var languageCode = Properties.Settings.Default.lang ?? "en";
                var pmgr = MainWindow.Localization.LoadLocalization(languageCode, "pmgr");

                var main = pmgr["main"];

                this.Title = main["settings"];
                InfoText.Text = main["exclinfo"];
                SaveBtn.Content = main["save"];
                OnlyMbCheck.Content = main["onlymb"];
                ProcessNameStyleLabel.Text = main["viewtype"];
                RadioNameFriendly.Content = main["friendly"] + " (Microsoft Edge)";
                RadioNameSystem.Content = main["systematic"] + " (msedge.exe)";
                RadioNameBoth.Content = main["bothvar"];
                ReplaceTaskmgrCheck.Content = main["taskmgr"];
            }
            catch (Exception ex)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(ex.Message, "MakuTweaker Error", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }
    }
}
