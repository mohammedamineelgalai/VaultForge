using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using XnrgyEngineeringAutomationTools.Modules.ConfigUnite.Models;
using XnrgyEngineeringAutomationTools.Services;

namespace XnrgyEngineeringAutomationTools.Modules.CreateModule.Views
{
    /// <summary>
    /// Fenetre de lecture des configurations de modules depuis ConfigUnite
    /// Permet de selectionner un module et d'afficher ses configurations en lecture seule
    /// Sera utilisee par CreateModule pour charger les configs pre-remplies
    /// </summary>
    public partial class ModuleConfigReaderWindow : Window
    {
        private ConfigUniteDataModel _config;
        private List<ModuleDimension> _modules;
        private string _lastConfigPath;

        public ModuleConfigReaderWindow()
        {
            InitializeComponent();
            Logger.Log("[ModuleConfigReader] [+] Fenetre initialisee", Logger.LogLevel.DEBUG);
            
            // S'abonner aux changements de theme
            MainWindow.ThemeChanged += OnThemeChanged;
            this.Closed += (s, e) => MainWindow.ThemeChanged -= OnThemeChanged;
            ApplyTheme(MainWindow.CurrentThemeIsDark);
            
            // Tenter de charger automatiquement le dernier fichier config
            TryAutoLoadConfig();
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
        /// Tente de charger automatiquement le fichier ConfigUnite.json depuis le dossier par defaut
        /// </summary>
        private void TryAutoLoadConfig()
        {
            try
            {
                // Chemin par defaut du fichier ConfigUnite
                string defaultPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "XnrgyEngineeringAutomationTools", "ConfigUnite", "ConfigUnite.json");
                
                if (File.Exists(defaultPath))
                {
                    LoadConfigFromFile(defaultPath);
                    Logger.Log($"[ModuleConfigReader] [+] Config auto-chargee depuis: {defaultPath}", Logger.LogLevel.INFO);
                }
                else
                {
                    // Essayer le dossier du projet
                    string projectPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, "Modules", "ConfigUnite", "Data", "ConfigUnite.json");
                    
                    if (File.Exists(projectPath))
                    {
                        LoadConfigFromFile(projectPath);
                        Logger.Log($"[ModuleConfigReader] [+] Config auto-chargee depuis: {projectPath}", Logger.LogLevel.INFO);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ModuleConfigReader] [!] Erreur auto-load: {ex.Message}", Logger.LogLevel.WARNING);
            }
        }

        /// <summary>
        /// Charge le fichier de configuration depuis le chemin specifie
        /// </summary>
        private void LoadConfigFromFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };
                
                _config = JsonSerializer.Deserialize<ConfigUniteDataModel>(json, options);
                _lastConfigPath = filePath;
                
