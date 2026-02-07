# 🔄 HUMAN-IN-THE-LOOP RALPH LOOP — TÂCHE: Ajouter ℹ️ Info Tooltips ConfigUnite

## RÔLE
Tu es une IA EXÉCUTANTE. Tu n'as PAS l'autorité de valider la conformité finale.

## PROTOCOLE
Human-in-the-Loop Ralph Loop — Mode Persistant

## RÈGLES ABSOLUES
- Chaque réponse fait partie d'une BOUCLE CONTINUE
- Tu ne dois JAMAIS considérer une réponse humaine comme une "nouvelle requête indépendante"
- Toute réponse humaine est une ENTRÉE DE SPÉCIFICATION incrémentale
- Tu n'as PAS le droit de conclure ou de dire "terminé"
- Tu dois TOUJOURS poser la question de validation à la fin
- BUILD SUCCESS n'est PAS un critère de conformité

---

## SPECIFICATION DE LA TÂCHE

### Contexte
Module ConfigUnite dans XnrgyEngineeringAutomationTools (.NET Framework 4.8 WPF)
Fichier principal: `Modules/ConfigUnite/Views/ConfigUniteWindow.xaml`

### Objectif
Ajouter des icônes ℹ️ info avec tooltips sur TOUS les champs de formulaire pour afficher le nom du paramètre Inventor correspondant.

### Pattern XAML Validé (OBLIGATOIRE)
```xaml
<!-- Pour chaque label de champ -->
<StackPanel Grid.Row="X" Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center" Margin="0,8,0,0">
    <TextBlock Text="Label Complet:" Style="{StaticResource ModernLabel}" VerticalAlignment="Center"/>
    <TextBlock Text=" ℹ" FontSize="10" Foreground="#64B5F6" VerticalAlignment="Center" Cursor="Help" ToolTip="Inventor: Nom_Parametre_Form"/>
</StackPanel>

<!-- Pour les CheckBox -->
<CheckBox ... ToolTip="Inventor: Nom_Parametre_Form" .../>
```

### Règles de Style OBLIGATOIRES
1. **Colonne labels** : `Width="140"` minimum (pas 130)
2. **Labels COMPLETS** : Pas d'abréviations
   - ❌ "Panel Insul." → ✅ "Panel Insulation:"
   - ❌ "Panel Constr." → ✅ "Panel Construction:"
   - ❌ "Static Press." → ✅ "Static Pressure:"
   - ❌ "Panel Mat." → ✅ "Panel Material:"
   - ❌ "Liner Mat." → ✅ "Liner Material:"
3. **Centrage vertical** : `VerticalAlignment="Center"` sur StackPanel ET TextBlocks
4. **Margin sur StackPanel** : `Margin="0,8,0,0"` (pas sur les TextBlocks individuels)
5. **Icône ℹ** : `FontSize="10"`, `Foreground="#64B5F6"`, `Cursor="Help"`

---

## SECTIONS À MODIFIER

### ✅ DÉJÀ FAIT (NE PAS TOUCHER)
- Interior Wall 01 (Parallel To Right/Left) - lignes ~1177-1270
- Interior Wall 02 (Parallel To Right/Left) - lignes ~1275-1375

### 📋 PRIORITÉ 1: Interior Walls (Parallel To Front/Back)

Les 6 Tunnel Walls à modifier avec leurs préfixes Inventor:

| GroupBox | Préfixe Inventor |
|----------|------------------|
| Right Tunnel - Wall 01 | `RT_First_Internal_Wall` |
| Right Tunnel - Wall 02 | `RT_Second_Internal_Wall` |
| Left Tunnel - Wall 01 | `LT_First_Internal_Wall` |
| Left Tunnel - Wall 02 | `LT_Second_Internal_Wall` |
| Middle Tunnel - Wall 01 | `MT_First_Internal_Wall` |
| Middle Tunnel - Wall 02 | `MT_Second_Internal_Wall` |

**Champs par wall (10 champs chacun):**

| Champ UI | Pattern ToolTip Inventor |
|----------|-------------------------|
| Include (checkbox) | `Include_{Prefix}_Form` |
| Position | `{Prefix}_Position_Form` |
| Customize Construction (checkbox) | `Customize_{Prefix}_WallPanelConstruction_Form` |
| Panel Insulation | `{Prefix}_WallPanel_Insulation_Form` |
| Panel Construction | `{Prefix}_WallPanel_Construction_Form` |
| Panel Width | `{Prefix}_WallPanel_Panel_Width_Form` |
| Static Pressure | `{Prefix}_StaticPressure_Form` |
| Customize Material (checkbox) | `Customize_{Prefix}_WallPanelMaterial_Form` |
| Panel Material | `{Prefix}_WallPanelSMStyleName_Form` |
| Liner Material | `{Prefix}_WallLinerSMStyleName_Form` |

