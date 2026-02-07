# 🎯 TÂCHE POUR CURSOR / GLM : Ajouter ℹ️ Info Tooltips ConfigUnite

## DESCRIPTION DU PROBLÈME

Le module **ConfigUnite** permet de configurer les paramètres d'une unité AHU (Air Handling Unit) dans Inventor. Chaque champ de formulaire correspond à un **paramètre iLogic Inventor** (ex: `Panel_Insulation_Form`, `RT_First_Internal_Wall_Position_Form`).

**Problème actuel**: Les utilisateurs ne savent pas quel paramètre Inventor est associé à chaque champ du formulaire.

**Solution demandée**: Ajouter une icône **ℹ️** à côté de chaque label de champ, avec un **tooltip** affichant le nom du paramètre Inventor.

---

## EXEMPLE VISUEL

**AVANT:**
```
Panel Insulation:  [___________]
```

**APRÈS:**
```
Panel Insulation: ℹ  [___________]
                  ↑
         Tooltip: "Inventor: Panel_Insulation_Form"
```

---

## SPÉCIFICATION TECHNIQUE

### Fichier à modifier
```
XnrgyEngineeringAutomationTools/Modules/ConfigUnite/Views/ConfigUniteWindow.xaml
```

### Pattern XAML OBLIGATOIRE

**ANCIEN CODE (à remplacer):**
```xaml
<TextBlock Grid.Row="2" Grid.Column="0" Text="Panel Insulation:" Style="{StaticResource ModernLabel}" Margin="0,8,0,0"/>
```

**NOUVEAU CODE (pattern validé):**
```xaml
<StackPanel Grid.Row="2" Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center" Margin="0,8,0,0">
    <TextBlock Text="Panel Insulation:" Style="{StaticResource ModernLabel}" VerticalAlignment="Center"/>
    <TextBlock Text=" ℹ" FontSize="10" Foreground="#64B5F6" VerticalAlignment="Center" Cursor="Help" ToolTip="Inventor: Panel_Insulation_Form"/>
</StackPanel>
```

### Règles de Style OBLIGATOIRES

