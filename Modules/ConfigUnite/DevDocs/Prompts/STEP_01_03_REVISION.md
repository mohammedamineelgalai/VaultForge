# STEP 1-3 REVISION: Correction Design Info + Unit Specification

## COMMANDES SPEC KIT

```bash
# Pour corriger STEP 1-3:
/speckit.reimplement STEP_01_03

# Chemin complet:
/speckit.implement c:\Users\mohammedamine.elgala\source\repos\XnrgyEngineeringAutomationTools\Modules\ConfigUnite\DevDocs\Prompts\STEP_01_03_REVISION.md
```

---

**Agent**: GLM-4.7 via Cline  
**Difficulte**: 3/5  
**Temps estime**: 45 minutes  
**Reference**: CONFIG_UNITE_MASTER.md  

---

## OBJECTIF

Corriger les STEP 1-3 deja implementes pour:
1. Utiliser les **noms de parametres iLogic EXACTS**
2. Utiliser les **listes deroulantes EXACTES** depuis 000000000-params.xml
3. Respecter la **structure hierarchique** de Box Module iLogic

**NE PAS CASSER** ce qui fonctionne deja - SEULEMENT corriger les noms et listes.

---

## FICHIERS A MODIFIER

| Fichier | Action |
|---------|--------|
| `Modules/ConfigUnite/Views/ConfigUniteWindow.xaml` | Corriger les noms x:Name et listes |
| `Modules/ConfigUnite/Views/ConfigUniteWindow.xaml.cs` | Corriger initialisations ComboBox |
| `Modules/ConfigUnite/Models/ConfigUniteDataModel.cs` | Corriger noms proprietes |

---

## PARTIE 1: DESIGN INFO (Header)

### Structure iLogic (depuis captures)

```
Design Info
├── fx: Project Number      -> Numero_de_Projet_Form
├── fx: Drafter Name        -> Initiale_du_Dessinateur_Form
├── fx: Co-Drafter Name     -> Initiale_du_Co_Dessinateur_Form
└── fx: Creation Date       -> Creation_Date_Form
```

### Corrections XAML

Verifier que les controles ont les bons x:Name:

```xml
<!-- Project Number -->
<TextBox x:Name="TxtProjectNumber" ... />
<!-- Binding vers: Numero_de_Projet_Form -->

<!-- Drafter Name -->
<ComboBox x:Name="CmbDrafterName" ... />
<!-- Binding vers: Initiale_du_Dessinateur_Form -->

<!-- Co-Drafter Name -->
<ComboBox x:Name="CmbCoDrafterName" ... />
<!-- Binding vers: Initiale_du_Co_Dessinateur_Form -->

<!-- Creation Date -->
<DatePicker x:Name="DpCreationDate" ... />
<!-- Binding vers: Creation_Date_Form -->
```

### Liste Deroulante: Initiale_du_Dessinateur_Form

```csharp
// Dans InitializeComboBoxes() ou equivalent
CmbDrafterName.ItemsSource = new List<string>
{
    "N/A", "AC", "AM", "AR", "CC", "DC", "DL", "FL", "IM", "KB", 
    "KJ", "MAE", "MC", "NJ", "RO", "SB", "TG", "TV", "VK", "YS"
};
CmbDrafterName.SelectedItem = "N/A";

// Meme liste pour Co-Drafter
CmbCoDrafterName.ItemsSource = CmbDrafterName.ItemsSource;
CmbCoDrafterName.SelectedItem = "N/A";
```

---

## PARTIE 2: UNIT SPECIFICATION > UNIT INFO

### Structure iLogic (depuis captures)

```
Unit Specification
└── Unit Info
    ├── fx: CRAH Unit                   -> Crah_Unit_Form (CheckBox)
    ├── fx: Unit Type                   -> Unit_Type_Form
    ├── Airflow (Group)
    │   ├── fx: 1 Tunnel (Right)        -> Tunnel_Right_Form (CheckBox)
    │   ├── fx: Air Flow (Right)        -> AirFlow_Right_Form
    │   ├── fx: 2 Tunnels (Left)        -> Tunnel_Left_Form (CheckBox)
    │   ├── fx: AirFlow (Left)          -> AirFlow_Left_Form
    │   ├── fx: 3 Tunnels (Middle)      -> Tunnel_Middle_Form (CheckBox)
    │   └── fx: AirFlow (Middle)        -> AirFlow_Middle_Form
    ├── fx: Unit Option                 -> Unit_Option_Form
    ├── fx: Unit Design Pressure        -> StaticPressure_Form
    ├── fx: Unit Configuration          -> Unit_Configuration_Form
    ├── fx: Unit Certification          -> Unit_Certification_Form
    └── fx: Factory Testing             -> Factory_Testing_Form
```

