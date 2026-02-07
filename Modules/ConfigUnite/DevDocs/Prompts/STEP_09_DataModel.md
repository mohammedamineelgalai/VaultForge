# 🔧 STEP 9: Mettre à jour ConfigUniteDataModel

**Agent**: Cursor / Zed / Autre  
**Difficulté**: ⭐⭐⭐ (Moyen)  
**Temps estimé**: 25-30 minutes  
**Validation par**: GitHub Copilot

---

## 🎯 OBJECTIF

Compléter le `ConfigUniteDataModel.cs` pour inclure toutes les propriétés nécessaires aux nouveaux tabs créés.

---

## 📁 FICHIERS À MODIFIER

| Fichier | Action | Description |
|---------|--------|-------------|
| `Modules/ConfigUnite/Models/ConfigUniteDataModel.cs` | MODIFIER | Ajouter les classes et propriétés |

---

## 📋 STRUCTURE CIBLE

```csharp
ConfigUniteDataModel
├── UnitSpecification (existant - vérifier complétude)
├── WallSpecificationData (NOUVEAU)
│   ├── CustomizeWalls
│   ├── InteriorWalls (WallDirectionSet)
│   ├── AdditionalWall02 (WallDirectionSet + Tunnels)
│   ├── AdditionalWall03-05
├── ModularBracketsData (NOUVEAU)
├── EquipmentsData (NOUVEAU)
│   ├── SupplyFan
│   ├── ReturnFan
│   ├── HeatingCoil
│   ├── CoolingCoil
│   └── Filtration
└── RetrievedValuesData (NOUVEAU - read-only)
```

---

## 🔨 INSTRUCTIONS DÉTAILLÉES

### ÉTAPE 1: Ouvrir le fichier ConfigUniteDataModel.cs

Chemin: `Modules/ConfigUnite/Models/ConfigUniteDataModel.cs`

### ÉTAPE 2: Ajouter les nouvelles classes

Ajouter après les classes existantes:

```csharp
// =====================================================
// WALL SPECIFICATION DATA
// =====================================================

/// <summary>
/// Configuration d'un mur dans une direction specifique
/// </summary>
public class WallDirectionConfig
{
    public int Quantity { get; set; } = 1;
    public string Panel { get; set; } = "2 in";
    public string Material { get; set; } = "Galvanized Steel";
    public string Opening { get; set; } = "None";
    public bool IsInsulated { get; set; } = true;
}

/// <summary>
/// Ensemble de configurations pour les 5 directions de murs
/// </summary>
public class WallDirectionSet
{
    public WallDirectionConfig Back { get; set; } = new WallDirectionConfig();
    public WallDirectionConfig Front { get; set; } = new WallDirectionConfig();
    public WallDirectionConfig Right { get; set; } = new WallDirectionConfig();
    public WallDirectionConfig Left { get; set; } = new WallDirectionConfig();
    public WallDirectionConfig Roof { get; set; } = new WallDirectionConfig();
}

/// <summary>
/// Configuration des tunnels pour les murs additionnels
/// </summary>
public class TunnelConfig
{
    public bool TunnelRight { get; set; } = false;
    public bool TunnelLeft { get; set; } = false;
    public bool TunnelMiddle { get; set; } = false;
}

/// <summary>
/// Configuration complete des murs additionnels (Wall 02-05)
/// </summary>
public class AdditionalWallConfig
{
    public WallDirectionSet Walls { get; set; } = new WallDirectionSet();
    public TunnelConfig Tunnels { get; set; } = new TunnelConfig();
}

/// <summary>
/// Donnees completes de specification des murs
/// </summary>
public class WallSpecificationData
{
    public bool CustomizeWalls { get; set; } = false;
    public WallDirectionSet InteriorWalls { get; set; } = new WallDirectionSet();
    public AdditionalWallConfig AdditionalWall02 { get; set; } = new AdditionalWallConfig();
    public AdditionalWallConfig AdditionalWall03 { get; set; } = new AdditionalWallConfig();
    public AdditionalWallConfig AdditionalWall04 { get; set; } = new AdditionalWallConfig();
    public AdditionalWallConfig AdditionalWall05 { get; set; } = new AdditionalWallConfig();
}

// =====================================================
// MODULAR BRACKETS DATA
// =====================================================

/// <summary>
/// Configuration des supports modulaires
/// </summary>
public class ModularBracketsData
{
    public string BracketType { get; set; } = "Standard";
    public int Quantity { get; set; } = 4;
    public string Material { get; set; } = "Galvanized Steel";
    
    public bool AutoPosition { get; set; } = true;
    public double Spacing { get; set; } = 1000; // mm
    public string Alignment { get; set; } = "Center";
    
    public bool IncludeCornerBrackets { get; set; } = false;
    public bool ReinforcedBrackets { get; set; } = false;
    public bool SeismicRated { get; set; } = false;
}

// =====================================================
// EQUIPMENTS DATA
// =====================================================

/// <summary>
/// Configuration d'un ventilateur
/// </summary>
public class FanConfig
{
    public bool Included { get; set; } = false;
    public string FanType { get; set; } = "Plenum";
    public double CFM { get; set; } = 0;
    public string MotorType { get; set; } = "Standard Efficiency";
}

/// <summary>
/// Configuration d'une serpentine de chauffage
/// </summary>
public class HeatingCoilConfig
{
    public bool Included { get; set; } = false;
    public string CoilType { get; set; } = "Hot Water";
    public double Capacity { get; set; } = 0; // kW ou MBH
}

/// <summary>
/// Configuration d'une serpentine de refroidissement
/// </summary>
public class CoolingCoilConfig
{
    public bool Included { get; set; } = false;
    public string CoilType { get; set; } = "Chilled Water";
    public double Capacity { get; set; } = 0; // Tons
    public int Rows { get; set; } = 4;
}

/// <summary>
/// Configuration de la filtration
/// </summary>
public class FiltrationConfig
{
    public bool IncludePreFilter { get; set; } = false;
    public string PreFilterMERV { get; set; } = "MERV 8";
    public bool IncludeFinalFilter { get; set; } = false;
    public string FinalFilterMERV { get; set; } = "MERV 13";
}

/// <summary>
/// Donnees completes des equipements
/// </summary>
public class EquipmentsData
{
    public FanConfig SupplyFan { get; set; } = new FanConfig();
    public FanConfig ReturnFan { get; set; } = new FanConfig();
    public HeatingCoilConfig HeatingCoil { get; set; } = new HeatingCoilConfig();
    public CoolingCoilConfig CoolingCoil { get; set; } = new CoolingCoilConfig();
    public FiltrationConfig Filtration { get; set; } = new FiltrationConfig();
}

// =====================================================
// RETRIEVED VALUES DATA (Read-Only)
// =====================================================

/// <summary>
/// Valeurs recuperees depuis le fichier source (lecture seule)
/// </summary>
public class RetrievedValuesData
{
    // Source Info
    public string SourceFilePath { get; set; } = string.Empty;
    public DateTime? LastRetrievedDate { get; set; } = null;
    public string FileVersion { get; set; } = string.Empty;
    
    // Dimensions
    public double RetrievedLength { get; set; } = 0;
    public double RetrievedWidth { get; set; } = 0;
    public double RetrievedHeight { get; set; } = 0;
    public double RetrievedVolume => (RetrievedLength * RetrievedWidth * RetrievedHeight) / 1000000000; // m3
    
    // Performance
    public double TotalCFM { get; set; } = 0;
    public double StaticPressure { get; set; } = 0; // inWG
    public double FanPower { get; set; } = 0; // HP
    public double TotalWeight { get; set; } = 0; // lbs
}
```

