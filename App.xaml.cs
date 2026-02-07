using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using XnrgyEngineeringAutomationTools.Services;
using XnrgyEngineeringAutomationTools.Views;
using XnrgyEngineeringAutomationTools.Shared.Views;

namespace XnrgyEngineeringAutomationTools
{
    public partial class App : Application
    {
        private DeviceTrackingService _deviceTracker;
        private AutoUpdateService _autoUpdateService;
        private SplashScreenWindow _splashScreen;

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Configurer les handlers d'erreur AVANT tout
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            
            // NE PAS appeler base.OnStartup ici car on a supprime StartupUri
            AddInventorToPath();

            // ═══════════════════════════════════════════════════════════════════
            // CHARGER LES RESSOURCES DYNAMIQUES (Version, Date, Titres)
            // Doit etre fait AVANT l'affichage de toute fenetre
            // ═══════════════════════════════════════════════════════════════════
            DynamicResourceService.LoadDynamicResources();

            // ═══════════════════════════════════════════════════════════════════
            // SPLASH SCREEN - Afficher IMMEDIATEMENT pendant le chargement
            // ═══════════════════════════════════════════════════════════════════
            _splashScreen = new SplashScreenWindow();
            _splashScreen.Show();
            _splashScreen.UpdateStatus("Verification de la configuration...", 5);
            
            // Permettre au splash de s'afficher
            await Task.Delay(100);

            // ═══════════════════════════════════════════════════════════════════
            // VERIFICATION CONNECTIVITE OBLIGATOIRE - BLOQUE SI PAS DE CONNEXION
            // L'application EXIGE une connexion internet et l'acces a Firebase
            // ═══════════════════════════════════════════════════════════════════
            _splashScreen.UpdateStatus("Verification de la connexion reseau...", 10);
            
