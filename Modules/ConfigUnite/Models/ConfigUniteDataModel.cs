using System;
using System.Collections.Generic;

namespace XnrgyEngineeringAutomationTools.Modules.ConfigUnite.Models
{
    /// <summary>
    /// Modèle de données pour la configuration d'une unité complète (AHU)
    /// Ce fichier JSON sert de "Master" pour centraliser les propriétés communes à tous les modules d'une unité
    /// Les autres modules (Box Module, Build Module, etc.) peuvent lire ces informations au démarrage
    /// </summary>
    public class ConfigUniteDataModel
    {
        /// <summary>
        /// Version du format de configuration (pour migration future)
        /// </summary>
        public string Version { get; set; } = "1.0";

        // ============================================
        // SECTION: IDENTIFICATION (Phase 1 - Project/Reference/Unit)
        // ============================================
        
        /// <summary>
        /// Numero de projet XNRGY (ex: 10359, 10245)
        /// </summary>
        public string Project { get; set; } = "";

        /// <summary>
        /// Reference du projet (ex: REF09, REF10)
        /// </summary>
        public string Reference { get; set; } = "";

        /// <summary>
        /// Nom de l'unite (ex: AHU-01, AHU-02)
        /// </summary>
        public string UnitName { get; set; } = "";

        /// <summary>
        /// Date de dernière modification
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>
        /// Utilisateur qui a effectué la dernière modification
        /// </summary>
        public string LastModifiedBy { get; set; } = "";

        // ============================================
        // SECTION: REVISION
        // ============================================
        public RevisionInfo Revision { get; set; } = new RevisionInfo();

        // ============================================
        // SECTION: DESIGN INFO
        // ============================================
        public DesignInfo DesignInfo { get; set; } = new DesignInfo();

        // ============================================
        // SECTION: UNIT SPECIFICATION
        // ============================================
        public UnitSpecification UnitSpecification { get; set; } = new UnitSpecification();

        // ============================================
        // SECTION: FLOOR INFO
        // ============================================
        public FloorInfo FloorInfo { get; set; } = new FloorInfo();

        // ============================================
        // SECTION: CASING INFO
        // ============================================
        public CasingInfo CasingInfo { get; set; } = new CasingInfo();

        // ============================================
        // SECTION: MISCELLANEOUS
        // ============================================
        public MiscellaneousInfo Miscellaneous { get; set; } = new MiscellaneousInfo();

        // ============================================
        // SECTION: WALL SPECIFICATION
        // ============================================
        public WallSpecification WallSpecification { get; set; } = new WallSpecification();

        // ============================================
        // SECTION: MODULAR BRACKETS
        // ============================================
        public ModularBrackets ModularBrackets { get; set; } = new ModularBrackets();

        // ============================================
        // SECTION: MODULE DIMENSIONS
        // ============================================
        /// <summary>
        /// Liste des modules avec leurs dimensions spécifiques
        /// Chaque module d'une unité peut avoir des dimensions différentes (Height, Width, Length)
        /// </summary>
        public List<ModuleDimension> ModuleDimensions { get; set; } = new List<ModuleDimension>();
    }

    /// <summary>
    /// Informations de révision (créateur du template - appliqué une seule fois)
    /// NOTE: Ces champs sont pour le formulaire iLogic du template original
    /// </summary>
    public class RevisionInfo
    {
        public string RevisionNumber { get; set; } = "";
        
        /// <summary>
        /// Date de création du template (anciennement CheckedDate)
        /// Inventor: CreatedDate_Form
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Créateur du template (anciennement CheckedBy)
        /// Inventor: CreatedBy_Form
        /// </summary>
        public string CreatedBy { get; set; } = "";
    }

    /// <summary>
    /// Informations de design et révision fusionnées
    /// Ces champs sont poussés vers les Top ASSY et sous-assemblages impliqués
    /// </summary>
    public class DesignInfo
    {
        // ============================================
        // CHAMPS AUTO-REMPLIS (depuis config actuelle)
        // ============================================
        
