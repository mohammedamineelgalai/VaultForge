# 🔧 STEP 8: Rendre UnitVisualizer Réactif

**Agent**: Cursor / Zed / Autre  
**Difficulté**: ⭐⭐⭐⭐ (Complexe - Logique et Binding)  
**Temps estimé**: 40-50 minutes  
**Validation par**: GitHub Copilot

---

## 🎯 OBJECTIF

Rendre le UnitVisualizer (panneau droit avec la vue 3D schématique) réactif aux changements dans les tabs de configuration.

---

## 📁 FICHIERS À MODIFIER

| Fichier | Action | Description |
|---------|--------|-------------|
| `Modules/ConfigUnite/Views/ConfigUniteWindow.xaml` | MODIFIER | Améliorer le UnitVisualizer |
| `Modules/ConfigUnite/Views/ConfigUniteWindow.xaml.cs` | MODIFIER | Ajouter la logique de mise à jour |
| `Modules/ConfigUnite/ViewModels/UnitVisualizerViewModel.cs` | CRÉER | ViewModel pour le visualizer |

---

## 📋 FONCTIONNALITÉ CIBLE

Le UnitVisualizer doit:
1. Afficher une représentation schématique du module HVAC
2. Se mettre à jour quand les dimensions changent
3. Montrer visuellement les composants sélectionnés (fans, coils, etc.)
4. Afficher un résumé des specs en temps réel

---

## 🔨 INSTRUCTIONS DÉTAILLÉES

### ÉTAPE 1: Créer le ViewModel

Créer le fichier `Modules/ConfigUnite/ViewModels/UnitVisualizerViewModel.cs`:

```csharp
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace XnrgyEngineeringAutomationTools.Modules.ConfigUnite.ViewModels
{
    public class UnitVisualizerViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // === DIMENSIONS ===
        private double _length = 5000;
        public double Length
        {
            get => _length;
            set
            {
                _length = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScaledLength));
                OnPropertyChanged(nameof(DimensionSummary));
            }
        }

        private double _width = 2000;
        public double Width
        {
            get => _width;
            set
            {
                _width = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScaledWidth));
                OnPropertyChanged(nameof(DimensionSummary));
            }
        }

        private double _height = 2500;
        public double Height
        {
            get => _height;
            set
            {
                _height = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScaledHeight));
                OnPropertyChanged(nameof(DimensionSummary));
            }
        }

        // === SCALED VALUES FOR DISPLAY ===
        private const double ScaleFactor = 0.05; // 1/20 scale
        public double ScaledLength => Length * ScaleFactor;
        public double ScaledWidth => Width * ScaleFactor;
        public double ScaledHeight => Height * ScaleFactor;

        // === SUMMARY ===
        public string DimensionSummary => $"{Length} x {Width} x {Height} mm";

        // === COMPONENT VISIBILITY ===
        private bool _hasSupplyFan;
        public bool HasSupplyFan
        {
            get => _hasSupplyFan;
            set { _hasSupplyFan = value; OnPropertyChanged(); }
        }

        private bool _hasReturnFan;
        public bool HasReturnFan
        {
            get => _hasReturnFan;
            set { _hasReturnFan = value; OnPropertyChanged(); }
        }

        private bool _hasHeatingCoil;
        public bool HasHeatingCoil
        {
            get => _hasHeatingCoil;
            set { _hasHeatingCoil = value; OnPropertyChanged(); }
        }

        private bool _hasCoolingCoil;
        public bool HasCoolingCoil
        {
            get => _hasCoolingCoil;
            set { _hasCoolingCoil = value; OnPropertyChanged(); }
        }

        private bool _hasFilter;
        public bool HasFilter
        {
            get => _hasFilter;
            set { _hasFilter = value; OnPropertyChanged(); }
        }

        // === UNIT INFO ===
        private string _unitType = "AHU";
        public string UnitType
        {
            get => _unitType;
            set { _unitType = value; OnPropertyChanged(); }
        }

        private string _unitOption = "Standard";
        public string UnitOption
        {
            get => _unitOption;
            set { _unitOption = value; OnPropertyChanged(); }
        }

        // === COMPONENT COUNT ===
        public int ComponentCount
        {
            get
            {
                int count = 0;
                if (HasSupplyFan) count++;
                if (HasReturnFan) count++;
                if (HasHeatingCoil) count++;
                if (HasCoolingCoil) count++;
                if (HasFilter) count++;
                return count;
            }
        }
    }
}
```

### ÉTAPE 2: Améliorer le UnitVisualizer dans XAML

Localiser le Border du UnitVisualizer (panneau droit) et le remplacer par:

