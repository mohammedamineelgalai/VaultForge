// ============================================================================
// ParameterLists.cs - Listes de valeurs pour Config Unité
// Source: Inventor Box Module iLogic parameters (000000000-params.xml)
// Généré: 2025-01-27
// Modifié: 2025-01-28 (Revision STEP_01_03)
// ============================================================================
// Ces listes sont les valeurs EXACTES utilisées dans les templates Inventor
// et seront écrites dans les paramètres iLogic lors de la création de modules
// ============================================================================

using System.Collections.Generic;

namespace XnrgyEngineeringAutomationTools.Modules.ConfigUnite.Data
{
    /// <summary>
    /// Contient toutes les listes de valeurs pour les ComboBox de Config Unité
    /// Extraites directement du fichier XML des paramètres Inventor
    /// </summary>
    public static class ParameterLists
    {
        // ====================================================================
        // TAB: Design Info
        // ====================================================================

        /// <summary>Initiale_du_Dessinateur_Form - Initiales dessinateurs</summary>
        public static readonly List<string> DesignerInitials = new()
        {
            "N/A",
            "AC",
            "AM",
            "AR",
            "CC",
            "DC",
            "DL",
            "FL",
            "IM",
            "KB",
            "KJ",
            "MAE",
            "MC",
            "NJ",
            "RO",
            "SB",
            "TG",
            "TV",
            "VK",
            "YS"
        };

        /// <summary>Initiale_du_Dessinateur_Form - Valeur par défaut</summary>
        public const string DesignerInitials_Default = "N/A";

        // ====================================================================
        // TAB: Unit Info
        // ====================================================================

        /// <summary>Unit_Type_Form - Type d'unité</summary>
        public static readonly List<string> UnitType = new()
        {
            "None/Aucun",
            "Outdoor/ Extérieur",
            "Interior/Interieur"
        };

        /// <summary>Unit_Configuration_Form - Configuration unité</summary>
        public static readonly List<string> UnitConfiguration = new()
        {
            "Unstacked/Standard",
            "Stacked/ Empilé",
            "None/Aucun"
        };

        /// <summary>Unit_Option_Form - Option unité</summary>
        public static readonly List<string> UnitOption = new()
        {
            "Standard",
            "None/Aucun",
            "Washable/Lavable (aucune vis au plancher)"
        };

        /// <summary>StaticPressure_Form - Pression de design (ul)</summary>
        public static readonly List<string> StaticPressure = new()
        {
            "5 ul",
            "8 ul",
            "10 ul",
            "12 ul"
        };

        /// <summary>StaticPressure_Form - Valeur par défaut</summary>
        public const string StaticPressure_Default = "12 ul";

        /// <summary>AirFlow directions pour tunnels - Valeurs exactes depuis params.xml</summary>
        public static readonly List<string> AirFlowDirection = new()
        {
            "None/Aucun",
            "Back-To-Front",
            "Front-To-Back",
            "Vestibule"
        };

        /// <summary>Unit_Certification_Form - Certification</summary>
        public static readonly List<string> UnitCertification = new()
        {
            "Yes/Oui (Voir Notes)",
            "No/Non",
            "None/Aucun"
        };

        /// <summary>Factory_Testing_Form - Test en usine</summary>
        public static readonly List<string> FactoryTesting = new()
        {
            "Yes/Oui",
            "No/Non",
            "None/Aucun"
        };

        // ====================================================================
        // TAB: Floor Info
        // ====================================================================

        /// <summary>PerimeterMaterial_Form - Base Construction (Structure Périmètre)</summary>
        public static readonly List<string> BaseConstruction = new()
        {
            "None/Aucun",
            "ALUM_STRUC",
            "SS304_STRUC",
            "SS316_STRUC",
            "STEEL_STRUC"
        };

        /// <summary>Floor_Insulation_Form - Isolation plancher</summary>
        public static readonly List<string> FloorInsulation = new()
        {
            "None/Aucun",
            "Injected Polyurethane/Mousse Injecté",
            "Fiberglass/ Isolation en Fibre de Verre",
            "Special (See Note)/ Spécial (voir Notes)"
        };