            bool hasConnectivity = await CheckNetworkConnectivityAsync();
            if (!hasConnectivity)
            {
                _splashScreen.Close();
                MessageBox.Show(
                    "CONNEXION INTERNET REQUISE\n\n" +
                    "L'application XNRGY Engineering Automation Tools necessite une connexion internet active pour fonctionner.\n\n" +
                    "Veuillez verifier:\n" +
                    "  - Votre connexion reseau\n" +
                    "  - Les parametres de votre pare-feu\n" +
                    "  - L'acces aux services cloud Google/Firebase\n\n" +
                    "Si le probleme persiste, contactez votre administrateur reseau ou:\n" +
                    "mohammedamine.elgalai@xnrgy.com",
                    "Connexion Requise - XEAT",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Environment.Exit(1);
                return;
            }

            // ═══════════════════════════════════════════════════════════════════
            // VERIFICATION FIREBASE (avec progression)
            // ═══════════════════════════════════════════════════════════════════
            _splashScreen.UpdateStatus("Connexion aux services cloud...", 15);
            
            bool canContinue = await CheckFirebaseConfigurationWithProgressAsync();
            if (!canContinue)
            {
                _splashScreen.Close();
                Environment.Exit(0);
                return;
            }

            // ═══════════════════════════════════════════════════════════════════
            // ENREGISTREMENT DEVICE (en arriere-plan apres MainWindow)
            // ═══════════════════════════════════════════════════════════════════
            _splashScreen.UpdateStatus("Preparation de l'interface...", 85);

            // Demarrer le service de mise a jour automatique
            _autoUpdateService = new AutoUpdateService();
            _autoUpdateService.UpdateAvailable += OnUpdateAvailable;
            _autoUpdateService.Start();

            // ═══════════════════════════════════════════════════════════════════
            // LANCER MAINWINDOW - L'utilisateur voit l'app rapidement
            // ═══════════════════════════════════════════════════════════════════
            _splashScreen.UpdateStatus("Demarrage...", 95);
            _splashScreen.Complete();
            await Task.Delay(200); // Petit delai pour voir le "100%"

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
            
            // Fermer le splash avec animation
            await _splashScreen.CloseWithAnimationAsync();
            _splashScreen = null;
            
            // Changer le mode de fermeture maintenant que la fenetre est ouverte
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            // ═══════════════════════════════════════════════════════════════════
            // TACHES EN ARRIERE-PLAN (ne bloquent plus le demarrage)
            // ═══════════════════════════════════════════════════════════════════
            _ = Task.Run(async () =>
            {
                try
                {
                    // Enregistrer l'appareil (non bloquant)
                    _deviceTracker = new DeviceTrackingService();
                    await _deviceTracker.RegisterDeviceAsync();

                    // Initialiser le service Firebase Audit (session + heartbeat)
                    await FirebaseAuditService.Instance.InitializeAsync();
                }
                catch (Exception ex)
                {
                    // Log silencieux - ne pas bloquer l'app
                    System.Diagnostics.Debug.WriteLine($"[!] Background init: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Verification Firebase avec mise a jour de la progression du splash screen
        /// OPTIMISE: Les verifications critiques sont prioritaires, le reste est differe
        /// </summary>
        private async Task<bool> CheckFirebaseConfigurationWithProgressAsync()
        {
            try
            {
                _splashScreen?.UpdateStatus("Verification des autorisations...", 25);
                
                var result = await FirebaseRemoteConfigService.CheckConfigurationAsync();

                _splashScreen?.UpdateStatus("Analyse de la configuration...", 50);

                if (!result.Success)
                {
                    // ═══════════════════════════════════════════════════════════════════
                    // CONNEXION FIREBASE OBLIGATOIRE - NE PAS CONTINUER EN MODE HORS LIGNE
                    // ═══════════════════════════════════════════════════════════════════
                    _splashScreen?.Close();
                    MessageBox.Show(
                        "SERVICES CLOUD INACCESSIBLES\n\n" +
                        "L'application ne peut pas se connecter aux services Firebase.\n\n" +
                        "Causes possibles:\n" +
                        "  - Pare-feu bloquant les connexions sortantes\n" +
                        "  - Proxy d'entreprise non configure\n" +
                        "  - Services Google temporairement indisponibles\n\n" +
                        "Verifiez que les domaines suivants sont accessibles:\n" +
                        "  - *.googleapis.com\n" +
                        "  - *.firebaseio.com\n" +
                        "  - *.firebase.google.com\n\n" +
                        "Contactez votre administrateur reseau si necessaire.",
                        "Services Cloud Requis - XEAT",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }

                // 1. Kill Switch - BLOQUE
                if (result.KillSwitchActive)
                {
                    _splashScreen?.Close();
                    FirebaseAlertWindow.ShowKillSwitch(result.KillSwitchMessage);
                    return false;
                }

                _splashScreen?.UpdateStatus("Verification du poste de travail...", 55);

                // 2a. Device suspendu - BLOQUE
                if (result.DeviceDisabled)
                {
                    _splashScreen?.Close();
                    FirebaseAlertWindow.ShowDeviceDisabled(result.DeviceDisabledMessage, result.DeviceDisabledReason);
                    return false;
                }

                // 2b. Utilisateur suspendu sur ce device - BLOQUE
                if (result.DeviceUserDisabled)
                {
                    _splashScreen?.Close();
                    FirebaseAlertWindow.ShowDeviceUserDisabled(result.DeviceUserDisabledMessage, result.DeviceUserDisabledReason);
                    return false;
                }

                _splashScreen?.UpdateStatus("Verification de l'utilisateur...", 60);

                // 3. Utilisateur desactive globalement - BLOQUE
                if (result.UserDisabled)
                {
                    _splashScreen?.Close();
                    FirebaseAlertWindow.ShowUserDisabled(result.UserDisabledMessage);
                    return false;
                }

                // 4. Mode Maintenance - BLOQUE
                if (result.MaintenanceMode)
                {
                    _splashScreen?.Close();
                    FirebaseAlertWindow.ShowMaintenance(result.MaintenanceMessage);
                    return false;
                }

                _splashScreen?.UpdateStatus("Verification des mises a jour...", 70);

                // 5. Mise a jour disponible
                if (result.UpdateAvailable)
                {
                    string currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
                    currentVersion = $"{currentVersion.Split('.')[0]}.{currentVersion.Split('.')[1]}.{currentVersion.Split('.')[2]}";

                    _splashScreen?.Close();
                    
                    var (shouldContinue, shouldDownload) = FirebaseAlertWindow.ShowUpdateAvailable(
                        currentVersion,
                        result.LatestVersion,
                        result.Changelog,
                        result.DownloadUrl,
                        result.ForceUpdate);

                    if (result.ForceUpdate || !shouldContinue)
                    {
                        return false;
                    }
                    
                    // Recreer le splash si on continue
                    _splashScreen = new SplashScreenWindow();
                    _splashScreen.Show();
                }

                _splashScreen?.UpdateStatus("Chargement des messages...", 75);

                // 6. Message broadcast (ne bloque que si type "error")
                if (result.HasBroadcastMessage)
                {
                    _splashScreen?.Close();
                    bool shouldBlock = FirebaseAlertWindow.ShowBroadcastMessage(
                        result.BroadcastTitle,
                        result.BroadcastMessage,
                        result.BroadcastType);
                    
                    if (shouldBlock) return false;
                    
                    // Recreer le splash si on continue
                    _splashScreen = new SplashScreenWindow();
                    _splashScreen.Show();
                }

                // 7. Message de bienvenue (DIFFERE - affiche apres MainWindow)
                // Stocke pour affichage apres le demarrage complet
                if (result.HasWelcomeMessage)
                {
                    _splashScreen?.UpdateStatus("Bienvenue...", 80);
                    // Le message de bienvenue sera affiche APRES le splash
                    // pour ne pas ralentir le demarrage
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1500); // Attendre que MainWindow soit visible
                        await Dispatcher.InvokeAsync(() =>
                        {
                            FirebaseAlertWindow.ShowWelcomeMessage(
                                result.WelcomeTitle,
                                result.WelcomeMessage,
                                result.WelcomeType);
                        });
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[-] Erreur verification Firebase: {ex.Message}", Logger.LogLevel.ERROR);
                _splashScreen?.Close();
                MessageBox.Show(
                    "ERREUR DE CONNEXION\n\n" +
                    $"Une erreur s'est produite lors de la verification des services cloud:\n{ex.Message}\n\n" +
                    "L'application necessite une connexion internet fonctionnelle.\n\n" +
                    "Verifiez votre connexion reseau et reessayez.",
                    "Erreur de Connexion - XEAT",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Verifie la connectivite reseau et l'acces aux services Firebase
        /// BLOQUANT: L'application ne demarrera pas sans connexion
        /// Teste:
        /// 1. Connectivite reseau generale (google.com)
        /// 2. Acces a Firebase Realtime Database
        /// 3. Detection des pare-feux bloquants
        /// </summary>
        private async Task<bool> CheckNetworkConnectivityAsync()
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);

                    // Test 1: Connectivite internet generale
                    try
                    {
                        var googleResponse = await httpClient.GetAsync("https://www.google.com/generate_204");
                        if (!googleResponse.IsSuccessStatusCode)
                        {
                            Logger.Log("[-] Test connectivite Google echoue", Logger.LogLevel.ERROR);
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[-] Pas de connexion internet: {ex.Message}", Logger.LogLevel.ERROR);
                        return false;
                    }

                    // Test 2: Acces Firebase (endpoint de sante)
                    try
                    {
                        var firebaseResponse = await httpClient.GetAsync(
                            "https://xeat-remote-control-default-rtdb.firebaseio.com/.json?shallow=true");
                        if (!firebaseResponse.IsSuccessStatusCode)
                        {
                            Logger.Log($"[-] Firebase inaccessible: {firebaseResponse.StatusCode}", Logger.LogLevel.ERROR);
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[-] Pare-feu bloque Firebase: {ex.Message}", Logger.LogLevel.ERROR);
                        return false;
                    }

                    Logger.Log("[+] Connectivite reseau verifiee avec succes", Logger.LogLevel.INFO);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[-] Erreur verification connectivite: {ex.Message}", Logger.LogLevel.ERROR);
                return false;
            }
        }

        /// <summary>
        /// Gere la notification de mise a jour disponible (verification periodique)
        /// </summary>
        private void OnUpdateAvailable(object sender, UpdateAvailableEventArgs e)
        {
            // Afficher la notification de mise a jour
            var (shouldContinue, shouldDownload) = FirebaseAlertWindow.ShowUpdateAvailable(
                e.CurrentVersion,
                e.NewVersion,
                e.Changelog,
                e.DownloadUrl,
                e.IsForced);

            // Si mise a jour forcee et l'utilisateur refuse, forcer la fermeture
            if (e.IsForced && !shouldDownload)
            {
                Environment.Exit(0);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Enregistrer la fin de session Firebase - SYNCHRONE pour garantir l'execution
            try
            {
                // Utiliser Wait() avec timeout pour garantir que la requete part avant fermeture
                var task = FirebaseAuditService.Instance.RegisterSessionEndAsync();
                task.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Silencieux - ne pas bloquer la fermeture
            }

            // Arreter les services
            _autoUpdateService?.Stop();
            _autoUpdateService?.Dispose();
            _deviceTracker?.Dispose();
            base.OnExit(e);
        }

        private void AddInventorToPath()
        {
            try
            {
                string inventorPath = @"C:\Program Files\Autodesk\Inventor 2026\Bin";
                if (Directory.Exists(inventorPath))
                {
                    string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                    if (!currentPath.Contains(inventorPath))
                    {
                        Environment.SetEnvironmentVariable("PATH", inventorPath + ";" + currentPath);
                    }
                }
            }
            catch { }
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyName = new AssemblyName(args.Name).Name ?? "";
            string[] searchPaths = new[]
            {
                @"C:\Program Files\Autodesk\Vault Client 2026\Explorer",
                @"C:\Program Files\Autodesk\Autodesk Vault 2026 SDK\bin\x64",
                @"C:\Program Files\Autodesk\Inventor 2026\Bin"
            };
            foreach (var path in searchPaths)
            {
                string dllPath = Path.Combine(path, assemblyName + ".dll");
                if (File.Exists(dllPath))
                {
                    try { return Assembly.LoadFrom(dllPath); }
                    catch { }
                }
            }
            return null;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            Logger.Log($"[-] Exception non geree (AppDomain): {ex.Message}", Logger.LogLevel.ERROR);
            Logger.Log($"    StackTrace: {ex.StackTrace}", Logger.LogLevel.ERROR);
            
            // Utiliser XnrgyMessageBox au lieu de MessageBox classique
            Application.Current?.Dispatcher.Invoke(() =>
            {
                XnrgyMessageBox.ShowError("Erreur critique: " + ex.Message, "Erreur", null);
            });
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Log($"[-] Exception non geree (Dispatcher): {e.Exception.Message}", Logger.LogLevel.ERROR);
            Logger.Log($"    Source: {e.Exception.Source}", Logger.LogLevel.ERROR);
            Logger.Log($"    StackTrace: {e.Exception.StackTrace}", Logger.LogLevel.ERROR);
            if (e.Exception.InnerException != null)
            {
                Logger.Log($"    InnerException: {e.Exception.InnerException.Message}", Logger.LogLevel.ERROR);
            }
            
            // Utiliser XnrgyMessageBox au lieu de MessageBox classique
            XnrgyMessageBox.ShowError("Erreur: " + e.Exception.Message, "Erreur", null);
            e.Handled = true;
        }
    }
}
