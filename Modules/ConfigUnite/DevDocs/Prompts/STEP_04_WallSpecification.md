# STEP 4: Wall Specification - CORRECTIONS VISUELLES ET FONCTIONNELLES

## COMMANDES SPECKIT

```bash
/speckit.implement STEP_04   # Implementer toutes les exigences
/speckit.analyze STEP_04     # Analyser apres implementation (OBLIGATOIRE)
/speckit.fix STEP_04         # Corriger les ecarts detectes
```

---

# ⚠️ CORRECTIONS PRIORITAIRES IDENTIFIEES PAR LE CHEF D'ORCHESTRE

```
╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║                                                                                            ║
║   DATE: 2026-01-29                                                                        ║
║   ANALYSE: Tests visuels et fonctionnels apres implementation initiale                    ║
║   RESULTAT: 4 problemes majeurs identifies                                                ║
║                                                                                            ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝
```

## CORRECTION #1: SCROLL MOLETTE NON FONCTIONNEL (PRIORITE CRITIQUE)

**Symptome:** La molette de la souris ne fait pas defiler le contenu dans l'onglet "Wall Specification", mais fonctionne dans "Unit Specification".

**Fichiers:** `ConfigUniteWindow.xaml` et `ConfigUniteWindow.xaml.cs`

**CAUSE RACINE IDENTIFIEE:**
Le probleme est une **imbrication de ScrollViewers** dans Wall Specification:

```
Wall Specification (NE MARCHE PAS):
└── ScrollViewer (EXTERNE - ligne 930) <-- CAPTURE LES EVENEMENTS MOLETTE
    └── StackPanel
        └── TabControl (WallSpecTabControl)
            └── TabItem "Exterior Walls"
                └── ScrollViewer (INTERNE) <-- NE RECOIT JAMAIS LES EVENEMENTS

Unit Specification (MARCHE):
└── TabControl (pas de ScrollViewer externe)
    └── TabItem "Unit Info"
        └── ScrollViewer <-- RECOIT LES EVENEMENTS DIRECTEMENT
```

Le ScrollViewer externe (ligne 930) intercepte TOUS les evenements PreviewMouseWheel et les marque comme "Handled", donc les ScrollViewers internes ne les recoivent jamais.

**SOLUTION: Supprimer le ScrollViewer externe inutile dans Wall Specification**

**ETAPE 1: Dans ConfigUniteWindow.xaml, REMPLACER:**

AVANT (ligne 929-932):
```xml
<TabItem Header="Wall Specification" Style="{StaticResource ModernTabItem}">
    <ScrollViewer VerticalScrollBarVisibility="Auto" Padding="10">
        <StackPanel>
```

APRES:
```xml
<TabItem Header="Wall Specification" Style="{StaticResource ModernTabItem}">
    <StackPanel Margin="10">
```

**ETAPE 2: SUPPRIMER la fermeture du ScrollViewer externe**
Chercher la ligne `</ScrollViewer>` correspondante (juste avant `</TabItem>` de Wall Specification) et la SUPPRIMER.

**ETAPE 3: (OPTIONNEL) Supprimer le code C# du constructeur**
Les ScrollViewers internes n'ont PAS besoin de handler custom car ils sont maintenant au premier niveau (pas d'imbrication).
Le scroll natif WPF fonctionnera automatiquement.

Supprimer ces lignes si elles existent:
```csharp
// Activer le scroll molette pour tous les ScrollViewers
foreach (var sv in FindVisualChildren<ScrollViewer>(this))
{
    sv.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
}
```

**POURQUOI CETTE SOLUTION:**
- Le ScrollViewer externe n'est PAS necessaire car chaque sous-onglet (Exterior Walls, Interior Walls, etc.) a DEJA son propre ScrollViewer
- En supprimant l'imbrication, le scroll natif WPF fonctionne automatiquement
- C'est la MEME structure que "Unit Specification" qui fonctionne parfaitement

**VERIFICATION:**
1. La molette doit faire defiler le contenu dans TOUS les sous-onglets de Wall Specification
2. Le comportement doit etre identique a "Unit Specification"
`[ConfigUnite] [>] Activation scroll molette pour 12 ScrollViewers`
(le nombre doit etre > 0, sinon le bug persiste)

---

## CORRECTION #2: RETIRER LES NOMS iLOGIC DES LABELS UI (PRIORITE HAUTE)

**Symptome:** Les labels affichent les noms de parametres iLogic entre parentheses, ce qui cause:
- Chevauchement de texte avec les controles
- Interface surchargee et non professionnelle
- Information technique visible par l'utilisateur final

**Exemple AVANT (INCORRECT):**
```xml
Content="Include Back Wall 02 (Include_Back_Wall_02_Form)"
Text="Bottom Position (Back_Wall_02_Bottom_Position_Form):"
```

**Exemple APRES (CORRECT):**
```xml
Content="Include Back Wall 02"
Text="Bottom Position:"
```

**Fichier:** `ConfigUniteWindow.xaml`

**Action:** Rechercher et remplacer TOUS les patterns suivants:
- `(Include_*_Form)` -> supprimer
- `(*_Position_Form):` -> supprimer la partie entre parentheses
- `(*_Insulation_Form)` -> supprimer
- `(*_Construction_Form)` -> supprimer
- `(*_Width_Form)` -> supprimer
- `(*_StaticPressure_Form)` -> supprimer
- `(*_Material_Form)` -> supprimer
- `(*SMStyleName_Form)` -> supprimer

**NOTE:** Les noms iLogic doivent rester dans le CODE-BEHIND (pour le mapping avec Inventor), PAS dans l'interface utilisateur.

---

## CORRECTION #3: COMBOBOX SANS ITEMSSOURCE (PRIORITE HAUTE)

**Symptome:** Certains ComboBox apparaissent vides (pas de liste deroulante).

**Fichier:** `ConfigUniteWindow.xaml.cs`

**Action:** Verifier que TOUS les ComboBox ont leur ItemsSource configure.

**Liste des ItemsSource requis (depuis ParameterLists.cs):**

| ComboBox Pattern | ItemsSource |
|------------------|-------------|
| `Cmb*PanelInsulation` | `ParameterLists.PanelInsulation` |
| `Cmb*PanelConstruction` | `ParameterLists.PanelConstruction` |
| `Cmb*PanelWidth` | `ParameterLists.PanelWidth` |
| `Cmb*StaticPressure` | `ParameterLists.StaticPressure` |
| `Cmb*PanelMaterial` | `ParameterLists.WallPanelMaterial` |
| `Cmb*LinerMaterial` | `ParameterLists.WallLinerMaterial` |

**Code a verifier/completer dans InitializeWallSpecificationControls():**
```csharp
// Interior Wall 01
CmbInteriorWall01PanelInsulation.ItemsSource = ParameterLists.PanelInsulation;
CmbInteriorWall01PanelConstruction.ItemsSource = ParameterLists.PanelConstruction;
CmbInteriorWall01PanelWidth.ItemsSource = ParameterLists.PanelWidth;
CmbInteriorWall01StaticPressure.ItemsSource = ParameterLists.StaticPressure;
CmbInteriorWall01PanelMaterial.ItemsSource = ParameterLists.WallPanelMaterial;
CmbInteriorWall01LinerMaterial.ItemsSource = ParameterLists.WallLinerMaterial;

// Interior Wall 02 - meme pattern
// ... etc pour TOUS les ComboBox
```

---

## CORRECTION #4: LARGEUR DES COLONNES INSUFFISANTE (PRIORITE MOYENNE)

**Symptome:** Les labels sont tronques ou chevauchent les controles.

**Fichier:** `ConfigUniteWindow.xaml`

**Action:** Ajuster les ColumnDefinitions dans les grilles:

**AVANT:**
```xml
<ColumnDefinition Width="280"/>
```

**APRES:**
```xml
<ColumnDefinition Width="200"/>
```

Les labels etant maintenant plus courts (sans les noms iLogic), 200px suffit.

---

## CHECKLIST DE VERIFICATION APRES CORRECTIONS

```
╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║  VERIFICATION VISUELLE ET FONCTIONNELLE:                                                  ║
║                                                                                            ║
║  [ ] SCROLL: La molette de souris fait defiler le contenu dans TOUS les onglets          ║
║  [ ] LABELS: Aucun nom de parametre iLogic visible dans l'interface                       ║
║  [ ] LABELS: Aucun chevauchement de texte                                                 ║
║  [ ] COMBOBOX: Toutes les listes deroulantes affichent des options                        ║
║  [ ] COMBOBOX: Les valeurs par defaut sont selectionnees                                  ║
║  [ ] BUILD: Compile sans erreur                                                           ║
║  [ ] VISUEL: L'interface est propre et professionnelle                                    ║
║                                                                                            ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝
```

---

# 🔄 RALPH LOOP PROTOCOL v3.0.1 - PROTOCOLE DE CONVERGENCE CONTROLEE

