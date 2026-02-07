# 🔧 STEP 6: Implémenter Tab "Equipments"

**Agent**: Cursor / Zed / Autre  
**Difficulté**: ⭐⭐⭐ (Moyen-Complexe)  
**Temps estimé**: 25-30 minutes  
**Validation par**: GitHub Copilot

---

## 🎯 OBJECTIF

Créer le tab "Equipments" pour configurer les équipements HVAC du module (fans, coils, filters, etc.).

---

## 📁 FICHIERS À MODIFIER

| Fichier | Action | Description |
|---------|--------|-------------|
| `Modules/ConfigUnite/Views/ConfigUniteWindow.xaml` | MODIFIER | Ajouter le contenu du tab Equipments |

---

## 📋 STRUCTURE CIBLE

```
Tab "Equipments"
├── GroupBox "Supply Fan"
│   ├── CheckBox: Include Supply Fan
│   ├── ComboBox: Fan Type
│   ├── TextBox: CFM
│   └── ComboBox: Motor Type
│
├── GroupBox "Return Fan"
│   ├── CheckBox: Include Return Fan
│   ├── ComboBox: Fan Type
│   ├── TextBox: CFM
│   └── ComboBox: Motor Type
│
├── GroupBox "Heating Coil"
│   ├── CheckBox: Include Heating
│   ├── ComboBox: Coil Type (Hot Water, Electric, Steam)
│   └── TextBox: Capacity (kW/MBH)
│
├── GroupBox "Cooling Coil"
│   ├── CheckBox: Include Cooling
│   ├── ComboBox: Coil Type (Chilled Water, DX)
│   ├── TextBox: Capacity (Tons)
│   └── TextBox: Rows
│
└── GroupBox "Filtration"
    ├── CheckBox: Include Pre-Filter
    ├── CheckBox: Include Final Filter
    ├── ComboBox: Pre-Filter MERV
    └── ComboBox: Final Filter MERV
```

---

## 🔨 INSTRUCTIONS DÉTAILLÉES

### ÉTAPE 1: Localiser le Tab Equipments

Dans `ConfigUniteWindow.xaml`, trouver:

```xml
<!-- Tab: Equipments -->
<TabItem Header="Equipments" Style="{StaticResource ModernTabItem}">
    <!-- Contenu à ajouter ici -->
</TabItem>
```

### ÉTAPE 2: Ajouter le contenu

