using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using XnrgyEngineeringAutomationTools.Services;

namespace XnrgyEngineeringAutomationTools.Views
{
    /// <summary>
    /// Fenetre de telechargement et installation de mise a jour
    /// </summary>
    public partial class UpdateDownloadWindow : Window
    {
        private readonly string _downloadUrl;
        private readonly string _currentVersion;
        private readonly string _newVersion;
        private readonly bool _isForced;
        private CancellationTokenSource _cancellationToken;
        private bool _downloadStarted;

        public bool UpdateSuccessful { get; private set; }

        public UpdateDownloadWindow(string downloadUrl, string currentVersion, string newVersion, bool isForced)
        {
            InitializeComponent();
            
            _downloadUrl = downloadUrl;
            _currentVersion = currentVersion;
            _newVersion = newVersion;
            _isForced = isForced;
            _cancellationToken = new CancellationTokenSource();

            // Configurer l'affichage
            VersionText.Text = $"Version {currentVersion} → {newVersion}";
            
            if (isForced)
            {
                TitleText.Text = "⚠️ Mise a jour obligatoire";
                TitleText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 180, 100));
            }

            // S'abonner aux evenements
            if (AutoUpdateService.Instance != null)
            {
                AutoUpdateService.Instance.DownloadProgressChanged += OnDownloadProgress;
                AutoUpdateService.Instance.UpdateCompleted += OnUpdateCompleted;
            }

            // Demarrer le telechargement automatiquement
            Loaded += async (s, e) => await StartDownloadAsync();
        }

        /// <summary>
        /// Demarre le telechargement
        /// </summary>
        private async Task StartDownloadAsync()
        {
            try
            {
                _downloadStarted = true;
                CancelButton.Visibility = Visibility.Collapsed;
                
                StatusText.Text = "🔗 Connexion au serveur...";
                
                // Petit delai pour l'affichage
                await Task.Delay(500);

                if (AutoUpdateService.Instance != null)
                {
                    UpdateSuccessful = await AutoUpdateService.Instance.DownloadAndInstallUpdateAsync(_downloadUrl, _newVersion);
                }
                else
                {
                    // Creer une instance temporaire pour telecharger
                    StatusText.Text = "Telechargement direct...";
                    using (var tempService = new AutoUpdateService())
                    {
                        tempService.DownloadProgressChanged += OnDownloadProgress;
                        tempService.UpdateCompleted += OnUpdateCompleted;
                        UpdateSuccessful = await tempService.DownloadAndInstallUpdateAsync(_downloadUrl, _newVersion);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Erreur: {ex.Message}";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 100, 100));
                
                CancelButton.Content = "Fermer";
                CancelButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Mise a jour de la progression
        /// </summary>
        private void OnDownloadProgress(object sender, UpdateProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = e.ProgressPercent;
                PercentText.Text = $"{e.ProgressPercent}%";
                
                // Afficher les tailles
                string downloaded = FormatBytes(e.BytesDownloaded);
                string total = FormatBytes(e.TotalBytes);
                DownloadInfoText.Text = $"📥 {downloaded} / {total}";

                // Changer le statut selon la progression
                if (e.ProgressPercent < 100)
                {
                    TitleText.Text = "📥 Telechargement en cours";
                    StatusText.Text = "Ne fermez pas cette fenetre pendant la mise a jour.";
                }
                else
                {
                    TitleText.Text = "⚙️ Installation en cours";
                    StatusText.Text = "Installation silencieuse... L'application va redemarrer automatiquement.";
                    DownloadInfoText.Text = "Installation silencieuse en cours...";
                }
            });
        }

        /// <summary>
        /// Mise a jour terminee
        /// </summary>
        private void OnUpdateCompleted(object sender, UpdateCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Success)
                {
                    ProgressBar.Value = 100;
                    PercentText.Text = "✓";
                    DownloadInfoText.Text = "Redemarrage en cours...";
                    StatusText.Text = "L'application va redemarrer automatiquement.";
                    TitleText.Text = "✅ Mise a jour terminee";
                    TitleText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(100, 255, 100));
                    
                    UpdateSuccessful = true;
                }
                else
                {
                    TitleText.Text = "❌ Echec de la mise a jour";
                    TitleText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(255, 100, 100));
                    StatusText.Text = e.ErrorMessage ?? "Une erreur est survenue.";
                    StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(255, 100, 100));
                    
                    CancelButton.Content = "Fermer";
                    CancelButton.Visibility = Visibility.Visible;
                    
                    UpdateSuccessful = false;
                }
            });
        }

        /// <summary>
        /// Formate les bytes en unite lisible
        /// </summary>
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Annuler/Fermer
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationToken?.Cancel();
            Close();
        }

        /// <summary>
        /// Nettoyage
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            if (AutoUpdateService.Instance != null)
            {
                AutoUpdateService.Instance.DownloadProgressChanged -= OnDownloadProgress;
                AutoUpdateService.Instance.UpdateCompleted -= OnUpdateCompleted;
            }
            
            _cancellationToken?.Dispose();
        }

        /// <summary>
        /// Affiche la fenetre de telechargement et lance la mise a jour
        /// </summary>
        public static bool ShowAndDownload(string downloadUrl, string currentVersion, string newVersion, bool isForced)
        {
            var window = new UpdateDownloadWindow(downloadUrl, currentVersion, newVersion, isForced);
            window.ShowDialog();
            return window.UpdateSuccessful;
        }
    }
}
