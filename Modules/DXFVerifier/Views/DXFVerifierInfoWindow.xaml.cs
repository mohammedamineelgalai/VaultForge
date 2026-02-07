// ============================================================================
// DXFVerifierInfoWindow.xaml.cs
// DXF Verifier Info Window - Modern Info Dialog
// Author: Mohammed Amine Elgalai - XNRGY Climate Systems ULC
// ============================================================================

using System.Windows;
using System.Windows.Media;

namespace XnrgyEngineeringAutomationTools.Modules.DXFVerifier.Views
{
    /// <summary>
    /// Fenetre d'information moderne pour DXF Verifier
    /// Design unifie avec SmartToolsInfoWindow
    /// </summary>
    public partial class DXFVerifierInfoWindow : Window
    {
        public DXFVerifierInfoWindow()
        {
            InitializeComponent();
            
            // S'abonner aux changements de theme
            MainWindow.ThemeChanged += OnThemeChanged;
            this.Closed += (s, e) => MainWindow.ThemeChanged -= OnThemeChanged;
            ApplyTheme(MainWindow.CurrentThemeIsDark);
        }

        private void OnThemeChanged(bool isDarkTheme)
        {
            Dispatcher.Invoke(() => ApplyTheme(isDarkTheme));
        }

        private void ApplyTheme(bool isDarkTheme)
        {
            this.Background = new SolidColorBrush(isDarkTheme 
                ? Color.FromRgb(30, 30, 46)    // #1E1E2E
                : Color.FromRgb(245, 247, 250)); // #F5F7FA
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
