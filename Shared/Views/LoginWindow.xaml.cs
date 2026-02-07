using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using XnrgyEngineeringAutomationTools.Services;

namespace XnrgyEngineeringAutomationTools.Shared.Views
{
    public partial class LoginWindow : Window
    {
        private readonly VaultSdkService _vaultService;
        private Storyboard _spinnerStoryboard;
        private bool _autoConnectMode = false;
        private bool _isConnecting = false;
        private bool _closeAllowed = false;

        public LoginWindow(VaultSdkService vaultService, bool autoConnect = false)
        {
            InitializeComponent();
            _vaultService = vaultService;
            _autoConnectMode = autoConnect;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Charger les identifiants sauvegardés
            LoadSavedCredentials();
            
            // Préparer l'animation du spinner
            _spinnerStoryboard = (Storyboard)FindResource("SpinnerAnimation");
            
            // Si mode auto-connect et credentials complets, tenter la connexion auto
            if (_autoConnectMode)
            {
                await TryAutoConnect();
            }
        }
        
        /// <summary>
        /// Tente une connexion automatique si les credentials sont sauvegardés
        /// </summary>
        private async Task TryAutoConnect()
        {
            try
            {
                var credentials = CredentialsManager.Load();
                
                // Vérifier si credentials complets
                if (credentials.SaveCredentials && 
                    !string.IsNullOrEmpty(credentials.Username) &&
                    !string.IsNullOrEmpty(credentials.Password) &&
                    !string.IsNullOrEmpty(credentials.Server) &&
                    !string.IsNullOrEmpty(credentials.VaultName))
                {
                // Désactiver les contrôles et afficher le spinner
                SetControlsEnabled(false);
                ShowConnectionProgress("🔄 Connexion automatique en cours...");
                
                Logger.Log($"[>] Connexion automatique a {credentials.Server}/{credentials.VaultName}...", Logger.LogLevel.INFO);                    // Connexion asynchrone
                    bool success = await Task.Run(() => 
                        _vaultService.Connect(credentials.Server, credentials.VaultName, credentials.Username, credentials.Password));
                    
                if (success)
                {
                    ShowConnectionProgress("✅ Connexion reussie!");
                    Logger.Log($"[+] Connexion automatique reussie ({credentials.Username})", Logger.LogLevel.INFO);                        await Task.Delay(800); // Délai pour voir le succès
                        
                        _closeAllowed = true;  // Autoriser la fermeture apres succes
                        DialogResult = true;
                        Close();
                        return;
                    }
                    else
                    {
                        Logger.Log($"[!] Connexion automatique echouee - Intervention requise", Logger.LogLevel.WARNING);
                        ShowError("Connexion automatique échouée. Veuillez vérifier vos identifiants.");
                    }
                }
                else
                {
                    Logger.Log("[i] Pas de credentials sauvegardes - Intervention utilisateur requise", Logger.LogLevel.INFO);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[!] Erreur connexion auto: {ex.Message}", Logger.LogLevel.WARNING);
                ShowError($"Erreur: {ex.Message}");
            }
            finally
            {
                HideConnectionProgress();
                SetControlsEnabled(true);
            }
        }

        private void LoadSavedCredentials()
        {
            try
            {
                var credentials = CredentialsManager.Load();
                
                // Toujours charger serveur/vault
                ServerTextBox.Text = credentials.Server;
                VaultTextBox.Text = credentials.VaultName;
                
                // Si les credentials sont sauvegardés, les charger
                if (credentials.SaveCredentials && !string.IsNullOrEmpty(credentials.Username))
                {
                    UserTextBox.Text = credentials.Username;
                    PasswordBox.Password = credentials.Password;
                    SaveCredentialsCheckBox.IsChecked = true;
                    Logger.Log($"[+] Credentials charges pour {credentials.Username}", Logger.LogLevel.INFO);
                }
                else
                {
                    // Champs vides pour forcer la saisie
                    UserTextBox.Text = "";
                    PasswordBox.Password = "";
                    SaveCredentialsCheckBox.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[!] Erreur chargement credentials: {ex.Message}", Logger.LogLevel.DEBUG);
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // Cacher l'erreur précédente
            ErrorBorder.Visibility = Visibility.Collapsed;
            ErrorMessage.Text = "";
            
            string server = ServerTextBox.Text.Trim();
            string vault = VaultTextBox.Text.Trim();
            string user = UserTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(vault) || string.IsNullOrEmpty(user))
            {
                ShowError("Veuillez remplir tous les champs obligatoires.");
                return;
            }

            try
            {
                // Désactiver les contrôles et afficher le spinner
                SetControlsEnabled(false);
                ShowConnectionProgress("🔄 Connexion à Vault en cours...");
                
                Logger.Log($"[>] Tentative de connexion a {server}/{vault}...", Logger.LogLevel.INFO);
                
                // Connexion asynchrone pour ne pas bloquer l'UI
                bool success = await Task.Run(() => _vaultService.Connect(server, vault, user, password));
                
                if (success)
                {
                    ShowConnectionProgress("✅ Connexion reussie!");
                    await Task.Delay(500); // Petit délai pour voir le succès
                    
                    Logger.Log($"[+] Connexion reussie a {server}/{vault}", Logger.LogLevel.INFO);
                    
                    // Sauvegarder les credentials
                    SaveCredentials(server, vault, user, password);
                    
                    _closeAllowed = true;  // Autoriser la fermeture apres succes
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ShowError("Echec de la connexion. Verifiez vos identifiants.");
                    Logger.Log($"[-] Echec de connexion a {server}/{vault}", Logger.LogLevel.ERROR);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Erreur: {ex.Message}");
                Logger.Log($"[-] Erreur connexion: {ex.Message}", Logger.LogLevel.ERROR);
            }
            finally
            {
                HideConnectionProgress();
                SetControlsEnabled(true);
            }
        }

        private void SaveCredentials(string server, string vault, string user, string password)
        {
            var credentials = new CredentialsManager.VaultCredentials
            {
                Server = server,
                VaultName = vault,
                Username = SaveCredentialsCheckBox.IsChecked == true ? user : "",
                Password = SaveCredentialsCheckBox.IsChecked == true ? password : "",
                SaveCredentials = SaveCredentialsCheckBox.IsChecked == true
            };
            CredentialsManager.Save(credentials);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            UserTextBox.Text = "";
            PasswordBox.Password = "";
            SaveCredentialsCheckBox.IsChecked = false;
            CredentialsManager.Clear();
            ErrorBorder.Visibility = Visibility.Collapsed;
            Logger.Log("[i] Champs et credentials effacés", Logger.LogLevel.INFO);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Empeche la fermeture de la fenetre pendant le processus de connexion
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Permettre la fermeture si autorisee (succes) ou si pas en cours de connexion
            if (_isConnecting && !_closeAllowed)
            {
                // Empecher la fermeture pendant le processus
                e.Cancel = true;
                XnrgyMessageBox.ShowWarning(
                    "La connexion est en cours.\nVeuillez attendre la fin du processus.",
                    "Fermeture impossible",
                    this);
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            _isConnecting = !enabled;  // Connexion en cours si controles desactives
            ServerTextBox.IsEnabled = enabled;
            VaultTextBox.IsEnabled = enabled;
            UserTextBox.IsEnabled = enabled;
            PasswordBox.IsEnabled = enabled;
            SaveCredentialsCheckBox.IsEnabled = enabled;
            ConnectButton.IsEnabled = enabled;
            CancelButton.IsEnabled = enabled;
            ClearButton.IsEnabled = enabled;
            BtnBrowseServer.IsEnabled = enabled;
            BtnBrowseVault.IsEnabled = enabled;
            // Note: Le contenu du bouton est defini dans XAML avec l'emoji
        }

        private void ShowConnectionProgress(string message)
        {
            ConnectionStatusText.Text = message;
            ConnectionSpinner.Visibility = Visibility.Visible;
            _spinnerStoryboard?.Begin(this, true);
        }

        private void HideConnectionProgress()
        {
            ConnectionSpinner.Visibility = Visibility.Collapsed;
            _spinnerStoryboard?.Stop(this);
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Parcourir les serveurs Vault disponibles sur le reseau
        /// </summary>
        private async void BtnBrowseServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetControlsEnabled(false);
                ShowConnectionProgress("🔍 Recherche des serveurs Vault...");

                var servers = await Task.Run(() => _vaultService.DiscoverVaultServers());

                HideConnectionProgress();
                SetControlsEnabled(true);

                if (servers == null || servers.Count == 0)
                {
                    XnrgyMessageBox.ShowInfo(
                        "Aucun serveur Vault detecte sur le reseau.\n\nEntrez manuellement le nom du serveur.",
                        "Aucun serveur trouve",
                        this);
                    return;
                }

                // Afficher la liste des serveurs
                var selectedServer = ShowSelectionDialog("Serveurs Vault detectes", servers);
                if (!string.IsNullOrEmpty(selectedServer))
                {
                    ServerTextBox.Text = selectedServer;
                    Logger.Log($"[+] Serveur selectionne: {selectedServer}");
                }
            }
            catch (Exception ex)
            {
                HideConnectionProgress();
                SetControlsEnabled(true);
                Logger.Log($"[-] Erreur decouverte serveurs: {ex.Message}", Logger.LogLevel.ERROR);
                XnrgyMessageBox.ShowError($"Erreur lors de la recherche des serveurs:\n{ex.Message}", "Erreur", this);
            }
        }

        /// <summary>
        /// Parcourir les Vaults disponibles sur le serveur selectionne
        /// </summary>
        private async void BtnBrowseVault_Click(object sender, RoutedEventArgs e)
        {
            string server = ServerTextBox.Text.Trim();
            if (string.IsNullOrEmpty(server))
            {
                XnrgyMessageBox.ShowWarning("Veuillez d'abord entrer ou selectionner un serveur.", "Serveur requis", this);
                return;
            }

            try
            {
                SetControlsEnabled(false);
                ShowConnectionProgress($"🔍 Recherche des Vaults sur {server}...");

                var vaults = await Task.Run(() => _vaultService.GetVaultNames(server));

                HideConnectionProgress();
                SetControlsEnabled(true);

                if (vaults == null || vaults.Count == 0)
                {
                    XnrgyMessageBox.ShowInfo(
                        $"Aucun Vault trouve sur le serveur '{server}'.\n\nVerifiez que le serveur est correct.",
                        "Aucun Vault trouve",
                        this);
                    return;
                }

                // Afficher la liste des vaults
                var selectedVault = ShowSelectionDialog($"Vaults disponibles sur {server}", vaults);
                if (!string.IsNullOrEmpty(selectedVault))
                {
                    VaultTextBox.Text = selectedVault;
                    Logger.Log($"[+] Vault selectionne: {selectedVault}");
                }
            }
            catch (Exception ex)
            {
                HideConnectionProgress();
                SetControlsEnabled(true);
                Logger.Log($"[-] Erreur liste Vaults: {ex.Message}", Logger.LogLevel.ERROR);
                XnrgyMessageBox.ShowError($"Erreur lors de la recherche des Vaults:\n{ex.Message}", "Erreur", this);
            }
        }

        /// <summary>
        /// Affiche un dialogue de selection simple
        /// </summary>
        private string ShowSelectionDialog(string title, System.Collections.Generic.List<string> items)
        {
            // Creer une fenetre de selection simple
            var selectionWindow = new Window
            {
                Title = title,
                Width = 400,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 46)),
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

            var listBox = new System.Windows.Controls.ListBox
            {
                Margin = new Thickness(15),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(18, 18, 28)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 127, 191)),
                BorderThickness = new Thickness(2),
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };

            foreach (var item in items)
            {
                listBox.Items.Add(item);
            }

            // Selectionner PROD_XNGRY par defaut si present
            if (items.Contains("PROD_XNGRY"))
            {
                listBox.SelectedItem = "PROD_XNGRY";
            }
            else if (listBox.Items.Count > 0)
            {
                listBox.SelectedIndex = 0;
            }

            System.Windows.Controls.Grid.SetRow(listBox, 0);
            grid.Children.Add(listBox);

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(15, 0, 15, 15)
            };