| Règle | Valeur | Raison |
|-------|--------|--------|
| Colonne labels | `Width="140"` minimum | Évite la troncature du texte |
| Labels | **COMPLETS** (pas d'abréviations) | Lisibilité |
| Centrage | `VerticalAlignment="Center"` sur StackPanel ET TextBlocks | Alignement vertical correct |
| Margin | `Margin="0,8,0,0"` sur StackPanel uniquement | Uniformité |
| Icône ℹ | `FontSize="10"`, `Foreground="#64B5F6"`, `Cursor="Help"` | Standard visuel |

### Labels INTERDITS vs CORRECTS

| ❌ INTERDIT | ✅ CORRECT |
|-------------|-----------|
| Panel Insul. | Panel Insulation: |
| Panel Constr. | Panel Construction: |
| Static Press. | Static Pressure: |
| Panel Mat. | Panel Material: |
| Liner Mat. | Liner Material: |
| Design Press. | Design Pressure: |
| Customize Constr. | Customize Construction: |

---

## SECTIONS À MODIFIER

### ✅ DÉJÀ FAIT (NE PAS TOUCHER)
- **Interior Wall 01** (Parallel To Right/Left) - lignes ~1177-1270
- **Interior Wall 02** (Parallel To Right/Left) - lignes ~1275-1375

### 📋 À FAIRE - Priorité 1: Interior Walls (Parallel To Front/Back)

Les **6 Tunnel Walls** suivants doivent être modifiés :

| GroupBox | Préfixe Inventor pour les Tooltips |
|----------|-----------------------------------|
| Right Tunnel - Wall 01 | `RT_First_Internal_Wall` |
| Right Tunnel - Wall 02 | `RT_Second_Internal_Wall` |
| Left Tunnel - Wall 01 | `LT_First_Internal_Wall` |
| Left Tunnel - Wall 02 | `LT_Second_Internal_Wall` |
| Middle Tunnel - Wall 01 | `MT_First_Internal_Wall` |
| Middle Tunnel - Wall 02 | `MT_Second_Internal_Wall` |

**Champs par wall (10 champs chacun):**

| Champ UI | Pattern Tooltip Inventor |
|----------|-------------------------|
| Include (checkbox) | `Inventor: Include_{Prefix}_Form` |
| Position | `Inventor: {Prefix}_Position_Form` |
| Customize Construction (checkbox) | `Inventor: Customize_{Prefix}_WallPanelConstruction_Form` |
| Panel Insulation | `Inventor: {Prefix}_WallPanel_Insulation_Form` |
| Panel Construction | `Inventor: {Prefix}_WallPanel_Construction_Form` |
| Panel Width | `Inventor: {Prefix}_WallPanel_Panel_Width_Form` |
| Static Pressure | `Inventor: {Prefix}_StaticPressure_Form` |
| Customize Material (checkbox) | `Inventor: Customize_{Prefix}_WallPanelMaterial_Form` |
| Panel Material | `Inventor: {Prefix}_WallPanelSMStyleName_Form` |
| Liner Material | `Inventor: {Prefix}_WallLinerSMStyleName_Form` |

### 📋 À FAIRE - Priorité 2: Unit Specification (lignes ~370-764)

| Section | Champs | Paramètres Inventor |
|---------|--------|---------------------|
| Revision | Revision Number, Checked Date, Checked By | `RevisionNumber_Form`, `CheckedDate_Form`, `CheckedBy_Form` |
| Design Info | Project Number, Drafter Name, Co-Drafter Name, Creation Date | `ProjectNumber_Form`, `DrafterName_Form`, `CoDrafterName_Form`, `CreationDate_Form` |
| Unit Info | CRAH Unit, Unit Type | `IsCRAHUnit_Form`, `UnitType_Form` |
| Airflow | 1 Tunnel (Right), Air Flow (Right), 2 Tunnels (Left), etc. | `Tunnel1Right_Form`, `AirFlowRight_Form`, ... |
| Unit Options | Unit Option, Design Pressure, Static Pressure, etc. | `UnitOption_Form`, `UnitDesignPressure_Form`, ... |

### 📋 À FAIRE - Priorité 3-5
- Module Dimensions
- Wall Specification (Exterior Walls)
- Additional Walls

---

## COMMANDE BUILD

```powershell
cd "C:\Users\mohammedamine.elgala\source\repos\XnrgyEngineeringAutomationTools"
.\build-and-run.ps1 -BuildOnly
```

---

## PROTOCOLE DE TRAVAIL

Tu dois utiliser le **Human-in-the-Loop Ralph Loop Protocol** :

1. **Modifier** une section (ex: Right Tunnel - Wall 01)
2. **Build** avec la commande ci-dessus
3. **Vérifier** que le build passe (0 erreurs)
4. **Poser la question** suivante à l'utilisateur :

```
J'ai terminé [section]. 

Merci de fournir :
1) VALIDATION (oui / non)
2) INSTRUCTIONS (ce qui doit être modifié)
3) OBSERVATIONS (problèmes visuels constatés - texte coupé, icône mal alignée, etc.)
4) AMÉLIORATIONS (nouvelles exigences)
```

5. **Attendre** la réponse avant de continuer
6. **Intégrer** les retours et corriger si nécessaire
7. **Passer** à la section suivante

---

## INTERDICTIONS ABSOLUES

- ❌ **NE PAS** utiliser PowerShell pour remplacer du texte (corruption encodage)
- ❌ **NE PAS** abréger les labels
- ❌ **NE PAS** déclarer "terminé" sans validation humaine explicite
- ❌ **NE PAS** toucher Interior Wall 01 et 02 (déjà faits et validés)
- ❌ **NE PAS** mettre d'emojis dans le code C# backend (Logger, Console)
- ✅ **Emojis AUTORISÉS** dans XAML (interface utilisateur)

---

## FICHIERS DE RÉFÉRENCE

| Fichier | Usage |
|---------|-------|
| `ConfigUniteWindow.xaml` (lignes 1177-1375) | Exemple du pattern validé |
| `ConfigUniteDataModel.cs` | Noms des paramètres Inventor |
| `STEP_ADD_INFO_TOOLTIPS.md` | Prompt complet avec toutes les specs |

---

## CRITÈRE DE FIN

La tâche est terminée UNIQUEMENT quand l'utilisateur écrit :
**"VALIDATION FINALE — CONFORME"**

Jusqu'à ce moment, tu dois continuer la boucle de travail.
