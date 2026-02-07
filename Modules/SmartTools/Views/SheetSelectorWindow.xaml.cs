using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using XnrgyEngineeringAutomationTools.Modules.SmartTools.Services;
using XnrgyEngineeringAutomationTools.Shared.Views;

namespace XnrgyEngineeringAutomationTools.Modules.SmartTools.Views
{
    /// <summary>
    /// Fenetre de selection des feuilles pour l'export PDF Shop Drawing
    /// Migre depuis SmartToolsAmineAddin avec style XNRGY
    /// By Mohammed Amine Elgalai - XNRGY Climate Systems ULC - 2025
    /// </summary>
    public partial class SheetSelectorWindow : Window
    {
        #region Properties

        /// <summary>
        /// Liste des feuilles affichees dans l'interface
        /// </summary>
        public ObservableCollection<SheetViewModel> Sheets { get; } = new ObservableCollection<SheetViewModel>();

        /// <summary>
        /// Resultat de la selection (null si annule)
        /// </summary>
        public ExportSelectionResultModel? Result { get; private set; }

        /// <summary>
        /// Chemin de destination pour l'export
        /// </summary>
        public string DestinationPath { get; set; } = "";

        #endregion

        #region Constructor

        public SheetSelectorWindow()
        {
            InitializeComponent();
            SheetList.ItemsSource = Sheets;
            
            // S'abonner aux changements de theme
            MainWindow.ThemeChanged += OnThemeChanged;
            this.Closed += (s, e) => MainWindow.ThemeChanged -= OnThemeChanged;
            ApplyTheme(MainWindow.CurrentThemeIsDark);
        }

        private void OnThemeChanged(bool isDarkTheme)
        {
            Dispatcher.Invoke(() => ApplyTheme(isDarkTheme));
        }

        private void ApplyTheme(bool isDarkTheme)
        {
            this.Background = new SolidColorBrush(isDarkTheme 
                ? Color.FromRgb(30, 30, 46)    // #1E1E2E
                : Color.FromRgb(245, 247, 250)); // #F5F7FA
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialise la fenetre avec les informations du dessin
        /// </summary>
        /// <param name="fileName">Nom du fichier sans extension</param>
        /// <param name="filePath">Chemin complet du fichier</param>
        /// <param name="destinationPath">Chemin du dossier de destination</param>
        /// <param name="sheets">Liste des feuilles du dessin</param>
        public void Initialize(string fileName, string filePath, string destinationPath, List<SheetInfoModel> sheets)
        {
            TxtFileName.Text = fileName;
            TxtFilePath.Text = filePath;
            DestinationPath = destinationPath;
            TxtDestination.Text = System.IO.Path.GetFileName(destinationPath);
            TxtDestination.ToolTip = destinationPath;

            Sheets.Clear();
            
            bool foundNP = false;
            foreach (var sheet in sheets)
            {
                // Marquer que nous avons trouve une feuille NP_
                if (sheet.Name.StartsWith("NP_", StringComparison.OrdinalIgnoreCase))
                    foundNP = true;

                // Auto-selection: selectionner jusqu'a la premiere feuille NP_
                bool autoSelect = !foundNP && sheet.IsAvailable;

                var vm = new SheetViewModel
                {
                    Name = sheet.Name,
                    IsAvailable = sheet.IsAvailable,
                    IsActive = sheet.IsActive,
                    HasDoNotPrint = sheet.HasDoNotPrint,
                    StartsWithNP = sheet.Name.StartsWith("NP_", StringComparison.OrdinalIgnoreCase),
                    IsSelected = autoSelect || sheet.IsActive, // Toujours selectionner la feuille active
                    SheetReference = sheet.SheetReference
                };

                // Definir le background en fonction de l'etat
                if (!sheet.IsAvailable)
                    vm.Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2A)); // Disabled
                else if (sheet.IsActive)
                    vm.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x3A, 0x5F)); // Active
                else if (sheet.Name.StartsWith("NP_", StringComparison.OrdinalIgnoreCase))
                    vm.Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x3D)); // NP
                else
                    vm.Background = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x36)); // Normal

                Sheets.Add(vm);
            }

            UpdateSheetCount();
        }

        #endregion

        #region Event Handlers

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var sheet in Sheets.Where(s => s.IsAvailable))
            {
                sheet.IsSelected = true;
            }
            UpdateSheetCount();
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var sheet in Sheets.Where(s => s.IsAvailable))
            {
                sheet.IsSelected = false;
            }
            UpdateSheetCount();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = null;
            DialogResult = false;
            Close();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var selectedSheets = Sheets.Where(s => s.IsSelected && s.IsAvailable).ToList();

            if (selectedSheets.Count == 0)
            {
                Shared.Views.XnrgyMessageBox.ShowWarning(
                    "Veuillez selectionner au moins une feuille a exporter.",
                    "Aucune selection",
                    this);
                return;
            }

            Result = new ExportSelectionResultModel
            {
                SelectedSheets = selectedSheets.Select(s => s.Name).ToList(),
                ExportMode = RbSingleFile.IsChecked == true ? "single" : "multiple",
                DestinationPath = DestinationPath
            };

            DialogResult = true;
            Close();
        }

        #endregion

        #region Private Methods

        private void UpdateSheetCount()
        {
            int total = Sheets.Count;
            int available = Sheets.Count(s => s.IsAvailable);
            int selected = Sheets.Count(s => s.IsSelected && s.IsAvailable);
            TxtSheetCount.Text = $"{selected}/{available} selectionnee(s) sur {total} feuille(s)";
        }

        #endregion
    }

    #region ViewModel

    /// <summary>
    /// ViewModel pour l'affichage d'une feuille dans la liste
    /// </summary>
    public class SheetViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Name { get; set; } = "";
        public bool IsAvailable { get; set; } = true;
        public bool IsActive { get; set; } = false;
        public bool HasDoNotPrint { get; set; } = false;
        public bool StartsWithNP { get; set; } = false;
        public dynamic? SheetReference { get; set; }
        public Brush Background { get; set; } = Brushes.Transparent;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        // Visibilite des tags
        public Visibility ActiveTagVisibility => IsActive ? Visibility.Visible : Visibility.Collapsed;
        public Visibility DoNotPrintTagVisibility => HasDoNotPrint ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NpTagVisibility => StartsWithNP && !HasDoNotPrint ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #endregion
}
