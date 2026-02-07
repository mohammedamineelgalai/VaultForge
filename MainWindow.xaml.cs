using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using XnrgyEngineeringAutomationTools.Services;
using XnrgyEngineeringAutomationTools.Shared.Views;
using XnrgyEngineeringAutomationTools.Modules.CreateModule.Views;
using XnrgyEngineeringAutomationTools.Modules.UploadTemplate.Views;
using XnrgyEngineeringAutomationTools.Modules.SmartTools.Views;
using XnrgyEngineeringAutomationTools.Modules.UpdateWorkspace.Views;
using XnrgyEngineeringAutomationTools.Modules.DXFVerifier.Views;
using XnrgyEngineeringAutomationTools.Modules.ConfigUnite.Views;

namespace XnrgyEngineeringAutomationTools
{
    public partial class MainWindow : Window
    {
        private readonly VaultSdkService _vaultService;
        private readonly InventorService _inventorService;
        private bool _isVaultConnected = false;
        private bool _isInventorConnected = false;
        private bool _isDarkTheme = true;
        private System.Windows.Threading.DispatcherTimer? _inventorReconnectTimer;

        // === VERSIONS REQUISES ===
        private const string REQUIRED_INVENTOR_VERSION = "2026";  // Inventor Professional 2026.2
        private const string REQUIRED_VAULT_VERSION = "2026";     // Vault Professional 2026.2

        // === CHEMINS DES APPLICATIONS ===
        private readonly string _basePath = @"c:\Users\mohammedamine.elgala\source\repos";
        private string VaultUploadExePath => Path.Combine(_basePath, @"VaultAutomationTool\bin\Release\VaultAutomationTool.exe");
        
        // Checklist HVAC : Chemin dans le projet (migré)
        private string ChecklistHVACPath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Modules", "ChecklistHVAC", "Resources", "ChecklistHVAC.html"
        );
        
        // Fallback vers l'ancien emplacement si le nouveau n'existe pas
        private string ChecklistHVACPathFallback => Path.Combine(_basePath, @"ChecklistHVAC\Checklist HVACAHU - By Mohammed Amine Elgalai.html");

        private readonly string[] _workspaceFolders = new[]
        {
            "$/Content Center Files",
            "$/Engineering/Inventor_Standards",
            "$/Engineering/Library/Cabinet",
            "$/Engineering/Library/Xnrgy_M99",
            "$/Engineering/Library/Xnrgy_Module"
        };

        private readonly SolidColorBrush _greenBrush = new SolidColorBrush(Color.FromRgb(16, 124, 16));
        private readonly SolidColorBrush _redBrush = new SolidColorBrush(Color.FromRgb(232, 17, 35));

        // === THEME GLOBAL (accessible par tous les sous-formulaires) ===
        public static bool CurrentThemeIsDark { get; private set; } = true;
        public static event Action<bool>? ThemeChanged;

        public MainWindow()
        {
            InitializeComponent();
            Logger.Initialize();
            _vaultService = new VaultSdkService();
            _inventorService = new InventorService();
            
            // Charger le theme sauvegarde
            _isDarkTheme = UserPreferencesManager.LoadTheme();
            CurrentThemeIsDark = _isDarkTheme;
            
            // Timer pour réessayer la connexion Inventor si elle échoue au démarrage
            // Utile quand l'app est lancée par script avant que COM soit prêt
            _inventorReconnectTimer = new System.Windows.Threading.DispatcherTimer();
            _inventorReconnectTimer.Interval = TimeSpan.FromSeconds(3);
            _inventorReconnectTimer.Tick += InventorReconnectTimer_Tick;
        }
        
        /// <summary>
        /// Ajoute une entree au journal avec couleur et clignotement pour les erreurs
        /// Utilise JournalColorService pour uniformite des couleurs
        /// </summary>
        private void AddLog(string message, string level = "INFO")
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string icon = level switch
            {
                "ERROR" => "[-]",
                "WARN" => "[!]",
                "SUCCESS" => "[+]",
                "START" => "[>]",
                "STOP" => "[~]",
                "CRITICAL" => "[X]",
                _ => "[i]"
            };
            
            string text = "[" + timestamp + "] " + icon + " " + message;
            
            Dispatcher.Invoke(() =>
            {
                var textBlock = new TextBlock
                {
                    Text = text,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    Padding = new Thickness(10, 4, 10, 4),
                    TextWrapping = TextWrapping.Wrap
                };
                
                // Utilise JournalColorService pour les couleurs uniformisees
                switch (level)
                {
                    case "ERROR":
                    case "CRITICAL":
                        textBlock.Foreground = Services.JournalColorService.ErrorBrush;
                        textBlock.FontWeight = FontWeights.Bold;
                        StartBlinkAnimation(textBlock);
                        break;
                    case "WARN":
                        textBlock.Foreground = Services.JournalColorService.WarningBrush;
                        textBlock.FontWeight = FontWeights.SemiBold;
                        break;
                    case "SUCCESS":
                        textBlock.Foreground = Services.JournalColorService.SuccessBrush;
                        break;
                    case "START":
                        textBlock.Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255)); // Bleu clair pour START
                        textBlock.FontWeight = FontWeights.SemiBold;
                        break;
                    case "STOP":
                        textBlock.Foreground = new SolidColorBrush(Color.FromRgb(180, 100, 255)); // Violet pour STOP
                        break;
                    default:
                        // INFO - Blanc pur depuis JournalColorService
                        textBlock.Foreground = Services.JournalColorService.InfoBrush;
                        break;
                }
                
                LogListBox.Items.Add(textBlock);
                LogListBox.ScrollIntoView(textBlock);
                
