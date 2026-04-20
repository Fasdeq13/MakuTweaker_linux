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
using MicaWPF.Controls;

namespace MakuTweakerNew
{
    public partial class ExclusionWindow : MicaWindow
    {
        public ExclusionWindow()
        {
            InitializeComponent();
            LoadLang();
            ExclusionsTxt.Text = Properties.Settings.Default.ProcessExclusions;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ProcessExclusions = ExclusionsTxt.Text.Replace(" ", "").ToLower();
            Properties.Settings.Default.Save();
            this.Close();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ExclusionsTxt.Text = string.Empty;
        }

        public void LoadLang()
        {
            try
            {
                var languageCode = Properties.Settings.Default.lang ?? "en";
                var pmgr = MainWindow.Localization.LoadLocalization(languageCode, "pmgr");

                var main = pmgr["main"];

                this.Title = main["excltitle"];
                InfoText.Text = main["exclinfo"];
                ClearBtn.Content = main["clear"];
                SaveBtn.Content = main["save"];
            }
            catch (Exception ex)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(ex.Message, "MakuTweaker Error", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }
    }
}