### ÉTAPE 3: Mettre à jour la classe principale ConfigUniteDataModel

Ajouter les nouvelles propriétés à la classe principale:

```csharp
public class ConfigUniteDataModel
{
    // Proprietes existantes (garder tel quel)
    // ...
    
    // Nouvelles proprietes
    public WallSpecificationData WallSpecification { get; set; } = new WallSpecificationData();
    public ModularBracketsData ModularBrackets { get; set; } = new ModularBracketsData();
    public EquipmentsData Equipments { get; set; } = new EquipmentsData();
    public RetrievedValuesData RetrievedValues { get; set; } = new RetrievedValuesData();
    
    // Methode utilitaire pour verifier la completude
    public bool IsConfigurationComplete()
    {
        // Verifier les champs obligatoires
        return !string.IsNullOrEmpty(UnitType) && 
               !string.IsNullOrEmpty(UnitOption) &&
               AhuLength > 0 &&
               AhuWidth > 0 &&
               AhuHeight > 0;
    }
    
    // Methode pour calculer le nombre de composants
    public int GetComponentCount()
    {
        int count = 0;
        if (Equipments.SupplyFan.Included) count++;
        if (Equipments.ReturnFan.Included) count++;
        if (Equipments.HeatingCoil.Included) count++;
        if (Equipments.CoolingCoil.Included) count++;
        if (Equipments.Filtration.IncludePreFilter || Equipments.Filtration.IncludeFinalFilter) count++;
        return count;
    }
}
```

### ÉTAPE 4: Vérifier les imports nécessaires

En haut du fichier:

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
```

---

## ✅ CRITÈRES DE VALIDATION

- [ ] Toutes les nouvelles classes sont ajoutées
- [ ] La classe principale a les 4 nouvelles propriétés
- [ ] Les méthodes utilitaires fonctionnent
- [ ] Aucune erreur de compilation
- [ ] Build réussi avec `.\build-and-run.ps1 -BuildOnly`

---

## 🔧 COMMANDE DE BUILD

```powershell
cd "c:\Users\mohammedamine.elgala\source\repos\XnrgyEngineeringAutomationTools"
.\build-and-run.ps1 -BuildOnly
```

---

## ⚠️ RÈGLES IMPORTANTES

1. **NE PAS supprimer** les propriétés existantes
2. **AJOUTER** seulement les nouvelles classes et propriétés
3. **Garder** les valeurs par défaut cohérentes
4. **Pas d'emojis** dans le code C#

---

## 📝 QUAND TERMINÉ

Signaler:
> "STEP 9 terminé. ConfigUniteDataModel mis à jour avec WallSpecification, ModularBrackets, Equipments et RetrievedValues."

---

*Prompt créé par GitHub Copilot - 2026-01-28*