                while (LogListBox.Items.Count > 150)
                {
                    LogListBox.Items.RemoveAt(0);
                }
            });
            
            Logger.Log(message, level == "ERROR" || level == "CRITICAL" ? Logger.LogLevel.ERROR : Logger.LogLevel.INFO);
        }
        
        private void UpdateLogColors()
        {
            // Le journal a un fond noir FIXE, donc les logs INFO restent toujours blancs
            // Les autres couleurs (rouge erreur, vert succes, etc.) ne changent pas
            // Cette methode ne fait plus rien car le fond est fixe
        }
        
        private void StartBlinkAnimation(TextBlock textBlock)
        {
            // Utilise JournalColorService pour la couleur d'erreur
            var animation = new ColorAnimation
            {
                From = Services.JournalColorService.ErrorColor,
                To = Color.FromRgb(255, 200, 200),
                Duration = TimeSpan.FromMilliseconds(300),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(5)
            };
            
            var brush = new SolidColorBrush(Services.JournalColorService.ErrorColor);
            textBlock.Foreground = brush;
            brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
        
        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogListBox.Items.Clear();
            AddLog("Journal efface", "INFO");
        }
        
        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            
            // Sauvegarder le theme et notifier les sous-formulaires
            CurrentThemeIsDark = _isDarkTheme;
            UserPreferencesManager.SaveTheme(_isDarkTheme);
            ThemeChanged?.Invoke(_isDarkTheme);
            
            var whiteBrush = Brushes.White;
            var darkBgBrush = new SolidColorBrush(Color.FromRgb(37, 37, 54));
            var lightBgBrush = new SolidColorBrush(Color.FromRgb(250, 250, 250));
            var darkTextBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            var lightDescText = new SolidColorBrush(Color.FromRgb(220, 220, 220)); // Blanc léger pour descriptions (dark)
            var darkDescText = new SolidColorBrush(Color.FromRgb(50, 50, 50)); // Noir foncé pour descriptions (light)
            
            // Couleurs boutons
            var btnDarkBg = new SolidColorBrush(Color.FromRgb(37, 37, 54));
            var btnLightBg = new SolidColorBrush(Color.FromRgb(245, 245, 248));
            var btnLightBorder = new SolidColorBrush(Color.FromRgb(200, 200, 210));
            var btnDarkBorder = new SolidColorBrush(Color.FromRgb(64, 64, 96));
            
            // Status indicators
            var statusDarkBg = new SolidColorBrush(Color.FromRgb(37, 37, 54));
            var statusLightBg = new SolidColorBrush(Color.FromRgb(245, 245, 248));
            
            if (_isDarkTheme)
            {
                // Theme SOMBRE
                MainWindowRoot.Background = new SolidColorBrush(Color.FromRgb(30, 30, 46));
                ConnectionBorder.Background = new SolidColorBrush(Color.FromRgb(42, 74, 111)); // Bleu marine #2A4A6F - FIXE
                LogBorder.Background = new SolidColorBrush(Color.FromRgb(26, 26, 40)); // #1A1A28 - FIXE NOIR
                LogBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 74, 111)); // Bleu marine #2A4A6F
                LogHeaderBorder.Background = new SolidColorBrush(Color.FromRgb(42, 74, 111)); // Bleu marine #2A4A6F
                StatusBorder.Background = new SolidColorBrush(Color.FromRgb(37, 37, 54));
                
                // GroupBox
                GroupBox1.Background = darkBgBrush;
                GroupBox2.Background = darkBgBrush;
                GroupBox1.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 74, 111)); // Bleu marine #2A4A6F
                GroupBox2.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 74, 111)); // Bleu marine #2A4A6F
                
                // Titres GroupBox - BLANC sur fond sombre
                GroupBox1Header.Foreground = whiteBrush;
                GroupBox2Header.Foreground = whiteBrush;
                
                // Textes header et status
                TitleText.Foreground = whiteBrush;
                StatusText.Foreground = whiteBrush;
                CopyrightText.Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                
                // Status indicators Vault/Inventor - FIXE NOIR
                VaultStatusBorder.Background = new SolidColorBrush(Color.FromRgb(26, 26, 40)); // #1A1A28 - FIXE NOIR
                VaultStatusText.Foreground = whiteBrush;
                InventorStatusBorder.Background = new SolidColorBrush(Color.FromRgb(26, 26, 40)); // #1A1A28 - FIXE NOIR
                InventorLabel.Foreground = whiteBrush;
                InventorStatusText.Foreground = whiteBrush;
                
                // Journal header
                LogHeaderText.Foreground = whiteBrush;
                
                // Boutons - fond sombre
                BtnConfigUnits.Background = btnDarkBg;
                BtnOpenVaultProject.Background = btnDarkBg;
                BtnVaultUpload.Background = btnDarkBg;
                BtnPackAndGo.Background = btnDarkBg;
                BtnSmartTools.Background = btnDarkBg;
                BtnBuildModule.Background = btnDarkBg;
                BtnDXFVerifier.Background = btnDarkBg;
                BtnChecklistHVAC.Background = btnDarkBg;
                BtnUploadTemplate.Background = btnDarkBg;
                BtnPlaceEquipment.Background = btnDarkBg;
                BtnACP.Background = btnDarkBg;
                BtnNesting.Background = btnDarkBg;
                
                // Boutons - titres et descriptions (BLANC - pas de gris)
                BtnConfigUnitsTitle.Foreground = whiteBrush;
                BtnConfigUnitsDesc.Foreground = lightDescText;
                BtnOpenVaultProjectTitle.Foreground = whiteBrush;
                BtnOpenVaultProjectDesc.Foreground = lightDescText;
                BtnVaultUploadTitle.Foreground = whiteBrush;
                BtnVaultUploadDesc.Foreground = lightDescText;
                BtnPackAndGoTitle.Foreground = whiteBrush;
                BtnPackAndGoDesc.Foreground = lightDescText;
                BtnSmartToolsTitle.Foreground = whiteBrush;
                BtnSmartToolsDesc.Foreground = lightDescText;
                BtnBuildModuleTitle.Foreground = whiteBrush;
                BtnBuildModuleDesc.Foreground = lightDescText;
                BtnDXFVerifierTitle.Foreground = whiteBrush;
                BtnDXFVerifierDesc.Foreground = lightDescText;
                BtnChecklistHVACTitle.Foreground = whiteBrush;
                BtnChecklistHVACDesc.Foreground = lightDescText;
                BtnUploadTemplateTitle.Foreground = whiteBrush;
                BtnUploadTemplateDesc.Foreground = lightDescText;
                BtnPlaceEquipmentTitle.Foreground = whiteBrush;
                BtnPlaceEquipmentDesc.Foreground = lightDescText;
                BtnACPTitle.Foreground = whiteBrush;
                BtnACPDesc.Foreground = lightDescText;
                BtnNestingTitle.Foreground = whiteBrush;
                BtnNestingDesc.Foreground = lightDescText;
                
                // Bouton Theme - fond sombre pour thème sombre
                ThemeToggleButton.Background = new SolidColorBrush(Color.FromRgb(30, 30, 46));
                ThemeToggleButton.Foreground = whiteBrush;
                ThemeToggleButton.Content = "☀️ Theme Clair";
                
                // Bouton Update - violet
                UpdateButton.Background = new SolidColorBrush(Color.FromRgb(124, 58, 237)); // #7C3AED
                UpdateButton.Foreground = whiteBrush;
                
                AddLog("Theme sombre active", "INFO");
            }
            else
            {
                // Theme CLAIR - Couleurs élégantes
                MainWindowRoot.Background = new SolidColorBrush(Color.FromRgb(245, 247, 250)); // Bleu-gris très clair
                ConnectionBorder.Background = new SolidColorBrush(Color.FromRgb(42, 74, 111)); // Bleu marine #2A4A6F - FIXE
                LogBorder.Background = new SolidColorBrush(Color.FromRgb(26, 26, 40)); // #1A1A28 - FIXE NOIR
                LogBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 74, 111)); // Bleu marine #2A4A6F
                LogHeaderBorder.Background = new SolidColorBrush(Color.FromRgb(42, 74, 111)); // Bleu marine #2A4A6F
                StatusBorder.Background = new SolidColorBrush(Color.FromRgb(235, 240, 248)); // Bleu très pâle

                // GroupBox - fond blanc avec teinte bleutée
                GroupBox1.Background = new SolidColorBrush(Color.FromRgb(252, 253, 255));
                GroupBox2.Background = new SolidColorBrush(Color.FromRgb(252, 253, 255));
                GroupBox1.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 74, 111)); // Bleu marine #2A4A6F
                GroupBox2.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 74, 111)); // Bleu marine #2A4A6F
                
                // Titres GroupBox - BLANC sur fond bleu marine (header reste bleu marine meme en theme clair)
                GroupBox1Header.Foreground = whiteBrush;
                GroupBox2Header.Foreground = whiteBrush;
                
                // Textes - noir foncé
                TitleText.Foreground = whiteBrush; // Reste blanc sur fond bleu
                StatusText.Foreground = darkTextBrush;
                CopyrightText.Foreground = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                
                // Status indicators Vault/Inventor - FIXE NOIR meme en theme clair (texte blanc)
                VaultStatusBorder.Background = new SolidColorBrush(Color.FromRgb(26, 26, 40)); // #1A1A28 - FIXE NOIR
                VaultStatusText.Foreground = whiteBrush;
                InventorStatusBorder.Background = new SolidColorBrush(Color.FromRgb(26, 26, 40)); // #1A1A28 - FIXE NOIR
                InventorLabel.Foreground = whiteBrush;
                InventorStatusText.Foreground = whiteBrush;
                
                // Journal header - reste BLANC sur barre bleu marine (meme en theme clair)
                LogHeaderText.Foreground = whiteBrush;
                
                // Boutons - fond bleu très pâle élégant
                var btnLightBgElegant = new SolidColorBrush(Color.FromRgb(240, 245, 252));
                BtnConfigUnits.Background = btnLightBgElegant;
                BtnOpenVaultProject.Background = btnLightBgElegant;
                BtnVaultUpload.Background = btnLightBgElegant;
                BtnPackAndGo.Background = btnLightBgElegant;
                BtnSmartTools.Background = btnLightBgElegant;
                BtnBuildModule.Background = btnLightBgElegant;
                BtnDXFVerifier.Background = btnLightBgElegant;
                BtnChecklistHVAC.Background = btnLightBgElegant;
                BtnUploadTemplate.Background = btnLightBgElegant;
                BtnPlaceEquipment.Background = btnLightBgElegant;
                BtnACP.Background = btnLightBgElegant;
                BtnNesting.Background = btnLightBgElegant;
                
                // Boutons - titres et descriptions (NOIR FONCE - pas de gris)
                BtnConfigUnitsTitle.Foreground = darkTextBrush;
                BtnConfigUnitsDesc.Foreground = darkDescText;
                BtnOpenVaultProjectTitle.Foreground = darkTextBrush;
                BtnOpenVaultProjectDesc.Foreground = darkDescText;
                BtnVaultUploadTitle.Foreground = darkTextBrush;
                BtnVaultUploadDesc.Foreground = darkDescText;
                BtnPackAndGoTitle.Foreground = darkTextBrush;
                BtnPackAndGoDesc.Foreground = darkDescText;
                BtnSmartToolsTitle.Foreground = darkTextBrush;
                BtnSmartToolsDesc.Foreground = darkDescText;
                BtnBuildModuleTitle.Foreground = darkTextBrush;
                BtnBuildModuleDesc.Foreground = darkDescText;
                BtnDXFVerifierTitle.Foreground = darkTextBrush;
                BtnDXFVerifierDesc.Foreground = darkDescText;
                BtnChecklistHVACTitle.Foreground = darkTextBrush;
                BtnChecklistHVACDesc.Foreground = darkDescText;
                BtnUploadTemplateTitle.Foreground = darkTextBrush;
                BtnUploadTemplateDesc.Foreground = darkDescText;
                BtnPlaceEquipmentTitle.Foreground = darkTextBrush;
                BtnPlaceEquipmentDesc.Foreground = darkDescText;
                BtnACPTitle.Foreground = darkTextBrush;
                BtnACPDesc.Foreground = darkDescText;
                BtnNestingTitle.Foreground = darkTextBrush;
                BtnNestingDesc.Foreground = darkDescText;
                
                // Bouton Theme - fond bleu pâle élégant pour thème clair
                ThemeToggleButton.Background = new SolidColorBrush(Color.FromRgb(235, 240, 250));
                ThemeToggleButton.Foreground = darkTextBrush;
                ThemeToggleButton.Content = "🌙 Theme Sombre";
                
                // Bouton Update - violet
                UpdateButton.Background = new SolidColorBrush(Color.FromRgb(124, 58, 237)); // #7C3AED
                UpdateButton.Foreground = Brushes.White;
                
                AddLog("Theme clair active", "INFO");
            }
            
            // Mettre à jour les couleurs des logs existants
            UpdateLogColors();
            
            // Mettre à jour la couleur du bouton Connecter selon son état
            UpdateConnectionStatus();
        }

        /// <summary>
        /// Applique le theme actuel (appele au demarrage et lors du changement)
        /// </summary>
        private void ApplyCurrentTheme()
        {
            // Simuler un clic sur le bouton theme pour appliquer les couleurs
            // mais sans changer le theme (on le remet apres)
            bool currentTheme = _isDarkTheme;
            _isDarkTheme = !_isDarkTheme; // Inverser temporairement
            ThemeToggleButton_Click(this, new RoutedEventArgs()); // Cela remet le bon theme
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AddLog("===============================================================", "INFO");
            AddLog("XNRGY ENGINEERING AUTOMATION TOOLS v1.0", "START");
            AddLog("Developpe par Mohammed Amine Elgalai - XNRGY Climate Systems", "INFO");
            AddLog("===============================================================", "INFO");
            
            // Synchroniser les Feature Flags avec Firebase (en arriere-plan)
            _ = FirebaseAuditService.Instance.SyncFeatureFlagsAsync();
            
            // Synchroniser les categories de telemetrie avec les bons emojis (en arriere-plan)
            _ = FirebaseAuditService.Instance.SyncTelemetryCategoriesAsync();
            
            // Appliquer le theme sauvegarde au demarrage
            if (!_isDarkTheme)
            {
                // Si le theme sauvegarde est clair, on doit l'appliquer
                // (par defaut l'UI est en mode sombre)
                _isDarkTheme = true; // Forcer sombre d'abord
                ThemeToggleButton_Click(this, new RoutedEventArgs()); // Passer en clair
            }
            
            // Lancer la checklist de démarrage
            Dispatcher.BeginInvoke(new Action(async () => await RunStartupChecklist()), 
                System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// Exécute la checklist de démarrage ROBUSTE:
        /// 1. Connexion Vault SDK (obligatoire)
        /// 2. Update Workspace (addins, config) - FERME Inventor si ouvert
        /// 3. Lancement Inventor (UNIQUEMENT à la fin de l'update)
        /// 4. Lancement Vault Client
        /// 
        /// IMPORTANT: Inventor est lance UNIQUEMENT apres l'update pour eviter les conflits d'addins
        /// </summary>
        private async Task RunStartupChecklist()
        {
            AddLog("", "INFO");
            AddLog("[i] CHECKLIST DE DEMARRAGE", "START");
            AddLog("═══════════════════════════════════════════════", "INFO");

            // === ETAPE 1: Connexion Vault SDK ===
            AddLog("", "INFO");
            AddLog("[>] [1/4] Connexion a Vault SDK...", "INFO");
            
            if (!_isVaultConnected)
            {
                AddLog("   [>] Ouverture de la fenetre de connexion...", "INFO");
                
                var loginWindow = new LoginWindow(_vaultService, autoConnect: true)
                {
                    Owner = this
                };
                
                if (loginWindow.ShowDialog() == true)
                {
                    _isVaultConnected = true;
                    UpdateConnectionStatus();
                    AddLog("   [+] Connexion Vault etablie!", "SUCCESS");
                    
                    // Synchronisation automatique des parametres depuis Vault
                    SyncSettingsFromVaultAsync();
                }
                else
                {
                    AddLog("   [!] Connexion Vault annulee - Certaines fonctionnalites seront limitees", "WARN");
                    // Continuer quand meme avec les etapes suivantes
                }
            }
            else
            {
                AddLog("   [+] Deja connecte a Vault", "SUCCESS");
            }
            
            // === ETAPE 2: Fermer Inventor (si ouvert) pour Update ===
            AddLog("", "INFO");
            AddLog("[>] [2/4] Preparation de l'environnement...", "INFO");
            
            var inventorProcesses = Process.GetProcessesByName("Inventor");
            if (inventorProcesses.Length > 0)
            {
                AddLog("   [!] Inventor detecte - fermeture pour mise a jour des addins", "WARN");
                
                // Deconnecter COM d'abord
                if (_isInventorConnected)
                {
                    _inventorService?.Disconnect();
                    _isInventorConnected = false;
                    UpdateConnectionStatus();
                }
                
                // Tuer le processus Inventor
                foreach (var proc in inventorProcesses)
                {
                    try { proc.Kill(); } catch { }
                }
                
                // Attendre que le processus se termine
                await Task.Delay(1000);
                AddLog("   [+] Inventor ferme", "SUCCESS");
            }
            else
            {
                AddLog("   [+] Environnement pret (Inventor non actif)", "SUCCESS");
            }
            
            // === ETAPE 3: Update Workspace ===
            AddLog("", "INFO");
            AddLog("[>] [3/4] Mise a jour du workspace...", "INFO");
            
            if (_vaultService.Connection != null)
            {
                var updateWindow = new UpdateWorkspaceWindow(
                    _vaultService.Connection, 
                    autoStart: true, 
                    autoCloseOnSuccess: true)
                {
                    Owner = this
                };
                
                AddLog("   [~] Synchronisation en cours...", "INFO");
                
                if (updateWindow.ShowDialog() == true)
                {
                    if (updateWindow.WasSkipped)
                    {
                        AddLog("   [i] Mise a jour ignoree par l'utilisateur", "INFO");
                    }
                    else
                    {
                        AddLog("   [+] Mise a jour terminee avec succes", "SUCCESS");
                    }
                }
                else
                {
                    AddLog("   [!] Mise a jour annulee", "WARN");
                }
            }
            else
            {
                AddLog("   [!] Connexion Vault requise - mise a jour ignoree", "WARN");
            }
            
            // === ETAPE 4: Lancement des applications (Inventor + Vault Client) ===
            // IMPORTANT: Inventor est lance ICI, APRES l'update, pour charger les nouveaux addins
            AddLog("", "INFO");
            AddLog("[>] [4/4] Lancement des applications...", "INFO");
            
            await LaunchApplicationsSequentialAsync();
            
            // === RESUME FINAL ===
            ShowFinalResume();
        }
        
        /// <summary>
        /// Lance Inventor et Vault Client de maniere sequentielle et controlee
        /// UNIQUEMENT appele a la fin de la checklist de demarrage
        /// </summary>
        private async Task LaunchApplicationsSequentialAsync()
        {
            // === INVENTOR ===
            AddLog("   [>] Lancement d'Inventor Professional 2026...", "INFO");
            
            string inventorPath = FindInventorExecutable();
            if (!string.IsNullOrEmpty(inventorPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = inventorPath,
                        UseShellExecute = true
                    });
                    
                    AddLog("   [~] Inventor demarre, attente du chargement...", "INFO");
                    
                    // Attendre que le processus demarre (max 15 sec)
                    bool processStarted = false;
                    for (int i = 0; i < 30; i++)
                    {
                        await Task.Delay(500);
                        var procs = Process.GetProcessesByName("Inventor");
                        if (procs.Length > 0)
                        {
                            processStarted = true;
                            break;
                        }
                    }
                    
                    if (processStarted)
                    {
                        AddLog("   [+] Inventor demarre!", "SUCCESS");
                        
                        // Attendre un peu plus pour que COM soit pret
                        await Task.Delay(3000);
                        
                        // Tenter la connexion COM
                        if (_inventorService.TryConnect())
                        {
                            _isInventorConnected = true;
                            UpdateConnectionStatus();
                            AddLog("   [+] Connexion COM etablie", "SUCCESS");
                        }
                        else
                        {
                            AddLog("   [~] Connexion COM en attente (timer de reconnexion actif)", "INFO");
                            _inventorReconnectTimer?.Start();
                        }
                    }
                    else
                    {
                        AddLog("   [!] Timeout - Inventor n'a pas demarre", "WARN");
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"   [-] Erreur lancement Inventor: {ex.Message}", "ERROR");
                }
            }
            else
            {
                AddLog("   [-] Inventor 2026 non trouve sur ce poste", "ERROR");
            }
            
            // === VAULT CLIENT ===
            var vaultProcesses = Process.GetProcessesByName("Connectivity.VaultPro");
            if (vaultProcesses.Length == 0)
            {
                AddLog("   [>] Lancement de Vault Client...", "INFO");
                
                string vaultClientPath = FindVaultClientExecutable();
                if (!string.IsNullOrEmpty(vaultClientPath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = vaultClientPath,
                            UseShellExecute = true
                        });
                        AddLog("   [+] Vault Client lance", "SUCCESS");
                    }
                    catch (Exception ex)
                    {
                        AddLog($"   [!] Erreur lancement Vault Client: {ex.Message}", "WARN");
                    }
                }
                else
                {
                    AddLog("   [!] Vault Client non trouve", "WARN");
                }
            }
            else
            {
                AddLog("   [+] Vault Client deja actif", "SUCCESS");
            }
        }
        
        /// <summary>
        /// Verifie et retablit les connexions Vault et Inventor si necessaire
        /// Appele lors des operations qui necessitent les connexions
        /// </summary>
        /// <param name="requireVault">Si true, la connexion Vault est obligatoire</param>
        /// <param name="requireInventor">Si true, la connexion Inventor est obligatoire</param>
        /// <returns>True si toutes les connexions requises sont etablies</returns>
        public async Task<bool> EnsureConnectionsAsync(bool requireVault = true, bool requireInventor = false)
        {
            bool allConnected = true;
            
            // === VAULT ===
            if (requireVault && !_isVaultConnected)
            {
                AddLog("[>] Reconnexion a Vault requise...", "INFO");
                
                var loginWindow = new LoginWindow(_vaultService, autoConnect: true)
                {
                    Owner = this
                };
                
                if (loginWindow.ShowDialog() == true)
                {
                    _isVaultConnected = true;
                    UpdateConnectionStatus();
                    AddLog("[+] Reconnexion Vault reussie", "SUCCESS");
                }
                else
                {
                    AddLog("[-] Reconnexion Vault echouee", "ERROR");
                    allConnected = false;
                }
            }
            
            // === INVENTOR ===
            if (requireInventor)
            {
                // Verifier si Inventor est en cours d'execution
                var inventorProcesses = Process.GetProcessesByName("Inventor");
                
                if (inventorProcesses.Length == 0)
                {
                    // Inventor n'est pas lance - le demarrer
                    AddLog("[>] Lancement d'Inventor requis...", "INFO");
                    
                    string inventorPath = FindInventorExecutable();
                    if (!string.IsNullOrEmpty(inventorPath))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = inventorPath,
                            UseShellExecute = true
                        });
                        
                        // Attendre le demarrage
                        for (int i = 0; i < 30; i++)
                        {
                            await Task.Delay(500);
                            if (Process.GetProcessesByName("Inventor").Length > 0)
                                break;
                        }
                        
                        await Task.Delay(3000); // Attendre que COM soit pret
                    }
                    else
                    {
                        AddLog("[-] Inventor non trouve", "ERROR");
                        allConnected = false;
                    }
                }
                
                // Tenter la connexion COM si pas deja connecte
                if (!_isInventorConnected && Process.GetProcessesByName("Inventor").Length > 0)
                {
                    if (_inventorService.TryConnect())
                    {
                        _isInventorConnected = true;
                        UpdateConnectionStatus();
                        AddLog("[+] Connexion Inventor etablie", "SUCCESS");
                    }
                    else
                    {
                        AddLog("[!] Connexion Inventor en attente...", "WARN");
                        allConnected = false;
                    }
                }
            }
            
            return allConnected;
        }
        
        /// <summary>
        /// Ouvre la fenetre de connexion avec tentative de connexion automatique
        /// Comportement pro: affiche le spinner, se ferme auto si succes, reste ouverte sinon
        /// NOTE: Cette methode est conservee pour compatibilite mais la checklist principale
        /// gere maintenant tout le flux de demarrage
        /// </summary>
        private void OpenLoginWindowWithAutoConnect()
        {
            var loginWindow = new LoginWindow(_vaultService, autoConnect: true)
            {
                Owner = this
            };
            
            if (loginWindow.ShowDialog() == true)
            {
                _isVaultConnected = true;
                UpdateConnectionStatus();
                AddLog("[+] Connexion Vault etablie avec succes!", "SUCCESS");
                
                // Synchronisation automatique des parametres depuis Vault au demarrage
                SyncSettingsFromVaultAsync();
                
                // Proposer la mise a jour du workspace apres connexion reussie
                ShowUpdateWorkspaceWindow();
            }
            else
            {
                AddLog("[!] Connexion Vault annulee ou echouee", "WARN");
            }
        }
        
        /// <summary>
        /// Ouvre la fenetre de connexion Vault (mode manuel)
        /// </summary>
        private void OpenLoginWindow()
        {
            var loginWindow = new LoginWindow(_vaultService)
            {
                Owner = this
            };
            
            if (loginWindow.ShowDialog() == true)
            {
                _isVaultConnected = true;
                UpdateConnectionStatus();
                AddLog("[+] Connexion Vault etablie avec succes!", "SUCCESS");
                
                // Synchronisation automatique des parametres depuis Vault au demarrage
                SyncSettingsFromVaultAsync();
                
                // Proposer la mise a jour du workspace apres connexion reussie
                ShowUpdateWorkspaceWindow();
            }
        }
        
        /// <summary>
        /// Affiche la fenetre de mise a jour du workspace (MODE MANUEL - bouton Update)
        /// Pour le mode automatique au demarrage, voir RunStartupChecklist()
        /// </summary>
        private void ShowUpdateWorkspaceWindow()
        {
            ShowUpdateWorkspaceWindow(autoStart: false, autoCloseOnSuccess: false);
        }

        /// <summary>
        /// Affiche la fenetre de mise a jour du workspace
        /// SCENARIO: Utilisateur clique sur Update en cours d'utilisation
        /// </summary>
        /// <param name="autoStart">Si true, demarre automatiquement sans attendre un clic</param>
        /// <param name="autoCloseOnSuccess">Si true, ferme automatiquement apres succes</param>
        private async void ShowUpdateWorkspaceWindow(bool autoStart, bool autoCloseOnSuccess)
        {
            try
            {
                // === VERIFIER CONNEXION VAULT ===
                if (_vaultService.Connection == null)
                {
                    AddLog("[!] Connexion Vault requise pour la mise a jour", "WARN");
                    
                    // Tenter la reconnexion
                    var success = await EnsureConnectionsAsync(requireVault: true, requireInventor: false);
                    if (!success || _vaultService.Connection == null)
                    {
                        AddLog("[-] Impossible de se connecter a Vault - Update annule", "ERROR");
                        return;
                    }
                }
                
                // === VERIFIER SI INVENTOR EST OUVERT AVANT UPDATE ===
                bool wasInventorRunning = false;
                var inventorProcesses = Process.GetProcessesByName("Inventor");
                
                if (inventorProcesses.Length > 0)
                {
                    wasInventorRunning = true;
                    AddLog("[>] Preparation Update Workspace...", "INFO");
                    AddLog("   [!] Inventor detecte - fermeture pour update addins", "WARN");
                    
                    // Deconnecter COM d'abord
                    if (_isInventorConnected)
                    {
                        _inventorService?.Disconnect();
                        _isInventorConnected = false;
                        UpdateConnectionStatus();
                    }
                    
                    // Tuer le processus Inventor
                    foreach (var proc in inventorProcesses)
                    {
                        try { proc.Kill(); } catch { }
                    }
                    
                    await Task.Delay(1000);
                    AddLog("   [+] Inventor ferme", "SUCCESS");
                }
                
                // === UPDATE WORKSPACE ===
                var updateWindow = new UpdateWorkspaceWindow(
                    _vaultService.Connection, 
                    autoStart: autoStart, 
                    autoCloseOnSuccess: autoCloseOnSuccess)
                {
                    Owner = this
                };
                
                AddLog("[>] Mise a jour du workspace...", "INFO");
                
                bool updateCompleted = false;
                if (updateWindow.ShowDialog() == true)
                {
                    if (updateWindow.WasSkipped)
                    {
                        AddLog("[i] Mise a jour du workspace ignoree", "INFO");
                    }
                    else
                    {
                        AddLog("[+] Mise a jour du workspace terminee", "SUCCESS");
                        updateCompleted = true;
                    }
                }
                else
                {
                    AddLog("[!] Mise a jour du workspace annulee", "WARN");
                }
                
                // === TOUJOURS RELANCER INVENTOR APRES UPDATE ===
                // Que l'update soit complete ou annulee, on relance Inventor
                // car les addins ont peut-etre ete modifies
                if (wasInventorRunning || updateCompleted)
                {
                    AddLog("[>] Relancement d'Inventor...", "INFO");
                    await LaunchAndConnectInventorAsync();
                }
            }
            catch (Exception ex)
            {
                AddLog($"[-] Erreur lors de la mise a jour du workspace: {ex.Message}", "ERROR");
                Logger.Log($"[-] Erreur UpdateWorkspace: {ex.Message}", Logger.LogLevel.ERROR);
            }
        }
        
        /// <summary>
        /// Lance Inventor et etablit la connexion COM
        /// Methode utilitaire pour les scenarios de relancement
        /// </summary>
        private async Task LaunchAndConnectInventorAsync()
        {
            string inventorPath = FindInventorExecutable();
            if (string.IsNullOrEmpty(inventorPath))
            {
                AddLog("   [-] Inventor 2026 non trouve", "ERROR");
                return;
            }
            
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = inventorPath,
                    UseShellExecute = true
                });
                
                AddLog("   [~] Inventor demarre, attente...", "INFO");
                
                // Attendre le demarrage
                bool started = false;
                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(500);
                    if (Process.GetProcessesByName("Inventor").Length > 0)
                    {
                        started = true;
                        break;
                    }
                }
                
                if (started)
                {
                    AddLog("   [+] Inventor demarre!", "SUCCESS");
                    await Task.Delay(3000); // Attendre que COM soit pret
                    
                    if (_inventorService.TryConnect())
                    {
                        _isInventorConnected = true;
                        UpdateConnectionStatus();
                        AddLog("   [+] Connexion COM etablie", "SUCCESS");
                    }
                    else
                    {
                        AddLog("   [~] Connexion COM en attente...", "INFO");
                        _inventorReconnectTimer?.Start();
                    }
                }
                else
                {
                    AddLog("   [!] Timeout demarrage Inventor", "WARN");
                    _inventorReconnectTimer?.Start();
                }
            }
            catch (Exception ex)
            {
                AddLog($"   [-] Erreur: {ex.Message}", "ERROR");
                _inventorReconnectTimer?.Start();
            }
        }
        
        // Flag pour eviter de lancer Inventor plusieurs fois en parallele
        private static bool _isLaunchingInventor = false;
        private static readonly object _launchLock = new object();
        
        /// <summary>
        /// Lance Inventor et attend qu'il soit pret pour connexion COM
        /// PROTECTION: Vérifie qu'Inventor n'est pas déjà en cours de lancement
        /// </summary>
        private async void LaunchInventorAsync()
        {
            // Verifier si on est deja en train de lancer Inventor
            lock (_launchLock)
            {
                if (_isLaunchingInventor)
                {
                    Dispatcher.Invoke(() =>
                        AddLog("   [!] Lancement Inventor deja en cours...", "WARN"));
                    return;
                }
                _isLaunchingInventor = true;
            }
            
            try
            {
                await Task.Run(async () =>
                {
                    try
                    {
                        // Verifier si Inventor est deja en cours d'execution
                        var existingProcs = Process.GetProcessesByName("Inventor");
                        if (existingProcs.Length > 0)
                        {
                            Dispatcher.Invoke(() =>
                                AddLog("   [+] Inventor deja en cours d'execution", "SUCCESS"));
                            
                            // Timer de reconnexion gere la connexion COM
                            Dispatcher.Invoke(() =>
                            {
                                _inventorReconnectTimer?.Start();
                                UpdateConnectionStatus();
                            });
                            return;
                        }
                        
                        string inventorPath = FindInventorExecutable();
                        if (string.IsNullOrEmpty(inventorPath))
                        {
                            Dispatcher.Invoke(() =>
                                AddLog("   [-] Inventor 2026 non trouve sur ce poste", "ERROR"));
                            return;
                        }
                        
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = inventorPath,
                            UseShellExecute = true
                        });
                        
                        Dispatcher.Invoke(() =>
                            AddLog("   [~] Inventor demarre, veuillez patienter...", "INFO"));
                        
                        // Attendre que le processus demarre (max 10 sec - check rapide)
                        bool processStarted = false;
                        for (int i = 0; i < 20; i++)
                        {
                            await Task.Delay(500);
                            var procs = Process.GetProcessesByName("Inventor");
                            if (procs.Length > 0)
                            {
                                processStarted = true;
                                break;
                            }
                        }
                        
                        if (processStarted)
                        {
                            Dispatcher.Invoke(() =>
                                AddLog("   [+] Inventor demarre!", "SUCCESS"));
                        }
                        
                        // Timer de reconnexion gere la connexion COM
                        Dispatcher.Invoke(() =>
                        {
                            _inventorReconnectTimer?.Start();
                            UpdateConnectionStatus();
                            ShowFinalResume();
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            AddLog($"   [-] Erreur lancement Inventor: {ex.Message}", "ERROR");
                            _inventorReconnectTimer?.Start();
                        });
                    }
                });
            }
            finally
            {
                lock (_launchLock)
                {
                    _isLaunchingInventor = false;
                }
            }
        }
        
        /// <summary>
        /// Lance Inventor et Vault Client si non detectes
        /// </summary>
        private async void LaunchApplicationsAsync()
        {
            await Task.Run(async () =>
            {
                try
                {
                    // === INVENTOR ===
                    var inventorProcesses = Process.GetProcessesByName("Inventor");
                    bool inventorWasRunning = inventorProcesses.Length > 0;
                    
                    if (!inventorWasRunning)
                    {
                        Dispatcher.Invoke(() =>
                            AddLog("   [>] Lancement d'Inventor Professional 2026...", "INFO"));
                        
                        string inventorPath = FindInventorExecutable();
                        if (!string.IsNullOrEmpty(inventorPath))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = inventorPath,
                                UseShellExecute = true
                            });
                            
                            Dispatcher.Invoke(() =>
                                AddLog("   [~] Inventor demarre...", "INFO"));
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                                AddLog("   [!] Inventor non trouve", "WARN"));
                        }
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                            AddLog("   [+] Inventor deja en cours", "SUCCESS"));
                    }
                    
                    // === VAULT CLIENT ===
                    string[] vaultProcessNames = { "Connectivity.VaultPro", "Connectivity.Vault" };
                    bool vaultFound = false;
                    
                    foreach (var pName in vaultProcessNames)
                    {
                        if (Process.GetProcessesByName(pName).Length > 0)
                        {
                            vaultFound = true;
                            break;
                        }
                    }
                    
                    if (!vaultFound)
                    {
                        Dispatcher.Invoke(() =>
                            AddLog("   [>] Lancement de Vault Client 2026...", "INFO"));
                        
                        string vaultPath = FindVaultClientExecutable();
                        if (!string.IsNullOrEmpty(vaultPath))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = vaultPath,
                                UseShellExecute = true
                            });
                            
                            Dispatcher.Invoke(() =>
                                AddLog("   [~] Vault Client demarre...", "INFO"));
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                                AddLog("   [!] Vault Client non trouve", "WARN"));
                        }
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                            AddLog("   [+] Vault Client deja en cours", "SUCCESS"));
                    }
                    
                    // Attendre un peu et tenter connexion Inventor
                    await Task.Delay(5000);
                    
                    // Detection et connexion Inventor
                    Dispatcher.Invoke(() => DetectAndConnectInventorAsync());
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddLog($"   [-] Erreur lancement applications: {ex.Message}", "ERROR");
                        _inventorReconnectTimer?.Start();
                    });
                }
            });
        }
        
        /// <summary>
        /// Affiche le resume final de la checklist
        /// </summary>
        private void ShowFinalResume()
        {
            AddLog("", "INFO");
            AddLog("───────────────────────────────────────────────", "INFO");
            AddLog("[i] RESUME:", "INFO");
            AddLog("   • Vault:    " + (_isVaultConnected ? "Connecte" : "Deconnecte"), _isVaultConnected ? "SUCCESS" : "WARN");
            AddLog("   • Inventor: " + (_isInventorConnected ? "Connecte" : "Detection en cours..."), _isInventorConnected ? "SUCCESS" : "INFO");
            AddLog("───────────────────────────────────────────────", "INFO");
            AddLog("", "INFO");
            
            // Remettre l'application au premier plan apres la checklist de demarrage
            _ = WindowFocusService.BringToFrontAsync(this, delayMs: 500, attempts: 3);
        }
        
        /// <summary>
        /// Detecte si Inventor est en cours d'execution et tente une connexion COM
        /// </summary>
        private async void DetectAndConnectInventorAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Verifier si le processus Inventor est en cours
                    var inventorProcesses = Process.GetProcessesByName("Inventor");
                    
                    if (inventorProcesses.Length == 0)
                    {
                        // Inventor n'est pas lance - timer activera la reconnexion
                        Dispatcher.Invoke(() =>
                        {
                            _inventorReconnectTimer?.Start();
                            UpdateConnectionStatus();
                            ShowFinalResume();
                        });
                        return;
                    }
                    
                    // Inventor est en cours - verifier si la fenetre principale est prete
                    bool windowReady = false;
                    foreach (var proc in inventorProcesses)
                    {
                        try
                        {
                            if (proc.MainWindowHandle != IntPtr.Zero)
                            {
                                windowReady = true;
                                break;
                            }
                        }
                        catch { }
                    }
                    
                    if (!windowReady)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            AddLog("   [~] Inventor en cours de demarrage...", "INFO");
                            _inventorReconnectTimer?.Start();
                            UpdateConnectionStatus();
                            ShowFinalResume();
                        });
                        return;
                    }
                    
                    // Inventor est pret - tenter connexion COM
                    if (_inventorService.TryConnect())
                    {
                        _isInventorConnected = true;
                        string version = _inventorService.GetInventorVersion();
                        
                        Dispatcher.Invoke(() =>
                        {
                            if (!string.IsNullOrEmpty(version))
                                AddLog("   [i] Inventor " + version, "INFO");
                            AddLog("   [+] Inventor - Connecte!", "SUCCESS");
                            UpdateConnectionStatus();
                            ShowFinalResume();
                        });
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            AddLog("   [~] Reconnexion automatique activee", "INFO");
                            _inventorReconnectTimer?.Start();
                            UpdateConnectionStatus();
                            ShowFinalResume();
                        });
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddLog($"   [!] Erreur detection Inventor: {ex.Message}", "WARN");
                        _inventorReconnectTimer?.Start();
                        ShowFinalResume();
                    });
                }
            });
        }

        /// <summary>
        /// Synchronise les parametres de l'application depuis Vault en arriere-plan
        /// Appele automatiquement apres connexion Vault reussie
        /// </summary>
        private async void SyncSettingsFromVaultAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    var settingsService = new VaultSettingsService(_vaultService);
                    bool synced = settingsService.SyncFromVault();
                    
                    Dispatcher.Invoke(() =>
                    {
                        if (synced)
                        {
                            AddLog("[+] Parametres synchronises depuis Vault", "SUCCESS");
                        }
                        else
                        {
                            AddLog("[i] Parametres locaux utilises (fichier Vault non trouve)", "INFO");
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    AddLog($"[!] Erreur sync parametres: {ex.Message}", "WARN"));
            }
        }

        /// <summary>
        /// Verifie si Inventor est lance, sinon le lance
        /// </summary>
        private async Task<bool> CheckAndLaunchInventorAsync()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    // Verifier si le processus Inventor est en cours
                    var inventorProcesses = Process.GetProcessesByName("Inventor");
                    
                    if (inventorProcesses.Length == 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            AddLog("   [!] Inventor n'est pas en cours d'execution", "WARN");
                            AddLog("   [>] Lancement d'Inventor Professional 2026...", "INFO");
                        });
                        
                        // Chercher et lancer Inventor
                        string inventorPath = FindInventorExecutable();
                        if (!string.IsNullOrEmpty(inventorPath))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = inventorPath,
                                UseShellExecute = true
                            });
                            
                            Dispatcher.Invoke(() =>
                                AddLog("   [~] Inventor demarre, veuillez patienter...", "INFO"));
                            
                            // Attendre qu'Inventor demarre (processus visible)
                            bool processStarted = false;
                            for (int i = 0; i < 30; i++)
                            {
                                await Task.Delay(1000);
                                inventorProcesses = Process.GetProcessesByName("Inventor");
                                if (inventorProcesses.Length > 0)
                                {
                                    processStarted = true;
                                    Dispatcher.Invoke(() =>
                                        AddLog("   [+] Processus Inventor detecte!", "SUCCESS"));
                                    break;
                                }
                            }
                            
                            if (!processStarted)
                            {
                                Dispatcher.Invoke(() =>
                                    AddLog("   [-] Inventor n'a pas demarre", "ERROR"));
                                return false;
                            }
                            
                            // Attendre que COM soit pret (Inventor a besoin de temps pour s'initialiser)
                            Dispatcher.Invoke(() =>
                                AddLog("   [~] Attente initialisation COM (peut prendre 15-30 sec)...", "INFO"));
                            
                            // Tentatives de connexion COM avec attente progressive
                            for (int attempt = 1; attempt <= 6; attempt++)
                            {
                                await Task.Delay(5000); // Attendre 5 secondes entre chaque tentative
                                
                                Dispatcher.Invoke(() =>
                                    AddLog($"   [>] Tentative de connexion COM {attempt}/6...", "INFO"));
                                
                                if (_inventorService.TryConnect())
                                {
                                    _isInventorConnected = true;
                                    string version = _inventorService.GetInventorVersion();
                                    
                                    Dispatcher.Invoke(() =>
                                    {
                                        if (!string.IsNullOrEmpty(version))
                                            AddLog("   [i] Version: " + version, "INFO");
                                        AddLog("   [+] Inventor Professional - Connexion COM etablie!", "SUCCESS");
                                        UpdateConnectionStatus();
                                    });
                                    
                                    // Remettre l'application au premier plan apres demarrage Inventor
                                    await WindowFocusService.BringToFrontAfterInventorStartAsync(
                                        Application.Current.Dispatcher.Invoke(() => this));
                                    
                                    return true;
                                }
                            }
                            
                            // Si toujours pas connecte apres 6 tentatives (30 sec)
                            Dispatcher.Invoke(() =>
                            {
                                AddLog("   [!] Inventor demarre mais COM pas encore pret", "WARN");
                                AddLog("   -> L'application reessayera automatiquement", "INFO");
                            });
                            
                            // Remettre l'application au premier plan meme si COM pas pret
                            await WindowFocusService.BringToFrontAfterInventorStartAsync(
                                Application.Current.Dispatcher.Invoke(() => this));
                            
                            return false;
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                                AddLog("   [-] Inventor 2026 non trouve sur ce poste", "ERROR"));
                            return false;
                        }
                    }
                    
                    Dispatcher.Invoke(() => 
                        AddLog("   [>] Processus Inventor deja en cours", "INFO"));
                    
                    // Inventor deja lance - connexion directe
                    if (_inventorService.TryConnect())
                    {
                        _isInventorConnected = true;
                        
                        // Recuperer la version
                        string version = _inventorService.GetInventorVersion();
                        
                        Dispatcher.Invoke(() =>
                        {
                            if (!string.IsNullOrEmpty(version))
                            {
                                AddLog("   [i] Version: " + version, "INFO");
                                AddLog("   [+] Inventor Professional - Connexion etablie!", "SUCCESS");
                            }
                            else
                            {
                                AddLog("   [+] Connexion a Inventor etablie", "SUCCESS");
                            }
                            // Mettre a jour l'indicateur de connexion
                            UpdateConnectionStatus();
                        });
                        
                        return true;
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            AddLog("   [!] Inventor en cours mais connexion COM echouee", "WARN");
                            AddLog("   -> Verifiez qu'Inventor est completement charge", "INFO");
                        });
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddLog("   [-] Erreur verification Inventor: " + ex.Message, "ERROR");
                    });
                    return false;
                }
            });
        }
        
        /// <summary>
        /// Trouve l'exécutable Inventor sur le système
        /// </summary>
        private string FindInventorExecutable()
        {
            // Chemins standards pour Inventor 2026
            string[] possiblePaths = new[]
            {
                @"C:\Program Files\Autodesk\Inventor 2026\Bin\Inventor.exe",
                @"C:\Program Files\Autodesk\Inventor 2025\Bin\Inventor.exe",
                @"C:\Program Files\Autodesk\Inventor 2024\Bin\Inventor.exe",
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Autodesk\Inventor 2026\Bin\Inventor.exe"),
            };
            
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }
            
            // Chercher dans Program Files
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string autodeskPath = Path.Combine(programFiles, "Autodesk");
            
            if (Directory.Exists(autodeskPath))
            {
                foreach (var dir in Directory.GetDirectories(autodeskPath, "Inventor*"))
                {
                    string inventorExe = Path.Combine(dir, "Bin", "Inventor.exe");
                    if (File.Exists(inventorExe))
                        return inventorExe;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Trouve l'exécutable Vault Client sur le système
        /// </summary>
        private string FindVaultClientExecutable()
        {
            // Chemins standards pour Vault Client 2026
            string[] possiblePaths = new[]
            {
                @"C:\Program Files\Autodesk\Vault Professional 2026\Explorer\Connectivity.VaultPro.exe",
                @"C:\Program Files\Autodesk\Vault Client 2026\Explorer\Connectivity.VaultPro.exe",
                @"C:\Program Files\Autodesk\Vault Professional 2025\Explorer\Connectivity.VaultPro.exe",
                @"C:\Program Files\Autodesk\Autodesk Vault Professional 2026\Explorer\Connectivity.VaultPro.exe",
            };
            
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }
            
            // Chercher dans Program Files
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string autodeskPath = Path.Combine(programFiles, "Autodesk");
            
            if (Directory.Exists(autodeskPath))
            {
                foreach (var dir in Directory.GetDirectories(autodeskPath, "Vault*"))
                {
                    string explorerDir = Path.Combine(dir, "Explorer");
                    if (Directory.Exists(explorerDir))
                    {
                        string vaultExe = Path.Combine(explorerDir, "Connectivity.VaultPro.exe");
                        if (File.Exists(vaultExe))
                            return vaultExe;
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Vérifie si le Vault Client est lancé, sinon le lance
        /// </summary>
        private async Task<bool> CheckAndLaunchVaultClientAsync()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    // Noms de processus pour Vault Client
                    string[] vaultProcessNames = { "Connectivity.VaultPro", "Connectivity.Vault", "Explorer" };
                    bool vaultClientFound = false;
                    string foundProcessName = null;
                    
                    foreach (var processName in vaultProcessNames)
                    {
                        var processes = Process.GetProcessesByName(processName);
                        if (processes.Length > 0)
                        {
                            foreach (var proc in processes)
                            {
                                try
                                {
                                    string path = proc.MainModule?.FileName;
                                    if (path != null && path.ToLower().Contains("autodesk"))
                                    {
                                        vaultClientFound = true;
                                        foundProcessName = processName;
                                        break;
                                    }
                                }
                                catch { }
                            }
                        }
                        if (vaultClientFound) break;
                    }
                    
                    // Alternative: chercher fenêtre avec "Vault" dans le titre
                    if (!vaultClientFound)
                    {
                        var allProcesses = Process.GetProcesses();
                        foreach (var proc in allProcesses)
                        {
                            try
                            {
                                if (proc.MainWindowTitle.Contains("Vault") && 
                                    !proc.MainWindowTitle.Contains("VaultAutomation"))
                                {
                                    string path = proc.MainModule?.FileName;
                                    if (path != null && path.ToLower().Contains("autodesk"))
                                    {
                                        vaultClientFound = true;
                                        foundProcessName = proc.ProcessName;
                                        break;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    
                    if (vaultClientFound)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            AddLog("   [>] Vault Client detecte (processus: " + foundProcessName + ")", "INFO");
                            AddLog("   [+] Vault Professional Client - Pret pour connexion", "SUCCESS");
                        });
                        return true;
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            AddLog("   [!] Vault Client non detecte", "WARN");
                            AddLog("   [>] Lancement de Vault Client 2026...", "INFO");
                        });
                        
                        // Chercher et lancer Vault Client
                        string vaultPath = FindVaultClientExecutable();
                        if (!string.IsNullOrEmpty(vaultPath))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = vaultPath,
                                UseShellExecute = true
                            });
                            
                            Dispatcher.Invoke(() =>
                                AddLog("   [~] Vault Client demarre, veuillez patienter...", "INFO"));
                            
                            // Attendre que Vault demarre (max 15 sec)
                            for (int i = 0; i < 15; i++)
                            {
                                await Task.Delay(1000);
                                foreach (var pName in vaultProcessNames)
                                {
                                    var procs = Process.GetProcessesByName(pName);
                                    if (procs.Length > 0)
                                    {
                                        Dispatcher.Invoke(() =>
                                            AddLog("   [+] Vault Client demarre avec succes!", "SUCCESS"));
                                        return true;
                                    }
                                }
                            }
                            
                            Dispatcher.Invoke(() =>
                                AddLog("   [!] Vault Client en cours de demarrage...", "WARN"));
                            return false;
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                AddLog("   [!] Vault Client 2026 non trouve sur ce poste", "WARN");
                                AddLog("   -> La connexion SDK sera utilisee", "INFO");
                            });
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddLog("   [-] Erreur verification Vault Client: " + ex.Message, "ERROR");
                    });
                    return false;
                }
            });
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            AddLog("Fermeture de l'application...", "STOP");
            if (_isVaultConnected) _vaultService?.Disconnect();
            _inventorService?.Disconnect();
        }

        private void TryConnectInventorAuto()
        {
            AddLog("Recherche d'Inventor en cours...", "INFO");
            try
            {
                if (_inventorService.TryConnect())
                {
                    _isInventorConnected = true;
                    UpdateConnectionStatus();
                    AddLog("Connexion automatique a Inventor reussie!", "SUCCESS");
                }
                else
                {
                    AddLog("Inventor non detecte - Demarrez Inventor pour activer les fonctions", "WARN");
                }
            }
            catch (Exception ex)
            {
                AddLog("Inventor non disponible: " + ex.Message, "WARN");
            }
        }

        private void UpdateConnectionStatus()
        {
            // Couleurs pour bouton Connecter
            var redBtnBrush = new SolidColorBrush(Color.FromRgb(232, 17, 35));      // Rouge clair #E81123
            var greenBtnBrush = new SolidColorBrush(Color.FromRgb(16, 185, 16));    // Vert brillant #10B910
            
            if (_isVaultConnected && _vaultService.IsConnected)
            {
                VaultIndicator.Fill = _greenBrush;
                RunVaultName.Text = $" Vault: {_vaultService.VaultName}";
                RunUserName.Text = $" {_vaultService.UserName}";
                RunStatus.Text = " Connecte";
                ConnectButton.Content = "Deconnecter";
                ConnectButton.Background = greenBtnBrush;  // Vert brillant quand connecté
            }
            else
            {
                VaultIndicator.Fill = _redBrush;
                RunVaultName.Text = " Vault: --";
                RunUserName.Text = " --";
                RunStatus.Text = " Deconnecte";
                ConnectButton.Content = "Connecter";
                ConnectButton.Background = redBtnBrush;    // Rouge clair quand déconnecté
            }

            if (_isInventorConnected && _inventorService.IsConnected)
            {
                InventorIndicator.Fill = _greenBrush;
                string docName = _inventorService.GetActiveDocumentName();
                InventorStatusText.Text = !string.IsNullOrEmpty(docName) ? docName : "Connecte";
                
                // Arrêter le timer de reconnexion si on est connecté
                _inventorReconnectTimer?.Stop();
            }
            else
            {
                InventorIndicator.Fill = _redBrush;
                InventorStatusText.Text = "Deconnecte";
            }
        }

        /// <summary>
        /// Timer de reconnexion automatique à Inventor
        /// Se déclenche toutes les 3 secondes si Inventor n'est pas connecté
        /// Optimise pour eviter le spam de logs
        /// </summary>
        private void InventorReconnectTimer_Tick(object? sender, EventArgs e)
        {
            // Vérifier si Inventor est en cours d'exécution
            var inventorProcesses = Process.GetProcessesByName("Inventor");
            if (inventorProcesses.Length == 0)
            {
                // Inventor pas lancé, ne pas spammer les logs
                return;
            }

            // Tentative de reconnexion (le throttling est gere dans InventorService)
            if (_inventorService.TryConnect())
            {
                _isInventorConnected = true;
                string version = _inventorService.GetInventorVersion() ?? "";
                
                AddLog("[+] Connexion a Inventor etablie!", "SUCCESS");
                if (!string.IsNullOrEmpty(version))
                {
                    AddLog("   [i] Version: " + version, "INFO");
                }
                
                _inventorReconnectTimer?.Stop();
                UpdateConnectionStatus();
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isVaultConnected)
            {
                AddLog("Deconnexion de Vault...", "INFO");
                _vaultService.Disconnect();
                _isVaultConnected = false;
                StatusText.Text = "Deconnecte de Vault";
                AddLog("Deconnecte de Vault", "SUCCESS");
            }
            else
            {
                AddLog("Ouverture de la fenetre de connexion Vault...", "INFO");
                var loginWindow = new LoginWindow(_vaultService);
                loginWindow.Owner = this;
                if (loginWindow.ShowDialog() == true)
                {
                    _isVaultConnected = true;
                    StatusText.Text = "Connecte a " + _vaultService.VaultName;
                    AddLog("Connecte a Vault: " + _vaultService.VaultName + " (" + _vaultService.UserName + ")", "SUCCESS");
                    AddLog("Cliquez sur Update Workspace pour synchroniser les bibliotheques", "INFO");
                }
                else
                {
                    AddLog("Connexion Vault annulee", "WARN");
                }
            }
            UpdateConnectionStatus();
        }

        private async void OpenVaultUpload_Click(object sender, RoutedEventArgs e)
        {
            // [+] Verifier restrictions admin AVANT d'ouvrir le module
            if (!await CheckModuleRestrictionsAsync("vaultUpload", "Upload Module"))
            {
                return;
            }
            
            AddLog("Ouverture de l'outil Upload Module vers Vault...", "START");
            StatusText.Text = "Upload Module...";
            
            try
            {
                // [+] Tracker l'utilisation du module dans Firebase
                _ = FirebaseAuditService.Instance.TrackModuleUsageAsync("vaultUpload", "Module opened");
                
                // Ouvrir le module integre (plus besoin de l'exe externe)
                var uploadWindow = new Modules.UploadModule.Views.UploadModuleWindow(_isVaultConnected ? _vaultService : null);
                uploadWindow.Owner = this;
                
                AddLog("Fenetre Upload Module ouverte", "INFO");
                AddLog("Options disponibles:", "INFO");
                AddLog("  - Selection de module depuis C:\\Vault\\Engineering\\Projects", "INFO");
                AddLog("  - Parcourir dossier local", "INFO");
                AddLog("  - Depuis Inventor (document actif)", "INFO");
                
                var result = uploadWindow.ShowDialog();
                
                if (result == true)
                {
                    AddLog("Upload Module termine avec succes!", "SUCCESS");
                    StatusText.Text = "Upload termine";
                }
                else
                {
                    AddLog("Upload Module ferme", "INFO");
                    StatusText.Text = "Upload annule";
                }
            }
            catch (Exception ex)
            {
                AddLog("Erreur ouverture Upload Module: " + ex.Message, "ERROR");
                StatusText.Text = "Erreur";
            }
        }

        private async void OpenUploadTemplate_Click(object sender, RoutedEventArgs e)
        {
            // [+] Verifier restrictions admin AVANT d'ouvrir le module
            if (!await CheckModuleRestrictionsAsync("uploadTemplate", "Upload Template"))
            {
                return;
            }
            
            AddLog("Ouverture de l'outil Upload Template...", "START");
            StatusText.Text = "Upload Template...";
            
            try
            {
                // [+] Tracker l'utilisation du module dans Firebase
                _ = FirebaseAuditService.Instance.TrackModuleUsageAsync("uploadTemplate", "Module opened");
                
                // Verifier connexion Vault d'abord
                if (!_isVaultConnected || _vaultService == null)
                {
                    XnrgyMessageBox.ShowWarning(
                        "Veuillez d'abord vous connecter au Vault.",
                        "Connexion Vault requise",
                        this);
                    AddLog("Upload Template - Connexion Vault requise", "WARN");
                    StatusText.Text = "Connexion requise";
                    return;
                }

                // Verifier les droits admin via l'API Vault (meme logique que Creer Module)
                bool isAdmin = _vaultService.IsCurrentUserAdmin();
                string currentUser = _vaultService.GetCurrentUsername();

                if (!isAdmin)
                {
                    XnrgyMessageBox.ShowInfo(
                        "Cette fonctionnalite est reservee aux Administrateurs Vault.\n\n" +
                        "Vous devez etre membre du groupe 'Administrators' ou 'Admin_Designer'\n" +
                        "pour acceder a l'Upload Template.\n\n" +
                        "Si vous avez une demande d'amelioration ou besoin d'acces,\n" +
                        "veuillez contacter les Admins ou le developpeur de l'application.\n\n" +
                        "Contact: mohammedamine.elgalai@xnrgy.com",
                        "Acces Reserve - Admin Only",
                        this);
                    AddLog($"[!] Acces refuse pour '{currentUser}' - fonctionnalite reservee aux admins Vault", "WARN");
                    StatusText.Text = "Acces refuse";
                    return;
                }
                
                AddLog($"[+] Acces admin confirme pour '{currentUser}'", "SUCCESS");

                // Ouvrir la fenetre Upload Template avec le service partage
                var uploadTemplateWindow = new UploadTemplateWindow(_vaultService);
                uploadTemplateWindow.Owner = this;
                
                AddLog("Fenetre Upload Template ouverte", "INFO");
                AddLog("Fonctionnalite: Upload de templates vers Vault PROD", "INFO");
                
                var result = uploadTemplateWindow.ShowDialog();
                
                if (result == true)
                {
                    AddLog("Upload Template termine avec succes!", "SUCCESS");
                    StatusText.Text = "Upload termine";
                }
                else
                {
                    AddLog("Upload Template ferme", "INFO");
                    StatusText.Text = "Upload annule";
                }
            }
            catch (Exception ex)
            {
                AddLog("Erreur ouverture Upload Template: " + ex.Message, "ERROR");
                StatusText.Text = "Erreur";
            }
        }

        private async void OpenPackAndGo_Click(object sender, RoutedEventArgs e)
        {
            // [+] Verifier restrictions admin AVANT d'ouvrir le module
            if (!await CheckModuleRestrictionsAsync("copyDesign", "Creer Module"))
            {
                return;
            }
            
            AddLog("Ouverture de l'outil Créer Module...", "START");
            StatusText.Text = "Création de module...";
            
            try
            {
                // [+] Tracker l'utilisation du module dans Firebase
                _ = FirebaseAuditService.Instance.TrackModuleUsageAsync("copyDesign", "Module opened");
                
                // [+] Rafraîchir la connexion COM Inventor avant d'ouvrir le module
                if (_isInventorConnected)
                {
                    AddLog("[>] Rafraichissement connexion Inventor...", "DEBUG");
                    _inventorService.ForceReconnect();
                    AddLog("[+] Connexion Inventor rafraichie", "DEBUG");
                }
                
                // Passer les services Vault et Inventor pour héritage du statut de connexion
                var createModuleWindow = new CreateModuleWindow(
                    _isVaultConnected ? _vaultService : null,
                    _isInventorConnected ? _inventorService : null);
                createModuleWindow.Owner = this;
                
                AddLog("Fenêtre Créer Module ouverte", "INFO");
                AddLog("Options disponibles:", "INFO");
                AddLog("  - Créer depuis Template ($/Engineering/Library)", "INFO");
                AddLog("  - Créer depuis Projet Existant", "INFO");
                
                var result = createModuleWindow.ShowDialog();
                
                if (result == true)
                {
                    AddLog("Module créé avec succès!", "SUCCESS");
                    StatusText.Text = "Module créé";
                }
                else
                {
                    AddLog("Création de module annulée", "INFO");
                    StatusText.Text = "Création annulée";
                }
            }
            catch (Exception ex)
            {
                AddLog("Erreur ouverture Créer Module: " + ex.Message, "ERROR");
                StatusText.Text = "Erreur";
            }
        }

        private async void OpenPlaceEquipment_Click(object sender, RoutedEventArgs e)
        {
            // [VaultForge] Module non disponible dans cette version
            AddLog("Module Place Equipment non disponible dans VaultForge", "INFO");
            XnrgyMessageBox.Show(
                "Ce module n'est pas disponible dans VaultForge.\nContactez le support pour plus d'informations.",
                "Module Place Equipment",
                XnrgyMessageBoxType.Info,
                XnrgyMessageBoxButtons.OK);
            await Task.CompletedTask;
        }

        private async void OpenSmartTools_Click(object sender, RoutedEventArgs e)
        {
            // [+] Verifier restrictions admin AVANT d'ouvrir le module
            if (!await CheckModuleRestrictionsAsync("smartTools", "Smart Tools"))
            {
                return;
            }
            
            AddLog("Ouverture de Smart Tools...", "START");
            StatusText.Text = "Smart Tools...";
            
            try
            {
                // [+] Tracker l'utilisation du module dans Firebase
                _ = FirebaseAuditService.Instance.TrackModuleUsageAsync("smartTools", "Module opened");
                
                // [+] Rafraîchir la connexion COM Inventor avant d'ouvrir le module
                if (_isInventorConnected)
                {
                    AddLog("[>] Rafraichissement connexion Inventor...", "DEBUG");
                    _inventorService.ForceReconnect();
                    AddLog("[+] Connexion Inventor rafraichie", "DEBUG");
                }
                
                // Passer le service Vault et le callback de log pour affichage du statut de connexion
                var smartToolsWindow = new SmartToolsWindow(_isVaultConnected ? _vaultService : null, AddLog);
                smartToolsWindow.Owner = this;
                smartToolsWindow.Show();
                
                AddLog("Smart Tools ouvert avec succes", "SUCCESS");
            }
            catch (Exception ex)
            {
                AddLog($"Erreur lors de l'ouverture de Smart Tools: {ex.Message}", "ERROR");
                XnrgyMessageBox.ShowError($"Erreur lors de l'ouverture de Smart Tools:\n{ex.Message}", 
                    "Erreur", this);
            }
        }
        
        private async void OpenBuildModule_Click(object sender, RoutedEventArgs e)
        {
            // [VaultForge] Module non disponible dans cette version
            AddLog("Module Build Module non disponible dans VaultForge", "INFO");
            XnrgyMessageBox.Show(
                "Ce module n'est pas disponible dans VaultForge.\nContactez le support pour plus d'informations.",
                "Module Build Module",
                XnrgyMessageBoxType.Info,
                XnrgyMessageBoxButtons.OK);
            await Task.CompletedTask;
        }

        private async void OpenDXFVerifier_Click(object sender, RoutedEventArgs e)
        {
            // [+] Verifier restrictions admin AVANT d'ouvrir le module
            if (!await CheckModuleRestrictionsAsync("dxfVerifier", "DXF Verifier"))
            {
                return;
            }
            
            AddLog("Ouverture de DXF Verifier...", "START");
            StatusText.Text = "Ouverture de DXF Verifier...";
            try
            {
                // [+] Tracker l'utilisation du module dans Firebase
                _ = FirebaseAuditService.Instance.TrackModuleUsageAsync("dxfVerifier", "Module opened");
                
                // Forcer reconnexion Inventor si connecte
                if (_isInventorConnected)
                {
                    _inventorService.ForceReconnect();
                }
                
                var dxfVerifierWindow = new DXFVerifierWindow(
                    _isVaultConnected ? _vaultService : null,
                    _isInventorConnected ? _inventorService : null);
                dxfVerifierWindow.Owner = this;
                dxfVerifierWindow.Show();
                AddLog("[+] DXF Verifier ouvert avec succes", "SUCCESS");
                StatusText.Text = "DXF Verifier ouvert";
            }
            catch (Exception ex)
            {
                AddLog("[-] Erreur ouverture DXFVerifier: " + ex.Message, "CRITICAL");
                StatusText.Text = "Erreur";
            }
        }

        private async void OpenChecklistHVAC_Click(object sender, RoutedEventArgs e)
        {
            // [VaultForge] Module non disponible dans cette version
            AddLog("Module Checklist HVAC non disponible dans VaultForge", "INFO");
            XnrgyMessageBox.Show(
                "Ce module n'est pas disponible dans VaultForge.\nContactez le support pour plus d'informations.",
                "Module Checklist HVAC",
                XnrgyMessageBoxType.Info,
                XnrgyMessageBoxButtons.OK);
            await Task.CompletedTask;
        }

        private async void OpenACP_Click(object sender, RoutedEventArgs e)
        {
            // [+] Verifier restrictions admin AVANT d'ouvrir le module
            if (!await CheckModuleRestrictionsAsync("acp", "ACP"))
            {
                return;
            }
            
            // ACP - Fonctionnalite en cours de developpement
            AddLog("Module ACP (Assistant de Conception de Projet)...", "START");
            StatusText.Text = "ACP - En developpement";
            
            // [+] Tracker l'utilisation du module dans Firebase (meme en dev pour stats)
            _ = FirebaseAuditService.Instance.TrackModuleUsageAsync("acp", "Module opened (dev)");
            
            XnrgyMessageBox.ShowInfo(
                "Fonctionnalite en Developpement",
                "Le module ACP (Assistant de Conception de Projet) est actuellement en cours de developpement.\n\n" +
                "Ce module permettra de:\n" +
                "[+] Gerer les points critiques des modules\n" +
                "[+] Suivre l'avancement des conceptions\n" +
                "[+] Generer des rapports de projet\n\n" +
                "Disponible dans une prochaine version.",
                this
            );
            
            AddLog("ACP: Fonctionnalite en cours de developpement", "WARN");
            StatusText.Text = "Pret";
        }

        private async void OpenConfigUnits_Click(object sender, RoutedEventArgs e)
        {
            // [+] Verifier restrictions admin AVANT d'ouvrir le module
            if (!await CheckModuleRestrictionsAsync("configUnits", "Config Unites"))
            {
                return;
            }
            
            AddLog("Module Config Unites...", "START");
            StatusText.Text = "Ouverture Config Unites...";
            
            try
            {
                // Ouvrir la fenêtre Config Unité
                var configWindow = new ConfigUniteWindow(_vaultService);
                configWindow.Owner = this;
                configWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                configWindow.Show();
                
                AddLog("Config Unites: Fenetre ouverte", "SUCCESS");
                StatusText.Text = "Pret";
                
                // [+] Tracker l'utilisation du module dans Firebase
                _ = FirebaseAuditService.Instance.TrackModuleUsageAsync("configUnits", "Module opened");
            }
            catch (Exception ex)
            {
                AddLog($"Erreur ouverture Config Unites: {ex.Message}", "ERROR");
                StatusText.Text = "Erreur";
                XnrgyMessageBox.ShowError($"Erreur lors de l'ouverture du module Config Unites:\n\n{ex.Message}", "Erreur", this);
            }
        }

        private async void OpenVaultProject_Click(object sender, RoutedEventArgs e)
        {
            // [+] Verifier restrictions admin AVANT d'ouvrir le module
            if (!await CheckModuleRestrictionsAsync("openProject", "Ouvrir Projet"))
            {
                return;
            }
            
            if (!CheckVaultConnection()) return;
            if (!CheckInventorConnection()) return;
            
            AddLog("Ouverture de Ouvrir Projet Vault...", "START");
            StatusText.Text = "Ouverture de Ouvrir Projet Vault...";
            
            try
            {
                // [+] Tracker l'utilisation du module dans Firebase
                _ = FirebaseAuditService.Instance.TrackModuleUsageAsync("openProject", "Module opened");
                
                var openVaultWindow = new XnrgyEngineeringAutomationTools.Modules.OpenVaultProject.Views.OpenVaultProjectWindow(
                    _vaultService,
                    _inventorService
                );
                openVaultWindow.Owner = this;
                openVaultWindow.Show();
                
                AddLog("Ouvrir Projet Vault ouvert", "SUCCESS");
                StatusText.Text = "Ouvrir Projet Vault ouvert";
            }
            catch (Exception ex)
            {
                AddLog("Erreur ouverture Ouvrir Projet Vault: " + ex.Message, "CRITICAL");
                Logger.LogException("OpenVaultProject", ex, Logger.LogLevel.ERROR);
                StatusText.Text = "Erreur";
            }
        }

        private async void OpenNesting_Click(object sender, RoutedEventArgs e)
        {
            // [VaultForge] Module non disponible dans cette version
            AddLog("Module Nesting non disponible dans VaultForge", "INFO");
            XnrgyMessageBox.Show(
                "Ce module n'est pas disponible dans VaultForge.\nContactez le support pour plus d'informations.",
                "Module Nesting",
                XnrgyMessageBoxType.Info,
                XnrgyMessageBoxButtons.OK);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Bouton Update Workspace clique par l'utilisateur (mode MANUEL)
        /// L'utilisateur doit cliquer sur Demarrer et peut voir la progression
        /// </summary>
        private async void UpdateWorkspace_Click(object sender, RoutedEventArgs e)
        {
            // [+] Verifier restrictions admin AVANT d'ouvrir le module
            if (!await CheckModuleRestrictionsAsync("updateWorkspace", "Update Workspace"))
            {
                return;
            }
            
            if (!CheckVaultConnection()) return;
            
            // [+] Tracker l'utilisation du module dans Firebase
            _ = FirebaseAuditService.Instance.TrackModuleUsageAsync("updateWorkspace", "Module opened");
            
            // Ouvrir la fenetre de mise a jour du workspace en mode MANUEL
            // L'utilisateur doit cliquer sur Demarrer, et la fenetre reste ouverte apres
            ShowUpdateWorkspaceWindow(autoStart: false, autoCloseOnSuccess: false);
        }

        private bool CheckVaultConnection()
        {
            if (!_isVaultConnected || !_vaultService.IsConnected)
            {
                AddLog("CONNEXION VAULT REQUISE", "CRITICAL");
                AddLog("Cliquez sur Connecter pour vous connecter a Vault", "WARN");
                StatusText.Text = "Connexion Vault requise";
                return false;
            }
            return true;
        }

        private bool CheckInventorConnection()
        {
            if (!_isInventorConnected || !_inventorService.IsConnected)
            {
                AddLog("Inventor non connecte - Tentative de connexion...", "WARN");
                TryConnectInventorAuto();
                return _isInventorConnected;
            }
            return true;
        }

        /// <summary>
        /// Verifie si un module est autorise pour ce poste (restrictions admin)
        /// Retourne true si autorise, false si bloque
        /// </summary>
        /// <param name="moduleId">ID du module (vaultUpload, copyDesign, etc.)</param>
        /// <param name="moduleName">Nom affiche du module</param>
        private async Task<bool> CheckModuleRestrictionsAsync(string moduleId, string moduleName)
        {
            try
            {
                // Verifier les restrictions du device dans Firebase
                var isAllowed = await FirebaseAuditService.Instance.CheckModuleAccessAsync(moduleId, moduleName);
                
                if (!isAllowed)
                {
                    // Module bloque par l'admin
                    AddLog($"[!] Module {moduleName} BLOQUE par l'administrateur", "CRITICAL");
                    AddLog($"    Ce poste n'est pas autorise a utiliser ce module.", "WARN");
                    StatusText.Text = $"{moduleName} non autorise";
                    
                    // Afficher un message a l'utilisateur avec XnrgyMessageBox
                    XnrgyMessageBox.ShowWarning(
                        $"Le module '{moduleName}' a ete desactive pour ce poste par l'administrateur.\n\n" +
                        "Contactez votre administrateur si vous avez besoin d'acces a ce module.",
                        "Acces Refuse",
                        this);
                    
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                // En cas d'erreur (pas de connexion internet, etc.), autoriser par defaut
                Logger.Log($"[!] Erreur verification restrictions (autorise par defaut): {ex.Message}", Logger.LogLevel.DEBUG);
                return true;
            }
        }
    }
}

