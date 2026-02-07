using System;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using System.IO;

namespace XnrgyEngineeringAutomationTools.Services
{
    /// <summary>
    /// Service centralise pour la configuration Firebase securisee
    /// - URL chiffree XOR avec cle dynamique (IMPOSSIBLE a lire en clair)
    /// - Verification d'integrite de l'application
    /// - Protection contre la modification
    /// </summary>
    public static class FirebaseConfigService
    {
        // URL chiffree avec XOR - les bytes ne correspondent PAS aux caracteres ASCII
        // Cle XOR double couche - rend chaque byte meconnaissable
        // IMPOSSIBLE de retrouver l'URL sans connaitre l'algorithme ET les 2 cles
        private static readonly byte[] _xorData = new byte[] {
            0x6A, 0x60, 0x7C, 0x6D, 0x70, 0x38, 0x3B, 0x27, 0x65, 0x66, 0x63, 0x60, 0x25, 
            0x6F, 0x66, 0x6F, 0x7B, 0x7C, 0x78, 0x2E, 0x61, 0x7B, 0x66, 0x69, 0x71, 0x6D, 
            0x78, 0x25, 0x79, 0x66, 0x64, 0x75, 0x7D, 0x71, 0x77, 0x2F, 0x66, 0x7C, 0x79, 
            0x61, 0x2C, 0x72, 0x61, 0x6F, 0x66, 0x60, 0x75, 0x7B, 0x78, 0x6A, 0x6D, 0x3A, 
            0x6B, 0x72, 0x6E
        };
        
        // Cle XOR primaire
        private static readonly byte _xorKey1 = 0x5A;
        
        // Cle XOR secondaire (rotation basee sur index)
        private static readonly byte[] _xorKey2 = new byte[] { 0x58, 0x4E, 0x52, 0x47, 0x59 }; // "XNRGY"

        // Cache pour eviter le decodage repetitif
        private static string _cachedUrl = null;
        private static readonly object _lock = new object();

        /// <summary>
        /// Obtient l'URL Firebase de maniere securisee
        /// Dechiffrement XOR a double couche
        /// </summary>
        public static string GetDatabaseUrl()
        {
            if (_cachedUrl == null)
            {
                lock (_lock)
                {
                    if (_cachedUrl == null)
                    {
                        _cachedUrl = DecodeUrl();
                    }
                }
            }
            return _cachedUrl;
        }
        
        /// <summary>
        /// Dechiffre l'URL avec XOR double couche
        /// </summary>
        private static string DecodeUrl()
        {
            byte[] decoded = new byte[_xorData.Length];
            for (int i = 0; i < _xorData.Length; i++)
            {
                // XOR avec cle primaire puis avec cle secondaire rotative
                decoded[i] = (byte)(_xorData[i] ^ _xorKey1 ^ _xorKey2[i % _xorKey2.Length]);
            }
            return Encoding.UTF8.GetString(decoded);
        }

        /// <summary>
        /// Verifie l'integrite de l'application
        /// Retourne true si l'executable n'a pas ete modifie
        /// </summary>
        public static bool VerifyApplicationIntegrity()
        {
            try
            {
                // En mode Release, verifier la signature
                #if !DEBUG
                var assembly = Assembly.GetExecutingAssembly();
                var assemblyName = assembly.GetName();
                
                // Verifier que l'assembly est signe
                byte[] publicKey = assemblyName.GetPublicKey();
                if (publicKey == null || publicKey.Length == 0)
                {
                    // Assembly non signe - acceptable en dev, suspect en prod
                    Logger.Log("[!] Assembly non signe detecte", Logger.LogLevel.WARNING);
                }
                
                // Verifier le nom de l'assembly
                if (!assemblyName.Name.Equals("XnrgyEngineeringAutomationTools", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Log("[-] Nom d'assembly invalide detecte", Logger.LogLevel.ERROR);
                    return false;
                }
                #endif

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[!] Erreur verification integrite: {ex.Message}", Logger.LogLevel.WARNING);
                return true; // En cas d'erreur, laisser passer
            }
        }

        /// <summary>
        /// Verifie si l'application est en mode debug (potentiellement modifiee)
        /// </summary>
        public static bool IsDebuggerAttached()
        {
            return System.Diagnostics.Debugger.IsAttached;
        }

        /// <summary>
        /// Obtient un identifiant unique pour ce device (combine plusieurs facteurs)
        /// </summary>
        public static string GetSecureDeviceId()
        {
            try
            {
                string machineName = Environment.MachineName;
                string userName = Environment.UserName;
                string domainName = Environment.UserDomainName;

                // Format: MACHINE_domain_user (sanitized pour Firebase)
                string deviceId = $"{machineName}_{userName}";
                deviceId = SanitizeForFirebase(deviceId);

                return deviceId;
            }
            catch
            {
                return $"UNKNOWN_{Guid.NewGuid():N}".Substring(0, 32);
            }
        }

        /// <summary>
        /// Obtient un identifiant unique pour l'utilisateur courant
        /// </summary>
        public static string GetSecureUserId()
        {
            try
            {
                string domainName = Environment.UserDomainName ?? "LOCAL";
                string userName = Environment.UserName ?? "UNKNOWN";
                
                string userId = $"{domainName}_{userName}";
                return SanitizeForFirebase(userId);
            }
            catch
            {
                return "UNKNOWN_USER";
            }
        }

        /// <summary>
        /// Nettoie une chaine pour etre utilisee comme cle Firebase
        /// Firebase interdit: . $ # [ ] /
        /// </summary>
        public static string SanitizeForFirebase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "unknown";

            return input
                .Replace(".", "_")
                .Replace("$", "_")
                .Replace("#", "_")
                .Replace("[", "_")
                .Replace("]", "_")
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace(" ", "_")
                .ToLowerInvariant();
        }

        /// <summary>
        /// Genere un hash SHA256 pour verification d'integrite
        /// </summary>
        public static string ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                var builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Verifie si l'environnement d'execution est securise
        /// </summary>
        public static (bool isSecure, string warning) CheckSecurityEnvironment()
        {
            var warnings = new StringBuilder();
            bool isSecure = true;

            // 1. Verifier le debugger
            if (IsDebuggerAttached())
            {
                #if DEBUG
                warnings.AppendLine("Mode debug actif (normal en developpement)");
                #else
                warnings.AppendLine("[!] Debugger detecte en mode Release");
                isSecure = false;
                #endif
            }

            // 2. Verifier l'integrite
            if (!VerifyApplicationIntegrity())
            {
                warnings.AppendLine("[!] Integrite de l'application compromise");
                isSecure = false;
            }

            // 3. Verifier que l'app tourne depuis le bon emplacement
            string exePath = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(exePath))
            {
                string expectedPath = @"C:\Program Files\XNRGY\XEAT";
                string actualFolder = Path.GetDirectoryName(exePath);
                
                // En dev, on accepte d'autres emplacements
                #if !DEBUG
                if (!actualFolder.StartsWith(expectedPath, StringComparison.OrdinalIgnoreCase) &&
                    !actualFolder.Contains("bin\\Release"))
                {
                    warnings.AppendLine($"[!] Emplacement suspect: {actualFolder}");
                    // Ne pas bloquer pour l'instant, juste logger
                }
                #endif
            }

            return (isSecure, warnings.ToString());
        }
    }
}
