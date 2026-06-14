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
using System.Windows.Shapes;

namespace MakuTweakerNew
{
    public partial class MakuYan : MicaWindow
    {
        MainWindow mw = (MainWindow)Application.Current.MainWindow;
        public MakuYan()
        {
            InitializeComponent();
            LoadLang();
            ProcBlockTxt.Text = Properties.Settings.Default.MakuYanPar;
        }

        private void ApplyDisallowRun(string input)
        {
            var baseKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";

            using (var baseKey = Registry.CurrentUser.CreateSubKey(baseKeyPath))
            {
                if (baseKey.GetValue("DisallowRun") == null)
                {
                    mw.RebootNotify(1);
                }

                baseKey.SetValue("DisallowRun", 1, RegistryValueKind.DWord);

                using (var disallowKey = baseKey.CreateSubKey("DisallowRun"))
                {
                    foreach (var name in disallowKey.GetValueNames())
                    {
                        disallowKey.DeleteValue(name);
                    }

                    var processes = input
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim().ToLower())
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Distinct()
                        .ToList();

                    for (int i = 0; i < processes.Count; i++)
                    {
                        disallowKey.SetValue((i + 1).ToString(), processes[i]);
                    }
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var text = ProcBlockTxt.Text.Replace(" ", "").ToLower();
            var languageCode = Properties.Settings.Default.lang ?? "en";
            var myan = MainWindow.Localization.LoadLocalization(languageCode, "myan");
            var main = myan["main"];
            var forbidden = new[] { "makutweaker", "makutyper", "makufinder", "makutweakerportable",
                "dwm", "msedgewebview2", "startmenuexperiencehost", "taskmgr", "explorer", "system", "idle", "dllhost", "smss", "csrss", "wininit", "services", "lsass", "winlogon", "svchost", "fontdrvhost", "sihost", "shellexperiencehost", "ctfmon", "runtimebroker", "searchindexer", "msbuild", "crossdeviceservice", "bioenrollmenthost", "acergaicameraw", "vmtoolsd",
                "searchapp", "wpfsurface", "searchhost", "phoneexperiencehost", "textinputhost", "nvidia overlay", "vscodium", "lockapp", "shellhost", "systemsettings", "crossdeviceresume", "applicationframehost", "searchui", "gamebar", "xboxgamebarwidgets", "xboxpcappft", "icloudservices", "nvdisplay.container", "widgets", "xboxgamebarspotify", "backgroundtaskhost", "perfwatson2",
                "onedrive", "onedrive.sync.service", "igcctray", "igcc", "microsoft.cmdpal.ui", "wwahost", "onedrive.setup", "rtkuwp", "msedge", "nvcontainer", "snippingtool", "softlandingtask", "unsecapp", "gameinputredistservice", "accuserps", "useroobebroker", "smartscreen", "nvsphelper64", "acerhardwareservice", "rtkauduservice64", "acersyshardwareservice", "openrgb", "widgetservice",
                "applemobiledeviceprocess", "aqauserps", "windowspackagemanagerserver", "dataexchangehost", "inputpersonalization", "bootcamp", "settingsynchost", "igfxtray", "igfxhk", "securityhealthsystray","filecoauth", "storedesktopextension", "vm3dservice", "rundll32", "searchprotocolhost", "backgroundtransferhost", "systemsettingsadminflows",
                "regedit", "cmd", "powershell", "pwsh", "conhost", "mmc", "rstrui", "control", "taskhostw", "userinit", "logonui", "wmiprvse", "spoolsv", "audiodg", "wudfhost", "werfault", "msmpeng", "nissrv", "securityhealthservice", "radeonsoftware", "amdrsserv", "cncmd", "wavessyssvc64", "maxxaudio", "xgamehelper", "comppkgsrv", "onedrivestandaloneupdater", "gamebarftserver", "appactions", "systemsettingsbroker"};

            if (forbidden.Any(f => text.Contains(f)))
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(main["makutnah"], "MakuTweaker", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            Properties.Settings.Default.MakuYanPar = text;
            Properties.Settings.Default.Save();

            ApplyDisallowRun(text);

            this.Close();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ProcBlockTxt.Text = string.Empty;
        }

        public void LoadLang()
        {
            try
            {
                var languageCode = Properties.Settings.Default.lang ?? "en";
                var myan = MainWindow.Localization.LoadLocalization(languageCode, "myan");
                var main = myan["main"];
                this.Title = main["excltitle"];
                InfoText.Text = main["info"];
                ClearBtn.Content = main["clear"];
                SaveBtn.Content = main["applyban"];
                banyandex.Content = main["banyandex"];
                howitworks.Content = main["howitworks"];

                var allowedLangs = new[] { "ru", "uk", "kk", "lv", "et", "be", "az" };

                banyandex.Visibility = allowedLangs.Contains(languageCode)
                    ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(ex.Message, "MakuTweaker Error", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }

        private void HowItWorks_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://youtu.be/97CMTnJL9p0?si=JTmxlPkqhiVkGNOD&t=398";

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void BlockYandex_Click(object sender, RoutedEventArgs e)
        {
            ProcBlockTxt.Text = "yandex.exe, browser.exe, yandex_music.exe, YandexWorking.exe, yndxstp.exe";
        }
    }
}