            string selectedItem = null;

            // Style pour bouton vert avec hover glow bleu cyan (comme les autres boutons XNRGY)
            var okButton = new System.Windows.Controls.Button
            {
                Content = "Selectionner",
                Width = 120,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 127, 191)),
                BorderThickness = new Thickness(2)
            };
            
            // Hover effect pour OK button - glow bleu cyan #00D4FF
            okButton.MouseEnter += (s, args) =>
            {
                okButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 144, 255));
                okButton.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 212, 255));
                okButton.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = System.Windows.Media.Color.FromRgb(0, 212, 255), // #00D4FF
                    BlurRadius = 20,
                    ShadowDepth = 0,
                    Opacity = 0.7
                };
            };
            okButton.MouseLeave += (s, args) =>
            {
                okButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129));
                okButton.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 127, 191));
                okButton.Effect = null;
            };
            
            okButton.Click += (s, args) =>
            {
                selectedItem = listBox.SelectedItem?.ToString();
                selectionWindow.DialogResult = true;
                selectionWindow.Close();
            };

            // Style pour bouton rouge avec hover glow bleu cyan (comme les autres boutons XNRGY)
            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Annuler",
                Width = 100,
                Height = 35,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38)),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 127, 191)),
                BorderThickness = new Thickness(2)
            };
            
            // Hover effect pour Cancel button - glow bleu cyan #00D4FF
            cancelButton.MouseEnter += (s, args) =>
            {
                cancelButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 144, 255));
                cancelButton.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 212, 255));
                cancelButton.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = System.Windows.Media.Color.FromRgb(0, 212, 255), // #00D4FF
                    BlurRadius = 20,
                    ShadowDepth = 0,
                    Opacity = 0.7
                };
            };
            cancelButton.MouseLeave += (s, args) =>
            {
                cancelButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));
                cancelButton.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 127, 191));
                cancelButton.Effect = null;
            };
            
            cancelButton.Click += (s, args) =>
            {
                selectionWindow.DialogResult = false;
                selectionWindow.Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            System.Windows.Controls.Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            selectionWindow.Content = grid;

            // Double-clic pour selectionner
            listBox.MouseDoubleClick += (s, args) =>
            {
                if (listBox.SelectedItem != null)
                {
                    selectedItem = listBox.SelectedItem.ToString();
                    selectionWindow.DialogResult = true;
                    selectionWindow.Close();
                }
            };

            if (selectionWindow.ShowDialog() == true)
            {
                return selectedItem;
            }

            return null;
        }
    }
}
