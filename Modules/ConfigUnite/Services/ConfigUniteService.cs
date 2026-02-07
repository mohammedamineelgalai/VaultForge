using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using XnrgyEngineeringAutomationTools.Modules.ConfigUnite.Models;
using XnrgyEngineeringAutomationTools.Services;

namespace XnrgyEngineeringAutomationTools.Modules.ConfigUnite.Services
{
    /// <summary>
    /// Service pour gerer la configuration d'unite (JSON) et l'upload vers Vault
    /// Structure centralisee: Config_Unites/[Projet][Reference].config
    /// </summary>
    public class ConfigUniteService
    {
        private readonly VaultSdkService? _vaultService;
        
        // Chemins Vault et Local - Structure centralisee Config_Unites
        private const string VAULT_CONFIG_FOLDER = "$/Engineering/Projects/Config_Unites";
        private const string LOCAL_CONFIG_BASE = @"C:\Vault\Engineering\Projects\Config_Unites";

        // Options JSON pour serialisation
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public ConfigUniteService(VaultSdkService? vaultService = null)
        {
            _vaultService = vaultService;
        }

        /// <summary>
        /// Obtient le chemin local du fichier de configuration
        /// Format: C:\Vault\Engineering\Projects\Config_Unites\[Projet][Reference].config
        /// </summary>
        public string GetLocalConfigPath(string projectNumber, string reference)
        {
            if (string.IsNullOrEmpty(projectNumber) || string.IsNullOrEmpty(reference))
            {
                return Path.Combine(LOCAL_CONFIG_BASE, "default.config");
            }

            // Normaliser la reference (enlever REF prefix si present)
            string normalizedRef = reference.Replace("REF", "").Trim();
            if (normalizedRef.Length == 1) normalizedRef = "0" + normalizedRef;
            
            return Path.Combine(LOCAL_CONFIG_BASE, $"{projectNumber}{normalizedRef}.config");
        }

        /// <summary>
        /// Obtient le chemin Vault du fichier de configuration
        /// Format: $/Engineering/Projects/Config_Unites/[Projet][Reference].config
        /// </summary>
        public string GetVaultConfigPath(string projectNumber, string reference)
        {
            if (string.IsNullOrEmpty(projectNumber) || string.IsNullOrEmpty(reference))
            {
                return $"{VAULT_CONFIG_FOLDER}/default.config";
            }

            // Normaliser la reference (enlever REF prefix si present)
            string normalizedRef = reference.Replace("REF", "").Trim();
            if (normalizedRef.Length == 1) normalizedRef = "0" + normalizedRef;
            
            return $"{VAULT_CONFIG_FOLDER}/{projectNumber}{normalizedRef}.config";
        }

        /// <summary>
        /// Charge la configuration depuis un fichier JSON local
        /// </summary>
        public ConfigUniteDataModel? LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Logger.Log($"[ConfigUnite] Fichier de configuration introuvable: {filePath}", Logger.LogLevel.DEBUG);
                    return null;
                }

                // Retirer ReadOnly si nécessaire (fichiers Vault)
                VaultFileHelper.RemoveReadOnly(filePath);

                string jsonContent = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<ConfigUniteDataModel>(jsonContent, JsonOptions);

                if (config != null)
                {
                    Logger.Log($"[ConfigUnite] Configuration chargée depuis: {filePath}", Logger.LogLevel.INFO);
                }

