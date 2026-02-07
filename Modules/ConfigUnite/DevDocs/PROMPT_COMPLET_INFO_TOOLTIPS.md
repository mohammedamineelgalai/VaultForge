# 🎯 PROMPT COMPLET - Ajouter ℹ️ Info Tooltips ConfigUnite

---

## ⚠️ INSTRUCTIONS DE CONDITIONNEMENT (À LIRE EN PREMIER)

Tu es un agent IA EXÉCUTANT. Tu n'as PAS l'autorité de valider la conformité finale.
Tu travailles en mode **Human-in-the-Loop** : l'humain est ton oracle de validation.

### RÈGLES ABSOLUES
1. **NE JAMAIS** déclarer "terminé" sans validation humaine explicite
2. **NE JAMAIS** utiliser PowerShell pour remplacer du texte (corruption encodage)
3. **NE JAMAIS** abréger les labels (ex: "Panel Insul." → INTERDIT)
4. **NE JAMAIS** toucher les sections déjà validées (Interior Wall 01 et 02)
5. **TOUJOURS** poser la question de validation à la fin de chaque cycle
6. **BUILD SUCCESS** n'est PAS un critère de conformité - c'est juste un prérequis

---

## 📋 DESCRIPTION DU PROBLÈME

**Projet**: XnrgyEngineeringAutomationTools (.NET Framework 4.8 WPF)
**Module**: ConfigUnite - Configuration Master AHU (Air Handling Unit)
**Fichier principal**: `Modules/ConfigUnite/Views/ConfigUniteWindow.xaml`

**Problème**: Les utilisateurs ne savent pas quel paramètre Inventor est associé à chaque champ du formulaire.

**Solution**: Ajouter une icône **ℹ** à côté de chaque label de champ, avec un **tooltip** affichant le nom du paramètre Inventor.

---

## 🎨 EXEMPLE VISUEL

**AVANT:**
```
Panel Insulation:  [ComboBox___]
```

**APRÈS:**
```
Panel Insulation: ℹ  [ComboBox___]
                  ↑
         Au survol: "Inventor: Panel_Insulation_Form"
```

---

## 📐 PATTERN XAML VALIDÉ (OBLIGATOIRE)

### ANCIEN CODE (à remplacer):
```xaml
<TextBlock Grid.Row="2" Grid.Column="0" Text="Panel Insulation:" Style="{StaticResource ModernLabel}" Margin="0,8,0,0"/>
```

### NOUVEAU CODE (pattern validé):
```xaml
<StackPanel Grid.Row="2" Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center" Margin="0,8,0,0">
    <TextBlock Text="Panel Insulation:" Style="{StaticResource ModernLabel}" VerticalAlignment="Center"/>
    <TextBlock Text=" ℹ" FontSize="10" Foreground="#64B5F6" VerticalAlignment="Center" Cursor="Help" ToolTip="Inventor: Panel_Insulation_Form"/>
</StackPanel>
```

### Pour les CheckBox:
```xaml
<!-- Ajouter simplement le ToolTip -->
<CheckBox Content="Include" ToolTip="Inventor: Include_RT_First_Internal_Wall_Form" .../>
```

---

## 📏 RÈGLES DE STYLE (INVIOLABLES)

| Règle | Valeur | Pourquoi |
|-------|--------|----------|
| Colonne labels | `Width="140"` minimum | Évite troncature du texte |
| Labels | **COMPLETS** jamais abrégés | Lisibilité |
| StackPanel | `VerticalAlignment="Center"` | Centrage vertical |
| TextBlock label | `VerticalAlignment="Center"` | Alignement avec icône |
| TextBlock icône | `VerticalAlignment="Center"` | Alignement avec label |
| Margin | `Margin="0,8,0,0"` sur StackPanel UNIQUEMENT | Uniformité espacement |
| Icône ℹ | `FontSize="10"` | Taille correcte |
| Icône ℹ | `Foreground="#64B5F6"` | Bleu clair standard |
| Icône ℹ | `Cursor="Help"` | Indique interactivité |

### Labels - INTERDIT vs CORRECT

