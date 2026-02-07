#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VDF = Autodesk.DataManagement.Client.Framework;
using ACW = Autodesk.Connectivity.WebServices;

namespace XnrgyEngineeringAutomationTools.Services
{
    /// <summary>
    /// Service de mise a jour du workspace local depuis Vault.
    /// Telecharge les dossiers critiques, copie les plugins et execute les installations silencieuses.
    /// </summary>
    public class UpdateWorkspaceService
    {
        #region Configuration Constants

        // Chemins Vault source (utiliser / comme separateur pour Vault)
        private static readonly string[] VaultFolderPaths = new[]
        {
            "$/Engineering/Inventor_Standards/",
            "$/Engineering/Library/Cabinet/",
            "$/Engineering/Library/Xnrgy_M99/",
            "$/Engineering/Library/Xnrgy_Module/",
            "$/Engineering/Projects/Config_Unites/",  // Dossier des configurations d'unites (etape 6)
            "$/Engineering/Projects/Inventor_IPJ/"    // Dossier des projets Inventor IPJ (etape 7) - Sync miroir
        };

        // Chemins locaux destination correspondants
        private static readonly string[] LocalFolderPaths = new[]
        {
            @"C:\Vault\Engineering\Inventor_Standards\",
            @"C:\Vault\Engineering\Library\Cabinet\",
            @"C:\Vault\Engineering\Library\Xnrgy_M99\",
            @"C:\Vault\Engineering\Library\Xnrgy_Module\",
            @"C:\Vault\Engineering\Projects\Config_Unites\",  // Dossier local des configurations d'unites
            @"C:\Vault\Engineering\Projects\Inventor_IPJ\"    // Dossier local des projets Inventor IPJ
        };

        // Plugins a copier (depuis Application_Plugins)
        private static readonly (string SourceSubPath, string DestFolder)[] PluginCopyPaths = new[]
        {
            (@"Automation_Standard\Application_Plugins\SIBL_XNRGY_ADDINS_2026", "SIBL_XNRGY_ADDINS_2026"),
            (@"Automation_Standard\Application_Plugins\XNRGY_ADDINS_2026", "XNRGY_ADDINS_2026")
        };

        // Dossier ApplicationPlugins d'Autodesk
        private const string APPLICATION_PLUGINS_PATH = @"C:\ProgramData\Autodesk\ApplicationPlugins";

        // Dossiers a exclure lors de la copie des plugins
        private static readonly string[] ExcludedFolders = new[]
        {
            "Xnrgy_Software",
            "Automation_Data"
        };

        // Extensions de fichiers a exclure lors de la copie
        private static readonly string[] ExcludedExtensions = new[]
        {
            ".bak", ".old", ".tmp", ".log", ".lck", ".dwl", ".dwl2", ".pdb"
        };

        // Prefixes de fichiers a exclure (fichiers generes par installateurs Inno Setup, etc.)
        // Ces fichiers sont crees dans la DESTINATION par les installateurs et ne doivent pas etre supprimes
        private static readonly string[] ExcludedFilePrefixes = new[]
        {
            "unins"    // unins000.dat, unins000.exe - Inno Setup uninstaller files
        };

        // Dossier contenant les installateurs (scan automatique)
        private const string INSTALLERS_FOLDER = @"C:\Vault\Engineering\Inventor_Standards\Automation_Standard\Application_Plugins\XNRGY_ADDINS_2026\Xnrgy_Software";
        
        // Arguments par defaut pour les installateurs Inno Setup
        private const string SILENT_INSTALL_ARGS = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-";
        
        // Chemin du projet Inventor par defaut
        private const string DEFAULT_IPJ_PATH = @"C:\Users\Public\Documents\Autodesk\Inventor 2026\Templates\Default.ipj";

        #endregion

        #region Progress Reporting

        public event EventHandler<UpdateProgressEventArgs>? ProgressChanged;
        public event EventHandler<UpdateLogEventArgs>? LogMessage;
        public event EventHandler<UpdateStepEventArgs>? StepChanged;

        private void ReportProgress(int percent, string status, string? currentFile = null)
        {
            ProgressChanged?.Invoke(this, new UpdateProgressEventArgs(percent, status, currentFile));
        }

        private void Log(string message, LogLevel level = LogLevel.INFO)
        {
            LogMessage?.Invoke(this, new UpdateLogEventArgs(message, level));
            Logger.Log(message, level == LogLevel.ERROR ? Logger.LogLevel.ERROR 
                : level == LogLevel.WARNING ? Logger.LogLevel.WARNING 
                : Logger.LogLevel.INFO);
        }

        private void UpdateStep(int stepNumber, StepStatus status, string? message = null)
        {
            StepChanged?.Invoke(this, new UpdateStepEventArgs(stepNumber, status, message));
        }

        #endregion

        #region Safe Inventor Close
        