        /// <summary>
        /// Numéro de projet (auto-rempli depuis config, ex: 1234501)
        /// </summary>
        public string ProjectNumber { get; set; } = "";
        
        /// <summary>
        /// Job Title (depuis Créer Module)
        /// Inventor: Job_Title_Form
        /// </summary>
        public string JobTitle { get; set; } = "";
        
        /// <summary>
        /// Lead CAD (depuis Créer Module)
        /// Inventor: Lead_CAD_Form
        /// </summary>
        public string LeadCAD { get; set; } = "";
        
        /// <summary>
        /// Date de création de cette config (avec calendrier)
        /// </summary>
        public DateTime ConfigDate { get; set; } = DateTime.Now;
        
        // ============================================
        // NOUVEAUX CHAMPS - Poussés vers Top ASSY + sous-assy
        // ============================================
        
        /// <summary>
        /// Unit Tag - identifiant de l'unité
        /// Inventor: Unit_Tag_Form (nouveau paramètre à créer)
        /// </summary>
        public string UnitTag { get; set; } = "";
        
        /// <summary>
        /// Drawing Number
        /// Inventor: DrawingNo_Form
        /// </summary>
        public string DrawingNo { get; set; } = "";
        
        /// <summary>
        /// Rep. (Représentant)
        /// Inventor: Rep_Form (nouveau paramètre à créer)
        /// </summary>
        public string Rep { get; set; } = "";
        
        /// <summary>
        /// Drawn By - Dessiné par
        /// Inventor: DrawnBy_Form (nouveau paramètre à créer)
        /// </summary>
        public string DrawnBy { get; set; } = "";
        
        /// <summary>
        /// Rep Contact
        /// Inventor: RepContact_Form (nouveau paramètre à créer)
        /// </summary>
        public string RepContact { get; set; } = "";
        
        /// <summary>
        /// SAE / Eng - Sales Application Engineer
        /// Inventor: SAE_Eng_Form (nouveau paramètre à créer)
        /// </summary>
        public string SAE_Eng { get; set; } = "";
        
        /// <summary>
        /// CFM - Cubic Feet per Minute
        /// Inventor: CFM_Form (nouveau paramètre à créer)
        /// </summary>
        public string CFM { get; set; } = "";
        
        /// <summary>
        /// Drawing Submittal Date - Date de soumission du dessin
        /// Inventor: DrawingSubmittalDate_Form (nouveau paramètre à créer)
        /// </summary>
        public DateTime DrawingSubmittalDate { get; set; } = DateTime.Now;
        
        // ============================================
        // CHAMPS DÉPLACÉS VERS DATAGRID MODULES
        // (conservés ici pour compatibilité, mais UI les montre dans DataGrid)
        // ============================================
        
        /// <summary>
        /// Drafter Name (déplacé vers DataGrid modules)
        /// Inventor: Initiale_du_Dessinateur_Form
        /// </summary>
        public string DrafterName { get; set; } = "";
        
        /// <summary>
        /// Co-Drafter Name (déplacé vers DataGrid modules)
        /// Inventor: Initiale_du_Co_Dessinateur_Form
        /// </summary>
        public string CoDrafterName { get; set; } = "";
        
        /// <summary>
        /// Creation Date (déplacé vers DataGrid modules)
        /// Inventor: Creation_Date_Form
        /// </summary>
        public DateTime CreationDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Spécifications de l'unité
    /// </summary>
    public class UnitSpecification
    {
        public bool IsCRAHUnit { get; set; } = false;
        public string UnitType { get; set; } = "None/Aucun"; // Indoor, Outdoor, etc.
        
        // Airflow
        public bool Tunnel1Right { get; set; } = false;
        public string AirFlowRight { get; set; } = "None/Aucun";
        public bool Tunnel2Left { get; set; } = false;
        public string AirFlowLeft { get; set; } = "None/Aucun";
        public bool Tunnel3Middle { get; set; } = false;
        public string AirFlowMiddle { get; set; } = "None/Aucun";
        
