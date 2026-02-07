using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using XnrgyEngineeringAutomationTools.Modules.ConfigUnite.Data;
using XnrgyEngineeringAutomationTools.Modules.ConfigUnite.Models;
using XnrgyEngineeringAutomationTools.Modules.ConfigUnite.Services;
using XnrgyEngineeringAutomationTools.Services;
using XnrgyEngineeringAutomationTools.Shared.Views;
using ACW = Autodesk.Connectivity.WebServices;

namespace XnrgyEngineeringAutomationTools.Modules.ConfigUnite.Views
{
    /// <summary>
    /// Fenêtre de configuration d'unité (Master) - Centralise les propriétés communes à tous les modules d'une unité
    /// Les données sont sauvegardées en JSON et peuvent être uploadées vers Vault
    /// </summary>
    public partial class ConfigUniteWindow : Window
    {
        private readonly ConfigUniteService _configService;
        private readonly VaultSdkService? _vaultService;
        private readonly InventorService _inventorService;
        private ConfigUniteDataModel _currentConfig;
        private string _currentConfigPath = "";
        private System.Windows.Threading.DispatcherTimer? _inventorStatusTimer;
        
        /// <summary>
        /// Liste des dessinateurs pour le DataGrid des modules
        /// </summary>
        public List<string> DrafterList { get; set; } = new List<string>();

        public ConfigUniteWindow(VaultSdkService? vaultService = null)
        {
            InitializeComponent();
            
            Logger.Log("[ConfigUnite] [>] Ouverture du module Config Unite...", Logger.LogLevel.INFO);
            
            _vaultService = vaultService;
            _inventorService = new InventorService();
            
            // [+] Forcer la reconnexion COM à chaque ouverture (comme CreateModuleWindow)
            _inventorService.ForceReconnect();
            
            _configService = new ConfigUniteService(vaultService);
            _currentConfig = new ConfigUniteDataModel();

            // Initialiser les ComboBox avec les valeurs par défaut
            InitializeComboBoxes();

            // [+] Initialiser les ComboBox Project/Reference (Phase 1)
            InitializeProjectReferenceComboBoxes();

            // Initialiser les dates avec DatePickers
            DpConfigDate.SelectedDate = DateTime.Now;
            DpDrawingSubmittalDate.SelectedDate = DateTime.Now;
            // Note: TxtCreatedDate supprime - le groupe Template Creator Info a ete retire

            // Définir le DataContext pour le binding
            DataContext = _currentConfig;

            // Mettre à jour le statut
            UpdateStatus("Prêt - Nouvelle configuration");

            // S'abonner aux événements
            Loaded += ConfigUniteWindow_Loaded;
            Closed += ConfigUniteWindow_Closed;

            // Mettre à jour les statuts de connexion
            UpdateConnectionStatuses();

            // Initialiser la DataGrid des modules
            DgModuleDimensions.ItemsSource = _currentConfig.ModuleDimensions;
            
            // Permettre la suppression avec la touche Delete
            DgModuleDimensions.PreviewKeyDown += DgModuleDimensions_PreviewKeyDown;
            
            // S'abonner a l'evenement StackedModeChanged du visualizer pour afficher/masquer les onglets
            ModuleVisualizer.StackedModeChanged += ModuleVisualizer_StackedModeChanged;
            
            // S'abonner aux changements de theme
            MainWindow.ThemeChanged += OnThemeChanged;
            this.Closed += (s, e) => MainWindow.ThemeChanged -= OnThemeChanged;
            
            // Appliquer le theme actuel
            ApplyTheme(MainWindow.CurrentThemeIsDark);
        }

        private void OnThemeChanged(bool isDarkTheme)
        {
            Dispatcher.Invoke(() => ApplyTheme(isDarkTheme));
        }

        private void ApplyTheme(bool isDarkTheme)
        {
            if (isDarkTheme)
            {
                // Theme SOMBRE
                this.Background = new SolidColorBrush(Color.FromRgb(30, 30, 46)); // #1E1E2E
            }
            else
            {
                // Theme CLAIR
                this.Background = new SolidColorBrush(Color.FromRgb(245, 247, 250)); // #F5F7FA
            }
        }

        /// <summary>
        /// Handler quand le mode Stacked change dans le visualizer
        /// </summary>
        private void ModuleVisualizer_StackedModeChanged(object sender, bool isStacked)
        {
            VisualizerTabButtons.Visibility = isStacked ? Visibility.Visible : Visibility.Collapsed;
            Logger.Log($"[ConfigUnite] [>] Mode Stacked: {isStacked} - Onglets header: {(isStacked ? "Visible" : "Cache")}", Logger.LogLevel.DEBUG);
        }

        private void ConfigUniteWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Activer le scroll molette pour TOUS les ScrollViewers de la fenêtre
            // A CE MOMENT le visual tree est construit et on peut trouver tous les ScrollViewers
            var scrollViewers = FindVisualChildren<ScrollViewer>(this).ToList();
            Logger.Log($"[ConfigUnite] [>] Activation scroll molette pour {scrollViewers.Count} ScrollViewers", Logger.LogLevel.INFO);
            
            foreach (var sv in scrollViewers)
            {
                sv.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
            }
            
            UpdateConnectionStatuses();
            
            // Initialiser le visualiseur avec la config complete (modules + tunnels + interior walls)
            ModuleVisualizer.UpdateWithConfig(_currentConfig);
            
            // Timer pour mettre à jour le statut Inventor périodiquement
            _inventorStatusTimer = new System.Windows.Threading.DispatcherTimer();
            _inventorStatusTimer.Interval = TimeSpan.FromSeconds(3);
            _inventorStatusTimer.Tick += (s, args) => UpdateInventorStatus();
            _inventorStatusTimer.Start();
        }

        private void ConfigUniteWindow_Closed(object sender, EventArgs e)
        {
            _inventorStatusTimer?.Stop();
            Logger.Log("[ConfigUnite] [i] Module Config Unite ferme", Logger.LogLevel.INFO);
        }

        /// <summary>
        /// Met à jour les statuts de connexion Vault et Inventor
        /// </summary>
        private void UpdateConnectionStatuses()
        {
            UpdateVaultConnectionStatus();
            UpdateInventorStatus();
        }

