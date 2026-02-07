using System;
using System.Linq;
using System.Windows;
using XnrgyEngineeringAutomationTools.Models;
using XnrgyEngineeringAutomationTools.Services;

namespace XnrgyEngineeringAutomationTools.Views
{
    /// <summary>
    /// Fenetre d'alerte Firebase pour afficher les messages de controle a distance
    /// Design moderne XNRGY avec emojis professionnels
    /// </summary>
    public partial class FirebaseAlertWindow : Window
    {
        public enum AlertType
        {
            KillSwitch,
            Maintenance,
            MaintenanceScheduled,
            UpdateAvailable,
            ForceUpdate,
            UserDisabled,
            DeviceDisabled,
            BroadcastInfo,
            BroadcastWarning,
            BroadcastError,
            Welcome,
            WelcomeFirstInstall,
            WelcomeNewUser
        }

        public bool ShouldContinue { get; private set; }
        public bool ShouldDownload { get; private set; }

        private string _downloadUrl;
        
        // Nom d'affichage de l'utilisateur (personnalise si disponible)
        private static string _userDisplayName;
        
        /// <summary>
        /// Definit le nom d'affichage de l'utilisateur (depuis Azure/Firebase)
        /// </summary>
        public static void SetUserDisplayName(string displayName)
        {
            _userDisplayName = displayName;
        }
        
        /// <summary>
        /// Obtient le nom d'affichage personnalise ou le nom Windows par defaut
        /// </summary>
        private static string GetUserDisplayName()
        {
            if (!string.IsNullOrEmpty(_userDisplayName))
                return _userDisplayName;
            
            // Formatter le nom Windows (mohammedamine.elgala -> Mohammed Amine Elgala)
            string userName = System.Environment.UserName;
            if (userName.Contains("."))
            {
                var parts = userName.Split('.');
                userName = string.Join(" ", parts.Select(p => 
                    char.ToUpper(p[0]) + p.Substring(1).ToLower()));
            }
            return userName;
        }

        public FirebaseAlertWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Configure et affiche l'alerte Kill Switch
        /// </summary>
        public static bool ShowKillSwitch(string message)
        {
            var window = new FirebaseAlertWindow();
            window._downloadUrl = null;
            window.ConfigureKillSwitch(message);
            window.ShowDialog();
            return false; // Toujours bloquer l'application
        }

        /// <summary>
        /// Configure et affiche l'alerte Utilisateur desactive
        /// </summary>
        public static bool ShowUserDisabled(string message)
        {
            var window = new FirebaseAlertWindow();
            window.ConfigureUserDisabled(message);
            window.ShowDialog();
            return false; // Toujours bloquer l'application
        }

        /// <summary>
        /// Configure et affiche l'alerte Device (poste de travail) suspendu
        /// </summary>
        public static bool ShowDeviceDisabled(string message, string reason)
        {
            var window = new FirebaseAlertWindow();
            window.ConfigureDeviceDisabled(message, reason);
            window.ShowDialog();
            return false; // Toujours bloquer l'application
        }

        /// <summary>
        /// Configure et affiche l'alerte Utilisateur suspendu SUR UN DEVICE specifique
        /// </summary>
        public static bool ShowDeviceUserDisabled(string message, string reason)
        {
            var window = new FirebaseAlertWindow();
            window.ConfigureDeviceUserDisabled(message, reason);
            window.ShowDialog();
            return false; // Toujours bloquer l'application
        }

        /// <summary>
        /// Configure et affiche l'alerte Maintenance
        /// </summary>
        public static bool ShowMaintenance(string message)
        {
            var window = new FirebaseAlertWindow();
            window.ConfigureMaintenance(message);
            window.ShowDialog();
            return false; // Toujours bloquer l'application
        }

        /// <summary>
        /// Configure et affiche l'alerte de mise a jour disponible
        /// </summary>
        public static (bool shouldContinue, bool shouldDownload) ShowUpdateAvailable(
            string currentVersion, string newVersion, string changelog, string downloadUrl, bool forceUpdate)
        {
            var window = new FirebaseAlertWindow();
            window.ConfigureUpdate(currentVersion, newVersion, changelog, downloadUrl, forceUpdate);
            window.ShowDialog();
            return (window.ShouldContinue, window.ShouldDownload);
        }