        /// <summary>Floor_Height_Form - Hauteur plancher (pouces)</summary>
        public static readonly List<string> FloorHeight = new()
        {
            "4 in",
            "5 in",
            "6 in",
            "8 in",
            "10 in",
            "12 in"
        };

        /// <summary>Floor_Height_Form - Valeur par défaut</summary>
        public const string FloorHeight_Default = "8 in";

        /// <summary>Base_Thermal_Break_Form - Rupture thermique</summary>
        public static readonly List<string> BaseThermalBreak = new()
        {
            "None/Aucun",
            "Standard (Gasket)",
            "Special See Submittal/ Spécial Voir ''Submittal''",
            "NTM J-Plastic ou Polyblock (See Note Importante)/ NTM  J-Plastic ou Polyblock (Voir Note Importante)"
        };

        /// <summary>Floor_Construction_Form - Construction plancher</summary>
        public static readonly List<string> FloorConstruction = new()
        {
            "None/Aucun",
            "Welded",
            "Capped & Caulked"
        };

        /// <summary>FloorLiner_SMStyleName_Form - Matériau Floor Liner</summary>
        public static readonly List<string> FloorLiner = new()
        {
            "None/Aucun",
            "ALUM_8GA_0.125",
            "ALUM_8GA_0.125_PC",
            "ALUM_5GA_0.188",
            "ALUM_5GA_0.188_PC",
            "ALUM_10GA_0.100",
            "ALUM_10GA_0.100_PC",
            "ALUM_12GA_0.080",
            "ALUM_12GA_0.080_PC",
            "ALUMCKPL_10GA_0.100",
            "ALUMCKPL_10GA_0.100_PC",
            "ALUMCKPL_5GA_0.188",
            "ALUMCKPL_5GA_0.188_PC",
            "ALUMCKPL_8GA_0.125",
            "ALUMCKPL_8GA_0.125_PC",
            "GALV_10GA_0.138",
            "GALV_10GA_0.138_PC",
            "GALV_12GA_0.108",
            "GALV_12GA_0.108_PC",
            "GALV_14GA_0.078",
            "GALV_14GA_0.078_PC",
            "SS304_10GA_0.138",
            "SS304_10GA_0.138_PC",
            "SS304_12GA_0.108",
            "SS304_12GA_0.108_PC",
            "SS304_14GA_0.078",
            "SS304_14GA_0.078_PC",
            "SS304CKPL_11GA_0.125",
            "SS304CKPL_11GA_0.125_PC",
            "SS304CKPL_7GA_0.188",
            "SS304CKPL_7GA_0.188_PC",
            "SS316_10GA_0.138",
            "SS316_10GA_0.138_PC",
            "SS316_12GA_0.108",
            "SS316_12GA_0.108_PC",
            "SS316_14GA_0.078",
            "SS316_14GA_0.078_PC",
            "SS316CKPL_11GA_0.125",
            "SS316CKPL_11GA_0.125_PC",
            "SS316CKPL_7GA_0.188",
            "SS316CKPL_7GA_0.188_PC"
        };

        /// <summary>FloorSubLiner_SMStyleName_Form - Matériau Subfloor Liner</summary>
        public static readonly List<string> SubfloorLiner = new()
        {
            "None/Aucun",
            "ALUM_12GA_0.080",
            "ALUM_12GA_0.080_PC",
            "ALUM_14GA_0.063",
            "ALUM_14GA_0.063_PC",
            "ALUM_16GA_0.050",
            "ALUM_16GA_0.050_PC",
            "ALUM_18GA_0.040",
            "ALUM_18GA_0.040_PC",
            "GALV_14GA_0.078",
            "GALV_14GA_0.078_PC",
            "GALV_16GA_0.063",
            "GALV_16GA_0.063_PC",
            "GALV_18GA_0.051",
            "GALV_18GA_0.051_PC",
            "GALV_20GA_0.039",
            "GALV_20GA_0.039_PC",
            "GALV_22GA_0.033",
            "GALV_22GA_0.033_PC",
            "SS304_14GA_0.078",
            "SS304_14GA_0.078_PC",
            "SS304_16GA_0.063",
            "SS304_16GA_0.063_PC",
            "SS304_18GA_0.051",
            "SS304_18GA_0.051_PC",
            "SS304_20GA_0.039",
            "SS304_20GA_0.039_PC",
            "SS304_22GA_0.033",
            "SS304_22GA_0.033_PC",
            "SS316_14GA_0.078",
            "SS316_14GA_0.078_PC",
            "SS316_16GA_0.063",
            "SS316_16GA_0.063_PC",
            "SS316_18GA_0.051",
            "SS316_18GA_0.051_PC",
            "SS316_20GA_0.039",
            "SS316_20GA_0.039_PC",
            "SS316_22GA_0.033",
            "SS316_22GA_0.033_PC"
        };