| ❌ INTERDIT (abrégé) | ✅ CORRECT (complet) |
|---------------------|---------------------|
| Panel Insul. | Panel Insulation: |
| Panel Constr. | Panel Construction: |
| Static Press. | Static Pressure: |
| Panel Mat. | Panel Material: |
| Liner Mat. | Liner Material: |
| Design Press. | Design Pressure: |
| Customize Constr. | Customize Construction: |
| Customize Mat. | Customize Material: |

---

## 📂 FICHIERS DE RÉFÉRENCE

### Fichier à modifier:
```
C:\Users\mohammedamine.elgala\source\repos\XnrgyEngineeringAutomationTools\Modules\ConfigUnite\Views\ConfigUniteWindow.xaml
```

### Pattern validé à copier (exemple réel):
Lignes **~1177-1270** (Interior Wall 01) et **~1275-1375** (Interior Wall 02) contiennent le pattern correct déjà implémenté.

### Référence noms paramètres Inventor:
```
C:\Users\mohammedamine.elgala\source\repos\XnrgyEngineeringAutomationTools\Modules\ConfigUnite\Models\ConfigUniteDataModel.cs
```

---

## 🔨 COMMANDE BUILD

```powershell
cd "C:\Users\mohammedamine.elgala\source\repos\XnrgyEngineeringAutomationTools"
.\build-and-run.ps1 -BuildOnly
```

**Résultat attendu**: `Compilation reussie` (les warnings sont OK, seules les erreurs comptent)

---

## ✅ SECTIONS DÉJÀ FAITES (NE PAS TOUCHER)

- **Interior Wall 01** (Parallel To Right/Left) - lignes ~1177-1270 ✅
- **Interior Wall 02** (Parallel To Right/Left) - lignes ~1275-1375 ✅

---

## 📋 SECTIONS À FAIRE

### PRIORITÉ 1: Interior Walls (Parallel To Front/Back) - 6 Tunnel Walls

| GroupBox dans XAML | Préfixe pour les Tooltips |
|--------------------|---------------------------|
| Right Tunnel - Wall 01 | `RT_First_Internal_Wall` |
| Right Tunnel - Wall 02 | `RT_Second_Internal_Wall` |
| Left Tunnel - Wall 01 | `LT_First_Internal_Wall` |
| Left Tunnel - Wall 02 | `LT_Second_Internal_Wall` |
| Middle Tunnel - Wall 01 | `MT_First_Internal_Wall` |
| Middle Tunnel - Wall 02 | `MT_Second_Internal_Wall` |

**10 champs par wall avec leurs tooltips:**

| Label UI (complet!) | Tooltip Inventor |
|---------------------|------------------|
| Include (checkbox) | `Inventor: Include_{PREFIX}_Form` |
| Position: | `Inventor: {PREFIX}_Position_Form` |
| Customize Construction (checkbox) | `Inventor: Customize_{PREFIX}_WallPanelConstruction_Form` |
| Panel Insulation: | `Inventor: {PREFIX}_WallPanel_Insulation_Form` |
| Panel Construction: | `Inventor: {PREFIX}_WallPanel_Construction_Form` |
| Panel Width: | `Inventor: {PREFIX}_WallPanel_Panel_Width_Form` |
| Static Pressure: | `Inventor: {PREFIX}_StaticPressure_Form` |
| Customize Material (checkbox) | `Inventor: Customize_{PREFIX}_WallPanelMaterial_Form` |
| Panel Material: | `Inventor: {PREFIX}_WallPanelSMStyleName_Form` |
| Liner Material: | `Inventor: {PREFIX}_WallLinerSMStyleName_Form` |

**Exemple concret pour Right Tunnel - Wall 01:**
- `Include` → `Inventor: Include_RT_First_Internal_Wall_Form`
- `Position:` → `Inventor: RT_First_Internal_Wall_Position_Form`
- `Panel Insulation:` → `Inventor: RT_First_Internal_Wall_WallPanel_Insulation_Form`

### PRIORITÉ 2: Unit Specification (lignes ~370-764)