        /// <summary>
        /// Ferme Inventor de maniere propre et safe:
        /// 1. Save All - Sauvegarde tous les documents ouverts
        /// 2. Close All - Ferme tous les documents
        /// 3. Switch IPJ vers Default - Evite les conflits de projet
        /// 4. Quit - Ferme Inventor proprement
        /// </summary>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>True si ferme avec succes, False si fallback necessaire</returns>
        private async Task<bool> SafeCloseInventorAsync(CancellationToken cancellationToken)
        {
            Inventor.Application? inventorApp = null;
            bool success = false;
            
            try
            {
                // Essayer de se connecter a Inventor en cours d'execution
                try
                {
                    inventorApp = (Inventor.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Inventor.Application");
                }
                catch
                {
                    Log("[i] Inventor n'est pas en cours d'execution ou n'est pas accessible via COM");
                    return true; // Pas d'Inventor = succes
                }
                
                if (inventorApp == null)
                {
                    Log("[i] Inventor non detecte");
                    return true;
                }
                
                Log("[>] Connexion a Inventor etablie, fermeture propre en cours...");
                
                // Etape 1: Activer SilentOperation pour eviter les dialogues
                bool originalSilentOperation = inventorApp.SilentOperation;
                inventorApp.SilentOperation = true;
                Log("[+] SilentOperation active");
                
                try
                {
                    // Etape 2: Sauvegarder tous les documents ouverts
                    var documents = inventorApp.Documents;
                    int docCount = documents.Count;
                    
                    if (docCount > 0)
                    {
                        Log($"[>] {docCount} document(s) ouvert(s), sauvegarde en cours...");
                        
                        // Sauvegarder chaque document individuellement
                        for (int i = docCount; i >= 1; i--)
                        {
                            try
                            {
                                var doc = documents[i];
                                if (doc.Dirty) // Document modifie
                                {
                                    string docName = System.IO.Path.GetFileName(doc.FullFileName);
                                    doc.Save();
                                    Log($"    [+] Sauvegarde: {docName}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log($"    [!] Erreur sauvegarde doc {i}: {ex.Message}", LogLevel.WARNING);
                            }
                        }
                        
                        await Task.Delay(500, cancellationToken);
                        
                        // Etape 3: Fermer tous les documents
                        Log("[>] Fermeture de tous les documents...");
                        
                        // Fermer chaque document individuellement (plus fiable que CloseAll)
                        for (int i = documents.Count; i >= 1; i--)
                        {
                            try
                            {
                                var doc = documents[i];
                                string docName = System.IO.Path.GetFileName(doc.FullFileName);
                                doc.Close(true); // true = skip save prompt (deja sauvegarde)
                                Log($"    [+] Ferme: {docName}");
                            }
                            catch (Exception ex)
                            {
                                Log($"    [!] Erreur fermeture doc {i}: {ex.Message}", LogLevel.WARNING);
                            }
                        }
                        
                        await Task.Delay(500, cancellationToken);
                        
                        // Verifier qu'il ne reste plus de documents
                        if (documents.Count > 0)
                        {
                            Log($"[!] {documents.Count} document(s) restant(s), tentative CloseAll...", LogLevel.WARNING);
                            documents.CloseAll(true); // Force close all
                            await Task.Delay(500, cancellationToken);
                        }
                        
                        Log("[+] Tous les documents fermes");
                    }
                    else
                    {
                        Log("[i] Aucun document ouvert");
                    }
                    
                    // Etape 4: Switch vers le projet par defaut (evite les conflits IPJ)
                    try
                    {
                        var designProjectManager = inventorApp.DesignProjectManager;
                        string currentProject = designProjectManager.ActiveDesignProject?.FullFileName ?? "";
                        
                        // Chercher le projet par defaut
                        if (File.Exists(DEFAULT_IPJ_PATH))
                        {
                            if (!currentProject.Equals(DEFAULT_IPJ_PATH, StringComparison.OrdinalIgnoreCase))
                            {
                                // Ajouter le projet s'il n'existe pas deja dans la liste
                                try { designProjectManager.DesignProjects.AddExisting(DEFAULT_IPJ_PATH); } catch { }
                                
                                // Activer le projet Default via son chemin complet
                                foreach (dynamic proj in designProjectManager.DesignProjects)
                                {
                                    string projPath = proj.FullFileName?.ToString() ?? "";
                                    if (projPath.Equals(DEFAULT_IPJ_PATH, StringComparison.OrdinalIgnoreCase))
                                    {
                                        proj.Activate();
                                        Log("[+] Projet switch vers Default.ipj");
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Log("[i] Default.ipj non trouve, utilisation du projet courant");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"[!] Impossible de switch IPJ: {ex.Message}", LogLevel.WARNING);
                    }
                    
                    await Task.Delay(500, cancellationToken);
                    
                    // Etape 5: Fermer Inventor proprement
                    Log("[>] Fermeture d'Inventor...");
                    inventorApp.Quit();
                    
                    // Attendre la fermeture complete (max 30 secondes)
                    int waitTime = 0;
                    const int maxWaitTime = 30000;
                    const int checkInterval = 500;
                    
                    while (waitTime < maxWaitTime)
                    {
                        await Task.Delay(checkInterval, cancellationToken);
                        waitTime += checkInterval;
                        
                        var processes = Process.GetProcessesByName("Inventor");
                        if (processes.Length == 0)
                        {
                            Log("[+] Inventor ferme avec succes!");
                            success = true;
                            break;
                        }
                    }
                    
                    if (!success)
                    {
                        Log("[!] Inventor n'a pas repondu a la commande Quit dans le delai imparti", LogLevel.WARNING);
                    }
                }
                finally
                {
                    // Restaurer SilentOperation (au cas ou Inventor ne s'est pas ferme)
                    try
                    {
                        if (inventorApp != null && !success)
                        {
                            inventorApp.SilentOperation = originalSilentOperation;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log($"[!] Erreur lors de la fermeture safe d'Inventor: {ex.Message}", LogLevel.WARNING);
            }
            finally
            {
                // Liberer la reference COM
                if (inventorApp != null)
                {
                    try
                    {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(inventorApp);
                    }
                    catch { }
                }
            }
            
            return success;
        }
        
        /// <summary>
        /// Force la fermeture d'Inventor si SafeClose echoue (fallback)
        /// </summary>
        private async Task ForceCloseInventorAsync(CancellationToken cancellationToken)
        {
            Log("[!] Fermeture forcee d'Inventor...", LogLevel.WARNING);
            
            try
            {
                foreach (var proc in Process.GetProcessesByName("Inventor"))
                {
                    try
                    {
                        // Essayer d'abord CloseMainWindow (plus propre que Kill)
                        proc.CloseMainWindow();
                        bool closed = await Task.Run(() => proc.WaitForExit(5000), cancellationToken);
                        
                        if (!closed)
                        {
                            proc.Kill();
                            await Task.Run(() => proc.WaitForExit(3000), cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"[!] Erreur fermeture processus Inventor: {ex.Message}", LogLevel.WARNING);
                    }
                }
                
                await Task.Delay(2000, cancellationToken);
                Log("[+] Inventor ferme de force");
            }
            catch (Exception ex)
            {
                Log($"[-] Erreur critique fermeture Inventor: {ex.Message}", LogLevel.ERROR);
            }
        }
        
        #endregion

        #region Main Execution

        /// <summary>
        /// Execute la mise a jour complete du workspace
        /// </summary>
        /// <param name="connection">Connexion Vault active</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Resultat de la mise a jour</returns>
        public async Task<UpdateWorkspaceResult> ExecuteFullUpdateAsync(
            VDF.Vault.Currency.Connections.Connection connection,
            CancellationToken cancellationToken = default)
        {
            var result = new UpdateWorkspaceResult();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                Log("[>] Demarrage de la mise a jour du workspace...");

                // ETAPE CRITIQUE: Fermer Inventor et tous les processus lies AVANT tout
                // Ceci est OBLIGATOIRE pour que les plugins puissent etre copies
                Log("[>] Verification des processus Inventor...");
                bool inventorWasRunning = false;
                
                var inventorProcess = Process.GetProcessesByName("Inventor").FirstOrDefault();
                if (inventorProcess != null)
                {
                    inventorWasRunning = true;
                    Log("[!] Inventor detecte en cours d'execution", LogLevel.WARNING);
                    Log("[>] Fermeture SAFE d'Inventor (Save All + Close All + Switch IPJ)...");
                    
                    // Utiliser la methode SafeClose qui:
                    // 1. Active SilentOperation
                    // 2. Sauvegarde tous les documents (Save All)
                    // 3. Ferme tous les documents (Close All)
                    // 4. Switch vers Default.ipj
                    // 5. Quit proprement
                    bool safeClosed = await SafeCloseInventorAsync(cancellationToken);
                    
                    if (!safeClosed)
                    {
                        // Fallback: Fermeture forcee
                        Log("[!] SafeClose n'a pas fonctionne, tentative de fermeture forcee...", LogLevel.WARNING);
                        await ForceCloseInventorAsync(cancellationToken);
                    }
                }

                // Verifier qu'il ne reste plus de processus Inventor
                var remainingProcesses = Process.GetProcessesByName("Inventor");
                if (remainingProcesses.Length > 0)
                {
                    Log($"[!] {remainingProcesses.Length} processus Inventor restant(s), nettoyage...", LogLevel.WARNING);
                    await ForceCloseInventorAsync(cancellationToken);
                }
                
                if (inventorWasRunning)
                {
                    // Attendre que Windows libere les handles de fichiers
                    Log("[>] Attente liberation des fichiers (5s)...");
                    await Task.Delay(5000, cancellationToken);
                }

                // Etape 1: Verifier la connexion Vault
                UpdateStep(1, StepStatus.InProgress, "Verification de la connexion...");
                if (connection == null)
                {
                    UpdateStep(1, StepStatus.Failed, "Connexion Vault requise");
                    result.Success = false;
                    result.ErrorMessage = "Aucune connexion Vault active";
                    return result;
                }
                UpdateStep(1, StepStatus.Completed, "Connexion verifiee");
                Log("[+] Connexion Vault verifiee");

                cancellationToken.ThrowIfCancellationRequested();

                // Etapes 2-5: Telecharger les 4 dossiers Vault
                for (int i = 0; i < VaultFolderPaths.Length; i++)
                {
                    int stepNumber = i + 2; // Etapes 2, 3, 4, 5
                    var vaultPath = VaultFolderPaths[i];
                    var localPath = LocalFolderPaths[i];
                    var folderName = Path.GetFileName(vaultPath.TrimEnd('/'));

                    UpdateStep(stepNumber, StepStatus.InProgress, $"Telechargement {folderName}...");
                    
                    try
                    {
                        var downloadResult = await DownloadVaultFolderAsync(
                            connection, vaultPath, localPath, cancellationToken);
                        
                        if (downloadResult.Success)
                        {
                            UpdateStep(stepNumber, StepStatus.Completed, $"{downloadResult.FileCount} fichiers");
                            result.DownloadedFiles += downloadResult.FileCount;
                            Log($"[+] {folderName}: {downloadResult.FileCount} fichiers telecharges");
                        }
                        else
                        {
                            UpdateStep(stepNumber, StepStatus.Warning, downloadResult.ErrorMessage ?? "Erreur partielle");
                            Log($"[!] {folderName}: {downloadResult.ErrorMessage}", LogLevel.WARNING);
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdateStep(stepNumber, StepStatus.Failed, ex.Message);
                        Log($"[-] Erreur telechargement {folderName}: {ex.Message}", LogLevel.ERROR);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }

                // Etapes 6-7: Copier les plugins (necessite droits admin)
                bool isAdmin = new System.Security.Principal.WindowsPrincipal(
                    System.Security.Principal.WindowsIdentity.GetCurrent())
                    .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

                if (!isAdmin)
                {
                    Log("[!] L'application n'a pas les droits administrateur", LogLevel.WARNING);
                    Log("[!] La copie des plugins et l'installation peuvent echouer", LogLevel.WARNING);
                    Log("[i] Relancez l'application en tant qu'administrateur pour cette fonctionnalite");
                }

                for (int i = 0; i < PluginCopyPaths.Length; i++)
                {
                    int stepNumber = i + 8; // Etapes 8, 9 (apres les 6 dossiers Vault + connexion)
                    var (sourceSubPath, destFolder) = PluginCopyPaths[i];

                    UpdateStep(stepNumber, StepStatus.InProgress, $"Copie {destFolder}...");
                    
                    try
                    {
                        var copyResult = await CopyPluginFolderAsync(
                            sourceSubPath, destFolder, cancellationToken);
                        
                        if (copyResult.Success)
                        {
                            UpdateStep(stepNumber, StepStatus.Completed, $"{copyResult.FileCount} fichiers");
                            result.CopiedPluginFiles += copyResult.FileCount;
                            Log($"[+] {destFolder}: {copyResult.FileCount} fichiers copies");
                        }
                        else
                        {
                            UpdateStep(stepNumber, StepStatus.Warning, copyResult.ErrorMessage ?? "Erreur");
                            Log($"[!] {destFolder}: {copyResult.ErrorMessage}", LogLevel.WARNING);
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdateStep(stepNumber, StepStatus.Failed, ex.Message);
                        Log($"[-] Erreur copie {destFolder}: {ex.Message}", LogLevel.ERROR);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }

                // Etape 10: Installations silencieuses des applications
                UpdateStep(10, StepStatus.InProgress, "Installation des applications...");
                try
                {
                    var installResult = await RunSilentInstallersAsync(cancellationToken);
                    
                    if (installResult.Success)
                    {
                        UpdateStep(10, StepStatus.Completed, $"{installResult.SuccessCount}/{installResult.TotalCount} applications");
                        result.InstalledTools = installResult.SuccessCount;
                        Log($"[+] {installResult.SuccessCount}/{installResult.TotalCount} applications installees avec succes");
                    }
                    else
                    {
                        UpdateStep(10, StepStatus.Warning, installResult.ErrorMessage ?? "Erreurs d'installation");
                        Log($"[!] Installation: {installResult.ErrorMessage}", LogLevel.WARNING);
                    }
                }
                catch (Exception ex)
                {
                    UpdateStep(10, StepStatus.Failed, ex.Message);
                    Log($"[-] Erreur installations: {ex.Message}", LogLevel.ERROR);
                }

                // Etape 11: Finalisation
                UpdateStep(11, StepStatus.InProgress, "Finalisation...");
                await Task.Delay(500, cancellationToken); // Court delai pour s'assurer que tout est termine
                UpdateStep(11, StepStatus.Completed, "Mise a jour terminee");

                stopwatch.Stop();
                result.Success = true;
                result.Duration = stopwatch.Elapsed;
                Log($"[+] Mise a jour terminee en {stopwatch.Elapsed.TotalSeconds:F1}s");
                
                ReportProgress(100, "Mise a jour terminee!");
            }
            catch (OperationCanceledException)
            {
                Log("[!] Mise a jour annulee par l'utilisateur", LogLevel.WARNING);
                result.Success = false;
                result.ErrorMessage = "Annule par l'utilisateur";
            }
            catch (Exception ex)
            {
                Log($"[-] Erreur critique: {ex.Message}", LogLevel.ERROR);
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        #endregion

        #region Vault Download

        /// <summary>
        /// Telecharge recursivement un dossier Vault vers un chemin local (MODE MIROIR)
        /// Supprime les fichiers locaux qui n'existent plus dans Vault
        /// </summary>
        private async Task<DownloadResult> DownloadVaultFolderAsync(
            VDF.Vault.Currency.Connections.Connection connection,
            string vaultFolderPath,
            string localFolderPath,
            CancellationToken cancellationToken)
        {
            var result = new DownloadResult();

            return await Task.Run(() =>
            {
                try
                {
                    Log($"   [>] Telechargement: {vaultFolderPath}");

                    // Obtenir le dossier Vault
                    var folder = connection.WebServiceManager.DocumentService.GetFolderByPath(vaultFolderPath);
                    if (folder == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Dossier non trouve: {vaultFolderPath}";
                        return result;
                    }

                    // Collecter tous les fichiers recursivement
                    var allFiles = new List<ACW.File>();
                    var allFolders = new List<ACW.Folder>();
                    GetAllFilesRecursive(connection, folder, allFiles, allFolders);

                    Log($"   [i] {allFiles.Count} fichiers dans {allFolders.Count} dossiers");

                    if (allFiles.Count == 0)
                    {
                        result.Success = true;
                        result.FileCount = 0;
                        return result;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    // Creer les dossiers locaux
                    EnsureLocalFolderStructure(connection, vaultFolderPath, localFolderPath, allFolders);

                    // Telecharger les fichiers par batch
                    int batchSize = 50;
                    int downloadedCount = 0;
                    int totalFiles = allFiles.Count;

                    for (int i = 0; i < totalFiles; i += batchSize)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var batch = allFiles.Skip(i).Take(batchSize).ToList();
                        
                        try
                        {
                            var downloadSettings = new VDF.Vault.Settings.AcquireFilesSettings(connection, false);
                            
                            // Options: Download = telecharger sans checkout
                            foreach (var file in batch)
                            {
                                var fileIteration = new VDF.Vault.Currency.Entities.FileIteration(connection, file);
                                downloadSettings.AddFileToAcquire(fileIteration, 
                                    VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download);
                            }

                            var downloadResult = connection.FileManager.AcquireFiles(downloadSettings);
                            
                            if (downloadResult?.FileResults != null)
                            {
                                foreach (var fileResult in downloadResult.FileResults)
                                {
                                    if (fileResult.Status == VDF.Vault.Results.FileAcquisitionResult.AcquisitionStatus.Success)
                                    {
                                        downloadedCount++;
                                        
                                        // CRITIQUE: Retirer l'attribut ReadOnly apres telechargement
                                        // Les fichiers Vault sont TOUJOURS en ReadOnly
                                        try
                                        {
                                            if (fileResult.LocalPath != null)
                                            {
                                                VaultFileHelper.RemoveReadOnly(fileResult.LocalPath.FullPath);
                                            }
                                        }
                                        catch { /* Ignorer les erreurs individuelles */ }
                                    }
                                }
                            }

                            int progressPercent = (int)((i + batch.Count) * 100.0 / totalFiles);
                            ReportProgress(progressPercent, $"Telechargement {downloadedCount}/{totalFiles}...", 
                                batch.FirstOrDefault()?.Name);
                        }
                        catch (Exception batchEx)
                        {
                            Log($"   [!] Erreur batch: {batchEx.Message}", LogLevel.WARNING);
                        }
                    }

                    // MODE MIROIR COMPLET: Supprimer fichiers ET dossiers locaux qui n'existent plus dans Vault
                    try
                    {
                        // Construire les chemins relatifs complets de tous les fichiers Vault
                        var vaultRelativeFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var file in allFiles)
                        {
                            // Trouver le dossier parent du fichier
                            var parentFolder = allFolders.FirstOrDefault(f => f.Id == file.FolderId);
                            if (parentFolder != null)
                            {
                                var vaultFilePath = parentFolder.FullName.TrimEnd('/') + "/" + file.Name;
                                var relativePath = vaultFilePath.Substring(vaultFolderPath.TrimEnd('/').Length).TrimStart('/');
                                relativePath = relativePath.Replace("/", "\\");
                                vaultRelativeFilePaths.Add(relativePath);
                            }
                        }

                        // Construire les chemins relatifs complets de tous les dossiers Vault
                        var vaultRelativeFolderPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var vaultFolder in allFolders)
                        {
                            var relativePath = vaultFolder.FullName.TrimEnd('/').Substring(vaultFolderPath.TrimEnd('/').Length).TrimStart('/');
                            if (!string.IsNullOrEmpty(relativePath))
                            {
                                relativePath = relativePath.Replace("/", "\\");
                                vaultRelativeFolderPaths.Add(relativePath);
                            }
                        }

                        int deletedCount = CleanupLocalMirrorRecursive(localFolderPath, localFolderPath, vaultRelativeFilePaths, vaultRelativeFolderPaths);
                        if (deletedCount > 0)
                        {
                            Log($"   [i] Mode miroir: {deletedCount} fichier(s)/dossier(s) obsolete(s) supprime(s)");
                        }
                    }
                    catch (Exception mirrorEx)
                    {
                        Log($"   [!] Erreur mode miroir: {mirrorEx.Message}", LogLevel.WARNING);
                    }

                    result.Success = true;
                    result.FileCount = downloadedCount;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                }

                return result;
            }, cancellationToken);
        }

        /// <summary>
        /// Obtient tous les fichiers recursivement depuis un dossier Vault
        /// </summary>
        private void GetAllFilesRecursive(
            VDF.Vault.Currency.Connections.Connection connection, 
            ACW.Folder folder, 
            List<ACW.File> allFiles, 
            List<ACW.Folder> allFolders)
        {
            try
            {
                allFolders.Add(folder);

                // Obtenir les fichiers de ce dossier
                var files = connection.WebServiceManager.DocumentService.GetLatestFilesByFolderId(folder.Id, false);
                if (files != null && files.Length > 0)
                {
                    allFiles.AddRange(files);
                }

                // Obtenir les sous-dossiers
                var subFolders = connection.WebServiceManager.DocumentService.GetFoldersByParentId(folder.Id, false);
                if (subFolders != null && subFolders.Length > 0)
                {
                    foreach (var subFolder in subFolders)
                    {
                        GetAllFilesRecursive(connection, subFolder, allFiles, allFolders);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"   [!] Erreur enumeration {folder.FullName}: {ex.Message}", LogLevel.WARNING);
            }
        }

        /// <summary>
        /// Cree la structure de dossiers locaux correspondant a Vault
        /// </summary>
        private void EnsureLocalFolderStructure(
            VDF.Vault.Currency.Connections.Connection connection,
            string vaultRootPath,
            string localRootPath,
            List<ACW.Folder> folders)
        {
            try
            {
                // Creer le dossier racine s'il n'existe pas
                if (!Directory.Exists(localRootPath))
                {
                    Directory.CreateDirectory(localRootPath);
                }

                // Creer chaque sous-dossier
                foreach (var folder in folders)
                {
                    // Calculer le chemin relatif depuis la racine Vault
                    var vaultFullPath = folder.FullName; // ex: $/Engineering/Library/Cabinet/SubFolder
                    
                    // Normaliser les chemins
                    vaultRootPath = vaultRootPath.TrimEnd('/');
                    var relativePath = vaultFullPath.Substring(vaultRootPath.Length).TrimStart('/');
                    
                    if (!string.IsNullOrEmpty(relativePath))
                    {
                        // Convertir separateurs
                        relativePath = relativePath.Replace("/", "\\");
                        var localPath = Path.Combine(localRootPath, relativePath);
                        
                        if (!Directory.Exists(localPath))
                        {
                            Directory.CreateDirectory(localPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"   [!] Erreur creation dossiers: {ex.Message}", LogLevel.WARNING);
            }
        }

        /// <summary>
        /// MODE MIROIR COMPLET: Supprime recursivement les fichiers/dossiers locaux qui n'existent plus dans Vault
        /// Utilise les chemins relatifs complets pour une comparaison precise
        /// </summary>
        private int CleanupLocalMirrorRecursive(
            string localRootPath,
            string currentLocalPath, 
            HashSet<string> vaultRelativeFilePaths, 
            HashSet<string> vaultRelativeFolderPaths)
        {
            int deletedCount = 0;

            if (!Directory.Exists(currentLocalPath))
                return 0;

            try
            {
                // 1. Supprimer les FICHIERS locaux qui n'existent plus dans Vault
                foreach (var localFile in Directory.GetFiles(currentLocalPath))
                {
                    var fileName = Path.GetFileName(localFile);
                    
                    // Ignorer les fichiers systeme/temporaires
                    if (fileName.StartsWith(".") || fileName.StartsWith("~$") || fileName.EndsWith(".lck"))
                        continue;

                    // Calculer le chemin relatif
                    var relativePath = localFile.Substring(localRootPath.Length).TrimStart('\\');
                    
                    // Si le fichier n'existe pas dans Vault, le supprimer
                    if (!vaultRelativeFilePaths.Contains(relativePath))
                    {
                        try
                        {
                            VaultFileHelper.DeleteFile(localFile);
                            deletedCount++;
                            Log($"      [>] Fichier supprime (obsolete): {relativePath}");
                        }
                        catch (Exception delEx)
                        {
                            Log($"      [!] Impossible de supprimer {relativePath}: {delEx.Message}", LogLevel.WARNING);
                        }
                    }
                }

                // 2. Traiter les SOUS-DOSSIERS recursivement
                foreach (var localDir in Directory.GetDirectories(currentLocalPath))
                {
                    var dirName = Path.GetFileName(localDir);
                    
                    // Ignorer les dossiers systeme
                    if (dirName.StartsWith(".") || dirName.Equals("OldVersions", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Calculer le chemin relatif du dossier
                    var relativeDirPath = localDir.Substring(localRootPath.Length).TrimStart('\\');
                    
                    // Si le dossier n'existe pas dans Vault, le supprimer completement
                    if (!vaultRelativeFolderPaths.Contains(relativeDirPath))
                    {
                        try
                        {
                            // Supprimer le dossier et tout son contenu
                            ForceDeleteDirectoryRecursive(localDir);
                            deletedCount++;
                            Log($"      [>] Dossier supprime (obsolete): {relativeDirPath}");
                        }
                        catch (Exception delEx)
                        {
                            Log($"      [!] Impossible de supprimer dossier {relativeDirPath}: {delEx.Message}", LogLevel.WARNING);
                        }
                    }
                    else
                    {
                        // Le dossier existe dans Vault, descendre recursivement pour nettoyer son contenu
                        deletedCount += CleanupLocalMirrorRecursive(localRootPath, localDir, vaultRelativeFilePaths, vaultRelativeFolderPaths);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"   [!] Erreur nettoyage miroir: {ex.Message}", LogLevel.WARNING);
            }

            return deletedCount;
        }

        /// <summary>
        /// Supprime un dossier et tout son contenu de facon forcee
        /// </summary>
        private void ForceDeleteDirectoryRecursive(string path)
        {
            if (!Directory.Exists(path))
                return;

            // Enlever tous les attributs ReadOnly recursivement
            try
            {
                foreach (var file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                {
                    try { File.SetAttributes(file, FileAttributes.Normal); } catch { }
                }
            }
            catch { }

            // Supprimer le dossier
            try
            {
                Directory.Delete(path, true);
            }
            catch
            {
                // Tenter une suppression fichier par fichier si ca echoue
                try
                {
                    foreach (var file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                    {
                        try { VaultFileHelper.DeleteFile(file); } catch { }
                    }
                    foreach (var dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
                    {
                        try { Directory.Delete(dir); } catch { }
                    }
                    Directory.Delete(path);
                }
                catch { }
            }
        }

        #endregion

        #region Plugin Copy

        /// <summary>
        /// Ferme Inventor UNIQUEMENT pour liberer les DLLs des plugins
        /// NE TOUCHE PAS aux processus de licence (AdskLicensingAgent, AdskIdentityManager)
        /// </summary>
        private void CloseInventorOnly()
        {
            // SEULEMENT Inventor - les processus de licence doivent rester actifs
            var processesToClose = new[] { "Inventor" };

            foreach (var processName in processesToClose)
            {
                try
                {
                    var processes = Process.GetProcessesByName(processName);
                    foreach (var proc in processes)
                    {
                        try
                        {
                            Log($"   [>] Fermeture de: {proc.ProcessName} (PID: {proc.Id})");
                            proc.Kill();
                            proc.WaitForExit(5000);
                        }
                        catch { /* Ignorer si deja ferme */ }
                    }
                }
                catch { /* Ignorer */ }
            }
        }

        /// <summary>
        /// Copie un dossier avec Robocopy (methode robuste Windows)
        /// </summary>
        private (bool Success, int FilesCopied, string Output) CopyWithRobocopy(string source, string dest)
        {
            try
            {
                // Robocopy /MIR = Mirror (copie + supprime fichiers obsoletes)
                // /R:2 = 2 retries, /W:1 = 1 seconde entre retries
                // /NP = No Progress, /NFL /NDL = No File/Dir List pour moins de verbosity
                // /XD = exclure dossiers, /XF = exclure fichiers
                var excludeDirs = string.Join(" ", ExcludedFolders.Select(f => $"\"{f}\""));
                var excludeFilesByExt = string.Join(" ", ExcludedExtensions.Select(e => $"\"*{e}\""));
                var excludeFilesByPrefix = string.Join(" ", ExcludedFilePrefixes.Select(p => $"\"{p}*\""));
                
                var args = $"\"{source}\" \"{dest}\" /MIR /R:2 /W:1 /NP /NFL /NDL /XD {excludeDirs} /XF {excludeFilesByExt} {excludeFilesByPrefix}";
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "robocopy",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(60000); // 60 sec timeout

                    // Robocopy exit codes: 0-7 = succes, 8+ = erreur
                    bool success = process.ExitCode < 8;
                    
                    // Compter les fichiers copies depuis la sortie
                    int filesCopied = 0;
                    var match = System.Text.RegularExpressions.Regex.Match(output, @"Files\s*:\s*(\d+)");
                    if (match.Success)
                    {
                        int.TryParse(match.Groups[1].Value, out filesCopied);
                    }

                    return (success, filesCopied, output);
                }
            }
            catch (Exception ex)
            {
                return (false, 0, ex.Message);
            }
        }

        /// <summary>
        /// Copie un dossier plugin vers ApplicationPlugins avec Robocopy
        /// EXIGENCE: Ferme Inventor AVANT la copie pour liberer les DLLs
        /// </summary>
        private async Task<CopyResult> CopyPluginFolderAsync(
            string sourceSubPath,
            string destFolderName,
            CancellationToken cancellationToken)
        {
            var result = new CopyResult();

            return await Task.Run(async () =>
            {
                try
                {
                    // Construire les chemins complets
                    string sourcePath = Path.Combine(
                        @"C:\Vault\Engineering\Inventor_Standards",
                        sourceSubPath);
                    
                    string destPath = Path.Combine(APPLICATION_PLUGINS_PATH, destFolderName);

                    Log($"   [>] Copie avec Robocopy: {sourceSubPath} -> {destFolderName}");

                    if (!Directory.Exists(sourcePath))
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Source non trouvee: {sourcePath}";
                        return result;
                    }

                    // Creer le dossier ApplicationPlugins si necessaire
                    if (!Directory.Exists(APPLICATION_PLUGINS_PATH))
                    {
                        Directory.CreateDirectory(APPLICATION_PLUGINS_PATH);
                    }

                    // ETAPE UNIQUE: Fermer Inventor puis Robocopy
                    CloseInventorOnly();
                    await Task.Delay(2000); // Attendre que les handles soient liberes
                    
                    var (success, filesCopied, output) = CopyWithRobocopy(sourcePath, destPath);
                    
                    if (success)
                    {
                        result.Success = true;
                        result.FileCount = filesCopied;
                        Log($"   [+] Robocopy reussi: {filesCopied} fichiers");
                    }
                    else
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Robocopy echec: {output}";
                        Log($"   [-] Robocopy echec: {output}", LogLevel.ERROR);
                    }
                    
                    return result;
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Acces refuse: {uaEx.Message}";
                    Log($"   [-] Acces refuse lors de la copie. Relancer en administrateur.", LogLevel.ERROR);
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                }

                return result;
            }, cancellationToken);
        }

        #endregion

        #region Silent Installers

        /// <summary>
        /// Execute les installateurs silencieux - SCALABLE
        /// Scanne automatiquement le dossier Xnrgy_Software pour tous les .exe
        /// </summary>
        private async Task<InstallResult> RunSilentInstallersAsync(CancellationToken cancellationToken)
        {
            var result = new InstallResult { Success = true };
            int successCount = 0;
            var errors = new List<string>();

            // Enlever les attributs ReadOnly des fichiers existants dans ApplicationPlugins
            // Car Vault les telecharge souvent en ReadOnly
            try
            {
                var pluginPaths = new[]
                {
                    @"C:\ProgramData\Autodesk\ApplicationPlugins\XNRGY_ADDINS_2026",
                    @"C:\ProgramData\Autodesk\ApplicationPlugins\SIBL_XNRGY_ADDINS_2026"
                };
                
                foreach (var pluginPath in pluginPaths)
                {
                    if (Directory.Exists(pluginPath))
                    {
                        foreach (var file in Directory.GetFiles(pluginPath, "*.*", SearchOption.AllDirectories))
                        {
                            try
                            {
                                var fi = new FileInfo(file);
                                if (fi.IsReadOnly)
                                {
                                    fi.IsReadOnly = false;
                                }
                            }
                            catch { /* Ignorer les erreurs individuelles */ }
                        }
                    }
                }
                Log("   [i] Attributs ReadOnly enleves des plugins existants");
            }
            catch (Exception ex)
            {
                Log($"   [!] Impossible d'enlever ReadOnly: {ex.Message}", LogLevel.WARNING);
            }

            // Scanner dynamiquement le dossier Xnrgy_Software pour tous les .exe
            var installers = new List<string>();
            
            if (Directory.Exists(INSTALLERS_FOLDER))
            {
                // 1. Chercher les .exe directement a la racine de Xnrgy_Software
                var rootExeFiles = Directory.GetFiles(INSTALLERS_FOLDER, "*.exe", SearchOption.TopDirectoryOnly)
                    .Where(f => !Path.GetFileName(f).StartsWith("unins", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                installers.AddRange(rootExeFiles);
                
                // 2. Chercher les .exe dans les sous-dossiers
                foreach (var subDir in Directory.GetDirectories(INSTALLERS_FOLDER))
                {
                    var exeFiles = Directory.GetFiles(subDir, "*.exe", SearchOption.TopDirectoryOnly)
                        .Where(f => !Path.GetFileName(f).StartsWith("unins", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    installers.AddRange(exeFiles);
                }
                
                Log($"   [i] {installers.Count} installateur(s) detecte(s) dans Xnrgy_Software");
            }
            else
            {
                Log($"   [!] Dossier introuvable: {INSTALLERS_FOLDER}", LogLevel.WARNING);
            }

            // Executer chaque installateur
            foreach (var fullPath in installers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var installerName = Path.GetFileName(fullPath);
                Log($"   [>] Installation: {installerName}");

                try
                {
                    ReportProgress(0, $"Installation de {installerName}...", installerName);

                    // Verifier si on est deja admin
                    bool isAdmin = new System.Security.Principal.WindowsPrincipal(
                        System.Security.Principal.WindowsIdentity.GetCurrent())
                        .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = fullPath,
                        Arguments = SILENT_INSTALL_ARGS,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    };

                    if (isAdmin)
                    {
                        // Deja admin - lancer directement sans shell
                        startInfo.UseShellExecute = false;
                        startInfo.RedirectStandardOutput = true;
                        startInfo.RedirectStandardError = true;
                    }
                    else
                    {
                        // Pas admin - demander elevation via shell
                        startInfo.UseShellExecute = true;
                        startInfo.Verb = "runas";
                    }

                    var process = new Process
                    {
                        StartInfo = startInfo,
                        EnableRaisingEvents = true
                    };

                    try
                    {
                        process.Start();
                    }
                    catch (System.ComponentModel.Win32Exception w32ex) when (w32ex.NativeErrorCode == 1223)
                    {
                        // L'utilisateur a annule la demande UAC
                        Log($"   [!] Installation annulee par l'utilisateur: {installerName}", LogLevel.WARNING);
                        errors.Add($"Annule: {installerName}");
                        continue;
                    }

                    // Attendre avec timeout de 2 minutes
                    bool exited = await Task.Run(() => process.WaitForExit(120000), cancellationToken);

                    if (!exited)
                    {
                        process.Kill();
                        Log($"   [!] Timeout pour {installerName}", LogLevel.WARNING);
                        errors.Add($"Timeout: {installerName}");
                    }
                    else if (process.ExitCode == 0 || process.ExitCode == 3010) // 3010 = reboot required
                    {
                        successCount++;
                        Log($"   [+] {installerName} installe (code: {process.ExitCode})");
                    }
                    else if (process.ExitCode == 5)
                    {
                        // Code 5 = Acces refuse - fichier verrouille ou droits insuffisants
                        Log($"   [!] Acces refuse pour {installerName} - fermer Inventor et reessayer", LogLevel.WARNING);
                        errors.Add($"Acces refuse: {installerName}");
                    }
                    else if (process.ExitCode == 1602 || process.ExitCode == 1603)
                    {
                        // Erreurs MSI courantes
                        Log($"   [!] Installation annulee ou echouee: {installerName}", LogLevel.WARNING);
                        errors.Add($"Echec MSI: {installerName}");
                    }
                    else
                    {
                        Log($"   [!] {installerName} termine avec code: {process.ExitCode}", LogLevel.WARNING);
                        errors.Add($"Code {process.ExitCode}: {installerName}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"   [-] Erreur installation {installerName}: {ex.Message}", LogLevel.ERROR);
                    errors.Add($"Erreur: {installerName}");
                }

                // Court delai entre installations
                await Task.Delay(1000, cancellationToken);
            }

            result.SuccessCount = successCount;
            result.TotalCount = installers.Count;
            result.Success = errors.Count == 0;
            if (errors.Count > 0)
            {
                result.ErrorMessage = string.Join("; ", errors);
            }

            return result;
        }

        #endregion

        #region Result Classes

        public class UpdateWorkspaceResult
        {
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
            public int DownloadedFiles { get; set; }
            public int CopiedPluginFiles { get; set; }
            public int InstalledTools { get; set; }
            public TimeSpan Duration { get; set; }
        }

        private class DownloadResult
        {
            public bool Success { get; set; }
            public int FileCount { get; set; }
            public string? ErrorMessage { get; set; }
        }

        private class CopyResult
        {
            public bool Success { get; set; }
            public int FileCount { get; set; }
            public string? ErrorMessage { get; set; }
        }

        private class InstallResult
        {
            public bool Success { get; set; }
            public int SuccessCount { get; set; }
            public int TotalCount { get; set; }
            public string? ErrorMessage { get; set; }
        }

        #endregion

        #region Event Args

        public enum LogLevel { INFO, WARNING, ERROR, SUCCESS }
        public enum StepStatus { Pending, InProgress, Completed, Failed, Warning, Skipped }

        public class UpdateProgressEventArgs : EventArgs
        {
            public int Percent { get; }
            public string Status { get; }
            public string? CurrentFile { get; }

            public UpdateProgressEventArgs(int percent, string status, string? currentFile = null)
            {
                Percent = percent;
                Status = status;
                CurrentFile = currentFile;
            }
        }

        public class UpdateLogEventArgs : EventArgs
        {
            public string Message { get; }
            public LogLevel Level { get; }

            public UpdateLogEventArgs(string message, LogLevel level)
            {
                Message = message;
                Level = level;
            }
        }

        public class UpdateStepEventArgs : EventArgs
        {
            public int StepNumber { get; }
            public StepStatus Status { get; }
            public string? Message { get; }

            public UpdateStepEventArgs(int stepNumber, StepStatus status, string? message = null)
            {
                StepNumber = stepNumber;
                Status = status;
                Message = message;
            }
        }

        #endregion
    }
}
