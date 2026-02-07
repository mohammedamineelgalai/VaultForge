// ============================================================================
// AppInfo.cs
// XNRGY Engineering Automation Tools - Source UNIQUE de verite pour les infos application
// Auteur: Mohammed Amine Elgalai - XNRGY Climate Systems ULC
// ============================================================================
// 
// [!!!] FICHIER CRITIQUE - NE PAS DUPLIQUER CES VALEURS AILLEURS
// 
// Ce fichier contient TOUTES les informations centralisees de l'application:
// - Version
// - Date de release (AUTOMATIQUE a la compilation)
// - Auteur
// - Entreprise
// - Noms de l'application
//
// La date de release est generee automatiquement au moment de la compilation
// via la reflexion sur la date du build de l'assembly.
//
// ============================================================================

using System;
using System.Reflection;

namespace XnrgyEngineeringAutomationTools
{
    /// <summary>
    /// Source UNIQUE de verite pour toutes les informations de l'application.
    /// La date de release est automatiquement generee a partir de la date de compilation.
    /// </summary>
    public static class AppInfo
    {
        #region Version et Date - AUTOMATIQUES

        /// <summary>
        /// Version de l'application (format: v1.0.0)
        /// </summary>
        public static string Version { get; } = "v1.0.0";

        /// <summary>
        /// Version numerique sans prefix (format: 1.0.0)
        /// </summary>
        public static string VersionNumeric { get; } = "1.0.0";

        /// <summary>
        /// Date de compilation/release - AUTOMATIQUE
        /// Recuperee a partir de la date de build de l'assembly
        /// </summary>
        public static DateTime BuildDate { get; } = GetBuildDate();

        /// <summary>
        /// Date de release formatee (format: R YYYY-MM-DD)
        /// </summary>
        public static string ReleaseDate => $"R {BuildDate:yyyy-MM-dd}";

        /// <summary>
        /// Date de release courte (format: YYYY-MM-DD)
        /// </summary>
        public static string ReleaseDateShort => BuildDate.ToString("yyyy-MM-dd");

        #endregion

        #region Informations Auteur et Entreprise - CENTRALISEES

        /// <summary>
        /// Nom de l'auteur
        /// </summary>
        public const string Author = "Mohammed Amine Elgalai";

        /// <summary>
        /// Nom de l'entreprise (majuscules)
        /// </summary>
        public const string Company = "XNRGY CLIMATE SYSTEMS ULC";

        /// <summary>
        /// Nom de l'entreprise (format normal)
        /// </summary>
        public const string CompanyNormal = "XNRGY Climate Systems ULC";

        /// <summary>
        /// Email de support
        /// </summary>
        public const string SupportEmail = "mohammedamine.elgalai@xnrgy.com";

        #endregion

        #region Noms de l'Application - CENTRALISEES

        /// <summary>
        /// Nom complet de l'application
        /// </summary>
        public const string FullName = "XNRGY Engineering Automation Tools";

        /// <summary>
        /// Nom court de l'application
        /// </summary>
        public const string ShortName = "XEAT";

        /// <summary>
        /// Nom de l'executable
        /// </summary>
        public const string ExeName = "XnrgyEngineeringAutomationTools.exe";

        /// <summary>
        /// Nom de l'icone
        /// </summary>
        public const string IconName = "XnrgyEngineeringAutomationTools.ico";

        /// <summary>
        /// GUID d'installation
        /// </summary>
        public const string UninstallGuid = "{XNRGY-EAT-2026-INSTALL}";

        #endregion

        #region Titres et Copyright GENERES - NE PAS CODER EN DUR

        /// <summary>
        /// Suffixe pour les titres de fenetres modules
        /// Format: " - XEAT - v1.0.0 - R YYYY-MM-DD - By Author - Company"
        /// </summary>
        public static string TitleSuffix => $" - {ShortName} - {Version} - {ReleaseDate} - By {Author} - {Company}";

        /// <summary>
        /// Suffixe pour MainWindow (sans XEAT)
        /// Format: " - v1.0.0 - R YYYY-MM-DD - By Author - Company"
        /// </summary>
        public static string MainTitleSuffix => $" - {Version} - {ReleaseDate} - By {Author} - {Company}";