        /// <summary>DrainType_Form - Type de drain auxiliaire</summary>
        public static readonly List<string> AuxiliaryDrains = new()
        {
            "Ø1.25",
            "Ø2.00"
        };

        /// <summary>Cross_Member_Width_Form - Largeur traverse</summary>
        public static readonly List<string> CrossMemberWidth = new()
        {
            "3.00",
            "3.50",
            "4.00",
            "4.50",
            "5.00",
            "6.00"
        };

        /// <summary>Floor_Mount_Type_Form - Type de montage</summary>
        public static readonly List<string> FloorMountType = new()
        {
            "Steel_Dunnage/Égale au Périmètre",
            "Roof_Curb/Égale à la sous-Tôle de Plancher",
            "None/Aucun"
        };

        // ====================================================================
        // TAB: Casing Info
        // ====================================================================

        /// <summary>Panel_Construction_Form - Construction panneau</summary>
        public static readonly List<string> PanelConstruction = new()
        {
            "None/Aucun",
            "Standard (Gasket)",
            "NTM (J-Plastic)",
            "Special See Submittal/ Spécial Voir ''Submittal''"
        };

        /// <summary>Panel_Width_Form - Largeur panneau (pouces)</summary>
        public static readonly List<string> PanelWidth = new()
        {
            "2 in",
            "3 in",
            "4 in"
        };

        /// <summary>Panel_Width_Form - Valeur par défaut</summary>
        public const string PanelWidth_Default = "2 in";

        /// <summary>Panel_Insulation_Form - Isolation panneau</summary>
        public static readonly List<string> PanelInsulation = new()
        {
            "None/Aucun",
            "Injected Polyurethane/ Mousse Injecté",
            "Fiberglass/ Isolation en Fibre de Verre (Caulking pour retenir)",
            "Foam Board Insulation",
            "Special (See Note)/ Spécial (voir Notes)"
        };

        /// <summary>WallPanelSMStyleName_Form - Matériau panneau mur</summary>
        public static readonly List<string> WallPanelMaterial = new()
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
            "GALV_14GA_0.078_PC ",
            "GALV_16GA_0.063",
            "GALV_16GA_0.063_PC ",
            "SS304_14GA_0.078",
            "SS304_14GA_0.078_PC ",
            "SS304_16GA_0.063",
            "SS304_16GA_0.063_PC ",
            "SS316_14GA_0.078",
            "SS316_14GA_0.078_PC ",
            "SS316_16GA_0.063",
            "SS316_16GA_0.063_PC"
        };

        /// <summary>RoofPanelSMStyleName_Form - Matériau panneau toit</summary>
        public static readonly List<string> RoofPanelMaterial = new()
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

