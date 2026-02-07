using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace XnrgyEngineeringAutomationTools.Views
{
    /// <summary>
    /// Fenetre de chargement au demarrage de l'application
    /// Affiche la progression des verifications Firebase et initialisations
    /// Design moderne XNRGY avec spinner et barre de progression
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        private double _maxWidth;

        public SplashScreenWindow()
        {
            InitializeComponent();
            
            // Charger la version depuis AppInfo (source unique de verite)
            VersionText.Text = AppInfo.Version;
            
            Loaded += (s, e) => _maxWidth = ProgressBar.Parent is Grid grid ? grid.ActualWidth : 480;
        }

        /// <summary>
        /// Met a jour le statut et la progression
        /// </summary>
        /// <param name="message">Message a afficher</param>
        /// <param name="progress">Progression de 0 a 100</param>
        public void UpdateStatus(string message, int progress)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
                ProgressText.Text = $"{progress}%";
                
                // Animer la barre de progression
                double targetWidth = (_maxWidth * progress) / 100;
                
                var animation = new DoubleAnimation
                {
                    To = targetWidth,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                
                ProgressBar.BeginAnimation(WidthProperty, animation);
            });
        }

        /// <summary>
        /// Met a jour le statut sans changer la progression
        /// </summary>
        public void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
            });
        }

        /// <summary>
        /// Affiche un message d'erreur
        /// </summary>
        public void ShowError(string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 100, 100));
                ProgressBar.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 100, 100));
            });
        }

        /// <summary>
        /// Termine le chargement avec succes
        /// </summary>
        public void Complete()
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = "Demarrage...";
                ProgressText.Text = "100%";
                ProgressBar.Width = _maxWidth;
                ProgressBar.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0, 210, 106)); // Vert succes
            });
        }

        /// <summary>
        /// Ferme le splash screen avec animation
        /// </summary>
        public async Task CloseWithAnimationAsync()
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                
                BeginAnimation(OpacityProperty, fadeOut);
                await Task.Delay(300);
                Close();
            });
        }
    }
}