                if (_config != null && _config.ModuleDimensions != null)
                {
                    _modules = _config.ModuleDimensions;
                    PopulateModuleSelector();
                    
                    TxtConfigStatus.Text = $"Charge ({_modules.Count} modules)";
                    TxtConfigStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x00, 0xB8, 0x94));
                    TxtStatus.Text = $"Configuration chargee: {Path.GetFileName(filePath)}";
                    
                    Logger.Log($"[ModuleConfigReader] [+] Config chargee: {_modules.Count} modules", Logger.LogLevel.INFO);
                }
                else
                {
                    _modules = new List<ModuleDimension>();
                    TxtConfigStatus.Text = "Vide";
                    TxtStatus.Text = "Fichier charge mais aucun module trouve";
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ModuleConfigReader] [-] Erreur chargement: {ex.Message}", Logger.LogLevel.ERROR);
                MessageBox.Show($"Erreur lors du chargement:\n{ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Remplit le ComboBox avec la liste des modules
        /// </summary>
        private void PopulateModuleSelector()
        {
            CmbModuleSelector.Items.Clear();
            CmbModuleSelector.Items.Add(new ComboBoxItem { Content = "-- Selectionner un module --", IsSelected = true });
            
            if (_modules == null) return;
            
            foreach (var module in _modules)
            {
                string displayText = $"{module.ModuleNumber} - {module.Height}H x {module.Width}W x {module.Length}L";
                CmbModuleSelector.Items.Add(new ComboBoxItem { Content = displayText, Tag = module });
            }
        }

        /// <summary>
        /// Affiche les configurations du module selectionne
        /// </summary>
        private void DisplayModuleConfig(ModuleDimension module)
        {
            if (module == null)
            {
                PanelNoSelection.Visibility = Visibility.Visible;
                PanelModuleConfig.Visibility = Visibility.Collapsed;
                return;
            }
            
            PanelNoSelection.Visibility = Visibility.Collapsed;
            PanelModuleConfig.Visibility = Visibility.Visible;
            
            // Module Dimensions
            TxtModuleNumber.Text = module.ModuleNumber ?? "";
            TxtHeight.Text = module.Height ?? "";
            TxtWidth.Text = module.Width ?? "";
            TxtLength.Text = module.Length ?? "";
            
            // Stacked & Tunnel
            TxtStackedPosition.Text = module.TunnelPosition ?? "None";
            TxtTunnelAirFlow.Text = module.AirFlowDirection ?? "None";
            TxtTunnelType.Text = module.TunnelType ?? "Tunnel";
            
            // IW Left
            ChkIWLeftActive.IsChecked = module.HasInteriorWallLeft;
            TxtIWLeftDistance.Text = module.InteriorWallLeftDistance.ToString("0");
            TxtIWLeftThickness.Text = module.InteriorWallLeftThickness ?? "4";
            
            // IW Right
            ChkIWRightActive.IsChecked = module.HasInteriorWallRight;
            TxtIWRightDistance.Text = module.InteriorWallRightDistance.ToString("0");
            TxtIWRightThickness.Text = module.InteriorWallRightThickness ?? "4";
            
            // IW Front/Back
            ChkIWFBActive.IsChecked = module.HasInteriorWallFB;
            TxtIWFBDistance.Text = module.InteriorWallFBDistance.ToString("0");
            TxtIWFBThickness.Text = module.InteriorWallFBThickness ?? "4";
            
            TxtStatus.Text = $"Module selectionne: {module.ModuleNumber}";
            Logger.Log($"[ModuleConfigReader] [>] Affichage config: {module.ModuleNumber}", Logger.LogLevel.DEBUG);
        }

        #region Event Handlers

        private void CmbModuleSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbModuleSelector.SelectedItem is ComboBoxItem item && item.Tag is ModuleDimension module)
            {
                DisplayModuleConfig(module);
            }
            else
            {
                DisplayModuleConfig(null);
            }
        }

        private void BtnLoadConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Selectionner le fichier ConfigUnite",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".json"
            };
            
            // Proposer le dernier chemin utilise
            if (!string.IsNullOrEmpty(_lastConfigPath) && Directory.Exists(Path.GetDirectoryName(_lastConfigPath)))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(_lastConfigPath);
            }
            
            if (dialog.ShowDialog() == true)
            {
                LoadConfigFromFile(dialog.FileName);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_lastConfigPath) && File.Exists(_lastConfigPath))
            {
                LoadConfigFromFile(_lastConfigPath);
                TxtStatus.Text = "Configuration rafraichie";
            }
            else
            {
                MessageBox.Show("Aucun fichier de configuration charge.\nCliquez 'Charger ConfigUnite' d'abord.", 
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnOpenConfigUnite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ouvrir la fenetre ConfigUnite
                var configUniteWindow = new XnrgyEngineeringAutomationTools.Modules.ConfigUnite.Views.ConfigUniteWindow();
                configUniteWindow.Show();
                Logger.Log("[ModuleConfigReader] [>] Ouverture ConfigUnite", Logger.LogLevel.INFO);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ModuleConfigReader] [-] Erreur ouverture ConfigUnite: {ex.Message}", Logger.LogLevel.ERROR);
                MessageBox.Show($"Erreur lors de l'ouverture de ConfigUnite:\n{ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }
}