```xml
<TabItem Header="Equipments" Style="{StaticResource ModernTabItem}">
    <ScrollViewer VerticalScrollBarVisibility="Auto" Padding="10">
        <StackPanel>
            
            <!-- ===== GroupBox: Supply Fan ===== -->
            <GroupBox Style="{StaticResource ModernGroupBox}" Margin="0,0,0,15">
                <GroupBox.Header>
                    <TextBlock Text="Supply Fan" FontWeight="Bold" Foreground="White"/>
                </GroupBox.Header>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="150"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    
                    <CheckBox Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" x:Name="ChkIncludeSupplyFan" Content="Include Supply Fan" Style="{StaticResource ModernCheckBox}"/>
                    
                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Fan Type:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <ComboBox Grid.Row="1" Grid.Column="1" x:Name="CmbSupplyFanType" Style="{StaticResource ConfigComboBox}" Width="200" HorizontalAlignment="Left" Margin="0,10,0,0"
                              IsEnabled="{Binding IsChecked, ElementName=ChkIncludeSupplyFan}"/>
                    
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="CFM:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="2" Grid.Column="1" x:Name="TxtSupplyFanCFM" Style="{StaticResource ModernTextBox}" Width="120" HorizontalAlignment="Left" Margin="0,10,0,0"
                             IsEnabled="{Binding IsChecked, ElementName=ChkIncludeSupplyFan}"/>
                    
                    <TextBlock Grid.Row="3" Grid.Column="0" Text="Motor Type:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <ComboBox Grid.Row="3" Grid.Column="1" x:Name="CmbSupplyMotorType" Style="{StaticResource ConfigComboBox}" Width="200" HorizontalAlignment="Left" Margin="0,10,0,0"
                              IsEnabled="{Binding IsChecked, ElementName=ChkIncludeSupplyFan}"/>
                </Grid>
            </GroupBox>
            
            <!-- ===== GroupBox: Return Fan ===== -->
            <GroupBox Style="{StaticResource ModernGroupBox}" Margin="0,0,0,15">
                <GroupBox.Header>
                    <TextBlock Text="Return Fan" FontWeight="Bold" Foreground="White"/>
                </GroupBox.Header>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="150"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    
                    <CheckBox Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" x:Name="ChkIncludeReturnFan" Content="Include Return Fan" Style="{StaticResource ModernCheckBox}"/>
                    
                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Fan Type:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <ComboBox Grid.Row="1" Grid.Column="1" x:Name="CmbReturnFanType" Style="{StaticResource ConfigComboBox}" Width="200" HorizontalAlignment="Left" Margin="0,10,0,0"
                              IsEnabled="{Binding IsChecked, ElementName=ChkIncludeReturnFan}"/>
                    
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="CFM:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="2" Grid.Column="1" x:Name="TxtReturnFanCFM" Style="{StaticResource ModernTextBox}" Width="120" HorizontalAlignment="Left" Margin="0,10,0,0"
                             IsEnabled="{Binding IsChecked, ElementName=ChkIncludeReturnFan}"/>
                    
                    <TextBlock Grid.Row="3" Grid.Column="0" Text="Motor Type:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <ComboBox Grid.Row="3" Grid.Column="1" x:Name="CmbReturnMotorType" Style="{StaticResource ConfigComboBox}" Width="200" HorizontalAlignment="Left" Margin="0,10,0,0"
                              IsEnabled="{Binding IsChecked, ElementName=ChkIncludeReturnFan}"/>
                </Grid>
            </GroupBox>
            
            <!-- ===== GroupBox: Heating Coil ===== -->
            <GroupBox Style="{StaticResource ModernGroupBox}" Margin="0,0,0,15">
                <GroupBox.Header>
                    <TextBlock Text="Heating Coil" FontWeight="Bold" Foreground="White"/>
                </GroupBox.Header>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="150"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    
                    <CheckBox Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" x:Name="ChkIncludeHeating" Content="Include Heating Coil" Style="{StaticResource ModernCheckBox}"/>
                    
                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Coil Type:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <ComboBox Grid.Row="1" Grid.Column="1" x:Name="CmbHeatingCoilType" Style="{StaticResource ConfigComboBox}" Width="200" HorizontalAlignment="Left" Margin="0,10,0,0"
                              IsEnabled="{Binding IsChecked, ElementName=ChkIncludeHeating}"/>
                    
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="Capacity (kW/MBH):" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="2" Grid.Column="1" x:Name="TxtHeatingCapacity" Style="{StaticResource ModernTextBox}" Width="120" HorizontalAlignment="Left" Margin="0,10,0,0"
                             IsEnabled="{Binding IsChecked, ElementName=ChkIncludeHeating}"/>
                </Grid>
            </GroupBox>
            
            <!-- ===== GroupBox: Cooling Coil ===== -->
            <GroupBox Style="{StaticResource ModernGroupBox}" Margin="0,0,0,15">
                <GroupBox.Header>
                    <TextBlock Text="Cooling Coil" FontWeight="Bold" Foreground="White"/>
                </GroupBox.Header>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="150"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    
                    <CheckBox Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" x:Name="ChkIncludeCooling" Content="Include Cooling Coil" Style="{StaticResource ModernCheckBox}"/>
                    
                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Coil Type:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <ComboBox Grid.Row="1" Grid.Column="1" x:Name="CmbCoolingCoilType" Style="{StaticResource ConfigComboBox}" Width="200" HorizontalAlignment="Left" Margin="0,10,0,0"
                              IsEnabled="{Binding IsChecked, ElementName=ChkIncludeCooling}"/>
                    
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="Capacity (Tons):" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="2" Grid.Column="1" x:Name="TxtCoolingCapacity" Style="{StaticResource ModernTextBox}" Width="120" HorizontalAlignment="Left" Margin="0,10,0,0"
                             IsEnabled="{Binding IsChecked, ElementName=ChkIncludeCooling}"/>
                    
                    <TextBlock Grid.Row="3" Grid.Column="0" Text="Rows:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <ComboBox Grid.Row="3" Grid.Column="1" x:Name="CmbCoolingRows" Style="{StaticResource ConfigComboBox}" Width="100" HorizontalAlignment="Left" Margin="0,10,0,0"
                              IsEnabled="{Binding IsChecked, ElementName=ChkIncludeCooling}"/>
                </Grid>
            </GroupBox>
            
            <!-- ===== GroupBox: Filtration ===== -->
            <GroupBox Style="{StaticResource ModernGroupBox}">
                <GroupBox.Header>
                    <TextBlock Text="Filtration" FontWeight="Bold" Foreground="White"/>
                </GroupBox.Header>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="150"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    
                    <CheckBox Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" x:Name="ChkIncludePreFilter" Content="Include Pre-Filter" Style="{StaticResource ModernCheckBox}"/>
                    
                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Pre-Filter MERV:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <ComboBox Grid.Row="1" Grid.Column="1" x:Name="CmbPreFilterMERV" Style="{StaticResource ConfigComboBox}" Width="100" HorizontalAlignment="Left" Margin="0,10,0,0"
                              IsEnabled="{Binding IsChecked, ElementName=ChkIncludePreFilter}"/>
                    
                    <CheckBox Grid.Row="2" Grid.Column="0" Grid.ColumnSpan="2" x:Name="ChkIncludeFinalFilter" Content="Include Final Filter" Style="{StaticResource ModernCheckBox}" Margin="0,10,0,0"/>
                    
                    <TextBlock Grid.Row="3" Grid.Column="0" Text="Final Filter MERV:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <ComboBox Grid.Row="3" Grid.Column="1" x:Name="CmbFinalFilterMERV" Style="{StaticResource ConfigComboBox}" Width="100" HorizontalAlignment="Left" Margin="0,10,0,0"
                              IsEnabled="{Binding IsChecked, ElementName=ChkIncludeFinalFilter}"/>
                </Grid>
            </GroupBox>
            
        </StackPanel>
    </ScrollViewer>
</TabItem>
```