        /// <summary>
        /// Affiche un message broadcast
        /// Retourne true si l'application doit etre bloquee (type "error")
        /// </summary>
        public static bool ShowBroadcastMessage(string title, string message, string type)
        {
            var window = new FirebaseAlertWindow();
            window.ConfigureBroadcast(title, message, type);
            window.ShowDialog();
            
            // Bloquer seulement pour les messages de type "error"
            return type?.ToLowerInvariant() == "error" && !window.ShouldContinue;
        }

        /// <summary>
        /// Affiche un message de bienvenue au demarrage
        /// Ne bloque jamais l'application
        /// </summary>
        public static void ShowWelcomeMessage(string title, string message, string type)
        {
            var window = new FirebaseAlertWindow();
            window.ConfigureWelcome(title, message, type);
            window.ShowDialog();
        }

        private void ConfigureWelcome(string title, string message, string type)
        {
            // Toujours utiliser un style accueillant (vert ou cyan)
            var greenBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 210, 106));
            var cyanBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 212, 255));
            
            var colorBrush = type?.ToLowerInvariant() == "success" ? greenBrush : cyanBrush;
            
            AlertIcon.Text = "✅";
            AlertIcon.Foreground = colorBrush;
            
            // Personnaliser le titre avec le nom de l'utilisateur
            string userName = GetUserDisplayName();
            string personalizedTitle = title ?? "Bienvenue!";
            if (personalizedTitle.Contains("{userName}"))
                personalizedTitle = personalizedTitle.Replace("{userName}", userName);
            else if (!personalizedTitle.Contains(userName) && !personalizedTitle.ToLower().Contains("bienvenue"))
                personalizedTitle = $"Bienvenue {userName}!";
            
            AlertTitle.Text = personalizedTitle;
            AlertTitle.Foreground = colorBrush;
            
            // Personnaliser le message avec le nom de l'utilisateur
            string personalizedMessage = message?.Replace("\\n", "\n") 
                ?? GetDefaultWelcomeMessage();
            personalizedMessage = personalizedMessage.Replace("{userName}", userName);
            personalizedMessage = personalizedMessage.Replace("{machineName}", System.Environment.MachineName);
            
            AlertMessage.Text = personalizedMessage;
            
            PrimaryButton.Content = "Commencer ▶";
            PrimaryButton.Background = colorBrush;
            SecondaryButton.Visibility = Visibility.Collapsed;
            VersionInfoPanel.Visibility = Visibility.Collapsed;
            
            ShouldContinue = true; // Ne bloque jamais
        }
        
        /// <summary>
        /// Message de bienvenue par defaut avec toutes les fonctionnalites
        /// </summary>
        private static string GetDefaultWelcomeMessage()
        {
            string userName = GetUserDisplayName();
            return $"Bonjour {userName}!\n\n" +
                "Bienvenue dans XNRGY Engineering Automation Tools (XEAT).\n\n" +
                "\n" +
                " FONCTIONNALITES DISPONIBLES\n" +
                "\n\n" +
                " UPLOAD MODULE\n" +
                "     Televersez vos fichiers Inventor vers Vault\n" +
                "     avec gestion automatique des proprietes\n\n" +
                " UPLOAD TEMPLATE\n" +
                "     Televersez vos templates vers Vault\n\n" +
                " CREER MODULE\n" +
                "     Creez un nouveau module a partir d'un template\n" +
                "     avec renommage automatique des fichiers\n\n" +
                " OUVRIR PROJET\n" +
                "     Ouvrez un projet depuis Vault ou local\n\n" +
                " PLACE EQUIPMENT\n" +
                "     Placez des equipements dans vos assemblages\n\n" +
                " SMART TOOLS\n" +
                "     Outils avances pour Inventor (Export, iProperties)\n\n" +
                " DXF VERIFIER\n" +
                "     Verifiez vos fichiers DXF/CSV vs PDF\n\n" +
                " CONFIG VAULT\n" +
                "     Configurez votre connexion Vault\n\n" +
                "\n" +
                " BIENTOT DISPONIBLE\n" +
                "\n\n" +
                " Integration SharePoint\n" +
                " Gestion des BOM\n" +
                " Checklist HVAC avancee\n\n" +
                "\n" +
                "ℹ Support: mohammedamine.elgalai@xnrgy.com\n" +
                "";
        }

        private void ConfigureKillSwitch(string message)
        {
            var redBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 100, 100));
            
            AlertIcon.Text = "⛔";
            AlertIcon.Foreground = redBrush;
            AlertTitle.Text = "Application Desactivee";
            AlertTitle.Foreground = redBrush;
            
            string userName = GetUserDisplayName();
            string defaultMessage = $"Bonjour {userName},\n\n" +
                "⛔ Cette application a ete temporairement desactivee\n" +
                "     par l'administrateur systeme.\n\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                "📋 QUE FAIRE?\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                "📧 Contactez le support technique:\n" +
                "     mohammedamine.elgalai@xnrgy.com\n\n" +
                "📞 Ou contactez votre superviseur\n" +
                "     pour plus d'informations.\n\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
            
            string displayMessage = message ?? defaultMessage;
            displayMessage = displayMessage.Replace("{userName}", userName);
            displayMessage = displayMessage.Replace("\\n", "\n");
            
            AlertMessage.Text = displayMessage;
            
            PrimaryButton.Content = "Fermer";
            PrimaryButton.Background = redBrush;
            SecondaryButton.Visibility = Visibility.Collapsed;
            VersionInfoPanel.Visibility = Visibility.Collapsed;
        }

        private void ConfigureUserDisabled(string message)
        {
            var redBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 100, 100));
            
            AlertIcon.Text = "🚫";
            AlertIcon.Foreground = redBrush;
            AlertTitle.Text = "Acces refuse";
            AlertTitle.Foreground = redBrush;
            
            string userName = GetUserDisplayName();
            string defaultMessage = $"Bonjour {userName},\n\n" +
                "🚫 Votre compte utilisateur a ete desactive\n" +
                "     par l'administrateur systeme.\n\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                "📋 RAISONS POSSIBLES\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                "🔒 Compte temporairement suspendu\n" +
                "📝 Mise a jour des autorisations en cours\n" +
                "🔄 Changement de role ou departement\n\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                "📧 Contactez votre superviseur ou:\n" +
                "     mohammedamine.elgalai@xnrgy.com";
            
            string displayMessage = message ?? defaultMessage;
            displayMessage = displayMessage.Replace("{userName}", userName);
            displayMessage = displayMessage.Replace("\\n", "\n");
            
            AlertMessage.Text = displayMessage;
            
            PrimaryButton.Content = "Fermer";
            PrimaryButton.Background = redBrush;
            SecondaryButton.Visibility = Visibility.Collapsed;
            VersionInfoPanel.Visibility = Visibility.Collapsed;
        }

        private void ConfigureDeviceDisabled(string message, string reason)
        {
            string userName = GetUserDisplayName();
            string machineName = System.Environment.MachineName;
            
            // Couleur selon la raison
            System.Windows.Media.SolidColorBrush colorBrush;
            string icon;
            string title;
            string defaultMessage;

            switch (reason?.ToLowerInvariant())
            {
                case "maintenance":
                    colorBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(255, 193, 7)); // Jaune
                    icon = "🔧";
                    title = "Poste en Maintenance";
                    defaultMessage = $"Bonjour {userName},\n\n" +
                        $"🔧 Le poste '{machineName}' est actuellement\n" +
                        "     en maintenance technique.\n\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                        "📋 INFORMATIONS\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                        "🔄 Mise a jour systeme en cours\n" +
                        "⏳ Duree estimee: Quelques minutes\n\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                        "💡 Essayez de vous connecter depuis\n" +
                        "     un autre poste de travail.";
                    break;
                case "unauthorized":
                    colorBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(255, 100, 100)); // Rouge
                    icon = "⛔";
                    title = "Poste Non Autorise";
                    defaultMessage = $"Bonjour {userName},\n\n" +
                        $"⛔ Le poste '{machineName}' n'est pas autorise\n" +
                        "     a utiliser cette application.\n\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                        "📋 QUE FAIRE?\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                        "📧 Contactez l'administrateur pour\n" +
                        "     demander l'autorisation de ce poste.\n\n" +
                        "💻 Ou utilisez un poste autorise.";
                    break;
                case "suspended":
                default:
                    colorBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(255, 152, 0)); // Orange
                    icon = "🖥️";
                    title = "Poste Suspendu";
                    defaultMessage = $"Bonjour {userName},\n\n" +
                        $"🖥️ Le poste '{machineName}' a ete suspendu\n" +
                        "     temporairement.\n\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                        "📋 RAISONS POSSIBLES\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                        "🔒 Verification de securite en cours\n" +
                        "📝 Mise a jour des licences\n" +
                        "🔄 Reorganisation des postes\n\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                        "📧 Contact: mohammedamine.elgalai@xnrgy.com";
                    break;
            }
            
            AlertIcon.Text = icon;
            AlertIcon.Foreground = colorBrush;
            AlertTitle.Text = title;
            AlertTitle.Foreground = colorBrush;
            
            string displayMessage = message ?? defaultMessage;
            displayMessage = displayMessage.Replace("{userName}", userName);
            displayMessage = displayMessage.Replace("{machineName}", machineName);
            displayMessage = displayMessage.Replace("\\n", "\n");
            
            AlertMessage.Text = displayMessage;
            
            PrimaryButton.Content = "Fermer";
            PrimaryButton.Background = colorBrush;
            SecondaryButton.Visibility = Visibility.Collapsed;
            VersionInfoPanel.Visibility = Visibility.Collapsed;
        }

        private void ConfigureDeviceUserDisabled(string message, string reason)
        {
            string userName = GetUserDisplayName();
            string machineName = System.Environment.MachineName;
            
            // Couleur selon la raison - toujours nuance de rouge/orange pour utilisateur
            System.Windows.Media.SolidColorBrush colorBrush;
            string icon;
            string title;
            string defaultMessage;

            switch (reason?.ToLowerInvariant())
            {
                case "unauthorized":
                    colorBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(255, 100, 100)); // Rouge
                    icon = "🚷";
                    title = "Acces Non Autorise";
                    defaultMessage = $"Bonjour {userName},\n\n" +
                        $"🚷 Vous n'etes pas autorise(e) a utiliser\n" +
                        $"     XEAT sur le poste '{machineName}'.\n\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                        "📋 QUE FAIRE?\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                        "📧 Demandez l'autorisation a:\n" +
                        "     mohammedamine.elgalai@xnrgy.com\n\n" +
                        "💻 Ou utilisez un poste sur lequel\n" +
                        "     vous etes autorise(e).";
                    break;
                case "revoked":
                    colorBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(220, 53, 69)); // Rouge fonce
                    icon = "🔐";
                    title = "Acces Revoque";
                    defaultMessage = $"Bonjour {userName},\n\n" +
                        $"🔐 Votre acces au poste '{machineName}'\n" +
                        "     a ete revoque.\n\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                        "📋 RAISONS POSSIBLES\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                        "🔄 Changement d'affectation\n" +
                        "📝 Mise a jour des autorisations\n" +
                        "🔒 Verification de securite\n\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                        "📧 Contactez votre superviseur.";
                    break;
                case "suspended":
                default:
                    colorBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(255, 152, 0)); // Orange
                    icon = "👤";
                    title = "Utilisateur Suspendu";
                    defaultMessage = $"Bonjour {userName},\n\n" +
                        $"👤 Votre compte a ete suspendu\n" +
                        $"     sur le poste '{machineName}'.\n\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                        "📋 INFORMATIONS\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                        "⏳ Cette suspension est temporaire.\n" +
                        "📧 Contactez l'administrateur pour\n" +
                        "     connaitre la raison et la duree.\n\n" +
                        "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
                    break;
            }
            
            AlertIcon.Text = icon;
            AlertIcon.Foreground = colorBrush;
            AlertTitle.Text = title;
            AlertTitle.Foreground = colorBrush;
            
            string displayMessage = message ?? defaultMessage;
            displayMessage = displayMessage.Replace("{userName}", userName);
            displayMessage = displayMessage.Replace("{machineName}", machineName);
            displayMessage = displayMessage.Replace("\\n", "\n");
            
            AlertMessage.Text = displayMessage;
            
            PrimaryButton.Content = "Fermer";
            PrimaryButton.Background = colorBrush;
            SecondaryButton.Visibility = Visibility.Collapsed;
            VersionInfoPanel.Visibility = Visibility.Collapsed;
        }

        private void ConfigureMaintenance(string message)
        {
            var yellowBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 193, 7));
            
            AlertIcon.Text = "🔧";
            AlertIcon.Foreground = yellowBrush;
            AlertTitle.Text = "Maintenance en cours";
            AlertTitle.Foreground = yellowBrush;
            
            string userName = GetUserDisplayName();
            string defaultMessage = $"Bonjour {userName},\n\n" +
                "🔧 L'application est actuellement en maintenance.\n\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                "📋 INFORMATIONS\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                "⏳ Duree estimee: Quelques minutes\n" +
                "🔄 Action: Nous effectuons des mises a jour\n" +
                "     pour ameliorer votre experience.\n\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                "Veuillez reessayer dans quelques instants.\n\n" +
                "📧 Contact: mohammedamine.elgalai@xnrgy.com";
            
            string displayMessage = message ?? defaultMessage;
            displayMessage = displayMessage.Replace("{userName}", userName);
            displayMessage = displayMessage.Replace("\\n", "\n");
            
            AlertMessage.Text = displayMessage;
            
            PrimaryButton.Content = "Fermer";
            PrimaryButton.Background = yellowBrush;
            SecondaryButton.Visibility = Visibility.Collapsed;
            VersionInfoPanel.Visibility = Visibility.Collapsed;
        }
        
        /// <summary>
        /// Configure et affiche l'alerte Maintenance planifiee
        /// Ne bloque pas mais previent l'utilisateur
        /// </summary>
        public static bool ShowMaintenanceScheduled(string message, string scheduledTime, string estimatedDuration)
        {
            var window = new FirebaseAlertWindow();
            window.ConfigureMaintenanceScheduled(message, scheduledTime, estimatedDuration);
            window.ShowDialog();
            return true; // Ne bloque pas, juste informatif
        }
        
        private void ConfigureMaintenanceScheduled(string message, string scheduledTime, string estimatedDuration)
        {
            var orangeBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 152, 0));
            
            AlertIcon.Text = "📅";
            AlertIcon.Foreground = orangeBrush;
            AlertTitle.Text = "Maintenance planifiee";
            AlertTitle.Foreground = orangeBrush;
            
            string userName = GetUserDisplayName();
            string defaultMessage = $"Bonjour {userName},\n\n" +
                "📅 Une maintenance est planifiee prochainement.\n\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                "📋 DETAILS DE LA MAINTENANCE\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                $"📆 Date et heure: {scheduledTime ?? "A determiner"}\n" +
                $"⏱️ Duree estimee: {estimatedDuration ?? "Quelques minutes"}\n\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                "💾 Pensez a sauvegarder votre travail\n" +
                "     avant le debut de la maintenance.\n\n" +
                "📧 Contact: mohammedamine.elgalai@xnrgy.com";
            
            string displayMessage = message ?? defaultMessage;
            displayMessage = displayMessage.Replace("{userName}", userName);
            displayMessage = displayMessage.Replace("{scheduledTime}", scheduledTime ?? "A determiner");
            displayMessage = displayMessage.Replace("{estimatedDuration}", estimatedDuration ?? "Quelques minutes");
            displayMessage = displayMessage.Replace("\\n", "\n");
            
            AlertMessage.Text = displayMessage;
            
            PrimaryButton.Content = "Compris ✓";
            PrimaryButton.Background = orangeBrush;
            SecondaryButton.Visibility = Visibility.Collapsed;
            VersionInfoPanel.Visibility = Visibility.Collapsed;
            
            ShouldContinue = true; // Ne bloque pas
        }

        private void ConfigureBroadcast(string title, string message, string type)
        {
            type = type?.ToLowerInvariant() ?? "info";
            string userName = GetUserDisplayName();
            
            System.Windows.Media.SolidColorBrush colorBrush;
            
            switch (type)
            {
                case "error":
                    colorBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(255, 100, 100));
                    AlertIcon.Text = "❌";
                    AlertIcon.Foreground = colorBrush;
                    AlertTitle.Foreground = colorBrush;
                    PrimaryButton.Content = "Fermer";
                    PrimaryButton.Background = colorBrush;
                    SecondaryButton.Visibility = Visibility.Collapsed;
                    break;
                    
                case "warning":
                    colorBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(255, 193, 7));
                    AlertIcon.Text = "⚠️";
                    AlertIcon.Foreground = colorBrush;
                    AlertTitle.Foreground = colorBrush;
                    PrimaryButton.Content = "Compris ✓";
                    PrimaryButton.Background = colorBrush;
                    SecondaryButton.Visibility = Visibility.Collapsed;
                    ShouldContinue = true; // Warning ne bloque pas
                    break;
                    
                case "success":
                    colorBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0, 210, 106));
                    AlertIcon.Text = "✅";
                    AlertIcon.Foreground = colorBrush;
                    AlertTitle.Foreground = colorBrush;
                    PrimaryButton.Content = "Super! ▶";
                    PrimaryButton.Background = colorBrush;
                    SecondaryButton.Visibility = Visibility.Collapsed;
                    ShouldContinue = true;
                    break;
                    
                default: // info
                    colorBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0, 212, 255));
                    AlertIcon.Text = "📢";
                    AlertIcon.Foreground = colorBrush;
                    AlertTitle.Foreground = colorBrush;
                    PrimaryButton.Content = "OK ✓";
                    PrimaryButton.Background = colorBrush;
                    SecondaryButton.Visibility = Visibility.Collapsed;
                    ShouldContinue = true; // Info ne bloque pas
                    break;
            }
            
            AlertTitle.Text = title ?? "📢 Message";
            
            // Personnaliser le message
            string displayMessage = message ?? "";
            displayMessage = displayMessage.Replace("{userName}", userName);
            displayMessage = displayMessage.Replace("{machineName}", System.Environment.MachineName);
            displayMessage = displayMessage.Replace("\\n", "\n");
            
            AlertMessage.Text = displayMessage;
            VersionInfoPanel.Visibility = Visibility.Collapsed;
        }

        private void ConfigureUpdate(string currentVersion, string newVersion, 
            string changelog, string downloadUrl, bool forceUpdate)
        {
            _downloadUrl = downloadUrl;
            _currentVersion = currentVersion;
            _newVersion = newVersion;
            _isForceUpdate = forceUpdate;
            
            string userName = GetUserDisplayName();

            // Style uniforme pour toutes les mises a jour (forcee ou suggeree)
            // Couleur cyan pour les deux cas - aspect professionnel
            var cyanBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 212, 255));
                
            AlertIcon.Text = "🔄";
            AlertIcon.Foreground = cyanBrush;
            AlertTitle.Text = "Mise a jour disponible";
            AlertTitle.Foreground = cyanBrush;
            
            AlertMessage.Text = $"Bonjour {userName},\n\n" +
                "🔄 Une nouvelle version de XEAT est disponible!\n\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                "📋 NOUVEAUTES\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
            
            // Toujours cacher "Plus tard" - mise a jour obligatoire pour tous les dessinateurs
            // Le systeme reste en place pour les futures mises a jour suggerees
            PrimaryButton.Content = "🔄 Mettre à jour";
            PrimaryButton.Background = cyanBrush;
            SecondaryButton.Visibility = Visibility.Collapsed;

            // Afficher les informations de version
            VersionInfoPanel.Visibility = Visibility.Visible;
            string versionText = $"◆ Version actuelle: {currentVersion}\n" +
                                 $"📄 Nouvelle version: {newVersion}";
            
            if (!string.IsNullOrEmpty(changelog))
            {
                versionText += $"\n\n📋 Changelog:\n{changelog}";
            }
            
            VersionDetails.Text = versionText;
        }

        private string _currentVersion;
        private string _newVersion;
        private bool _isForceUpdate;

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            // Si c'est un bouton de telechargement (contient "Telecharger")
            string buttonContent = PrimaryButton.Content?.ToString() ?? "";
            bool isDownloadButton = buttonContent.Contains("Mettre") || buttonContent.Contains("jour") || 
                                    buttonContent.Contains("Telecharger") || buttonContent.Contains("telecharger");
            
            Logger.Info($"[>] PrimaryButton clicked - Content: '{buttonContent}', IsUpdate: {isDownloadButton}, DownloadUrl: '{_downloadUrl}'");
            
            if (isDownloadButton && !string.IsNullOrEmpty(_downloadUrl))
            {
                Logger.Info($"[+] Starting update from: {_downloadUrl}");
                ShouldDownload = true;
                
                // Fermer cette fenetre d'abord
                Close();
                
                // Lancer le telechargement automatique
                try
                {
                    UpdateDownloadWindow.ShowAndDownload(_downloadUrl, _currentVersion, _newVersion, _isForceUpdate);
                }
                catch (Exception ex)
                {
                    Logger.Error($"[-] Update error: {ex.Message}");
                    System.Windows.MessageBox.Show(
                        $"Erreur lors de la mise a jour:\n{ex.Message}\n\nURL: {_downloadUrl}",
                        "Erreur de mise a jour",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
                return;
            }

            // Pour Kill Switch et Maintenance, ne pas continuer
            // Pour ForceUpdate, ne pas continuer
            // Pour Update optionnel avec telechargement, ne pas continuer
            ShouldContinue = false;
            
            Close();
        }

        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            // Bouton "Plus tard" - continuer sans telecharger
            ShouldContinue = true;
            ShouldDownload = false;
            Close();
        }
    }
}