```xml
<!-- Right Panel: Unit Visualizer -->
<Border Grid.Column="2" Background="#1E1E1E" CornerRadius="10" Margin="10">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Header -->
        <Border Grid.Row="0" Background="#2D2D30" CornerRadius="10,10,0,0" Padding="15">
            <StackPanel>
                <TextBlock Text="📐 Unit Visualizer" FontSize="16" FontWeight="Bold" Foreground="White"/>
                <TextBlock x:Name="TxtVisualizerSummary" Text="AHU Module Preview" Foreground="#888" Margin="0,5,0,0"/>
            </StackPanel>
        </Border>
        
        <!-- 3D Schematic View -->
        <Grid Grid.Row="1" Margin="15">
            <Grid.RowDefinitions>
                <RowDefinition Height="*"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            
            <!-- Schematic Box (Isometric-like representation) -->
            <Canvas x:Name="UnitCanvas" Grid.Row="0" Background="#252526" ClipToBounds="True">
                <!-- Main Unit Box - Updated dynamically -->
                <Border x:Name="UnitBox" 
                        Canvas.Left="50" Canvas.Top="50"
                        Width="200" Height="150"
                        Background="#3E3E42" 
                        BorderBrush="#007ACC" BorderThickness="2"
                        CornerRadius="5">
                    <Grid>
                        <!-- Component indicators -->
                        <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" VerticalAlignment="Top" Margin="5">
                            <Border x:Name="IndicatorFan" Width="20" Height="20" CornerRadius="10" Background="#4CAF50" Margin="2" Visibility="Collapsed" ToolTip="Supply Fan"/>
                            <Border x:Name="IndicatorHeating" Width="20" Height="20" CornerRadius="10" Background="#FF5722" Margin="2" Visibility="Collapsed" ToolTip="Heating Coil"/>
                            <Border x:Name="IndicatorCooling" Width="20" Height="20" CornerRadius="10" Background="#2196F3" Margin="2" Visibility="Collapsed" ToolTip="Cooling Coil"/>
                            <Border x:Name="IndicatorFilter" Width="20" Height="20" CornerRadius="10" Background="#9C27B0" Margin="2" Visibility="Collapsed" ToolTip="Filter"/>
                        </StackPanel>
                        
                        <!-- Center text -->
                        <TextBlock x:Name="TxtUnitTypeDisplay" Text="AHU" 
                                   HorizontalAlignment="Center" VerticalAlignment="Center"
                                   FontSize="24" FontWeight="Bold" Foreground="#007ACC"/>
                    </Grid>
                </Border>
                
                <!-- Dimension labels -->
                <TextBlock x:Name="LblLengthDisplay" Canvas.Left="50" Canvas.Top="210" Text="L: 5000mm" Foreground="#4CAF50" FontSize="11"/>
                <TextBlock x:Name="LblWidthDisplay" Canvas.Left="260" Canvas.Top="120" Text="W: 2000mm" Foreground="#4CAF50" FontSize="11"/>
                <TextBlock x:Name="LblHeightDisplay" Canvas.Left="260" Canvas.Top="50" Text="H: 2500mm" Foreground="#4CAF50" FontSize="11"/>
            </Canvas>
            
            <!-- Live Specs Panel -->
            <Border Grid.Row="1" Background="#2D2D30" CornerRadius="5" Padding="10" Margin="0,10,0,0">
                <StackPanel>
                    <TextBlock Text="Live Specifications" FontWeight="Bold" Foreground="#888" Margin="0,0,0,10"/>
                    
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        
                        <TextBlock Grid.Row="0" Grid.Column="0" Text="Type:" Foreground="#888"/>
                        <TextBlock Grid.Row="0" Grid.Column="1" x:Name="LblLiveUnitType" Text="AHU" Foreground="White" FontWeight="Bold"/>
                        
                        <TextBlock Grid.Row="1" Grid.Column="0" Text="Components:" Foreground="#888" Margin="0,5,0,0"/>
                        <TextBlock Grid.Row="1" Grid.Column="1" x:Name="LblLiveComponents" Text="0" Foreground="#4CAF50" FontWeight="Bold" Margin="0,5,0,0"/>
                        
                        <TextBlock Grid.Row="2" Grid.Column="0" Text="Dimensions:" Foreground="#888" Margin="0,5,0,0"/>
                        <TextBlock Grid.Row="2" Grid.Column="1" x:Name="LblLiveDimensions" Text="5000 x 2000 x 2500" Foreground="#2196F3" FontWeight="Bold" Margin="0,5,0,0" FontSize="10"/>
                    </Grid>
                </StackPanel>
            </Border>
        </Grid>
        
        <!-- Footer with Update Button -->
        <Border Grid.Row="2" Background="#2D2D30" CornerRadius="0,0,10,10" Padding="10">
            <Button x:Name="BtnUpdateVisualizer" Content="🔄 Update Preview" 
                    Style="{StaticResource SecondaryButton}"
                    Click="BtnUpdateVisualizer_Click"
                    HorizontalAlignment="Stretch"/>
        </Border>
    </Grid>
</Border>
```

### ÉTAPE 3: Ajouter la logique de mise à jour dans le code-behind

Dans `ConfigUniteWindow.xaml.cs`, ajouter:

