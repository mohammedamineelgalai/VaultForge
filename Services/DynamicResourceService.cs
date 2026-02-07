// ============================================================================
// DynamicResourceService.cs
// Service pour charger les ressources dynamiques (titres, copyrights) depuis AppInfo
// Auteur: Mohammed Amine Elgalai - XNRGY Climate Systems ULC
// ============================================================================

using System;
using System.Windows;

namespace XnrgyEngineeringAutomationTools.Services
{
    /// <summary>
    /// Service qui charge les ressources dynamiques dans Application.Resources
    /// a partir de la classe AppInfo centralisee.
    /// 
    /// Permet de garder les StaticResource dans XAML tout en ayant
    /// des valeurs generees dynamiquement au runtime (date de build, version).
    /// </summary>
    public static class DynamicResourceService
    {
        /// <summary>
        /// Charge toutes les ressources dynamiques depuis AppInfo dans Application.Resources
        /// Doit etre appele au demarrage de l'application (App.xaml.cs OnStartup)
        /// </summary>
        public static void LoadDynamicResources()
        {
            var resources = Application.Current.Resources;

            // ========== VARIABLES GLOBALES ==========
            UpdateResource(resources, "AppVersion", AppInfo.Version);
            UpdateResource(resources, "AppReleaseDate", AppInfo.ReleaseDate);
            UpdateResource(resources, "AppAuthor", AppInfo.Author);
            UpdateResource(resources, "AppCompany", AppInfo.Company);
            UpdateResource(resources, "AppShortName", AppInfo.ShortName);
            UpdateResource(resources, "AppFullName", AppInfo.FullName);

            // ========== GRANDS TITRES INTERNES ==========
            UpdateResource(resources, "MainTitleWithVersion", AppInfo.MainTitleWithVersion);
            UpdateResource(resources, "AppTitleSuffix", AppInfo.TitleSuffix);
            UpdateResource(resources, "AppMainTitleSuffix", AppInfo.MainTitleSuffix);

            // ========== TITRES COMPLETS DES FENETRES ==========
            // MainWindow
            UpdateResource(resources, "WindowTitleMain", AppInfo.WindowTitles.Main);
            
            // Modules principaux
            UpdateResource(resources, "WindowTitleUploadModule", AppInfo.WindowTitles.UploadModule);
            UpdateResource(resources, "WindowTitleUploadTemplate", AppInfo.WindowTitles.UploadTemplate);
            UpdateResource(resources, "WindowTitleCreateModule", AppInfo.WindowTitles.CreateModule);
            UpdateResource(resources, "WindowTitleConfigUnite", AppInfo.WindowTitles.ConfigUnite);
            UpdateResource(resources, "WindowTitlePlaceEquipment", AppInfo.WindowTitles.PlaceEquipment);
            UpdateResource(resources, "WindowTitleBuildModule", AppInfo.WindowTitles.BuildModule);
            UpdateResource(resources, "WindowTitleSmartTools", AppInfo.WindowTitles.SmartTools);
            UpdateResource(resources, "WindowTitleChecklistHVAC", AppInfo.WindowTitles.ChecklistHVAC);
            UpdateResource(resources, "WindowTitleACP", AppInfo.WindowTitles.ACP);
            UpdateResource(resources, "WindowTitleUpdateWorkspace", AppInfo.WindowTitles.UpdateWorkspace);
            UpdateResource(resources, "WindowTitleOpenVaultProject", AppInfo.WindowTitles.OpenVaultProject);
            UpdateResource(resources, "WindowTitleNesting", AppInfo.WindowTitles.Nesting);
            
            // Fenetres secondaires
            UpdateResource(resources, "WindowTitleLogin", AppInfo.WindowTitles.Login);
            UpdateResource(resources, "WindowTitlePreview", AppInfo.WindowTitles.Preview);
            UpdateResource(resources, "WindowTitleModuleSelection", AppInfo.WindowTitles.ModuleSelection);
            UpdateResource(resources, "WindowTitleCreateModuleSettings", AppInfo.WindowTitles.CreateModuleSettings);
            UpdateResource(resources, "WindowTitlePlaceEquipmentSettings", AppInfo.WindowTitles.PlaceEquipmentSettings);
            
            // SmartTools
            UpdateResource(resources, "WindowTitleExportOptions", AppInfo.WindowTitles.ExportOptions);
            UpdateResource(resources, "WindowTitleProgress", AppInfo.WindowTitles.Progress);
            UpdateResource(resources, "WindowTitleProgressHtml", AppInfo.WindowTitles.ProgressHtml);
            UpdateResource(resources, "WindowTitleHtmlPopup", AppInfo.WindowTitles.HtmlPopup);
            UpdateResource(resources, "WindowTitleConstraintReport", AppInfo.WindowTitles.ConstraintReport);
            UpdateResource(resources, "WindowTitleIProperties", AppInfo.WindowTitles.IProperties);
            UpdateResource(resources, "WindowTitleCustomPropertyBatch", AppInfo.WindowTitles.CustomPropertyBatch);
            UpdateResource(resources, "WindowTitleSmartProgress", AppInfo.WindowTitles.Progress);
            
            // DXF Verifier
            UpdateResource(resources, "WindowTitleDXFVerifier", AppInfo.WindowTitles.DXFVerifier);
            UpdateResource(resources, "WindowTitleDXFProjectSelector", AppInfo.WindowTitles.DXFProjectSelector);
            UpdateResource(resources, "WindowTitleDXFVerifierInfo", AppInfo.WindowTitles.DXFVerifierInfo);
            
            // Fenetres utilitaires SmartTools
            UpdateResource(resources, "WindowTitleSmartToolsInfo", AppInfo.WindowTitles.SmartToolsInfo);
            UpdateResource(resources, "WindowTitleSheetSelector", AppInfo.WindowTitles.SheetSelector);
            UpdateResource(resources, "WindowTitleFolderBrowser", AppInfo.WindowTitles.FolderBrowser);
            
            // Fenetres systeme
            UpdateResource(resources, "WindowTitleSplashScreen", AppInfo.WindowTitles.SplashScreen);
            UpdateResource(resources, "WindowTitleFirebaseAlert", AppInfo.WindowTitles.FirebaseAlert);
            UpdateResource(resources, "WindowTitleUpdateDownload", AppInfo.WindowTitles.UpdateDownload);
            UpdateResource(resources, "WindowTitleMessageBox", AppInfo.WindowTitles.MessageBox);
            UpdateResource(resources, "WindowTitleInstaller", AppInfo.WindowTitles.Installer);

            // ========== TEXTES COPYRIGHT COMPLETS ==========
            UpdateResource(resources, "CopyrightMain", AppInfo.Copyrights.Main);
            UpdateResource(resources, "CopyrightSmartTools", AppInfo.Copyrights.SmartTools);
            UpdateResource(resources, "CopyrightChecklist", AppInfo.Copyrights.ChecklistHVAC);
            UpdateResource(resources, "CopyrightACP", AppInfo.Copyrights.ACP);
            UpdateResource(resources, "CopyrightUploadModule", AppInfo.Copyrights.UploadModule);
            UpdateResource(resources, "CopyrightUploadTpl", AppInfo.Copyrights.UploadTemplate);
            UpdateResource(resources, "CopyrightCreateModule", AppInfo.Copyrights.CreateModule);
            UpdateResource(resources, "CopyrightConfigUnite", AppInfo.Copyrights.ConfigUnite);
            UpdateResource(resources, "CopyrightPlaceEquip", AppInfo.Copyrights.PlaceEquipment);
            UpdateResource(resources, "CopyrightBuildModule", AppInfo.Copyrights.BuildModule);
            UpdateResource(resources, "CopyrightUpdateWS", AppInfo.Copyrights.UpdateWorkspace);
            UpdateResource(resources, "CopyrightOpenVaultProject", AppInfo.Copyrights.OpenVaultProject);
            UpdateResource(resources, "CopyrightNesting", AppInfo.Copyrights.Nesting);
            UpdateResource(resources, "CopyrightLogin", AppInfo.Copyrights.Login);
            UpdateResource(resources, "CopyrightDXFVerifier", AppInfo.Copyrights.DXFVerifier);
            UpdateResource(resources, "CopyrightDXFProjectSelector", AppInfo.GetCopyright("Selection Projet"));
            UpdateResource(resources, "CopyrightDXFVerifierInfo", AppInfo.GetCopyright("A propos - DXF Verifier"));
            UpdateResource(resources, "CopyrightExportOptions", AppInfo.Copyrights.ExportOptions);
            UpdateResource(resources, "CopyrightProgress", AppInfo.Copyrights.Progress);
            UpdateResource(resources, "CopyrightHtmlPopup", AppInfo.Copyrights.HtmlPopup);
            UpdateResource(resources, "CopyrightConstraintReport", AppInfo.Copyrights.ConstraintReport);
            UpdateResource(resources, "CopyrightIProperties", AppInfo.Copyrights.IProperties);
            UpdateResource(resources, "CopyrightCustomPropertyBatch", AppInfo.Copyrights.CustomPropertyBatch);
            
            // Copyrights fenetres utilitaires SmartTools
            UpdateResource(resources, "CopyrightSmartToolsInfo", AppInfo.GetCopyright("A propos - Smart Tools"));
            UpdateResource(resources, "CopyrightSheetSelector", AppInfo.GetCopyright("Export PDF Shop Drawing"));
            UpdateResource(resources, "CopyrightFolderBrowser", AppInfo.GetCopyright("Parcourir Dossier"));
            UpdateResource(resources, "CopyrightSmartProgress", AppInfo.GetCopyright("Progression"));

            // ========== STATUS DXF VERIFIER ==========
            UpdateResource(resources, "StatusDXFVerifier", $"Pret - DXF-CSV vs PDF Verifier v1.2 - {AppInfo.CompanyNormal}");

            Logger.Info($"[+] Ressources dynamiques chargees - Version: {AppInfo.Version} - Date: {AppInfo.ReleaseDate}");
        }