        // Unit Options
        public string UnitOption { get; set; } = "None/Aucun";
        public string UnitDesignPressure { get; set; } = "12 ul"; // en inches water gauge
        public string UnitConfiguration { get; set; } = "None/Aucun"; // Stacked, etc.
        public string UnitCertification { get; set; } = "None/Aucun";
        public bool FactoryTesting { get; set; } = false;
        public string MaxHoleDistanceForm { get; set; } = "12 in";
    }

    /// <summary>
    /// Informations du plancher
    /// </summary>
    public class FloorInfo
    {
        // Base Construction
        public string BaseConstruction { get; set; } = "None/Aucun"; // Tubular Aluminum, etc.
        public string BaseInsulation { get; set; } = "None/Aucun"; // 3" Polyurethane closed cell foam (R19.5)
        public string FloorHeight { get; set; } = "8 in";
        public string BaseCoating { get; set; } = "No Paint/Non Peint";
        public string BaseThermalBreak { get; set; } = "None/Aucun"; // No-Thru Metal (NTM)
        
        // Floor Construction
        public string FloorConstruction { get; set; } = "None/Aucun"; // Continuously welded seams, 2" bent up lip at perimeter
        public string FloorLiner { get; set; } = "None/Aucun"; // 5 ga. (0.188") aluminum checkerplate
        public string SubfloorLiner { get; set; } = "None/Aucun"; // 18 ga. (0.040") 3003 aluminum
        
        // Drains
        public string AuxiliaryDrains { get; set; } = "Ø1.25"; // 1.25" MPT stainless steel pipe
        public string CrossMemberWidth { get; set; } = "4.50";
        public string FloorMountType { get; set; } = "None/Aucun"; // Concrete Pad
    }

    /// <summary>
    /// Informations de la carrosserie (casing)
    /// </summary>
    public class CasingInfo
    {
        public string PanelConstruction { get; set; } = "None/Aucun"; // No-Thru Metal (NTM)
        public string PanelWidth { get; set; } = "2 in";
        public string PanelInsulation { get; set; } = "None/Aucun"; // 4" Injected Class I Polyurethane closed cell foam (R-26)
        
        // Panel & Liner Material
        public PanelLinerMaterial PanelLinerMaterial { get; set; } = new PanelLinerMaterial();
        
        public string Coating { get; set; } = "No Paint/Non Peint";
        public string SinglePanelLength { get; set; } = "14.875 in";
        public string WallMaterial { get; set; } = "Galvanized";
        public string SelectedExceIRow { get; set; } = "F6";
        public string CriticalPanelLength { get; set; } = "3.5 in";
    }

    /// <summary>
    /// Matériaux des panneaux et doublures
    /// </summary>
    public class PanelLinerMaterial
    {
        public string WallPanelMaterial { get; set; } = "None/Aucun"; // 14 ga. (0.063") 3003 embossed aluminum
        public string WallLinerMaterial { get; set; } = "None/Aucun"; // 18 ga. (0.040") 3003 aluminum
        public string RoofPanelMtl { get; set; } = "None/Aucun";
        public string RoofLinerMtl { get; set; } = "None/Aucun";
    }

    /// <summary>
    /// Informations diverses
    /// </summary>
    public class MiscellaneousInfo
    {
        public string HardwareMaterial { get; set; } = "None / Aucun"; // Zinc coated fasteners, etc.
        public string ShrinkWrap { get; set; } = "None/Aucun"; // Shrink wrap entire unit
        public string Sealant { get; set; } = "None/Aucun"; // Polyether
    }

    /// <summary>
    /// Dimensions d'un module individuel
    /// Chaque module d'une unité peut avoir des dimensions spécifiques
    /// </summary>
    public class ModuleDimension
    {
        /// <summary>
        /// Numéro ou identifiant du module (ex: "Module 1", "Module 2", "Module 13")
        /// </summary>
        public string ModuleNumber { get; set; } = "";