        /// <summary>
        /// Titre complet MainWindow
        /// </summary>
        public static string MainWindowTitle => $"{FullName}{MainTitleSuffix}";

        /// <summary>
        /// Copyright complet
        /// </summary>
        public static string CopyrightFull => $"Copyright (c) {BuildDate.Year} {CompanyNormal}";

        /// <summary>
        /// Publisher complet
        /// </summary>
        public static string Publisher => $"{Author} - {CompanyNormal}";

        /// <summary>
        /// Version avec titre (pour MainWindow interne)
        /// </summary>
        public static string MainTitleWithVersion => $"{FullName} {Version}";

        #endregion

        #region Methodes de Generation de Titres

        /// <summary>
        /// Genere un titre complet pour une fenetre module
        /// </summary>
        /// <param name="moduleName">Nom du module (ex: "Upload Module")</param>
        /// <returns>Titre complet formate</returns>
        public static string GetWindowTitle(string moduleName)
        {
            return $"{moduleName}{TitleSuffix}";
        }

        /// <summary>
        /// Genere un texte copyright complet pour une fenetre
        /// </summary>
        /// <param name="moduleName">Nom du module (ex: "Smart Tools")</param>
        /// <returns>Copyright complet formate</returns>
        public static string GetCopyright(string moduleName)
        {
            return $"{moduleName} - {ShortName} - {Version} - {ReleaseDate} - By {Author} - {Company}";
        }

        /// <summary>
        /// Genere un copyright pour MainWindow (sans XEAT)
        /// </summary>
        /// <returns>Copyright MainWindow</returns>
        public static string GetMainCopyright()
        {
            return $"{FullName} - {Version} - {ReleaseDate} - By {Author} - {Company}";
        }

        #endregion

        #region Date de Build Automatique

        /// <summary>
        /// Recupere la date de build a partir des metadonnees de l'assembly.
        /// Utilise plusieurs methodes pour garantir la precision.
        /// </summary>
        private static DateTime GetBuildDate()
        {
            try
            {
                // Methode 1: Essayer de lire depuis le PE header (plus fiable)
                var assembly = Assembly.GetExecutingAssembly();
                var buildDate = GetLinkerTime(assembly);
                
                if (buildDate.Year >= 2024 && buildDate.Year <= 2030)
                {
                    return buildDate;
                }
            }
            catch
            {
                // Ignorer les erreurs et utiliser fallback
            }

            // Methode 2: Date de derniere modification du fichier assembly
            try
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(assemblyPath) && System.IO.File.Exists(assemblyPath))
                {
                    return System.IO.File.GetLastWriteTime(assemblyPath);
                }
            }
            catch
            {
                // Ignorer les erreurs
            }

