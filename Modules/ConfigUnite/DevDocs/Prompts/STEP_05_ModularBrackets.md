# 🔧 STEP 5: Implémenter Tab "Modular Brackets"

**Agent**: Cursor / Zed / Autre  
**Difficulté**: ⭐⭐ (Moyen)  
**Temps estimé**: 15-20 minutes  
**Validation par**: GitHub Copilot

---

## 🎯 OBJECTIF

Créer le tab "Modular Brackets" pour configurer les supports modulaires du module HVAC.

---

## 📁 FICHIERS À MODIFIER

| Fichier | Action | Description |
|---------|--------|-------------|
| `Modules/ConfigUnite/Views/ConfigUniteWindow.xaml` | MODIFIER | Ajouter le contenu du tab Modular Brackets |

---

## 📋 STRUCTURE CIBLE

```
Tab "Modular Brackets"
├── GroupBox "Bracket Configuration"
│   ├── ComboBox: Bracket Type
│   ├── TextBox: Bracket Quantity
│   └── ComboBox: Bracket Material
│
├── GroupBox "Positioning"
│   ├── CheckBox: Auto-position
│   ├── TextBox: Spacing (mm)
│   └── ComboBox: Alignment
│
└── GroupBox "Additional Options"
    ├── CheckBox: Include Corner Brackets
    ├── CheckBox: Reinforced Brackets
    └── CheckBox: Seismic Rated
```

---

## 🔨 INSTRUCTIONS DÉTAILLÉES

### ÉTAPE 1: Localiser le Tab Modular Brackets

Dans `ConfigUniteWindow.xaml`, trouver:

```xml
<!-- Tab: Modular Brackets -->
<TabItem Header="Modular Brackets" Style="{StaticResource ModernTabItem}">
    <!-- Contenu à ajouter ici -->
</TabItem>
```

### ÉTAPE 2: Ajouter le contenu

```xml
<TabItem Header="Modular Brackets" Style="{StaticResource ModernTabItem}">
    <ScrollViewer VerticalScrollBarVisibility="Auto" Padding="10">
        <StackPanel>
            
            <!-- GroupBox: Bracket Configuration -->
            <GroupBox Style="{StaticResource ModernGroupBox}" Margin="0,0,0,15">
                <GroupBox.Header>
                    <TextBlock Text="Bracket Configuration" FontWeight="Bold" Foreground="White"/>
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
                    
                    <TextBlock Grid.Row="0" Grid.Column="0" Text="Bracket Type:" Style="{StaticResource ModernLabel}"/>
                    <ComboBox Grid.Row="0" Grid.Column="1" x:Name="CmbBracketType" Style="{StaticResource ConfigComboBox}" Width="200" HorizontalAlignment="Left"/>
                    
                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Quantity:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="1" Grid.Column="1" x:Name="TxtBracketQuantity" Style="{StaticResource ModernTextBox}" Width="100" HorizontalAlignment="Left" Margin="0,10,0,0"/>
                    
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="Material:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <ComboBox Grid.Row="2" Grid.Column="1" x:Name="CmbBracketMaterial" Style="{StaticResource ConfigComboBox}" Width="200" HorizontalAlignment="Left" Margin="0,10,0,0"/>
                </Grid>
            </GroupBox>
            
            <!-- GroupBox: Positioning -->
            <GroupBox Style="{StaticResource ModernGroupBox}" Margin="0,0,0,15">
                <GroupBox.Header>
                    <TextBlock Text="Positioning" FontWeight="Bold" Foreground="White"/>
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
                    
                    <CheckBox Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" x:Name="ChkAutoPosition" Content="Auto-position Brackets" Style="{StaticResource ModernCheckBox}"/>
                    
                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Spacing (mm):" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="1" Grid.Column="1" x:Name="TxtBracketSpacing" Style="{StaticResource ModernTextBox}" Width="100" HorizontalAlignment="Left" Margin="0,10,0,0"
                             IsEnabled="{Binding IsChecked, ElementName=ChkAutoPosition, Converter={StaticResource InverseBoolConverter}}"/>
                    
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="Alignment:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <ComboBox Grid.Row="2" Grid.Column="1" x:Name="CmbBracketAlignment" Style="{StaticResource ConfigComboBox}" Width="200" HorizontalAlignment="Left" Margin="0,10,0,0"/>
                </Grid>
            </GroupBox>
            
            <!-- GroupBox: Additional Options -->
            <GroupBox Style="{StaticResource ModernGroupBox}">
                <GroupBox.Header>
                    <TextBlock Text="Additional Options" FontWeight="Bold" Foreground="White"/>
                </GroupBox.Header>
                <StackPanel>
                    <CheckBox x:Name="ChkCornerBrackets" Content="Include Corner Brackets" Style="{StaticResource ModernCheckBox}" Margin="0,5"/>
                    <CheckBox x:Name="ChkReinforcedBrackets" Content="Reinforced Brackets" Style="{StaticResource ModernCheckBox}" Margin="0,5"/>
                    <CheckBox x:Name="ChkSeismicRated" Content="Seismic Rated" Style="{StaticResource ModernCheckBox}" Margin="0,5"/>
                </StackPanel>
            </GroupBox>
            
        </StackPanel>
    </ScrollViewer>
</TabItem>
```

### ÉTAPE 3: Initialiser les ComboBox

Dans `ConfigUniteWindow.xaml.cs`, ajouter dans `InitializeComboBoxes()`:

```csharp
// Modular Brackets
CmbBracketType.ItemsSource = new List<string> { "Standard", "Heavy Duty", "Light Duty", "Custom" };
CmbBracketMaterial.ItemsSource = new List<string> { "Galvanized Steel", "Stainless Steel 304", "Stainless Steel 316", "Aluminum" };
CmbBracketAlignment.ItemsSource = new List<string> { "Center", "Left", "Right", "Distributed" };
```

### ÉTAPE 4: Ajouter le InverseBoolConverter (si nécessaire)

Si le converter n'existe pas, l'ajouter dans les Resources:

```xml
<Window.Resources>
    <!-- Inverse Boolean Converter -->
    <local:InverseBooleanConverter x:Key="InverseBoolConverter"/>
</Window.Resources>
```

Et créer la classe dans le code-behind ou dans un fichier Converters.cs:

```csharp
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(bool)value;
    }
}
```

---

## ✅ CRITÈRES DE VALIDATION

- [ ] Le tab "Modular Brackets" s'affiche correctement
- [ ] Les 3 GroupBox sont visibles avec leurs contrôles
- [ ] Les ComboBox sont initialisés avec les bonnes valeurs
- [ ] Le spacing se désactive quand Auto-position est coché
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
> "STEP 5 terminé. Tab Modular Brackets implémenté avec 3 GroupBox."

---

*Prompt créé par GitHub Copilot - 2026-01-28*