        /// <summary>
        /// Hauteur du module (en inches)
        /// </summary>
        public string Height { get; set; } = "0 in";

        /// <summary>
        /// Largeur du module (en inches)
        /// </summary>
        public string Width { get; set; } = "0 in";

        /// <summary>
        /// Longueur du module (en inches)
        /// </summary>
        public string Length { get; set; } = "0 in";

        // ============================================
        // SECTION: DRAFTER INFO (par module)
        // ============================================
        
        /// <summary>
        /// Nom du dessinateur pour ce module
        /// Inventor: Initiale_du_Dessinateur_Form
        /// </summary>
        public string DrafterName { get; set; } = "";
        
        /// <summary>
        /// Nom du co-dessinateur pour ce module
        /// Inventor: Initiale_du_Co_Dessinateur_Form
        /// </summary>
        public string CoDrafterName { get; set; } = "";
        
        /// <summary>
        /// Date de création du module
        /// Inventor: Creation_Date_Form
        /// </summary>
        public DateTime CreationDate { get; set; } = DateTime.Now;

        // ============================================
        // SECTION: TUNNEL CONFIGURATION
        // ============================================
        
        /// <summary>
        /// Indique si ce module a un tunnel (Top, Bottom, ou les deux)
        /// </summary>
        public bool HasTunnel { get; set; } = false;

        /// <summary>
        /// Position du tunnel: "None", "Top", "Bottom", "Both"
        /// </summary>
        public string TunnelPosition { get; set; } = "None";

        /// <summary>
        /// Hauteur du tunnel Top (en inches) - applicable si TunnelPosition = Top ou Both
        /// </summary>
        public string TunnelTopHeight { get; set; } = "0";

        /// <summary>
        /// Hauteur du tunnel Bottom (en inches) - applicable si TunnelPosition = Bottom ou Both
        /// </summary>
        public string TunnelBottomHeight { get; set; } = "0";

        /// <summary>
        /// Direction du flux d'air: "None", "FrontToBack", "BackToFront", "LeftToRight", "RightToLeft"
        /// </summary>
        public string AirFlowDirection { get; set; } = "None";

        /// <summary>
        /// Type de tunnel: "Tunnel" (avec airflow) ou "Vestibule" (sans airflow, juste passage)
        /// </summary>
        public string TunnelType { get; set; } = "Tunnel";

        // ============================================
        // SECTION: INTERIOR WALLS PER MODULE
        // ============================================
        
        /// <summary>
        /// Indique si ce module contient un mur interieur parallele cote Left (Top on visualizer)
        /// </summary>
        public bool HasInteriorWallLeft { get; set; } = false;
        
        /// <summary>
        /// Distance en pouces du mur interieur Left par rapport au mur gauche
        /// </summary>
        public double InteriorWallLeftDistance { get; set; } = 0;
        
        /// <summary>
        /// Epaisseur du mur interieur Left en pouces (2, 3 ou 4)
        /// </summary>
        public string InteriorWallLeftThickness { get; set; } = "4";
        
        /// <summary>
        /// Indique si ce module contient un mur interieur parallele cote Right (Bottom on visualizer)
        /// </summary>
        public bool HasInteriorWallRight { get; set; } = false;
        
        /// <summary>
        /// Distance en pouces du mur interieur Right par rapport au mur droit
        /// </summary>
        public double InteriorWallRightDistance { get; set; } = 0;
        
        /// <summary>
        /// Epaisseur du mur interieur Right en pouces (2, 3 ou 4)
        /// </summary>
        public string InteriorWallRightThickness { get; set; } = "4";
        
        /// <summary>
        /// Indique si ce module contient un mur interieur parallele a Front
        /// </summary>
        public bool HasInteriorWallFront { get; set; } = false;
        
        /// <summary>
        /// Distance en pouces du mur interieur Front par rapport au mur Front
        /// </summary>
        public double InteriorWallFrontDistance { get; set; } = 0;
        
        /// <summary>
        /// Epaisseur du mur interieur Front en pouces (2, 3 ou 4)
        /// </summary>
        public string InteriorWallFrontThickness { get; set; } = "4";
        