```
╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║                                                                                            ║
║   REFERENCE COMPLETE: RALPH_LOOP_PROTOCOL_V3.md (a la racine du projet)                   ║
║                                                                                            ║
║   ════════════════════════════════════════════════════════════════════════════════════    ║
║   PRINCIPE FONDAMENTAL                                                                     ║
║   ════════════════════════════════════════════════════════════════════════════════════    ║
║                                                                                            ║
║   Le Ralph Loop Protocol n'est PAS un protocole de compilation.                           ║
║   La compilation sans erreur est une condition MINIMALE sans valeur fonctionnelle.        ║
║                                                                                            ║
║   ❌ Ce n'est PAS un protocole "one-shot"                                                 ║
║   ❌ BUILD SUCCESS n'est qu'un SOUS-CRITERE, jamais un critere de sortie                  ║
║   ✅ C'est un protocole de CONVERGENCE CONTROLEE                                          ║
║   ✅ Il est INDEPENDANT de la puissance du modele IA                                      ║
║   ✅ Meme une IA faible peut converger vers une solution avancee                          ║
║                                                                                            ║
║   L'IA ne peut JAMAIS declarer une tache "terminee" tant que TOUTES les exigences         ║
║   formelles, fonctionnelles et structurelles de la specification ne sont pas              ║
║   INTEGRALEMENT respectees.                                                               ║
║                                                                                            ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝
```

## BOUCLE FORMELLE (Ralph Loop v3.0.1 - VERSION RENFORCEE)

```
WHILE conformite < 100% vis-a-vis de STEP_04_WallSpecification.md
{
    PHASE 1: LECTURE
    LIRE STEP_04_WallSpecification.md (INTEGRALEMENT, sans interpretation implicite)

    PHASE 2: IMPLEMENTATION
    /speckit.implement STEP_04
        - Implementer UNIQUEMENT ce qui est requis
        - Ne rien supposer non explicitement specifie

    PHASE 3: COMPILATION
    BUILD (.\build-and-run.ps1 -BuildOnly)
    IF (BUILD_FAILED)
    {
        CORRIGER exclusivement les erreurs de build
        NE PAS considerer cela comme une progression fonctionnelle
        CONTINUE
    }

    PHASE 4: ANALYSE CRITIQUE
    /speckit.analyze STEP_04
        - Comparer CHAQUE exigence de SPECIFICATION
        - Marquer chaque exigence comme:
            [CONFORME] / [PARTIELLE] / [NON CONFORME]
        - Justifier explicitement chaque statut

    SCORE = (NOMBRE_EXIGENCES_CONFORMES / NOMBRE_EXIGENCES_TOTAL) * 100%

    PHASE 5: CORRECTION CIBLEE
    IF (SCORE < 100%)
    {
        IDENTIFIER explicitement TOUS les ecarts
        CLASSER les ecarts: (manque, erreur, approximation, regression)

        /speckit.fix STEP_04
            - Corriger UNIQUEMENT les ecarts identifies
            - NE PAS introduire de nouvelles fonctionnalites
            - NE PAS degrader les exigences deja conformes

        CONTINUE
    }
}

RETURN "STEP_04 COMPLETE - CONFORMITE 100% VERIFIEE AVEC SPECIFICATION"
```

## PROCEDURE DETAILLEE

### ETAPE 1: IMPLEMENTER
Fais toutes les modifications demandees dans ce prompt.

### ETAPE 2: BUILD
```powershell
cd C:\Users\mohammedamine.elgala\source\repos\XnrgyEngineeringAutomationTools
.\build-and-run.ps1 -BuildOnly
```

### ETAPE 3: ANALYSER LE BUILD

**SI BUILD ECHOUE:** Corriger les erreurs -> Retourner ETAPE 2

**SI BUILD REUSSI:** PASSER A ETAPE 4 (Auto-Verification)

### ETAPE 4: AUTO-VERIFICATION (/speckit.analyze)

**APRES chaque build reussi, tu DOIS:**
1. Relire les REQUIS de ce prompt (section REQUIS FONCTIONNELS)
2. Comparer avec ce que tu as REELLEMENT implemente (pas ce que tu penses avoir fait)
3. Calculer le pourcentage de completion

```
╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║  CHECKLIST DE VERIFICATION STEP_04:                                                        ║
║                                                                                            ║
║  [ ] Interior Wall 01 - Back/Front/Right/Left                                             ║
║  [ ] Interior Wall 02 - Back/Front/Right/Left                                             ║
║  [ ] Additional Wall 02 - Back/Front/Right/Left/Top                                       ║
║  [ ] Additional Wall 03 - Back/Front/Right/Left/Top                                       ║
║  [ ] Additional Wall 04 - Back/Front/Right/Left/Top                                       ║
║  [ ] Additional Wall 05 - Back/Front/Right/Left/Top                                       ║
║  [ ] Parallel To Front (Tunnel Walls) - toutes directions                                 ║
║  [ ] Parallel To Back (Tunnel Walls) - toutes directions                                  ║
║  [ ] Tous les controles XAML existent avec bons noms x:Name                               ║
║  [ ] Tous les bindings fonctionnent                                                       ║
║  [ ] Build compile sans erreurs                                                           ║
║                                                                                            ║
║  SCORE: [X]/12 items = XX% completion                                                     ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝
```

### ETAPE 5: FIX OU TERMINER

**SI completion < 100%:** /speckit.fix -> Corriger -> Retourner ETAPE 2

**SI completion = 100%:** MISSION ACCOMPLIE - Generer RAPPORT FINAL

## REGLES ABSOLUES INVIOLABLES

```
╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║                                                                                            ║
║  1. NE JAMAIS dire "termine" si conformite < 100%                                         ║
║                                                                                            ║
║  2. NE JAMAIS ignorer un requis de la specification                                       ║
║                                                                                            ║
║  3. TOUJOURS comparer implementation vs specification (pas vs intuition)                  ║
║                                                                                            ║
║  4. TOUJOURS corriger les ecarts AVANT de terminer                                        ║
║                                                                                            ║
║  5. BUILD SUCCESS n'est PAS suffisant - CONFORMITE TOTALE est le but                      ║
║                                                                                            ║
║  6. La boucle ne peut se terminer QUE sur conformite 100% explicite et justifiee          ║
║                                                                                            ║
║  7. Si blocage technique empeche 100%, DECLARER explicitement le blocage                  ║
║     (pour escalade vers un agent plus puissant)                                           ║
║                                                                                            ║
║  [!!!] BUILD SUCCESS + REQUIS NON RESPECTES = ECHEC                                       ║
║  [!!!] BUILD SUCCESS + 100% REQUIS = SUCCES                                               ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝
```

## GESTION DES BLOCAGES TECHNIQUES

Si tu ne peux pas atteindre 100% a cause d'un empechement technique:

1. **DECLARER explicitement le blocage:**
   ```
   [!] BLOCAGE TECHNIQUE DETECTE
   - Requis concerne: [description]
   - Raison du blocage: [explication technique]
   - Score atteint: XX%
   - Actions tentees: [liste]
   ```

2. **ESCALADER vers un agent plus puissant**
   - L'utilisateur peut assigner la tache a un autre agent
   - Le nouvel agent reprend avec le contexte complet

3. **NE JAMAIS:**
   - Ignorer silencieusement le blocage
   - Declarer "termine" avec moins de 100%
   - Approximer ou contourner le requis

## ERREURS COURANTES ET SOLUTIONS

| Erreur | Cause | Solution |
|--------|-------|----------|
| `CS0103: The name 'CmbXxx' does not exist` | C# reference un controle XAML inexistant | Creer le controle dans XAML AVANT |
| `XML parsing error: EntityName` | Caractere `&` non echappe | Remplacer `&` par `et` ou `&amp;` |
| `XAML parse error line 1` | Ligne 1 corrompue | Restaurer: `<Window x:Class="...` |
| `CS0246: Type not found` | Using manquant | Ajouter le using necessaire |
| `Binding path not found` | Propriete inexistante dans DataModel | Verifier ConfigUniteDataModel.cs |

## MINDSET RALPH v3.0

```
╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║                                                                                            ║
║   "Ralph will test you. Every time Ralph has taken a wrong direction,                     ║
║    I haven't blamed the tools; instead, I've looked inside.                               ║
║    Each time Ralph does something bad, Ralph gets tuned - like a guitar."                 ║
║                                                                                            ║
║   TU ES RALPH. Tu tombes, tu te releves, tu continues.                                    ║
║   Les erreurs ne sont pas des echecs, ce sont des FEEDBACKS.                              ║
║   Chaque erreur te rapproche de la solution.                                              ║
║                                                                                            ║
║   v3.0: La puissance de l'IA n'est pas le facteur determinant.                            ║
║         La BOUCLE est le facteur determinant.                                             ║
║         Meme une IA faible peut converger vers une solution avancee.                      ║
║                                                                                            ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝
```

---

## 📋 BRIEFING PROJET - CONTEXTE CRITIQUE AVANT TOUTE MODIFICATION

### 1. NATURE DU PROJET - COMPLEXITE EXTREME

```
╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║  XNRGY ENGINEERING AUTOMATION TOOLS (XEAT)                                                ║
║  ═══════════════════════════════════════════                                              ║
║                                                                                            ║
║  Type:        Application WPF Desktop (12 modules)                                        ║
║  Langage:     C# 10.0 / VB.NET                                                            ║
║  Framework:   .NET Framework 4.8 (PAS .NET 8/9!)                                          ║
║  UI:          WPF avec XAML (Windows Presentation Foundation)                             ║
║  SDK:         Autodesk Inventor 2026.2 + Vault SDK v31.0.84                               ║
║                                                                                            ║
║  [!!!] CE N'EST PAS UNE APPLICATION WEB OU CONSOLE SIMPLE!                                ║
║  [!!!] WPF + Inventor COM Interop = Regles strictes de developpement                      ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝
```