            // Fallback: Date actuelle (ne devrait jamais arriver en production)
            return DateTime.Now;
        }

        /// <summary>
        /// Lit la date du linker depuis le PE header de l'assembly.
        /// Cette methode donne la date exacte de compilation.
        /// </summary>
        private static DateTime GetLinkerTime(Assembly assembly)
        {
            const int peHeaderOffset = 60;
            const int linkerTimestampOffset = 8;

            var filePath = assembly.Location;
            var buffer = new byte[2048];

            using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                stream.Read(buffer, 0, buffer.Length);
            }

            var i = BitConverter.ToInt32(buffer, peHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, i + linkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, TimeZoneInfo.Local);

            return localTime;
        }

        #endregion

        #region Dictionnaires de Titres Pre-generes (pour performance XAML)

        /// <summary>
        /// Dictionnaire de tous les titres de fenetres pre-generes
        /// </summary>
        public static class WindowTitles
        {
            // MainWindow
            public static string Main => MainWindowTitle;
            
            // Modules principaux
            public static string UploadModule => GetWindowTitle("Upload Module");
            public static string UploadTemplate => GetWindowTitle("Upload Template");
            public static string CreateModule => GetWindowTitle("Creer Module");
            public static string ConfigUnite => GetWindowTitle("Config Unite");
            public static string PlaceEquipment => GetWindowTitle("Place Equipment");
            public static string BuildModule => GetWindowTitle("Build Module");
            public static string SmartTools => GetWindowTitle("Smart Tools");
            public static string ChecklistHVAC => GetWindowTitle("Checklist HVAC");
            public static string ACP => GetWindowTitle("ACP");
            public static string UpdateWorkspace => GetWindowTitle("Update Workspace");
            public static string OpenVaultProject => GetWindowTitle("Ouvrir Projet");
            public static string Nesting => GetWindowTitle("Module Nesting");
            
            // Fenetres secondaires
            public static string Login => GetWindowTitle("Connexion Vault");
            public static string Preview => GetWindowTitle("Previsualisation");
            public static string ModuleSelection => GetWindowTitle("Selection Module");
            public static string CreateModuleSettings => GetWindowTitle("Reglages Creer Module");
            public static string PlaceEquipmentSettings => GetWindowTitle("Reglages Place Equipment");
            
            // SmartTools
            public static string ExportOptions => GetWindowTitle("Export IAM Options");
            public static string Progress => GetWindowTitle("Progression");
            public static string ProgressHtml => GetWindowTitle("Progression HTML");
            public static string HtmlPopup => GetWindowTitle("Popup HTML");
            public static string ConstraintReport => GetWindowTitle("Rapport Contraintes");
            public static string IProperties => GetWindowTitle("iProperties Summary");
            public static string CustomPropertyBatch => GetWindowTitle("Gestionnaire Proprietes");
            
            // DXF Verifier
            public static string DXFVerifier => GetWindowTitle("DXF Verifier");
            public static string DXFProjectSelector => GetWindowTitle("Selection Projet");
            public static string DXFVerifierInfo => GetWindowTitle("A propos - DXF Verifier");
            
            // Fenetres utilitaires SmartTools (sans copyright)
            public static string SmartToolsInfo => GetWindowTitle("Smart Tools - Informations");
            public static string SheetSelector => GetWindowTitle("Export PDF Shop Drawing");
            public static string FolderBrowser => GetWindowTitle("Selection Dossier");
            
            // Fenetres systeme (titres simples pour SplashScreen, MessageBox, etc.)
            public static string SplashScreen => FullName;
            public static string FirebaseAlert => FullName;
            public static string UpdateDownload => GetWindowTitle("Mise a jour");
            public static string MessageBox => ShortName;
            
            // Installateur
            public static string Installer => $"{FullName} - Installation";
        }

        /// <summary>
        /// Dictionnaire de tous les copyrights pre-generes
        /// </summary>
        public static class Copyrights
        {
            // MainWindow
            public static string Main => GetMainCopyright();
            
            // Modules
            public static string SmartTools => GetCopyright("Smart Tools");
            public static string ChecklistHVAC => GetCopyright("Checklist HVAC");
            public static string ACP => GetCopyright("ACP");
            public static string UploadModule => GetCopyright("Upload Module");
            public static string UploadTemplate => GetCopyright("Upload Template");
            public static string CreateModule => GetCopyright("Creer Module");
            public static string ConfigUnite => GetCopyright("Config Unite");
            public static string PlaceEquipment => GetCopyright("Place Equipment");
            public static string BuildModule => GetCopyright("Build Module");
            public static string UpdateWorkspace => GetCopyright("Update Workspace");
            public static string OpenVaultProject => GetCopyright("Ouvrir Projet Vault");
            public static string Nesting => GetCopyright("Module Nesting - Production");
            public static string Login => GetCopyright("Connexion Vault");
            public static string DXFVerifier => GetCopyright("DXF Verifier");
            
            // SmartTools
            public static string ExportOptions => GetCopyright("Export Options");
            public static string Progress => GetCopyright("Progression");
            public static string HtmlPopup => GetCopyright("Popup HTML");
            public static string ConstraintReport => GetCopyright("Rapport Contraintes");
            public static string IProperties => GetCopyright("iProperties Summary");
            public static string CustomPropertyBatch => GetCopyright("Gestionnaire Proprietes");
        }

        #endregion
    }
}