        /// <summary>
        /// Indique si ce module contient un mur interieur parallele a Back
        /// </summary>
        public bool HasInteriorWallBack { get; set; } = false;
        
        /// <summary>
        /// Distance en pouces du mur interieur Back par rapport au mur Back
        /// </summary>
        public double InteriorWallBackDistance { get; set; } = 0;
        
        /// <summary>
        /// Epaisseur du mur interieur Back en pouces (2, 3 ou 4)
        /// </summary>
        public string InteriorWallBackThickness { get; set; } = "4";

        // ============================================
        // SECTION: MODULE TYPE & EXTERIOR WALLS
        // ============================================
        
        /// <summary>
        /// Type de module: First_Module, Middle_Module, Last_Module, Unibase
        /// Inventor: Module_Type_Form
        /// </summary>
        public string ModuleType { get; set; } = "First_Module";
        
        /// <summary>
        /// Mur avant (cote Front) - desactive pour Last_Module (zone liaison)
        /// Inventor: Front_Wall_01
        /// </summary>
        public bool FrontWall01 { get; set; } = true;
        
        /// <summary>
        /// Mur arriere (cote Back) - desactive pour First_Module (zone liaison)
        /// Inventor: Back_Wall_01
        /// </summary>
        public bool BackWall01 { get; set; } = true;
        
        /// <summary>
        /// Mur gauche (cote Left)
        /// Inventor: Left_Wall_01
        /// </summary>
        public bool LeftWall01 { get; set; } = true;
        
        /// <summary>
        /// Mur droit (cote Right)
        /// Inventor: Right_Wall_01
        /// </summary>
        public bool RightWall01 { get; set; } = true;
        
        /// <summary>
        /// Toit
        /// Inventor: Roof_01
        /// </summary>
        public bool Roof01 { get; set; } = true;

        /// <summary>
        /// Description ou notes additionnelles pour ce module
        /// </summary>
        public string Description { get; set; } = "";
    }

    // ============================================
    // SECTION: WALL SPECIFICATION (iLogic Parameters)
    // ============================================

    /// <summary>
    /// Configuration complete des murs - Noms iLogic EXACTS
    /// </summary>
    public class WallSpecification
    {
        // Inventor Name: CustomizeWalls_Form
        public bool CustomizeWalls { get; set; } = false;
        
        public ExteriorWallsConfig ExteriorWalls { get; set; } = new ExteriorWallsConfig();
        public InteriorWallsConfig InteriorWalls { get; set; } = new InteriorWallsConfig();
        public AdditionalWallRoofConfig AdditionalWallRoof { get; set; } = new AdditionalWallRoofConfig();
    }

    #region Exterior Walls

    /// <summary>
    /// Configuration des murs extérieurs (Back, Front, Right, Left, Roof)
    /// Panel Width + Material (Panel + Liner)
    /// </summary>
    public class ExteriorWallsConfig
    {
        public ExteriorWallDetail Back { get; set; } = new ExteriorWallDetail("Back");
        public ExteriorWallDetail Front { get; set; } = new ExteriorWallDetail("Front");
        public ExteriorWallDetail Right { get; set; } = new ExteriorWallDetail("Right");
        public ExteriorWallDetail Left { get; set; } = new ExteriorWallDetail("Left");
        public ExteriorRoofDetail Roof { get; set; } = new ExteriorRoofDetail();
    }

    /// <summary>
    /// Détail d'un mur extérieur avec noms iLogic EXACTS
    /// </summary>
    public class ExteriorWallDetail
    {
        private string _direction; // Back, Front, Right, Left
        
        public ExteriorWallDetail() { _direction = "Back"; }
        public ExteriorWallDetail(string direction) { _direction = direction; }
        
        // Inventor Names - EXACTS depuis iLogic Editor
        public string PanelWidthParameterName => $"{_direction}_WallPanel_Panel_Width_Form";
        public string PanelMaterialParameterName => $"{_direction}_WallPanelSMStyleName_Form";
        public string LinerMaterialParameterName => $"{_direction}_WallLinerSMStyleName_Form";
        