### 2. PARTICULARITES .NET FRAMEWORK 4.8 + WPF

```
╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║  DIFFERENCE CRITIQUE: .NET Framework 4.8 vs .NET 8/9                                      ║
║  ════════════════════════════════════════════════════                                     ║
║                                                                                            ║
║  BUILD COMMAND:                                                                           ║
║  ─────────────                                                                            ║
║  ✅ CORRECT:   MSBuild via .\build-and-run.ps1                                           ║
║  ❌ INTERDIT:  dotnet build (NE FONCTIONNE PAS pour ce projet!)                          ║
║                                                                                            ║
║  RAISON: dotnet build ne genere PAS les fichiers .g.cs pour WPF                          ║
║  CONSEQUENCE: Application crash au demarrage si build avec dotnet                        ║
║                                                                                            ║
║  FICHIERS GENERES PAR MSBUILD:                                                            ║
║  - obj\Debug\*.g.cs         -> Code-behind genere depuis XAML                            ║
║  - obj\Debug\*.g.i.cs       -> Fichiers intermediaires                                   ║
║  - obj\Debug\*.baml         -> XAML compile en binaire                                   ║
║                                                                                            ║
║  SI CES FICHIERS SONT ABSENTS = L'APPLICATION NE DEMARRE PAS!                             ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝
```

### 3. ARCHITECTURE WPF - XAML + CODE-BEHIND

```
╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║  LIAISON XAML <-> C# - MECANISME CRITIQUE                                                 ║
║  ════════════════════════════════════════                                                 ║
║                                                                                            ║
║  ConfigUniteWindow.xaml     -> Definition UI (controles, layout, bindings)               ║
║  ConfigUniteWindow.xaml.cs  -> Code-behind (logique, evenements)                         ║
║  ConfigUniteWindow.g.cs     -> GENERE AUTOMATIQUEMENT par MSBuild                        ║
║                                                                                            ║
║  Le fichier .g.cs contient:                                                               ║
║  - Les declarations des controles nommes (x:Name="...")                                  ║
║  - Les connexions entre XAML et C#                                                       ║
║  - InitializeComponent() qui charge le XAML                                              ║
║                                                                                            ║
║  WORKFLOW DE BUILD:                                                                       ║
║  1. MSBuild parse ConfigUniteWindow.xaml                                                  ║
║  2. MSBuild genere ConfigUniteWindow.g.cs avec tous les x:Name                           ║
║  3. MSBuild compile ConfigUniteWindow.xaml.cs + .g.cs ensemble                           ║
║  4. Si x:Name dans .xaml.cs mais PAS dans .xaml -> Erreur CS0103!                        ║
║                                                                                            ║
║  CONSEQUENCE:                                                                              ║
║  - Un controle doit EXISTER dans XAML (x:Name="...") AVANT d'etre reference en C#        ║
║  - Si C# reference un controle inexistant -> CS0103: name does not exist                 ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝
```

### 4. SDK INVENTOR 2026 + VAULT v31.0.84

```
╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║  INTEGRATION AUTODESK - REGLES SPECIALES                                                  ║
║  ═══════════════════════════════════════                                                  ║
║                                                                                            ║
║  INVENTOR COM INTEROP:                                                                     ║
║  - Toujours ouvrir documents en mode Visible: true                                        ║
║  - Mode silencieux (Visible: false) cause des echecs de Check-In                         ║
║  - Les parametres iLogic ont des noms EXACTS (ex: First_Internal_Wall_Position_Form)     ║
║                                                                                            ║
║  VAULT SDK v31.0.84:                                                                       ║
║  - Connexion via VDF.Vault.Library.ConnectionManager                                      ║
║  - Proprietes custom: Project=ID112, Reference=ID121, Module=ID122                        ║
║  - Erreur 1013 = CheckOut requis avant modification                                       ║
║  - Erreur 1003 = Job Processor actif                                                      ║
║                                                                                            ║
║  iLOGIC PARAMETERS:                                                                        ║
║  - Source de verite: 000000000-params.xml                                                 ║
║  - Les noms sont case-sensitive et ont des patterns stricts                              ║
║  - Exemple: Include_First_Internal_Wall_Form (pas Include_First_Internal_wall_Form)      ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝
```

### 5. ENCODAGE ET CARACTERES - PIEGES FREQUENTS

```
╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║  PROBLEMES D'ENCODAGE RENCONTRES PENDANT LE DEVELOPPEMENT                                 ║
║  ══════════════════════════════════════════════════════                                   ║
║                                                                                            ║
║  XAML = XML STRICT:                                                                        ║
║  - Le caractere & (ampersand) doit etre echappe: &amp;                                   ║
║  - Ou mieux: remplacer "01 & 02" par "01 et 02"                                          ║
║  - Erreur type: "An error occurred while parsing EntityName"                             ║
║                                                                                            ║
║  LIGNE 1 DU XAML:                                                                          ║
║  - Contient la declaration <Window x:Class="...">                                        ║
║  - SI CORROMPUE = Le fichier entier est invalide                                         ║
║  - Ne JAMAIS modifier cette ligne via rechercher/remplacer en masse                      ║
║                                                                                            ║
║  POWERSHELL -replace:                                                                      ║
║  - INTERDIT pour modifier du XAML ou des fichiers sources                                ║
║  - Casse l'encodage Unicode (emojis, caracteres speciaux, boites)                        ║
║  - Utiliser UNIQUEMENT replace_string_in_file de Copilot                                 ║
║                                                                                            ║
║  EMOJIS:                                                                                   ║
║  - ✅ AUTORISES dans XAML (interfaces utilisateur)                                        ║
║  - ❌ INTERDITS dans code C# (Logger, Console, commentaires)                              ║
║  - Remplacer par marqueurs ASCII: [+] [-] [!] [>] [i] [~]                                ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝
```

### 6. ERREURS VECUES PENDANT CE DEVELOPPEMENT (A EVITER ABSOLUMENT)

```
╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║  ERREUR #1: CODE C# POUR CONTROLES XAML INEXISTANTS                                       ║
║  ═══════════════════════════════════════════════════                                      ║
║                                                                                            ║
║  CE QUI S'EST PASSE:                                                                       ║
║  - GLM a ajoute dans ConfigUniteWindow.xaml.cs:                                          ║
║    CmbBackWallPanel.ItemsSource = ParameterLists.PanelWidth;                             ║
║  - MAIS CmbBackWallPanel n'existait PAS dans ConfigUniteWindow.xaml                      ║
║                                                                                            ║
║  RESULTAT: 10 erreurs CS0103 au build                                                     ║
║  CORRECTION: Commenter le code C# en attendant la creation XAML                          ║
╠═══════════════════════════════════════════════════════════════════════════════════════════╣
║  ERREUR #2: AMPERSAND NON ECHAPPE DANS XAML                                               ║
║  ════════════════════════════════════════════                                             ║
║                                                                                            ║
║  CE QUI S'EST PASSE:                                                                       ║
║  - GLM a ecrit: <TextBlock Text="Interior Wall 01 & 02"/>                                ║
║  - Le parser XML a echoue sur le caractere &                                             ║
║                                                                                            ║
║  RESULTAT: Erreur XML "An error occurred while parsing EntityName"                        ║
║  CORRECTION: Remplacer & par "et" ou &amp;                                               ║
╠═══════════════════════════════════════════════════════════════════════════════════════════╣
║  ERREUR #3: CORRUPTION LIGNE 1 DU XAML                                                    ║
║  ════════════════════════════════════════                                                 ║
║                                                                                            ║
║  CE QUI S'EST PASSE:                                                                       ║
║  - Une operation de rechercher/remplacer a corrompu la ligne 1                           ║
║  - <Window x:Class="..." est devenu <Wind Pff ow                                         ║
║                                                                                            ║
║  RESULTAT: Fichier XAML completement invalide, build impossible                           ║
║  CORRECTION: Restauration manuelle de la ligne 1                                          ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝
```

### 7. STRUCTURE DES FICHIERS DU MODULE CONFIG UNITE

```
Modules/ConfigUnite/
├── Views/
│   ├── ConfigUniteWindow.xaml       <- UI principale (~1300 lignes)
│   └── ConfigUniteWindow.xaml.cs    <- Code-behind (~300 lignes)
├── ViewModels/
│   └── ConfigUniteViewModel.cs      <- Logique MVVM
├── Models/
│   ├── ConfigUniteDataModel.cs      <- Modele de donnees
│   └── ParameterLists.cs            <- Listes dropdowns (UTILISER CECI!)
├── Services/
│   └── ConfigUniteService.cs        <- Logique metier
└── DevDocs/
    ├── CONFIG_UNITE_MASTER.md       <- Reference MASTER
    ├── Validations/
    │   └── 000000000-params.xml     <- Source VERITE iLogic
    └── Prompts/
        └── STEP_04_WallSpecification.md <- CE FICHIER
```