        /// <summary>WallLinerSMStyleName_Form - Matériau liner mur (liste complète)</summary>
        public static readonly List<string> WallLinerMaterial = new()
        {
            "None/Aucun",
            // Aluminium standard
            "ALUM_10GA_0.100",
            "ALUM_10GA_0.100_PC",
            "ALUM_12GA_0.080",
            "ALUM_12GA_0.080_PC",
            "ALUM_14GA_0.063",
            "ALUM_14GA_0.063_PC",
            "ALUM_16GA_0.050",
            "ALUM_16GA_0.050_PC",
            "ALUM_18GA_0.040",
            "ALUM_18GA_0.040_PC",
            // Aluminium perforé 23%
            "ALUMPERF23_14GA_0.063",
            "ALUMPERF23_14GA_0.063_PC",
            "ALUMPERF23_16GA_0.050",
            "ALUMPERF23_16GA_0.050_PC",
            "ALUMPERF23_18GA_0.040",
            "ALUMPERF23_18GA_0.040_PC",
            // Aluminium perforé 51%
            "ALUMPERF51_14GA_0.063",
            "ALUMPERF51_14GA_0.063_PC",
            "ALUMPERF51_16GA_0.050",
            "ALUMPERF51_16GA_0.050_PC",
            "ALUMPERF51_18GA_0.040",
            "ALUMPERF51_18GA_0.040_PC",
            // Galvanisé standard
            "GALV_10GA_0.138",
            "GALV_10GA_0.138_PC",
            "GALV_12GA_0.108",
            "GALV_12GA_0.108_PC",
            "GALV_14GA_0.078",
            "GALV_14GA_0.078_PC",
            "GALV_16GA_0.063",
            "GALV_16GA_0.063_PC",
            "GALV_18GA_0.051",
            "GALV_18GA_0.051_PC",
            "GALV_20GA_0.039",
            "GALV_20GA_0.039_PC",
            "GALV_22GA_0.033",
            "GALV_22GA_0.033_PC",
            // Galvanisé perforé 23%
            "GALVPERF23_18GA_0.051",
            "GALVPERF23_18GA_0.051_PC",
            "GALVPERF23_20GA_0.039",
            "GALVPERF23_20GA_0.039_PC",
            "GALVPERF23_22GA_0.033",
            "GALVPERF23_22GA_0.033_PC",
            // Galvanisé perforé 51%
            "GALVPERF51_18GA_0.051",
            "GALVPERF51_18GA_0.051_PC",
            "GALVPERF51_20GA_0.039",
            "GALVPERF51_20GA_0.039_PC",
            "GALVPERF51_22GA_0.033",
            "GALVPERF51_22GA_0.033_PC",
            // Stainless Steel 304 standard
            "SS304_10GA_0.138",
            "SS304_10GA_0.138_PC",
            "SS304_12GA_0.108",
            "SS304_12GA_0.108_PC",
            "SS304_14GA_0.078",
            "SS304_14GA_0.078_PC",
            "SS304_16GA_0.063",
            "SS304_16GA_0.063_PC",
            "SS304_18GA_0.051",
            "SS304_18GA_0.051_PC",
            "SS304_20GA_0.039",
            "SS304_20GA_0.039_PC",
            "SS304_22GA_0.033",
            "SS304_22GA_0.033_PC",
            // SS304 perforé 23%
            "SS304PERF23_18GA_0.051",
            "SS304PERF23_18GA_0.051_PC",
            "SS304PERF23_20GA_0.039",
            "SS304PERF23_20GA_0.039_PC",
            "SS304PERF23_22GA_0.033",
            "SS304PERF23_22GA_0.033_PC",
            // SS304 perforé 51%
            "SS304PERF51_18GA_0.051",
            "SS304PERF51_18GA_0.051_PC",
            "SS304PERF51_20GA_0.039",
            "SS304PERF51_20GA_0.039_PC",
            "SS304PERF51_22GA_0.033",
            "SS304PERF51_22GA_0.033_PC",
            // Stainless Steel 316 standard
            "SS316_10GA_0.138",
            "SS316_10GA_0.138_PC",
            "SS316_12GA_0.108",
            "SS316_12GA_0.108_PC",
            "SS316_14GA_0.078",
            "SS316_14GA_0.078_PC",
            "SS316_16GA_0.063",
            "SS316_16GA_0.063_PC",
            "SS316_18GA_0.051",
            "SS316_18GA_0.051_PC",
            "SS316_20GA_0.039",
            "SS316_20GA_0.039_PC",
            "SS316_22GA_0.033",
            "SS316_22GA_0.033_PC",
            // SS316 perforé 23%
            "SS316PERF23_18GA_0.051",
            "SS316PERF23_18GA_0.051_PC",
            "SS316PERF23_20GA_0.039",
            "SS316PERF23_20GA_0.039_PC",
            "SS316PERF23_22GA_0.033",
            "SS316PERF23_22GA_0.033_PC",
            // SS316 perforé 51%
            "SS316PERF51_18GA_0.051",
            "SS316PERF51_18GA_0.051_PC",
            "SS316PERF51_20GA_0.039",
            "SS316PERF51_20GA_0.039_PC",
            "SS316PERF51_22GA_0.033",
            "SS316PERF51_22GA_0.033_PC"
        };

