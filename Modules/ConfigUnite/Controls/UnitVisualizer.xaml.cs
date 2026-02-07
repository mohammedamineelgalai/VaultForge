using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using XnrgyEngineeringAutomationTools.Modules.ConfigUnite.Models;
using XnrgyEngineeringAutomationTools.Services;

namespace XnrgyEngineeringAutomationTools.Modules.ConfigUnite.Controls
{
    /// <summary>
    /// UserControl pour visualiser graphiquement les modules d'une unite AHU
    /// Vue en PLAN (Top View) - Les modules sont COLLES comme un container
    /// Les tunnels sont des extensions au-dessus/en-dessous du module principal
    /// Supporte maintenant les tunnels globaux (Right/Left/Middle) et Interior Walls
    /// </summary>
    public partial class UnitVisualizer : UserControl
    {
        // Couleurs XNRGY
        private readonly SolidColorBrush _moduleFill = new SolidColorBrush(Color.FromRgb(0x4A, 0x7F, 0xBF));
        private readonly SolidColorBrush _moduleBorder = new SolidColorBrush(Color.FromRgb(0x3D, 0x5A, 0x80));
        private readonly SolidColorBrush _tunnelFill = new SolidColorBrush(Color.FromRgb(0x6C, 0x5C, 0xE7));
        private readonly SolidColorBrush _tunnelRightFill = new SolidColorBrush(Color.FromRgb(0xFF, 0xB7, 0x4D)); // Orange pour Right
        private readonly SolidColorBrush _tunnelLeftFill = new SolidColorBrush(Color.FromRgb(0x64, 0xB5, 0xF6)); // Bleu pour Left
        private readonly SolidColorBrush _tunnelMiddleFill = new SolidColorBrush(Color.FromRgb(0x81, 0xC7, 0x84)); // Vert pour Middle
        private readonly SolidColorBrush _vestibuleFill = new SolidColorBrush(Color.FromRgb(0xFF, 0xA7, 0x26));
        private readonly SolidColorBrush _wallFill = new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x56));
        private readonly SolidColorBrush _interiorWallFill = new SolidColorBrush(Color.FromRgb(0xE5, 0x73, 0x73)); // Rouge pour Interior Walls
        private readonly SolidColorBrush _exteriorWallFill = new SolidColorBrush(Color.FromRgb(0xFF, 0xA7, 0x26)); // Orange pour Exterior Walls
        private readonly SolidColorBrush _exteriorWallBorder = new SolidColorBrush(Color.FromRgb(0xE6, 0x85, 0x00)); // Orange fonce pour bordure
        private readonly SolidColorBrush _textColor = new SolidColorBrush(Colors.White);
        private readonly SolidColorBrush _dimColor = new SolidColorBrush(Color.FromRgb(0x00, 0xB8, 0x94));
        private readonly SolidColorBrush _arrowColor = new SolidColorBrush(Color.FromRgb(0x00, 0xB8, 0x94));
        
        // Constante epaisseur murs exterieurs pour visualisation
        private const double EXTERIOR_WALL_THICKNESS_PX = 6.0;
        
        // Parametres de dessin - VUE EN PLAN (Top View)
        // En vue plan: X = Length (profondeur du module), Y = Width (largeur du module)
        private const double SCALE_FACTOR = 1.2;      // Pixels par pouce
        private const double WALL_THICKNESS = 4.0;    // Epaisseur mur en pouces (valeur par defaut)
        private const double PADDING = 40;            // Padding autour du dessin
        private const double MIN_MODULE_LENGTH = 50;  // Longueur minimum visible
        private const double MIN_MODULE_WIDTH = 80;   // Largeur minimum visible
        private const double TUNNEL_HEIGHT = 40;      // Hauteur tunnel en pixels (visuel)
        private const double GLOBAL_TUNNEL_WIDTH = 50; // Largeur tunnel global en pixels
        
        private List<ModuleDimension> _modules = new List<ModuleDimension>();
        private double _wallThicknessInches = 4.0;    // Lue depuis CasingInfo.PanelWidth
        
        // Configuration globale des tunnels (depuis UnitSpecification)
        private bool _hasTunnelRight = false;
        private bool _hasTunnelLeft = false;
        private bool _hasTunnelMiddle = false;
        
        // AirFlow direction pour chaque tunnel (depuis UnitSpecification)
        private string _airFlowRight = "None/Aucun";
        private string _airFlowLeft = "None/Aucun";
        private string _airFlowMiddle = "None/Aucun";
        
        // Configuration Interior Walls paralleles R/L
        private bool _hasInteriorWall01 = false;
        private double _interiorWall01Position = 0;
        private bool _hasInteriorWall02 = false;
        private double _interiorWall02Position = 0;
        
        // Parametres de zoom
        private double _currentZoom = 1.0;
        private const double ZOOM_MIN = 0.25;
        private const double ZOOM_MAX = 4.0;
        private const double ZOOM_STEP = 0.1;
        
        // Mode d'affichage
        private bool _isStackedMode = false;
        
        // Event pour notifier le parent
        public event EventHandler ApplyClicked;

        public UnitVisualizer()
        {
            InitializeComponent();
            Logger.Log("[UnitVisualizer] [+] Composant initialise", Logger.LogLevel.DEBUG);
        }
        
        /// <summary>
        /// Gestion du zoom avec Ctrl + Molette souris
        /// </summary>
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                
                // Calculer le nouveau zoom
                double delta = e.Delta > 0 ? ZOOM_STEP : -ZOOM_STEP;
                double newZoom = Math.Max(ZOOM_MIN, Math.Min(ZOOM_MAX, _currentZoom + delta));
                
                if (Math.Abs(newZoom - _currentZoom) > 0.001)
                {
                    _currentZoom = newZoom;
                    ApplyZoom();
                }
            }
        }
        
        /// <summary>
        /// Reset du zoom sur double-clic
        /// </summary>
        private void ScrollViewer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _currentZoom = 1.0;
            ApplyZoom();
        }
        
        /// <summary>
        /// Applique le niveau de zoom actuel a tous les canvas
        /// </summary>
        private void ApplyZoom()
        {
            // Appliquer aux ScaleTransform
            if (ScaleSingle != null)
            {
                ScaleSingle.ScaleX = _currentZoom;
                ScaleSingle.ScaleY = _currentZoom;
            }
            if (ScaleTop != null)
            {
                ScaleTop.ScaleX = _currentZoom;
                ScaleTop.ScaleY = _currentZoom;
            }
            if (ScaleBottom != null)
            {
                ScaleBottom.ScaleX = _currentZoom;
                ScaleBottom.ScaleY = _currentZoom;
            }
            
            // Mettre a jour l'indicateur de zoom
            if (TxtZoomLevel != null)
            {
                TxtZoomLevel.Text = $"{_currentZoom * 100:0}%";
            }
            
            Logger.Log($"[UnitVisualizer] [>] Zoom: {_currentZoom * 100:0}%", Logger.LogLevel.DEBUG);
        }

        /// <summary>
        /// Met a jour la liste des modules et redessine
        /// </summary>
        public void UpdateModules(List<ModuleDimension> modules)
        {
            _modules = modules ?? new List<ModuleDimension>();
            Logger.Log($"[UnitVisualizer] [>] Mise a jour: {_modules.Count} module(s)", Logger.LogLevel.DEBUG);
            DrawModules();
        }
        
        /// <summary>
        /// Met a jour avec la configuration complete (tunnels globaux + interior walls)
        /// </summary>
        public void UpdateWithConfig(ConfigUniteDataModel config)
        {
            if (config == null) return;
            
            _modules = config.ModuleDimensions ?? new List<ModuleDimension>();
            
            // Tunnels globaux depuis UnitSpecification
            if (config.UnitSpecification != null)
            {
                _hasTunnelRight = config.UnitSpecification.Tunnel1Right;
                _hasTunnelLeft = config.UnitSpecification.Tunnel2Left;
                _hasTunnelMiddle = config.UnitSpecification.Tunnel3Middle;
                
                // AirFlow direction pour chaque tunnel
                _airFlowRight = config.UnitSpecification.AirFlowRight ?? "None/Aucun";
                _airFlowLeft = config.UnitSpecification.AirFlowLeft ?? "None/Aucun";
                _airFlowMiddle = config.UnitSpecification.AirFlowMiddle ?? "None/Aucun";
            }
            
            // Interior Walls paralleles R/L
            if (config.WallSpecification?.InteriorWalls?.ParallelToRightLeft != null)
            {
                var parallelRL = config.WallSpecification.InteriorWalls.ParallelToRightLeft;
                _hasInteriorWall01 = parallelRL.InteriorWall01?.Include ?? false;
                _interiorWall01Position = ParseDimension(parallelRL.InteriorWall01?.Position, 0);
                _hasInteriorWall02 = parallelRL.InteriorWall02?.Include ?? false;
                _interiorWall02Position = ParseDimension(parallelRL.InteriorWall02?.Position, 0);
            }
            
            Logger.Log($"[UnitVisualizer] [>] Config complete: {_modules.Count} module(s), Tunnels R={_hasTunnelRight}({_airFlowRight}) L={_hasTunnelLeft}({_airFlowLeft}) M={_hasTunnelMiddle}({_airFlowMiddle}), Walls={_hasInteriorWall01}/{_hasInteriorWall02}", Logger.LogLevel.DEBUG);
            DrawModules();
        }

        /// <summary>
        /// Definir l'epaisseur du mur (depuis CasingInfo.PanelWidth)
        /// </summary>
        public void SetWallThickness(double thicknessInches)
        {
            _wallThicknessInches = thicknessInches;
            TxtWallThickness.Text = $"{thicknessInches:0} in";
        }

        /// <summary>
        /// Evenement declenche quand les onglets doivent etre affiches (mode Stacked)
        /// </summary>
        public event EventHandler<bool> StackedModeChanged;

        /// <summary>
        /// Selectionne un onglet (0=TOP, 1=BOTTOM) - appele depuis le parent
        /// </summary>
        public void SelectTab(int tabIndex)
        {
            if (TabViews != null && TabViews.Visibility == Visibility.Visible)
            {
                TabViews.SelectedIndex = tabIndex;
                Logger.Log($"[UnitVisualizer] [>] Tab selectionne: {(tabIndex == 0 ? "TOP" : "BOTTOM")}", Logger.LogLevel.DEBUG);
            }
        }

        /// <summary>
        /// Retourne true si le mode Stacked est actif (onglets visibles)
        /// </summary>
        public bool IsStackedMode => TabViews?.Visibility == Visibility.Visible;

        /// <summary>
        /// Methode publique pour forcer le refresh (appelee depuis le parent)
        /// </summary>
        public void RefreshView()
        {
            Logger.Log("[UnitVisualizer] [>] Refresh demande", Logger.LogLevel.DEBUG);
            DrawModules();
            ApplyClicked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Redessine tous les modules sur le canvas - VUE EN PLAN avec ZOOM
        /// Supporte les unites STACKED avec onglets separes (TOP et BOTTOM)
        /// Zoom: Ctrl+Scroll | Reset: Double-clic
        /// </summary>
        public void DrawModules()
        {
            // Vider tous les canvas
            CanvasModules.Children.Clear();
            CanvasTop?.Children.Clear();
            CanvasBottom?.Children.Clear();
            
            // Mettre a jour le compteur
            TxtModuleCount.Text = $"{_modules.Count} module(s)";
            
            // Gerer l'affichage du message "Aucun module"
            if (PanelNoModules != null)
            {
                PanelNoModules.Visibility = _modules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            
            if (_modules.Count == 0)
            {
                TxtTotalDimensions.Text = "Total: 0 in";
                CanvasModules.Width = 400;
                CanvasModules.Height = 200;
                TabViews.Visibility = Visibility.Collapsed;
                SingleViewBorder.Visibility = Visibility.Visible;
                return;
            }
            
            // Separer les modules par position (Top, Bottom, Standard)
            var topModules = _modules.Where(m => m.TunnelPosition == "Top").ToList();
            var bottomModules = _modules.Where(m => m.TunnelPosition == "Bottom").ToList();
            var standardModules = _modules.Where(m => 
                m.TunnelPosition == "Standard" || 
                string.IsNullOrEmpty(m.TunnelPosition) || 
                m.TunnelPosition == "None").ToList();
            
            bool isStacked = topModules.Count > 0 && bottomModules.Count > 0;
            _isStackedMode = isStacked;
            
            if (isStacked)
            {
                // ========== UNITE STACKED: Onglets TOP et BOTTOM ==========
                Logger.Log($"[UnitVisualizer] [>] Mode STACKED avec onglets: {topModules.Count} TOP, {bottomModules.Count} BOTTOM", Logger.LogLevel.DEBUG);
                
                // Afficher les onglets, cacher la vue unique
                TabViews.Visibility = Visibility.Visible;
                SingleViewBorder.Visibility = Visibility.Collapsed;
                
                // Notifier le parent pour afficher ses onglets dans le header
                StackedModeChanged?.Invoke(this, true);
                
                // Dessiner la vue TOP sur CanvasTop
                double topWidth, topHeight;
                DrawUnitViewOnCanvas(CanvasTop, topModules, "TOP UNIT", out topWidth, out topHeight);
                CanvasTop.Width = topWidth;
                CanvasTop.Height = topHeight;
                
                // Dessiner la vue BOTTOM sur CanvasBottom
                double bottomWidth, bottomHeight;
                DrawUnitViewOnCanvas(CanvasBottom, bottomModules, "BOTTOM UNIT", out bottomWidth, out bottomHeight);
                CanvasBottom.Width = bottomWidth;
                CanvasBottom.Height = bottomHeight;
                
                // Mettre a jour les dimensions totales affichees
                double totalTopLength = topModules.Sum(m => ParseDimension(m.Length, 48)) + (topModules.Count - 1) * _wallThicknessInches;
                double totalBottomLength = bottomModules.Sum(m => ParseDimension(m.Length, 48)) + (bottomModules.Count - 1) * _wallThicknessInches;
                TxtTotalDimensions.Text = $"Top: {totalTopLength:0} in | Bottom: {totalBottomLength:0} in";
            }
            else
            {
                // ========== UNITE STANDARD: Vue unique ==========
                var modulesToDraw = standardModules.Count > 0 ? standardModules : 
                                    topModules.Count > 0 ? topModules : 
                                    bottomModules.Count > 0 ? bottomModules : _modules.ToList();
                
                // Cacher les onglets, afficher la vue unique
                TabViews.Visibility = Visibility.Collapsed;
                SingleViewBorder.Visibility = Visibility.Visible;
                
                // Notifier le parent pour masquer ses onglets
                StackedModeChanged?.Invoke(this, false);
                
                string viewTitle = topModules.Count > 0 ? "TOP UNIT" : 
                                   bottomModules.Count > 0 ? "BOTTOM UNIT" : "";
                
                double viewWidth, viewHeight;
                DrawUnitViewOnCanvas(CanvasModules, modulesToDraw, viewTitle, out viewWidth, out viewHeight);
                CanvasModules.Width = viewWidth;
                CanvasModules.Height = viewHeight;
                
                // Mettre a jour les dimensions totales
                double totalLength = modulesToDraw.Sum(m => ParseDimension(m.Length, 48)) + (modulesToDraw.Count - 1) * _wallThicknessInches;
                double maxWidth = modulesToDraw.Max(m => ParseDimension(m.Width, 100));
                TxtTotalDimensions.Text = $"Total: {totalLength:0} in x {maxWidth:0} in";
            }
            
            // Reset zoom a 100% par defaut (Fit All)
            _currentZoom = 1.0;
            ApplyZoom();
            
            Logger.Log($"[UnitVisualizer] [+] Dessin termine: {_modules.Count} module(s), Stacked={isStacked}", Logger.LogLevel.DEBUG);
        }
        
        /// <summary>
        /// Dessine une vue complete d'unite sur un Canvas specifique
        /// </summary>
        private void DrawUnitViewOnCanvas(Canvas targetCanvas, List<ModuleDimension> modules, string viewTitle, out double viewWidth, out double viewHeight)
        {
            if (targetCanvas == null || modules.Count == 0)
            {
                viewWidth = 400;
                viewHeight = 200;
                return;
            }
            
            double wallPixels = _wallThicknessInches * SCALE_FACTOR;
            double currentX = PADDING / 2;
            double baseY = PADDING / 2 + TUNNEL_HEIGHT + 25;
            
            // Dessiner chaque module
            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                
                double length = ParseDimension(module.Length, 48);
                double width = ParseDimension(module.Width, 100);
                double height = ParseDimension(module.Height, 100);
                
                double pixelLength = Math.Max(length * SCALE_FACTOR, MIN_MODULE_LENGTH);
                double pixelWidth = Math.Max(width * SCALE_FACTOR, MIN_MODULE_WIDTH);
                
                DrawModuleBoxOnCanvas(targetCanvas, module, currentX, baseY, pixelLength, pixelWidth, length, width, height, i + 1);
                DrawModuleInteriorWallsOnCanvas(targetCanvas, module, currentX, baseY, pixelLength, pixelWidth, width);
                
                currentX += pixelLength;
                
                if (i < modules.Count - 1)
                {
                    double maxPixelWidthForWall = modules.Max(m => ParseDimension(m.Width, 100)) * SCALE_FACTOR;
                    DrawWallOnCanvas(targetCanvas, currentX, baseY, wallPixels, maxPixelWidthForWall);
                    currentX += wallPixels;
                }
            }
            
            double maxPixelWidth = Math.Max(modules.Max(m => ParseDimension(m.Width, 100)) * SCALE_FACTOR, MIN_MODULE_WIDTH);
            double unitStartX = PADDING / 2;
            double unitEndX = currentX - PADDING / 2 + 10;
            
            DrawUnitFrameOnCanvas(targetCanvas, unitStartX - 5, baseY - 5, unitEndX, maxPixelWidth + 10);
            DrawOrientationLabelsOnCanvas(targetCanvas, unitStartX, baseY, unitEndX, maxPixelWidth, modules, viewTitle);
            DrawGlobalTunnelsOnCanvas(targetCanvas, unitStartX, baseY, unitEndX, maxPixelWidth);
            
            viewWidth = currentX + PADDING + (_hasTunnelRight || _hasTunnelLeft ? GLOBAL_TUNNEL_WIDTH + 20 : 0);
            viewHeight = baseY + maxPixelWidth + TUNNEL_HEIGHT + PADDING;
        }
        
        /// <summary>
        /// Dessine les labels d'orientation pour une vue specifique sur un canvas cible
        /// </summary>
        private void DrawOrientationLabelsOnCanvas(Canvas targetCanvas, double startX, double startY, double endX, double unitHeight, List<ModuleDimension> modules, string viewTitle = "")
        {
            // Label BACK a gauche
            var backLabel = new TextBlock
            {
                Text = "BACK",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)), // Orange
                Opacity = 0.8
            };
            backLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(backLabel, startX - backLabel.DesiredSize.Width - 8);
            Canvas.SetTop(backLabel, startY + unitHeight / 2 - backLabel.DesiredSize.Height / 2);
            targetCanvas.Children.Add(backLabel);
            
            // Label FRONT a droite
            var frontLabel = new TextBlock
            {
                Text = "FRONT",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)), // Vert
                Opacity = 0.8
            };
            frontLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(frontLabel, endX + 8);
            Canvas.SetTop(frontLabel, startY + unitHeight / 2 - frontLabel.DesiredSize.Height / 2);
            targetCanvas.Children.Add(frontLabel);
            
            // ======== LABELS LEFT / RIGHT (Top / Bottom du visualiseur) ========
            double unitWidth = endX - startX;
            double centerX = startX + (unitWidth / 2);
            
            // Label LEFT en haut
            var leftLabel = new TextBlock
            {
                Text = "LEFT",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0xB5, 0xF6)),
                Opacity = 0.8
            };
            leftLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(leftLabel, centerX - leftLabel.DesiredSize.Width / 2);
            Canvas.SetTop(leftLabel, startY - 22);
            targetCanvas.Children.Add(leftLabel);
            
            // Label RIGHT en bas
            var rightLabel = new TextBlock
            {
                Text = "RIGHT",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xB7, 0x4D)),
                Opacity = 0.8
            };
            rightLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(rightLabel, centerX - rightLabel.DesiredSize.Width / 2);
            Canvas.SetTop(rightLabel, startY + unitHeight + 6);
            targetCanvas.Children.Add(rightLabel);
            
            // Titre de la vue (TOP UNIT / BOTTOM UNIT) en bas de RIGHT, centre avec lui
            if (!string.IsNullOrEmpty(viewTitle))
            {
                var titleLabel = new TextBlock
                {
                    Text = viewTitle,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)), // Jaune/Or
                    Opacity = 0.9
                };
                titleLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                // Placer juste en dessous de RIGHT, centre
                Canvas.SetLeft(titleLabel, centerX - titleLabel.DesiredSize.Width / 2);
                Canvas.SetTop(titleLabel, startY + unitHeight + 6 + rightLabel.DesiredSize.Height + 4);
                targetCanvas.Children.Add(titleLabel);
            }
        }
        
        /// <summary>
        /// Dessine le cadre de l'unite sur un canvas specifique
        /// </summary>
        private void DrawUnitFrameOnCanvas(Canvas targetCanvas, double x, double y, double width, double height)
        {
            var frame = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x77)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 5, 3 }
            };
            
            Canvas.SetLeft(frame, x);
            Canvas.SetTop(frame, y);
            targetCanvas.Children.Add(frame);
        }
        
        /// <summary>
        /// Dessine un mur entre modules sur un canvas specifique
        /// </summary>
        private void DrawWallOnCanvas(Canvas targetCanvas, double x, double y, double thickness, double height)
        {
            var wall = new Rectangle
            {
                Width = thickness,
                Height = height,
                Fill = _wallFill,
                Stroke = _wallFill,
                StrokeThickness = 0
            };
            
            Canvas.SetLeft(wall, x);
            Canvas.SetTop(wall, y);
            targetCanvas.Children.Add(wall);
        }
        
        /// <summary>
        /// Dessine la boite d'un module sur un canvas specifique
        /// </summary>
        private void DrawModuleBoxOnCanvas(Canvas targetCanvas, ModuleDimension module, double x, double y, 
                                           double pixelLength, double pixelWidth,
                                           double realLength, double realWidth, double realHeight, int index)
        {
            // Rectangle principal du module
            var rect = new Rectangle
            {
                Width = pixelLength,
                Height = pixelWidth,
                Fill = _moduleFill,
                Stroke = _moduleBorder,
                StrokeThickness = 1,
                Opacity = 0.85
            };
            
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            targetCanvas.Children.Add(rect);
            
            // ======== MURS EXTERIEURS ========
            DrawExteriorWallsOnCanvas(targetCanvas, module, x, y, pixelLength, pixelWidth);
            
            // Numero du module (en haut)
            string moduleNum = module.ModuleNumber ?? $"M{index}";
            var moduleLabel = new TextBlock
            {
                Text = moduleNum,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = _textColor
            };
            
            moduleLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double labelX = x + (pixelLength - moduleLabel.DesiredSize.Width) / 2;
            double labelY = y + 8;
            
            Canvas.SetLeft(moduleLabel, labelX);
            Canvas.SetTop(moduleLabel, labelY);
            targetCanvas.Children.Add(moduleLabel);
            
            // Dimensions (au centre du module)
            string dimText = $"{realLength:0}\"";
            var dimLabel = new TextBlock
            {
                Text = dimText,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = _dimColor
            };
            
            dimLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double dimX = x + (pixelLength - dimLabel.DesiredSize.Width) / 2;
            double dimY = y + (pixelWidth / 2) - 5;
            
            Canvas.SetLeft(dimLabel, dimX);
            Canvas.SetTop(dimLabel, dimY);
            targetCanvas.Children.Add(dimLabel);
        }
        
        /// <summary>
        /// Dessine les murs exterieurs autour d'un module (Front, Back, Left, Right)
        /// Meme couleur orange que les murs interieurs
        /// </summary>
        private void DrawExteriorWallsOnCanvas(Canvas targetCanvas, ModuleDimension module, double x, double y, 
                                                double pixelLength, double pixelWidth)
        {
            double wallThickness = EXTERIOR_WALL_THICKNESS_PX;
            
            // Mur LEFT (en haut du visualiseur, horizontale)
            if (module.LeftWall01)
            {
                var leftWall = new Rectangle
                {
                    Width = pixelLength,
                    Height = wallThickness,
                    Fill = _exteriorWallFill,
                    Stroke = _exteriorWallBorder,
                    StrokeThickness = 0.5,
                    Opacity = 0.9
                };
                Canvas.SetLeft(leftWall, x);
                Canvas.SetTop(leftWall, y - wallThickness);
                targetCanvas.Children.Add(leftWall);
            }
            
            // Mur RIGHT (en bas du visualiseur, horizontale)
            if (module.RightWall01)
            {
                var rightWall = new Rectangle
                {
                    Width = pixelLength,
                    Height = wallThickness,
                    Fill = _exteriorWallFill,
                    Stroke = _exteriorWallBorder,
                    StrokeThickness = 0.5,
                    Opacity = 0.9
                };
                Canvas.SetLeft(rightWall, x);
                Canvas.SetTop(rightWall, y + pixelWidth);
                targetCanvas.Children.Add(rightWall);
            }
            
            // Mur BACK (a gauche du visualiseur, verticale)
            if (module.BackWall01)
            {
                var backWall = new Rectangle
                {
                    Width = wallThickness,
                    Height = pixelWidth + (module.LeftWall01 ? wallThickness : 0) + (module.RightWall01 ? wallThickness : 0),
                    Fill = _exteriorWallFill,
                    Stroke = _exteriorWallBorder,
                    StrokeThickness = 0.5,
                    Opacity = 0.9
                };
                Canvas.SetLeft(backWall, x - wallThickness);
                Canvas.SetTop(backWall, y - (module.LeftWall01 ? wallThickness : 0));
                targetCanvas.Children.Add(backWall);
            }
            
            // Mur FRONT (a droite du visualiseur, verticale)
            if (module.FrontWall01)
            {
                var frontWall = new Rectangle
                {
                    Width = wallThickness,
                    Height = pixelWidth + (module.LeftWall01 ? wallThickness : 0) + (module.RightWall01 ? wallThickness : 0),
                    Fill = _exteriorWallFill,
                    Stroke = _exteriorWallBorder,
                    StrokeThickness = 0.5,
                    Opacity = 0.9
                };
                Canvas.SetLeft(frontWall, x + pixelLength);
                Canvas.SetTop(frontWall, y - (module.LeftWall01 ? wallThickness : 0));
                targetCanvas.Children.Add(frontWall);
            }
        }
        
        /// <summary>
        /// Dessine les tunnels globaux sur un canvas specifique
        /// </summary>
        private void DrawGlobalTunnelsOnCanvas(Canvas targetCanvas, double startX, double startY, double endX, double unitHeight)
        {
            double tunnelOffset = 10;
            double tunnelWidth = GLOBAL_TUNNEL_WIDTH;
            
            // Tunnel Right (a droite de l'unite)
            if (_hasTunnelRight)
            {
                double tunnelX = endX + tunnelOffset;
                var tunnelRect = new Rectangle
                {
                    Width = tunnelWidth,
                    Height = unitHeight,
                    Fill = _tunnelRightFill,
                    Stroke = _tunnelRightFill,
                    StrokeThickness = 1,
                    Opacity = 0.75
                };
                Canvas.SetLeft(tunnelRect, tunnelX);
                Canvas.SetTop(tunnelRect, startY);
                targetCanvas.Children.Add(tunnelRect);
                
                // Label "RIGHT"
                var label = new TextBlock
                {
                    Text = "RIGHT",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = _textColor
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(label, tunnelX + (tunnelWidth - label.DesiredSize.Width) / 2);
                Canvas.SetTop(label, startY + 5);
                targetCanvas.Children.Add(label);
                
                // AirFlow indicator
                DrawTunnelAirFlowIndicatorOnCanvas(targetCanvas, tunnelX, startY, tunnelWidth, unitHeight, _airFlowRight);
            }
            
            // Tunnel Left (a gauche de l'unite)
            if (_hasTunnelLeft)
            {
                double tunnelX = startX - tunnelWidth - tunnelOffset;
                var tunnelRect = new Rectangle
                {
                    Width = tunnelWidth,
                    Height = unitHeight,
                    Fill = _tunnelLeftFill,
                    Stroke = _tunnelLeftFill,
                    StrokeThickness = 1,
                    Opacity = 0.75
                };
                Canvas.SetLeft(tunnelRect, tunnelX);
                Canvas.SetTop(tunnelRect, startY);
                targetCanvas.Children.Add(tunnelRect);
                
                // Label "LEFT"
                var label = new TextBlock
                {
                    Text = "LEFT",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = _textColor
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(label, tunnelX + (tunnelWidth - label.DesiredSize.Width) / 2);
                Canvas.SetTop(label, startY + 5);
                targetCanvas.Children.Add(label);
                
                // AirFlow indicator
                DrawTunnelAirFlowIndicatorOnCanvas(targetCanvas, tunnelX, startY, tunnelWidth, unitHeight, _airFlowLeft);
            }
            
            // Tunnel Middle (au centre)
            if (_hasTunnelMiddle)
            {
                double middleX = (startX + endX) / 2 - tunnelWidth / 2;
                var tunnelRect = new Rectangle
                {
                    Width = tunnelWidth,
                    Height = unitHeight,
                    Fill = _tunnelMiddleFill,
                    Stroke = _tunnelMiddleFill,
                    StrokeThickness = 1,
                    Opacity = 0.75
                };
                Canvas.SetLeft(tunnelRect, middleX);
                Canvas.SetTop(tunnelRect, startY);
                targetCanvas.Children.Add(tunnelRect);
                
                // Label "MIDDLE"
                var label = new TextBlock
                {
                    Text = "MIDDLE",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = _textColor
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(label, middleX + (tunnelWidth - label.DesiredSize.Width) / 2);
                Canvas.SetTop(label, startY + 5);
                targetCanvas.Children.Add(label);
                
                // AirFlow indicator
                DrawTunnelAirFlowIndicatorOnCanvas(targetCanvas, middleX, startY, tunnelWidth, unitHeight, _airFlowMiddle);
            }
        }
        
        /// <summary>
        /// Dessine l'indicateur AirFlow sur un tunnel (version Canvas specifique)
        /// </summary>
        private void DrawTunnelAirFlowIndicatorOnCanvas(Canvas targetCanvas, double tunnelX, double tunnelY, double tunnelWidth, double tunnelHeight, string airFlow)
        {
            if (string.IsNullOrEmpty(airFlow) || airFlow == "None/Aucun") return;
            
            string indicator = airFlow switch
            {
                "Back-To-Front" => "\u2192",  // Fleche droite
                "Front-To-Back" => "\u2190",  // Fleche gauche
                "Vestibule" => "-V",
                _ => ""
            };
            
            if (string.IsNullOrEmpty(indicator)) return;
            
            var airFlowLabel = new TextBlock
            {
                Text = indicator,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)) // Or
            };
            airFlowLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(airFlowLabel, tunnelX + (tunnelWidth - airFlowLabel.DesiredSize.Width) / 2);
            Canvas.SetTop(airFlowLabel, tunnelY + tunnelHeight / 2 - airFlowLabel.DesiredSize.Height / 2);
            targetCanvas.Children.Add(airFlowLabel);
        }
        
        /// <summary>
        /// Dessine les murs interieurs d'un module sur un canvas specifique
        /// </summary>
        private void DrawModuleInteriorWallsOnCanvas(Canvas targetCanvas, ModuleDimension module, double x, double y, 
                                                      double pixelLength, double pixelWidth, double realWidth)
        {
            // Mur interieur Left (parallele a Right/Left, positionne du cote Top/Left)
            if (module.HasInteriorWallLeft && module.InteriorWallLeftDistance > 0)
            {
                double thickness = ParseWallThickness(module.InteriorWallLeftThickness);
                double distance = module.InteriorWallLeftDistance;
                
                double wallY = y + (distance * SCALE_FACTOR);
                double wallThicknessPixels = thickness * SCALE_FACTOR;
                
                var wallRect = new Rectangle
                {
                    Width = pixelLength,
                    Height = wallThicknessPixels,
                    Fill = _interiorWallFill,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),
                    StrokeThickness = 0.5,
                    Opacity = 0.85
                };
                Canvas.SetLeft(wallRect, x);
                Canvas.SetTop(wallRect, wallY);
                targetCanvas.Children.Add(wallRect);
                
                var distLabel = new TextBlock
                {
                    Text = $"{distance:0}\"",
                    FontSize = 7,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0xCC))
                };
                distLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(distLabel, x + pixelLength - distLabel.DesiredSize.Width - 2);
                Canvas.SetTop(distLabel, wallY + 1);
                targetCanvas.Children.Add(distLabel);
            }
            
            // Mur interieur Right (parallele a Right/Left, positionne du cote Bottom/Right)
            if (module.HasInteriorWallRight && module.InteriorWallRightDistance > 0)
            {
                double thickness = ParseWallThickness(module.InteriorWallRightThickness);
                double distance = module.InteriorWallRightDistance;
                
                double wallY = y + pixelWidth - (distance * SCALE_FACTOR) - (thickness * SCALE_FACTOR);
                double wallThicknessPixels = thickness * SCALE_FACTOR;
                
                var wallRect = new Rectangle
                {
                    Width = pixelLength,
                    Height = wallThicknessPixels,
                    Fill = _interiorWallFill,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),
                    StrokeThickness = 0.5,
                    Opacity = 0.85
                };
                Canvas.SetLeft(wallRect, x);
                Canvas.SetTop(wallRect, wallY);
                targetCanvas.Children.Add(wallRect);
                
                var distLabel = new TextBlock
                {
                    Text = $"{distance:0}\"",
                    FontSize = 7,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0xCC))
                };
                distLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(distLabel, x + pixelLength - distLabel.DesiredSize.Width - 2);
                Canvas.SetTop(distLabel, wallY + 1);
                targetCanvas.Children.Add(distLabel);
            }
            
            // Mur interieur Front (parallele a Front, vertical - cote droit du module)
            if (module.HasInteriorWallFront && module.InteriorWallFrontDistance > 0)
            {
                double thickness = ParseWallThickness(module.InteriorWallFrontThickness);
                double distance = module.InteriorWallFrontDistance;
                
                double wallX = x + pixelLength - (distance * SCALE_FACTOR) - (thickness * SCALE_FACTOR);
                double wallThicknessPixels = thickness * SCALE_FACTOR;
                
                var wallRect = new Rectangle
                {
                    Width = wallThicknessPixels,
                    Height = pixelWidth,
                    Fill = _interiorWallFill,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),
                    StrokeThickness = 0.5,
                    Opacity = 0.85
                };
                Canvas.SetLeft(wallRect, wallX);
                Canvas.SetTop(wallRect, y);
                targetCanvas.Children.Add(wallRect);
            }
            
            // Mur interieur Back (parallele a Back, vertical - cote gauche du module)
            if (module.HasInteriorWallBack && module.InteriorWallBackDistance > 0)
            {
                double thickness = ParseWallThickness(module.InteriorWallBackThickness);
                double distance = module.InteriorWallBackDistance;
                
                double wallX = x + (distance * SCALE_FACTOR);
                double wallThicknessPixels = thickness * SCALE_FACTOR;
                
                var wallRect = new Rectangle
                {
                    Width = wallThicknessPixels,
                    Height = pixelWidth,
                    Fill = _interiorWallFill,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),
                    StrokeThickness = 0.5,
                    Opacity = 0.85
                };
                Canvas.SetLeft(wallRect, wallX);
                Canvas.SetTop(wallRect, y);
                targetCanvas.Children.Add(wallRect);
            }
            
            // ======== Dessin des fleches AirFlow par module ========
            // La fleche est placee en haut (si mur Left) ou en bas (si mur Right) du module
            if (!string.IsNullOrEmpty(module.AirFlowDirection) && module.AirFlowDirection != "None" && module.AirFlowDirection != "None/Aucun")
            {
                // Determiner la direction et position de la fleche
                bool isBackToFront = module.AirFlowDirection == "Back-To-Front";
                bool isFrontToBack = module.AirFlowDirection == "Front-To-Back";
                bool isVestibule = module.AirFlowDirection == "Vestibule";
                
                if (isBackToFront || isFrontToBack || isVestibule)
                {
                    // Determiner la position Y de la fleche selon les murs interieurs
                    double arrowX = x + pixelLength / 2;
                    double arrowY;
                    bool positionTop = false;
                    
                    // Si mur interieur Left: fleche en haut (zone entre Left et mur)
                    if (module.HasInteriorWallLeft && module.InteriorWallLeftDistance > 0)
                    {
                        double wallTopY = y + (module.InteriorWallLeftDistance * SCALE_FACTOR);
                        arrowY = y + (wallTopY - y) / 2; // Milieu entre le haut et le mur
                        positionTop = true;
                    }
                    // Si mur interieur Right: fleche en bas (zone entre mur et Right)
                    else if (module.HasInteriorWallRight && module.InteriorWallRightDistance > 0)
                    {
                        double wallBottomY = y + pixelWidth - (module.InteriorWallRightDistance * SCALE_FACTOR);
                        arrowY = wallBottomY + (y + pixelWidth - wallBottomY) / 2; // Milieu entre le mur et le bas
                    }
                    // Sinon: centre du module
                    else
                    {
                        arrowY = y + pixelWidth / 2;
                    }
                    
                    // Dessiner la fleche elegante avec le mot "AIR"
                    DrawAirFlowArrowOnCanvas(targetCanvas, arrowX, arrowY, pixelLength * 0.6, isBackToFront, isVestibule);
                }
            }
        }
        
        /// <summary>
        /// Dessine une fleche elegante avec le mot "AIR" pour indiquer le sens du flux d'air
        /// </summary>
        private void DrawAirFlowArrowOnCanvas(Canvas targetCanvas, double centerX, double centerY, double arrowLength, bool isBackToFront, bool isVestibule)
        {
            // Couleurs
            var arrowBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF)); // Cyan brillant
            var textBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)); // Blanc
            var glowBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xB8, 0xD4)); // Cyan fonce
            
            double halfLength = arrowLength / 2;
            double arrowHeadSize = 12;
            double lineThickness = 3;
            
            if (isVestibule)
            {
                // Vestibule: juste le texte "V" avec un cercle
                var vestibuleCircle = new Ellipse
                {
                    Width = 24,
                    Height = 24,
                    Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0xA7, 0x26)),
                    Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0x80, 0x00)),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(vestibuleCircle, centerX - 12);
                Canvas.SetTop(vestibuleCircle, centerY - 12);
                targetCanvas.Children.Add(vestibuleCircle);
                
                var vestibuleText = new TextBlock
                {
                    Text = "V",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                };
                vestibuleText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(vestibuleText, centerX - vestibuleText.DesiredSize.Width / 2);
                Canvas.SetTop(vestibuleText, centerY - vestibuleText.DesiredSize.Height / 2);
                targetCanvas.Children.Add(vestibuleText);
                return;
            }
            
            // Direction de la fleche
            double startX = isBackToFront ? centerX - halfLength : centerX + halfLength;
            double endX = isBackToFront ? centerX + halfLength : centerX - halfLength;
            
            // Ligne principale de la fleche (avec effet glow)
            var glowLine = new Line
            {
                X1 = startX,
                Y1 = centerY,
                X2 = endX,
                Y2 = centerY,
                Stroke = glowBrush,
                StrokeThickness = lineThickness + 4,
                Opacity = 0.3,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            targetCanvas.Children.Add(glowLine);
            
            var mainLine = new Line
            {
                X1 = startX,
                Y1 = centerY,
                X2 = endX,
                Y2 = centerY,
                Stroke = arrowBrush,
                StrokeThickness = lineThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            targetCanvas.Children.Add(mainLine);
            
            // Tete de la fleche (triangle)
            double headX = endX;
            double headDirection = isBackToFront ? -1 : 1;
            
            var arrowHead = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(headX, centerY),
                    new Point(headX + headDirection * arrowHeadSize, centerY - arrowHeadSize / 2),
                    new Point(headX + headDirection * arrowHeadSize, centerY + arrowHeadSize / 2)
                },
                Fill = arrowBrush,
                Stroke = glowBrush,
                StrokeThickness = 1
            };
            targetCanvas.Children.Add(arrowHead);
            
            // Texte "AIR" au centre de la fleche
            var airText = new TextBlock
            {
                Text = "AIR",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = textBrush
            };
            airText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            
            // Background pour le texte AIR
            var textBg = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 0x00, 0x80, 0xA0)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1, 4, 1),
                Child = new TextBlock
                {
                    Text = "AIR",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = textBrush
                }
            };
            textBg.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(textBg, centerX - textBg.DesiredSize.Width / 2);
            Canvas.SetTop(textBg, centerY - textBg.DesiredSize.Height / 2 - 12); // Au-dessus de la fleche
            targetCanvas.Children.Add(textBg);
        }

        /// <summary>
        /// Dessine la boite d'un module (vue plan)
        /// </summary>
        private void DrawModuleBox(ModuleDimension module, double x, double y, 
                                   double pixelLength, double pixelWidth,
                                   double realLength, double realWidth, double realHeight, int index)
        {
            // Rectangle principal du module
            var rect = new Rectangle
            {
                Width = pixelLength,
                Height = pixelWidth,
                Fill = _moduleFill,
                Stroke = _moduleBorder,
                StrokeThickness = 1,
                Opacity = 0.85
            };
            
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            CanvasModules.Children.Add(rect);
            
            // Numero du module (en haut)
            string moduleNum = module.ModuleNumber ?? $"M{index}";
            var moduleLabel = new TextBlock
            {
                Text = moduleNum,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = _textColor
            };
            
            moduleLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double labelX = x + (pixelLength - moduleLabel.DesiredSize.Width) / 2;
            double labelY = y + 8;
            
            Canvas.SetLeft(moduleLabel, labelX);
            Canvas.SetTop(moduleLabel, labelY);
            CanvasModules.Children.Add(moduleLabel);
            
            // Dimensions (au centre du module)
            string dimText = $"{realLength:0}\"";
            var dimLabel = new TextBlock
            {
                Text = dimText,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = _dimColor
            };
            
            dimLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double dimX = x + (pixelLength - dimLabel.DesiredSize.Width) / 2;
            double dimY = y + (pixelWidth / 2) - 5;
            
            Canvas.SetLeft(dimLabel, dimX);
            Canvas.SetTop(dimLabel, dimY);
            CanvasModules.Children.Add(dimLabel);
            
            // Largeur sur le cote (vertical)
            var widthLabel = new TextBlock
            {
                Text = $"{realWidth:0}\"",
                FontSize = 8,
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                RenderTransform = new RotateTransform(-90)
            };
            
            widthLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(widthLabel, x + 3);
            Canvas.SetTop(widthLabel, y + pixelWidth / 2 + widthLabel.DesiredSize.Width / 2);
            CanvasModules.Children.Add(widthLabel);
        }
        
        /// <summary>
        /// Dessine les murs interieurs d'un module (Left, Right, Front/Back)
        /// Utilise l'echelle reelle: distance et epaisseur en pouces
        /// </summary>
        private void DrawModuleInteriorWalls(ModuleDimension module, double x, double y, 
                                              double pixelLength, double pixelWidth, double realWidth)
        {
            // Mur interieur Left (parallele a Right/Left, positionne du cote Top/Left)
            if (module.HasInteriorWallLeft && module.InteriorWallLeftDistance > 0)
            {
                double thickness = ParseWallThickness(module.InteriorWallLeftThickness);
                double distance = module.InteriorWallLeftDistance;
                
                // Position en pixels depuis le haut (Left side = Top du dessin)
                double wallY = y + (distance * SCALE_FACTOR);
                double wallThicknessPixels = thickness * SCALE_FACTOR;
                
                var wallRect = new Rectangle
                {
                    Width = pixelLength,
                    Height = wallThicknessPixels,
                    Fill = _interiorWallFill,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),
                    StrokeThickness = 0.5,
                    Opacity = 0.85
                };
                Canvas.SetLeft(wallRect, x);
                Canvas.SetTop(wallRect, wallY);
                CanvasModules.Children.Add(wallRect);
                
                // Label dimension
                var distLabel = new TextBlock
                {
                    Text = $"{distance:0}\"",
                    FontSize = 7,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0xCC))
                };
                distLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(distLabel, x + pixelLength - distLabel.DesiredSize.Width - 2);
                Canvas.SetTop(distLabel, wallY + 1);
                CanvasModules.Children.Add(distLabel);
            }
            
            // Mur interieur Right (parallele a Right/Left, positionne du cote Bottom/Right)
            if (module.HasInteriorWallRight && module.InteriorWallRightDistance > 0)
            {
                double thickness = ParseWallThickness(module.InteriorWallRightThickness);
                double distance = module.InteriorWallRightDistance;
                
                // Position en pixels depuis le bas (Right side = Bottom du dessin)
                double wallY = y + pixelWidth - (distance * SCALE_FACTOR) - (thickness * SCALE_FACTOR);
                double wallThicknessPixels = thickness * SCALE_FACTOR;
                
                var wallRect = new Rectangle
                {
                    Width = pixelLength,
                    Height = wallThicknessPixels,
                    Fill = _interiorWallFill,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),
                    StrokeThickness = 0.5,
                    Opacity = 0.85
                };
                Canvas.SetLeft(wallRect, x);
                Canvas.SetTop(wallRect, wallY);
                CanvasModules.Children.Add(wallRect);
                
                // Label dimension
                var distLabel = new TextBlock
                {
                    Text = $"{distance:0}\"",
                    FontSize = 7,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0xCC))
                };
                distLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(distLabel, x + pixelLength - distLabel.DesiredSize.Width - 2);
                Canvas.SetTop(distLabel, wallY + 1);
                CanvasModules.Children.Add(distLabel);
            }
            
            // Mur interieur Front (parallele a Front, vertical - cote droit du module)
            if (module.HasInteriorWallFront && module.InteriorWallFrontDistance > 0)
            {
                double thickness = ParseWallThickness(module.InteriorWallFrontThickness);
                double distance = module.InteriorWallFrontDistance;
                
                // Position en pixels depuis le Front (droite du dessin = fin du module)
                double wallX = x + pixelLength - (distance * SCALE_FACTOR) - (thickness * SCALE_FACTOR);
                double wallThicknessPixels = thickness * SCALE_FACTOR;
                
                var wallRect = new Rectangle
                {
                    Width = wallThicknessPixels,
                    Height = pixelWidth,
                    Fill = _interiorWallFill,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),
                    StrokeThickness = 0.5,
                    Opacity = 0.85
                };
                Canvas.SetLeft(wallRect, wallX);
                Canvas.SetTop(wallRect, y);
                CanvasModules.Children.Add(wallRect);
                
                // Label dimension (vertical)
                var distLabel = new TextBlock
                {
                    Text = $"{distance:0}\"",
                    FontSize = 7,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0xCC)),
                    RenderTransform = new RotateTransform(-90)
                };
                distLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(distLabel, wallX + 1);
                Canvas.SetTop(distLabel, y + pixelWidth - 2);
                CanvasModules.Children.Add(distLabel);
            }
            
            // Mur interieur Back (parallele a Back, vertical - cote gauche du module)
            if (module.HasInteriorWallBack && module.InteriorWallBackDistance > 0)
            {
                double thickness = ParseWallThickness(module.InteriorWallBackThickness);
                double distance = module.InteriorWallBackDistance;
                
                // Position en pixels depuis le Back (gauche du dessin = debut du module)
                double wallX = x + (distance * SCALE_FACTOR);
                double wallThicknessPixels = thickness * SCALE_FACTOR;
                
                var wallRect = new Rectangle
                {
                    Width = wallThicknessPixels,
                    Height = pixelWidth,
                    Fill = _interiorWallFill,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),
                    StrokeThickness = 0.5,
                    Opacity = 0.85
                };
                Canvas.SetLeft(wallRect, wallX);
                Canvas.SetTop(wallRect, y);
                CanvasModules.Children.Add(wallRect);
                
                // Label dimension (vertical)
                var distLabel = new TextBlock
                {
                    Text = $"{distance:0}\"",
                    FontSize = 7,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0xCC)),
                    RenderTransform = new RotateTransform(-90)
                };
                distLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(distLabel, wallX + 1);
                Canvas.SetTop(distLabel, y + pixelWidth - 2);
                CanvasModules.Children.Add(distLabel);
            }
        }
        
        /// <summary>
        /// Parse l'epaisseur du mur depuis la chaine (2, 3 ou 4 pouces)
        /// </summary>
        private double ParseWallThickness(string thickness)
        {
            if (double.TryParse(thickness, out double result))
                return result;
            return 4.0; // Valeur par defaut: 4 pouces
        }

        /// <summary>
        /// Dessine les tunnels (Top et/ou Bottom)
        /// </summary>
        private void DrawTunnels(ModuleDimension module, double x, double y, double pixelLength, double pixelWidth)
        {
            bool isVestibule = module.TunnelType == "Vestibule";
            SolidColorBrush fill = isVestibule ? _vestibuleFill : _tunnelFill;
            
            // Tunnel Top
            if (module.TunnelPosition == "Top" || module.TunnelPosition == "Both")
            {
                double tunnelHeight = ParseDimension(module.TunnelTopHeight, 30) * SCALE_FACTOR * 0.3;
                tunnelHeight = Math.Max(tunnelHeight, 20);
                
                var tunnelRect = new Rectangle
                {
                    Width = pixelLength,
                    Height = tunnelHeight,
                    Fill = fill,
                    Stroke = fill,
                    StrokeThickness = 1,
                    Opacity = 0.7
                };
                
                Canvas.SetLeft(tunnelRect, x);
                Canvas.SetTop(tunnelRect, y - tunnelHeight - 2);
                CanvasModules.Children.Add(tunnelRect);
                
                // Label "T" ou "V"
                var label = new TextBlock
                {
                    Text = isVestibule ? "V" : "T",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = _textColor
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(label, x + (pixelLength - label.DesiredSize.Width) / 2);
                Canvas.SetTop(label, y - tunnelHeight - 2 + (tunnelHeight - label.DesiredSize.Height) / 2);
                CanvasModules.Children.Add(label);
            }
            
            // Tunnel Bottom
            if (module.TunnelPosition == "Bottom" || module.TunnelPosition == "Both")
            {
                double tunnelHeight = ParseDimension(module.TunnelBottomHeight, 30) * SCALE_FACTOR * 0.3;
                tunnelHeight = Math.Max(tunnelHeight, 20);
                
                var tunnelRect = new Rectangle
                {
                    Width = pixelLength,
                    Height = tunnelHeight,
                    Fill = fill,
                    Stroke = fill,
                    StrokeThickness = 1,
                    Opacity = 0.7
                };
                
                Canvas.SetLeft(tunnelRect, x);
                Canvas.SetTop(tunnelRect, y + pixelWidth + 2);
                CanvasModules.Children.Add(tunnelRect);
                
                // Label "T" ou "V"
                var label = new TextBlock
                {
                    Text = isVestibule ? "V" : "T",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = _textColor
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(label, x + (pixelLength - label.DesiredSize.Width) / 2);
                Canvas.SetTop(label, y + pixelWidth + 2 + (tunnelHeight - label.DesiredSize.Height) / 2);
                CanvasModules.Children.Add(label);
            }
        }

        /// <summary>
        /// Dessine un mur de separation entre modules
        /// </summary>
        private void DrawWall(double x, double y, double thickness, double height)
        {
            var wall = new Rectangle
            {
                Width = thickness,
                Height = height,
                Fill = _wallFill,
                Stroke = _wallFill,
                StrokeThickness = 0
            };
            
            Canvas.SetLeft(wall, x);
            Canvas.SetTop(wall, y);
            CanvasModules.Children.Add(wall);
        }

        /// <summary>
        /// Dessine le cadre exterieur de l'unite
        /// </summary>
        private void DrawUnitFrame(double x, double y, double width, double height)
        {
            var frame = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x77)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 5, 3 }
            };
            
            Canvas.SetLeft(frame, x - 5);
            Canvas.SetTop(frame, y - 5);
            CanvasModules.Children.Add(frame);
        }

        /// <summary>
        /// Determine le type d'unite (Bottom/Top/Standard) en fonction des tunnels configures
        /// </summary>
        private string DetermineUnitType()
        {
            // Verifier si des modules ont des positions de tunnel
            bool hasTopTunnel = _modules.Any(m => m.TunnelPosition == "Top");
            bool hasBottomTunnel = _modules.Any(m => m.TunnelPosition == "Bottom");
            bool hasStandard = _modules.All(m => m.TunnelPosition == "Standard" || string.IsNullOrEmpty(m.TunnelPosition) || m.TunnelPosition == "None");
            
            if (hasTopTunnel && hasBottomTunnel)
                return "STACKED";
            else if (hasTopTunnel)
                return "TOP UNIT";
            else if (hasBottomTunnel)
                return "BOTTOM UNIT";
            else if (hasStandard && !_hasTunnelRight && !_hasTunnelLeft && !_hasTunnelMiddle)
                return "STANDARD";
            else if (_hasTunnelRight || _hasTunnelLeft || _hasTunnelMiddle)
                return "WITH TUNNELS";
            
            return "";
        }

        /// <summary>
        /// Dessine les tunnels globaux (Right/Left/Middle) sur le cote de l'unite
        /// Right Tunnel = Cote droit (apres le dernier module)
        /// Left Tunnel = Cote gauche (avant le premier module)
        /// Middle Tunnel = Au centre de l'unite
        /// Chaque tunnel a son propre AirFlow (fleche ou Vestibule)
        /// </summary>
        private void DrawGlobalTunnels(double startX, double startY, double endX, double unitHeight)
        {
            double tunnelOffset = 10;
            double tunnelWidth = GLOBAL_TUNNEL_WIDTH;
            
            // Tunnel Right (a droite de l'unite)
            if (_hasTunnelRight)
            {
                double tunnelX = endX + tunnelOffset;
                var tunnelRect = new Rectangle
                {
                    Width = tunnelWidth,
                    Height = unitHeight,
                    Fill = _tunnelRightFill,
                    Stroke = _tunnelRightFill,
                    StrokeThickness = 1,
                    Opacity = 0.75
                };
                Canvas.SetLeft(tunnelRect, tunnelX);
                Canvas.SetTop(tunnelRect, startY);
                CanvasModules.Children.Add(tunnelRect);
                
                // Label "RIGHT TUNNEL"
                var label = new TextBlock
                {
                    Text = "RIGHT",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = _textColor
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(label, tunnelX + (tunnelWidth - label.DesiredSize.Width) / 2);
                Canvas.SetTop(label, startY + 5);
                CanvasModules.Children.Add(label);
                
                // Indicateur T1 + AirFlow
                DrawTunnelAirFlowIndicator(tunnelX, startY, tunnelWidth, unitHeight, _airFlowRight, "T1");
            }
            
            // Tunnel Left (a gauche de l'unite)
            if (_hasTunnelLeft)
            {
                double tunnelX = startX - tunnelWidth - tunnelOffset;
                var tunnelRect = new Rectangle
                {
                    Width = tunnelWidth,
                    Height = unitHeight,
                    Fill = _tunnelLeftFill,
                    Stroke = _tunnelLeftFill,
                    StrokeThickness = 1,
                    Opacity = 0.75
                };
                Canvas.SetLeft(tunnelRect, tunnelX);
                Canvas.SetTop(tunnelRect, startY);
                CanvasModules.Children.Add(tunnelRect);
                
                // Label "LEFT TUNNEL"
                var label = new TextBlock
                {
                    Text = "LEFT",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = _textColor
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(label, tunnelX + (tunnelWidth - label.DesiredSize.Width) / 2);
                Canvas.SetTop(label, startY + 5);
                CanvasModules.Children.Add(label);
                
                // Indicateur T2 + AirFlow
                DrawTunnelAirFlowIndicator(tunnelX, startY, tunnelWidth, unitHeight, _airFlowLeft, "T2");
            }
            
            // Tunnel Middle (au centre de l'unite, horizontalement)
            if (_hasTunnelMiddle)
            {
                double totalWidth = endX - startX;
                double tunnelX = startX + (totalWidth - tunnelWidth) / 2;
                double tunnelHeight = 30;
                
                // Tunnel en haut de l'unite (indicateur)
                var tunnelRect = new Rectangle
                {
                    Width = tunnelWidth,
                    Height = tunnelHeight,
                    Fill = _tunnelMiddleFill,
                    Stroke = _tunnelMiddleFill,
                    StrokeThickness = 1,
                    Opacity = 0.75
                };
                Canvas.SetLeft(tunnelRect, tunnelX);
                Canvas.SetTop(tunnelRect, startY - tunnelHeight - 5);
                CanvasModules.Children.Add(tunnelRect);
                
                // Label + AirFlow indicator pour Middle Tunnel
                string middleText = "T3";
                if (_airFlowMiddle == "Vestibule")
                    middleText = "T3-V";
                else if (_airFlowMiddle.Contains("Back"))
                    middleText = "T3 →";
                else if (_airFlowMiddle.Contains("Front"))
                    middleText = "T3 ←";
                    
                var label = new TextBlock
                {
                    Text = middleText,
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = _textColor
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(label, tunnelX + (tunnelWidth - label.DesiredSize.Width) / 2);
                Canvas.SetTop(label, startY - tunnelHeight - 5 + (tunnelHeight - label.DesiredSize.Height) / 2);
                CanvasModules.Children.Add(label);
            }
        }
        
        /// <summary>
        /// Dessine l'indicateur AirFlow pour un tunnel (fleche ou V pour Vestibule)
        /// </summary>
        private void DrawTunnelAirFlowIndicator(double tunnelX, double startY, double tunnelWidth, double unitHeight, string airFlow, string tunnelLabel)
        {
            // Couleur selon le type
            SolidColorBrush labelBrush = new SolidColorBrush(Colors.White);
            string displayText = tunnelLabel;
            
            if (airFlow == "Vestibule")
            {
                displayText = $"{tunnelLabel}-V";
                labelBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xA7, 0x26)); // Orange pour vestibule
            }
            else if (airFlow.Contains("Back-To-Front") || airFlow.Contains("Back to Front"))
            {
                // Fleche vers la droite (vers FRONT)
                displayText = $"{tunnelLabel} →";
            }
            else if (airFlow.Contains("Front-To-Back") || airFlow.Contains("Front to Back"))
            {
                // Fleche vers la gauche (vers BACK)
                displayText = $"{tunnelLabel} ←";
            }
            
            var label = new TextBlock
            {
                Text = displayText,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = labelBrush,
                Opacity = 0.95
            };
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(label, tunnelX + (tunnelWidth - label.DesiredSize.Width) / 2);
            Canvas.SetTop(label, startY + unitHeight / 2 - label.DesiredSize.Height / 2);
            CanvasModules.Children.Add(label);
        }

        /// <summary>
        /// Dessine les Interior Walls paralleles a Right/Left
        /// Ces murs sont des lignes verticales traversant l'unite
        /// </summary>
        private void DrawInteriorWalls(double startX, double startY, double endX, double unitHeight)
        {
            double wallThickness = 4;
            
            // Interior Wall 01
            if (_hasInteriorWall01 && _interiorWall01Position > 0)
            {
                double wallX = startX + (_interiorWall01Position * SCALE_FACTOR);
                
                // S'assurer que le mur est dans les limites de l'unite
                if (wallX > startX && wallX < endX)
                {
                    var wall = new Rectangle
                    {
                        Width = wallThickness,
                        Height = unitHeight,
                        Fill = _interiorWallFill,
                        Stroke = _interiorWallFill,
                        StrokeThickness = 0,
                        Opacity = 0.85
                    };
                    Canvas.SetLeft(wall, wallX);
                    Canvas.SetTop(wall, startY);
                    CanvasModules.Children.Add(wall);
                    
                    // Label position
                    var label = new TextBlock
                    {
                        Text = $"IW1 @{_interiorWall01Position:0}\"",
                        FontSize = 8,
                        Foreground = _interiorWallFill,
                        FontWeight = FontWeights.Bold
                    };
                    label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    Canvas.SetLeft(label, wallX - label.DesiredSize.Width / 2);
                    Canvas.SetTop(label, startY + unitHeight + 5);
                    CanvasModules.Children.Add(label);
                }
            }
            
            // Interior Wall 02
            if (_hasInteriorWall02 && _interiorWall02Position > 0)
            {
                double wallX = startX + (_interiorWall02Position * SCALE_FACTOR);
                
                // S'assurer que le mur est dans les limites de l'unite
                if (wallX > startX && wallX < endX)
                {
                    var wall = new Rectangle
                    {
                        Width = wallThickness,
                        Height = unitHeight,
                        Fill = _interiorWallFill,
                        Stroke = _interiorWallFill,
                        StrokeThickness = 0,
                        Opacity = 0.85
                    };
                    Canvas.SetLeft(wall, wallX);
                    Canvas.SetTop(wall, startY);
                    CanvasModules.Children.Add(wall);
                    
                    // Label position
                    var label = new TextBlock
                    {
                        Text = $"IW2 @{_interiorWall02Position:0}\"",
                        FontSize = 8,
                        Foreground = _interiorWallFill,
                        FontWeight = FontWeights.Bold
                    };
                    label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    Canvas.SetLeft(label, wallX - label.DesiredSize.Width / 2);
                    Canvas.SetTop(label, startY + unitHeight + 5);
                    CanvasModules.Children.Add(label);
                }
            }
        }

        /// <summary>
        /// Affiche un message quand il n'y a pas de modules
        /// </summary>
        private void DrawEmptyMessage()
        {
            var message = new TextBlock
            {
                Text = "Aucun module defini\n\nCliquez '+ Ajouter Module' puis '🔄 Appliquer'",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x88)),
                TextAlignment = TextAlignment.Center
            };
            
            message.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            
            double x = (CanvasModules.Width - message.DesiredSize.Width) / 2;
            double y = (CanvasModules.Height - message.DesiredSize.Height) / 2;
            
            Canvas.SetLeft(message, Math.Max(x, 50));
            Canvas.SetTop(message, Math.Max(y, 50));
            CanvasModules.Children.Add(message);
        }

        /// <summary>
        /// Parse une dimension string en double
        /// </summary>
        private double ParseDimension(string value, double defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;
            
            // Retirer les suffixes communs
            string cleaned = value.Replace("\"", "").Replace("in", "").Replace("IN", "").Replace(" ", "").Trim();
            
            if (double.TryParse(cleaned, out double result) && result > 0)
                return result;
            
            return defaultValue;
        }

        /// <summary>
        /// Force un rafraichissement du dessin
        /// </summary>
        public void Refresh()
        {
            DrawModules();
        }
    }
}

