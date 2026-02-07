# CONFIG UNITE - MASTER REFERENCE (Chef d'Orchestre)

**Module**: Config Unite - Master Configuration System  
**Version**: v2.0.0  
**Date Creation**: 2026-01-26  
**Derniere MAJ**: 2026-01-28  
**Auteur**: Mohammed Amine Elgalai - XNRGY Climate Systems ULC  
**Chef d'Orchestre**: GitHub Copilot  

---

## UTILISATION DE CE FICHIER

Ce fichier est le **MASTER REFERENCE** pour tout developpement du module Config Unite.
**TOUJOURS inclure ce fichier comme reference dans les demandes de developpement.**

```
REGLE D'OR: Ce fichier = Aide-memoire du Chef d'Orchestre (Copilot)
- Contient TOUS les noms de parametres iLogic EXACTS
- Contient TOUTES les listes deroulantes depuis 000000000-params.xml
- Contient la structure hierarchique complete de Box Module
- Contient les regles de developpement a respecter
```

---

## TABLE DES MATIERES

1. [Regles de Developpement](#regles-de-developpement)
2. [Structure Hierarchique Box Module](#structure-hierarchique-box-module)
3. [Mapping Complet des Parametres iLogic](#mapping-complet-des-parametres-ilogic)
4. [Listes Deroulantes Officielles](#listes-deroulantes-officielles)
5. [STEPs de Developpement](#steps-de-developpement)
6. [Commandes Spec Kit](#commandes-spec-kit)
7. [Fichiers du Module](#fichiers-du-module)
8. [Journal des Modifications](#journal-des-modifications)

---

## REGLES DE DEVELOPPEMENT

### Regles Critiques (VIOLATIONS = ECHEC)

```
1. NOMS iLOGIC EXACTS
   - Utiliser EXACTEMENT les noms de la colonne "Inventor Name"
   - Toute erreur de nom = Echec du push vers Inventor

2. LISTES DEROULANTES
   - Source: 000000000-params.xml
   - Utiliser EXACTEMENT les valeurs <multiValues> du fichier
   - Ne JAMAIS inventer de valeurs

3. EMOJIS
   - UI (XAML, MessageBox): AUTORISES
   - Code Backend (Logger, Console): INTERDITS
   - Remplacements: [+] [-] [!] [>] [i] [~]

4. BUILD
   - .NET Framework 4.8: .\build-and-run.ps1 -BuildOnly
   - Ne JAMAIS utiliser dotnet build pour ce projet

5. NE JAMAIS CASSER CE QUI FONCTIONNE
   - AJOUTER du code, pas MODIFIER la logique existante
   - DEBUG et AVANCER uniquement
```

### Structure des Fichiers

```
Modules/ConfigUnite/
├── Views/
│   ├── ConfigUniteWindow.xaml          # UI principale
│   └── ConfigUniteWindow.xaml.cs       # Code-behind
├── Models/
│   └── ConfigUniteDataModel.cs         # Modele de donnees
├── Services/
│   └── ConfigUniteService.cs           # Logique metier
├── Controls/
│   └── UnitVisualizer.xaml             # Visualiseur modules
└── DevDocs/
    ├── CONFIG_UNITE_MASTER.md          # CE FICHIER (Reference)
    └── Prompts/                         # Prompts pour agents IA
        ├── STEP_01_*.md
        ├── STEP_02_*.md
        └── ...
```

---

## STRUCTURE HIERARCHIQUE BOX MODULE

Structure EXACTE depuis l'editeur iLogic Inventor:

```
Box Module (Form)
│
├── Revision (GroupBox)
│   ├── Row 1
│   │   ├── Revision Number
│   │   ├── Checked Date
│   │   └── Checked By
│   └── Picture 1 (XNRGY Logo)
│
├── Design Info (Section)
│   ├── Row 3
│   │   ├── fx: Project Number              -> Numero_de_Projet_Form
│   │   ├── fx: Drafter Name                -> Initiale_du_Dessinateur_Form
│   │   ├── fx: Co-Drafter Name             -> Initiale_du_Co_Dessinateur_Form
│   │   └── fx: Creation Date               -> Creation_Date_Form
│
├── Unit Specification (Tab Principal)
│   │
│   ├── Unit Info (Sous-Tab)
│   │   ├── fx: CRAH Unit                   -> Crah_Unit_Form
│   │   ├── fx: Unit Type                   -> Unit_Type_Form
│   │   ├── Airflow (Group)
│   │   │   ├── fx: 1 Tunnel (Right)        -> Tunnel_Right_Form
│   │   │   ├── fx: Air Flow (Right)        -> AirFlow_Right_Form
│   │   │   ├── fx: 2 Tunnels (Left)        -> Tunnel_Left_Form
│   │   │   ├── fx: AirFlow (Left)          -> AirFlow_Left_Form
│   │   │   ├── fx: 3 Tunnels (Middle)      -> Tunnel_Middle_Form
│   │   │   └── fx: AirFlow (Middle)        -> AirFlow_Middle_Form
│   │   ├── fx: Unit Option                 -> Unit_Option_Form
│   │   ├── fx: Unit Design Pressure        -> StaticPressure_Form
│   │   ├── fx: Unit Configuration          -> Unit_Configuration_Form
│   │   ├── fx: Unit Certification          -> Unit_Certification_Form
│   │   ├── fx: Factory Testing             -> Factory_Testing_Form
│   │   └── fx: MaxHoleDistance_Form        -> MaxHoleDistance_Form
│   │
│   ├── Floor Info (Sous-Tab)
│   │   ├── fx: Base Construction           -> PerimeterMaterial_Form
│   │   ├── fx: Base Insulation             -> Floor_Insulation_Form
│   │   ├── fx: Floor Height                -> Floor_Height_Form
│   │   ├── fx: Base Coating                -> Coating_Floor_Form
│   │   ├── fx: Base Thermal Break          -> Base_Thermal_Break_Form
│   │   ├── fx: Floor Construction          -> Floor_Construction_Form
│   │   ├── fx: Floor Liner                 -> FloorLiner_SMStyleName_Form
│   │   ├── fx: Subfloor Liner              -> FloorSubLiner_SMStyleName_Form
│   │   ├── fx: Auxiliary Drains            -> DrainType_Form
│   │   ├── fx: Cross Member Width          -> Cross_Member_Width_Form
│   │   └── fx: Floor Mount Type            -> Floor_Mount_Type_Form
│   │
│   ├── Casing Info (Sous-Tab)
│   │   ├── fx: Panel Construction          -> Panel_Construction_Form
│   │   ├── fx: Panel Width                 -> Panel_Width_Form
│   │   ├── fx: Panel Insulation            -> Panel_Insulation_Form
│   │   ├── Panel & Liner Material (Group)
│   │   │   ├── Wall mtl
│   │   │   │   ├── fx: Wall Panel Material -> WallPanelSMStyleName_Form
│   │   │   │   └── fx: Wall Liner Material -> WallLinerSMStyleName_Form
│   │   │   └── Roof mtl
│   │   │       ├── fx: Roof Panel Mtl      -> RoofPanelSMStyleName_Form
│   │   │       └── fx: Roof Liner Mtl      -> RoofLinerSMStyleName_Form
│   │   ├── fx: Coating                     -> Coating_Form
│   │   ├── fx: Single Panel Length         -> SinglePanel_Length_Form
│   │   ├── fx: Wall Material               -> Wall_Material_Form
│   │   ├── fx: Selected Excel Row          -> SelectedExcelRow
│   │   └── fx: Critical Panel Length       -> CriticalPanel_Length_Form
│   │
│   └── Miscellaneous (Sous-Tab)
│       └── fx: Hardware Material           -> Hardware_Material_Form
│
├── Module Dimensions (Tab Principal)
│   ├── Row 2
│   │   ├── Group 2
│   │   │   ├── fx: Module_Width            -> Module_Width_Form
│   │   │   ├── fx: Module_Depth            -> Module_Depth_Form
│   │   │   └── fx: Module_Height           -> Module_Height_Form
│   │   └── Group 1 > Group 5
│   │       ├── Module Position (Label)
│   │       ├── fx: Module_Type             -> Module_Type_Form
│   │       └── Wall & Roof Requirement (Label)
│   │           ├── fx: Front_Wall_01       -> Front_Wall_01_Form
│   │           ├── fx: Back_Wall_01        -> Back_Wall_01_Form
│   │           ├── fx: Left_Wall_01        -> Left_Wall_01_Form
│   │           ├── fx: Right_Wall_01       -> Right_Wall_01_Form
│   │           └── fx: Roof_01             -> Roof_01_Form
│
├── Wall Specification (Tab Principal)
│   ├── fx: Customize Walls                 -> CustomizeWalls_Form
│   ├── Customize Wall Options (GroupBox)
│   │   │
│   │   ├── Interior Walls (Tab)
│   │   │   ├── Parallel To Right / Left (Tab)
│   │   │   │   ├── Interior Wall 01
│   │   │   │   │   ├── fx: Include Interior Wall 01      -> Include_First_Internal_Wall_Form
│   │   │   │   │   ├── fx: Interior Wall 01 Position     -> First_Internal_Wall_Position_Form
│   │   │   │   │   ├── fx: Customize Interior Wall Construction -> Customize_First_Internal_WallPanelConstruction_Form
│   │   │   │   │   ├── Interior Wall Construction (Group)
│   │   │   │   │   │   ├── fx: Interior Wall Panel Insulation   -> First_Internal_WallPanel_Insulation_Form
│   │   │   │   │   │   ├── fx: Interior Wall Panel Construction -> First_Internal_WallPanel_Construction_Form
│   │   │   │   │   │   ├── fx: Interior Wall Width              -> First_Internal_WallPanel_Panel_Width_Form
│   │   │   │   │   │   └── fx: Interior Wall Static Pressure    -> First_Internal_Wall_StaticPressure_Form
│   │   │   │   │   ├── fx: Customize Interior Wall Panel Material -> Customize_First_Internal_WallPanelMaterial_Form
│   │   │   │   │   └── Interior Wall Material (Group)
│   │   │   │   │       ├── fx: Interior Wall Panel Material     -> First_Internal_WallPanelSMStyleName_Form
│   │   │   │   │       └── fx: Interior Wall Liner Material     -> First_Internal_WallLinerSMStyleName_Form
│   │   │   │   │
│   │   │   │   └── Interior Wall 02 (meme structure avec "Second_" au lieu de "First_")
│   │   │   │
│   │   │   └── Parallel To Front / Back (Tab)
│   │   │       ├── Right Tunnel
│   │   │       │   ├── Interior Wall 03
│   │   │       │   └── Interior Wall 04
│   │   │       ├── Left Tunnel
│   │   │       │   ├── Interior Wall 05
│   │   │       │   └── Interior Wall 06
│   │   │       └── Middle Tunnel
│   │   │           ├── Interior Wall 07
│   │   │           └── Interior Wall 08
│   │   │
│   │   └── Additional Wall / Roof (02 to 05) (Tab)
│   │       ├── Back
│   │       │   ├── Back Wall 02
│   │       │   │   ├── fx: Include Back Wall 02  -> Include_Back_Wall_02_Form
│   │       │   │   ├── fx: Dist_To_Bottom        -> Back_Wall_02_Bottom_Position_Form
│   │       │   │   ├── fx: Dist_To_Left          -> Back_Wall_02_Left_Position_Form
│   │       │   │   └── fx: Dist_To_Back          -> Back_Wall_02_Out_Position_Form
│   │       │   ├── Back Wall 03, 04, 05 (meme pattern)
│   │       ├── Front
│   │       │   ├── Front Wall 02-05
│   │       │   │   ├── Dist_To_Bottom, Dist_To_Right, Dist_To_Front
│   │       ├── Right
│   │       │   ├── Right Wall 02-05
│   │       │   │   ├── Dist_To_Bottom, Dist_To_Back, Dist_To_Right
│   │       ├── Left
│   │       │   ├── Left Wall 02-05
│   │       │   │   ├── Dist_To_Bottom, Dist_To_Front, Dist_To_Left
│   │       └── Roof
│   │           ├── Roof 02-05
│   │           │   ├── fx: Include Roof 02       -> Include_Roof_02_Form
│   │           │   ├── fx: Dist_To_Back          -> Roof_02_Back_Position_Form
│   │           │   ├── fx: Dist_To_Left          -> Roof_02_Left_Position_Form
│   │           │   └── fx: Dist_To_Top           -> Roof_02_Out_Position_Form
│
├── Modular Brackets (Tab Principal)
│   ├── Exterior Brackets
│   │   ├── fx: Exterior Brackets Required  -> Ext_Brackets_Required_Form
│   │   ├── Front (Tab)
│   │   └── Back (Tab)
│   └── Interior Brackets
│       ├── Front (Tab)
│       └── Back (Tab)
│
├── Equipments (Tab Principal)
│   └── (Liste des equipements avec positions)
│
└── Retrieved Values (Tab Principal)
    └── (Valeurs calculees/recuperees en lecture seule)
```

---

## MAPPING COMPLET DES PARAMETRES iLOGIC

### Design Info

| UI Label | Inventor Parameter Name | Type | Liste Deroulante |
|----------|------------------------|------|------------------|
| Project Number | `Numero_de_Projet_Form` | String | Non |
| Drafter Name | `Initiale_du_Dessinateur_Form` | String | Oui (voir section) |
| Co-Drafter Name | `Initiale_du_Co_Dessinateur_Form` | String | Oui (meme liste) |
| Creation Date | `Creation_Date_Form` | String | Non (DatePicker) |

### Unit Info

| UI Label | Inventor Parameter Name | Type | Liste Deroulante |
|----------|------------------------|------|------------------|
| CRAH Unit | `Crah_Unit_Form` | Boolean | CheckBox |
| Unit Type | `Unit_Type_Form` | String | Oui |
| 1 Tunnel (Right) | `Tunnel_Right_Form` | Boolean | CheckBox |
| Air Flow (Right) | `AirFlow_Right_Form` | String | Oui |
| 2 Tunnels (Left) | `Tunnel_Left_Form` | Boolean | CheckBox |
| AirFlow (Left) | `AirFlow_Left_Form` | String | Oui |
| 3 Tunnels (Middle) | `Tunnel_Middle_Form` | Boolean | CheckBox |
| AirFlow (Middle) | `AirFlow_Middle_Form` | String | Oui |
| Unit Option | `Unit_Option_Form` | String | Oui |
| Unit Design Pressure | `StaticPressure_Form` | String | Oui |
| Unit Configuration | `Unit_Configuration_Form` | String | Oui |
| Unit Certification | `Unit_Certification_Form` | String | Oui |
| Factory Testing | `Factory_Testing_Form` | String | Oui |

### Floor Info

| UI Label | Inventor Parameter Name | Type | Liste Deroulante |
|----------|------------------------|------|------------------|
| Base Construction | `PerimeterMaterial_Form` | String | Oui |
| Base Insulation | `Floor_Insulation_Form` | String | Oui |
| Floor Height | `Floor_Height_Form` | String | Oui |
| Base Coating | `Coating_Floor_Form` | String | Oui |
| Base Thermal Break | `Base_Thermal_Break_Form` | String | Oui |
| Floor Construction | `Floor_Construction_Form` | String | Oui |
| Floor Liner | `FloorLiner_SMStyleName_Form` | String | Oui |
| Subfloor Liner | `FloorSubLiner_SMStyleName_Form` | String | Oui |
| Auxiliary Drains | `DrainType_Form` | String | Oui |
| Cross Member Width | `Cross_Member_Width_Form` | String | Oui |
| Floor Mount Type | `Floor_Mount_Type_Form` | String | Oui |

### Casing Info

| UI Label | Inventor Parameter Name | Type | Liste Deroulante |
|----------|------------------------|------|------------------|
| Panel Construction | `Panel_Construction_Form` | String | Oui |
| Panel Width | `Panel_Width_Form` | String | Oui |
| Panel Insulation | `Panel_Insulation_Form` | String | Oui |
| Wall Panel Material | `WallPanelSMStyleName_Form` | String | Oui |
| Wall Liner Material | `WallLinerSMStyleName_Form` | String | Oui |
| Roof Panel Mtl | `RoofPanelSMStyleName_Form` | String | Oui |
| Roof Liner Mtl | `RoofLinerSMStyleName_Form` | String | Oui |
| Coating | `Coating_Form` | String | Oui |
| Single Panel Length | `SinglePanel_Length_Form` | Number | Non |
| Wall Material | `Wall_Material_Form` | String | Oui |
| Critical Panel Length | `CriticalPanel_Length_Form` | Number | Non |

### Miscellaneous

| UI Label | Inventor Parameter Name | Type | Liste Deroulante |
|----------|------------------------|------|------------------|
| Hardware Material | `Hardware_Material_Form` | String | Oui |

### Module Dimensions

| UI Label | Inventor Parameter Name | Type |
|----------|------------------------|------|
| Module_Width | `Module_Width_Form` | Number (in) |
| Module_Depth | `Module_Depth_Form` | Number (in) |
| Module_Height | `Module_Height_Form` | Number (in) |
| Module_Type | `Module_Type_Form` | String |
| Front_Wall_01 | `Front_Wall_01_Form` | String |
| Back_Wall_01 | `Back_Wall_01_Form` | String |
| Left_Wall_01 | `Left_Wall_01_Form` | String |
| Right_Wall_01 | `Right_Wall_01_Form` | String |
| Roof_01 | `Roof_01_Form` | String |

### Wall Specification - Interior Walls

| UI Label | Inventor Parameter Name |
|----------|------------------------|
| Include Interior Wall 01 | `Include_First_Internal_Wall_Form` |
| Interior Wall 01 Position | `First_Internal_Wall_Position_Form` |
| Customize Interior Wall Construction | `Customize_First_Internal_WallPanelConstruction_Form` |
| Interior Wall Panel Insulation | `First_Internal_WallPanel_Insulation_Form` |
| Interior Wall Panel Construction | `First_Internal_WallPanel_Construction_Form` |
| Interior Wall Width | `First_Internal_WallPanel_Panel_Width_Form` |
| Interior Wall Static Pressure | `First_Internal_Wall_StaticPressure_Form` |
| Customize Interior Wall Panel Material | `Customize_First_Internal_WallPanelMaterial_Form` |
| Interior Wall Panel Material | `First_Internal_WallPanelSMStyleName_Form` |
| Interior Wall Liner Material | `First_Internal_WallLinerSMStyleName_Form` |
| Include Interior Wall 02 | `Include_Second_Internal_Wall_Form` |
| (Meme pattern avec "Second_" pour Wall 02) | |

### Wall Specification - Additional Walls

| Direction | Wall | Include | Bottom | Left/Right | Out |
|-----------|------|---------|--------|------------|-----|
| Back | 02 | `Include_Back_Wall_02_Form` | `Back_Wall_02_Bottom_Position_Form` | `Back_Wall_02_Left_Position_Form` | `Back_Wall_02_Out_Position_Form` |
| Back | 03 | `Include_Back_Wall_03_Form` | `Back_Wall_03_Bottom_Position_Form` | `Back_Wall_03_Left_Position_Form` | `Back_Wall_03_Out_Position_Form` |
| Back | 04 | `Include_Back_Wall_04_Form` | `Back_Wall_04_Bottom_Position_Form` | `Back_Wall_04_Left_Position_Form` | `Back_Wall_04_Out_Position_Form` |
| Back | 05 | `Include_Back_Wall_05_Form` | `Back_Wall_05_Bottom_Position_Form` | `Back_Wall_05_Left_Position_Form` | `Back_Wall_05_Out_Position_Form` |
| Front | 02-05 | `Include_Front_Wall_XX_Form` | `Front_Wall_XX_Bottom_Position_Form` | `Front_Wall_XX_Left_Position_Form` | `Front_Wall_XX_Out_Position_Form` |
| Right | 02-05 | `Include_Right_Wall_XX_Form` | `Right_Wall_XX_Bottom_Position_Form` | `Right_Wall_XX_Left_Position_Form` | `Right_Wall_XX_Out_Position_Form` |
| Left | 02-05 | `Include_Left_Wall_XX_Form` | `Left_Wall_XX_Bottom_Position_Form` | `Left_Wall_XX_Left_Position_Form` | `Left_Wall_XX_Out_Position_Form` |
| Roof | 02 | `Include_Roof_02_Form` | `Roof_02_Back_Position_Form` | `Roof_02_Left_Position_Form` | `Roof_02_Out_Position_Form` |
| Roof | 03-05 | (meme pattern) | | | |

---

## LISTES DEROULANTES OFFICIELLES

Source: `SUBMITTAL/Rules_iLogic/BOX_MODULE/000000000-params.xml`

### Initiale_du_Dessinateur_Form (Drafter Name)
```
N/A, AC, AM, AR, CC, DC, DL, FL, IM, KB, KJ, MAE, MC, NJ, RO, SB, TG, TV, VK, YS
```

### Unit_Type_Form
```
Outdoor/ Exterieur
Interior/Interieur
None/Aucun
```

### Unit_Configuration_Form
```
Unstacked/Standard
Stacked/ Empile
None/Aucun
```

### Unit_Option_Form
```
Standard
None/Aucun
Washable/Lavable (aucune vis au plancher)
```

### Unit_Certification_Form
```
Yes/Oui (Voir Notes)
No/Non
None/Aucun
```

### AirFlow Direction (AirFlow_Right_Form, etc.)
```
Front To Back
Back To Front
```

### StaticPressure_Form
```
5 ul
8 ul
10 ul
12 ul
```

### Floor_Height_Form
```
4 in
5 in
6 in
8 in
10 in
12 in
```

### Floor_Insulation_Form
```
None/Aucun
Injected Polyurethane/Mousse Injecte
Fiberglass/ Isolation en Fibre de Verre
Special (See Note)/ Special (voir Notes)
```

### Base_Thermal_Break_Form
```
None/Aucun
Standard (Gasket)
Special See Submittal/ Special Voir ''Submittal''
NTM J-Plastic ou Polyblock (See Note Importante)/ NTM  J-Plastic ou Polyblock (Voir Note Importante)
```

### Floor_Mount_Type_Form
```
Steel_Dunnage/Egale au Perimetre
Roof_Curb/Egale a la sous-Tole de Plancher
None/Aucun
```

### Panel_Construction_Form
```
None/Aucun
Standard (Gasket)
NTM (J-Plastic)
Special See Submittal/ Special Voir ''Submittal''
```

### Panel_Width_Form
```
2 in
3 in
4 in
```

### Panel_Insulation_Form
```
None/Aucun
Injected Polyurethane/ Mousse Injecte
Fiberglass/ Isolation en Fibre de Verre (Caulking pour retenir)
Foam_Board_Insulation
Special (See Note)/ Special (voir Notes)
```

### Coating_Form
```
No Paint/ Non Peint
Xnrgy White Paint/ Peint Blanc Xnrgy
Special Paint Color/ Paint CouleurSpecial
```

### Wall_Material_Form
```
Aluminium
Galvanized
Stainless
```

### Hardware_Material_Form
```
None / Aucun
Standard According to Material / Materiel Selon Standard
Stainless Steel / Acier Inoxydable
```

### WallPanelSMStyleName_Form (Wall Panel Material)
```
None/Aucun
ALUM_12GA_0.080
ALUM_12GA_0.080_PC
ALUM_14GA_0.063
ALUM_14GA_0.063_PC
ALUMTEXT_12GA_0.080
ALUMTEXT_12GA_0.080_PC
ALUMTEXT_14GA_0.063
ALUMTEXT_14GA_0.063_PC
GALV_14GA_0.078
GALV_14GA_0.078_PC
GALV_16GA_0.063
GALV_16GA_0.063_PC
SS304_14GA_0.078
SS304_14GA_0.078_PC
SS304_16GA_0.063
SS304_16GA_0.063_PC
SS316_14GA_0.078
SS316_14GA_0.078_PC
SS316_16GA_0.063
SS316_16GA_0.063_PC
```

### WallLinerSMStyleName_Form (Wall Liner Material)
```
None/Aucun
ALUM_10GA_0.100, ALUM_12GA_0.080, ALUM_14GA_0.063, ALUM_16GA_0.050, ALUM_18GA_0.040
(versions _PC disponibles)
ALUMPERF23_14GA_0.063, ALUMPERF23_16GA_0.050, ALUMPERF23_18GA_0.040
ALUMPERF51_14GA_0.063, ALUMPERF51_16GA_0.050, ALUMPERF51_18GA_0.040
GALV_10GA_0.138, GALV_12GA_0.108, GALV_14GA_0.078, GALV_16GA_0.063, GALV_18GA_0.051, GALV_20GA_0.039, GALV_22GA_0.033
GALVPERF23_18GA, GALVPERF23_20GA, GALVPERF23_22GA
GALVPERF51_18GA, GALVPERF51_20GA, GALVPERF51_22GA
SS304_10GA, SS304_12GA, SS304_14GA, SS304_16GA, SS304_18GA, SS304_20GA, SS304_22GA
SS304PERF23/51 variantes
SS316 toutes variantes
```

---

## STEPS DE DEVELOPPEMENT

### STEP 1: Layout Principal (COMPLETE)
- Grid 3 zones: Header + Content + Footer
- Header: Projet, Reference, Unit Name, Existing Configs
- Status: FAIT mais a REVOIR avec noms iLogic

### STEP 2: TabControl 6 Tabs (COMPLETE)
- [Unit Specification] [Module Dimensions] [Wall Specification] [Modular Brackets] [Equipments] [Retrieved Values]
- Status: FAIT mais a REVOIR avec noms iLogic

### STEP 3: Unit Specification avec 4 Sous-Tabs (COMPLETE)
- [Unit Info] [Floor Info] [Casing Info] [Miscellaneous]
- Status: FAIT mais a REVOIR avec noms iLogic et listes deroulantes

### STEP 4: Wall Specification (EN COURS - Cline GLM)
- Interior Walls: Parallel To Right/Left + Parallel To Front/Back (Tunnels)
- Additional Wall/Roof 02-05: Back, Front, Right, Left, Roof
- Status: Cline travaille avec prompt v3 FINAL

### STEP 5: Modular Brackets (TODO)
- Exterior Brackets + Interior Brackets
- Front/Back pour chaque

### STEP 6: Equipments (TODO)
- Liste des equipements avec positions

### STEP 7: Retrieved Values (TODO)
- Valeurs calculees en lecture seule

### STEP 8: Unit Visualizer (COMPLETE)
- Canvas avec modules cote a cote
- Status: FAIT

### STEP 9: DataModel Final (TODO)
- Synchronisation avec tous les noms iLogic

### STEP 10: Push vers Inventor (TODO)
- Service pour pousser les valeurs vers l'assemblage

---

## COMMANDES SPEC KIT

### Pour implementer un STEP
```bash
/speckit.implement [chemin_du_fichier_prompt.md]
```

### Pour reimplementer (corriger)
```bash
/speckit.reimplement STEP_XX
```

### Pour analyser/valider (Copilot)
```bash
/speckit.analyze STEP_XX
```

---

## FICHIERS DU MODULE

### Fichiers Principaux
```
ConfigUniteWindow.xaml          - UI principale
ConfigUniteWindow.xaml.cs       - Code-behind
ConfigUniteDataModel.cs         - Modele de donnees
ConfigUniteService.cs           - Logique metier
UnitVisualizer.xaml             - Visualiseur canvas
```

### Fichiers Reference
```
CONFIG_UNITE_MASTER.md          - CE FICHIER (Reference)
000000000-params.xml            - Parametres iLogic source
```

### Fichiers Prompts
```
DevDocs/Prompts/STEP_XX_*.md    - Prompts pour agents IA
```

---

## JOURNAL DES MODIFICATIONS

### 2026-01-28
- Creation du fichier MASTER fusionne
- Ajout mapping complet des parametres iLogic
- Ajout listes deroulantes depuis params.xml
- STEP 4 en cours avec Cline (GLM-4.7)

### 2026-01-27
- STEP 1-3 completes
- Restructuration plan cree
- Unit Visualizer fonctionnel

### 2026-01-26
- Debut du projet Config Unite
- Vision et architecture definies

---

## NOTES IMPORTANTES

1. **Toujours verifier les noms iLogic** avant d'implementer
2. **Les listes deroulantes viennent UNIQUEMENT** de 000000000-params.xml
3. **Build avec** `.\build-and-run.ps1 -BuildOnly`
4. **Pas d'emojis dans Logger/Console** - utiliser [+] [-] [!] etc.
5. **Ce fichier est la REFERENCE** - l'inclure dans toutes les demandes

---

*Derniere mise a jour: 2026-01-28 par GitHub Copilot (Chef d'Orchestre)*