### Listes Deroulantes Unit Info

```csharp
// Unit_Type_Form
CmbUnitType.ItemsSource = new List<string>
{
    "Outdoor/ Exterieur",
    "Interior/Interieur",
    "None/Aucun"
};

// AirFlow Direction (pour Right, Left, Middle)
var airflowOptions = new List<string>
{
    "Front To Back",
    "Back To Front"
};
CmbAirFlowRight.ItemsSource = airflowOptions;
CmbAirFlowLeft.ItemsSource = airflowOptions;
CmbAirFlowMiddle.ItemsSource = airflowOptions;

// Unit_Option_Form
CmbUnitOption.ItemsSource = new List<string>
{
    "Standard",
    "None/Aucun",
    "Washable/Lavable (aucune vis au plancher)"
};

// StaticPressure_Form (Unit Design Pressure)
CmbStaticPressure.ItemsSource = new List<string>
{
    "5 ul",
    "8 ul",
    "10 ul",
    "12 ul"
};
CmbStaticPressure.SelectedItem = "12 ul";

// Unit_Configuration_Form
CmbUnitConfiguration.ItemsSource = new List<string>
{
    "Unstacked/Standard",
    "Stacked/ Empile",
    "None/Aucun"
};

// Unit_Certification_Form
CmbUnitCertification.ItemsSource = new List<string>
{
    "Yes/Oui (Voir Notes)",
    "No/Non",
    "None/Aucun"
};

// Factory_Testing_Form
CmbFactoryTesting.ItemsSource = new List<string>
{
    "Yes/Oui",
    "No/Non",
    "None/Aucun"
};
```

---

## PARTIE 3: UNIT SPECIFICATION > FLOOR INFO

### Structure iLogic (depuis captures)

```
Unit Specification
└── Floor Info
    ├── fx: Base Construction           -> PerimeterMaterial_Form
    ├── fx: Base Insulation             -> Floor_Insulation_Form
    ├── fx: Floor Height                -> Floor_Height_Form
    ├── fx: Base Coating                -> Coating_Floor_Form
    ├── fx: Base Thermal Break          -> Base_Thermal_Break_Form
    ├── fx: Floor Construction          -> Floor_Construction_Form
    ├── fx: Floor Liner                 -> FloorLiner_SMStyleName_Form
    ├── fx: Subfloor Liner              -> FloorSubLiner_SMStyleName_Form
    ├── fx: Auxiliary Drains            -> DrainType_Form
    ├── fx: Cross Member Width          -> Cross_Member_Width_Form
    └── fx: Floor Mount Type            -> Floor_Mount_Type_Form
```

### Listes Deroulantes Floor Info

```csharp
// Floor_Insulation_Form
CmbFloorInsulation.ItemsSource = new List<string>
{
    "None/Aucun",
    "Injected Polyurethane/Mousse Injecte",
    "Fiberglass/ Isolation en Fibre de Verre",
    "Special (See Note)/ Special (voir Notes)"
};

// Floor_Height_Form
CmbFloorHeight.ItemsSource = new List<string>
{
    "4 in",
    "5 in",
    "6 in",
    "8 in",
    "10 in",
    "12 in"
};
CmbFloorHeight.SelectedItem = "8 in";

// Base_Thermal_Break_Form
CmbBaseThermalBreak.ItemsSource = new List<string>
{
    "None/Aucun",
    "Standard (Gasket)",
    "Special See Submittal/ Special Voir ''Submittal''",
    "NTM J-Plastic ou Polyblock (See Note Importante)/ NTM  J-Plastic ou Polyblock (Voir Note Importante)"
};

// Floor_Mount_Type_Form
CmbFloorMountType.ItemsSource = new List<string>
{
    "Steel_Dunnage/Egale au Perimetre",
    "Roof_Curb/Egale a la sous-Tole de Plancher",
    "None/Aucun"
};

// Coating_Floor_Form (Base Coating)
CmbBaseCoating.ItemsSource = new List<string>
{
    "No Paint/ Non Peint",
    "Xnrgy White Paint/ Peint Blanc Xnrgy",
    "Special Paint Color/ Paint CouleurSpecial"
};
```

