using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using XnrgyEngineeringAutomationTools.Modules.OpenVaultProject.Models;
using XnrgyEngineeringAutomationTools.Modules.OpenVaultProject.Services;
using XnrgyEngineeringAutomationTools.Services;
using XnrgyEngineeringAutomationTools.Shared.Views;

namespace XnrgyEngineeringAutomationTools.Modules.OpenVaultProject.Views
{
    /// <summary>
    /// Fenetre pour telecharger et ouvrir des projets depuis Vault ou Local
    /// </summary>
    public partial class OpenVaultProjectWindow : Window
    {
        private readonly VaultSdkService _vaultService;
        private readonly InventorService _inventorService;
        private VaultDownloadService? _downloadService;
        
        // Selections Vault
        private VaultProjectItem? _selectedProject;
        private VaultProjectItem? _selectedReference;
        private VaultProjectItem? _selectedModule;

        // Selections Local
        private VaultProjectItem? _selectedLocalProject;
        private VaultProjectItem? _selectedLocalReference;
        private VaultProjectItem? _selectedLocalModule;

        // Mode actuel (Local par defaut)
        private bool _isVaultMode = false;
        private const string LOCAL_PROJECTS_ROOT = @"C:\Vault\Engineering\Projects";

        // Timer et progression
        private DispatcherTimer? _progressTimer;
        private Stopwatch _stopwatch = new Stopwatch();
        private int _totalFiles;
        private int _currentFile;
        private double _progressBarWidth;

        public OpenVaultProjectWindow(VaultSdkService vaultService, InventorService inventorService)
        {
            InitializeComponent();
            _vaultService = vaultService ?? throw new ArgumentNullException(nameof(vaultService));
            _inventorService = inventorService ?? throw new ArgumentNullException(nameof(inventorService));
            
            // Initialiser le timer
            _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _progressTimer.Tick += ProgressTimer_Tick;
            
            // S'abonner aux changements de theme
            MainWindow.ThemeChanged += OnThemeChanged;
            this.Closed += (s, e) => MainWindow.ThemeChanged -= OnThemeChanged;
            
            // Appliquer le theme actuel au demarrage
            ApplyTheme(MainWindow.CurrentThemeIsDark);
        }

        /// <summary>
        /// Gestionnaire de changement de theme depuis MainWindow
        /// </summary>
        private void OnThemeChanged(bool isDarkTheme)
        {
            Dispatcher.Invoke(() => ApplyTheme(isDarkTheme));
        }

        /// <summary>
        /// Applique le theme a cette fenetre
        /// </summary>
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
                this.Background = new SolidColorBrush(Color.FromRgb(245, 247, 250)); // Bleu-gris tres clair
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _downloadService = new VaultDownloadService(_vaultService, _inventorService);
                _downloadService.OnProgress += OnDownloadProgress;
                _downloadService.OnFileProgress += OnFileProgress;
                _downloadService.OnConfirmationRequired += OnConfirmationRequired;

                AddLog("[+] Fenetre initialisee", "SUCCESS");
                AddLog($"[i] Workspace: {_downloadService.GetLocalWorkspacePath()}", "INFO");
                
                // Mettre a jour les statuts de connexion
                UpdateVaultStatus();
                UpdateInventorStatus();
                
                // Charger selon l'onglet actif (Local par defaut maintenant)
                // Mettre a jour l'etat initial selon l'onglet selectionne
                _isVaultMode = TabVault.IsSelected;
                