### ÉTAPE 3: Initialiser les ComboBox

Dans `ConfigUniteWindow.xaml.cs`, ajouter dans `InitializeComboBoxes()`:

```csharp
// Equipments - Fans
var fanTypes = new List<string> { "Plenum", "Plug", "Belt Drive", "Direct Drive", "EC Motor" };
CmbSupplyFanType.ItemsSource = fanTypes;
CmbReturnFanType.ItemsSource = fanTypes;

var motorTypes = new List<string> { "Standard Efficiency", "Premium Efficiency", "EC Motor", "VFD Compatible" };
CmbSupplyMotorType.ItemsSource = motorTypes;
CmbReturnMotorType.ItemsSource = motorTypes;

// Equipments - Coils
CmbHeatingCoilType.ItemsSource = new List<string> { "Hot Water", "Electric", "Steam", "Gas" };
CmbCoolingCoilType.ItemsSource = new List<string> { "Chilled Water", "DX (Direct Expansion)", "Glycol" };
CmbCoolingRows.ItemsSource = new List<string> { "2", "3", "4", "5", "6", "8", "10" };

// Equipments - Filtration
var mervOptions = new List<string> { "MERV 8", "MERV 10", "MERV 11", "MERV 13", "MERV 14", "MERV 15", "HEPA" };
CmbPreFilterMERV.ItemsSource = mervOptions;
CmbFinalFilterMERV.ItemsSource = mervOptions;
```

---

## ✅ CRITÈRES DE VALIDATION

- [ ] Le tab "Equipments" s'affiche correctement
- [ ] Les 5 GroupBox sont visibles (Supply Fan, Return Fan, Heating, Cooling, Filtration)
- [ ] Les CheckBox activent/désactivent les contrôles associés
- [ ] Les ComboBox sont initialisés avec les bonnes valeurs
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
> "STEP 6 terminé. Tab Equipments implémenté avec 5 GroupBox (Fans, Coils, Filtration)."

---

*Prompt créé par GitHub Copilot - 2026-01-28*