### 8. COMMANDE DE BUILD - A UTILISER SYSTEMATIQUEMENT

```powershell
# TOUJOURS depuis le dossier XnrgyEngineeringAutomationTools
cd XnrgyEngineeringAutomationTools
.\build-and-run.ps1 -BuildOnly

# VERIFIER: 0 erreurs avant de continuer
# Les warnings (710+) sont acceptables
```

---

## ⚠️ REGLES ABSOLUES POUR GLM - LIRE AVANT TOUTE ACTION

```
╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║  REGLE #1: SYNCHRONISATION XAML <-> C# OBLIGATOIRE                                        ║
║  ════════════════════════════════════════════════════════════════                         ║
║  JAMAIS ajouter du code C# qui reference un controle XAML inexistant!                     ║
║  TOUJOURS creer le ComboBox/TextBox dans XAML AVANT d'y faire reference en C#             ║
║                                                                                            ║
║  WORKFLOW OBLIGATOIRE:                                                                     ║
║  1. Creer le controle dans ConfigUniteWindow.xaml avec x:Name="..."                       ║
║  2. Sauvegarder le XAML                                                                   ║
║  3. ENSUITE ajouter la reference dans ConfigUniteWindow.xaml.cs                           ║
║                                                                                            ║
║  ERREUR TYPIQUE A EVITER:                                                                 ║
║  ❌ CmbBackWallPanel.ItemsSource = ... // Si CmbBackWallPanel n'existe pas dans XAML     ║
║  ✅ D'abord creer <ComboBox x:Name="CmbBackWallPanel" .../> dans XAML                    ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝

╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║  REGLE #2: ENCODAGE XML - CARACTERES SPECIAUX                                             ║
║  ════════════════════════════════════════════════════════════════                         ║
║  Dans le texte XAML (Content, Text, Header):                                              ║
║  ❌ INTERDIT: &  (ampersand nu)                                                          ║
║  ✅ UTILISER: &amp; ou remplacer par "et"                                                ║
║                                                                                            ║
║  EXEMPLE:                                                                                  ║
║  ❌ <TextBlock Text="Interior Wall 01 & 02"/>                                            ║
║  ✅ <TextBlock Text="Interior Wall 01 et 02"/>                                           ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝

╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║  REGLE #3: LIGNE 1 DU XAML - NE JAMAIS TOUCHER                                            ║
║  ════════════════════════════════════════════════════════════════                         ║
║  La ligne 1 DOIT rester EXACTEMENT:                                                       ║
║  <Window x:Class="XnrgyEngineeringAutomationTools.Modules.ConfigUnite.Views...            ║
║                                                                                            ║
║  NE JAMAIS modifier, supprimer ou corrompre cette ligne!                                  ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝

╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║  REGLE #4: UTILISER ParameterLists.cs POUR LES DROPDOWNS                                  ║
║  ════════════════════════════════════════════════════════════════                         ║
║  Le fichier ParameterLists.cs contient TOUTES les listes de valeurs.                      ║
║  TOUJOURS utiliser: ParameterLists.NomDeLaListe                                           ║
║  JAMAIS creer des new List<string> { ... } dans le code-behind                            ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝

╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║  REGLE #5: NOMS DE CONTROLES = PATTERN STRICT                                             ║
║  ════════════════════════════════════════════════════════════════                         ║
║  ComboBox: Cmb[Section][Field]    ex: CmbBackWallPanelWidth                               ║
║  TextBox:  Txt[Section][Field]    ex: TxtBackWall02DistBottom                             ║
║  CheckBox: Chk[Section][Field]    ex: ChkIncludeBackWall02                                ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝
```

---

**Agent**: GLM-4.7 via Cline  
**Difficulte**: 5/5 (CRITIQUE - Noms iLogic EXACTS)  
**Temps estime**: 60-90 minutes  
**Validation par**: GitHub Copilot

---

## FICHIERS DE REFERENCE (LECTURE OBLIGATOIRE)

| Fichier | Chemin | Usage |
|---------|--------|-------|
| **CONFIG_UNITE_MASTER.md** | `Modules/ConfigUnite/DevDocs/CONFIG_UNITE_MASTER.md` | Reference MASTER - Tous les noms iLogic |
| **000000000-params.xml** | `Modules/ConfigUnite/DevDocs/Validations/000000000-params.xml` | Source VERITE des parametres iLogic |
| **ParameterLists.cs** | `Modules/ConfigUnite/Models/ParameterLists.cs` | Listes de valeurs pour dropdowns |
| **ConfigUniteWindow.xaml** | `Modules/ConfigUnite/Views/ConfigUniteWindow.xaml` | UI a modifier |
| **ConfigUniteWindow.xaml.cs** | `Modules/ConfigUnite/Views/ConfigUniteWindow.xaml.cs` | Code-behind a modifier |

---

## FICHIERS A MODIFIER

| Fichier | Action |
|---------|--------|
| `Modules/ConfigUnite/Views/ConfigUniteWindow.xaml` | AJOUTER les controles dans le TabItem "Wall Specification" |
| `Modules/ConfigUnite/Views/ConfigUniteWindow.xaml.cs` | DECOMMENTER et adapter les initialisations (lignes 251-289) |
| `Modules/ConfigUnite/Models/ParameterLists.cs` | Verifier/ajouter les listes manquantes |

---

## OBJECTIF

Implementer l'onglet **Wall Specification** avec:
1. Exterior Walls (Back, Front, Right, Left, Roof) - Panel Width + Material
2. Interior Walls 01-02 (Parallel to Right/Left)
3. Additional Walls 02-05 (Back, Front, Right, Left)
4. Tunnels (Right, Left, Middle) - placeholder pour futur

---

## STRUCTURE iLogic COMPLETE (depuis 000000000-params.xml)

