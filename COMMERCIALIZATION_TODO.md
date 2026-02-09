# VaultForge - Plan de Commercialisation

**Date de création**: 7 Février 2026  
**Dernière mise à jour**: 9 Février 2026  
**Statut**: En cours - Build fonctionnel ✅

---

## ✅ Étapes Complétées

| # | Tâche | Statut | Notes |
|---|-------|--------|-------|
| 1 | Analyser application source | ✅ | Architecture: 29 services, 13 modules, Firebase |
| 2 | Créer répertoire VaultForge | ✅ | `c:\Users\mohammedamine.elgala\source\repos\VaultForge` |
| 3 | Copier fichiers source | ✅ | Services, Models, Views, Modules universels |
| 4 | Modifier .csproj/.sln | ✅ | Namespace VaultForge, GUID unique, refs HVAC supprimées |
| 5 | Corriger erreurs compilation | ✅ | MainWindow.xaml.cs, SettingsService.cs, stubs créés |
| 6 | Nettoyer MainWindow.xaml | ✅ | 4 boutons HVAC masqués (Visibility="Collapsed") |
| 7 | Corriger App.xaml | ✅ | x:Class corrigé pour matcher App.xaml.cs |

---

## 🔄 Étapes Restantes

### 0. ⚠️ TRAVAIL CRITIQUE - Personnalisation des Modules (Priorité: TRÈS HAUTE)

**Problème identifié lors des tests**:
Les modules copiés ne sont PAS entièrement personnalisables. Ils contiennent:
- Des chemins hardcodés spécifiques XNRGY
- Pas de boutons "Réglages" pour permettre à l'utilisateur de modifier les paths
- Des références visuelles XNRGY dans les UI WPF (logos, textes, couleurs)

**Actions OBLIGATOIRES avant commercialisation**:

#### A. Chemins Hardcodés à Rendre Configurables

| Module | Fichier | Problème | Solution |
|--------|---------|----------|----------|
| CreateModule | `InventorCopyDesignService.cs` | Paths templates hardcodés | Ajouter settings utilisateur |
| UploadModule | `UploadModuleWindow.xaml.cs` | Paths Vault hardcodés | Bouton réglages + config |
| SmartTools | `SmartToolsService.cs` | Paths exports hardcodés | Settings window |
| DXFVerifier | `DXFVerifierWindow.xaml.cs` | Paths PDF/DXF hardcodés | Config personnalisable |
| OpenVaultProject | `VaultDownloadService.cs` | Paths workspace hardcodés | Bouton configuration |
| ConfigUnite | `ConfigUniteService.cs` | Paths config hardcodés | Settings dialog |
| ACP | `ACPWindow.xaml.cs` | Path HTML hardcodé | Rendre configurable |

**Fichiers de configuration à créer**:
```
VaultForge/
├── Config/
│   ├── UserSettings.json      ← Préférences utilisateur (paths, options)
│   ├── DefaultSettings.json   ← Valeurs par défaut
│   └── SettingsSchema.json    ← Schéma de validation
```

**Structure UserSettings.json suggérée**:
```json
{
  "paths": {
    "vaultRoot": "C:\\Vault",
    "workspaceRoot": "C:\\Vault\\Engineering",
    "templatesFolder": "$/Engineering/Templates",
    "exportFolder": "C:\\Exports",
    "projectFile": "C:\\Vault\\Vault_Project.ipj"
  },
  "vault": {
    "server": "",
    "vaultName": "",
    "defaultFolder": "$/Engineering/Projects"
  },
  "modules": {
    "createModule": {
      "defaultTemplateFolder": "",
      "defaultOutputFolder": ""
    },
    "smartTools": {
      "exportPath": "",
      "pdfOutputPath": ""
    },
    "dxfVerifier": {
      "pdfSourceFolder": "",
      "dxfSourceFolder": "",
      "reportOutputFolder": ""
    }
  }
}
```

#### B. Boutons Réglages à Ajouter dans Chaque Module

Chaque fenêtre de module doit avoir un bouton ⚙️ Réglages:

```xaml
<!-- À ajouter dans chaque ModuleWindow.xaml -->
<Button x:Name="BtnSettings" 
        Content="⚙️" 
        ToolTip="Réglages du module"
        Width="40" Height="40"
        Click="OpenModuleSettings_Click"/>
```

