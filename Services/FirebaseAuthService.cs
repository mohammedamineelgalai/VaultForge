using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace XnrgyEngineeringAutomationTools.Services
{
    /// <summary>
    /// Service d'authentification Firebase pour l'application XEAT
    /// Utilise le Service Account pour generer des tokens d'acces securises (OAuth2)
    /// 
    /// Les comptes autorises:
    /// - elgalaiamine@gmail.com (UID: O2Jgqjfh1Tf5cskZa0DbTosB4A12)
    /// - mohammedamine.elgalai@xnrgy.com (UID: LeIel3jEkwaj0UFwUjxPHJRS20w2)
    /// 
    /// Cette implementation utilise Google OAuth2 avec le Service Account
    /// pour obtenir un Access Token directement utilisable avec Firebase REST API
    /// Utilise BouncyCastle pour le parsing des cles RSA (compatible .NET Framework 4.8)
    /// </summary>
    public static class FirebaseAuthService
    {
        #region Constants - Service Account Configuration

        // Service Account Email (firebase-adminsdk)
        private static readonly string SERVICE_ACCOUNT_EMAIL = "firebase-adminsdk-fbsvc@xeat-remote-control.iam.gserviceaccount.com";
        
        // Google OAuth2 Token URL
        private static readonly string GOOGLE_TOKEN_URL = "https://oauth2.googleapis.com/token";
        
        // Scope pour Firebase Realtime Database
        private static readonly string FIREBASE_SCOPE = "https://www.googleapis.com/auth/firebase.database https://www.googleapis.com/auth/userinfo.email";
        
        // Cache du token
        private static string _cachedAccessToken = null;
        private static DateTime _tokenExpiry = DateTime.MinValue;
        private static readonly object _tokenLock = new object();
        
        // HttpClient reutilisable
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        #endregion

        #region Public Methods

        /// <summary>
        /// Obtient un Access Token Google OAuth2 pour authentifier les requetes Firebase
        /// Le token est cache et renouvele automatiquement avant expiration
        /// </summary>
        public static async Task<string> GetAccessTokenAsync()
        {
            // Verifier le cache
            lock (_tokenLock)
            {
                if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
                {
                    return _cachedAccessToken;
                }
            }

            try
            {
                // 1. Generer un JWT assertion signe avec le Service Account
                var jwtAssertion = GenerateJwtAssertion();

                // 2. Echanger le JWT contre un Access Token Google
                var accessToken = await ExchangeJwtForAccessTokenAsync(jwtAssertion);

                // 3. Mettre en cache (tokens valides 1 heure)
                lock (_tokenLock)
                {
                    _cachedAccessToken = accessToken;
                    _tokenExpiry = DateTime.UtcNow.AddMinutes(55); // Marge de 5 min
                }

                Logger.Log("[+] Firebase Auth: Access Token obtenu avec succes", Logger.LogLevel.INFO);
                return accessToken;
            }
            catch (Exception ex)
            {
                Logger.Log($"[-] Firebase Auth: Erreur obtention token - {ex.Message}", Logger.LogLevel.ERROR);
                return null;
            }
        }

        /// <summary>
        /// Construit l'URL Firebase avec le token d'authentification
        /// Pour Firebase REST API avec Service Account, on utilise access_token
        /// </summary>
        public static async Task<string> GetAuthenticatedUrlAsync(string baseUrl)
        {
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                // Fallback sans auth si erreur (permet le fonctionnement degrade)
                Logger.Log("[!] Firebase: Utilisation sans authentification (mode degrade)", Logger.LogLevel.WARNING);
                return baseUrl;
            }
            
            // Pour Service Account, utiliser access_token au lieu de auth
            return $"{baseUrl}?access_token={token}";
        }

        /// <summary>
        /// Verifie si l'authentification Firebase est disponible
        /// </summary>
        public static bool IsAuthAvailable()
        {
            return !string.IsNullOrEmpty(GetPrivateKeyPem());
        }

        #endregion

        #region Private Methods - Token Generation

        /// <summary>
        /// Genere un JWT assertion pour OAuth2 Service Account flow
        /// Format: Header.Payload.Signature (RS256)
        /// Utilise BouncyCastle pour la signature RSA (compatible .NET Framework 4.8)
        /// </summary>
        private static string GenerateJwtAssertion()
        {
            var now = DateTime.UtcNow;
            var privateKeyPem = GetPrivateKeyPem();
            
            if (string.IsNullOrEmpty(privateKeyPem))
            {
                throw new InvalidOperationException("Service Account private key not found");
            }

            // Header JSON
            var headerJson = "{\"alg\":\"RS256\",\"typ\":\"JWT\"}";

            // Payload JSON (claims)
            var iat = (long)(now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            var exp = (long)(now.AddHours(1) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            var payloadJson = $"{{\"iss\":\"{SERVICE_ACCOUNT_EMAIL}\",\"sub\":\"{SERVICE_ACCOUNT_EMAIL}\",\"aud\":\"{GOOGLE_TOKEN_URL}\",\"scope\":\"{FIREBASE_SCOPE}\",\"iat\":{iat},\"exp\":{exp}}}";

            // Encoder Header et Payload en Base64Url
            var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            var message = $"{headerBase64}.{payloadBase64}";

            // Signer avec RSA-SHA256 en utilisant BouncyCastle
            var signatureBytes = SignWithBouncyCastle(Encoding.UTF8.GetBytes(message), privateKeyPem);
            var signature = Base64UrlEncode(signatureBytes);

            return $"{message}.{signature}";
        }

        /// <summary>
        /// Signe les donnees avec RSA-SHA256 en utilisant BouncyCastle
        /// Compatible avec .NET Framework 4.8
        /// </summary>
        private static byte[] SignWithBouncyCastle(byte[] data, string privateKeyPem)
        {
            // Parser la cle PEM avec BouncyCastle
            AsymmetricKeyParameter privateKey;
            using (var reader = new StringReader(privateKeyPem))
            {
                var pemReader = new PemReader(reader);
                var keyObject = pemReader.ReadObject();
                
                if (keyObject is AsymmetricCipherKeyPair keyPair)
                {
                    privateKey = keyPair.Private;
                }
                else if (keyObject is AsymmetricKeyParameter keyParam)
                {
                    privateKey = keyParam;
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected key type: {keyObject?.GetType().Name ?? "null"}");
                }
            }

            // Creer le signataire RSA-SHA256
            var signer = SignerUtilities.GetSigner("SHA256withRSA");
            signer.Init(true, privateKey);
            signer.BlockUpdate(data, 0, data.Length);
            
            return signer.GenerateSignature();
        }

        /// <summary>
        /// Echange le JWT assertion contre un Access Token Google
        /// </summary>
        private static async Task<string> ExchangeJwtForAccessTokenAsync(string jwtAssertion)
        {
            var requestBody = new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = jwtAssertion
            };

            var content = new FormUrlEncodedContent(requestBody);
            var response = await _httpClient.PostAsync(GOOGLE_TOKEN_URL, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Google OAuth2 error: {response.StatusCode} - {responseBody}");
            }

            // Parser la reponse pour extraire access_token
            // Format: {"access_token":"...","expires_in":3600,"token_type":"Bearer"}
            var accessToken = ExtractJsonValue(responseBody, "access_token");
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new Exception("access_token not found in response");
            }

            return accessToken;
        }

        #endregion

        #region Private Methods - Configuration

        /// <summary>
        /// Retourne la cle privee PEM du Service Account
        /// Priorite: 1) Credentials embarques (SecureCredentialsService) 2) Fichier externe (debug)
        /// </summary>
        private static string GetPrivateKeyPem()
        {
            try
            {
                // PRIORITE 1: Utiliser les credentials embarques et securises
                var embeddedKey = SecureCredentialsService.GetPrivateKeyPem();
                if (!string.IsNullOrEmpty(embeddedKey))
                {
                    Logger.Log("[+] Firebase Auth: Utilisation des credentials embarques", Logger.LogLevel.DEBUG);
                    return embeddedKey;
                }

                // PRIORITE 2 (Fallback Debug): Lire depuis serviceAccountKey.json
                #if DEBUG
                var keyPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "serviceAccountKey.json"
                );

                if (System.IO.File.Exists(keyPath))
                {
                    var content = System.IO.File.ReadAllText(keyPath);
                    var privateKey = ExtractJsonValue(content, "private_key");
                    if (!string.IsNullOrEmpty(privateKey))
                    {
                        // Decoder les \n en vrais retours a la ligne
                        Logger.Log("[+] Firebase Auth: Utilisation du fichier externe (DEBUG)", Logger.LogLevel.DEBUG);
                        return privateKey.Replace("\\n", "\n");
                    }
                }
                #endif

                Logger.Log("[-] Firebase Auth: Aucune cle privee disponible", Logger.LogLevel.ERROR);
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"[-] Firebase Auth: Erreur lecture cle privee - {ex.Message}", Logger.LogLevel.ERROR);
                return null;
            }
        }

        #endregion

        #region Private Methods - Utilities

        private static string Base64UrlEncode(byte[] input)
        {
            var base64 = Convert.ToBase64String(input);
            // Convertir en Base64Url (RFC 4648)
            return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static string ExtractJsonValue(string json, string key)
        {
            // Parser simple pour extraire une valeur string
            var searchKey = $"\"{key}\"";
            var keyIndex = json.IndexOf(searchKey);
            if (keyIndex < 0) return null;

            var colonIndex = json.IndexOf(':', keyIndex + searchKey.Length);
            if (colonIndex < 0) return null;

            var valueStart = json.IndexOf('"', colonIndex + 1);
            if (valueStart < 0) return null;

            var valueEnd = valueStart + 1;
            while (valueEnd < json.Length)
            {
                if (json[valueEnd] == '"' && json[valueEnd - 1] != '\\')
                    break;
                valueEnd++;
            }

            return json.Substring(valueStart + 1, valueEnd - valueStart - 1);
        }

        #endregion
    }
}