        /// <summary>RoofLinerSMStyleName_Form - Matériau liner toit (même liste que WallLiner)</summary>
        public static readonly List<string> RoofLinerMaterial = WallLinerMaterial;

        /// <summary>Wall_Material_Form - Matériau mur général</summary>
        public static readonly List<string> WallMaterial = new()
        {
            "Aluminium",
            "Galvanized",
            "Stainless"
        };

        /// <summary>Coating_Form - Revêtement/Peinture (général)</summary>
        public static readonly List<string> Coating = new()
        {
            "No Paint/ Non Peint",
            "Xnrgy White Paint/ Peint Blanc Xnrgy",
            "Special Paint Color/ Paint CouleurSpécial"
        };

        /// <summary>Coating_Floor_Form - Revêtement plancher</summary>
        public static readonly List<string> CoatingFloor = new()
        {
            "No Paint/ Non Peint",
            "Xnrgy White Paint/ Peint Blanc Xnrgy",
            "Special Paint Color/ Paint CouleurSpécial"
        };

        // ====================================================================
        // TAB: Miscellaneous
        // ====================================================================

        /// <summary>Hardware_Material_Form - Matériau quincaillerie</summary>
        public static readonly List<string> HardwareMaterial = new()
        {
            "None / Aucun",
            "Standard According to Material / Matériel Selon Standard",
            "Stainless Steel / Acier Inoxydable"
        };

        // ====================================================================
        // TAB: Module Dimensions
        // ====================================================================

        /// <summary>Module_Position - Position du module dans l'unité</summary>
        public static readonly List<string> ModulePosition = new()
        {
            "First",
            "Middle",
            "Last",
            "Unibase"
        };

        // ====================================================================
        // Correspondance Paramètre Inventor <-> Propriété C#
        // ====================================================================
        // Pour référence lors de l'écriture des paramètres iLogic
        // 
        // Design Info:
        // Initiale_du_Dessinateur_Form -> DesignerInitials
        //
        // Unit Info:
        // Unit_Type_Form              -> UnitType
        // Unit_Configuration_Form     -> UnitConfiguration
        // Unit_Option_Form            -> UnitOption
        // Unit_Certification_Form     -> UnitCertification
        // StaticPressure_Form         -> StaticPressure
        // Factory_Testing_Form        -> FactoryTesting
        // AirFlow_Right_Form          -> AirFlowDirection
        // AirFlow_Left_Form           -> AirFlowDirection
        // AirFlow_Middle_Form         -> AirFlowDirection
        //
        // Floor Info:
        // PerimeterMaterial_Form      -> BaseConstruction
        // Floor_Insulation_Form       -> FloorInsulation
        // Floor_Height_Form           -> FloorHeight
        // Coating_Floor_Form          -> CoatingFloor
        // Base_Thermal_Break_Form     -> BaseThermalBreak
        // Floor_Construction_Form     -> FloorConstruction
        // FloorLiner_SMStyleName_Form -> FloorLiner
        // FloorSubLiner_SMStyleName_Form -> SubfloorLiner
        // DrainType_Form              -> AuxiliaryDrains
        // Cross_Member_Width_Form     -> CrossMemberWidth
        // Floor_Mount_Type_Form       -> FloorMountType
        //
        // Casing Info:
        // Panel_Construction_Form     -> PanelConstruction
        // Panel_Width_Form            -> PanelWidth
        // Panel_Insulation_Form       -> PanelInsulation
        // WallPanelSMStyleName_Form   -> WallPanelMaterial
        // RoofPanelSMStyleName_Form   -> RoofPanelMaterial
        // WallLinerSMStyleName_Form   -> WallLinerMaterial
        // RoofLinerSMStyleName_Form   -> RoofLinerMaterial
        // Wall_Material_Form          -> WallMaterial
        // Coating_Form                -> Coating
        //
        // Miscellaneous:
        // Hardware_Material_Form      -> HardwareMaterial
    }
}