                return config;
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConfigUnite] Erreur lors du chargement: {ex.Message}", Logger.LogLevel.ERROR);
                return null;
            }
        }

        /// <summary>
        /// Sauvegarde la configuration dans un fichier JSON local
        /// </summary>
        public bool SaveToFile(ConfigUniteDataModel config, string filePath)
        {
            try
            {
                // Créer le dossier si nécessaire
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Logger.Log($"[ConfigUnite] Dossier créé: {directory}", Logger.LogLevel.DEBUG);
                }

                // Mettre à jour les métadonnées
                config.LastModified = DateTime.Now;
                // config.LastModifiedBy sera défini par l'appelant

                // Retirer ReadOnly si nécessaire
                if (File.Exists(filePath))
                {
                    VaultFileHelper.RemoveReadOnly(filePath);
                }

                // Sérialiser en JSON
                string jsonContent = JsonSerializer.Serialize(config, JsonOptions);
                File.WriteAllText(filePath, jsonContent);

                Logger.Log($"[ConfigUnite] Configuration sauvegardee: {filePath}", Logger.LogLevel.INFO);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConfigUnite] Erreur lors de la sauvegarde: {ex.Message}", Logger.LogLevel.ERROR);
                return false;
            }
        }

        /// <summary>
        /// Serialise la configuration en bytes (pour UpdateFileContentInVault)
        /// </summary>
        private byte[] SerializeConfigToBytes(ConfigUniteDataModel config)
        {
            // Mettre à jour les métadonnées
            config.LastModified = DateTime.Now;
            
            // Sérialiser en JSON puis en bytes UTF8
            string jsonContent = JsonSerializer.Serialize(config, JsonOptions);
            return System.Text.Encoding.UTF8.GetBytes(jsonContent);
        }

        /// <summary>
        /// Upload la configuration vers Vault (structure centralisee Config_Unites)
        /// Utilise la meme logique que VaultSettingsService: Delete + Add si existe deja
        /// </summary>
        public async Task<bool> UploadToVaultAsync(string localFilePath, string projectNumber, string reference, string? comment = null)
        {
            if (_vaultService == null || !_vaultService.IsConnected)
            {
                Logger.Log("[ConfigUnite] Vault non connecte - upload impossible", Logger.LogLevel.WARNING);
                return false;
            }

            try
            {
                if (!File.Exists(localFilePath))
                {
                    Logger.Log("[ConfigUnite] Fichier local introuvable pour upload", Logger.LogLevel.ERROR);
                    return false;
                }

                // Construire le chemin Vault centralise
                string vaultFilePath = GetVaultConfigPath(projectNumber, reference);
                string fileName = Path.GetFileName(localFilePath);
                
                Logger.Log($"[ConfigUnite] Upload vers Vault: {vaultFilePath}", Logger.LogLevel.INFO);

                // Verifier si le fichier existe deja dans Vault
                var existingFile = _vaultService.FindFileByPath(vaultFilePath);

                if (existingFile != null)
                {
                    // Fichier existe deja - utiliser UpdateFileInVault pour garder l'historique
                    // NE PAS supprimer/re-ajouter car ca perd l'historique des versions
                    Logger.Log("[ConfigUnite] Fichier existe dans Vault - mise a jour avec versioning...", Logger.LogLevel.DEBUG);
                    string updateComment = comment ?? $"[Auto] MAJ Config Unite | XEAT";
                    bool updateSuccess = _vaultService.UpdateFileInVault(existingFile, localFilePath, updateComment);
                    if (updateSuccess)
                    {
                        Logger.Log("[ConfigUnite] Configuration mise a jour dans Vault (nouvelle version)", Logger.LogLevel.INFO);
                        
                        // GET pour synchroniser le fichier local avec Vault (enleve icone jaune)
                        SyncLocalFileWithVault(vaultFilePath, localFilePath);
                    }
                    else
                    {
                        Logger.Log("[ConfigUnite] Echec mise a jour Vault", Logger.LogLevel.ERROR);
                    }
                    return updateSuccess;
                }

                // Ajouter le nouveau fichier (premiere version)
                Logger.Log("[ConfigUnite] Ajout nouveau fichier config dans Vault...", Logger.LogLevel.DEBUG);
                string addComment = comment ?? $"[Auto] Nouvelle config | XEAT";
                
                // Le dossier parent est Config_Unites (pas besoin d'extraire du path)
                bool success = _vaultService.AddFileToVault(localFilePath, VAULT_CONFIG_FOLDER, addComment);
                if (success)
                {
                    Logger.Log($"[ConfigUnite] Configuration ajoutee dans Vault: {vaultFilePath}", Logger.LogLevel.INFO);
                    
                    // GET pour synchroniser le fichier local avec Vault (enleve icone jaune)
                    SyncLocalFileWithVault(vaultFilePath, localFilePath);
                }
                else
                {
                    Logger.Log($"[ConfigUnite] Echec ajout dans Vault: {vaultFilePath}", Logger.LogLevel.ERROR);
                }
                return success;
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConfigUnite] Erreur upload vers Vault: {ex.Message}", Logger.LogLevel.ERROR);
                return false;
            }
        }

        /// <summary>
        /// Met a jour un fichier config existant dans Vault avec versioning (GET+CheckOut -> Save -> CheckIn)
        /// Utilise pour le bouton "Sauvegarder" pour conserver l'historique des versions
        /// WORKFLOW SIMPLIFIE:
        /// 1. GET + CheckOut (synchronise Vault -> local)
        /// 2. Sauvegarder le modele sur le fichier local (apres GET)
        /// 3. CheckIn (local -> Vault, nouvelle version)
        /// </summary>
        /// <param name="configData">Modele de configuration a sauvegarder</param>
        /// <param name="localFilePath">Chemin du fichier local</param>
        /// <param name="projectNumber">Numero de projet</param>
        /// <param name="reference">Reference</param>
        /// <param name="jobTitle">Titre du job (pour le commentaire)</param>
        /// <returns>true si succes, false sinon</returns>
        public async Task<(bool success, string message)> CheckoutUpdateCheckinAsync(
            ConfigUniteDataModel configData,
            string localFilePath, 
            string projectNumber, 
            string reference, 
            string jobTitle)
        {
            if (_vaultService == null || !_vaultService.IsConnected)
            {
                Logger.Log("[ConfigUnite] Vault non connecte - mise a jour impossible", Logger.LogLevel.WARNING);
                return (false, "Vault non connecte");
            }

            try
            {
                string vaultFilePath = GetVaultConfigPath(projectNumber, reference);
                string fileName = Path.GetFileName(localFilePath);
                
                Logger.Log($"[ConfigUnite] Mise a jour versionnee: {vaultFilePath}", Logger.LogLevel.INFO);

                // Verifier si le fichier existe dans Vault
                var existingFile = _vaultService.FindFileByPath(vaultFilePath);

                if (existingFile == null)
                {
                    // Fichier n'existe pas - sauvegarder local puis AddFile
                    Logger.Log("[ConfigUnite] Fichier non trouve dans Vault - upload initial...", Logger.LogLevel.INFO);
                    
                    // Sauvegarder localement d'abord
                    SaveToFile(configData, localFilePath);
                    
                    string addComment = GenerateCheckinComment("Nouvelle config", projectNumber, reference, jobTitle);
                    bool addSuccess = _vaultService.AddFileToVault(localFilePath, VAULT_CONFIG_FOLDER, addComment);
                    
                    if (addSuccess)
                    {
                        Logger.Log($"[ConfigUnite] Nouvelle configuration ajoutee dans Vault", Logger.LogLevel.INFO);
                        return (true, "Nouvelle configuration creee dans Vault");
                    }
                    else
                    {
                        return (false, "Echec de l'ajout dans Vault");
                    }
                }

                // ============================================
                // FICHIER EXISTE DANS VAULT - MISE A JOUR VERSIONNEE
                // ============================================
                // WORKFLOW CORRECT AVEC PRESERVATION DU CONTENU:
                // 1. Serialiser le nouveau contenu en memoire (bytes)
                // 2. UpdateFileContentInVault (CheckOut -> Reecrit contenu -> CheckIn)
                // 3. GET pour synchroniser et enlever icone jaune
                
                Logger.Log($"[ConfigUnite] Mise a jour versionnee: {fileName}", Logger.LogLevel.DEBUG);
                string checkinComment = GenerateCheckinComment("MAJ Config Unite", projectNumber, reference, jobTitle);
                
                // ETAPE 1: Serialiser le nouveau contenu en memoire
                Logger.Log($"[ConfigUnite] ETAPE 1 - Serialisation du contenu: {fileName}", Logger.LogLevel.DEBUG);
                byte[] newContent = SerializeConfigToBytes(configData);
                Logger.Log($"[ConfigUnite] Contenu serialise: {newContent.Length} bytes", Logger.LogLevel.DEBUG);
                
                // ETAPE 2: UpdateFileContentInVault 
                // Cette methode fait: CheckOut -> Reecrit notre contenu -> CheckIn (nouvelle version)
                Logger.Log($"[ConfigUnite] ETAPE 2 - UpdateFileContentInVault: {fileName}", Logger.LogLevel.DEBUG);
                bool updateSuccess = _vaultService.UpdateFileContentInVault(existingFile, localFilePath, newContent, checkinComment);
                
                if (updateSuccess)
                {
                    // ETAPE 3: GET pour synchroniser (enleve icone jaune)
                    Logger.Log($"[ConfigUnite] ETAPE 3 - Synchronisation locale: {fileName}", Logger.LogLevel.DEBUG);
                    SyncLocalFileWithVault(vaultFilePath, localFilePath);
                    
                    Logger.Log($"[ConfigUnite] Configuration mise a jour dans Vault (nouvelle version)", Logger.LogLevel.INFO);
                    return (true, "Configuration mise a jour (nouvelle version dans Vault)");
                }
                else
                {
                    Logger.Log("[ConfigUnite] Echec de UpdateFileContentInVault", Logger.LogLevel.ERROR);
                    return (false, "Echec de la mise a jour Vault");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConfigUnite] Erreur mise a jour Vault: {ex.Message}", Logger.LogLevel.ERROR);
                return (false, $"Erreur: {ex.Message}");
            }
        }

        /// <summary>
        /// Genere un commentaire de checkin standardise (format XNRGY)
        /// Format: [Auto] Action | XEAT
        /// Exemple: [Auto] MAJ Config Unite | XEAT
        /// </summary>
        private string GenerateCheckinComment(string action, string projectNumber, string reference, string jobTitle)
        {
            // Format simplifie: [Auto] Action | XEAT
            return $"[Auto] {action} | XEAT";
        }

        /// <summary>
        /// Charge la configuration depuis Vault (telecharge si necessaire)
        /// </summary>
        public async Task<ConfigUniteDataModel?> LoadFromVaultAsync(string projectNumber, string reference)
        {
            if (_vaultService == null || !_vaultService.IsConnected)
            {
                Logger.Log("[ConfigUnite] Vault non connecte - chargement impossible", Logger.LogLevel.WARNING);
                return null;
            }

            try
            {
                string vaultPath = GetVaultConfigPath(projectNumber, reference);
                string localPath = GetLocalConfigPath(projectNumber, reference);

                // Verifier si le fichier existe dans Vault
                var vaultFile = _vaultService.FindFileByPath(vaultPath);
                if (vaultFile == null)
                {
                    Logger.Log($"[ConfigUnite] Fichier non trouve dans Vault: {vaultPath}", Logger.LogLevel.DEBUG);
                    return null;
                }

                // Telecharger depuis Vault si necessaire
                if (!File.Exists(localPath) || 
                    File.GetLastWriteTime(localPath) < vaultFile.CreateDate)
                {
                    Logger.Log($"[ConfigUnite] Telechargement depuis Vault: {vaultPath}", Logger.LogLevel.INFO);
                    bool downloaded = _vaultService.AcquireFile(vaultFile, localPath, checkout: false);
                    if (!downloaded)
                    {
                        Logger.Log("[ConfigUnite] Echec du telechargement depuis Vault", Logger.LogLevel.ERROR);
                        return null;
                    }
                }

                // Charger depuis le fichier local
                return LoadFromFile(localPath);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ConfigUnite] Erreur chargement depuis Vault: {ex.Message}", Logger.LogLevel.ERROR);
                return null;
            }
        }

        /// <summary>
        /// Synchronise le fichier local avec Vault apres une operation (AddFile ou UpdateFile)
        /// Ceci telecharge la version Vault vers le fichier local pour synchroniser les metadonnees
        /// et enlever l'icone jaune dans Vault Client
        /// </summary>
        private void SyncLocalFileWithVault(string vaultFilePath, string localFilePath)
        {
            if (_vaultService == null || !_vaultService.IsConnected)
                return;

            try
            {
                Logger.Log($"[ConfigUnite] Synchronisation locale apres operation Vault...", Logger.LogLevel.DEBUG);
                
                var vaultFile = _vaultService.FindFileByPath(vaultFilePath);
                if (vaultFile != null)
                {
                    _vaultService.AcquireFile(vaultFile, localFilePath, checkout: false);
                    Logger.Log("[ConfigUnite] Fichier local synchronise avec Vault", Logger.LogLevel.DEBUG);
                }
            }
            catch (Exception ex)
            {
                // Ne pas echouer si la synchro echoue - l'operation Vault a reussi
                Logger.Log($"[ConfigUnite] [!] Synchro post-operation: {ex.Message}", Logger.LogLevel.WARNING);
            }
        }
    }
}

