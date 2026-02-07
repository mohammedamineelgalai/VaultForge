using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace XnrgyEngineeringAutomationTools.Services;

/// <summary>
/// Logger securise avec cryptage AES pour les logs de production.
/// Les logs sont stockes dans %AppData%\XEAT\Logs\ et sont cryptes.
/// Seule l'application ou quelqu'un avec le mot de passe peut les lire.
/// </summary>
public static class SecureLogger
{
    private static string _logFilePath = string.Empty;
    private static readonly object _lockObj = new();
    private static bool _isInitialized = false;
    
    // Clé dérivée du mot de passe (ne pas modifier)
    private static byte[]? _aesKey;
    private static byte[]? _aesIV;
    
    // Dossier AppData pour les logs de production
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XEAT", "Logs");

    /// <summary>
    /// Initialise le logger securise avec le mot de passe de cryptage
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized) return;
        
        try
        {
            // Creer le dossier si necessaire
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }
            
            // Generer la cle AES a partir du mot de passe
            string password = "Vtr8aPz21930**";
            using (var deriveBytes = new Rfc2898DeriveBytes(password, 
                Encoding.UTF8.GetBytes("XEAT_SALT_2026"), 10000, HashAlgorithmName.SHA256))
            {
                _aesKey = deriveBytes.GetBytes(32); // AES-256
                _aesIV = deriveBytes.GetBytes(16);  // IV 128-bit
            }
            
            // Creer le fichier log du jour
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logFilePath = Path.Combine(AppDataFolder, $"XEAT_{timestamp}.xlog");
            
            _isInitialized = true;
            
            // Ecrire l'en-tete
            WriteEncrypted("═══════════════════════════════════════════════════════");
            WriteEncrypted("  XEAT - SESSION DEMARREE (SECURE LOG)");
            WriteEncrypted($"  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            WriteEncrypted($"  Machine: {Environment.MachineName}");
            WriteEncrypted($"  User: {Environment.UserName}");
            WriteEncrypted("═══════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            // Fallback silencieux - ne pas bloquer l'app
            System.Diagnostics.Debug.WriteLine($"[SecureLogger] Init failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Ecrit un message crypte dans le log
    /// </summary>
    public static void Log(string message, string level = "INFO")
    {
        if (!_isInitialized) Initialize();
        if (string.IsNullOrEmpty(_logFilePath) || _aesKey == null) return;
        
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string formattedMessage = $"[{timestamp}] [{level.PadRight(7)}] {message}";
            WriteEncrypted(formattedMessage);
        }
        catch
        {
            // Silencieux
        }
    }
    
    /// <summary>
    /// Ecrit une ligne cryptee dans le fichier
    /// </summary>
    private static void WriteEncrypted(string plainText)
    {
        if (_aesKey == null || _aesIV == null) return;
        
        lock (_lockObj)
        {
            try
            {
                // Crypter le message
                byte[] encrypted = EncryptString(plainText);
                
                // Ecrire en base64 + newline
                string base64Line = Convert.ToBase64String(encrypted);
                File.AppendAllText(_logFilePath, base64Line + Environment.NewLine);
            }
            catch
            {
                // Silencieux
            }
        }
    }
    
    /// <summary>
    /// Crypte une chaine avec AES-256
    /// </summary>
    private static byte[] EncryptString(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _aesKey!;
        aes.IV = _aesIV!;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        
        using var encryptor = aes.CreateEncryptor();
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
    }
    
    /// <summary>
    /// Decrypte une chaine AES-256 (pour lecture des logs)
    /// </summary>
    private static string DecryptString(byte[] cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = _aesKey!;
        aes.IV = _aesIV!;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        
        using var decryptor = aes.CreateDecryptor();
        byte[] plainBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
    
    /// <summary>
    /// Lit et decrypte un fichier log complet (pour investigation)
    /// </summary>
    public static string ReadLogFile(string logFilePath)
    {
        if (!_isInitialized) Initialize();
        if (_aesKey == null) return "[ERROR] Logger not initialized";
        
        var sb = new StringBuilder();
        
        try
        {
            foreach (string line in File.ReadAllLines(logFilePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                try
                {
                    byte[] encrypted = Convert.FromBase64String(line);
                    string decrypted = DecryptString(encrypted);
                    sb.AppendLine(decrypted);
                }
                catch
                {
                    sb.AppendLine($"[DECRYPT ERROR] {line.Substring(0, Math.Min(50, line.Length))}...");
                }
            }
        }
        catch (Exception ex)
        {
            return $"[ERROR] Cannot read log file: {ex.Message}";
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Liste tous les fichiers logs disponibles
    /// </summary>
    public static string[] GetLogFiles()
    {
        if (!Directory.Exists(AppDataFolder))
            return Array.Empty<string>();
            
        return Directory.GetFiles(AppDataFolder, "*.xlog");
    }
    
    /// <summary>
    /// Ferme la session de log
    /// </summary>
    public static void Close()
    {
        if (!_isInitialized) return;
        
        WriteEncrypted("═══════════════════════════════════════════════════════");
        WriteEncrypted("  SESSION TERMINEE");
        WriteEncrypted($"  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        WriteEncrypted("═══════════════════════════════════════════════════════");
    }
    
    // Raccourcis pour les niveaux de log
    public static void Debug(string message) => Log(message, "DEBUG");
    public static void Info(string message) => Log(message, "INFO");
    public static void Warning(string message) => Log(message, "WARNING");
    public static void Error(string message) => Log(message, "ERROR");
    public static void Fatal(string message) => Log(message, "FATAL");
    
    /// <summary>
    /// Log une exception complete
    /// </summary>
    public static void LogException(string context, Exception ex)
    {
        Log($"[-] EXCEPTION dans {context}:", "ERROR");
        Log($"   Message: {ex.Message}", "ERROR");
        Log($"   Type: {ex.GetType().Name}", "ERROR");
        if (ex.InnerException != null)
        {
            Log($"   Inner: {ex.InnerException.Message}", "ERROR");
        }
        if (!string.IsNullOrEmpty(ex.StackTrace))
        {
            Log($"   StackTrace: {ex.StackTrace.Replace("\r\n", " | ")}", "ERROR");
        }
    }
}