```
Wall Specification
|
+-- fx: Customize Walls (Back-01/Front-01/Right-01/Left-01)
|       Inventor Name: CustomizeWalls_Form
|
+-- Customize Wall Options (Show Customize Wall Form)
    |
    +-- Interior Walls
    |   |
    |   +-- Parallel To Right / Left
    |   |   |
    |   |   +-- Interior Wall 01
    |   |   |   +-- fx: Include Interior Wall 01          -> Include_First_Internal_Wall_Form
    |   |   |   +-- fx: Interior Wall 01 Position         -> First_Internal_Wall_Position_Form
    |   |   |   +-- fx: Customize Interior Wall Construction -> Customize_First_Internal_WallPanelConstruction_Form
    |   |   |   +-- Interior Wall Construction (Group)
    |   |   |   |   +-- fx: Interior Wall Panel Insulation    -> First_Internal_WallPanel_Insulation_Form
    |   |   |   |   +-- fx: Interior Wall Panel Construction  -> First_Internal_WallPanel_Construction_Form
    |   |   |   |   +-- fx: Interior Wall Width               -> First_Internal_WallPanel_Panel_Width_Form
    |   |   |   |   +-- fx: Interior Wall Static Pressure     -> First_Internal_Wall_StaticPressure_Form
    |   |   |   +-- fx: Customize Interior Wall Panel Material -> Customize_First_Internal_WallPanelMaterial_Form
    |   |   |   +-- Interior Wall Material (Group)
    |   |   |       +-- fx: Interior Wall Panel Material      -> First_Internal_WallPanelSMStyleName_Form
    |   |   |       +-- fx: Interior Wall Liner Material      -> First_Internal_WallLinerSMStyleName_Form
    |   |   |
    |   |   +-- Interior Wall 02
    |   |       +-- fx: Include Interior Wall 02          -> Include_Second_Internal_Wall_Form
    |   |       +-- fx: Interior Wall 02 Position         -> Second_Internal_Wall_Position_Form
    |   |       +-- fx: Customize Interior Wall Construction -> Customize_Second_Internal_WallPanelConstruction_Form
    |   |       +-- Interior Wall Construction (Group)
    |   |       |   +-- fx: Interior Wall Panel Insulation    -> Second_Internal_WallPanel_Insulation_Form
    |   |       |   +-- fx: Interior Wall Panel Construction  -> Second_Internal_WallPanel_Construction_Form
    |   |       |   +-- fx: Interior Wall Width               -> Second_Internal_WallPanel_Panel_Width_Form
    |   |       |   +-- fx: Interior Wall Static Pressure     -> Second_Internal_Wall_StaticPressure_Form
    |   |       +-- fx: Customize Interior Wall Panel Material -> Customize_Second_Internal_WallPanelMaterial_Form
    |   |       +-- Interior Wall Mtl (Group)
    |   |           +-- fx: Interior Wall Panel Material      -> Second_Internal_WallPanelSMStyleName_Form
    |   |           +-- fx: Interior Wall Liner Material      -> Second_Internal_WallLinerSMStyleName_Form
    |   |
    |   +-- Parallel To Front / Back
    |       |
    |       +-- Right Tunnel
    |       |   +-- Interior Wall 03 (meme structure que 01/02)
    |       |   +-- Interior Wall 04
    |       |
    |       +-- Left Tunnel
    |       |   +-- Interior Wall 05
    |       |   +-- Interior Wall 06
    |       |
    |       +-- Middle Tunnel
    |           +-- Interior Wall 07
    |           +-- Interior Wall 08
    |
    +-- Additional Wall / Roof (02 to 05)
        |
        +-- Back
        |   +-- Back Wall 02
        |   |   +-- fx: Include Back Wall 02    -> Include_Back_Wall_02_Form
        |   |   +-- fx: Dist_To_Bottom          -> Back_Wall_02_Bottom_Position_Form
        |   |   +-- fx: Dist_To_Left            -> Back_Wall_02_Left_Position_Form
        |   |   +-- fx: Dist_To_Back            -> Back_Wall_02_Out_Position_Form
        |   +-- Back Wall 03
        |   |   +-- fx: Include Back Wall 03    -> Include_Back_Wall_03_Form
        |   |   +-- fx: Dist_To_Bottom          -> Back_Wall_03_Bottom_Position_Form
        |   |   +-- fx: Dist_To_Left            -> Back_Wall_03_Left_Position_Form
        |   |   +-- fx: Dist_To_Back            -> Back_Wall_03_Out_Position_Form
        |   +-- Back Wall 04
        |   |   +-- fx: Include Back Wall 04    -> Include_Back_Wall_04_Form
        |   |   +-- fx: Dist_To_Bottom          -> Back_Wall_04_Bottom_Position_Form
        |   |   +-- fx: Dist_To_Left            -> Back_Wall_04_Left_Position_Form
        |   |   +-- fx: Dist_To_Back            -> Back_Wall_04_Out_Position_Form
        |   +-- Back Wall 05
        |       +-- fx: Include Back Wall 05    -> Include_Back_Wall_05_Form
        |       +-- fx: Dist_To_Bottom          -> Back_Wall_05_Bottom_Position_Form
        |       +-- fx: Dist_To_Left            -> Back_Wall_05_Left_Position_Form
        |       +-- fx: Dist_To_Back            -> Back_Wall_05_Out_Position_Form
        |
        +-- Front
        |   +-- Front Wall 02
        |   |   +-- fx: Include Front Wall 02   -> Include_Front_Wall_02_Form
        |   |   +-- fx: Dist_To_Bottom          -> Front_Wall_02_Bottom_Position_Form
        |   |   +-- fx: Dist_To_Right           -> Front_Wall_02_Left_Position_Form
        |   |   +-- fx: Dist_To_Front           -> Front_Wall_02_Out_Position_Form
        |   +-- Front Wall 03
        |   |   +-- fx: Include Front Wall 03   -> Include_Front_Wall_03_Form
        |   |   +-- fx: Dist_To_Bottom          -> Front_Wall_03_Bottom_Position_Form
        |   |   +-- fx: Dist_To_Right           -> Front_Wall_03_Left_Position_Form
        |   |   +-- fx: Dist_To_Front           -> Front_Wall_03_Out_Position_Form
        |   +-- Front Wall 04
        |   |   +-- fx: Include Front Wall 04   -> Include_Front_Wall_04_Form
        |   |   +-- fx: Dist_To_Bottom          -> Front_Wall_04_Bottom_Position_Form
        |   |   +-- fx: Dist_To_Right           -> Front_Wall_04_Left_Position_Form
        |   |   +-- fx: Dist_To_Front           -> Front_Wall_04_Out_Position_Form
        |   +-- Front Wall 05
        |       +-- fx: Include Front Wall 05   -> Include_Front_Wall_05_Form
        |       +-- fx: Dist_To_Bottom          -> Front_Wall_05_Bottom_Position_Form
        |       +-- fx: Dist_To_Right           -> Front_Wall_05_Left_Position_Form
        |       +-- fx: Dist_To_Front           -> Front_Wall_05_Out_Position_Form
        |
        +-- Right
        |   +-- Right Wall 02
        |   |   +-- fx: Include Right Wall 02   -> Include_Right_Wall_02_Form
        |   |   +-- fx: Dist_To_Bottom          -> Right_Wall_02_Bottom_Position_Form
        |   |   +-- fx: Dist_To_Back            -> Right_Wall_02_Left_Position_Form
        |   |   +-- fx: Dist_To_Right           -> Right_Wall_02_Out_Position_Form
        |   +-- Right Wall 03
        |   |   +-- fx: Include Right Wall 03   -> Include_Right_Wall_03_Form
        |   |   +-- fx: Dist_To_Bottom          -> Right_Wall_03_Bottom_Position_Form
        |   |   +-- fx: Dist_To_Back            -> Right_Wall_03_Left_Position_Form
        |   |   +-- fx: Dist_To_Right           -> Right_Wall_03_Out_Position_Form
        |   +-- Right Wall 04
        |   |   +-- fx: Include Right Wall 04   -> Include_Right_Wall_04_Form
        |   |   +-- fx: Dist_To_Bottom          -> Right_Wall_04_Bottom_Position_Form
        |   |   +-- fx: Dist_To_Back            -> Right_Wall_04_Left_Position_Form
        |   |   +-- fx: Dist_To_Right           -> Right_Wall_04_Out_Position_Form
        |   +-- Right Wall 05
        |       +-- fx: Include Right Wall 05   -> Include_Right_Wall_05_Form
        |       +-- fx: Dist_To_Bottom          -> Right_Wall_05_Bottom_Position_Form
        |       +-- fx: Dist_To_Back            -> Right_Wall_05_Left_Position_Form
        |       +-- fx: Dist_To_Right           -> Right_Wall_05_Out_Position_Form
        |
        +-- Left
        |   +-- Left Wall 02
        |   |   +-- fx: Include Left Wall 02    -> Include_Left_Wall_02_Form
        |   |   +-- fx: Dist_To_Bottom          -> Left_Wall_02_Bottom_Position_Form
        |   |   +-- fx: Dist_To_Front           -> Left_Wall_02_Left_Position_Form
        |   |   +-- fx: Dist_To_Left            -> Left_Wall_02_Out_Position_Form
        |   +-- Left Wall 03
        |   |   +-- fx: Include Left Wall 03    -> Include_Left_Wall_03_Form
        |   |   +-- fx: Dist_To_Bottom          -> Left_Wall_03_Bottom_Position_Form
        |   |   +-- fx: Dist_To_Front           -> Left_Wall_03_Left_Position_Form
        |   |   +-- fx: Dist_To_Left            -> Left_Wall_03_Out_Position_Form
        |   +-- Left Wall 04
        |   |   +-- fx: Include Left Wall 04    -> Include_Left_Wall_04_Form
        |   |   +-- fx: Dist_To_Bottom          -> Left_Wall_04_Bottom_Position_Form
        |   |   +-- fx: Dist_To_Front           -> Left_Wall_04_Left_Position_Form
        |   |   +-- fx: Dist_To_Left            -> Left_Wall_04_Out_Position_Form
        |   +-- Left Wall 05
        |       +-- fx: Include Left Wall 05    -> Include_Left_Wall_05_Form
        |       +-- fx: Dist_To_Bottom          -> Left_Wall_05_Bottom_Position_Form
        |       +-- fx: Dist_To_Front           -> Left_Wall_05_Left_Position_Form
        |       +-- fx: Dist_To_Left            -> Left_Wall_05_Out_Position_Form
        |
        +-- Roof
            +-- Roof 02
            |   +-- fx: Include Roof 02         -> Include_Roof_02_Form
            |   +-- fx: Dist_To_Back            -> Roof_02_Back_Position_Form
            |   +-- fx: Dist_To_Left            -> Roof_02_Left_Position_Form
            |   +-- fx: Dist_To_Top             -> Roof_02_Out_Position_Form
            +-- Roof 03
            |   +-- fx: Include Roof 03         -> Include_Roof_03_Form
            |   +-- fx: Dist_To_Back            -> Roof_03_Back_Position_Form
            |   +-- fx: Dist_To_Left            -> Roof_03_Left_Position_Form
            |   +-- fx: Dist_To_Top             -> Roof_03_Out_Position_Form
            +-- Roof 04
            |   +-- fx: Include Roof 04         -> Include_Roof_04_Form
            |   +-- fx: Dist_To_Back            -> Roof_04_Back_Position_Form
            |   +-- fx: Dist_To_Left            -> Roof_04_Left_Position_Form
            |   +-- fx: Dist_To_Top             -> Roof_04_Out_Position_Form
            +-- Roof 05
                +-- fx: Include Roof 05         -> Include_Roof_05_Form
                +-- fx: Dist_To_Back            -> Roof_05_Back_Position_Form
                +-- fx: Dist_To_Left            -> Roof_05_Left_Position_Form
                +-- fx: Dist_To_Top             -> Roof_05_Out_Position_Form
```

---

## DATAMODEL AVEC NOMS iLOGIC (ConfigUniteDataModel.cs)

SUPPRIMER les anciennes classes Wall et AJOUTER:

```csharp
// ============================================
// SECTION: WALL SPECIFICATION (iLogic Parameters)
// ============================================

/// <summary>
/// Configuration complete des murs - Noms iLogic EXACTS
/// </summary>
public class WallSpecificationConfig
{
    // Inventor Name: CustomizeWalls_Form
    public bool CustomizeWalls { get; set; } = false;
    
    public InteriorWallsConfig InteriorWalls { get; set; } = new InteriorWallsConfig();
    public AdditionalWallRoofConfig AdditionalWallRoof { get; set; } = new AdditionalWallRoofConfig();
}

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
    
    public DirectionWallsConfig() { _direction = "Back"; }
    public DirectionWallsConfig(string direction) { _direction = direction; }
    
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
```