        /// <summary>
        /// Met a jour une ressource si elle existe deja, sinon l'ajoute
        /// Met aussi a jour dans les MergedDictionaries si present
        /// </summary>
        private static void UpdateResource(ResourceDictionary resources, string key, object value)
        {
            try
            {
                // D'abord mettre a jour dans les MergedDictionaries (XnrgyStyles.xaml, etc.)
                UpdateResourceInMergedDictionaries(resources, key, value);

                // Puis ajouter/mettre a jour au niveau racine pour les DynamicResource
                if (resources.Contains(key))
                {
                    resources[key] = value;
                }
                else
                {
                    resources.Add(key, value);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[!] Impossible de mettre a jour la ressource '{key}': {ex.Message}");
            }
        }

        /// <summary>
        /// Parcourt les MergedDictionaries et met a jour la ressource si elle y existe
        /// </summary>
        private static void UpdateResourceInMergedDictionaries(ResourceDictionary resources, string key, object value)
        {
            foreach (ResourceDictionary mergedDict in resources.MergedDictionaries)
            {
                try
                {
                    if (mergedDict.Contains(key))
                    {
                        mergedDict[key] = value;
                    }
                    
                    // Recursion pour les dictionnaires imbriques
                    if (mergedDict.MergedDictionaries.Count > 0)
                    {
                        UpdateResourceInMergedDictionaries(mergedDict, key, value);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[!] Erreur mise a jour MergedDictionary '{key}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Obtient le titre de fenetre pour un module specifique
        /// Utile pour les fenetres creees dynamiquement
        /// </summary>
        public static string GetWindowTitle(string moduleName)
        {
            return AppInfo.GetWindowTitle(moduleName);
        }

        /// <summary>
        /// Obtient le copyright pour un module specifique
        /// </summary>
        public static string GetCopyright(string moduleName)
        {
            return AppInfo.GetCopyright(moduleName);
        }
    }
}
