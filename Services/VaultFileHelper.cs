using System;
using System.IO;

namespace XnrgyEngineeringAutomationTools.Services
{
    /// <summary>
    /// Classe utilitaire pour la gestion des fichiers Vault.
    /// Les fichiers telecharges depuis Vault sont TOUJOURS en ReadOnly.
    /// Cette classe fournit des methodes pour retirer cet attribut automatiquement.
    /// 
    /// USAGE: Appeler RemoveReadOnlyRecursive() ou RemoveReadOnly() AVANT toute
    /// operation de modification sur des fichiers dans C:\Vault\
    /// </summary>
    public static class VaultFileHelper
    {
        private static readonly NLog.Logger NLogger = NLog.LogManager.GetCurrentClassLogger();
        
        /// <summary>
        /// Chemin racine Vault standard
        /// </summary>
        public const string VAULT_ROOT = @"C:\Vault";
        
        /// <summary>
        /// Verifie si un chemin est dans le dossier Vault
        /// </summary>
        public static bool IsVaultPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.StartsWith(VAULT_ROOT, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Retire l'attribut ReadOnly d'un fichier s'il est present.
        /// </summary>
        /// <param name="filePath">Chemin du fichier</param>
        /// <returns>True si l'attribut a ete retire, False sinon</returns>
        public static bool RemoveReadOnly(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) 
                    return false;
                
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.IsReadOnly)
                {
                    fileInfo.IsReadOnly = false;
                    NLogger.Trace($"[VaultFileHelper] ReadOnly retire: {Path.GetFileName(filePath)}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                NLogger.Warn($"[VaultFileHelper] Erreur RemoveReadOnly({filePath}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retire l'attribut ReadOnly de tous les fichiers et dossiers dans un chemin.
        /// Utilise la recursivite pour traiter tous les sous-dossiers.
        /// </summary>
        /// <param name="path">Chemin du dossier ou fichier</param>
        /// <param name="logCallback">Callback optionnel pour le logging</param>
        /// <returns>Nombre de fichiers/dossiers modifies</returns>
        public static int RemoveReadOnlyRecursive(string path, Action<string, string>? logCallback = null)
        {
            int count = 0;
            
            try
            {
                if (string.IsNullOrEmpty(path)) return 0;
                
                // Verifier si c'est un fichier
                if (File.Exists(path))
                {
                    if (RemoveReadOnly(path))
                        count++;
                    return count;
                }
                
                // Verifier si c'est un dossier
                if (!Directory.Exists(path)) return 0;
                
                // Retirer ReadOnly du dossier lui-meme
                var dirInfo = new DirectoryInfo(path);
                if ((dirInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    dirInfo.Attributes &= ~FileAttributes.ReadOnly;
                    count++;
                }
                
                // Traiter tous les fichiers
                foreach (var file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                {
                    try
                    {
                        if (RemoveReadOnly(file))
                            count++;
                    }
                    catch { /* Ignorer les erreurs individuelles */ }
                }
                
                // Traiter tous les sous-dossiers
                foreach (var dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var subDirInfo = new DirectoryInfo(dir);
                        if ((subDirInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            subDirInfo.Attributes &= ~FileAttributes.ReadOnly;
                            count++;
                        }
                    }
                    catch { /* Ignorer les erreurs individuelles */ }
                }
                
                if (count > 0)
                {
                    NLogger.Debug($"[VaultFileHelper] ReadOnly retire de {count} elements dans: {path}");
                    logCallback?.Invoke($"[+] ReadOnly retire de {count} elements", "DEBUG");
                }
            }
            catch (Exception ex)
            {
                NLogger.Warn($"[VaultFileHelper] Erreur RemoveReadOnlyRecursive: {ex.Message}");
                logCallback?.Invoke($"[!] Erreur RemoveReadOnlyRecursive: {ex.Message}", "WARN");
            }
            
            return count;
        }

        /// <summary>
        /// Prepare un fichier ou dossier Vault pour modification.
        /// Retire ReadOnly si le chemin est dans C:\Vault\
        /// </summary>
        /// <param name="path">Chemin du fichier ou dossier</param>
        /// <returns>Nombre d'elements prepares</returns>
        public static int PrepareForModification(string path)
        {
            if (!IsVaultPath(path)) return 0;
            return RemoveReadOnlyRecursive(path);
        }

        /// <summary>
        /// Copie un fichier et retire l'attribut ReadOnly de la destination.
        /// TOUJOURS utiliser cette methode au lieu de File.Copy pour les fichiers Vault.
        /// </summary>
        public static void CopyFile(string source, string destination, bool overwrite = true)
        {
            File.Copy(source, destination, overwrite);
            RemoveReadOnly(destination);
        }

        /// <summary>
        /// Deplace un fichier. Si la destination est dans Vault, retire ReadOnly apres.
        /// TOUJOURS utiliser cette methode au lieu de File.Move pour les fichiers Vault.
        /// </summary>
        public static void MoveFile(string source, string destination)
        {
            // Si source est ReadOnly, retirer d'abord
            RemoveReadOnly(source);
            File.Move(source, destination);
            // Si destination est dans Vault, s'assurer qu'il est editable
            if (IsVaultPath(destination))
            {
                RemoveReadOnly(destination);
            }
        }

        /// <summary>
        /// Ecrit du texte dans un fichier. Retire ReadOnly si necessaire.
        /// TOUJOURS utiliser cette methode au lieu de File.WriteAllText pour les fichiers Vault.
        /// </summary>
        public static void WriteAllText(string path, string contents)
        {
            RemoveReadOnly(path);
            File.WriteAllText(path, contents);
        }

        /// <summary>
        /// Ecrit des bytes dans un fichier. Retire ReadOnly si necessaire.
        /// TOUJOURS utiliser cette methode au lieu de File.WriteAllBytes pour les fichiers Vault.
        /// </summary>
        public static void WriteAllBytes(string path, byte[] bytes)
        {
            RemoveReadOnly(path);
            File.WriteAllBytes(path, bytes);
        }

        /// <summary>
        /// Ouvre un FileStream pour ecriture. Retire ReadOnly si necessaire.
        /// </summary>
        public static FileStream OpenWrite(string path)
        {
            RemoveReadOnly(path);
            return new FileStream(path, FileMode.Create, FileAccess.Write);
        }

        /// <summary>
        /// Supprime un fichier. Retire ReadOnly si necessaire.
        /// </summary>
        public static void DeleteFile(string path)
        {
            RemoveReadOnly(path);
            File.Delete(path);
        }
    }
}
