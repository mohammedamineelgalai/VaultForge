using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using XnrgyEngineeringAutomationTools.Services;
using XnrgyEngineeringAutomationTools.Shared.Views;

namespace XnrgyEngineeringAutomationTools.Modules.SmartTools.Views
{
    /// <summary>
    /// Fenêtre de progression HTML dynamique pour Smart Save et Safe Close
    /// Affiche les étapes en temps réel avec mise à jour automatique
    /// Utilise WebView2 pour un rendu HTML moderne
    /// By Mohammed Amine Elgalai - XNRGY Climate Systems ULC
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private bool _isInitialized = false;
        private string _pendingHtml = string.Empty;
        private bool _isClosing = false;

        public ProgressWindow()
        {
            InitializeComponent();
            Loaded += ProgressWindow_Loaded;
            
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

        /// <summary>
        /// Gestionnaire du bouton fermer
        /// </summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }

        /// <summary>
        /// Constructeur avec titre et contenu HTML initial
        /// </summary>
        public ProgressWindow(string title, string htmlContent) : this()
        {
            TxtTitle.Text = title;
            Title = title;
            _pendingHtml = htmlContent;
        }

        private async void ProgressWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Creer un UserDataFolder dedie pour eviter l'erreur 0x800700AA
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "XnrgyEngineeringAutomationTools", "WebView2Cache", "SmartTools");
                
                Directory.CreateDirectory(userDataFolder);
                
                // Creer l'environnement WebView2 avec le UserDataFolder dedie
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, null);
                
                // Initialiser WebView2 avec cet environnement
                await WebViewContent.EnsureCoreWebView2Async(env);
                _isInitialized = true;

                // Injecter le script pour permettre window.external.CloseForm()
                // On utilise PostWebMessageAsJson pour la communication
                WebViewContent.CoreWebView2.WebMessageReceived += (sender, args) =>
                {
                    try
                    {
                        string message = args.TryGetWebMessageAsString();
                        if (message == "CLOSE_FORM")
                        {
                            Dispatcher.Invoke(() => CloseWindow());
                        }
                    }
                    catch { }
                };

                // Si du HTML est en attente, l'afficher
                if (!string.IsNullOrEmpty(_pendingHtml))
                {
                    WebViewContent.NavigateToString(_pendingHtml);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[SmartTools] [!] WebView2 non disponible: {ex.Message}", Logger.LogLevel.WARNING);
                XnrgyMessageBox.ShowWarning(
                    "WebView2 n'est pas disponible. Veuillez installer le runtime Microsoft Edge WebView2.",
                    "Erreur d'affichage");
                Close();
            }
        }

        /// <summary>
        /// Affiche du contenu HTML dans la fenêtre
        /// </summary>
        public void SetHtmlContent(string htmlContent)
        {
            if (_isInitialized)
            {
                WebViewContent.NavigateToString(htmlContent);
            }
            else
            {
                _pendingHtml = htmlContent;
            }
        }

        /// <summary>
        /// Définit le titre de la fenêtre
        /// </summary>
        public void SetTitle(string title)
        {
            TxtTitle.Text = title;
            Title = title;
        }

        /// <summary>
        /// Met à jour le statut d'une étape dans le HTML
        /// </summary>
        /// <param name="stepId">ID de l'étape (ex: "step1", "step2")</param>
        /// <param name="content">Nouveau contenu de l'étape</param>
        /// <param name="statusClass">Classe CSS (completed, error, info)</param>
        public async Task UpdateStepStatusAsync(string stepId, string content, string statusClass)
        {
            if (!_isInitialized || WebViewContent.CoreWebView2 == null)
                return;

            try
            {
                // Échapper les caractères spéciaux pour JavaScript
                string escapedContent = content.Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", "\\n");
                
                // Script JavaScript pour mettre à jour l'étape
                string script = $@"
                    (function() {{
                        var el = document.getElementById('{stepId}');
                        if (el) {{
                            el.innerHTML = '<span class=""emoji"">' + 
                                ('{statusClass}' === 'completed' ? '✅' : 
                                 '{statusClass}' === 'error' ? '❌' : 
                                 '{statusClass}' === 'info' ? 'ℹ️' : '⏳') + 
                                '</span> {escapedContent}';
                            el.className = '{statusClass}';
                        }}
                    }})();
                ";

                await WebViewContent.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                Logger.Log($"[SmartTools] [UpdateStepStatus] Erreur: {ex.Message}", Logger.LogLevel.DEBUG);
            }
        }

        /// <summary>
        /// Affiche le message de complétion
        /// </summary>
        public async Task ShowCompletionAsync(string message)
        {
            if (!_isInitialized || WebViewContent.CoreWebView2 == null)
                return;

            try
            {
                string escapedMessage = message.Replace("'", "\\'").Replace("\"", "\\\"");
                string script = $@"
                    (function() {{
                        var el = document.getElementById('completion');
                        if (el) {{
                            el.innerHTML = '{escapedMessage}';
                            el.style.display = 'block';
                        }}
                    }})();
                ";

                await WebViewContent.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                Logger.Log($"[SmartTools][ShowCompletion] Erreur: {ex.Message}", Logger.LogLevel.ERROR);
            }
        }

        /// <summary>
        /// Ferme la fenêtre (peut être appelée depuis JavaScript)
        /// </summary>
        public void CloseWindow()
        {
            if (!_isClosing)
            {
                _isClosing = true;
                Dispatcher.Invoke(() => Close());
            }
        }

        /// <summary>
        /// Affiche une fenêtre de progression avec HTML dynamique
        /// </summary>
        public static ProgressWindow ShowProgress(string title, string htmlContent, Window? owner = null)
        {
            var window = new ProgressWindow(title, htmlContent);
            if (owner != null)
            {
                window.Owner = owner;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            window.Show();
            return window;
        }

        /// <summary>
        /// Affiche une fenêtre de progression modale avec HTML dynamique
        /// </summary>
        public static ProgressWindow ShowProgressDialog(string title, string htmlContent, Window? owner = null)
        {
            var window = new ProgressWindow(title, htmlContent);
            if (owner != null)
            {
                window.Owner = owner;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            window.ShowDialog();
            return window;
        }

        protected override void OnClosed(EventArgs e)
        {
            _isClosing = true;
            base.OnClosed(e);
        }
    }

    /// <summary>
    /// Interface pour mettre à jour une fenêtre de progression
    /// </summary>
    public interface IProgressWindow
    {
        Task UpdateStepStatusAsync(string stepId, string content, string statusClass);
        Task ShowCompletionAsync(string message);
        void CloseWindow();
    }

    /// <summary>
    /// Implémentation de IProgressWindow pour ProgressWindow
    /// </summary>
    public class ProgressWindowWrapper : IProgressWindow
    {
        private readonly ProgressWindow _window;

        public ProgressWindowWrapper(ProgressWindow window)
        {
            _window = window;
        }

        public Task UpdateStepStatusAsync(string stepId, string content, string statusClass)
        {
            return _window.UpdateStepStatusAsync(stepId, content, statusClass);
        }

        public Task ShowCompletionAsync(string message)
        {
            return _window.ShowCompletionAsync(message);
        }

        public void CloseWindow()
        {
            _window.CloseWindow();
        }
    }
}