---

## TABLE DE REFERENCE DES NOMS iLOGIC

### Interior Walls (Parallel To Right/Left)

| UI Label | Inventor Parameter Name |
|----------|------------------------|
| Include Interior Wall 01 | `Include_First_Internal_Wall_Form` |
| Interior Wall 01 Position | `First_Internal_Wall_Position_Form` |
| Customize Interior Wall Construction | `Customize_First_Internal_WallPanelConstruction_Form` |
| Interior Wall Panel Insulation | `First_Internal_WallPanel_Insulation_Form` |
| Interior Wall Panel Construction | `First_Internal_WallPanel_Construction_Form` |
| Interior Wall Width | `First_Internal_WallPanel_Panel_Width_Form` |
| Interior Wall Static Pressure | `First_Internal_Wall_StaticPressure_Form` |
| Customize Interior Wall Panel Material | `Customize_First_Internal_WallPanelMaterial_Form` |
| Interior Wall Panel Material | `First_Internal_WallPanelSMStyleName_Form` |
| Interior Wall Liner Material | `First_Internal_WallLinerSMStyleName_Form` |
| Include Interior Wall 02 | `Include_Second_Internal_Wall_Form` |
| (meme pattern avec "Second" au lieu de "First") | |

### Additional Walls (Back, Front, Right, Left)

| UI Label | Inventor Parameter Name Pattern |
|----------|--------------------------------|
| Include [Dir] Wall [XX] | `Include_[Dir]_Wall_[XX]_Form` |
| Dist_To_Bottom | `[Dir]_Wall_[XX]_Bottom_Position_Form` |
| Dist_To_Left/Right/Front | `[Dir]_Wall_[XX]_Left_Position_Form` |
| Dist_To_Back/Front/Right/Left | `[Dir]_Wall_[XX]_Out_Position_Form` |

Exemples:
- `Include_Back_Wall_02_Form`
- `Back_Wall_02_Bottom_Position_Form`
- `Back_Wall_02_Left_Position_Form`
- `Back_Wall_02_Out_Position_Form`

### Roof

| UI Label | Inventor Parameter Name |
|----------|------------------------|
| Include Roof 02 | `Include_Roof_02_Form` |
| Dist_To_Back | `Roof_02_Back_Position_Form` |
| Dist_To_Left | `Roof_02_Left_Position_Form` |
| Dist_To_Top | `Roof_02_Out_Position_Form` |

---

## REGLES CRITIQUES

1. **NOMS iLOGIC EXACTS** - Copier exactement les noms depuis la table
2. **NE PAS UTILISER d'emojis dans le code C# (Logger, Console)**
3. **Utiliser les marqueurs ASCII**: [+] [-] [!] [>] [i]
4. **Build command**: `.\build-and-run.ps1 -BuildOnly`

---

## IMPLEMENTATION XAML DETAILLEE

### ETAPE 1: Localiser le TabItem "Wall Specification"

Le TabItem existe deja dans ConfigUniteWindow.xaml (autour des lignes 600-1150).
Il faut REMPLACER son contenu interne.

### ETAPE 2: Noms de controles XAML a creer (OBLIGATOIRE)

**Exterior Walls - CREER CES ComboBox DANS XAML:**
```
CmbBackWallPanelWidth
CmbFrontWallPanelWidth
CmbRightWallPanelWidth
CmbLeftWallPanelWidth
CmbRoofPanelWidth
CmbBackWallLinerMaterial
CmbFrontWallLinerMaterial
CmbRightWallLinerMaterial
CmbLeftWallLinerMaterial
CmbRoofLinerMaterial
```

**Interior Wall 01 - CREER CES controles DANS XAML:**
```
ChkIncludeInteriorWall01          (CheckBox)
TxtInteriorWall01Position         (TextBox)
ChkCustomizeInteriorWall01Construction (CheckBox)
CmbInteriorWall01PanelInsulation  (ComboBox)
CmbInteriorWall01PanelConstruction (ComboBox)
CmbInteriorWall01PanelWidth       (ComboBox)
CmbInteriorWall01StaticPressure   (ComboBox)
ChkCustomizeInteriorWall01Material (CheckBox)
CmbInteriorWall01PanelMaterial    (ComboBox)
CmbInteriorWall01LinerMaterial    (ComboBox)
```

**Interior Wall 02 - meme pattern avec "02"**

### ETAPE 3: Code-behind APRES creation XAML

SEULEMENT apres avoir cree les controles dans XAML, ajouter dans ConfigUniteWindow.xaml.cs:

```csharp
// Exterior Walls
CmbBackWallPanelWidth.ItemsSource = ParameterLists.PanelWidth;
CmbFrontWallPanelWidth.ItemsSource = ParameterLists.PanelWidth;
// ... etc
```

### ETAPE 4: Verifier ParameterLists.cs

S'assurer que ces listes existent:
- `ParameterLists.PanelWidth`
- `ParameterLists.PanelInsulation`
- `ParameterLists.PanelConstruction`
- `ParameterLists.WallLinerMaterial`
- `ParameterLists.WallPanelMaterial`
- `ParameterLists.StaticPressure`

---

## VALIDATION FINALE

```powershell
cd XnrgyEngineeringAutomationTools
.\build-and-run.ps1 -BuildOnly
```

**Criteres de succes:**
- [ ] Build reussit sans erreur CS0103
- [ ] Tous les x:Name dans XAML AVANT reference en C#
- [ ] Aucun caractere `&` nu dans XAML
- [ ] Ligne 1 XAML intacte

---

## COMMANDE POUR CLINE

```
/speckit.implement STEP_04
```

---

# ══════════════════════════════════════════════════════════════════════════════
# SECTION TRAVAIL RESTANT - SUITE DE L'IMPLEMENTATION
# ══════════════════════════════════════════════════════════════════════════════

## ETAT ACTUEL - CE QUI EST DEJA FAIT (NE PAS TOUCHER)

```
╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║  DEJA IMPLEMENTE - NE PAS MODIFIER:                                                       ║
║                                                                                            ║
║  ✅ Interior Wall 01 (First)    - 10 controles XAML + Bindings + ItemsSource             ║
║  ✅ Interior Wall 02 (Second)   - 10 controles XAML + Bindings + ItemsSource             ║
║  ✅ Back Wall 02                - 4 controles XAML + Bindings                            ║
║  ✅ Front Wall 02               - 4 controles XAML + Bindings                            ║
║  ✅ Roof 02                     - 4 controles XAML + Bindings                            ║
║  ✅ ParameterLists.cs           - Toutes les listes necessaires                          ║
║  ✅ Code-behind ItemsSource     - Configure pour Interior Walls 01/02                    ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝
```

---

## CORRECTION #1: UTILISER ParameterLists AU LIEU DE LISTE INLINE

**Fichier**: `ConfigUniteWindow.xaml.cs`
**Localisation**: Autour de la ligne 255

**CODE ACTUEL A SUPPRIMER:**
```csharp
var panelConstructionOptions = new List<string> { "None/Aucun", "Single Wall", "Double Wall", "Triple Wall" };
```

**REMPLACER PAR:**
```csharp
// Utiliser ParameterLists.PanelConstruction au lieu de liste inline
```

**ET MODIFIER LES LIGNES SUIVANTES:**
```csharp
// AVANT:
CmbInteriorWall01PanelConstruction.ItemsSource = panelConstructionOptions;
CmbInteriorWall02PanelConstruction.ItemsSource = panelConstructionOptions;

// APRES:
CmbInteriorWall01PanelConstruction.ItemsSource = ParameterLists.PanelConstruction;
CmbInteriorWall02PanelConstruction.ItemsSource = ParameterLists.PanelConstruction;
```

---

## CORRECTION #2: AJOUTER Right Wall 02 et Left Wall 02

**Fichier**: `ConfigUniteWindow.xaml`
**Localisation**: Tabs "Right" et "Left" dans Additional Wall 02 (lignes ~1240-1270)

### Right Wall 02 - REMPLACER LE PLACEHOLDER

**TROUVER CE BLOC:**
```xml
<!-- Right Wall 02 -->
<TabItem Header="Right" Style="{StaticResource SubTabItem}">
    <GroupBox Style="{StaticResource ModernGroupBox}" Margin="5">
        <GroupBox.Header>
            <TextBlock Text="Right Wall 02" FontWeight="Bold" Foreground="White"/>
        </GroupBox.Header>
        <TextBlock Text="Structure identique a Back Wall 02 (Include + 3 positions)" 
                   Foreground="#888" FontStyle="Italic" Margin="5"/>
    </GroupBox>
</TabItem>
```