        // Valeurs
        public string PanelWidth { get; set; } = "2 in";
        public string PanelMaterial { get; set; } = "None/Aucun";
        public string LinerMaterial { get; set; } = "None/Aucun";
    }

    /// <summary>
    /// Détail du Roof (noms différents)
    /// </summary>
    public class ExteriorRoofDetail
    {
        // Inventor Names - EXACTS depuis iLogic Editor
        public string PanelWidthParameterName => "RoofPanel_Panel_Width_Form"; // À vérifier dans params.xml
        public string PanelMaterialParameterName => "RoofPanelSMStyleName_Form";
        public string LinerMaterialParameterName => "RoofLinerSMStyleName_Form";
        
        // Valeurs
        public string PanelWidth { get; set; } = "2 in";
        public string PanelMaterial { get; set; } = "None/Aucun";
        public string LinerMaterial { get; set; } = "None/Aucun";
    }

    #endregion

    #region Interior Walls

    public class InteriorWallsConfig
    {
        public ParallelToRightLeftConfig ParallelToRightLeft { get; set; } = new ParallelToRightLeftConfig();
        public ParallelToFrontBackConfig ParallelToFrontBack { get; set; } = new ParallelToFrontBackConfig();
    }

    public class ParallelToRightLeftConfig
    {
        public InteriorWallDetail InteriorWall01 { get; set; } = new InteriorWallDetail("First");
        public InteriorWallDetail InteriorWall02 { get; set; } = new InteriorWallDetail("Second");
    }

    /// <summary>
    /// Detail d'un mur interieur avec noms iLogic
    /// </summary>
    public class InteriorWallDetail
    {
        private string _prefix; // "First" ou "Second"
        
        public InteriorWallDetail() { _prefix = "First"; }
        public InteriorWallDetail(string prefix) { _prefix = prefix; }
        
        // Inventor Names generees automatiquement
        public string IncludeParameterName => $"Include_{_prefix}_Internal_Wall_Form";
        public string PositionParameterName => $"{_prefix}_Internal_Wall_Position_Form";
        public string CustomizeConstructionParameterName => $"Customize_{_prefix}_Internal_WallPanelConstruction_Form";
        public string PanelInsulationParameterName => $"{_prefix}_Internal_WallPanel_Insulation_Form";
        public string PanelConstructionParameterName => $"{_prefix}_Internal_WallPanel_Construction_Form";
        public string PanelWidthParameterName => $"{_prefix}_Internal_WallPanel_Panel_Width_Form";
        public string StaticPressureParameterName => $"{_prefix}_Internal_Wall_StaticPressure_Form";
        public string CustomizeMaterialParameterName => $"Customize_{_prefix}_Internal_WallPanelMaterial_Form";
        public string PanelMaterialParameterName => $"{_prefix}_Internal_WallPanelSMStyleName_Form";
        public string LinerMaterialParameterName => $"{_prefix}_Internal_WallLinerSMStyleName_Form";
        
        // Valeurs
        public bool Include { get; set; } = false;
        public string Position { get; set; } = "20 in";
        public bool CustomizeConstruction { get; set; } = false;
        public string PanelInsulation { get; set; } = "None/Aucun";
        public string PanelConstruction { get; set; } = "None/Aucun";
        public string PanelWidth { get; set; } = "2.0 in";
        public string StaticPressure { get; set; } = "12 ul";
        public bool CustomizeMaterial { get; set; } = false;
        public string PanelMaterial { get; set; } = "None/Aucun";
        public string LinerMaterial { get; set; } = "None/Aucun";
    }

    public class ParallelToFrontBackConfig
    {
        public TunnelConfig RightTunnel { get; set; } = new TunnelConfig();
        public TunnelConfig LeftTunnel { get; set; } = new TunnelConfig();
        public TunnelConfig MiddleTunnel { get; set; } = new TunnelConfig();
    }

