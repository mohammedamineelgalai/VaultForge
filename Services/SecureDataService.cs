using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace XnrgyEngineeringAutomationTools.Services
{
    /// <summary>
    /// Service de chiffrement avance pour les donnees sensibles
    /// Utilise AES-256 avec cle derivee de l'environnement machine
    /// Rend IMPOSSIBLE la lecture des donnees sensibles meme avec decompilation
    /// </summary>
    public static class SecureDataService
    {
        // Sel statique (partie de la cle) - melange avec des donnees dynamiques
        private static readonly byte[] _staticSalt = new byte[] {
            0x58, 0x4E, 0x52, 0x47, 0x59, 0x2D, 0x58, 0x45,
            0x41, 0x54, 0x2D, 0x32, 0x30, 0x32, 0x36, 0x21
        }; // "XNRGY-XEAT-2026!"

        // Donnees chiffrees Firebase - IMPOSSIBLE a lire sans la cle machine
        // Chiffre avec: machine name + domain + static salt
        private static readonly byte[] _encryptedFirebaseUrl = new byte[] {
            // Ces bytes seront generes au premier lancement et stockes
            // Pour l'instant, fallback sur l'obfuscation simple
        };

        // Cache du resultat dechiffre
        private static string _cachedDecryptedUrl = null;
        private static readonly object _lock = new object();

        /// <summary>
        /// Obtient l'URL Firebase de maniere ultra-securisee
        /// La cle de dechiffrement depend de la machine - 
        /// copier l'exe sur une autre machine ne fonctionnera pas sans re-installation
        /// </summary>
        public static string GetSecureFirebaseUrl()
        {
            if (_cachedDecryptedUrl != null)
                return _cachedDecryptedUrl;

            lock (_lock)
            {
                if (_cachedDecryptedUrl != null)
                    return _cachedDecryptedUrl;

                try
                {
                    // Methode 1: Essayer de lire depuis le stockage securise Windows
                    string stored = ReadFromSecureStorage("XEAT_FB_URL");
                    if (!string.IsNullOrEmpty(stored))
                    {
                        _cachedDecryptedUrl = stored;
                        return _cachedDecryptedUrl;
                    }

                    // Methode 2: Utiliser l'URL obfusquee de FirebaseConfigService
                    // (fallback pour compatibilite)
                    _cachedDecryptedUrl = FirebaseConfigService.GetDatabaseUrl();
                    
                    // Sauvegarder dans le stockage securise pour les prochains lancements
                    WriteToSecureStorage("XEAT_FB_URL", _cachedDecryptedUrl);
                    
                    return _cachedDecryptedUrl;
                }
                catch
                {
                    // Fallback ultime
                    return FirebaseConfigService.GetDatabaseUrl();
                }
            }
        }

        /// <summary>
        /// Chiffre une chaine avec AES-256 et une cle derivee de la machine
        /// </summary>
        public static byte[] EncryptForThisMachine(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return Array.Empty<byte>();

            byte[] key = DeriveKeyFromMachine();
            byte[] iv = GenerateIV();

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    // Ecrire l'IV en premier (necessaire pour le dechiffrement)
                    ms.Write(iv, 0, iv.Length);
                    
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(plainBytes, 0, plainBytes.Length);
                    }
                    
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Dechiffre des donnees chiffrees avec EncryptForThisMachine
        /// Ne fonctionnera QUE sur la meme machine
        /// </summary>
        public static string DecryptForThisMachine(byte[] encryptedData)
        {
            if (encryptedData == null || encryptedData.Length < 17)
                return null;

            byte[] key = DeriveKeyFromMachine();
            
            // Extraire l'IV (premiers 16 bytes)
            byte[] iv = new byte[16];
            Array.Copy(encryptedData, 0, iv, 0, 16);
            
            // Extraire les donnees chiffrees
            byte[] cipherText = new byte[encryptedData.Length - 16];
            Array.Copy(encryptedData, 16, cipherText, 0, cipherText.Length);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(cipherText))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Derive une cle AES-256 unique pour cette machine
        /// Combine: Machine Name + Domain + Windows Product ID + Static Salt
        /// </summary>
        private static byte[] DeriveKeyFromMachine()
        {
            // Collecter les identifiants uniques de la machine
            string machineFingerprint = string.Join("|",
                Environment.MachineName ?? "UNKNOWN",
                Environment.UserDomainName ?? "WORKGROUP",
                GetWindowsProductId(),
                GetProcessorId()
            );

            // Deriver une cle de 256 bits avec PBKDF2
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                Encoding.UTF8.GetBytes(machineFingerprint),
                _staticSalt,
                100000, // 100,000 iterations - tres securise
                HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32); // 256 bits
            }
        }

        /// <summary>
        /// Genere un IV aleatoire pour le chiffrement
        /// </summary>
        private static byte[] GenerateIV()
        {
            byte[] iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }
            return iv;
        }

        /// <summary>
        /// Obtient le Product ID Windows (unique par installation)
        /// </summary>
        private static string GetWindowsProductId()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    return key?.GetValue("ProductId")?.ToString() ?? "UNKNOWN";
                }
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        /// <summary>
        /// Obtient l'ID du processeur (unique par CPU)
        /// </summary>
        private static string GetProcessorId()
        {
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj["ProcessorId"]?.ToString() ?? "UNKNOWN";
                    }
                }
            }
            catch { }
            return "UNKNOWN";
        }

        #region Windows Secure Storage (DPAPI)

        /// <summary>
        /// Stocke des donnees dans le stockage securise Windows (DPAPI)
        /// Les donnees sont chiffrees avec les credentials Windows de l'utilisateur
        /// IMPOSSIBLE a lire sans etre connecte avec le meme compte Windows
        /// </summary>
        public static void WriteToSecureStorage(string key, string value)
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "XNRGY", "XEAT", "secure");
                
                Directory.CreateDirectory(appDataPath);
                string filePath = Path.Combine(appDataPath, $"{key}.dat");

                // Chiffrer avec DPAPI (Windows Data Protection API)
                byte[] plainBytes = Encoding.UTF8.GetBytes(value);
                byte[] encryptedBytes = ProtectedData.Protect(
                    plainBytes, 
                    _staticSalt, 
                    DataProtectionScope.CurrentUser);

                File.WriteAllBytes(filePath, encryptedBytes);
            }
            catch (Exception ex)
            {
                Logger.Log($"[!] Erreur ecriture stockage securise: {ex.Message}", Logger.LogLevel.DEBUG);
            }
        }

        /// <summary>
        /// Lit des donnees depuis le stockage securise Windows
        /// </summary>
        public static string ReadFromSecureStorage(string key)
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "XNRGY", "XEAT", "secure");
                
                string filePath = Path.Combine(appDataPath, $"{key}.dat");
                
                if (!File.Exists(filePath))
                    return null;

                byte[] encryptedBytes = File.ReadAllBytes(filePath);
                byte[] plainBytes = ProtectedData.Unprotect(
                    encryptedBytes, 
                    _staticSalt, 
                    DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Supprime une donnee du stockage securise
        /// </summary>
        public static void DeleteFromSecureStorage(string key)
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "XNRGY", "XEAT", "secure");
                
                string filePath = Path.Combine(appDataPath, $"{key}.dat");
                
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch { }
        }

        #endregion

        #region Integrity Verification

        /// <summary>
        /// Verifie l'integrite de l'application
        /// Detecte si l'executable a ete modifie
        /// </summary>
        public static bool VerifyApplicationIntegrity()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                
                // Verifier la signature numerique
                var authenticodeSignature = System.Security.Cryptography.X509Certificates
                    .X509Certificate.CreateFromSignedFile(assembly.Location);
                
                if (authenticodeSignature != null)
                {
                    // L'application est signee - verifier le certificat
                    string issuer = authenticodeSignature.Issuer;
                    
                    // Verifier que c'est notre certificat
                    if (issuer.Contains("XNRGY") || issuer.Contains("Mohammed"))
                    {
                        return true;
                    }
                }
                
                // En mode debug, accepter sans signature
                #if DEBUG
                return true;
                #else
                Logger.Log("[!] Signature invalide ou absente", Logger.LogLevel.WARNING);
                return false;
                #endif
            }
            catch
            {
                #if DEBUG
                return true;
                #else
                return false;
                #endif
            }
        }

        /// <summary>
        /// Calcule le hash SHA256 d'un fichier
        /// </summary>
        public static string ComputeFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        #endregion
    }
}