**REMPLACER PAR:**
```xml
<!-- Right Wall 02 -->
<TabItem Header="Right" Style="{StaticResource SubTabItem}">
    <GroupBox Style="{StaticResource ModernGroupBox}" Margin="5">
        <GroupBox.Header>
            <TextBlock Text="Right Wall 02" FontWeight="Bold" Foreground="White"/>
        </GroupBox.Header>
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="280"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            
            <CheckBox Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" 
                      x:Name="ChkRightWall02Include" 
                      Content="Include Right Wall 02 (Include_Right_Wall_02_Form)" 
                      Style="{StaticResource ModernCheckBox}"
                      IsChecked="{Binding WallSpecification.AdditionalWallRoof.Right.Wall02.Include}"/>
            
            <TextBlock Grid.Row="1" Grid.Column="0" Text="Bottom Position (Right_Wall_02_Bottom_Position_Form):" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
            <TextBox Grid.Row="1" Grid.Column="1" x:Name="TxtRightWall02BottomPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                     Text="{Binding WallSpecification.AdditionalWallRoof.Right.Wall02.DistToBottom, UpdateSourceTrigger=PropertyChanged}"/>
            
            <TextBlock Grid.Row="2" Grid.Column="0" Text="Left Position (Right_Wall_02_Left_Position_Form):" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
            <TextBox Grid.Row="2" Grid.Column="1" x:Name="TxtRightWall02LeftPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                     Text="{Binding WallSpecification.AdditionalWallRoof.Right.Wall02.DistToLeft, UpdateSourceTrigger=PropertyChanged}"/>
            
            <TextBlock Grid.Row="3" Grid.Column="0" Text="Out Position (Right_Wall_02_Out_Position_Form):" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
            <TextBox Grid.Row="3" Grid.Column="1" x:Name="TxtRightWall02OutPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                     Text="{Binding WallSpecification.AdditionalWallRoof.Right.Wall02.DistToOut, UpdateSourceTrigger=PropertyChanged}"/>
        </Grid>
    </GroupBox>
</TabItem>
```

### Left Wall 02 - REMPLACER LE PLACEHOLDER

**TROUVER CE BLOC:**
```xml
<!-- Left Wall 02 -->
<TabItem Header="Left" Style="{StaticResource SubTabItem}">
    <GroupBox Style="{StaticResource ModernGroupBox}" Margin="5">
        <GroupBox.Header>
            <TextBlock Text="Left Wall 02" FontWeight="Bold" Foreground="White"/>
        </GroupBox.Header>
        <TextBlock Text="Structure identique a Back Wall 02 (Include + 3 positions)" 
                   Foreground="#888" FontStyle="Italic" Margin="5"/>
    </GroupBox>
</TabItem>
```

**REMPLACER PAR:**
```xml
<!-- Left Wall 02 -->
<TabItem Header="Left" Style="{StaticResource SubTabItem}">
    <GroupBox Style="{StaticResource ModernGroupBox}" Margin="5">
        <GroupBox.Header>
            <TextBlock Text="Left Wall 02" FontWeight="Bold" Foreground="White"/>
        </GroupBox.Header>
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="280"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            
            <CheckBox Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" 
                      x:Name="ChkLeftWall02Include" 
                      Content="Include Left Wall 02 (Include_Left_Wall_02_Form)" 
                      Style="{StaticResource ModernCheckBox}"
                      IsChecked="{Binding WallSpecification.AdditionalWallRoof.Left.Wall02.Include}"/>
            
            <TextBlock Grid.Row="1" Grid.Column="0" Text="Bottom Position (Left_Wall_02_Bottom_Position_Form):" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
            <TextBox Grid.Row="1" Grid.Column="1" x:Name="TxtLeftWall02BottomPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                     Text="{Binding WallSpecification.AdditionalWallRoof.Left.Wall02.DistToBottom, UpdateSourceTrigger=PropertyChanged}"/>
            
            <TextBlock Grid.Row="2" Grid.Column="0" Text="Front Position (Left_Wall_02_Left_Position_Form):" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
            <TextBox Grid.Row="2" Grid.Column="1" x:Name="TxtLeftWall02FrontPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                     Text="{Binding WallSpecification.AdditionalWallRoof.Left.Wall02.DistToLeft, UpdateSourceTrigger=PropertyChanged}"/>
            
            <TextBlock Grid.Row="3" Grid.Column="0" Text="Out Position (Left_Wall_02_Out_Position_Form):" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
            <TextBox Grid.Row="3" Grid.Column="1" x:Name="TxtLeftWall02OutPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                     Text="{Binding WallSpecification.AdditionalWallRoof.Left.Wall02.DistToOut, UpdateSourceTrigger=PropertyChanged}"/>
        </Grid>
    </GroupBox>
</TabItem>
```

---

## CORRECTION #3: IMPLEMENTER Additional Wall 03

**Fichier**: `ConfigUniteWindow.xaml`
**Localisation**: TabItem "Additional Wall 03" (ligne ~1298)

**TROUVER:**
```xml
<TabItem Header="Additional Wall 03" Style="{StaticResource SubTabItem}">
    <TextBlock Text="Structure identique a Additional Wall 02 (Back/Front/Right/Left/Roof)" 
               Foreground="#888" FontStyle="Italic" Margin="20"/>
</TabItem>
```

**REMPLACER PAR LA STRUCTURE COMPLETE (copier le pattern de Additional Wall 02):**