### 📋 PRIORITÉ 2: Unit Specification (lignes ~370-764)

| Section | Champs | Paramètres Inventor |
|---------|--------|---------------------|
| Revision | Revision Number, Checked Date, Checked By | `RevisionNumber_Form`, `CheckedDate_Form`, `CheckedBy_Form` |
| Design Info | Project Number, Drafter Name, Co-Drafter Name, Creation Date | `ProjectNumber_Form`, `DrafterName_Form`, `CoDrafterName_Form`, `CreationDate_Form` |
| Unit Info | CRAH Unit, Unit Type | `IsCRAHUnit_Form`, `UnitType_Form` |
| Airflow | 1 Tunnel (Right), Air Flow (Right), 2 Tunnels (Left), AirFlow (Left), 3 Tunnels (Middle), AirFlow (Middle) | `Tunnel1Right_Form`, `AirFlowRight_Form`, `Tunnel2Left_Form`, `AirFlowLeft_Form`, `Tunnel3Middle_Form`, `AirFlowMiddle_Form` |
| Unit Options | Unit Option, Design Pressure, Static Pressure, Unit Configuration, Unit Certification, Factory Testing, MaxHoleDistance | `UnitOption_Form`, `UnitDesignPressure_Form`, `StaticPressure_Form`, `UnitConfiguration_Form`, `UnitCertification_Form`, `FactoryTesting_Form`, `MaxHoleDistanceForm` |

### 📋 PRIORITÉ 3: Floor Info, Casing Info, Miscellaneous
À documenter après validation Priorité 2

### 📋 PRIORITÉ 4: Module Dimensions
Tous les champs de dimensions (Width, Height, Length, etc.)

### 📋 PRIORITÉ 5: Wall Specification (Exterior Walls, Additional Walls)
Même pattern que Interior Walls

---

## COMMANDE BUILD

```powershell
cd "C:\Users\mohammedamine.elgala\source\repos\XnrgyEngineeringAutomationTools"
.\build-and-run.ps1 -BuildOnly
```

---

## BOUCLE D'EXÉCUTION

1. **LIRE** intégralement cette specification + toutes les INSTRUCTIONS/OBSERVATIONS humaines précédentes

2. **/speckit.implement**
   - Modifier ConfigUniteWindow.xaml selon le pattern validé
   - Procéder section par section (une priorité à la fois)
   - Ne rien supposer, ne rien ignorer

3. **BUILD**
   - Exécuter: `.\build-and-run.ps1 -BuildOnly`
   - Si échec: corriger uniquement les erreurs de build
   - Reprendre sans progresser fonctionnellement

4. **/speckit.analyze**
   - Comparer l'implémentation avec la specification
   - Identifier écarts, approximations et manques

5. **SUSPENDRE** toute action

6. **POSER OBLIGATOIREMENT LA QUESTION SUIVANTE:**

```
Merci de fournir :
1) VALIDATION (oui / non)
2) INSTRUCTIONS (ce qui doit être fait ou modifié)
3) OBSERVATIONS (écarts constatés à l'exécution réelle - UI, centrage, texte coupé, etc.)
4) AMÉLIORATIONS (attendus non explicités précédemment)
```

7. **ATTENDRE** la réponse humaine

8. **Intégrer** automatiquement la réponse comme extension de la SPECIFICATION

9. **/speckit.fix**
   - Corriger UNIQUEMENT sur la base des retours humains
   - Ne pas introduire de nouvelles fonctionnalités

10. **Reprendre** la boucle depuis l'étape 1

---

## CRITÈRE DE SORTIE
UNIQUEMENT si l'humain écrit explicitement:
**"VALIDATION FINALE — CONFORME"**

---

## FICHIERS RÉFÉRENCE
- **Pattern validé**: Interior Wall 01 et 02 dans `ConfigUniteWindow.xaml` (lignes ~1177-1375)
- **Data Model**: `Modules/ConfigUnite/Models/ConfigUniteDataModel.cs` (pour les noms de paramètres Inventor)
- **Instructions globales**: `.github/copilot-instructions.md`

---

## INTERDICTIONS ABSOLUES
- ❌ NE PAS utiliser PowerShell pour remplacer du texte (corruption encodage)
- ❌ NE PAS abréger les labels
- ❌ NE PAS déclarer "terminé" sans validation humaine
- ❌ NE PAS modifier Interior Wall 01 et 02 (déjà faits et validés)
- ❌ NE PAS mettre d'emojis dans le code backend (Logger, Console)
- ✅ Emojis AUTORISÉS dans XAML (interface utilisateur)
