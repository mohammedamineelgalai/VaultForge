using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace XnrgyEngineeringAutomationTools.Services
{
    /// <summary>
    /// Service Firebase pour l'application XEAT
    /// - Envoie les erreurs/bugs a Firebase Audit Logs
    /// - Enregistre les sessions utilisateur
    /// - Met a jour le heartbeat device
    /// PERFORMANCE: Seules les erreurs sont envoyees, pas les logs INFO/DEBUG
    /// </summary>
    public class FirebaseAuditService
    {
        #region Constants

        // URL de la Firebase Realtime Database - OBFUSQUEE via FirebaseConfigService
        private static readonly string FIREBASE_DATABASE_URL = FirebaseConfigService.GetDatabaseUrl();
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        #endregion

        #region Private Fields

        private static FirebaseAuditService _instance;
        private static readonly object _lock = new object();

        private readonly string _deviceId;
        private readonly string _machineName;
        private readonly string _userName;
        private readonly string _appVersion;
        private bool _isInitialized;

        // Queue pour batch les erreurs (eviter trop de requetes)
        private readonly Queue<AuditLogEntry> _errorQueue = new Queue<AuditLogEntry>();
        private readonly object _queueLock = new object();
        private DateTime _lastFlush = DateTime.MinValue;
        private const int FLUSH_INTERVAL_SECONDS = 30;
        private const int MAX_QUEUE_SIZE = 10;

        #endregion

        #region Singleton

        public static FirebaseAuditService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new FirebaseAuditService();
                        }
                    }
                }
                return _instance;
            }
        }

        private FirebaseAuditService()
        {
            _machineName = Environment.MachineName;
            _userName = Environment.UserName;
            _deviceId = $"{_machineName}_{_userName}".Replace(".", "_").Replace(" ", "_");
            _appVersion = "1.0.0"; // TODO: Lire depuis assembly
            _isInitialized = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialise le service et enregistre le demarrage de session
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                await RegisterSessionStartAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[!] Firebase init warning: {ex.Message}");
                // Continue meme si Firebase echoue
            }
        }

        /// <summary>
        /// Enregistre une erreur dans Firebase Audit Logs
        /// ASYNC - N'attend pas la reponse pour ne pas bloquer l'UI
        /// </summary>
        public void LogError(string errorType, string message, string stackTrace = null, string context = null)
        {
            var entry = new AuditLogEntry
            {
                Action = "error_reported",
                Category = "error",
                ErrorType = errorType,
                Message = message,
                StackTrace = stackTrace,
                Context = context,
                Timestamp = DateTime.UtcNow
            };

            lock (_queueLock)
            {
                _errorQueue.Enqueue(entry);

                // Flush si queue pleine ou intervalle depasse
                if (_errorQueue.Count >= MAX_QUEUE_SIZE ||
                    (DateTime.Now - _lastFlush).TotalSeconds >= FLUSH_INTERVAL_SECONDS)
                {
                    _ = FlushErrorQueueAsync();
                }
            }
        }

        /// <summary>
        /// Enregistre une exception dans Firebase
        /// </summary>
        public void LogException(Exception ex, string context = null)
        {
            LogError(
                errorType: ex.GetType().Name,
                message: ex.Message,
                stackTrace: ex.StackTrace,
                context: context
            );
        }

        /// <summary>
        /// Enregistre une action utilisateur (module utilise, etc.)
        /// </summary>
        public async Task LogUserActionAsync(string action, string details = null)
        {
            try
            {
                var logId = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                var timestamp = DateTime.UtcNow.ToString("o");

                var auditEntry = new Dictionary<string, object>
                {
                    ["id"] = logId,
                    ["action"] = action,
                    ["category"] = "user_action",
                    ["userId"] = _userName,
                    ["userName"] = _userName,
                    ["deviceId"] = _deviceId,
                    ["timestamp"] = timestamp,
                    ["details"] = details ?? "none",
                    ["success"] = true,
                    ["ipAddress"] = "local",
                    ["oldValue"] = "none",
                    ["newValue"] = "none",
                    ["errorMessage"] = "none"
                };

                await PutFirebaseAsync($"auditLog/{logId}", auditEntry);
            }
            catch
            {
                // Silencieux - ne pas bloquer l'app
            }
        }

        /// <summary>
        /// Enregistre l'utilisation d'un module XEAT dans les statistiques Firebase
        /// ET met a jour le module actuel du device pour affichage en temps reel dans la console HTML
        /// </summary>
        /// <param name="moduleId">ID du module (vaultUpload, copyDesign, smartTools, checklistHVAC, dxfVerifier, etc.)</param>
        /// <param name="actionDetails">Details optionnels de l'action</param>
        public async Task TrackModuleUsageAsync(string moduleId, string actionDetails = null)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("o");

                // 1. IMPORTANT: Mettre a jour le module actuel sur le device (visible dans console HTML)
                await PatchFirebaseAsync($"devices/{_deviceId}/status", new Dictionary<string, object>
                {
                    ["currentModule"] = moduleId,
                    ["currentModuleStarted"] = timestamp,
                    ["lastActivity"] = timestamp
                });

                // 2. Incrementer le compteur d'utilisation du module
                await IncrementStatAsync($"statistics/byModule/{moduleId}/usageCount");

                // 3. Mettre a jour la date de derniere utilisation
                await PatchFirebaseAsync($"statistics/byModule/{moduleId}", new Dictionary<string, object>
                {
                    ["lastUsed"] = timestamp
                });

                // 4. Incrementer les stats du device pour ce module
                await IncrementStatAsync($"devices/{_deviceId}/moduleStats/{moduleId}/usageCount");
                await PatchFirebaseAsync($"devices/{_deviceId}/moduleStats/{moduleId}", new Dictionary<string, object>
                {
                    ["lastUsed"] = timestamp
                });

                // 5. Enregistrer dans moduleUsage collection (historique)
                var usageId = $"{_deviceId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                await PutFirebaseAsync($"moduleUsage/{usageId}", new Dictionary<string, object>
                {
                    ["deviceId"] = _deviceId,
                    ["moduleId"] = moduleId,
                    ["userId"] = _userName,
                    ["action"] = actionDetails ?? "opened",
                    ["timestamp"] = timestamp
                });

                // 6. Log l'action dans audit (optionnel - seulement si actionDetails fourni)
                if (!string.IsNullOrEmpty(actionDetails))
                {
                    await LogUserActionAsync($"module_{moduleId}_used", actionDetails);
                }

                Logger.Log($"[+] Firebase: Module {moduleId} usage tracked", Logger.LogLevel.DEBUG);
            }
            catch (Exception ex)
            {
                Logger.Log($"[!] Firebase track module warning: {ex.Message}", Logger.LogLevel.DEBUG);
                // Silencieux - ne pas bloquer l'app
            }
        }

        /// <summary>
        /// Efface le module actuel quand l'utilisateur ferme un module
        /// </summary>
        public async Task ClearCurrentModuleAsync()
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("o");
                await PatchFirebaseAsync($"devices/{_deviceId}/status", new Dictionary<string, object>
                {
                    ["currentModule"] = null,
                    ["currentModuleStarted"] = null,
                    ["lastActivity"] = timestamp
                });
            }
            catch { }
        }

        /// <summary>
        /// Enregistre une erreur critique dans Firebase pour monitoring
        /// Visible dans la console HTML section erreurs
        /// </summary>
        public async Task LogCriticalErrorAsync(string moduleId, string errorType, string message, string stackTrace = null)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("o");
                var errorId = $"err_{_deviceId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}";

                // Enregistrer dans criticalErrors collection
                await PutFirebaseAsync($"criticalErrors/{errorId}", new Dictionary<string, object>
                {
                    ["deviceId"] = _deviceId,
                    ["moduleId"] = moduleId,
                    ["userId"] = _userName,
                    ["machineName"] = _machineName,
                    ["errorType"] = errorType,
                    ["message"] = message,
                    ["stackTrace"] = stackTrace ?? "",
                    ["timestamp"] = timestamp,
                    ["severity"] = "critical",
                    ["resolved"] = false,
                    ["appVersion"] = _appVersion
                });

                // Incrementer le compteur d'erreurs du device
                await IncrementStatAsync($"devices/{_deviceId}/errorStats/totalErrors");
                await IncrementStatAsync($"devices/{_deviceId}/errorStats/byModule/{moduleId}");

                // Incrementer stats globales
                await IncrementStatAsync($"statistics/errors/total");
                await IncrementStatAsync($"statistics/errors/byModule/{moduleId}");

                Logger.Log($"[!] Firebase: Critical error logged for {moduleId}: {errorType}", Logger.LogLevel.ERROR);
            }
            catch (Exception ex)
            {
                Logger.Log($"[!] Firebase log error failed: {ex.Message}", Logger.LogLevel.DEBUG);
            }
        }

        /// <summary>
        /// Verifie si un module est restreint pour ce device
        /// Retourne true si le module est BLOQUE, false si autorise
        /// </summary>
        /// <param name="moduleId">ID du module a verifier (vaultUpload, copyDesign, etc.)</param>
        /// <returns>True si le module est restreint/bloque</returns>
        public async Task<bool> IsModuleRestrictedAsync(string moduleId)
        {
            try
            {
                // Lire les restrictions du device depuis Firebase
                var restrictionsUrl = $"{FIREBASE_DATABASE_URL}/devices/{_deviceId}/restrictions.json";
                var response = await _httpClient.GetAsync(restrictionsUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    return false; // Pas de restrictions = autorise
                }

                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content) || content == "null")
                {
                    return false; // Pas de restrictions = autorise
                }

                // Parser le JSON pour trouver disabledModules
                // Format: { "disabledModules": ["vaultUpload", "copyDesign"], "updatedAt": "...", "updatedBy": "..." }
                if (content.Contains("\"disabledModules\""))
                {
                    // Extraction simple - chercher si moduleId est dans la liste
                    // Format attendu: "disabledModules":["module1","module2"]
                    var startIndex = content.IndexOf("\"disabledModules\"");
                    if (startIndex >= 0)
                    {
                        var bracketStart = content.IndexOf('[', startIndex);
                        var bracketEnd = content.IndexOf(']', bracketStart);
                        if (bracketStart >= 0 && bracketEnd > bracketStart)
                        {
                            var moduleList = content.Substring(bracketStart, bracketEnd - bracketStart + 1);
                            var isRestricted = moduleList.Contains($"\"{moduleId}\"");
                            
                            if (isRestricted)
                            {
                                Logger.Log($"[!] Module {moduleId} est RESTREINT pour ce poste", Logger.LogLevel.WARNING);
                            }
                            
                            return isRestricted;
                        }
                    }
                }

                return false; // Par defaut = autorise
            }
            catch (Exception ex)
            {
                Logger.Log($"[!] Erreur verification restrictions: {ex.Message}", Logger.LogLevel.DEBUG);
                return false; // En cas d'erreur, autoriser par defaut
            }
        }

        /// <summary>
        /// Verifie si un module est autorise et affiche un message si bloque
        /// Retourne true si AUTORISE, false si BLOQUE
        /// </summary>
        public async Task<bool> CheckModuleAccessAsync(string moduleId, string moduleName)
        {
            var isRestricted = await IsModuleRestrictedAsync(moduleId);
            
            if (isRestricted)
            {
                // Log l'acces refuse
                await LogUserActionAsync($"module_access_denied", $"Module {moduleName} ({moduleId}) bloque par restrictions admin");
                
                // Retourner false = module non autorise
                return false;
            }
            
            return true; // Autorise
        }

        /// <summary>
        /// Enregistre la creation d'un module HVAC
        /// </summary>
        public async Task TrackModuleCreatedAsync(string projectNumber, string moduleName)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("o");

                // Incrementer le compteur global de modules crees
                await IncrementStatAsync("statistics/global/totalModulesCreated");

                // Incrementer le compteur du device
                await IncrementStatAsync($"devices/{_deviceId}/usage/totalModulesCreated");

                // Mettre a jour la date de derniere creation
                await PatchFirebaseAsync($"devices/{_deviceId}/usage", new Dictionary<string, object>
                {
                    ["lastModuleCreated"] = timestamp
                });

                // Log l'action
                await LogUserActionAsync("module_created", $"Project: {projectNumber}, Module: {moduleName}");

                System.Diagnostics.Debug.WriteLine($"[+] Firebase: Module created tracked - {projectNumber}/{moduleName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[!] Firebase track module created warning: {ex.Message}");
            }
        }

        /// <summary>
        /// Enregistre un upload vers Vault
        /// </summary>
        public async Task TrackVaultUploadAsync(int fileCount, string folderPath = null)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("o");

                // Incrementer le compteur global d'uploads
                await IncrementStatAsync("statistics/global/totalUploads");

                // Incrementer le compteur du device
                await IncrementStatAsync($"devices/{_deviceId}/usage/totalUploads");

                // Mettre a jour la date de dernier upload
                await PatchFirebaseAsync($"devices/{_deviceId}/usage", new Dictionary<string, object>
                {
                    ["lastUpload"] = timestamp
                });

                // Tracker l'utilisation du module vaultUpload
                await TrackModuleUsageAsync("vaultUpload", $"{fileCount} fichiers uploades");

                System.Diagnostics.Debug.WriteLine($"[+] Firebase: Vault upload tracked - {fileCount} files");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[!] Firebase track upload warning: {ex.Message}");
            }
        }

        /// <summary>
        /// Met a jour le heartbeat du device (appeler periodiquement)
        /// </summary>
        public async Task UpdateHeartbeatAsync()
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("o");
                var heartbeatData = new Dictionary<string, object>
                {
                    ["status"] = "online",
                    ["lastHeartbeat"] = timestamp,
                    ["missedHeartbeats"] = 0,
                    ["cpuUsage"] = GetCpuUsage(),
                    ["ramUsage"] = GetRamUsage(),
                    ["diskUsage"] = 0
                };

                await PatchFirebaseAsync($"devices/{_deviceId}/heartbeat", heartbeatData);

                // Mettre a jour le status online
                await PatchFirebaseAsync($"devices/{_deviceId}/status", new Dictionary<string, object>
                {
                    ["online"] = true,
                    ["lastSeen"] = timestamp
                });
            }
            catch
            {
                // Silencieux
            }
        }

        /// <summary>
        /// Enregistre la fin de session
        /// </summary>
        public async Task RegisterSessionEndAsync()
        {
            try
            {
                // Flush les erreurs en attente
                await FlushErrorQueueAsync();

                var timestamp = DateTime.UtcNow.ToString("o");

                // Marquer le device comme offline
                await PatchFirebaseAsync($"devices/{_deviceId}/status", new Dictionary<string, object>
                {
                    ["online"] = false,
                    ["lastSeen"] = timestamp
                });

                await PatchFirebaseAsync($"devices/{_deviceId}/heartbeat", new Dictionary<string, object>
                {
                    ["status"] = "offline"
                });

                // Log de fin de session
                var logId = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                await PutFirebaseAsync($"auditLog/{logId}", new Dictionary<string, object>
                {
                    ["id"] = logId,
                    ["action"] = "session_end",
                    ["category"] = "session",
                    ["userId"] = _userName,
                    ["userName"] = _userName,
                    ["deviceId"] = _deviceId,
                    ["timestamp"] = timestamp,
                    ["details"] = "Application closed",
                    ["success"] = true,
                    ["ipAddress"] = "local",
                    ["oldValue"] = "none",
                    ["newValue"] = "none",
                    ["errorMessage"] = "none"
                });
            }
            catch
            {
                // Silencieux
            }
        }

        #endregion

        #region Private Methods

        private async Task RegisterSessionStartAsync()
        {
            var timestamp = DateTime.UtcNow.ToString("o");

            // Mettre a jour le device comme online
            await PatchFirebaseAsync($"devices/{_deviceId}/status", new Dictionary<string, object>
            {
                ["online"] = true,
                ["lastSeen"] = timestamp,
                ["currentUser"] = _userName
            });

            await PatchFirebaseAsync($"devices/{_deviceId}/heartbeat", new Dictionary<string, object>
            {
                ["status"] = "online",
                ["lastHeartbeat"] = timestamp
            });

            await PatchFirebaseAsync($"devices/{_deviceId}/software", new Dictionary<string, object>
            {
                ["xeatVersion"] = _appVersion,
                ["xeatLastUpdated"] = timestamp
            });

            // Log de debut de session
            var logId = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            await PutFirebaseAsync($"auditLog/{logId}", new Dictionary<string, object>
            {
                ["id"] = logId,
                ["action"] = "session_start",
                ["category"] = "session",
                ["userId"] = _userName,
                ["userName"] = _userName,
                ["deviceId"] = _deviceId,
                ["timestamp"] = timestamp,
                ["details"] = $"XEAT v{_appVersion} started",
                ["success"] = true,
                ["ipAddress"] = "local",
                ["oldValue"] = "none",
                ["newValue"] = "none",
                ["errorMessage"] = "none"
            });

            // Incrementer les statistiques
            await IncrementStatAsync("statistics/global/totalSessions");
        }

        private async Task FlushErrorQueueAsync()
        {
            List<AuditLogEntry> entriesToSend;

            lock (_queueLock)
            {
                if (_errorQueue.Count == 0) return;

                entriesToSend = new List<AuditLogEntry>(_errorQueue);
                _errorQueue.Clear();
                _lastFlush = DateTime.Now;
            }

            foreach (var entry in entriesToSend)
            {
                try
                {
                    var logId = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                    var auditEntry = new Dictionary<string, object>
                    {
                        ["id"] = logId,
                        ["action"] = entry.Action,
                        ["category"] = entry.Category,
                        ["userId"] = _userName,
                        ["userName"] = _userName,
                        ["deviceId"] = _deviceId,
                        ["timestamp"] = entry.Timestamp.ToString("o"),
                        ["details"] = $"[{entry.ErrorType}] {entry.Message}" + (entry.Context != null ? $" (Context: {entry.Context})" : ""),
                        ["success"] = false,
                        ["ipAddress"] = "local",
                        ["oldValue"] = "none",
                        ["newValue"] = "none",
                        ["errorMessage"] = entry.StackTrace ?? entry.Message
                    };

                    await PutFirebaseAsync($"auditLog/{logId}", auditEntry);

                    // Incrementer le compteur d'erreurs
                    await IncrementStatAsync("statistics/global/totalErrors");
                }
                catch
                {
                    // Silencieux
                }
            }
        }

        private async Task PutFirebaseAsync(string path, Dictionary<string, object> data)
        {
            var json = DictionaryToJson(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Obtenir l'URL authentifiee avec le token Firebase
            var url = await FirebaseAuthService.GetAuthenticatedUrlAsync($"{FIREBASE_DATABASE_URL}/{path}.json");
            await _httpClient.PutAsync(url, content);
        }

        private async Task PatchFirebaseAsync(string path, Dictionary<string, object> data)
        {
            var json = DictionaryToJson(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Obtenir l'URL authentifiee avec le token Firebase
            var url = await FirebaseAuthService.GetAuthenticatedUrlAsync($"{FIREBASE_DATABASE_URL}/{path}.json");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
            {
                Content = content
            };
            await _httpClient.SendAsync(request);
        }

        private async Task IncrementStatAsync(string path)
        {
            try
            {
                // Obtenir l'URL authentifiee pour la lecture
                var readUrl = await FirebaseAuthService.GetAuthenticatedUrlAsync($"{FIREBASE_DATABASE_URL}/{path}.json");
                var response = await _httpClient.GetStringAsync(readUrl);
                int currentValue = 0;
                if (int.TryParse(response.Trim('"'), out int val))
                    currentValue = val;

                var content = new StringContent((currentValue + 1).ToString(), Encoding.UTF8, "application/json");
                // Obtenir l'URL authentifiee pour l'ecriture
                var writeUrl = await FirebaseAuthService.GetAuthenticatedUrlAsync($"{FIREBASE_DATABASE_URL}/{path}.json");
                await _httpClient.PutAsync(writeUrl, content);
            }
            catch { }
        }

        private int GetCpuUsage()
        {
            // Approximation simple
            return 0;
        }

        private int GetRamUsage()
        {
            try
            {
                var proc = System.Diagnostics.Process.GetCurrentProcess();
                var usedMB = proc.WorkingSet64 / (1024 * 1024);
                // Approximation du pourcentage
                return (int)(usedMB / 10); // Rough estimate
            }
            catch
            {
                return 0;
            }
        }

        private static string DictionaryToJson(Dictionary<string, object> dict)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (var kvp in dict)
            {
                if (!first) sb.Append(",");
                first = false;
                sb.Append($"\"{kvp.Key}\":");
                sb.Append(ObjectToJson(kvp.Value));
            }
            sb.Append("}");
            return sb.ToString();
        }

        private static string ObjectToJson(object obj)
        {
            if (obj == null) return "null";
            if (obj is string s) return $"\"{EscapeJson(s)}\"";
            if (obj is bool b) return b.ToString().ToLower();
            if (obj is int || obj is long || obj is double || obj is float) return obj.ToString();
            if (obj is Dictionary<string, object> dict) return DictionaryToJson(dict);
            return $"\"{EscapeJson(obj.ToString())}\"";
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }

        #endregion

        #region Feature Flags Sync

        /// <summary>
        /// Synchronise les Feature Flags avec Firebase.
        /// Corrige les icones corrompues et ajoute les modules manquants.
        /// A appeler au demarrage de l'application.
        /// </summary>
        public async Task SyncFeatureFlagsAsync()
        {
            try
            {
                Logger.Log("[>] Synchronisation des Feature Flags vers Firebase...", Logger.LogLevel.INFO);

                // Tous les modules avec les bons emojis (Firebase les supporte)
                var allModules = new Dictionary<string, object>
                {
                    // === MODULES PRINCIPAUX ===
                    ["openProject"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "Ouvrir Projet - Ouvrir un projet depuis Vault ou Local",
                        ["displayName"] = "Ouvrir Projet",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "admin-adminDesigner-designer-user",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F4E5" // 📥
                    },
                    ["configUnits"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "Config Unites - Configuration Master des Unites HVAC",
                        ["displayName"] = "Config Unites",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "admin-adminDesigner-designer-user",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\u2699\uFE0F" // ⚙️
                    },
                    ["copyDesign"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "Creer Module - Copy Design intelligent",
                        ["displayName"] = "Creer Module",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "admin-adminDesigner-designer",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F4E6" // 📦
                    },
                    ["placeEquipment"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "Place Equipment - Placer equipement HVAC dans un module",
                        ["displayName"] = "Place Equipment",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "admin-adminDesigner-designer",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F9CA" // 🧊
                    },
                    ["buildModule"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "Build Module - Construction et assemblage de modules",
                        ["displayName"] = "Build Module",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "admin-adminDesigner-designer",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F3D7\uFE0F" // 🏗️
                    },
                    ["smartTools"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "Smart Tools - Outils intelligents pour Inventor",
                        ["displayName"] = "Smart Tools",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "admin-adminDesigner-designer",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F6E0\uFE0F" // 🛠️
                    },
                    ["vaultUpload"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "Upload Module - Televersement vers Vault",
                        ["displayName"] = "Upload Module",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "admin-adminDesigner-designer-user",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F4E4" // 📤
                    },
                    ["uploadTemplate"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = true,
                        ["description"] = "Upload Template - Admin Templates Vault",
                        ["displayName"] = "Upload Template",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "admin",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F4E4" // 📤
                    },
                    ["checklistHVAC"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "Checklist HVAC - Verification qualite AHU",
                        ["displayName"] = "Checklist HVAC",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "admin-adminDesigner-designer-user",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F4DD" // 📝
                    },
                    ["acp"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "ACP - Points Critiques Modules",
                        ["displayName"] = "ACP",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "admin-adminDesigner-designer",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F4CB" // 📋
                    },
                    ["dxfVerifier"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "DXF/CSV vs PDF - Verification des fichiers",
                        ["displayName"] = "DXF/CSV vs PDF",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "admin-adminDesigner-designer-user",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F50D" // 🔍
                    },
                    
                    // === FONCTIONNALITES SYSTEME (corriger les icones corrompues) ===
                    ["autoUpdate"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "Mises a jour automatiques de l'application",
                        ["displayName"] = "Mise a jour auto",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "all",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F504" // 🔄
                    },
                    ["broadcastMessages"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "Reception des messages broadcast",
                        ["displayName"] = "Messages Broadcast",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "all",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F4E2" // 📢
                    },
                    ["welcomeMessages"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "Affichage des messages de bienvenue",
                        ["displayName"] = "Messages Bienvenue",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "all",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F44B" // 👋
                    },
                    ["darkTheme"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "Theme sombre de l'interface",
                        ["displayName"] = "Theme sombre",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "all",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F319" // 🌙
                    },
                    ["telemetryCollection"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "Collecte des donnees de telemetrie",
                        ["displayName"] = "Telemetrie",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "all",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F4CA" // 📊
                    },
                    ["maintenanceAlerts"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = false,
                        ["description"] = "Alertes de maintenance planifiee",
                        ["displayName"] = "Alertes Maintenance",
                        ["enabled"] = true,
                        ["enabledForRoles"] = "all",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\U0001F527" // 🔧
                    },
                    ["sharepointIntegration"] = new Dictionary<string, object>
                    {
                        ["betaOnly"] = true,
                        ["description"] = "Integration avec SharePoint",
                        ["displayName"] = "SharePoint",
                        ["enabled"] = false,
                        ["enabledForRoles"] = "admin",
                        ["enabledForSites"] = "all",
                        ["icon"] = "\u2601\uFE0F" // ☁️
                    }
                    // NOTE: Les anciens modules obsoletes (copyDesignModule, smartToolsModule,
                    // vaultUploadModule, vaultSettingsModule, exportDxfPdf) ont ete supprimes
                    // Ils sont geres dans la console admin via le bouton "Nettoyer obsoletes"
                };

                // Utiliser PATCH pour ajouter/mettre a jour sans ecraser les autres champs
                await PatchFirebaseAsync("featureFlags", allModules);
                
                Logger.Log("[+] Feature Flags synchronises avec succes", Logger.LogLevel.INFO);
            }
            catch (Exception ex)
            {
                Logger.Log($"[-] Erreur synchronisation Feature Flags: {ex.Message}", Logger.LogLevel.WARNING);
            }
        }

        /// <summary>
        /// Synchronise les categories de telemetrie avec les bons emojis
        /// Corrige les icones corrompues par des problemes d'encodage
        /// </summary>
        public async Task SyncTelemetryCategoriesAsync()
        {
            try
            {
                Logger.Log("[>] Synchronisation des categories de telemetrie...", Logger.LogLevel.INFO);

                var telemetryCategories = new Dictionary<string, object>
                {
                    ["action"] = new Dictionary<string, object>
                    {
                        ["color"] = "#4CAF50",
                        ["description"] = "Actions utilisateur dans l'application",
                        ["icon"] = "\u26A1" // ⚡
                    },
                    ["auth"] = new Dictionary<string, object>
                    {
                        ["color"] = "#2196F3",
                        ["description"] = "Authentification et autorisation",
                        ["icon"] = "\U0001F510" // 🔐
                    },
                    ["error"] = new Dictionary<string, object>
                    {
                        ["color"] = "#F44336",
                        ["description"] = "Erreurs et exceptions",
                        ["icon"] = "\u274C" // ❌
                    },
                    ["lifecycle"] = new Dictionary<string, object>
                    {
                        ["color"] = "#9C27B0",
                        ["description"] = "Cycle de vie de l'application",
                        ["icon"] = "\U0001F504" // 🔄
                    },
                    ["navigation"] = new Dictionary<string, object>
                    {
                        ["color"] = "#FF9800",
                        ["description"] = "Navigation entre les modules",
                        ["icon"] = "\U0001F9ED" // 🧭
                    },
                    ["performance"] = new Dictionary<string, object>
                    {
                        ["color"] = "#607D8B",
                        ["description"] = "Metriques de performance",
                        ["icon"] = "\U0001F4CA" // 📊
                    }
                };

                // Utiliser PATCH pour mettre a jour les categories
                await PatchFirebaseAsync("telemetry/categories", telemetryCategories);
                
                Logger.Log("[+] Categories de telemetrie synchronisees avec succes", Logger.LogLevel.INFO);
            }
            catch (Exception ex)
            {
                Logger.Log($"[-] Erreur synchronisation categories telemetrie: {ex.Message}", Logger.LogLevel.WARNING);
            }
        }

        #endregion

        #region Inner Classes

        private class AuditLogEntry
        {
            public string Action { get; set; }
            public string Category { get; set; }
            public string ErrorType { get; set; }
            public string Message { get; set; }
            public string StackTrace { get; set; }
            public string Context { get; set; }
            public DateTime Timestamp { get; set; }
        }

        #endregion
    }
}