```xml
<TabItem Header="Additional Wall 03" Style="{StaticResource SubTabItem}">
    <TabControl Style="{StaticResource SubTabControl}" Margin="5">
        
        <!-- Back Wall 03 -->
        <TabItem Header="Back" Style="{StaticResource SubTabItem}">
            <GroupBox Style="{StaticResource ModernGroupBox}" Margin="5">
                <GroupBox.Header>
                    <TextBlock Text="Back Wall 03" FontWeight="Bold" Foreground="White"/>
                </GroupBox.Header>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="280"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    
                    <CheckBox Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" 
                              x:Name="ChkBackWall03Include" 
                              Content="Include Back Wall 03 (Include_Back_Wall_03_Form)" 
                              Style="{StaticResource ModernCheckBox}"
                              IsChecked="{Binding WallSpecification.AdditionalWallRoof.Back.Wall03.Include}"/>
                    
                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Bottom Position:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="1" Grid.Column="1" x:Name="TxtBackWall03BottomPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                             Text="{Binding WallSpecification.AdditionalWallRoof.Back.Wall03.DistToBottom, UpdateSourceTrigger=PropertyChanged}"/>
                    
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="Left Position:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="2" Grid.Column="1" x:Name="TxtBackWall03LeftPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                             Text="{Binding WallSpecification.AdditionalWallRoof.Back.Wall03.DistToLeft, UpdateSourceTrigger=PropertyChanged}"/>
                    
                    <TextBlock Grid.Row="3" Grid.Column="0" Text="Out Position:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="3" Grid.Column="1" x:Name="TxtBackWall03OutPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                             Text="{Binding WallSpecification.AdditionalWallRoof.Back.Wall03.DistToOut, UpdateSourceTrigger=PropertyChanged}"/>
                </Grid>
            </GroupBox>
        </TabItem>
        
        <!-- Front Wall 03 -->
        <TabItem Header="Front" Style="{StaticResource SubTabItem}">
            <GroupBox Style="{StaticResource ModernGroupBox}" Margin="5">
                <GroupBox.Header>
                    <TextBlock Text="Front Wall 03" FontWeight="Bold" Foreground="White"/>
                </GroupBox.Header>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="280"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    
                    <CheckBox Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" 
                              x:Name="ChkFrontWall03Include" 
                              Content="Include Front Wall 03 (Include_Front_Wall_03_Form)" 
                              Style="{StaticResource ModernCheckBox}"
                              IsChecked="{Binding WallSpecification.AdditionalWallRoof.Front.Wall03.Include}"/>
                    
                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Bottom Position:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="1" Grid.Column="1" x:Name="TxtFrontWall03BottomPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                             Text="{Binding WallSpecification.AdditionalWallRoof.Front.Wall03.DistToBottom, UpdateSourceTrigger=PropertyChanged}"/>
                    
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="Left Position:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="2" Grid.Column="1" x:Name="TxtFrontWall03LeftPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                             Text="{Binding WallSpecification.AdditionalWallRoof.Front.Wall03.DistToLeft, UpdateSourceTrigger=PropertyChanged}"/>
                    
                    <TextBlock Grid.Row="3" Grid.Column="0" Text="Out Position:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="3" Grid.Column="1" x:Name="TxtFrontWall03OutPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                             Text="{Binding WallSpecification.AdditionalWallRoof.Front.Wall03.DistToOut, UpdateSourceTrigger=PropertyChanged}"/>
                </Grid>
            </GroupBox>
        </TabItem>
        
        <!-- Right Wall 03 -->
        <TabItem Header="Right" Style="{StaticResource SubTabItem}">
            <GroupBox Style="{StaticResource ModernGroupBox}" Margin="5">
                <GroupBox.Header>
                    <TextBlock Text="Right Wall 03" FontWeight="Bold" Foreground="White"/>
                </GroupBox.Header>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="280"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    
                    <CheckBox Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" 
                              x:Name="ChkRightWall03Include" 
                              Content="Include Right Wall 03 (Include_Right_Wall_03_Form)" 
                              Style="{StaticResource ModernCheckBox}"
                              IsChecked="{Binding WallSpecification.AdditionalWallRoof.Right.Wall03.Include}"/>
                    
                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Bottom Position:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="1" Grid.Column="1" x:Name="TxtRightWall03BottomPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                             Text="{Binding WallSpecification.AdditionalWallRoof.Right.Wall03.DistToBottom, UpdateSourceTrigger=PropertyChanged}"/>
                    
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="Left Position:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="2" Grid.Column="1" x:Name="TxtRightWall03LeftPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                             Text="{Binding WallSpecification.AdditionalWallRoof.Right.Wall03.DistToLeft, UpdateSourceTrigger=PropertyChanged}"/>
                    
                    <TextBlock Grid.Row="3" Grid.Column="0" Text="Out Position:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="3" Grid.Column="1" x:Name="TxtRightWall03OutPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                             Text="{Binding WallSpecification.AdditionalWallRoof.Right.Wall03.DistToOut, UpdateSourceTrigger=PropertyChanged}"/>
                </Grid>
            </GroupBox>
        </TabItem>
        
        <!-- Left Wall 03 -->
        <TabItem Header="Left" Style="{StaticResource SubTabItem}">
            <GroupBox Style="{StaticResource ModernGroupBox}" Margin="5">
                <GroupBox.Header>
                    <TextBlock Text="Left Wall 03" FontWeight="Bold" Foreground="White"/>
                </GroupBox.Header>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="280"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    
                    <CheckBox Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" 
                              x:Name="ChkLeftWall03Include" 
                              Content="Include Left Wall 03 (Include_Left_Wall_03_Form)" 
                              Style="{StaticResource ModernCheckBox}"
                              IsChecked="{Binding WallSpecification.AdditionalWallRoof.Left.Wall03.Include}"/>
                    
                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Bottom Position:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="1" Grid.Column="1" x:Name="TxtLeftWall03BottomPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                             Text="{Binding WallSpecification.AdditionalWallRoof.Left.Wall03.DistToBottom, UpdateSourceTrigger=PropertyChanged}"/>
                    
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="Front Position:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="2" Grid.Column="1" x:Name="TxtLeftWall03FrontPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                             Text="{Binding WallSpecification.AdditionalWallRoof.Left.Wall03.DistToLeft, UpdateSourceTrigger=PropertyChanged}"/>
                    
                    <TextBlock Grid.Row="3" Grid.Column="0" Text="Out Position:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="3" Grid.Column="1" x:Name="TxtLeftWall03OutPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                             Text="{Binding WallSpecification.AdditionalWallRoof.Left.Wall03.DistToOut, UpdateSourceTrigger=PropertyChanged}"/>
                </Grid>
            </GroupBox>
        </TabItem>
        
        <!-- Roof 03 -->
        <TabItem Header="Roof" Style="{StaticResource SubTabItem}">
            <GroupBox Style="{StaticResource ModernGroupBox}" Margin="5">
                <GroupBox.Header>
                    <TextBlock Text="Roof 03" FontWeight="Bold" Foreground="#FFB74D"/>
                </GroupBox.Header>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="280"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    
                    <CheckBox Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" 
                              x:Name="ChkRoof03Include" 
                              Content="Include Roof 03 (Include_Roof_03_Form)" 
                              Style="{StaticResource ModernCheckBox}"
                              IsChecked="{Binding WallSpecification.AdditionalWallRoof.Roof.Roof03.Include}"/>
                    
                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Back Position:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="1" Grid.Column="1" x:Name="TxtRoof03BackPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                             Text="{Binding WallSpecification.AdditionalWallRoof.Roof.Roof03.DistToBack, UpdateSourceTrigger=PropertyChanged}"/>
                    
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="Left Position:" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="2" Grid.Column="1" x:Name="TxtRoof03LeftPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                             Text="{Binding WallSpecification.AdditionalWallRoof.Roof.Roof03.DistToLeft, UpdateSourceTrigger=PropertyChanged}"/>
                    
                    <TextBlock Grid.Row="3" Grid.Column="0" Text="Out Position (Top):" Style="{StaticResource ModernLabel}" Margin="0,10,0,0"/>
                    <TextBox Grid.Row="3" Grid.Column="1" x:Name="TxtRoof03OutPosition" Style="{StaticResource ModernTextBox}" Margin="0,10,0,0" Width="150" HorizontalAlignment="Left"
                             Text="{Binding WallSpecification.AdditionalWallRoof.Roof.Roof03.DistToTop, UpdateSourceTrigger=PropertyChanged}"/>
                </Grid>
            </GroupBox>
        </TabItem>
        
    </TabControl>
</TabItem>
```

---

## CORRECTION #4: IMPLEMENTER Additional Wall 04

**MEME PATTERN** que Additional Wall 03, mais avec "04" au lieu de "03":
- ChkBackWall04Include, TxtBackWall04BottomPosition, etc.
- ChkFrontWall04Include, TxtFrontWall04BottomPosition, etc.
- ChkRightWall04Include, TxtRightWall04BottomPosition, etc.
- ChkLeftWall04Include, TxtLeftWall04BottomPosition, etc.
- ChkRoof04Include, TxtRoof04BackPosition, etc.

**TROUVER:**
```xml
<TabItem Header="Additional Wall 04" Style="{StaticResource SubTabItem}">
    <TextBlock Text="Structure identique a Additional Wall 02 (Back/Front/Right/Left/Roof)" 
               Foreground="#888" FontStyle="Italic" Margin="20"/>
</TabItem>
```

**REMPLACER** avec la meme structure que Wall 03 mais en changeant tous les "03" par "04".

---

## CORRECTION #5: IMPLEMENTER Additional Wall 05

**MEME PATTERN** que Additional Wall 03/04, mais avec "05":
- ChkBackWall05Include, TxtBackWall05BottomPosition, etc.
- etc.

**TROUVER:**
```xml
<TabItem Header="Additional Wall 05" Style="{StaticResource SubTabItem}">
    <TextBlock Text="Structure identique a Additional Wall 02 (Back/Front/Right/Left/Roof)" 
               Foreground="#888" FontStyle="Italic" Margin="20"/>
</TabItem>
```

**REMPLACER** avec la meme structure que Wall 03 mais en changeant tous les "03" par "05".

---

## RAPPEL DES NOMS iLOGIC POUR WALLS ADDITIONNELS

### Pattern de nommage:

| Direction | Wall XX | Include | Bottom | Left | Out |
|-----------|---------|---------|--------|------|-----|
| Back | 02-05 | `Include_Back_Wall_XX_Form` | `Back_Wall_XX_Bottom_Position_Form` | `Back_Wall_XX_Left_Position_Form` | `Back_Wall_XX_Out_Position_Form` |
| Front | 02-05 | `Include_Front_Wall_XX_Form` | `Front_Wall_XX_Bottom_Position_Form` | `Front_Wall_XX_Left_Position_Form` | `Front_Wall_XX_Out_Position_Form` |
| Right | 02-05 | `Include_Right_Wall_XX_Form` | `Right_Wall_XX_Bottom_Position_Form` | `Right_Wall_XX_Left_Position_Form` | `Right_Wall_XX_Out_Position_Form` |
| Left | 02-05 | `Include_Left_Wall_XX_Form` | `Left_Wall_XX_Bottom_Position_Form` | `Left_Wall_XX_Left_Position_Form` | `Left_Wall_XX_Out_Position_Form` |
| Roof | 02-05 | `Include_Roof_XX_Form` | `Roof_XX_Back_Position_Form` | `Roof_XX_Left_Position_Form` | `Roof_XX_Out_Position_Form` |

---

## CHECKLIST FINALE POUR GLM

```
╔═══════════════════════════════════════════════════════════════════════════════════════════╗
║  AVANT DE TERMINER - VERIFIER:                                                            ║
║                                                                                            ║
║  [ ] CORRECTION #1: panelConstructionOptions remplace par ParameterLists.PanelConstruction║
║  [ ] CORRECTION #2: Right Wall 02 et Left Wall 02 implementes                             ║
║  [ ] CORRECTION #3: Additional Wall 03 complet (5 tabs)                                   ║
║  [ ] CORRECTION #4: Additional Wall 04 complet (5 tabs)                                   ║
║  [ ] CORRECTION #5: Additional Wall 05 complet (5 tabs)                                   ║
║                                                                                            ║
║  [ ] BUILD: .\build-and-run.ps1 -BuildOnly -> 0 erreurs                                  ║
║  [ ] Aucun caractere & non echappe dans XAML                                              ║
║  [ ] Ligne 1 du XAML intacte                                                              ║
╚═══════════════════════════════════════════════════════════════════════════════════════════╝
```

---

## NOTE SUR LES TUNNELS (OPTIONNEL - PHASE 2)

Les tunnels (Right Tunnel, Left Tunnel, Middle Tunnel) dans l'onglet "Parallel To Front/Back" peuvent rester en placeholder pour l'instant. C'est une fonctionnalite avancee qui sera implementee dans une phase ulterieure.

---

## ORDRE D'EXECUTION RECOMMANDE

1. **D'ABORD** - Correction #1 (code-behind - 1 minute)
2. **ENSUITE** - Correction #2 (Right/Left Wall 02 - 5 minutes)
3. **PUIS** - Corrections #3, #4, #5 (Walls 03-05 - 15 minutes)
4. **ENFIN** - Build et verification

**TEMPS ESTIME TOTAL**: 25-30 minutes