    public class TunnelConfig
    {
        public InteriorWallDetail Wall1 { get; set; } = new InteriorWallDetail();
        public InteriorWallDetail Wall2 { get; set; } = new InteriorWallDetail();
    }

    #endregion

    #region Additional Wall / Roof (02 to 05)

    public class AdditionalWallRoofConfig
    {
        public DirectionWallsConfig Back { get; set; } = new DirectionWallsConfig("Back");
        public DirectionWallsConfig Front { get; set; } = new DirectionWallsConfig("Front");
        public DirectionWallsConfig Right { get; set; } = new DirectionWallsConfig("Right");
        public DirectionWallsConfig Left { get; set; } = new DirectionWallsConfig("Left");
        public RoofWallsConfig Roof { get; set; } = new RoofWallsConfig();
    }

    /// <summary>
    /// Configuration des murs additionnels 02-05 pour une direction
    /// </summary>
    public class DirectionWallsConfig
    {
        private string _direction;
        
        public DirectionWallsConfig() { _direction = "Back"; Initialize(); }
        public DirectionWallsConfig(string direction) { _direction = direction; Initialize(); }
        
        public AdditionalWallDetail Wall02 { get; set; }
        public AdditionalWallDetail Wall03 { get; set; }
        public AdditionalWallDetail Wall04 { get; set; }
        public AdditionalWallDetail Wall05 { get; set; }
        
        public void Initialize()
        {
            Wall02 = new AdditionalWallDetail(_direction, "02");
            Wall03 = new AdditionalWallDetail(_direction, "03");
            Wall04 = new AdditionalWallDetail(_direction, "04");
            Wall05 = new AdditionalWallDetail(_direction, "05");
        }
    }

    /// <summary>
    /// Detail d'un mur additionnel avec noms iLogic EXACTS
    /// </summary>
    public class AdditionalWallDetail
    {
        private string _direction; // Back, Front, Right, Left
        private string _number;    // 02, 03, 04, 05
        
        public AdditionalWallDetail() { _direction = "Back"; _number = "02"; }
        public AdditionalWallDetail(string direction, string number) 
        { 
            _direction = direction; 
            _number = number; 
        }
        
        // Inventor Names - EXACTS depuis iLogic Editor
        public string IncludeParameterName => $"Include_{_direction}_Wall_{_number}_Form";
        public string BottomPositionParameterName => $"{_direction}_Wall_{_number}_Bottom_Position_Form";
        public string LeftPositionParameterName => $"{_direction}_Wall_{_number}_Left_Position_Form";
        public string OutPositionParameterName => $"{_direction}_Wall_{_number}_Out_Position_Form";
        
        // Valeurs
        public bool Include { get; set; } = false;
        public string DistToBottom { get; set; } = "0 in";
        public string DistToLeft { get; set; } = "1.989 in";   // ou Dist_To_Right, Dist_To_Front selon direction
        public string DistToOut { get; set; } = "0.136 in";    // Dist_To_Back, Dist_To_Front, etc.
    }

    /// <summary>
    /// Configuration specifique pour le Roof (noms differents)
    /// </summary>
    public class RoofWallsConfig
    {
        public RoofDetail Roof02 { get; set; } = new RoofDetail("02");
        public RoofDetail Roof03 { get; set; } = new RoofDetail("03");
        public RoofDetail Roof04 { get; set; } = new RoofDetail("04");
        public RoofDetail Roof05 { get; set; } = new RoofDetail("05");
    }

    public class RoofDetail
    {
        private string _number;
        
        public RoofDetail() { _number = "02"; }
        public RoofDetail(string number) { _number = number; }
        
        // Inventor Names - EXACTS depuis iLogic Editor
        public string IncludeParameterName => $"Include_Roof_{_number}_Form";
        public string BackPositionParameterName => $"Roof_{_number}_Back_Position_Form";
        public string LeftPositionParameterName => $"Roof_{_number}_Left_Position_Form";
        public string OutPositionParameterName => $"Roof_{_number}_Out_Position_Form";
        