                if (_isVaultMode)
                {
                    // Mode Vault
                    TxtHeaderSubtitle.Text = "Telecharger et ouvrir un module depuis Vault";
                    BtnDownload.Content = "📥 Telecharger et Ouvrir";
                    BtnRefresh.Visibility = Visibility.Visible;
                    CheckoutOptionBorder.Visibility = Visibility.Visible;
                    LoadProjects();
                }
                else
                {
                    // Mode Local (par defaut)
                    TxtHeaderSubtitle.Text = "Ouvrir un module depuis le disque local";
                    BtnDownload.Content = "📂 Ouvrir";
                    BtnRefresh.Visibility = Visibility.Collapsed;
                    CheckoutOptionBorder.Visibility = Visibility.Collapsed;
                    LoadLocalProjects();
                }
            }
            catch (Exception ex)
            {
                AddLog($"[-] Erreur initialisation: {ex.Message}", "ERROR");
                Logger.LogException("OpenVaultProject.Window_Loaded", ex, Logger.LogLevel.ERROR);
            }
        }

        /// <summary>
        /// Met a jour l'indicateur de statut Vault
        /// </summary>
        private void UpdateVaultStatus()
        {
            try
            {
                if (_vaultService != null && _vaultService.IsConnected)
                {
                    VaultStatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(16, 124, 16)); // Vert
                    RunVaultName.Text = $" Vault: {_vaultService.VaultName ?? "--"}";
                    RunUserName.Text = $" {_vaultService.UserName ?? "--"}";
                    RunStatus.Text = " Connecte";
                    AddLog($"[+] Connexion Vault active: {_vaultService.UserName ?? "N/A"}@{_vaultService.ServerName ?? "N/A"}/{_vaultService.VaultName ?? "N/A"}", "SUCCESS");
                }
                else
                {
                    VaultStatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(232, 17, 35)); // Rouge
                    RunVaultName.Text = " Vault: --";
                    RunUserName.Text = " --";
                    RunStatus.Text = " Deconnecte";
                    AddLog("[!] Vault non connecte", "WARNING");
                }
            }
            catch (Exception ex)
            {
                VaultStatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(232, 17, 35)); // Rouge
                RunVaultName.Text = " Vault: --";
                RunUserName.Text = " --";
                RunStatus.Text = " Deconnecte";
                AddLog($"[!] Erreur verification Vault: {ex.Message}", "WARNING");
            }
        }

        /// <summary>
        /// Met a jour l'indicateur de statut Inventor
        /// </summary>
        private void UpdateInventorStatus()
        {
            try
            {
                if (_inventorService != null && _inventorService.IsConnected)
                {
                    InventorStatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(16, 124, 16)); // Vert
                    RunInventorStatus.Text = " Inventor : Connecte";
                    AddLog("[+] Inventor detecte et connecte", "SUCCESS");
                }
                else
                {
                    InventorStatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(232, 17, 35)); // Rouge
                    RunInventorStatus.Text = " Inventor : Deconnecte";
                    AddLog("[i] Inventor non detecte", "INFO");
                }
            }
            catch (Exception ex)
            {
                InventorStatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(232, 17, 35)); // Rouge
                RunInventorStatus.Text = " Inventor : Deconnecte";
                AddLog($"[i] Inventor non detecte: {ex.Message}", "INFO");
            }
        }

        private void TabSourceSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // IMPORTANT: Ne traiter que les changements d'onglet du TabControl, pas les ListBox
            if (e.Source != TabSourceSelection) return;
            
            // Ignorer si les controles ne sont pas encore initialises
            if (TabSourceSelection == null || TabVault == null || TxtHeaderSubtitle == null || 
                BtnDownload == null || BtnRefresh == null || !IsLoaded)
                return;

            _isVaultMode = TabVault.IsSelected;
            
            // Mettre a jour le sous-titre et le bouton
            if (_isVaultMode)
            {
                TxtHeaderSubtitle.Text = "Telecharger et ouvrir un module depuis Vault";
                BtnDownload.Content = "📥 Telecharger et Ouvrir";
                BtnRefresh.Visibility = Visibility.Visible;
                CheckoutOptionBorder.Visibility = Visibility.Visible; // Afficher option Check-Out
                LoadProjects();
            }
            else
            {
                TxtHeaderSubtitle.Text = "Ouvrir un module depuis le disque local";
                BtnDownload.Content = "📂 Ouvrir";
                BtnRefresh.Visibility = Visibility.Collapsed;
                CheckoutOptionBorder.Visibility = Visibility.Collapsed; // Cacher option Check-Out
                LoadLocalProjects();
            }
            
            // Desactiver le bouton jusqu'a selection complete
            BtnDownload.IsEnabled = false;
        }

        private void LoadProjects()
        {
            if (_downloadService == null) return;

            AddLog("[>] Chargement des projets Vault...", "START");
            TxtStatus.Text = "Chargement des projets Vault...";

            try
            {
                var projects = _downloadService.GetProjects();
                LstProjects.ItemsSource = projects;
                TxtProjectCount.Text = $" ({projects.Count})";
                
                // Reset les autres colonnes
                LstReferences.ItemsSource = null;
                LstModules.ItemsSource = null;
                TxtRefCount.Text = " (0)";
                TxtModuleCount.Text = " (0)";
                BtnDownload.IsEnabled = false;

                AddLog($"[+] {projects.Count} projets charges", "SUCCESS");
                TxtStatus.Text = $"{projects.Count} projets disponibles - Selectionnez un projet";
            }
            catch (Exception ex)
            {
                AddLog($"[-] Erreur chargement projets: {ex.Message}", "ERROR");
                TxtStatus.Text = "Erreur lors du chargement";
            }
        }

        #region Local Mode Methods

        private void LoadLocalProjects()
        {
            AddLog("[>] Chargement des projets locaux...", "START");
            TxtStatus.Text = "Chargement des projets locaux...";

            try
            {
                var projects = new List<VaultProjectItem>();
                
                if (Directory.Exists(LOCAL_PROJECTS_ROOT))
                {
                    foreach (var dir in Directory.GetDirectories(LOCAL_PROJECTS_ROOT))
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        // Ne garder que les dossiers numeriques (projets)
                        if (dirInfo.Name.All(char.IsDigit) && dirInfo.Name.Length >= 4)
                        {
                            projects.Add(new VaultProjectItem
                            {
                                Name = dirInfo.Name,
                                Path = dirInfo.FullName,
                                Type = "Project",
                                LastModified = dirInfo.LastWriteTime
                            });
                        }
                    }
                }

                // Trier par nom
                projects = projects.OrderByDescending(p => p.Name).ToList();
                
                LstLocalProjects.ItemsSource = projects;
                TxtLocalProjectCount.Text = $" ({projects.Count})";
                
                // Reset les autres colonnes
                LstLocalReferences.ItemsSource = null;
                LstLocalModules.ItemsSource = null;
                TxtLocalRefCount.Text = " (0)";
                TxtLocalModuleCount.Text = " (0)";
                BtnDownload.IsEnabled = false;

                AddLog($"[+] {projects.Count} projets locaux trouves", "SUCCESS");
                TxtStatus.Text = $"{projects.Count} projets locaux - Selectionnez un projet";
            }
            catch (Exception ex)
            {
                AddLog($"[-] Erreur chargement projets locaux: {ex.Message}", "ERROR");
                TxtStatus.Text = "Erreur lors du chargement";
            }
        }

        private void LstLocalProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedLocalProject = LstLocalProjects.SelectedItem as VaultProjectItem;
            _selectedLocalReference = null;
            _selectedLocalModule = null;

            LstLocalReferences.ItemsSource = null;
            LstLocalModules.ItemsSource = null;
            TxtLocalRefCount.Text = " (0)";
            TxtLocalModuleCount.Text = " (0)";
            BtnDownload.IsEnabled = false;

            if (_selectedLocalProject != null)
            {
                AddLog($"[>] Chargement des references pour {_selectedLocalProject.Name}...", "INFO");
                TxtStatus.Text = $"Chargement des references pour {_selectedLocalProject.Name}...";

                var references = new List<VaultProjectItem>();
                
                foreach (var dir in Directory.GetDirectories(_selectedLocalProject.Path))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    // Chercher les dossiers REFxx
                    if (dirInfo.Name.StartsWith("REF", StringComparison.OrdinalIgnoreCase))
                    {
                        references.Add(new VaultProjectItem
                        {
                            Name = dirInfo.Name,
                            Path = dirInfo.FullName,
                            Type = "Reference",
                            LastModified = dirInfo.LastWriteTime
                        });
                    }
                }

                references = references.OrderByDescending(r => r.Name).ToList();
                LstLocalReferences.ItemsSource = references;
                TxtLocalRefCount.Text = $" ({references.Count})";

                AddLog($"[+] {references.Count} references trouvees", "SUCCESS");
                TxtStatus.Text = $"Projet {_selectedLocalProject.Name} - {references.Count} references";
            }
        }

        private void LstLocalReferences_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedLocalReference = LstLocalReferences.SelectedItem as VaultProjectItem;
            _selectedLocalModule = null;

            LstLocalModules.ItemsSource = null;
            TxtLocalModuleCount.Text = " (0)";
            BtnDownload.IsEnabled = false;

            if (_selectedLocalReference != null)
            {
                AddLog($"[>] Chargement des modules pour {_selectedLocalReference.Name}...", "INFO");
                TxtStatus.Text = $"Chargement des modules pour {_selectedLocalReference.Name}...";

                var modules = new List<VaultProjectItem>();
                
                foreach (var dir in Directory.GetDirectories(_selectedLocalReference.Path))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    // Chercher les dossiers Mxx
                    if (dirInfo.Name.StartsWith("M", StringComparison.OrdinalIgnoreCase) && 
                        dirInfo.Name.Length >= 2 && 
                        dirInfo.Name.Substring(1).All(char.IsDigit))
                    {
                        modules.Add(new VaultProjectItem
                        {
                            Name = dirInfo.Name,
                            Path = dirInfo.FullName,
                            Type = "Module",
                            LastModified = dirInfo.LastWriteTime
                        });
                    }
                }

                modules = modules.OrderBy(m => m.Name).ToList();
                LstLocalModules.ItemsSource = modules;
                TxtLocalModuleCount.Text = $" ({modules.Count})";

                AddLog($"[+] {modules.Count} modules trouves", "SUCCESS");
                TxtStatus.Text = $"{_selectedLocalProject?.Name}/{_selectedLocalReference.Name} - {modules.Count} modules";
            }
        }

        private void LstLocalModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedLocalModule = LstLocalModules.SelectedItem as VaultProjectItem;
            BtnDownload.IsEnabled = _selectedLocalModule != null;

            if (_selectedLocalModule != null)
            {
                TxtStatus.Text = $"Module selectionne: {_selectedLocalProject?.Name}/{_selectedLocalReference?.Name}/{_selectedLocalModule.Name}";
                AddLog($"[i] Module local selectionne: {_selectedLocalModule.Path}", "INFO");
            }
        }

        #endregion

        private void LstProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedProject = LstProjects.SelectedItem as VaultProjectItem;
            _selectedReference = null;
            _selectedModule = null;

            LstReferences.ItemsSource = null;
            LstModules.ItemsSource = null;
            TxtRefCount.Text = " (0)";
            TxtModuleCount.Text = " (0)";
            BtnDownload.IsEnabled = false;

            if (_selectedProject != null)
            {
                if (_downloadService == null)
                {
                    AddLog("[-] Service non initialise!", "ERROR");
                    return;
                }

                AddLog($"[>] Chargement des references pour {_selectedProject.Name}...", "INFO");
                TxtStatus.Text = $"Chargement des references pour {_selectedProject.Name}...";

                try
                {
                    var references = _downloadService.GetReferences(_selectedProject.Path);
                    LstReferences.ItemsSource = references;
                    TxtRefCount.Text = $" ({references.Count})";

                    AddLog($"[+] {references.Count} references trouvees", "SUCCESS");
                    TxtStatus.Text = $"Projet {_selectedProject.Name} - {references.Count} references";
                }
                catch (Exception ex)
                {
                    AddLog($"[-] Erreur chargement references: {ex.Message}", "ERROR");
                }
            }
        }

        private void LstReferences_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedReference = LstReferences.SelectedItem as VaultProjectItem;
            _selectedModule = null;

            LstModules.ItemsSource = null;
            TxtModuleCount.Text = " (0)";
            BtnDownload.IsEnabled = false;

            if (_selectedReference != null && _downloadService != null)
            {
                AddLog($"[>] Chargement des modules pour {_selectedReference.Name}...", "INFO");
                TxtStatus.Text = $"Chargement des modules pour {_selectedReference.Name}...";

                var modules = _downloadService.GetModules(_selectedReference.Path);
                LstModules.ItemsSource = modules;
                TxtModuleCount.Text = $" ({modules.Count})";

                AddLog($"[+] {modules.Count} modules trouves", "SUCCESS");
                TxtStatus.Text = $"{_selectedProject?.Name}/{_selectedReference.Name} - {modules.Count} modules";
            }
        }

        private void LstModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedModule = LstModules.SelectedItem as VaultProjectItem;
            BtnDownload.IsEnabled = _selectedModule != null;

            if (_selectedModule != null)
            {
                TxtStatus.Text = $"Module selectionne: {_selectedProject?.Name}/{_selectedReference?.Name}/{_selectedModule.Name}";
                AddLog($"[i] Module selectionne: {_selectedModule.Path}", "INFO");
            }
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (_isVaultMode)
            {
                await DownloadFromVaultAsync();
            }
            else
            {
                await OpenLocalModuleAsync();
            }
        }

        private async Task DownloadFromVaultAsync()
        {
            if (_selectedModule == null || _downloadService == null)
            {
                AddLog("[-] Aucun module selectionne", "ERROR");
                return;
            }

            // Desactiver les controles pendant le telechargement
            SetControlsEnabled(false);

            // Demarrer la progression
            StartProgress();

            try
            {
                // Option Check-Out recursif
                bool checkoutOnDownload = ChkCheckoutOnDownload.IsChecked == true;
                string modeText = checkoutOnDownload ? "Check-Out" : "Telechargement";
                
                AddLog($"[>] Debut du {modeText}: {_selectedModule.Path}", "START");
                if (checkoutOnDownload)
                {
                    AddLog("[i] Mode Check-Out recursif active - Les fichiers seront verouilles", "INFO");
                }
                TxtStatus.Text = $"{modeText} en cours...";

                bool success = await _downloadService.DownloadAndOpenModuleAsync(_selectedModule, checkoutOnDownload);

                if (success)
                {
                    // Progression complete
                    UpdateProgressBar(100);
                    TxtProgressPercent.Text = "100%";
                    string successMsg = checkoutOnDownload 
                        ? "[+] Module telecharge avec Check-Out et ouvert avec succes" 
                        : "[+] Module telecharge et ouvert avec succes";
                    AddLog(successMsg, "SUCCESS");
                    TxtStatus.Text = "Module ouvert dans Inventor";
                }
                else
                {
                    AddLog($"[!] {modeText} termine avec des avertissements", "WARN");
                    TxtStatus.Text = $"{modeText} termine (verifier le journal)";
                }
            }
            catch (Exception ex)
            {
                AddLog($"[-] Erreur: {ex.Message}", "ERROR");
                TxtStatus.Text = "Erreur lors du telechargement";
                Logger.LogException("OpenVaultProject.DownloadFromVaultAsync", ex, Logger.LogLevel.ERROR);
            }
            finally
            {
                // Arreter la progression
                StopProgress();
                
                // Reactiver les controles
                SetControlsEnabled(true);
            }
        }

        private async Task OpenLocalModuleAsync()
        {
            if (_selectedLocalModule == null || _downloadService == null)
            {
                AddLog("[-] Aucun module local selectionne", "ERROR");
                return;
            }

            // Desactiver les controles
            SetControlsEnabled(false);

            // Demarrer la progression
            StartProgress();

            try
            {
                AddLog($"[>] Ouverture du module local: {_selectedLocalModule.Path}", "START");
                TxtStatus.Text = "Ouverture en cours...";

                // Trouver le fichier .iam master dans le module
                string? masterIamPath = FindMasterIam(_selectedLocalModule.Path);
                
                if (string.IsNullOrEmpty(masterIamPath))
                {
                    AddLog("[-] Aucun fichier .iam trouve dans le module", "ERROR");
                    TxtStatus.Text = "Erreur: pas de fichier .iam";
                    return;
                }

                AddLog($"[i] Fichier master: {Path.GetFileName(masterIamPath)}", "INFO");

                // Progression
                UpdateProgressBar(20);
                TxtProgressPercent.Text = "20%";

                // Switch IPJ et ouvrir via le service
                bool success = await Task.Run(() =>
                {
                    try
                    {
                        // Utiliser PrepareViewAfterOpen pour switch IPJ, nettoyer et zoom
                        return _downloadService.OpenLocalModule(masterIamPath);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => AddLog($"[-] Erreur ouverture: {ex.Message}", "ERROR"));
                        return false;
                    }
                });

                if (success)
                {
                    UpdateProgressBar(100);
                    TxtProgressPercent.Text = "100%";
                    AddLog("[+] Module local ouvert avec succes", "SUCCESS");
                    TxtStatus.Text = "Module ouvert dans Inventor";
                }
                else
                {
                    AddLog("[!] Ouverture terminee avec des avertissements", "WARN");
                    TxtStatus.Text = "Ouverture terminee (verifier le journal)";
                }
            }
            catch (Exception ex)
            {
                AddLog($"[-] Erreur: {ex.Message}", "ERROR");
                TxtStatus.Text = "Erreur lors de l'ouverture";
                Logger.LogException("OpenVaultProject.OpenLocalModuleAsync", ex, Logger.LogLevel.ERROR);
            }
            finally
            {
                StopProgress();
                SetControlsEnabled(true);
            }
        }

        private string? FindMasterIam(string modulePath)
        {
            // Chercher les fichiers .iam dans le module
            var iamFiles = Directory.GetFiles(modulePath, "*.iam", SearchOption.TopDirectoryOnly);
            
            if (iamFiles.Length == 0) return null;
            if (iamFiles.Length == 1) return iamFiles[0];

            // Si plusieurs .iam, chercher celui qui correspond au pattern du module
            string moduleName = Path.GetFileName(modulePath);
            
            // Priority 1: Exact match with module name
            var exactMatch = iamFiles.FirstOrDefault(f => 
                Path.GetFileNameWithoutExtension(f).Equals(moduleName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null) return exactMatch;

            // Priority 2: Contains module name (case insensitive)
            var containsMatch = iamFiles.FirstOrDefault(f => 
                Path.GetFileNameWithoutExtension(f).IndexOf(moduleName, StringComparison.OrdinalIgnoreCase) >= 0);
            if (containsMatch != null) return containsMatch;

            // Priority 3: Not a sub-assembly (doesn't contain "Sub" or other keywords)
            var mainAssembly = iamFiles.FirstOrDefault(f => 
                Path.GetFileNameWithoutExtension(f).IndexOf("Sub", StringComparison.OrdinalIgnoreCase) < 0 &&
                !Path.GetFileNameWithoutExtension(f).Contains("_"));
            if (mainAssembly != null) return mainAssembly;

            // Default: first file
            return iamFiles[0];
        }

        private void SetControlsEnabled(bool enabled)
        {
            BtnDownload.IsEnabled = enabled && (_isVaultMode ? _selectedModule != null : _selectedLocalModule != null);
            BtnRefresh.IsEnabled = enabled;
            LstProjects.IsEnabled = enabled;
            LstReferences.IsEnabled = enabled;
            LstModules.IsEnabled = enabled;
            LstLocalProjects.IsEnabled = enabled;
            LstLocalReferences.IsEnabled = enabled;
            LstLocalModules.IsEnabled = enabled;
            TabSourceSelection.IsEnabled = enabled;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadProjects();
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            LstLog.Items.Clear();
            AddLog("[i] Journal efface", "INFO");
        }

        private void OnDownloadProgress(string message, string level)
        {
            Dispatcher.Invoke(() =>
            {
                AddLog(message, level);
                
                // Extraire le nom du fichier si c'est un telechargement
                if (message.StartsWith("Telechargement:"))
                {
                    string fileName = message.Replace("Telechargement:", "").Trim();
                    TxtCurrentFile.Text = fileName;
                }
                else if (!message.Contains("fichier"))
                {
                    TxtStatus.Text = message;
                }
            });
        }

        private void OnFileProgress(int current, int total)
        {
            Dispatcher.Invoke(() =>
            {
                _currentFile = current;
                _totalFiles = total;
                
                // Calculer le pourcentage
                double percent = total > 0 ? (double)current / total * 100 : 0;
                TxtProgressPercent.Text = $"{percent:F0}%";
                TxtFileProgress.Text = $"({current}/{total})";
                
                // Mettre a jour la barre de progression
                UpdateProgressBar(percent);
                
                // Estimer le temps restant
                if (current > 0 && _stopwatch.IsRunning)
                {
                    double elapsed = _stopwatch.Elapsed.TotalSeconds;
                    double avgTimePerFile = elapsed / current;
                    double remaining = avgTimePerFile * (total - current);
                    
                    TimeSpan estimatedRemaining = TimeSpan.FromSeconds(remaining);
                    TimeSpan estimatedTotal = TimeSpan.FromSeconds(elapsed + remaining);
                    TxtProgressTimeEstimated.Text = FormatTime(estimatedTotal);
                }
            });
        }

        /// <summary>
        /// Handler pour les demandes de confirmation (ex: suppression dossier local)
        /// </summary>
        private bool OnConfirmationRequired(string title, string message)
        {
            bool result = false;
            
            Dispatcher.Invoke(() =>
            {
                // Utilisation de XnrgyMessageBox au lieu de MessageBox standard
                result = XnrgyMessageBox.Confirm(message, title, this);
                
                if (result)
                {
                    AddLog("[i] Utilisateur a confirme la suppression du dossier local", "INFO");
                }
                else
                {
                    AddLog("[i] Utilisateur a refuse la suppression du dossier local", "INFO");
                }
            });
            
            return result;
        }

        private void ProgressTimer_Tick(object? sender, EventArgs e)
        {
            if (_stopwatch.IsRunning)
            {
                TxtProgressTimeElapsed.Text = FormatTime(_stopwatch.Elapsed);
            }
        }

        private void UpdateProgressBar(double percent)
        {
            // Calculer la largeur de la barre de progression
            var container = ProgressBarFill.Parent as Grid;
            if (container != null)
            {
                double maxWidth = container.ActualWidth > 0 ? container.ActualWidth : 600;
                double targetWidth = maxWidth * (percent / 100);
                ProgressBarFill.Width = targetWidth;
            }
        }

        private void ShowProgressUI(bool show)
        {
            var visibility = show ? Visibility.Visible : Visibility.Collapsed;
            TxtTimeLabel.Visibility = visibility;
            TxtProgressTimeElapsed.Visibility = visibility;
            TxtEstimatedLabel.Visibility = visibility;
            TxtProgressTimeEstimated.Visibility = visibility;
            TxtSeparator.Visibility = visibility;
            TxtProgressPercent.Visibility = visibility;
            
            if (!show)
            {
                ProgressBarFill.Width = 0;
                TxtCurrentFile.Text = "";
                TxtFileProgress.Text = "";
            }
        }

        private void StartProgress()
        {
            _stopwatch.Reset();
            _stopwatch.Start();
            _progressTimer?.Start();
            _currentFile = 0;
            _totalFiles = 0;
            
            ShowProgressUI(true);
            TxtProgressTimeElapsed.Text = "00:00";
            TxtProgressTimeEstimated.Text = "--:--";
            TxtProgressPercent.Text = "0%";
        }

        private void StopProgress()
        {
            _stopwatch.Stop();
            _progressTimer?.Stop();
        }

        private string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
            return $"{time.Minutes:D2}:{time.Seconds:D2}";
        }

        private void AddLog(string message, string level)
        {
            Dispatcher.Invoke(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string text = $"[{timestamp}] {message}";

                var textBlock = new TextBlock
                {
                    Text = text,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                    Padding = new Thickness(8, 2, 8, 2),
                    TextWrapping = TextWrapping.Wrap
                };

                // Detection automatique du niveau basee sur le prefixe du message
                // Utilise JournalColorService pour uniformite avec les autres formulaires
                var trimmedMsg = message.TrimStart();
                
                if (trimmedMsg.StartsWith("[+]"))
                {
                    textBlock.Foreground = XnrgyEngineeringAutomationTools.Services.JournalColorService.SuccessBrush;  // Vert #00FF7F
                }
                else if (trimmedMsg.StartsWith("[-]"))
                {
                    textBlock.Foreground = XnrgyEngineeringAutomationTools.Services.JournalColorService.ErrorBrush;    // Rouge #FF4444
                    textBlock.FontWeight = FontWeights.Bold;
                }
                else if (trimmedMsg.StartsWith("[!]"))
                {
                    textBlock.Foreground = XnrgyEngineeringAutomationTools.Services.JournalColorService.WarningBrush;  // Jaune #FFD700
                }
                else
                {
                    textBlock.Foreground = XnrgyEngineeringAutomationTools.Services.JournalColorService.InfoBrush;     // Blanc #FFFFFF (defaut)
                }

                LstLog.Items.Add(textBlock);
                LstLog.ScrollIntoView(textBlock);

                // Limiter le nombre de lignes
                while (LstLog.Items.Count > 100)
                {
                    LstLog.Items.RemoveAt(0);
                }
            });
        }
    }
}