        /// <summary>
        /// Met à jour l'indicateur de connexion Vault dans l'en-tête
        /// </summary>
        private void UpdateVaultConnectionStatus()
        {
            Dispatcher.Invoke(() =>
            {
                bool isConnected = _vaultService != null && _vaultService.IsConnected;
                
                if (VaultStatusIndicator != null)
                {
                    VaultStatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(
                        isConnected ? System.Windows.Media.Color.FromRgb(16, 124, 16) : System.Windows.Media.Color.FromRgb(232, 17, 35));
                }
                
                if (RunVaultName != null && RunUserName != null && RunStatus != null)
                {
                    if (isConnected && _vaultService != null)
                    {
                        RunVaultName.Text = $" Vault: {_vaultService.VaultName ?? "--"}";
                        RunUserName.Text = $" {_vaultService.UserName ?? "--"}";
                        RunStatus.Text = " Connecte";
                        RunStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(144, 238, 144));
                    }
                    else
                    {
                        RunVaultName.Text = " Vault: --";
                        RunUserName.Text = " --";
                        RunStatus.Text = " Deconnecte";
                        RunStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                    }
                }
            });
        }

        /// <summary>
        /// Met à jour l'indicateur de connexion Inventor dans l'en-tête
        /// </summary>
        private void UpdateInventorStatus()
        {
            Dispatcher.Invoke(() =>
            {
                bool isConnected = _inventorService.IsConnected;
                
                if (InventorIndicator != null)
                {
                    InventorIndicator.Fill = new System.Windows.Media.SolidColorBrush(
                        isConnected ? System.Windows.Media.Color.FromRgb(16, 124, 16) : System.Windows.Media.Color.FromRgb(232, 17, 35));
                }
                
                if (RunInventorStatus != null)
                {
                    if (isConnected)
                    {
                        // Récupérer le nom du fichier actif
                        string? activeFileName = _inventorService.GetActiveDocumentName();
                        if (!string.IsNullOrEmpty(activeFileName))
                        {
                            // Tronquer si trop long
                            if (activeFileName.Length > 25)
                                activeFileName = activeFileName.Substring(0, 22) + "...";
                            RunInventorStatus.Text = $" Inventor : {activeFileName}";
                        }
                        else
                        {
                            RunInventorStatus.Text = " Inventor : Connecte";
                        }
                    }
                    else
                    {
                        RunInventorStatus.Text = " Inventor : Deconnecte";
                    }
                }
            });
        }

        /// <summary>
        /// Initialise les ComboBox avec les valeurs possibles (depuis ParameterLists.cs)
        /// </summary>
        private void InitializeComboBoxes()
        {
            // ====================================================================
            // TAB: Design Info & Revision - Lead CAD, Drawn By
            // ====================================================================
            CmbLeadCAD.ItemsSource = ParameterLists.DesignerInitials;
            CmbLeadCAD.SelectedItem = ParameterLists.DesignerInitials_Default;
            CmbDrawnBy.ItemsSource = ParameterLists.DesignerInitials;
            CmbDrawnBy.SelectedItem = ParameterLists.DesignerInitials_Default;
            
            // Liste des drafters pour le DataGrid des modules
            DrafterList = ParameterLists.DesignerInitials;

            // ====================================================================
            // TAB: Unit Info - Unit Specification
            // ====================================================================
            CmbUnitType.ItemsSource = ParameterLists.UnitType;
            CmbUnitOption.ItemsSource = ParameterLists.UnitOption;
            CmbUnitConfiguration.ItemsSource = ParameterLists.UnitConfiguration;
            CmbUnitCertification.ItemsSource = ParameterLists.UnitCertification;
            CmbStaticPressure.ItemsSource = ParameterLists.StaticPressure;
            CmbStaticPressure.SelectedItem = ParameterLists.StaticPressure_Default;

            // Air Flow - pour tunnels
            CmbAirFlowRight.ItemsSource = ParameterLists.AirFlowDirection;
            CmbAirFlowLeft.ItemsSource = ParameterLists.AirFlowDirection;
            CmbAirFlowMiddle.ItemsSource = ParameterLists.AirFlowDirection;

            // ====================================================================
            // TAB: Floor Info
            // ====================================================================
            CmbBaseConstruction.ItemsSource = ParameterLists.BaseConstruction;
            CmbBaseInsulation.ItemsSource = ParameterLists.FloorInsulation;
            CmbBaseCoating.ItemsSource = ParameterLists.CoatingFloor;
            CmbBaseThermalBreak.ItemsSource = ParameterLists.BaseThermalBreak;
            CmbFloorConstruction.ItemsSource = ParameterLists.FloorConstruction;
            CmbFloorLiner.ItemsSource = ParameterLists.FloorLiner;
            CmbSubfloorLiner.ItemsSource = ParameterLists.SubfloorLiner;
            CmbFloorMountType.ItemsSource = ParameterLists.FloorMountType;
            
            // Set default for Floor Height
            TxtFloorHeight.Text = ParameterLists.FloorHeight_Default;

            // ====================================================================
            // TAB: Casing Info
            // ====================================================================
            CmbPanelConstruction.ItemsSource = ParameterLists.PanelConstruction;
            CmbPanelWidth.ItemsSource = ParameterLists.PanelWidth;
            CmbPanelWidth.SelectedItem = ParameterLists.PanelWidth_Default;
            CmbPanelInsulation.ItemsSource = ParameterLists.PanelInsulation;
            
            // Panel & Liner Materials
            CmbWallPanelMaterial.ItemsSource = ParameterLists.WallPanelMaterial;
            CmbWallLinerMaterial.ItemsSource = ParameterLists.WallLinerMaterial;
            CmbRoofPanelMtl.ItemsSource = ParameterLists.RoofPanelMaterial;
            CmbRoofLinerMtl.ItemsSource = ParameterLists.RoofLinerMaterial;

            // Additional Casing
            CmbCoating.ItemsSource = ParameterLists.Coating;
            CmbWallMaterial.ItemsSource = ParameterLists.WallMaterial;

            // ====================================================================
            // TAB: Miscellaneous
            // ====================================================================
            CmbHardwareMaterial.ItemsSource = ParameterLists.HardwareMaterial;

            // Shrink Wrap et Sealant - valeurs personnalisees (pas dans ParameterLists)
            CmbShrinkWrap.ItemsSource = new List<string> { "None/Aucun", "Yes/Oui", "No/Non" };
            CmbSealant.ItemsSource = new List<string> { "None/Aucun", "Polyether", "Silicone", "Special (See Note)" };

            // ====================================================================
            // TAB: Wall Specification
            // ====================================================================
            // Exterior Walls
            CmbBackWallPanelWidth.ItemsSource = ParameterLists.PanelWidth;
            CmbBackWallPanelMaterial.ItemsSource = ParameterLists.WallPanelMaterial;
            CmbBackWallLinerMaterial.ItemsSource = ParameterLists.WallLinerMaterial;
            
            CmbFrontWallPanelWidth.ItemsSource = ParameterLists.PanelWidth;
            CmbFrontWallPanelMaterial.ItemsSource = ParameterLists.WallPanelMaterial;
            CmbFrontWallLinerMaterial.ItemsSource = ParameterLists.WallLinerMaterial;
            
            CmbRightWallPanelWidth.ItemsSource = ParameterLists.PanelWidth;
            CmbRightWallPanelMaterial.ItemsSource = ParameterLists.WallPanelMaterial;
            CmbRightWallLinerMaterial.ItemsSource = ParameterLists.WallLinerMaterial;
            
            CmbLeftWallPanelWidth.ItemsSource = ParameterLists.PanelWidth;
            CmbLeftWallPanelMaterial.ItemsSource = ParameterLists.WallPanelMaterial;
            CmbLeftWallLinerMaterial.ItemsSource = ParameterLists.WallLinerMaterial;
            
            CmbRoofPanelWidth.ItemsSource = ParameterLists.PanelWidth;
            CmbRoofPanelMaterial.ItemsSource = ParameterLists.RoofPanelMaterial;
            CmbRoofLinerMaterial.ItemsSource = ParameterLists.RoofLinerMaterial;
            
            // Interior Wall 01
            CmbInteriorWall01PanelInsulation.ItemsSource = ParameterLists.PanelInsulation;
            CmbInteriorWall01PanelConstruction.ItemsSource = ParameterLists.PanelConstruction;
            CmbInteriorWall01PanelMaterial.ItemsSource = ParameterLists.WallPanelMaterial;
            CmbInteriorWall01LinerMaterial.ItemsSource = ParameterLists.WallLinerMaterial;
            
            // Interior Wall 02
            CmbInteriorWall02PanelInsulation.ItemsSource = ParameterLists.PanelInsulation;
            CmbInteriorWall02PanelConstruction.ItemsSource = ParameterLists.PanelConstruction;
            CmbInteriorWall02PanelMaterial.ItemsSource = ParameterLists.WallPanelMaterial;
            CmbInteriorWall02LinerMaterial.ItemsSource = ParameterLists.WallLinerMaterial;

            // Tunnel Walls - Right/Left/Middle Tunnel (STEP_04)
            // Right Tunnel - Wall 01
            CmbRightTunnelWall01PanelInsulation.ItemsSource = ParameterLists.PanelInsulation;
            CmbRightTunnelWall01PanelConstruction.ItemsSource = ParameterLists.PanelConstruction;
            CmbRightTunnelWall01PanelMaterial.ItemsSource = ParameterLists.WallPanelMaterial;
            CmbRightTunnelWall01LinerMaterial.ItemsSource = ParameterLists.WallLinerMaterial;
            
            // Right Tunnel - Wall 02
            CmbRightTunnelWall02PanelInsulation.ItemsSource = ParameterLists.PanelInsulation;
            CmbRightTunnelWall02PanelConstruction.ItemsSource = ParameterLists.PanelConstruction;
            CmbRightTunnelWall02PanelMaterial.ItemsSource = ParameterLists.WallPanelMaterial;
            CmbRightTunnelWall02LinerMaterial.ItemsSource = ParameterLists.WallLinerMaterial;
            
            // Left Tunnel - Wall 01
            CmbLeftTunnelWall01PanelInsulation.ItemsSource = ParameterLists.PanelInsulation;
            CmbLeftTunnelWall01PanelConstruction.ItemsSource = ParameterLists.PanelConstruction;
            CmbLeftTunnelWall01PanelMaterial.ItemsSource = ParameterLists.WallPanelMaterial;
            CmbLeftTunnelWall01LinerMaterial.ItemsSource = ParameterLists.WallLinerMaterial;
            
            // Left Tunnel - Wall 02
            CmbLeftTunnelWall02PanelInsulation.ItemsSource = ParameterLists.PanelInsulation;
            CmbLeftTunnelWall02PanelConstruction.ItemsSource = ParameterLists.PanelConstruction;
            CmbLeftTunnelWall02PanelMaterial.ItemsSource = ParameterLists.WallPanelMaterial;
            CmbLeftTunnelWall02LinerMaterial.ItemsSource = ParameterLists.WallLinerMaterial;
            
            // Middle Tunnel - Wall 01
            CmbMiddleTunnelWall01PanelInsulation.ItemsSource = ParameterLists.PanelInsulation;
            CmbMiddleTunnelWall01PanelConstruction.ItemsSource = ParameterLists.PanelConstruction;
            CmbMiddleTunnelWall01PanelMaterial.ItemsSource = ParameterLists.WallPanelMaterial;
            CmbMiddleTunnelWall01LinerMaterial.ItemsSource = ParameterLists.WallLinerMaterial;
            
            // Middle Tunnel - Wall 02
            CmbMiddleTunnelWall02PanelInsulation.ItemsSource = ParameterLists.PanelInsulation;
            CmbMiddleTunnelWall02PanelConstruction.ItemsSource = ParameterLists.PanelConstruction;
            CmbMiddleTunnelWall02PanelMaterial.ItemsSource = ParameterLists.WallPanelMaterial;
            CmbMiddleTunnelWall02LinerMaterial.ItemsSource = ParameterLists.WallLinerMaterial;
            // Note: Les tunnels utilisent InteriorWallDetail qui a déjà les ItemsSource configurés
            // via les ComboBox individuels (CmbRightTunnelWall01PanelInsulation, etc.)
            
            // Additional Walls 03-05 (STEP_04)
            // Note: Les Additional Walls n'ont PAS de Panel Insulation/Construction/Material
            // Ils ont seulement: Include + 3 positions (Bottom, Left/Right/Front, Out)
            // Aucun ItemsSource nécessaire - ce sont des TextBox pour les positions

            // ====================================================================
            // TAB: Modular Brackets (STEP 5)
            // ====================================================================
            // Pas d'ItemsSource requis - tous les champs utilisent des bindings directes
        }

        /// <summary>
        /// Met à jour le statut affiché
        /// </summary>
        private void UpdateStatus(string message, bool isError = false)
        {
            TxtStatus.Text = message;
            TxtStatus.Foreground = isError 
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 17, 35))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(144, 238, 144));
        }

        /// <summary>
        /// Charge une configuration depuis un fichier JSON
        /// </summary>
        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Configuration Files (*.config)|*.config|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    Title = "Charger une configuration d'unité",
                    InitialDirectory = @"C:\Vault\Engineering\Projects"
                };

                if (dialog.ShowDialog() == true)
                {
                    var config = _configService.LoadFromFile(dialog.FileName);
                    if (config != null)
                    {
                        _currentConfig = config;
                        _currentConfigPath = dialog.FileName;
                        DataContext = _currentConfig;

                        // Mettre à jour les champs qui ne sont pas bindés automatiquement
                        UpdateUIFromConfig();

                        UpdateStatus($"Configuration chargée: {Path.GetFileName(dialog.FileName)}");
                        TxtConfigPath.Text = dialog.FileName;
                    }
                    else
                    {
                        XnrgyMessageBox.ShowError("Impossible de charger la configuration.\n\nVérifiez que le fichier est valide.", "Erreur", this);
                        UpdateStatus("Erreur lors du chargement", true);
                    }
                }
            }
            catch (Exception ex)
            {
                XnrgyMessageBox.ShowError($"Erreur lors du chargement:\n\n{ex.Message}", "Erreur", this);
                UpdateStatus("Erreur lors du chargement", true);
            }
        }

        /// <summary>
        /// Sauvegarde la configuration dans un fichier JSON + Upload automatique vers Vault
        /// Format: Config_Unites/[Projet][Reference].config (ex: 1035901.config)
        /// </summary>
        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Log("[ConfigUnite] [>] Sauvegarde de la configuration...", Logger.LogLevel.INFO);
                
                // Synchroniser l'UI avec le modèle avant sauvegarde
                UpdateConfigFromUI();

                // [+] Phase 1: Utiliser Project/Reference/JobTitle pour le chemin
                string project = CmbProject.Text?.Trim() ?? "";
                string referenceText = CmbReference.Text?.Trim() ?? "";
                string jobTitle = TxtJobTitle.Text?.Trim() ?? "";
                
                Logger.Log($"[ConfigUnite]    Projet: {project}, Reference: {referenceText}, Job: {jobTitle}", Logger.LogLevel.DEBUG);

                // Validation: Project et Reference sont requis
                if (string.IsNullOrEmpty(project))
                {
                    Logger.Log("[ConfigUnite] [!] Validation echouee: Projet manquant", Logger.LogLevel.WARNING);
                    XnrgyMessageBox.ShowWarning("Veuillez entrer ou selectionner un numero de Projet.", "Projet requis", this);
                    CmbProject.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(referenceText) || referenceText == "-- Nouvelle reference --")
                {
                    Logger.Log("[ConfigUnite] [!] Validation echouee: Reference manquante", Logger.LogLevel.WARNING);
                    XnrgyMessageBox.ShowWarning("Veuillez selectionner ou entrer une Reference (01-99).", "Reference requise", this);
                    CmbReference.Focus();
                    return;
                }

                // Normaliser la reference (retirer REF prefix si present, garder juste 2 chiffres)
                string reference = referenceText.Replace("REF", "").Trim();
                if (reference.Length == 1) reference = "0" + reference;

                // Mettre a jour le model avec les valeurs selectionnees
                _currentConfig.Project = project;
                _currentConfig.Reference = reference;
                _currentConfig.UnitName = jobTitle; // JobTitle = UnitName dans le model
                
                // Mettre aussi a jour DesignInfo.ProjectNumber pour coherence
                _currentConfig.DesignInfo.ProjectNumber = project;

                // Construire le chemin de sauvegarde centralise: Projects\Config_Unites\[Projet][Reference].config
                var configDir = CONFIG_UNITES_PATH;
                
                // Creer le dossier Config_Unites s'il n'existe pas
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                    UpdateStatus($"[+] Dossier cree: {configDir}");
                }

                // Nom du fichier: [Projet][Reference].config (ex: 1035901.config)
                string configFileName = $"{project}{reference}.config";
                _currentConfigPath = Path.Combine(configDir, configFileName);

                // Mettre a jour les metadonnees
                _currentConfig.LastModified = DateTime.Now;
                _currentConfig.LastModifiedBy = Environment.UserName;

                // ============================================
                // VAULT VERSIONING: GET+CheckOut -> Save -> CheckIn
                // ============================================
                // La sauvegarde locale se fait APRES le GET+CheckOut dans le service
                bool vaultSuccess = false;
                bool isNewVersion = false;
                string vaultStatus = "";
                string vaultMessage = "";
                
                if (_vaultService != null && _vaultService.IsConnected)
                {
                    UpdateStatus($"[>] Synchronisation Vault: {configFileName}...");
                    
                    try
                    {
                        // Workflow simplifie:
                        // 1. GET + CheckOut (Vault -> local)
                        // 2. Sauvegarder le modele (apres GET)
                        // 3. CheckIn (local -> Vault)
                        var (success, resultMessage) = await _configService.CheckoutUpdateCheckinAsync(
                            _currentConfig,  // Passer le modele directement
                            _currentConfigPath,
                            project,
                            reference,
                            jobTitle
                        );
                        
                        vaultSuccess = success;
                        vaultMessage = resultMessage;
                        
                        // Determiner si c'est une nouvelle version basee sur le message
                        isNewVersion = resultMessage.Contains("nouvelle version");
                        
                        if (vaultSuccess)
                        {
                            // Mettre a jour le chemin affiche
                            TxtConfigPath.Text = _currentConfigPath;
                            
                            // Rafraichir la liste des configs existantes
                            LoadAllExistingConfigs();
                            
                            if (isNewVersion)
                            {
                                vaultStatus = "[+] Sauvegarde OK (nouvelle version Vault)";
                                Logger.Log($"[ConfigUnite] [+] CheckIn Vault reussi: {configFileName} (version mise a jour)", Logger.LogLevel.INFO);
                                UpdateStatus($"[+] CheckIn Vault reussi: {configFileName} (version mise a jour)");
                            }
                            else
                            {
                                vaultStatus = "[+] Sauvegarde OK (premiere version Vault)";
                                Logger.Log($"[ConfigUnite] [+] Upload Vault reussi: {configFileName} (premiere version)", Logger.LogLevel.INFO);
                                UpdateStatus($"[+] Upload Vault reussi: {configFileName} (premiere version)");
                            }
                        }
                        else
                        {
                            vaultStatus = $"[-] ECHEC: {vaultMessage}";
                            Logger.Log($"[ConfigUnite] [-] Erreur Vault: {vaultMessage}", Logger.LogLevel.ERROR);
                            UpdateStatus($"[-] Erreur Vault: {vaultMessage}", true);
                        }
                    }
                    catch (Exception vaultEx)
                    {
                        vaultStatus = $"[-] Erreur: {vaultEx.Message}";
                        UpdateStatus($"[-] Erreur Vault: {vaultEx.Message}", true);
                        Logger.Log($"[ConfigUnite] Erreur Vault: {vaultEx.Message}", Logger.LogLevel.WARNING);
                    }
                }
                else
                {
                    // Vault non connecte - sauvegarde locale uniquement
                    UpdateStatus($"[>] Sauvegarde locale (Vault non connecte): {configFileName}...");
                    bool localSuccess = _configService.SaveToFile(_currentConfig, _currentConfigPath);
                    
                    if (localSuccess)
                    {
                        TxtConfigPath.Text = _currentConfigPath;
                        LoadAllExistingConfigs();
                        vaultStatus = "[i] Sauvegarde locale OK (Vault non connecte)";
                        UpdateStatus($"[+] Sauvegarde locale OK (Vault non connecte)");
                    }
                    else
                    {
                        vaultStatus = "[-] Echec sauvegarde locale";
                        UpdateStatus($"[-] Echec sauvegarde locale", true);
                    }
                }

                // ============================================
                // RESUME FINAL
                // ============================================
                string message = $"Configuration sauvegardee:\n\n";
                message += $"Fichier: {configFileName}\n";
                message += $"Projet: {project}\n";
                message += $"Reference: REF{reference}\n";
                message += $"Job Title: {jobTitle}\n\n";
                message += $"Status: {vaultStatus}";
                if (isNewVersion)
                {
                    message += "\n\n[i] Une nouvelle version a ete creee dans Vault.";
                }
                
                if (vaultSuccess || (_vaultService == null || !_vaultService.IsConnected))
                {
                    XnrgyMessageBox.ShowSuccess(message, "Sauvegarde reussie", this);
                }
                else
                {
                    XnrgyMessageBox.ShowWarning(message + "\n\nL'operation Vault a echoue. La configuration est sauvegardee localement.", "Sauvegarde partielle", this);
                }
            }
            catch (Exception ex)
            {
                XnrgyMessageBox.ShowError($"Erreur lors de la sauvegarde:\n\n{ex.Message}", "Erreur", this);
                UpdateStatus("[-] Erreur lors de la sauvegarde", true);
            }
        }

        /// <summary>
        /// Handler pour le bouton Appliquer du Visualizer (rafraichit la vue)
        /// </summary>
        private void BtnApplyVisualizer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Log("[ConfigUnite] [>] Bouton Appliquer Visualizer clique", Logger.LogLevel.DEBUG);
                
                // Commit les edits en cours dans le DataGrid
                if (DgModuleDimensions != null)
                {
                    DgModuleDimensions.CommitEdit(DataGridEditingUnit.Row, true);
                    DgModuleDimensions.Items.Refresh();
                }
                
                // Rafraichir le visualizer avec la config complete (modules + tunnels + interior walls)
                ModuleVisualizer.UpdateWithConfig(_currentConfig);
                
                UpdateStatus($"[+] Visualisation rafraichie ({_currentConfig.ModuleDimensions.Count} module(s))");
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConfigUnite] [-] Erreur refresh visualizer: {ex.Message}", Logger.LogLevel.ERROR);
            }
        }

        /// <summary>
        /// Handler pour l'onglet TOP UNIT dans le header visualizer
        /// </summary>
        private void BtnTabTop_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                // Mise a jour visuelle des onglets
                BtnTabTop.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));
                BtnTabTop.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00D4FF"));
                ((TextBlock)BtnTabTop.Child).Foreground = Brushes.White;
                
                BtnTabBottom.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D3D56"));
                BtnTabBottom.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A7FBF"));
                ((TextBlock)BtnTabBottom.Child).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B0B0B0"));
                
                // Activer l'onglet TOP dans le visualizer
                ModuleVisualizer.SelectTab(0);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConfigUnite] [-] Erreur switch to TOP: {ex.Message}", Logger.LogLevel.ERROR);
            }
        }

        /// <summary>
        /// Handler pour l'onglet BOTTOM UNIT dans le header visualizer
        /// </summary>
        private void BtnTabBottom_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                // Mise a jour visuelle des onglets
                BtnTabBottom.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));
                BtnTabBottom.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00D4FF"));
                ((TextBlock)BtnTabBottom.Child).Foreground = Brushes.White;
                
                BtnTabTop.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D3D56"));
                BtnTabTop.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A7FBF"));
                ((TextBlock)BtnTabTop.Child).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B0B0B0"));
                
                // Activer l'onglet BOTTOM dans le visualizer
                ModuleVisualizer.SelectTab(1);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConfigUnite] [-] Erreur switch to BOTTOM: {ex.Message}", Logger.LogLevel.ERROR);
            }
        }

        /// <summary>
        /// Upload la configuration vers Vault (methode conservee pour compatibilite)
        /// </summary>
        private async void BtnUploadVault_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_vaultService == null || !_vaultService.IsConnected)
                {
                    XnrgyMessageBox.ShowWarning("Vault n'est pas connecté.\n\nVeuillez vous connecter à Vault avant d'uploader.", "Vault non connecté", this);
                    return;
                }

                // Synchroniser l'UI avec le modèle avant upload
                UpdateConfigFromUI();

                // Vérifier que la configuration est sauvegardée localement
                if (string.IsNullOrEmpty(_currentConfigPath) || !File.Exists(_currentConfigPath))
                {
                    // Sauvegarder d'abord
                    BtnSave_Click(sender, e);
                    if (string.IsNullOrEmpty(_currentConfigPath) || !File.Exists(_currentConfigPath))
                    {
                        XnrgyMessageBox.ShowWarning("Veuillez sauvegarder la configuration avant de l'uploader vers Vault.", "Sauvegarde requise", this);
                        return;
                    }
                }

                // Obtenir les informations du projet
                string projectNumber = _currentConfig.DesignInfo.ProjectNumber;
                if (string.IsNullOrEmpty(projectNumber))
                {
                    XnrgyMessageBox.ShowWarning("Le numéro de projet est requis pour l'upload vers Vault.\n\nVeuillez remplir le champ 'Project Number' dans Design Info.", "Information manquante", this);
                    return;
                }

                // Utiliser la reference depuis le model (structure centralisee Config_Unites)
                string reference = _currentConfig.Reference;
                if (string.IsNullOrEmpty(reference)) reference = "01";

                UpdateStatus("Upload vers Vault en cours...");

                bool success = await _configService.UploadToVaultAsync(
                    _currentConfigPath,
                    projectNumber,
                    reference,
                    $"Configuration Unite {projectNumber}{reference} | {DateTime.Now:yyyy-MM-dd HH:mm}"
                );

                if (success)
                {
                    UpdateStatus("Configuration uploadee vers Vault avec succes");
                    XnrgyMessageBox.ShowSuccess($"Configuration uploadee vers Vault avec succes:\n\n{_configService.GetVaultConfigPath(projectNumber, reference)}", "Upload reussi", this);
                }
                else
                {
                    UpdateStatus("Erreur lors de l'upload vers Vault", true);
                    XnrgyMessageBox.ShowError("Impossible d'uploader la configuration vers Vault.\n\nVérifiez les logs pour plus de détails.", "Erreur", this);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("Erreur lors de l'upload vers Vault", true);
                XnrgyMessageBox.ShowError($"Erreur lors de l'upload vers Vault:\n\n{ex.Message}", "Erreur", this);
            }
        }

        /// <summary>
        /// Met à jour l'UI depuis le modèle de données (pour les champs non bindés)
        /// </summary>
        private void UpdateUIFromConfig()
        {
            try
            {
                // ============================================
                // HEADER: Project/Reference/JobTitle
                // ============================================
                if (!string.IsNullOrEmpty(_currentConfig.Project))
                    CmbProject.Text = _currentConfig.Project;
                if (!string.IsNullOrEmpty(_currentConfig.Reference))
                    CmbReference.Text = _currentConfig.Reference;
                if (!string.IsNullOrEmpty(_currentConfig.UnitName))
                    TxtJobTitle.Text = _currentConfig.UnitName;

                // ============================================
                // TAB: Design Info & Revision (fusionné)
                // Note: Template Creator Info groupe supprime
                // ============================================
                // Project Number - auto-rempli depuis la config
                if (!string.IsNullOrEmpty(_currentConfig.DesignInfo.ProjectNumber))
                    TxtProjectNumber.Text = _currentConfig.DesignInfo.ProjectNumber;
                else if (!string.IsNullOrEmpty(_currentConfig.Project))
                    TxtProjectNumber.Text = _currentConfig.Project;
                    
                if (!string.IsNullOrEmpty(_currentConfig.DesignInfo.LeadCAD))
                    CmbLeadCAD.SelectedItem = _currentConfig.DesignInfo.LeadCAD;
                if (!string.IsNullOrEmpty(_currentConfig.DesignInfo.DrawnBy))
                    CmbDrawnBy.SelectedItem = _currentConfig.DesignInfo.DrawnBy;
                DpConfigDate.SelectedDate = _currentConfig.DesignInfo.ConfigDate;
                DpDrawingSubmittalDate.SelectedDate = _currentConfig.DesignInfo.DrawingSubmittalDate;

                // ============================================
                // TAB: Unit Specification
                // ============================================
                if (!string.IsNullOrEmpty(_currentConfig.UnitSpecification.UnitType))
                    CmbUnitType.SelectedItem = _currentConfig.UnitSpecification.UnitType;
                if (!string.IsNullOrEmpty(_currentConfig.UnitSpecification.AirFlowRight))
                    CmbAirFlowRight.SelectedItem = _currentConfig.UnitSpecification.AirFlowRight;
                if (!string.IsNullOrEmpty(_currentConfig.UnitSpecification.AirFlowLeft))
                    CmbAirFlowLeft.SelectedItem = _currentConfig.UnitSpecification.AirFlowLeft;
                if (!string.IsNullOrEmpty(_currentConfig.UnitSpecification.AirFlowMiddle))
                    CmbAirFlowMiddle.SelectedItem = _currentConfig.UnitSpecification.AirFlowMiddle;
                if (!string.IsNullOrEmpty(_currentConfig.UnitSpecification.UnitOption))
                    CmbUnitOption.SelectedItem = _currentConfig.UnitSpecification.UnitOption;
                if (!string.IsNullOrEmpty(_currentConfig.UnitSpecification.UnitConfiguration))
                    CmbUnitConfiguration.SelectedItem = _currentConfig.UnitSpecification.UnitConfiguration;
                if (!string.IsNullOrEmpty(_currentConfig.UnitSpecification.UnitCertification))
                    CmbUnitCertification.SelectedItem = _currentConfig.UnitSpecification.UnitCertification;

                // ============================================
                // TAB: Floor Info
                // ============================================
                if (!string.IsNullOrEmpty(_currentConfig.FloorInfo.BaseConstruction))
                    CmbBaseConstruction.SelectedItem = _currentConfig.FloorInfo.BaseConstruction;
                if (!string.IsNullOrEmpty(_currentConfig.FloorInfo.BaseInsulation))
                    CmbBaseInsulation.SelectedItem = _currentConfig.FloorInfo.BaseInsulation;
                if (!string.IsNullOrEmpty(_currentConfig.FloorInfo.BaseCoating))
                    CmbBaseCoating.SelectedItem = _currentConfig.FloorInfo.BaseCoating;
                if (!string.IsNullOrEmpty(_currentConfig.FloorInfo.BaseThermalBreak))
                    CmbBaseThermalBreak.SelectedItem = _currentConfig.FloorInfo.BaseThermalBreak;
                if (!string.IsNullOrEmpty(_currentConfig.FloorInfo.FloorConstruction))
                    CmbFloorConstruction.SelectedItem = _currentConfig.FloorInfo.FloorConstruction;
                if (!string.IsNullOrEmpty(_currentConfig.FloorInfo.FloorLiner))
                    CmbFloorLiner.SelectedItem = _currentConfig.FloorInfo.FloorLiner;
                if (!string.IsNullOrEmpty(_currentConfig.FloorInfo.SubfloorLiner))
                    CmbSubfloorLiner.SelectedItem = _currentConfig.FloorInfo.SubfloorLiner;
                if (!string.IsNullOrEmpty(_currentConfig.FloorInfo.FloorMountType))
                    CmbFloorMountType.SelectedItem = _currentConfig.FloorInfo.FloorMountType;

                // ============================================
                // TAB: Casing Info
                // ============================================
                if (!string.IsNullOrEmpty(_currentConfig.CasingInfo.PanelConstruction))
                    CmbPanelConstruction.SelectedItem = _currentConfig.CasingInfo.PanelConstruction;
                if (!string.IsNullOrEmpty(_currentConfig.CasingInfo.PanelWidth))
                    CmbPanelWidth.SelectedItem = _currentConfig.CasingInfo.PanelWidth;
                if (!string.IsNullOrEmpty(_currentConfig.CasingInfo.PanelInsulation))
                    CmbPanelInsulation.SelectedItem = _currentConfig.CasingInfo.PanelInsulation;
                if (!string.IsNullOrEmpty(_currentConfig.CasingInfo.PanelLinerMaterial.WallPanelMaterial))
                    CmbWallPanelMaterial.SelectedItem = _currentConfig.CasingInfo.PanelLinerMaterial.WallPanelMaterial;
                if (!string.IsNullOrEmpty(_currentConfig.CasingInfo.PanelLinerMaterial.WallLinerMaterial))
                    CmbWallLinerMaterial.SelectedItem = _currentConfig.CasingInfo.PanelLinerMaterial.WallLinerMaterial;
                if (!string.IsNullOrEmpty(_currentConfig.CasingInfo.PanelLinerMaterial.RoofPanelMtl))
                    CmbRoofPanelMtl.SelectedItem = _currentConfig.CasingInfo.PanelLinerMaterial.RoofPanelMtl;
                if (!string.IsNullOrEmpty(_currentConfig.CasingInfo.PanelLinerMaterial.RoofLinerMtl))
                    CmbRoofLinerMtl.SelectedItem = _currentConfig.CasingInfo.PanelLinerMaterial.RoofLinerMtl;
                if (!string.IsNullOrEmpty(_currentConfig.CasingInfo.Coating))
                    CmbCoating.SelectedItem = _currentConfig.CasingInfo.Coating;
                if (!string.IsNullOrEmpty(_currentConfig.CasingInfo.WallMaterial))
                    CmbWallMaterial.SelectedItem = _currentConfig.CasingInfo.WallMaterial;

                // ============================================
                // TAB: Miscellaneous
                // ============================================
                if (!string.IsNullOrEmpty(_currentConfig.Miscellaneous.HardwareMaterial))
                    CmbHardwareMaterial.SelectedItem = _currentConfig.Miscellaneous.HardwareMaterial;
                if (!string.IsNullOrEmpty(_currentConfig.Miscellaneous.ShrinkWrap))
                    CmbShrinkWrap.SelectedItem = _currentConfig.Miscellaneous.ShrinkWrap;
                if (!string.IsNullOrEmpty(_currentConfig.Miscellaneous.Sealant))
                    CmbSealant.SelectedItem = _currentConfig.Miscellaneous.Sealant;
                
                // ============================================
                // TAB: Module Dimensions (DataGrid)
                // ============================================
                // IMPORTANT: Reassigner ItemsSource apres changement de _currentConfig
                // Items.Refresh() seul ne suffit pas car la reference de liste a change
                DgModuleDimensions.ItemsSource = null;
                DgModuleDimensions.ItemsSource = _currentConfig.ModuleDimensions;
                
                Logger.Log($"[ConfigUnite] [i] DataGrid rafraichi: {_currentConfig.ModuleDimensions.Count} module(s)", Logger.LogLevel.DEBUG);
                
                // ============================================
                // Mettre a jour le visualiseur graphique avec config complete
                // ============================================
                ModuleVisualizer.UpdateWithConfig(_currentConfig);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConfigUnite] Erreur UpdateUIFromConfig: {ex.Message}", Logger.LogLevel.DEBUG);
            }
        }

        /// <summary>
        /// Met à jour le modèle depuis l'UI (pour les champs non bindés automatiquement)
        /// </summary>
        private void UpdateConfigFromUI()
        {
            try
            {
                // ============================================
                // HEADER: Project/Reference/JobTitle
                // ============================================
                _currentConfig.Project = CmbProject.Text?.Trim() ?? "";
                string refText = CmbReference.Text?.Trim() ?? "";
                _currentConfig.Reference = refText.Replace("REF", "").Trim();
                _currentConfig.UnitName = TxtJobTitle.Text?.Trim() ?? "";

                // ============================================
                // TAB: Design Info & Revision (fusionné)
                // Note: Template Creator Info groupe supprime
                // ============================================
                _currentConfig.Revision.RevisionNumber = TxtRevisionNumber.Text?.Trim() ?? "";
                _currentConfig.DesignInfo.ProjectNumber = TxtProjectNumber.Text?.Trim() ?? "";
                _currentConfig.DesignInfo.JobTitle = TxtDesignJobTitle.Text?.Trim() ?? "";
                _currentConfig.DesignInfo.LeadCAD = CmbLeadCAD.SelectedItem?.ToString() ?? "";
                _currentConfig.DesignInfo.DrawnBy = CmbDrawnBy.SelectedItem?.ToString() ?? "";
                if (DpConfigDate.SelectedDate.HasValue)
                    _currentConfig.DesignInfo.ConfigDate = DpConfigDate.SelectedDate.Value;
                if (DpDrawingSubmittalDate.SelectedDate.HasValue)
                    _currentConfig.DesignInfo.DrawingSubmittalDate = DpDrawingSubmittalDate.SelectedDate.Value;

                // ============================================
                // TAB: Unit Specification
                // ============================================
                _currentConfig.UnitSpecification.IsCRAHUnit = ChkCRAHUnit.IsChecked ?? false;
                _currentConfig.UnitSpecification.UnitType = CmbUnitType.SelectedItem?.ToString() ?? "None/Aucun";
                
                // Tunnels et AirFlow
                _currentConfig.UnitSpecification.Tunnel1Right = ChkTunnel1Right.IsChecked ?? false;
                _currentConfig.UnitSpecification.AirFlowRight = CmbAirFlowRight.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.UnitSpecification.Tunnel2Left = ChkTunnel2Left.IsChecked ?? false;
                _currentConfig.UnitSpecification.AirFlowLeft = CmbAirFlowLeft.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.UnitSpecification.Tunnel3Middle = ChkTunnel3Middle.IsChecked ?? false;
                _currentConfig.UnitSpecification.AirFlowMiddle = CmbAirFlowMiddle.SelectedItem?.ToString() ?? "None/Aucun";
                
                // Unit Options
                _currentConfig.UnitSpecification.UnitOption = CmbUnitOption.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.UnitSpecification.UnitDesignPressure = TxtUnitDesignPressure.Text?.Trim() ?? "12 ul";
                _currentConfig.UnitSpecification.UnitConfiguration = CmbUnitConfiguration.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.UnitSpecification.UnitCertification = CmbUnitCertification.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.UnitSpecification.FactoryTesting = ChkFactoryTesting.IsChecked ?? false;
                _currentConfig.UnitSpecification.MaxHoleDistanceForm = TxtMaxHoleDistanceForm.Text?.Trim() ?? "12 in";

                // ============================================
                // TAB: Floor Info
                // ============================================
                _currentConfig.FloorInfo.BaseConstruction = CmbBaseConstruction.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.FloorInfo.BaseInsulation = CmbBaseInsulation.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.FloorInfo.FloorHeight = TxtFloorHeight.Text?.Trim() ?? "8 in";
                _currentConfig.FloorInfo.BaseCoating = CmbBaseCoating.SelectedItem?.ToString() ?? "No Paint/Non Peint";
                _currentConfig.FloorInfo.BaseThermalBreak = CmbBaseThermalBreak.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.FloorInfo.FloorConstruction = CmbFloorConstruction.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.FloorInfo.FloorLiner = CmbFloorLiner.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.FloorInfo.SubfloorLiner = CmbSubfloorLiner.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.FloorInfo.AuxiliaryDrains = TxtAuxiliaryDrains.Text?.Trim() ?? "Ø1.25";
                _currentConfig.FloorInfo.CrossMemberWidth = TxtCrossMemberWidth.Text?.Trim() ?? "4.50";
                _currentConfig.FloorInfo.FloorMountType = CmbFloorMountType.SelectedItem?.ToString() ?? "None/Aucun";

                // ============================================
                // TAB: Casing Info
                // ============================================
                _currentConfig.CasingInfo.PanelConstruction = CmbPanelConstruction.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.CasingInfo.PanelWidth = CmbPanelWidth.SelectedItem?.ToString() ?? "2 in";
                _currentConfig.CasingInfo.PanelInsulation = CmbPanelInsulation.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.CasingInfo.PanelLinerMaterial.WallPanelMaterial = CmbWallPanelMaterial.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.CasingInfo.PanelLinerMaterial.WallLinerMaterial = CmbWallLinerMaterial.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.CasingInfo.PanelLinerMaterial.RoofPanelMtl = CmbRoofPanelMtl.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.CasingInfo.PanelLinerMaterial.RoofLinerMtl = CmbRoofLinerMtl.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.CasingInfo.Coating = CmbCoating.SelectedItem?.ToString() ?? "No Paint/Non Peint";
                _currentConfig.CasingInfo.SinglePanelLength = TxtSinglePanelLength.Text?.Trim() ?? "14.875 in";
                _currentConfig.CasingInfo.WallMaterial = CmbWallMaterial.SelectedItem?.ToString() ?? "Galvanized";
                _currentConfig.CasingInfo.SelectedExceIRow = TxtSelectedExceIRow.Text?.Trim() ?? "F6";
                _currentConfig.CasingInfo.CriticalPanelLength = TxtCriticalPanelLength.Text?.Trim() ?? "3.5 in";

                // ============================================
                // TAB: Miscellaneous
                // ============================================
                _currentConfig.Miscellaneous.HardwareMaterial = CmbHardwareMaterial.SelectedItem?.ToString() ?? "None / Aucun";
                _currentConfig.Miscellaneous.ShrinkWrap = CmbShrinkWrap.SelectedItem?.ToString() ?? "None/Aucun";
                _currentConfig.Miscellaneous.Sealant = CmbSealant.SelectedItem?.ToString() ?? "None/Aucun";
                
                // ============================================
                // TAB: Module Dimensions
                // ============================================
                // IMPORTANT: Forcer le commit des cellules en edition du DataGrid
                // Sans cela, les modifications en cours ne sont pas sauvegardees
                if (DgModuleDimensions != null)
                {
                    // Forcer la fin de l'edition de la cellule courante
                    DgModuleDimensions.CommitEdit(DataGridEditingUnit.Row, true);
                    
                    // Rafraichir les bindings pour s'assurer que tout est synchronise
                    DgModuleDimensions.Items.Refresh();
                    
                    Logger.Log($"[ConfigUnite] [i] ModuleDimensions synchronises: {_currentConfig.ModuleDimensions.Count} module(s)", Logger.LogLevel.DEBUG);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConfigUnite] Erreur UpdateConfigFromUI: {ex.Message}", Logger.LogLevel.DEBUG);
            }
        }

        /// <summary>
        /// Appelé avant la fermeture pour synchroniser l'UI avec le modèle
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UpdateConfigFromUI();
        }

        /// <summary>
        /// Ajoute un nouveau module à la liste
        /// </summary>
        private void BtnAddModule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Log("[ConfigUnite] [>] Ajout d'un nouveau module...", Logger.LogLevel.INFO);
                
                // Trouver le prochain numéro de module disponible (format 01, 02, etc.)
                int nextModuleNumber = 1;
                if (_currentConfig.ModuleDimensions.Count > 0)
                {
                    var existingNumbers = _currentConfig.ModuleDimensions
                        .Select(m => 
                        {
                            // Extraire le numero de 01, 02, M01, M02, etc.
                            string numStr = m.ModuleNumber.Replace("M", "").Replace("Module ", "").Trim();
                            if (int.TryParse(numStr, out int num))
                                return num;
                            return 0;
                        })
                        .Where(n => n > 0)
                        .ToList();
                    
                    if (existingNumbers.Any())
                        nextModuleNumber = existingNumbers.Max() + 1;
                }

                var newModule = new Models.ModuleDimension
                {
                    ModuleNumber = $"{nextModuleNumber:D2}",  // Format 01, 02, etc.
                    Height = "",
                    Width = "",
                    Length = "",
                    Description = "",
                    // Nouvelles proprietes tunnel
                    HasTunnel = false,
                    TunnelPosition = "None",
                    TunnelTopHeight = "0",
                    TunnelBottomHeight = "0",
                    AirFlowDirection = "None",
                    TunnelType = "Tunnel"
                };

                _currentConfig.ModuleDimensions.Add(newModule);
                Logger.Log($"[ConfigUnite] [+] Module {newModule.ModuleNumber} ajoute (Total: {_currentConfig.ModuleDimensions.Count})", Logger.LogLevel.INFO);
                
                // Forcer le refresh visuel du DataGrid
                DgModuleDimensions.ItemsSource = null;
                DgModuleDimensions.ItemsSource = _currentConfig.ModuleDimensions;
                
                // Mettre a jour le visualiseur graphique avec config complete
                ModuleVisualizer.UpdateWithConfig(_currentConfig);
                
                // Sélectionner le nouveau module
                DgModuleDimensions.SelectedItem = newModule;
                DgModuleDimensions.ScrollIntoView(newModule);
                
                UpdateStatus($"Module {nextModuleNumber} ajouté");
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConfigUnite] [-] Erreur ajout module: {ex.Message}", Logger.LogLevel.ERROR);
                XnrgyMessageBox.ShowError($"Erreur lors de l'ajout du module:\n\n{ex.Message}", "Erreur", this);
            }
        }

        /// <summary>
        /// Supprime le module sélectionné
        /// </summary>
        private void BtnRemoveModule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DgModuleDimensions.SelectedItem is Models.ModuleDimension selectedModule)
                {
                    Logger.Log($"[ConfigUnite] [?] Demande de suppression du module {selectedModule.ModuleNumber}", Logger.LogLevel.INFO);
                    
                    bool confirmed = XnrgyMessageBox.Confirm(
                        $"Voulez-vous vraiment supprimer {selectedModule.ModuleNumber} ?",
                        "Confirmation de suppression",
                        this
                    );

                    if (confirmed)
                    {
                        _currentConfig.ModuleDimensions.Remove(selectedModule);
                        Logger.Log($"[ConfigUnite] [+] Module {selectedModule.ModuleNumber} supprime (Restant: {_currentConfig.ModuleDimensions.Count})", Logger.LogLevel.INFO);
                        
                        // Forcer le refresh visuel du DataGrid
                        DgModuleDimensions.ItemsSource = null;
                        DgModuleDimensions.ItemsSource = _currentConfig.ModuleDimensions;
                        
                        // Mettre a jour le visualiseur graphique avec config complete
                        ModuleVisualizer.UpdateWithConfig(_currentConfig);
                        
                        UpdateStatus($"{selectedModule.ModuleNumber} supprimé");
                    }
                    else
                    {
                        Logger.Log($"[ConfigUnite] [~] Suppression annulee par l'utilisateur", Logger.LogLevel.DEBUG);
                    }
                }
                else
                {
                    XnrgyMessageBox.ShowWarning("Veuillez sélectionner un module à supprimer.", "Aucune sélection", this);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConfigUnite] [-] Erreur suppression module: {ex.Message}", Logger.LogLevel.ERROR);
                XnrgyMessageBox.ShowError($"Erreur lors de la suppression du module:\n\n{ex.Message}", "Erreur", this);
            }
        }

        /// <summary>
        /// Gère la sélection dans la DataGrid
        /// </summary>
        private void DgModuleDimensions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnRemoveModule.IsEnabled = DgModuleDimensions.SelectedItem != null;
        }

        /// <summary>
        /// Gère la suppression avec la touche Delete
        /// </summary>
        private void DgModuleDimensions_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete && DgModuleDimensions.SelectedItem != null)
            {
                BtnRemoveModule_Click(sender, e);
                e.Handled = true;
            }
        }

        #region Project/Reference Navigation (Phase 1 - Final)

        // Chemin centralise du dossier Config_Unites (au niveau Projects, pas dans chaque projet)
        // Structure: C:\Vault\Engineering\Projects\Config_Unites\[Projet][Reference].config
        private const string CONFIG_UNITES_FOLDER = "Config_Unites";
        private const string VAULT_PROJECTS_PATH = @"C:\Vault\Engineering\Projects";
        private static string CONFIG_UNITES_PATH => Path.Combine(VAULT_PROJECTS_PATH, CONFIG_UNITES_FOLDER);

        // Cache des projets et references existants (pour recherche intelligente)
        private List<string> _existingProjects = new List<string>();
        private Dictionary<string, List<string>> _existingRefsByProject = new Dictionary<string, List<string>>();
        private List<string> _allReferences = new List<string>(); // Liste complete 01-99

        /// <summary>
        /// Initialise les ComboBox Project/Reference avec les donnees disponibles
        /// Recherche INTELLIGENTE: detecte les projets (5 premiers caracteres) et references (2 suivants) depuis les fichiers .config
        /// </summary>
        private void InitializeProjectReferenceComboBoxes()
        {
            try
            {
                // Initialiser la liste complete des references (01 a 99) - toujours disponible
                _allReferences = new List<string>();
                for (int i = 1; i <= 99; i++)
                {
                    _allReferences.Add(i.ToString("D2")); // 01, 02, ..., 99
                }
                CmbReference.ItemsSource = _allReferences;

                // Scanner les configs existantes pour extraire projets et references
                ScanExistingConfigs();

                // Charger les configs existantes dans le ComboBox
                LoadAllExistingConfigs();

                UpdateStatus("[+] Pret - Saisir Projet/Reference/Job Title puis cliquer Nouveau, ou charger une config existante");
            }
            catch (Exception ex)
            {
                UpdateStatus($"[-] Erreur initialisation: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Scanne le dossier Config_Unites et extrait les projets/references existants
        /// Format fichier: [Projet 5 chars][Reference 2 chars].config (ex: 1234501.config)
        /// </summary>
        private void ScanExistingConfigs()
        {
            _existingProjects.Clear();
            _existingRefsByProject.Clear();

            try
            {
                if (!Directory.Exists(CONFIG_UNITES_PATH))
                {
                    Logger.Log($"[ConfigUnite] Dossier Config_Unites non trouve: {CONFIG_UNITES_PATH}", Logger.LogLevel.DEBUG);
                    return;
                }

                var configFiles = Directory.GetFiles(CONFIG_UNITES_PATH, "*.config");
                
                foreach (var file in configFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    
                    // Format attendu: [Projet 5 chars][Reference 2 chars] = 7 caracteres minimum
                    if (fileName.Length >= 7)
                    {
                        string project = fileName.Substring(0, 5);  // 5 premiers = Projet
                        string reference = fileName.Substring(5, 2); // 2 suivants = Reference
                        
                        // Ajouter le projet s'il n'existe pas
                        if (!_existingProjects.Contains(project))
                        {
                            _existingProjects.Add(project);
                        }
                        
                        // Ajouter la reference pour ce projet
                        if (!_existingRefsByProject.ContainsKey(project))
                        {
                            _existingRefsByProject[project] = new List<string>();
                        }
                        if (!_existingRefsByProject[project].Contains(reference))
                        {
                            _existingRefsByProject[project].Add(reference);
                        }
                    }
                }

                // Trier les projets (plus recent en premier = plus grand numero)
                _existingProjects = _existingProjects.OrderByDescending(p => p).ToList();

                // Mettre a jour le ComboBox Projet avec les projets existants
                if (_existingProjects.Any())
                {
                    CmbProject.ItemsSource = _existingProjects;
                    Logger.Log($"[ConfigUnite] {_existingProjects.Count} projet(s) avec configs existantes detecte(s)", Logger.LogLevel.DEBUG);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConfigUnite] Erreur scan configs: {ex.Message}", Logger.LogLevel.ERROR);
            }
        }

        /// <summary>
        /// Met a jour les references disponibles selon le projet selectionne
        /// Affiche d'abord les refs existantes pour ce projet, puis les autres 01-99
        /// </summary>
        private void UpdateReferencesForProject(string project)
        {
            // Sauvegarder la reference actuellement saisie AVANT de modifier l'ItemsSource
            string currentRefText = CmbReference.Text?.Trim() ?? "";
            
            // Desactiver l'evenement SelectionChanged pendant la modification
            CmbReference.SelectionChanged -= CmbReference_SelectionChanged;
            
            try
            {
                if (string.IsNullOrEmpty(project) || project.Length < 5)
                {
                    // Pas de projet valide - afficher toutes les references
                    CmbReference.ItemsSource = _allReferences;
                    CmbReference.Text = currentRefText; // Restaurer le texte saisi
                    return;
                }

                // Extraire les 5 premiers caracteres (au cas ou l'utilisateur a saisi plus)
                string projectKey = project.Length >= 5 ? project.Substring(0, 5) : project;

                var orderedRefs = new List<string>();

                // D'abord ajouter les references existantes pour ce projet (avec indicateur)
                if (_existingRefsByProject.ContainsKey(projectKey))
                {
                    var existingRefs = _existingRefsByProject[projectKey].OrderBy(r => r).ToList();
                    foreach (var existingRef in existingRefs)
                    {
                        orderedRefs.Add($"{existingRef} [Config]"); // Marquer les refs avec config existante
                    }
                }

                // Ensuite ajouter toutes les autres references (01-99)
                foreach (var allRef in _allReferences)
                {
                    // Ne pas re-ajouter si deja dans la liste avec [Config]
                    if (!orderedRefs.Any(r => r.StartsWith(allRef + " ")))
                    {
                        orderedRefs.Add(allRef);
                    }
                }

                CmbReference.ItemsSource = orderedRefs;
                
                // Restaurer le texte saisi par l'utilisateur
                CmbReference.Text = currentRefText;
            }
            finally
            {
                // Toujours reactiver l'evenement
                CmbReference.SelectionChanged += CmbReference_SelectionChanged;
            }
        }

        /// <summary>
        /// Charge TOUTES les configs existantes depuis le dossier Config_Unites centralise
        /// </summary>
        private void LoadAllExistingConfigs()
        {
            try
            {
                CmbExistingConfigs.ItemsSource = null;
                var configList = new List<string>();

                if (Directory.Exists(CONFIG_UNITES_PATH))
                {
                    var configFiles = Directory.GetFiles(CONFIG_UNITES_PATH, "*.config")
                        .Select(f => Path.GetFileNameWithoutExtension(f))
                        .OrderBy(c => c) // Ordre alphabetique croissant
                        .ToList();

                    if (configFiles.Any())
                    {
                        configList.AddRange(configFiles);
                        UpdateStatus($"[+] {configFiles.Count} configuration(s) existante(s) | {_existingProjects.Count} projet(s)");
                    }
                    else
                    {
                        UpdateStatus("[i] Aucune configuration existante - Creer une nouvelle");
                    }
                }
                else
                {
                    UpdateStatus($"[i] Dossier {CONFIG_UNITES_FOLDER} non existant - Creer une nouvelle config");
                }

                CmbExistingConfigs.ItemsSource = configList;
            }
            catch (Exception ex)
            {
                UpdateStatus($"[-] Erreur chargement configs: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Evenement quand le projet saisi change - Met a jour les references et verifie si config existe
        /// </summary>
        private void CmbProject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Sauvegarder la reference actuelle avant de mettre a jour
            string currentRef = GetSelectedReference();
            
            // Mettre a jour les references disponibles pour ce projet
            string project = GetSelectedProject();
            UpdateReferencesForProject(project);
            
            // [+] Auto-remplir le champ Project Number dans Design Info
            _currentConfig.DesignInfo.ProjectNumber = project;
            TxtProjectNumber.Text = project;
            
            // Restaurer la reference si elle etait deja saisie
            if (!string.IsNullOrEmpty(currentRef))
            {
                CmbReference.Text = currentRef;
            }
            
            // Verifier si une config existe pour ce projet + reference
            CheckAndOfferExistingConfig();
        }

        /// <summary>
        /// Evenement quand la reference change - Verifie si config existe
        /// </summary>
        private void CmbReference_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Verifier si une config existe pour ce projet + reference
            CheckAndOfferExistingConfig();
        }

        /// <summary>
        /// Verifie si une config existe pour le projet/reference actuel et propose de la charger
        /// Synchronise aussi le dropdown Configs Existantes et charge le Job Title
        /// </summary>
        private void CheckAndOfferExistingConfig()
        {
            try
            {
                string project = GetSelectedProject();
                string reference = GetSelectedReference();

                // Si les deux sont remplis (projet=5 chars, ref=2 chars), verifier si config existe
                if (project.Length >= 5 && reference.Length >= 1)
                {
                    // Normaliser la reference (enlever [Config] si present et formater sur 2 digits)
                    string refNormalized = reference.Replace("[Config]", "").Replace("REF", "").Trim();
                    if (refNormalized.Length == 1) refNormalized = "0" + refNormalized;
                    
                    // Prendre seulement les 5 premiers caracteres du projet
                    string projectNormalized = project.Length >= 5 ? project.Substring(0, 5) : project;

                    string configFileName = $"{projectNormalized}{refNormalized}";
                    string configPath = Path.Combine(CONFIG_UNITES_PATH, configFileName + ".config");

                    if (File.Exists(configPath))
                    {
                        // Config existe! Mettre a jour le status
                        UpdateStatus($"[+] Config trouvee: {configFileName}.config - Cliquez Charger", false);
                        
                        // Selectionner automatiquement dans le dropdown Configs Existantes
                        if (CmbExistingConfigs.ItemsSource != null)
                        {
                            var items = CmbExistingConfigs.ItemsSource as List<string>;
                            if (items != null && items.Contains(configFileName))
                            {
                                // Eviter de declencher des evenements en cascade
                                CmbExistingConfigs.SelectionChanged -= CmbExistingConfigs_SelectionChanged;
                                CmbExistingConfigs.SelectedItem = configFileName;
                                CmbExistingConfigs.SelectionChanged += CmbExistingConfigs_SelectionChanged;
                            }
                        }
                        
                        // Charger le Job Title depuis la config (lecture rapide sans charger tout)
                        LoadJobTitleFromConfig(configPath);
                    }
                    else
                    {
                        // Aucune config - mode nouveau
                        UpdateStatus($"[i] Nouvelle config: {projectNormalized}-REF{refNormalized}", false);
                        CmbExistingConfigs.SelectionChanged -= CmbExistingConfigs_SelectionChanged;
                        CmbExistingConfigs.SelectedItem = null;
                        CmbExistingConfigs.SelectionChanged += CmbExistingConfigs_SelectionChanged;
                        // Ne pas effacer le Job Title - l'utilisateur peut l'avoir saisi
                    }
                }
            }
            catch
            {
                // Ignorer les erreurs silencieusement
            }
        }
        
        /// <summary>
        /// Charge uniquement le Job Title depuis un fichier config (sans charger toute la config)
        /// </summary>
        private void LoadJobTitleFromConfig(string configPath)
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var config = Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigUniteDataModel>(json);
                    if (config != null && !string.IsNullOrEmpty(config.UnitName))
                    {
                        TxtJobTitle.Text = config.UnitName;
                    }
                }
            }
            catch
            {
                // Ignorer - le Job Title restera vide
            }
        }

        /// <summary>
        /// Evenement quand l'utilisateur tape dans le ComboBox Projet
        /// </summary>
        private void CmbProject_KeyUp(object sender, KeyEventArgs e)
        {
            // Ignorer les touches de navigation
            if (e.Key != Key.Tab && e.Key != Key.Enter)
            {
                // Mettre a jour les references disponibles quand le projet change
                string project = GetSelectedProject();
                if (project.Length >= 5)
                {
                    UpdateReferencesForProject(project);
                }
                
                // Verifier si config existe apres chaque frappe
                CheckAndOfferExistingConfig();
            }
        }

        /// <summary>
        /// Evenement quand l'utilisateur tape dans le ComboBox Reference
        /// </summary>
        private void CmbReference_KeyUp(object sender, KeyEventArgs e)
        {
            // Verifier si config existe apres chaque frappe
            if (e.Key != Key.Tab && e.Key != Key.Enter)
            {
                CheckAndOfferExistingConfig();
            }
        }

        /// <summary>
        /// Recupere le projet saisi
        /// </summary>
        private string GetSelectedProject()
        {
            return CmbProject.Text?.Trim() ?? "";
        }

        /// <summary>
        /// Recupere la reference saisie (nettoie le tag [Config] si present)
        /// </summary>
        private string GetSelectedReference()
        {
            string reference = CmbReference.Text?.Trim() ?? "";
            // Enlever le tag [Config] si present
            if (reference.Contains("[Config]"))
            {
                reference = reference.Replace("[Config]", "").Trim();
            }
            return reference;
        }

        /// <summary>
        /// Bouton "Nouveau" - Cree une nouvelle config ET la sauvegarde immediatement (Local + Vault)
        /// Pour que les autres dessinateurs voient le job des qu'il est cree
        /// </summary>
        private async void BtnApplyNew_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Log("[ConfigUnite] [>] Creation d'une nouvelle configuration...", Logger.LogLevel.INFO);
                
                string project = GetSelectedProject();
                string reference = GetSelectedReference();
                string jobTitle = TxtJobTitle.Text?.Trim() ?? "";
                
                Logger.Log($"[ConfigUnite]    Projet: {project}, Reference: {reference}, Job: {jobTitle}", Logger.LogLevel.DEBUG);

                // Validation
                if (string.IsNullOrEmpty(project) || project.Length < 5)
                {
                    Logger.Log("[ConfigUnite] [!] Validation echouee: Projet invalide", Logger.LogLevel.WARNING);
                    XnrgyMessageBox.ShowWarning("Veuillez saisir un numero de projet (5 chiffres).", "Projet requis", this);
                    CmbProject.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(reference))
                {
                    Logger.Log("[ConfigUnite] [!] Validation echouee: Reference manquante", Logger.LogLevel.WARNING);
                    XnrgyMessageBox.ShowWarning("Veuillez saisir ou selectionner une reference.", "Reference requise", this);
                    CmbReference.Focus();
                    return;
                }

                // Normaliser la reference (garder juste 2 chiffres)
                reference = reference.Replace("REF", "").Replace("[Config]", "").Trim();
                if (reference.Length == 1) reference = "0" + reference;

                // ============================================
                // VERIFICATION EXISTENCE: LOCAL ET VAULT
                // ============================================
                string configFileName = $"{project}{reference}.config";
                string configPath = Path.Combine(CONFIG_UNITES_PATH, configFileName);
                string vaultPath = $"$/Engineering/Projects/Config_Unites/{configFileName}";
                
                bool existsLocal = File.Exists(configPath);
                bool existsVault = false;
                ACW.File? vaultFile = null;
                
                // Verifier dans Vault si connecte
                if (_vaultService != null && _vaultService.IsConnected)
                {
                    vaultFile = _vaultService.FindFileByPath(vaultPath);
                    existsVault = vaultFile != null;
                }
                
                // CAS 1: Existe en local
                if (existsLocal)
                {
                    bool loadExisting = XnrgyMessageBox.Confirm(
                        $"Une configuration existe deja localement pour {project}-REF{reference}.\n\n" +
                        $"Emplacement: {configPath}\n\n" +
                        "Voulez-vous la charger au lieu de creer une nouvelle?",
                        "Configuration existante (Local)", this);
                    
                    if (loadExisting)
                    {
                        LoadConfigFromFile(configPath);
                        return;
                    }
                    else
                    {
                        // L'utilisateur veut ecraser - continuer
                    }
                }
                // CAS 2: N'existe pas en local MAIS existe dans Vault
                else if (existsVault && vaultFile != null)
                {
                    bool loadFromVault = XnrgyMessageBox.Confirm(
                        $"Cette configuration existe dans Vault mais pas localement:\n\n" +
                        $"Vault: {vaultPath}\n\n" +
                        "Voulez-vous telecharger la configuration depuis Vault?",
                        "Configuration trouvee dans Vault", this);
                    
                    if (loadFromVault)
                    {
                        // Telecharger depuis Vault (GET)
                        UpdateStatus($"[>] Telechargement depuis Vault: {configFileName}...");
                        
                        // Creer le dossier si necessaire
                        if (!Directory.Exists(CONFIG_UNITES_PATH))
                        {
                            Directory.CreateDirectory(CONFIG_UNITES_PATH);
                        }
                        
                        bool getSuccess = _vaultService.AcquireFile(vaultFile, configPath, checkout: false);
                        
                        if (getSuccess)
                        {
                            UpdateStatus($"[+] Configuration telechargee depuis Vault");
                            LoadConfigFromFile(configPath);
                        }
                        else
                        {
                            XnrgyMessageBox.ShowError(
                                "Impossible de telecharger la configuration depuis Vault.",
                                "Erreur GET", this);
                            UpdateStatus("[-] Erreur lors du telechargement depuis Vault", true);
                        }
                        return;
                    }
                    else
                    {
                        // L'utilisateur ne veut pas charger depuis Vault - annuler
                        XnrgyMessageBox.ShowInfo(
                            "Creation annulee.\n\nLa configuration existe deja dans Vault. " +
                            "Utilisez 'Telecharger depuis Vault' pour la recuperer.",
                            "Creation annulee", this);
                        return;
                    }
                }
                // CAS 3: N'existe nulle part - OK pour creer

                // ============================================
                // CREER LA NOUVELLE CONFIG
                // ============================================
                _currentConfig = new ConfigUniteDataModel
                {
                    Project = project,
                    Reference = reference,
                    UnitName = jobTitle,
                    LastModified = DateTime.Now,
                    LastModifiedBy = Environment.UserName
                };

                // Mettre a jour le Project Number dans DesignInfo aussi
                _currentConfig.DesignInfo.ProjectNumber = project;

                _currentConfigPath = configPath;

                // Mettre a jour l'interface
                UpdateUIFromConfig();

                // ============================================
                // SAUVEGARDE LOCALE IMMEDIATE
                // ============================================
                UpdateStatus($"[>] Creation et sauvegarde: {configFileName}...");
                
                // Creer le dossier Config_Unites s'il n'existe pas
                if (!Directory.Exists(CONFIG_UNITES_PATH))
                {
                    Directory.CreateDirectory(CONFIG_UNITES_PATH);
                }

                bool localSuccess = _configService.SaveToFile(_currentConfig, _currentConfigPath);
                
                if (!localSuccess)
                {
                    XnrgyMessageBox.ShowError("Impossible de creer la configuration localement.", "Erreur", this);
                    UpdateStatus("[-] Erreur lors de la creation locale", true);
                    return;
                }
                
                UpdateStatus($"[+] Configuration creee localement: {configFileName}");
                TxtConfigPath.Text = _currentConfigPath;
                
                // Rafraichir la liste des configs existantes
                ScanExistingConfigs();
                LoadAllExistingConfigs();

                // ============================================
                // UPLOAD AUTOMATIQUE VAULT (si connecte)
                // ============================================
                bool vaultSuccess = false;
                string vaultStatus = "";
                
                if (_vaultService != null && _vaultService.IsConnected)
                {
                    UpdateStatus($"[>] Upload Vault: {configFileName}...");
                    
                    try
                    {
                        vaultSuccess = await _configService.UploadToVaultAsync(
                            _currentConfigPath,
                            project,
                            reference,
                            $"Nouvelle config {project}-REF{reference} | {jobTitle} | {DateTime.Now:yyyy-MM-dd HH:mm}"
                        );
                        
                        if (vaultSuccess)
                        {
                            vaultStatus = "Local + Vault OK";
                            UpdateStatus($"[+] Upload Vault reussi: {configFileName}");
                        }
                        else
                        {
                            vaultStatus = "Local OK, Vault ECHEC";
                            UpdateStatus($"[!] Creation locale OK, mais upload Vault echoue", true);
                        }
                    }
                    catch (Exception vaultEx)
                    {
                        vaultStatus = $"Local OK, Vault erreur";
                        UpdateStatus($"[!] Creation locale OK, erreur Vault: {vaultEx.Message}", true);
                        Logger.Log($"[ConfigUnite] Erreur upload Vault nouveau: {vaultEx.Message}", Logger.LogLevel.WARNING);
                    }
                }
                else
                {
                    vaultStatus = "Local OK (Vault non connecte)";
                    UpdateStatus($"[+] Configuration creee localement (Vault non connecte)");
                }

                // ============================================
                // MESSAGE DE CONFIRMATION
                // ============================================
                string message = $"Nouvelle configuration creee et sauvegardee:\n\n" +
                    $"Fichier: {configFileName}\n" +
                    $"Projet: {project}\n" +
                    $"Reference: REF{reference}\n" +
                    $"Job Title: {jobTitle}\n\n" +
                    $"Status: {vaultStatus}\n\n" +
                    $"Les autres dessinateurs peuvent maintenant voir cette configuration.";
                
                if (vaultSuccess)
                {
                    XnrgyMessageBox.ShowSuccess(message, "Configuration creee", this);
                }
                else if (_vaultService == null || !_vaultService.IsConnected)
                {
                    XnrgyMessageBox.ShowInfo(message, "Configuration creee (local)", this);
                }
                else
                {
                    XnrgyMessageBox.ShowWarning(message + "\n\nNote: L'upload Vault a echoue.", "Configuration creee (partielle)", this);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"[-] Erreur: {ex.Message}", true);
                XnrgyMessageBox.ShowError($"Erreur lors de la creation:\n\n{ex.Message}", "Erreur", this);
            }
        }

        /// <summary>
        /// Quand une config existante est selectionnee dans le dropdown
        /// Met a jour automatiquement Projet/Reference/JobTitle
        /// </summary>
        private void CmbExistingConfigs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CmbExistingConfigs.SelectedItem == null) return;

                string selectedConfig = CmbExistingConfigs.SelectedItem.ToString() ?? "";
                
                // Format attendu: XXXXXNN (5 chars projet + 2 chars reference)
                if (selectedConfig.Length >= 7)
                {
                    string project = selectedConfig.Substring(0, 5);
                    string reference = selectedConfig.Substring(5, 2);
                    
                    // Mettre a jour les ComboBox Projet et Reference sans declencher d'evenements en cascade
                    CmbProject.SelectionChanged -= CmbProject_SelectionChanged;
                    CmbReference.SelectionChanged -= CmbReference_SelectionChanged;
                    
                    CmbProject.Text = project;
                    CmbReference.Text = reference;
                    
                    CmbProject.SelectionChanged += CmbProject_SelectionChanged;
                    CmbReference.SelectionChanged += CmbReference_SelectionChanged;
                    
                    // Charger le Job Title depuis la config
                    string configPath = Path.Combine(CONFIG_UNITES_PATH, selectedConfig + ".config");
                    LoadJobTitleFromConfig(configPath);
                    
                    UpdateStatus($"[+] Config selectionnee: {project}-REF{reference} - Cliquez Charger pour ouvrir", false);
                }
            }
            catch
            {
                // Ignorer les erreurs
            }
        }

        /// <summary>
        /// Bouton "Charger" - Charge la configuration selectionnee dans la ComboBox
        /// </summary>
        private void BtnLoadConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CmbExistingConfigs.SelectedItem == null)
                {
                    XnrgyMessageBox.ShowWarning("Veuillez selectionner une configuration a charger.", "Selection requise", this);
                    return;
                }

                string selectedConfig = CmbExistingConfigs.SelectedItem.ToString() ?? "";
                Logger.Log($"[ConfigUnite] [>] Bouton Charger: {selectedConfig}", Logger.LogLevel.INFO);
                
                string configPath = Path.Combine(CONFIG_UNITES_PATH, selectedConfig + ".config");

                LoadConfigFromFile(configPath);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConfigUnite] [-] Erreur chargement: {ex.Message}", Logger.LogLevel.ERROR);
                UpdateStatus($"[-] Erreur chargement: {ex.Message}", true);
                XnrgyMessageBox.ShowError($"Erreur lors du chargement:\n\n{ex.Message}", "Erreur", this);
            }
        }

        /// <summary>
        /// Charge une configuration depuis un fichier
        /// </summary>
        private void LoadConfigFromFile(string configPath)
        {
            Logger.Log($"[ConfigUnite] [>] Chargement de la configuration: {Path.GetFileName(configPath)}", Logger.LogLevel.INFO);
            
            if (!File.Exists(configPath))
            {
                Logger.Log($"[ConfigUnite] [-] Fichier non trouve: {configPath}", Logger.LogLevel.ERROR);
                XnrgyMessageBox.ShowError($"Fichier non trouve:\n{configPath}", "Erreur", this);
                return;
            }

            var json = File.ReadAllText(configPath);
            var loadedConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigUniteDataModel>(json);

            if (loadedConfig != null)
            {
                _currentConfig = loadedConfig;
                _currentConfigPath = configPath;

                // Mettre a jour les champs du header
                CmbProject.Text = _currentConfig.Project ?? "";
                CmbReference.Text = _currentConfig.Reference ?? "";
                TxtJobTitle.Text = _currentConfig.UnitName ?? "";

                // Mettre a jour l'interface
                UpdateUIFromConfig();
                
                // Log des modules charges
                Logger.Log($"[ConfigUnite] [+] Configuration chargee: Projet={_currentConfig.Project}, Ref={_currentConfig.Reference}", Logger.LogLevel.INFO);
                Logger.Log($"[ConfigUnite]    Modules: {_currentConfig.ModuleDimensions.Count}, Job: {_currentConfig.UnitName}", Logger.LogLevel.DEBUG);

                string configName = Path.GetFileNameWithoutExtension(configPath);
                UpdateStatus($"[+] Configuration chargee: {configName}");
                
                XnrgyMessageBox.ShowSuccess(
                    $"Configuration chargee avec succes:\n\n{configName}\n\n" +
                    $"Projet: {_currentConfig.Project}\n" +
                    $"Reference: REF{_currentConfig.Reference}\n" +
                    $"Job Title: {_currentConfig.UnitName}",
                    "Configuration chargee", this);
            }
            else
            {
                Logger.Log($"[ConfigUnite] [-] Echec deserialisation JSON: {configPath}", Logger.LogLevel.ERROR);
            }
        }

        #endregion

        #region ScrollViewer MouseWheel Support

        /// <summary>
        /// Handler pour activer le scroll molette sur les ScrollViewers
        /// </summary>
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Méthode helper pour trouver les enfants visuels d'un type donné
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (var childOfChild in FindVisualChildren<T>(child)) yield return childOfChild;
            }
        }

        #endregion
    }

    /// <summary>
    /// Convertisseur pour inverser une valeur booléenne
    /// Utilisé pour désactiver des contrôles quand un CheckBox est coché
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }
    }

    /// <summary>
    /// Convertisseur MultiValue qui vérifie si TOUTES les valeurs booléennes sont vraies (AND logique)
    /// Utilisé pour les bindings où plusieurs conditions doivent être remplies
    /// </summary>
    public class MultiBooleanAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            foreach (object value in values)
            {
                if (value is bool boolValue)
                {
                    if (!boolValue)
                        return false;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