| Section | Champ | Tooltip |
|---------|-------|---------|
| Revision | Revision Number: | `Inventor: RevisionNumber_Form` |
| Revision | Checked Date: | `Inventor: CheckedDate_Form` |
| Revision | Checked By: | `Inventor: CheckedBy_Form` |
| Design Info | Project Number: | `Inventor: ProjectNumber_Form` |
| Design Info | Drafter Name: | `Inventor: DrafterName_Form` |
| Design Info | Co-Drafter Name: | `Inventor: CoDrafterName_Form` |
| Design Info | Creation Date: | `Inventor: CreationDate_Form` |
| Unit Info | CRAH Unit (checkbox) | `Inventor: IsCRAHUnit_Form` |
| Unit Info | Unit Type: | `Inventor: UnitType_Form` |
| Airflow | 1 Tunnel (Right) (checkbox) | `Inventor: Tunnel1Right_Form` |
| Airflow | Air Flow (Right): | `Inventor: AirFlowRight_Form` |
| Airflow | 2 Tunnels (Left) (checkbox) | `Inventor: Tunnel2Left_Form` |
| Airflow | Air Flow (Left): | `Inventor: AirFlowLeft_Form` |
| Airflow | 3 Tunnels (Middle) (checkbox) | `Inventor: Tunnel3Middle_Form` |
| Airflow | Air Flow (Middle): | `Inventor: AirFlowMiddle_Form` |
| Unit Options | Unit Option: | `Inventor: UnitOption_Form` |
| Unit Options | Design Pressure: | `Inventor: UnitDesignPressure_Form` |
| Unit Options | Static Pressure: | `Inventor: StaticPressure_Form` |
| Unit Options | Unit Configuration: | `Inventor: UnitConfiguration_Form` |
| Unit Options | Unit Certification: | `Inventor: UnitCertification_Form` |
| Unit Options | Factory Testing: | `Inventor: FactoryTesting_Form` |
| Unit Options | Max Hole Distance: | `Inventor: MaxHoleDistanceForm` |

### PRIORITÉ 3+: (après validation Priorité 1 et 2)
- Module Dimensions
- Wall Specification (Exterior Walls)
- Additional Walls
- Floor Info
- Casing Info
- Miscellaneous

---

## 🔄 PROTOCOLE DE TRAVAIL (OBLIGATOIRE)

### Cycle de travail:

```
RÉPÉTER {
    1. MODIFIER une section (ex: Right Tunnel - Wall 01)
    2. BUILD avec: .\build-and-run.ps1 -BuildOnly
    3. SI erreurs build → CORRIGER et revenir à étape 2
    4. SI build OK → POSER LA QUESTION CI-DESSOUS
    5. ATTENDRE la réponse humaine
    6. INTÉGRER les corrections demandées
} JUSQU'À validation humaine "CONFORME"
```

### Question OBLIGATOIRE à poser après chaque section:

```
✅ Section [NOM] terminée et build réussi.

Merci de vérifier visuellement et fournir:

1) VALIDATION: oui / non
2) INSTRUCTIONS: modifications à faire
3) OBSERVATIONS: problèmes visuels (texte coupé, icône mal alignée, etc.)
4) AMÉLIORATIONS: nouvelles exigences

En attente de votre retour avant de continuer.
```

---

## ❌ INTERDICTIONS ABSOLUES

1. **PowerShell text replace** - Corrompt l'encodage UTF-8, détruit les emojis et caractères spéciaux
2. **Labels abrégés** - "Panel Insul." au lieu de "Panel Insulation:" = ERREUR
3. **Déclarer "terminé"** - Seul l'humain peut valider
4. **Modifier Interior Wall 01/02** - Déjà validés, ne pas toucher
5. **Emojis dans code C# backend** - Logger, Console = marqueurs ASCII seulement ([+], [-], [!])
6. **Emojis dans XAML** - ✅ AUTORISÉS (interface utilisateur)

---

## 🏁 CRITÈRE DE FIN

La tâche est terminée **UNIQUEMENT** quand l'humain écrit explicitement:

**"VALIDATION FINALE — CONFORME"**

Jusqu'à ce message, tu dois continuer la boucle de travail.

---

## 🚀 COMMENCE MAINTENANT

1. Ouvre le fichier `ConfigUniteWindow.xaml`
2. Trouve la section "Right Tunnel - Wall 01" (dans Interior Walls - Parallel To Front/Back)
3. Applique le pattern validé sur les 10 champs
4. Build et pose la question de validation

**GO!**