**Fenêtres Settings à créer**:
- [ ] `Views/Settings/GeneralSettingsWindow.xaml` - Paramètres globaux
- [ ] `Views/Settings/VaultSettingsWindow.xaml` - Configuration Vault
- [ ] `Views/Settings/PathSettingsWindow.xaml` - Chemins personnalisés
- [ ] `Modules/*/Views/*SettingsWindow.xaml` - Settings par module

#### C. Éléments UI XNRGY à Supprimer/Remplacer

**Rechercher et remplacer dans tous les fichiers XAML**:

| Élément | Fichiers concernés | Action |
|---------|-------------------|--------|
| Logo XNRGY | `MainWindow.xaml`, `SplashScreen.xaml` | Remplacer par logo VaultForge |
| Texte "XNRGY" | Tous les `.xaml` | Remplacer par "VaultForge" |
| Couleur orange XNRGY (#FF8C00) | Styles | Changer pour couleur VaultForge |
| "XNRGY Engineering Automation Tools" | Titres fenêtres | → "VaultForge" |
| "xnrgy_logo.png" | Resources | Créer/remplacer logo |
| Email @xnrgy.com | About, Contact | Supprimer ou générique |
| Mentions XNRGY Climate Systems | Footer, About | Supprimer |

**Fichiers XAML critiques à nettoyer**:
```
MainWindow.xaml
Views/SplashScreenWindow.xaml
Views/FirebaseAlertWindow.xaml
Views/UpdateDownloadWindow.xaml
Shared/Views/XnrgyMessageBox.xaml      ← Renommer en VaultForgeMessageBox
Shared/Views/LoginWindow.xaml
Shared/Views/PreviewWindow.xaml
Modules/*/Views/*.xaml                  ← Tous les modules
Styles/XnrgyStyles.xaml                 ← Renommer en VaultForgeStyles.xaml
```

**Commande pour trouver toutes les références XNRGY**:
```powershell
# Rechercher dans les XAML
Get-ChildItem -Path "c:\Users\mohammedamine.elgala\source\repos\VaultForge" -Recurse -Include "*.xaml" | 
    Select-String -Pattern "XNRGY|xnrgy|Xnrgy" | 
    Select-Object Path, LineNumber, Line

# Rechercher dans les CS
Get-ChildItem -Path "c:\Users\mohammedamine.elgala\source\repos\VaultForge" -Recurse -Include "*.cs" | 
    Select-String -Pattern "XNRGY|xnrgy|Xnrgy" | 
    Select-Object Path, LineNumber, Line
```

#### D. Services à Modifier pour Paths Dynamiques

**VaultSettingsService.cs** - Lignes critiques:
```csharp
// AVANT (hardcodé XNRGY):
private const string VAULT_APP_FOLDER = "$/Engineering/Inventor_Standards/Automation_Standard/Configuration_Files/XnrgyEngineeringAutomationToolsApp";
private const string LOCAL_APP_FOLDER = @"C:\Vault\Engineering\Inventor_Standards\Automation_Standard\Configuration_Files\XnrgyEngineeringAutomationToolsApp";

// APRÈS (configurable):
private string VaultAppFolder => UserSettings.Instance.Paths.VaultAppFolder ?? "$/VaultForge/Config";
private string LocalAppFolder => UserSettings.Instance.Paths.LocalAppFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VaultForge", "Config");
```

**UserPreferencesManager.cs** - Lignes critiques:
```csharp
// AVANT:
"XnrgyEngineeringAutomationTools"

// APRÈS:
"VaultForge"
```

**CredentialsManager.cs** - Lignes critiques:
```csharp
// AVANT:
"XnrgyEngineeringAutomationTools"

// APRÈS:
"VaultForge"
```

---

### 1. Créer Firebase Commercial (Priorité: HAUTE)

**Objectif**: Nouveau projet Firebase séparé pour VaultForge

**Actions à faire**:
- [ ] Créer projet Firebase: `vaultforge-commercial` sur https://console.firebase.google.com
- [ ] Configurer Realtime Database avec structure licensing
- [ ] Générer nouveau `serviceAccountKey.json` et le placer dans:
  ```
  VaultForge\Firebase Realtime Database configuration\serviceAccountKey.json
  ```
- [ ] Configurer les règles de sécurité Firebase
- [ ] Créer admin-panel HTML pour gérer les licences

**Structure Firebase recommandée**:
```json
{
  "licenses": {
    "LICENSE_KEY_1": {
      "email": "client@company.com",
      "company": "Company Name",
      "type": "professional|enterprise|trial",
      "expirationDate": "2027-02-09",
      "maxDevices": 5,
      "activatedDevices": ["DEVICE_ID_1", "DEVICE_ID_2"],
      "features": ["smartTools", "dxfVerifier", "vaultUpload", "createModule"],
      "isActive": true
    }
  },
  "devices": {
    "DEVICE_ID": {
      "licenseKey": "LICENSE_KEY_1",
      "machineName": "WORKSTATION-01",
      "userName": "john.doe",
      "lastSeen": "2026-02-09T10:30:00Z",
      "appVersion": "1.0.0"
    }
  },
  "appConfig": {
    "latestVersion": "1.0.0",
    "minVersion": "1.0.0",
    "downloadUrl": "https://...",
    "maintenance": false
  },
  "auditLog": {
    "...": "logs d'utilisation"
  }
}
```

---

### 2. Système de Licensing (Priorité: HAUTE)

**Objectif**: Implémenter validation de licences avec trial period

**Fichiers à modifier/créer**:
- [ ] `Services/LicensingService.cs` - Nouveau service de gestion des licences
- [ ] `Views/LicenseActivationWindow.xaml` - Fenêtre d'activation
- [ ] `Views/TrialExpiredWindow.xaml` - Fenêtre trial expiré
- [ ] `Models/LicenseInfo.cs` - Modèle de licence

**Fonctionnalités à implémenter**:
- [ ] Validation de clé de licence au démarrage
- [ ] Trial period de 14 jours (ou configurable)
- [ ] Activation par clé de licence (format: `VF-XXXX-XXXX-XXXX-XXXX`)
- [ ] Vérification du nombre de devices activés
- [ ] Blocage si licence expirée ou invalide
- [ ] Mode offline avec cache local (grace period 7 jours)

**Flux de démarrage**:
```
App.OnStartup()
  → LicensingService.ValidateLicense()
    → Si pas de licence: Afficher TrialWindow ou ActivationWindow
    → Si trial expiré: Bloquer avec message
    → Si licence valide: Continuer normalement
    → Si licence expirée: Afficher renouvellement
```

---

### 3. Rebranding Complet (Priorité: MOYENNE)

**Objectif**: Remplacer toutes les références "XnrgyEngineeringAutomationTools" par "VaultForge"

**Statistiques actuelles**:
- ~303 occurrences dans ~133 fichiers
- Principalement dans les `namespace` et `using` statements

**Fichiers critiques à modifier**:
```
Services/*.cs           - namespace
Models/*.cs             - namespace
Views/*.xaml.cs         - namespace + using
Modules/**/*.cs         - namespace + using
Converters/*.cs         - namespace
ViewModels/*.cs         - namespace
Shared/**/*.cs          - namespace + using
```

**Méthode recommandée**:
1. Utiliser Visual Studio "Find and Replace in Files" (Ctrl+Shift+H)
2. Pattern: `XnrgyEngineeringAutomationTools` → `VaultForge`
3. Inclure: `*.cs, *.xaml`
4. Exclure: `bin\, obj\, Backups\`
5. Rebuild complet après

**⚠️ ATTENTION**: Ne PAS utiliser PowerShell pour le remplacement (corruption d'encodage)

---

### 4. Documentation Commerciale (Priorité: MOYENNE)

**Documents à créer**:

- [ ] `README.md` - Description produit, features, requirements
- [ ] `CHANGELOG.md` - Historique des versions
- [ ] `INSTALLATION.md` - Guide d'installation pas à pas
- [ ] `LICENSE.md` - Conditions de licence commerciale
- [ ] `EULA.txt` - End User License Agreement
- [ ] `PRIVACY.md` - Politique de confidentialité (télémétrie)

**Contenu README.md suggéré**:
```markdown
# VaultForge

Professional Autodesk Vault & Inventor Automation Suite

## Features
- Smart Tools for Inventor
- DXF Verifier
- Vault Upload Module
- Create Module (Pack & Go)
- Open Vault Project
- Update Workspace
- Config Unite

## Requirements
- Autodesk Inventor Professional 2026+
- Autodesk Vault Professional 2026+
- Windows 10/11 64-bit
- .NET Framework 4.8
- Internet connection (for license validation)

## Installation
...

## License
Commercial license required. Contact sales@...
```

---

### 5. Préparation Autodesk App Store (Priorité: BASSE)

**Objectif**: Préparer le package pour publication sur Autodesk App Store

**Éléments requis**:
- [ ] Icône application (256x256, 128x128, 64x64, 32x32)
- [ ] Screenshots (1920x1080 minimum)
- [ ] Vidéo démo (optionnel mais recommandé)
- [ ] Description courte (150 caractères)
- [ ] Description longue (2000 caractères)
- [ ] Manifeste d'application (.addin ou PackageContents.xml)
- [ ] Installateur MSI/EXE signé

**Catégorie suggérée**: "Data Management" ou "Productivity"

---

### 6. Test Complet (Priorité: HAUTE)

**Modules à tester**:
- [ ] Smart Tools - Toutes les fonctionnalités
- [ ] DXF Verifier - Validation PDF/DXF
- [ ] Upload Module - Upload vers Vault
- [ ] Create Module - Pack & Go / Copy Design
- [ ] Open Vault Project - Téléchargement depuis Vault
- [ ] Update Workspace - Synchronisation
- [ ] Upload Template - Upload templates
- [ ] Config Unite - Configuration des unités
- [ ] ACP - Assistant de Conception

**Tests spécifiques**:
- [ ] Connexion Vault (login/logout)
- [ ] Connexion Inventor (détection version)
- [ ] Firebase (télémétrie, audit)
- [ ] Thème sombre/clair
- [ ] Multi-écran
- [ ] Performances sur gros projets

---

## 📁 Structure Actuelle VaultForge

```
VaultForge/
├── VaultForge.sln
├── VaultForge.csproj
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / MainWindow.xaml.cs
├── bin/Release/VaultForge.exe          ← EXÉCUTABLE
├── Services/                            (29 services)
├── Models/
├── Views/
├── ViewModels/
├── Converters/
├── Shared/
├── Styles/
├── Resources/
├── Assets/
├── Modules/
│   ├── CreateModule/        ✅
│   ├── UploadModule/        ✅
│   ├── SmartTools/          ✅
│   ├── DXFVerifier/         ✅
│   ├── OpenVaultProject/    ✅
│   ├── UpdateWorkspace/     ✅
│   ├── UploadTemplate/      ✅
│   ├── ConfigUnite/         ✅
│   └── ACP/                 ✅
└── Firebase Realtime Database configuration/
    └── serviceAccountKey.json (PLACEHOLDER - à remplacer)
```

---

## ❌ Modules Supprimés (HVAC-spécifiques)

Ces modules sont masqués dans l'UI (Visibility="Collapsed"):
- PlaceEquipment
- BuildModule
- NestingModule
- ChecklistHVAC

---

## 🔧 Commandes Utiles

```powershell
# Build VaultForge
cd "c:\Users\mohammedamine.elgala\source\repos\VaultForge"
& "C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe" VaultForge.csproj /t:Rebuild /p:Configuration=Release /m /v:minimal

# Lancer VaultForge
Start-Process "c:\Users\mohammedamine.elgala\source\repos\VaultForge\bin\Release\VaultForge.exe"

# Terminer processus si bloqué
Get-Process | Where-Object {$_.ProcessName -like "*VaultForge*"} | Stop-Process -Force
```

---

## 📞 Contact & Notes

**Auteur original**: Mohammed Amine Elgalai - XNRGY Climate Systems ULC  
**Projet commercial**: VaultForge  
**Basé sur**: XnrgyEngineeringAutomationTools

---

## 📊 Statistiques de Nettoyage Requises

| Type | Occurrences | Action |
|------|-------------|--------|
| Références "XNRGY" dans `.xaml` | **322** | Remplacer par VaultForge |
| Références "XNRGY" dans `.cs` | **751** | Remplacer namespaces + textes |
| Chemins hardcodés (`C:\Vault`, `$/Engineering`) | **134** | Rendre configurables |
| **TOTAL** | **~1207** | Travail significatif requis |

**Estimation temps de travail**: 2-3 jours pour nettoyage complet

---

*Document créé automatiquement le 9 Février 2026*