        // Valeurs
        public bool Include { get; set; } = false;
        public string DistToBack { get; set; } = "-0.136 in";
        public string DistToLeft { get; set; } = "-0.136 in";
        public string DistToTop { get; set; } = "0 in";
    }

    #endregion

    #region Modular Brackets

    /// <summary>
    /// Configuration des brackets modulaires
    /// Structure iLogic: Exterior/Interior -> Front/Back -> Left/Right -> Top/Bottom
    /// </summary>
    public class ModularBrackets
    {
        // Exterior Brackets
        public bool ExteriorBracketsRequired { get; set; } = false;
        public ExteriorBracketsConfig ExteriorBrackets { get; set; } = new ExteriorBracketsConfig();

        // Interior Brackets
        public bool InteriorBracketsRequired { get; set; } = false;
        public InteriorBracketsConfig InteriorBrackets { get; set; } = new InteriorBracketsConfig();
    }

    /// <summary>
    /// Configuration des brackets extérieurs
    /// Face: Front / Back
    /// Position: Left/Right (Top/Bottom pour chaque)
    /// </summary>
    public class ExteriorBracketsConfig
    {
        public ExteriorFaceConfig Front { get; set; } = new ExteriorFaceConfig();
        public ExteriorFaceConfig Back { get; set; } = new ExteriorFaceConfig();
    }

    /// <summary>
    /// Configuration d'une face (Front ou Back) pour brackets extérieurs
    /// </summary>
    public class ExteriorFaceConfig
    {
        public ExteriorSideConfig Left { get; set; } = new ExteriorSideConfig();
        public ExteriorSideConfig Right { get; set; } = new ExteriorSideConfig();
    }

    /// <summary>
    /// Configuration d'un côté (Left ou Right) avec brackets Top et Bottom
    /// </summary>
    public class ExteriorSideConfig
    {
        public BracketDetail Top { get; set; } = new BracketDetail();
        public BracketDetail Bottom { get; set; } = new BracketDetail();
    }

    /// <summary>
    /// Configuration des brackets intérieurs
    /// Face: Front / Back
    /// Position: Left/Right (Top/Bottom pour chaque)
    /// </summary>
    public class InteriorBracketsConfig
    {
        public InteriorFaceConfig Front { get; set; } = new InteriorFaceConfig();
        public InteriorFaceConfig Back { get; set; } = new InteriorFaceConfig();
    }

    /// <summary>
    /// Configuration d'une face (Front ou Back) pour brackets intérieurs
    /// </summary>
    public class InteriorFaceConfig
    {
        public InteriorSideConfig Left { get; set; } = new InteriorSideConfig();
        public InteriorSideConfig Right { get; set; } = new InteriorSideConfig();
    }

    /// <summary>
    /// Configuration d'un côté (Left ou Right) avec brackets Top et Bottom
    /// </summary>
    public class InteriorSideConfig
    {
        public BracketDetail Top { get; set; } = new BracketDetail();
        public BracketDetail Bottom { get; set; } = new BracketDetail();
    }

    /// <summary>
    /// Détail d'un bracket individuel
    /// Inventor Parameter Names: Ex_Ext_Left_Front_Bracket_Required_Form, etc.
    /// </summary>
    public class BracketDetail
    {
        // Inventor Name: Ex_Ext_Left_Front_Bracket_Required_Form (exemple)
        public string RequiredParameterName { get; set; } = "";

        // Inventor Name: Ex_Ext_Left_Front_Customize_Position_Form (exemple)
        public string CustomizePositionParameterName { get; set; } = "";

        // Inventor Name: Ex_Ext_Left_Front_Distance_Form (exemple)
        public string DistanceParameterName { get; set; } = "";

        // Valeurs
        public bool Required { get; set; } = false;
        public bool CustomizePosition { get; set; } = false;
        public string Distance { get; set; } = "8 in";  // Toujours en inch
    }

    #endregion
}