---

## PARTIE 4: UNIT SPECIFICATION > CASING INFO

### Structure iLogic (depuis captures)

```
Unit Specification
└── Casing Info
    ├── fx: Panel Construction          -> Panel_Construction_Form
    ├── fx: Panel Width                 -> Panel_Width_Form
    ├── fx: Panel Insulation            -> Panel_Insulation_Form
    ├── Panel & Liner Material (Group)
    │   ├── Wall mtl
    │   │   ├── fx: Wall Panel Material -> WallPanelSMStyleName_Form
    │   │   └── fx: Wall Liner Material -> WallLinerSMStyleName_Form
    │   └── Roof mtl
    │       ├── fx: Roof Panel Mtl      -> RoofPanelSMStyleName_Form
    │       └── fx: Roof Liner Mtl      -> RoofLinerSMStyleName_Form
    ├── fx: Coating                     -> Coating_Form
    ├── fx: Single Panel Length         -> SinglePanel_Length_Form
    ├── fx: Wall Material               -> Wall_Material_Form
    └── fx: Critical Panel Length       -> CriticalPanel_Length_Form
```

### Listes Deroulantes Casing Info

```csharp
// Panel_Construction_Form
CmbPanelConstruction.ItemsSource = new List<string>
{
    "None/Aucun",
    "Standard (Gasket)",
    "NTM (J-Plastic)",
    "Special See Submittal/ Special Voir ''Submittal''"
};

// Panel_Width_Form
CmbPanelWidth.ItemsSource = new List<string>
{
    "2 in",
    "3 in",
    "4 in"
};
CmbPanelWidth.SelectedItem = "2 in";

// Panel_Insulation_Form
CmbPanelInsulation.ItemsSource = new List<string>
{
    "None/Aucun",
    "Injected Polyurethane/ Mousse Injecte",
    "Fiberglass/ Isolation en Fibre de Verre (Caulking pour retenir)",
    "Foam_Board_Insulation",
    "Special (See Note)/ Special (voir Notes)"
};

// Coating_Form
CmbCoating.ItemsSource = new List<string>
{
    "No Paint/ Non Peint",
    "Xnrgy White Paint/ Peint Blanc Xnrgy",
    "Special Paint Color/ Paint CouleurSpecial"
};

// Wall_Material_Form
CmbWallMaterial.ItemsSource = new List<string>
{
    "Aluminium",
    "Galvanized",
    "Stainless"
};

// WallPanelSMStyleName_Form (Wall Panel Material)
CmbWallPanelMaterial.ItemsSource = new List<string>
{
    "None/Aucun",
    "ALUM_12GA_0.080",
    "ALUM_12GA_0.080_PC",
    "ALUM_14GA_0.063",
    "ALUM_14GA_0.063_PC",
    "ALUMTEXT_12GA_0.080",
    "ALUMTEXT_12GA_0.080_PC",
    "ALUMTEXT_14GA_0.063",
    "ALUMTEXT_14GA_0.063_PC",
    "GALV_14GA_0.078",
    "GALV_14GA_0.078_PC",
    "GALV_16GA_0.063",
    "GALV_16GA_0.063_PC",
    "SS304_14GA_0.078",
    "SS304_14GA_0.078_PC",
    "SS304_16GA_0.063",
    "SS304_16GA_0.063_PC",
    "SS316_14GA_0.078",
    "SS316_14GA_0.078_PC",
    "SS316_16GA_0.063",
    "SS316_16GA_0.063_PC"
};

// WallLinerSMStyleName_Form (Wall Liner Material) - Liste complete
CmbWallLinerMaterial.ItemsSource = new List<string>
{
    "None/Aucun",
    "ALUM_10GA_0.100", "ALUM_12GA_0.080", "ALUM_14GA_0.063", "ALUM_16GA_0.050", "ALUM_18GA_0.040",
    "ALUM_10GA_0.100_PC", "ALUM_12GA_0.080_PC", "ALUM_14GA_0.063_PC", "ALUM_16GA_0.050_PC", "ALUM_18GA_0.040_PC",
    "ALUMPERF23_14GA_0.063", "ALUMPERF23_16GA_0.050", "ALUMPERF23_18GA_0.040",
    "ALUMPERF51_14GA_0.063", "ALUMPERF51_16GA_0.050", "ALUMPERF51_18GA_0.040",
    "GALV_10GA_0.138", "GALV_12GA_0.108", "GALV_14GA_0.078", "GALV_16GA_0.063", "GALV_18GA_0.051", "GALV_20GA_0.039", "GALV_22GA_0.033",
    "SS304_10GA_0.138", "SS304_12GA_0.108", "SS304_14GA_0.078", "SS304_16GA_0.063", "SS304_18GA_0.051", "SS304_20GA_0.039", "SS304_22GA_0.033",
    "SS316_10GA_0.138", "SS316_12GA_0.108", "SS316_14GA_0.078", "SS316_16GA_0.063", "SS316_18GA_0.051", "SS316_20GA_0.039", "SS316_22GA_0.033"
};

// RoofPanelSMStyleName_Form - Meme liste que WallPanelSMStyleName_Form
CmbRoofPanelMaterial.ItemsSource = CmbWallPanelMaterial.ItemsSource;

// RoofLinerSMStyleName_Form - Meme liste que WallLinerSMStyleName_Form
CmbRoofLinerMaterial.ItemsSource = CmbWallLinerMaterial.ItemsSource;
```