```csharp
// Dans la classe, ajouter un champ pour le ViewModel
private UnitVisualizerViewModel _visualizerViewModel;

// Dans le constructeur, après InitializeComponent()
_visualizerViewModel = new UnitVisualizerViewModel();
UpdateVisualizerDisplay();

// Ajouter les méthodes
private void BtnUpdateVisualizer_Click(object sender, RoutedEventArgs e)
{
    UpdateVisualizerFromControls();
    UpdateVisualizerDisplay();
}

private void UpdateVisualizerFromControls()
{
    // Récupérer les valeurs des contrôles
    if (double.TryParse(TxtAhuLength?.Text, out double length))
        _visualizerViewModel.Length = length;
    
    if (double.TryParse(TxtAhuWidth?.Text, out double width))
        _visualizerViewModel.Width = width;
    
    if (double.TryParse(TxtAhuHeight?.Text, out double height))
        _visualizerViewModel.Height = height;
    
    // Unit Type
    _visualizerViewModel.UnitType = CmbUnitType?.SelectedItem?.ToString() ?? "AHU";
    _visualizerViewModel.UnitOption = CmbUnitOption?.SelectedItem?.ToString() ?? "Standard";
    
    // Components (si les contrôles existent)
    _visualizerViewModel.HasSupplyFan = ChkIncludeSupplyFan?.IsChecked ?? false;
    _visualizerViewModel.HasReturnFan = ChkIncludeReturnFan?.IsChecked ?? false;
    _visualizerViewModel.HasHeatingCoil = ChkIncludeHeating?.IsChecked ?? false;
    _visualizerViewModel.HasCoolingCoil = ChkIncludeCooling?.IsChecked ?? false;
    _visualizerViewModel.HasFilter = (ChkIncludePreFilter?.IsChecked ?? false) || (ChkIncludeFinalFilter?.IsChecked ?? false);
}

private void UpdateVisualizerDisplay()
{
    // Update dimension labels
    LblLengthDisplay.Text = $"L: {_visualizerViewModel.Length}mm";
    LblWidthDisplay.Text = $"W: {_visualizerViewModel.Width}mm";
    LblHeightDisplay.Text = $"H: {_visualizerViewModel.Height}mm";
    
    // Update live specs
    LblLiveUnitType.Text = _visualizerViewModel.UnitType;
    LblLiveComponents.Text = _visualizerViewModel.ComponentCount.ToString();
    LblLiveDimensions.Text = _visualizerViewModel.DimensionSummary;
    
    // Update unit type display
    TxtUnitTypeDisplay.Text = _visualizerViewModel.UnitType;
    TxtVisualizerSummary.Text = $"{_visualizerViewModel.UnitType} - {_visualizerViewModel.UnitOption}";
    
    // Update component indicators
    IndicatorFan.Visibility = _visualizerViewModel.HasSupplyFan ? Visibility.Visible : Visibility.Collapsed;
    IndicatorHeating.Visibility = _visualizerViewModel.HasHeatingCoil ? Visibility.Visible : Visibility.Collapsed;
    IndicatorCooling.Visibility = _visualizerViewModel.HasCoolingCoil ? Visibility.Visible : Visibility.Collapsed;
    IndicatorFilter.Visibility = _visualizerViewModel.HasFilter ? Visibility.Visible : Visibility.Collapsed;
    
    // Scale the unit box based on dimensions (simplified)
    double scale = Math.Min(200 / _visualizerViewModel.Length * 1000, 150 / _visualizerViewModel.Height * 1000);
    scale = Math.Max(0.5, Math.Min(2, scale)); // Clamp between 0.5 and 2
    UnitBox.Width = Math.Max(100, Math.Min(250, _visualizerViewModel.ScaledLength));
    UnitBox.Height = Math.Max(80, Math.Min(180, _visualizerViewModel.ScaledHeight));
}
```

### ÉTAPE 4: Ajouter le using

En haut de `ConfigUniteWindow.xaml.cs`:

```csharp
using XnrgyEngineeringAutomationTools.Modules.ConfigUnite.ViewModels;
```

### ÉTAPE 5: Ajouter le fichier au projet

Dans le `.csproj`, s'assurer que le fichier est inclus:

```xml
<Compile Include="Modules\ConfigUnite\ViewModels\UnitVisualizerViewModel.cs" />
```

---

## ✅ CRITÈRES DE VALIDATION

- [ ] Le ViewModel est créé et compile sans erreur
- [ ] Le panneau UnitVisualizer affiche les dimensions
- [ ] Le bouton "Update Preview" fonctionne
- [ ] Les indicateurs de composants s'affichent/cachent
- [ ] Les specs live se mettent à jour
- [ ] Build réussi avec `.\build-and-run.ps1 -BuildOnly`

---

## 🔧 COMMANDE DE BUILD

```powershell
cd "c:\Users\mohammedamine.elgala\source\repos\XnrgyEngineeringAutomationTools"
.\build-and-run.ps1 -BuildOnly
```

---

## 📝 QUAND TERMINÉ

Signaler:
> "STEP 8 terminé. UnitVisualizer réactif avec ViewModel et mise à jour dynamique."

---

*Prompt créé par GitHub Copilot - 2026-01-28*