---

## PARTIE 5: UNIT SPECIFICATION > MISCELLANEOUS

### Structure iLogic (depuis captures)

```
Unit Specification
└── Miscellaneous
    └── fx: Hardware Material           -> Hardware_Material_Form
```

### Liste Deroulante Miscellaneous

```csharp
// Hardware_Material_Form
CmbHardwareMaterial.ItemsSource = new List<string>
{
    "None / Aucun",
    "Standard According to Material / Materiel Selon Standard",
    "Stainless Steel / Acier Inoxydable"
};
```

---

## PARTIE 6: MODULE DIMENSIONS

### Structure iLogic (depuis captures)

```
Module Dimensions
├── Row 2 > Group 2
│   ├── fx: Module_Width            -> Module_Width_Form
│   ├── fx: Module_Depth            -> Module_Depth_Form
│   └── fx: Module_Height           -> Module_Height_Form
└── Group 1 > Group 5
    ├── Module Position (Label)
    ├── fx: Module_Type             -> Module_Type_Form
    └── Wall & Roof Requirement
        ├── fx: Front_Wall_01       -> Front_Wall_01_Form
        ├── fx: Back_Wall_01        -> Back_Wall_01_Form
        ├── fx: Left_Wall_01        -> Left_Wall_01_Form
        ├── fx: Right_Wall_01       -> Right_Wall_01_Form
        └── fx: Roof_01             -> Roof_01_Form
```

### Verifications Module Dimensions

Les dimensions (Width, Depth, Height) sont des TextBox numeriques.
Les Wall_01 et Roof_01 sont des ComboBox ou CheckBox selon le contexte.

---

## REGLES CRITIQUES

1. **NE PAS CASSER** le code existant - seulement corriger les listes
2. **Utiliser les valeurs EXACTES** du fichier 000000000-params.xml
3. **Pas d'emojis** dans le code C# (Logger, Console)
4. **Build avec**: `.\build-and-run.ps1 -BuildOnly`

---

## VALIDATION

Apres implementation:
1. Build doit reussir
2. Toutes les ComboBox doivent avoir les listes correctes
3. Les valeurs par defaut doivent etre "None/Aucun" ou selon le params.xml

---

## COMMANDE POUR CLINE

```
Lis et implemente ce fichier de correction. 
Modifie UNIQUEMENT les listes deroulantes (ItemsSource) dans le code-behind.
NE TOUCHE PAS au XAML layout, seulement les initialisations des ComboBox.
Reference: CONFIG_UNITE_MASTER.md pour les noms iLogic.
Build avec: .\build-and-run.ps1 -BuildOnly
```
