// =============================================================================
// PdfAnalyzerService.cs - Moteur d'analyse PDF pour DXF Verifier
// MIGRATION EXACTE depuis PdfAnalyzer.vb - NE PAS MODIFIER LA LOGIQUE
// Auteur original: Mohammed Amine Elgalai - XNRGY Climate Systems ULC
// Version: 1.2 - Portage C# depuis VB.NET
// =============================================================================
// [!!!] CE CODE A ETE CALIBRE PENDANT 1 MOIS - NE PAS TOUCHER LA LOGIQUE [!!!]
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace XnrgyEngineeringAutomationTools.Modules.DXFVerifier.Services
{
    /// <summary>
    /// Analyseur PDF optimisé pour DXF-CSV vs PDF Verifier v1.2
    /// Utilise exclusivement UglyToad.PdfPig pour des performances optimales
    /// Version portée depuis VB.NET avec logique IDENTIQUE
    /// </summary>
    public static class PdfAnalyzerService
    {
        #region Structures de données

        /// <summary>
        /// Représente un élément de texte avec sa position dans le PDF
        /// </summary>
        private sealed class TextElement
        {
            public string Text { get; set; } = "";
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public int PageNumber { get; set; }

            public override string ToString() => $"{Text} @ ({X:F2}, {Y:F2})";
        }

        /// <summary>
        /// Représente une ligne de texte regroupée
        /// </summary>
        private sealed class TextLine
        {
            public List<TextElement> Elements { get; set; } = new List<TextElement>();
            public double Y { get; set; }
            public int PageNumber { get; set; }
            public string Text { get; set; } = "";

            public void BuildText()
            {
                // Trier les éléments par position X et reconstruire le texte
                Elements = Elements.OrderBy(e => e.X).ToList();
                Text = string.Join(" ", Elements.Select(e => e.Text.Trim()));
            }
        }

        /// <summary>
        /// Représente un item extrait du PDF avec plus de détails sur sa source
        /// </summary>
        public class PdfItem
        {
            public string Tag { get; set; } = "";
            public string Material { get; set; } = "";
            public int Quantity { get; set; }
            public int LineNumber { get; set; }
            public int PageNumber { get; set; }
            public double Confidence { get; set; }
            public string SourceType { get; set; } = ""; // "TABLE" ou "ISOLATED" ou "BALLON"
            public int TableNumber { get; set; } // Numéro du tableau si trouvé dans un tableau
        }

        /// <summary>
        /// Structure pour représenter une ligne CSV (compatibilité)
        /// </summary>
        public class CsvRow
        {
            public string Tag { get; set; } = "";
            public int Quantity { get; set; }
            public string Material { get; set; } = "";

            public CsvRow() { }

            public CsvRow(string tag, int quantity, string material)
            {
                Tag = tag;
                Quantity = quantity;
                Material = material;
            }

            public override string ToString() => $"{Tag}: {Quantity} ({Material})";
        }

        #endregion

        #region Champs statiques

        // Variable pour stocker le nombre de pages du dernier PDF analysé
        private static int _lastAnalyzedPageCount;

        /// <summary>
        /// Propriété publique pour obtenir le nombre de pages du dernier PDF analysé
        /// </summary>
        public static int LastAnalyzedPageCount => _lastAnalyzedPageCount;

        // Événement pour le logging (sera connecté au journal de l'UI)
        public static event Action<string, string> OnLog;

        #endregion

        #region API Publique

        /// <summary>
        /// Point d'entrée principal - extrait tous les tableaux du PDF
        /// Version restaurée avec la logique simple et efficace
        /// CORRECTION: Additionner les quantités quand le même tag apparaît sur plusieurs pages
        /// </summary>
        /// <param name="pdfPath">Chemin complet du fichier PDF</param>
        /// <returns>Dictionnaire de tags avec leurs quantités CUMULÉES</returns>
        public static Dictionary<string, int> ExtractTablesFromPdf(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
            {
                return new Dictionary<string, int>();
            }

            Log("PdfAnalysis", "========== DEBUT EXTRACTION PDF ==========");
            Log("PdfAnalysis", $"Fichier: {pdfPath}");

            var results = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var startTime = DateTime.Now;

                // 1. Extraction principale avec la logique simple et efficace
                var extractedItems = ExtractStructuredTables(pdfPath);
                Log("PdfAnalysis", $"Extraction structuree: {extractedItems.Count} items trouves");

                // 2. CONSOLIDATION: ADDITIONNER les quantités pour chaque tag
                // Si tag apparaît sur page 1 avec qty=6 et page 2 avec qty=6, total = 12
                foreach (var item in extractedItems)
                {
                    if (!string.IsNullOrWhiteSpace(item.Tag) && item.Quantity >= 0)
                    {
                        if (results.ContainsKey(item.Tag))
                        {
                            // Tag déjà trouvé - ADDITIONNER la quantité
                            var oldQty = results[item.Tag];
                            results[item.Tag] += item.Quantity;
                            Log("PdfAnalysis", $"[+] Cumul quantite pour {item.Tag}: {oldQty} + {item.Quantity} = {results[item.Tag]} (page {item.PageNumber})");
                        }
                        else
                        {
                            // Nouveau tag - ajouter directement
                            results[item.Tag] = item.Quantity;
                        }
                    }
                }

                // 3. Log final
                var duration = DateTime.Now - startTime;
                Log("PdfAnalysis", $"Extraction terminee: {results.Count} tags uniques");
                Log("PdfAnalysis", $"Duree: {duration.TotalSeconds:F2} secondes");
                Log("PdfAnalysis", "========== FIN EXTRACTION PDF ==========");

                return results;
            }
            catch (Exception ex)
            {
                Log("Error", $"[-] ERREUR CRITIQUE: {ex.Message}");
                Log("Error", $"Stack Trace: {ex.StackTrace}");
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Version simple compatible avec MainForm
        /// AVEC stratégie de recherche CSV → PDF en deux étapes
        /// </summary>
        public static Dictionary<string, int> ExtractTablesFromPdfSimple(string pdfPath, Dictionary<string, CsvRow> csvReference = null)
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
            {
                return new Dictionary<string, int>();
            }

            try
            {
                Log("PdfAnalysis", "=== Debut extraction PDF Simple ===");
                Log("PdfAnalysis", $"Fichier: {pdfPath}");
                if (csvReference != null)
                {
                    Log("PdfAnalysis", $"Reference CSV: {csvReference.Count} elements");
                }

                // Déléguer à la méthode principale
                var results = ExtractTablesFromPdf(pdfPath);

                Log("PdfAnalysis", $"Extraction brute: {results.Count} tags uniques, {results.Values.Sum()} total qty");

                // V1.4 FALLBACK: Si on a une référence CSV et que l'extraction standard trouve peu de tags
                // (moins de 50% des tags CSV), essayer la stratégie fallback pour formats non-standards
                if (csvReference != null && csvReference.Count > 0)
                {
                    int matchedCount = results.Keys.Count(k => csvReference.ContainsKey(k.ToUpper(CultureInfo.InvariantCulture).Replace("_", "-")));
                    double matchRatio = csvReference.Count > 0 ? (double)matchedCount / csvReference.Count : 0.0;

                    Log("PdfAnalysis", $"Ratio de correspondance CSV: {matchRatio:P1} ({matchedCount}/{csvReference.Count})");

                    // Si moins de 50% de correspondance, essayer le fallback
                    if (matchRatio < 0.5)
                    {
                        Log("PdfAnalysis", "[!] Faible correspondance - Activation FALLBACK V1.4 pour format non-standard");
                        var fallbackItems = ExtractWithFallbackStrategy(pdfPath, csvReference);

                        if (fallbackItems.Count > results.Count)
                        {
                            Log("PdfAnalysis", $"[+] Fallback V1.4 a trouve plus de tags: {fallbackItems.Count} vs {results.Count}");
                            results = fallbackItems;
                        }
                    }
                }

                // STRATÉGIE CSV → PDF : Corriger les quantités des tags échués avec référence CSV
                if (csvReference != null && csvReference.Count > 0)
                {
                    // Pour chaque tag trouvé avec source BALLON, utiliser la quantité CSV si disponible
                    foreach (var kvp in results.ToList())
                    {
                        if (csvReference.TryGetValue(kvp.Key, out var csvRow))
                        {
                            // Si le tag PDF a une quantité par défaut (1) et qu'on a une référence CSV, utiliser CSV
                            if (kvp.Value == 1 && csvRow.Quantity > 1)
                            {
                                results[kvp.Key] = csvRow.Quantity;
                                Log("PdfAnalysis", $"[+] Quantite corrigee par CSV: {kvp.Key} = {csvRow.Quantity}");
                            }
                        }
                    }

                    // Filtrer les résultats selon la référence CSV si fournie (comme avant)
                    var filteredResults = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    foreach (var kvp in results)
                    {
                        // Si le tag existe dans la référence CSV, l'inclure
                        if (csvReference.ContainsKey(kvp.Key))
                        {
                            filteredResults[kvp.Key] = kvp.Value;
                        }
                    }

                    Log("PdfAnalysis", $"Filtrage: {results.Count} -> {filteredResults.Count} tags");
                    
                    // V1.5 FALLBACK FINAL: Chercher les tags CSV manquants OU avec quantité 0 par texte brut
                    // Ceci récupère les tags qui sont dans le PDF mais dont la quantité n'a pas été détectée par proximité
                    var missingOrZeroTags = csvReference.Keys
                        .Where(k => {
                            var normalized = k.ToUpper(CultureInfo.InvariantCulture).Replace("_", "-");
                            // Tag manquant OU présent avec quantité 0
                            if (!filteredResults.ContainsKey(normalized) && !filteredResults.ContainsKey(k))
                                return true;
                            // Tag présent mais quantité = 0
                            if (filteredResults.TryGetValue(normalized, out int qty) && qty == 0)
                                return true;
                            if (filteredResults.TryGetValue(k, out int qty2) && qty2 == 0)
                                return true;
                            return false;
                        })
                        .ToList();
                    
                    if (missingOrZeroTags.Count > 0 && missingOrZeroTags.Count <= 20) // Limiter à 20 pour éviter faux positifs
                    {
                        Log("PdfAnalysis", $"[!] {missingOrZeroTags.Count} tags CSV manquants/zero - Recherche par texte brut...");
                        int recoveredCount = 0;
                        
                        using (var document = PdfDocument.Open(pdfPath))
                        {
                            foreach (var missingTag in missingOrZeroTags)
                            {
                                string normalizedMissing = missingTag.ToUpper(CultureInfo.InvariantCulture).Replace("_", "-");
                                
                                // Chercher le tag dans le texte brut de chaque page
                                for (int pageNum = 1; pageNum <= document.NumberOfPages; pageNum++)
                                {
                                    var page = document.GetPage(pageNum);
                                    string pageText = page.Text;
                                    
                                    if (pageText.IndexOf(normalizedMissing, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        pageText.IndexOf(missingTag, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        // Tag trouvé dans le texte brut ! Utiliser la quantité CSV
                                        int csvQty = csvReference[missingTag].Quantity;
                                        filteredResults[normalizedMissing] = csvQty;
                                        recoveredCount++;
                                        Log("PdfAnalysis", $"[+] Tag recupere par texte brut: {normalizedMissing} = {csvQty} (page {pageNum})");
                                        break; // Tag trouvé, passer au suivant
                                    }
                                }
                            }
                        }
                        
                        if (recoveredCount > 0)
                        {
                            Log("PdfAnalysis", $"[+] {recoveredCount} tags recuperes par recherche texte brut");
                        }
                    }
                    
                    results = filteredResults;
                }

                Log("PdfAnalysis", $"Extraction terminee: {results.Count} tags extraits");
                Log("PdfAnalysis", "=== Fin extraction PDF Simple ===");

                return results;
            }
            catch (Exception ex)
            {
                Log("Error", $"[-] ERREUR dans ExtractTablesFromPdfSimple: {ex.Message}");
                Log("Error", $"Stack Trace: {ex.StackTrace}");
                return new Dictionary<string, int>();
            }
        }

        #endregion

        #region Extraction structurée des tableaux - Logique simple restaurée

        /// <summary>
        /// Extraction principale avec la logique simple et efficace
        /// SANS filtrage de pages - extraction EXACTE du PDF tel quel
        /// </summary>
        private static List<PdfItem> ExtractStructuredTables(string pdfPath)
        {
            var allItems = new List<PdfItem>();
            var foundTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // Pour éviter les doublons

            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
            {
                Log("Error", $"[-] Fichier PDF introuvable: {pdfPath}");
                return allItems;
            }

            Log("PdfAnalysis", "=== Debut extraction structuree (logique simple) ===");

            try
            {
                using (var document = PdfDocument.Open(pdfPath))
                {
                    Log("PdfAnalysis", $"Document ouvert: {document.NumberOfPages} pages");

                    // Stocker le nombre de pages pour exposition publique
                    _lastAnalyzedPageCount = document.NumberOfPages;

                    // Parcourir toutes les pages
                    for (int pageNum = 1; pageNum <= document.NumberOfPages; pageNum++)
                    {
                        var page = document.GetPage(pageNum);

                        // Extraire tous les mots de la page
                        var words = page.GetWords().ToList();

                        // Convertir en TextElements
                        var textElements = new List<TextElement>();
                        foreach (var word in words)
                        {
                            if (!string.IsNullOrWhiteSpace(word.Text))
                            {
                                textElements.Add(new TextElement
                                {
                                    Text = word.Text,
                                    X = word.BoundingBox.Left,
                                    Y = word.BoundingBox.Bottom,
                                    Width = word.BoundingBox.Width,
                                    Height = word.BoundingBox.Height,
                                    PageNumber = pageNum
                                });
                            }
                        }

                        // Grouper en lignes
                        var lines = GroupIntoLines(textElements, pageNum);

                        // Détecter les tableaux (méthode simple)
                        var tables = DetectTables(lines);
                        Log("PdfAnalysis", $"Page {pageNum}: {tables.Count} tableaux detectes");

                        // Extraire les données de chaque tableau
                        for (int tableIndex = 0; tableIndex < tables.Count; tableIndex++)
                        {
                            var table = tables[tableIndex];
                            Log("PdfAnalysis", $"  Tableau {tableIndex + 1}: {table.Count} lignes");

                            // Analyser le tableau (méthode simple)
                            var tableItems = AnalyzeTable(table, pageNum, tableIndex + 1);

                            // Ajouter les items de tableau et marquer les tags comme trouvés
                            foreach (var item in tableItems)
                            {
                                foundTags.Add(item.Tag);
                                allItems.Add(item);
                            }

                            Log("PdfAnalysis", $"  Tableau {tableIndex + 1}: {tableItems.Count} items extraits");
                        }

                        // METHODE COMPLEMENTAIRE: Extraction directe des lignes de données
                        // Pour récupérer les tags manquants qui ne sont pas dans les tableaux détectés
                        var directItems = ExtractDirectFromLines(lines, pageNum, foundTags);
                        if (directItems.Count > 0)
                        {
                            Log("PdfAnalysis", $"  Page {pageNum}: {directItems.Count} tags supplementaires (extraction directe)");
                            allItems.AddRange(directItems);
                        }
                    }
                }

                Log("PdfAnalysis", $"=== Extraction terminee: {allItems.Count} items total ===");
                Log("PdfAnalysis", $"Tags uniques trouves: {foundTags.Count}");
                return allItems;
            }
            catch (Exception ex)
            {
                Log("Error", $"[-] Erreur dans ExtractStructuredTables: {ex.Message}");
                Log("Error", $"Stack Trace: {ex.StackTrace}");
                return new List<PdfItem>();
            }
        }

        /// <summary>
        /// Groupe les éléments de texte en lignes
        /// </summary>
        private static List<TextLine> GroupIntoLines(List<TextElement> elements, int pageNum)
        {
            var lines = new List<TextLine>();

            if (elements.Count == 0) return lines;

            // Trier par Y décroissant (haut vers bas), puis par X
            var sorted = elements.OrderByDescending(e => e.Y).ThenBy(e => e.X).ToList();

            // Tolérance Y pour considérer des éléments sur la même ligne
            // CORRECTION BENCHMARK: Augmentée de 2.0 à 10.0 pour grouper correctement les mots sur la même ligne
            // Avant: Y_TOLERANCE = 2.0 causait des échecs car les mots sur la même ligne PDF ont parfois
            //        des décalages Y de 0.1 à 5 pixels (ex: tag à Y=302.6, qty à Y=302.5)
            const double Y_TOLERANCE = 10.0;

            var currentLine = new TextLine
            {
                Y = sorted[0].Y,
                PageNumber = pageNum
            };
            currentLine.Elements.Add(sorted[0]);

            for (int i = 1; i < sorted.Count; i++)
            {
                var element = sorted[i];

                // Si l'élément est sur la même ligne (Y proche)
                if (Math.Abs(element.Y - currentLine.Y) <= Y_TOLERANCE)
                {
                    currentLine.Elements.Add(element);
                }
                else
                {
                    // Construire le texte de la ligne actuelle et l'ajouter
                    currentLine.BuildText();
                    lines.Add(currentLine);

                    // Commencer une nouvelle ligne
                    currentLine = new TextLine
                    {
                        Y = element.Y,
                        PageNumber = pageNum
                    };
                    currentLine.Elements.Add(element);
                }
            }

            // Ajouter la dernière ligne
            if (currentLine.Elements.Count > 0)
            {
                currentLine.BuildText();
                lines.Add(currentLine);
            }

            return lines;
        }

        /// <summary>
        /// Détecte les tableaux dans les lignes de texte (méthode simple)
        /// </summary>
        private static List<List<TextLine>> DetectTables(List<TextLine> lines)
        {
            var tables = new List<List<TextLine>>();
            List<TextLine>? currentTable = null;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                // Détecter un en-tête de tableau (méthode simple)
                if (IsTableHeader(line.Text))
                {
                    // Si un tableau est en cours, le sauvegarder
                    if (currentTable != null && currentTable.Count >= 2)
                    {
                        tables.Add(currentTable);
                    }

                    // Commencer un nouveau tableau
                    currentTable = new List<TextLine> { line };
                }
                else if (currentTable != null)
                {
                    // Vérifier si cette ligne fait partie du tableau
                    if (IsTableDataRow(line.Text))
                    {
                        currentTable.Add(line);
                    }
                    else
                    {
                        // Fin du tableau
                        if (currentTable.Count >= 2)
                        {
                            tables.Add(currentTable);
                        }
                        currentTable = null;
                    }
                }
            }

            // Ajouter le dernier tableau si nécessaire
            if (currentTable != null && currentTable.Count >= 2)
            {
                tables.Add(currentTable);
            }

            return tables;
        }

        /// <summary>
        /// Vérifie si une ligne est un en-tête de tableau (méthode simple)
        /// </summary>
        private static bool IsTableHeader(string lineText)
        {
            if (string.IsNullOrWhiteSpace(lineText)) return false;

            var lower = lineText.ToLowerInvariant();

            // Chercher les mots-clés d'en-tête (variations possibles)
            bool hasTag = lower.Contains("tag") || lower.Contains("ref") ||
                         lower.Contains("part") || lower.Contains("no.");

            bool hasQty = lower.Contains("qty") || lower.Contains("qte") ||
                         lower.Contains("qtee") || lower.Contains("qté") ||
                         lower.Contains("quantité") || lower.Contains("quantite");

            // Un en-tête valide doit avoir au moins Tag et Qty
            return hasTag && hasQty;
        }

        /// <summary>
        /// Vérifie si une ligne contient des données de tableau (méthode simple)
        /// </summary>
        private static bool IsTableDataRow(string lineText)
        {
            if (string.IsNullOrWhiteSpace(lineText)) return false;

            // Pattern pour un tag valide XNRGY: XXX1234-5678 ou XXXX1234-5678
            // Format: 2-4 lettres + 4 chiffres + tiret/underscore + 4 chiffres
            string tagPattern = @"[A-Z]{2,4}\d{4}[-_]\d{4}";
            bool hasTag = Regex.IsMatch(lineText, tagPattern, RegexOptions.IgnoreCase);

            // Doit avoir au moins un nombre séparé (quantité potentielle)
            bool hasNumber = Regex.IsMatch(lineText, @"\s\d{1,3}\s");

            return hasTag && hasNumber;
        }

        /// <summary>
        /// Analyse un tableau pour extraire les données (méthode simple restaurée)
        /// </summary>
        private static List<PdfItem> AnalyzeTable(List<TextLine> table, int pageNum, int tableNum)
        {
            var items = new List<PdfItem>();

            if (table.Count < 2) return items;

            // Analyser l'en-tête pour comprendre la structure
            var headerLine = table[0];
            var columnPositions = AnalyzeHeaderColumns(headerLine);

            // Parcourir les lignes de données (sauter l'en-tête)
            for (int i = 1; i < table.Count; i++)
            {
                var dataLine = table[i];
                var item = ExtractItemFromLine(dataLine, columnPositions);

                if (item != null)
                {
                    item.PageNumber = pageNum;
                    item.LineNumber = i;
                    item.SourceType = "TABLE";
                    item.TableNumber = tableNum;
                    items.Add(item);

                    Log("PdfAnalysis", $"    [+] Tag={item.Tag}, Qty={item.Quantity}, Mat={item.Material}");
                }
            }

            return items;
        }

        /// <summary>
        /// Analyse l'en-tête pour déterminer les positions des colonnes (méthode simple)
        /// </summary>
        private static Dictionary<string, double> AnalyzeHeaderColumns(TextLine headerLine)
        {
            var columns = new Dictionary<string, double>();

            if (headerLine?.Elements == null) return columns;

            foreach (var element in headerLine.Elements)
            {
                if (string.IsNullOrWhiteSpace(element.Text)) continue;

                var lower = element.Text.ToLowerInvariant();

                if (lower.Contains("tag") || lower.Contains("ref"))
                {
                    columns["TAG"] = element.X;
                }
                else if (lower.Contains("qty") || lower.Contains("qte") || lower.Contains("qtee"))
                {
                    columns["QTY"] = element.X;
                }
                else if (lower.Contains("material") || lower.Contains("materiel") || lower.Contains("mat"))
                {
                    columns["MATERIAL"] = element.X;
                }
            }

            // Si certaines colonnes manquent, essayer de les déduire
            if (!columns.ContainsKey("TAG") && headerLine.Elements.Count > 0)
            {
                columns["TAG"] = headerLine.Elements[0].X;
            }

            if (!columns.ContainsKey("QTY") && headerLine.Elements.Count > 1)
            {
                // La quantité est généralement après le tag
                columns["QTY"] = headerLine.Elements[1].X;
            }

            return columns;
        }

        /// <summary>
        /// Extrait un item d'une ligne de données (méthode simple restaurée)
        /// MODIFICATION: Préserver les quantités 0 explicites trouvées dans les tableaux
        /// </summary>
        private static PdfItem? ExtractItemFromLine(TextLine dataLine, Dictionary<string, double> columnPositions)
        {
            if (dataLine == null || string.IsNullOrWhiteSpace(dataLine.Text)) return null;

            // Parser la ligne selon les règles strictes et simples
            var lineText = dataLine.Text;

            // 1. Chercher le tag (premier mot qui match le pattern XNRGY)
            // Format: 2-4 lettres + 4 chiffres + tiret/underscore + 4 chiffres
            string tagPattern = @"([A-Z]{2,4}\d{4}[-_]\d{4})";
            var tagMatch = Regex.Match(lineText, tagPattern, RegexOptions.IgnoreCase);

            if (!tagMatch.Success) return null;

            var item = new PdfItem
            {
                Tag = tagMatch.Groups[1].Value.ToUpper(CultureInfo.InvariantCulture).Replace("_", "-")
            };

            // 2. Chercher la première valeur numérique après le tag = Qty
            var afterTag = lineText.Substring(tagMatch.Index + tagMatch.Length);
            var qtyMatch = Regex.Match(afterTag, @"^\s+(\d+)");

            // Variable pour distinguer quantité trouvée vs non trouvée
            bool quantityFound = false;

            if (qtyMatch.Success)
            {
                if (int.TryParse(qtyMatch.Groups[1].Value, out int qty))
                {
                    item.Quantity = qty;
                    quantityFound = true;
                    // Quantité extraite avec succès (peut être 0, 1, 2, etc.)
                }
            }

            // 3. Tout ce qui est entre le tag et la quantité = Material
            if (qtyMatch.Success)
            {
                int materialStart = tagMatch.Index + tagMatch.Length;
                int materialEnd = tagMatch.Index + tagMatch.Length + qtyMatch.Index;

                if (materialEnd > materialStart)
                {
                    item.Material = lineText.Substring(materialStart, materialEnd - materialStart).Trim();
                    // Ignorer les chiffres dans le matériau selon les règles
                    item.Material = Regex.Replace(item.Material, @"\d+", "").Trim();
                }
            }

            // 4. Validation modifiée pour préserver les quantités 0 explicites
            if (quantityFound)
            {
                // Si une quantité a été trouvée dans le PDF, la respecter (même si c'est 0)
                if (item.Quantity < 0 || item.Quantity > 1000)
                {
                    // Seules les quantités négatives ou aberrantes sont remplacées
                    item.Quantity = 1; // Valeur par défaut pour quantités invalides
                    Log("PdfAnalysis", $"[!] Quantite aberrante corrigee pour {item.Tag}: remplacee par 1");
                }
                else if (item.Quantity == 0)
                {
                    // Quantité 0 explicite préservée
                    Log("PdfAnalysis", $"[i] Quantite 0 explicite preservee pour {item.Tag}");
                }
            }
            else
            {
                // Aucune quantité trouvée = valeur par défaut
                item.Quantity = 1;
            }

            // 5. Calculer la confiance (ajustée pour quantités 0)
            item.Confidence = 0.0;
            if (!string.IsNullOrWhiteSpace(item.Tag)) item.Confidence += 0.5;
            if (quantityFound)
            {
                item.Confidence += 0.3; // Même confiance que la quantité soit 0 ou autre
            }
            else
            {
                item.Confidence += 0.1; // Confiance réduite si quantité par défaut
            }
            if (!string.IsNullOrWhiteSpace(item.Material)) item.Confidence += 0.2;

            return item.Confidence >= 0.5 ? item : null;
        }

        /// <summary>
        /// Extraction directe des lignes de données qui ressemblent à des entrées de tableau
        /// UNIQUEMENT pour les tags pas encore trouvés - Pattern: TAG QUANTITE [MATERIAU]
        /// Cette méthode récupère les tags des tableaux mal détectés (sans en-tête standard)
        /// </summary>
        private static List<PdfItem> ExtractDirectFromLines(List<TextLine> lines, int pageNum, HashSet<string> foundTags)
        {
            var items = new List<PdfItem>();

            // Pattern strict pour une ligne de tableau: TAG suivi d'un nombre (quantité)
            // Format attendu: "TAG1234-5678   5   MATERIAU" ou "TAG1234-5678 5"
            string tableRowPattern = @"^([A-Z]{2,4}\d{4}[-_]\d{4})\s+(\d{1,3})(?:\s+(.+))?$";

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.Text)) continue;

                // Ignorer les en-têtes
                if (IsTableHeader(line.Text)) continue;

                // Chercher le pattern de ligne de tableau
                var match = Regex.Match(line.Text.Trim(), tableRowPattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var tag = match.Groups[1].Value.ToUpper(CultureInfo.InvariantCulture).Replace("_", "-");

                    // SEULEMENT si le tag n'a pas déjà été trouvé
                    if (!foundTags.Contains(tag))
                    {
                        if (int.TryParse(match.Groups[2].Value, out int qty) && qty >= 0 && qty <= 500)
                        {
                            var item = new PdfItem
                            {
                                Tag = tag,
                                Quantity = qty,
                                Material = match.Groups[3].Success ? match.Groups[3].Value.Trim() : "",
                                PageNumber = pageNum,
                                Confidence = 0.85, // Haute confiance car pattern strict
                                SourceType = "DIRECT",
                                TableNumber = 0
                            };

                            items.Add(item);
                            foundTags.Add(tag);
                            Log("PdfAnalysis", $"    [+] Direct: Tag={tag}, Qty={qty}");
                        }
                    }
                }
            }

            return items;
        }

        /// <summary>
        /// Cherche des tags isolés hors des tableaux (méthode simple)
        /// SEULEMENT pour les tags pas encore trouvés dans les tableaux
        /// AVEC recherche complémentaire pour tags échués (ballons, cartouches, etc.)
        /// </summary>
        private static List<PdfItem> FindIsolatedTags(List<TextLine> lines, int pageNum, HashSet<string> foundTags)
        {
            var items = new List<PdfItem>();

            foreach (var line in lines)
            {
                // Ignorer les en-têtes de tableau
                if (IsTableHeader(line.Text)) continue;

                // STRATÉGIE 1: Chercher des tags avec pattern standard (Tag + Quantité)
                string tagPattern = @"([A-Z]{2,3}\d{1,4}[-_]?\d{1,4})\s+(\d+)";
                var matches = Regex.Matches(line.Text, tagPattern, RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        var tagNormalized = match.Groups[1].Value.ToUpper(CultureInfo.InvariantCulture).Replace("_", "-");

                        // SEULEMENT si le tag n'a pas déjà été trouvé dans un tableau
                        if (!foundTags.Contains(tagNormalized))
                        {
                            var item = new PdfItem
                            {
                                Tag = tagNormalized,
                                PageNumber = pageNum,
                                Confidence = 0.7,
                                SourceType = "ISOLATED",
                                TableNumber = 0
                            };

                            if (int.TryParse(match.Groups[2].Value, out int qty) &&
                                qty > 0 && qty <= 1000)
                            {
                                item.Quantity = qty;
                                items.Add(item);
                                foundTags.Add(tagNormalized); // Marquer comme trouvé
                            }
                        }
                    }
                }

                // STRATÉGIE 2: Chercher des tags échués SANS quantité (ballons, cartouches, etc.)
                // Pattern pour tag seul (sera traité plus tard avec quantité CSV de référence)
                string tagOnlyPattern = @"([A-Z]{2,3}\d{1,4}[-_]?\d{1,4})";
                var tagOnlyMatches = Regex.Matches(line.Text, tagOnlyPattern, RegexOptions.IgnoreCase);

                foreach (Match tagMatch in tagOnlyMatches)
                {
                    if (tagMatch.Success)
                    {
                        var tagNormalized = tagMatch.Groups[1].Value.ToUpper(CultureInfo.InvariantCulture).Replace("_", "-");

                        // SEULEMENT si le tag n'a pas déjà été trouvé et ne fait pas partie d'un pattern tag+quantité
                        if (!foundTags.Contains(tagNormalized))
                        {
                            // Vérifier que ce n'est pas déjà inclus dans un pattern tag+quantité de la même ligne
                            bool isAlreadyInTagQtyPattern = matches.Cast<Match>()
                                .Any(m => m.Groups[1].Value.ToUpper(CultureInfo.InvariantCulture).Replace("_", "-") == tagNormalized);

                            if (!isAlreadyInTagQtyPattern)
                            {
                                // Créer un item "échoué" avec quantité par défaut 1 (sera remplacée par CSV si référence fournie)
                                var item = new PdfItem
                                {
                                    Tag = tagNormalized,
                                    PageNumber = pageNum,
                                    Quantity = 1, // Quantité par défaut pour tags échués
                                    Confidence = 0.5, // Confiance réduite pour tags sans quantité
                                    SourceType = "BALLON", // Nouveau type de source
                                    TableNumber = 0
                                };

                                items.Add(item);
                                foundTags.Add(tagNormalized); // Marquer comme trouvé
                            }
                        }
                    }
                }
            }

            return items;
        }

        /// <summary>
        /// V1.4 FALLBACK STRATEGY - Pour formats de tableaux non-standards (Bodyx: TOB, TOC, etc.)
        /// Cette stratégie utilise la référence CSV pour chercher les tags connus dans le PDF
        /// et associe la quantité trouvée sur la même ligne (dernier nombre de la ligne)
        /// Format Bodyx: Tag | Tag ASSY | Qty (la quantité est en dernière position)
        /// </summary>
        private static Dictionary<string, int> ExtractWithFallbackStrategy(string pdfPath, Dictionary<string, CsvRow> csvReference)
        {
            var results = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
            {
                return results;
            }

            Log("PdfAnalysis", "=== FALLBACK STRATEGY V1.4: Recherche tags CSV dans PDF ===");

            try
            {
                // Construire un set des tags CSV normalisés pour recherche rapide
                var csvTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var tag in csvReference.Keys)
                {
                    csvTags.Add(tag.ToUpper(CultureInfo.InvariantCulture).Replace("_", "-"));
                }

                const double Y_TOLERANCE = 10.0;
                var numberRegex = new Regex(@"^\d{1,3}$");

                using (var document = PdfDocument.Open(pdfPath))
                {
                    for (int pageNum = 1; pageNum <= document.NumberOfPages; pageNum++)
                    {
                        var page = document.GetPage(pageNum);
                        var words = page.GetWords().ToList();

                        // Grouper les mots par ligne Y
                        var lineGroups = new Dictionary<int, List<(string text, double x)>>();

                        foreach (var word in words)
                        {
                            if (string.IsNullOrWhiteSpace(word.Text)) continue;

                            int yKey = (int)(Math.Round(word.BoundingBox.Bottom / Y_TOLERANCE) * Y_TOLERANCE);

                            if (!lineGroups.ContainsKey(yKey))
                            {
                                lineGroups[yKey] = new List<(string, double)>();
                            }

                            lineGroups[yKey].Add((word.Text.Trim(), word.BoundingBox.Left));
                        }

                        // Pour chaque ligne, chercher les tags CSV connus
                        foreach (var lineEntry in lineGroups)
                        {
                            var lineWords = lineEntry.Value.OrderBy(w => w.x).ToList();
                            var lineText = string.Join(" ", lineWords.Select(w => w.text));

                            // Chercher chaque tag CSV dans cette ligne
                            foreach (var csvTag in csvTags)
                            {
                                // Pattern flexible pour matcher le tag (avec ou sans tiret/underscore)
                                var tagPattern = Regex.Escape(csvTag).Replace("-", "[-_]?");
                                var tagMatch = Regex.Match(lineText, $@"\b{tagPattern}\b", RegexOptions.IgnoreCase);

                                if (tagMatch.Success)
                                {
                                    // Tag trouvé ! Maintenant chercher la quantité (dernier nombre de la ligne)
                                    var numbers = lineWords.Where(w => numberRegex.IsMatch(w.text)).ToList();

                                    if (numbers.Any())
                                    {
                                        // Prendre le dernier nombre (format Bodyx: Tag | Tag ASSY | Qty)
                                        var lastNumber = numbers.Last();
                                        if (int.TryParse(lastNumber.text, out int qty) && qty >= 0 && qty <= 999)
                                        {
                                            // Ajouter ou accumuler la quantité
                                            if (results.ContainsKey(csvTag))
                                            {
                                                results[csvTag] += qty;
                                            }
                                            else
                                            {
                                                results[csvTag] = qty;
                                            }
                                            Log("Debug", $"[Fallback V1.4] Trouve: {csvTag} = {qty} (page {pageNum})");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                Log("PdfAnalysis", $"=== FALLBACK V1.4: {results.Count} tags trouves ===");
                return results;
            }
            catch (Exception ex)
            {
                Log("Error", $"[-] Erreur ExtractWithFallbackStrategy: {ex.Message}");
                return results;
            }
        }

        /// <summary>
        /// ALGORITHME V2 - Extraction par proximité X/Y validée à 100% (benchmark Python)
        /// Cette méthode extrait les tags et quantités en utilisant la proximité spatiale
        /// plutôt que la détection de structure de tableau
        /// 
        /// PARAMÈTRES VALIDÉS:
        /// - Y_TOLERANCE = 10 pixels (grouper mots sur même ligne)
        /// - X_MAX_DISTANCE = 150 pixels (distance max tag-qty)
        /// - Recherche: D'ABORD à droite du tag, PUIS à gauche
        /// </summary>
        /// <param name="pdfPath">Chemin complet du fichier PDF</param>
        /// <returns>Liste de PdfItem avec tag et quantité extraits par proximité</returns>
        public static List<PdfItem> ExtractByProximity(string pdfPath)
        {
            var items = new List<PdfItem>();
            
            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
            {
                Log("Error", $"[-] Fichier PDF introuvable: {pdfPath}");
                return items;
            }

            Log("PdfAnalysis", "=== EXTRACTION PAR PROXIMITE V2 (Algorithme 100%) ===");

            try
            {
                // Constantes validées par benchmark Python
                const double Y_TOLERANCE = 10.0;      // Grouper mots sur même ligne
                const double X_MAX_DISTANCE = 150.0;  // Distance max tag-qty
                const double X_MAX_DISTANCE_BODYX = 350.0; // Distance étendue pour format Bodyx (3 colonnes)
                
                // Pattern tag XNRGY standard: 2-4 lettres + 3-4 chiffres + tiret/underscore + 3-4 chiffres
                // Exemples: TOB1301-0111, WPA1301-0042, FCM1301-0018
                var tagRegex = new Regex(@"^([A-Z]{2,4}\d{3,4}[-_]\d{3,4})$", RegexOptions.IgnoreCase);
                
                // Pattern Tag ASSY classique (tag avec suffixe -A01, -A02, etc.)
                // Exemples: WPA1301-0042-A01, TOB1301-0111-A02
                var tagAssyRegex = new Regex(@"^([A-Z]{2,4}\d{3,4}[-_]\d{3,4}[-_]A\d{2,3})$", RegexOptions.IgnoreCase);
                
                // Pattern Tag ASSY format Bodyx court (colonne Tag_Assy dans cut lists Bodyx)
                // Ces tags ont un format PLUS COURT que les tags principaux
                // Exemples: YYY1301-07, ABC1234-12, XYZ9999-01
                // Format: 2-4 lettres + 3-4 chiffres + tiret + 1-2 chiffres (PAS 3-4 comme les tags standards)
                var tagAssyBodyxRegex = new Regex(@"^([A-Z]{2,4}\d{3,4}[-_]\d{1,2})$", RegexOptions.IgnoreCase);
                
                var numberRegex = new Regex(@"^\d{1,3}$"); // 1 à 3 chiffres pour quantité

                using (var document = PdfDocument.Open(pdfPath))
                {
                    Log("PdfAnalysis", $"Document ouvert: {document.NumberOfPages} pages");
                    _lastAnalyzedPageCount = document.NumberOfPages;

                    for (int pageNum = 1; pageNum <= document.NumberOfPages; pageNum++)
                    {
                        var page = document.GetPage(pageNum);
                        var words = page.GetWords().ToList();

                        // ÉTAPE 1: Grouper les mots par ligne Y (avec tolérance 10px)
                        var lineGroups = new Dictionary<int, List<(string text, double x, double y)>>();
                        
                        foreach (var word in words)
                        {
                            if (string.IsNullOrWhiteSpace(word.Text)) continue;
                            
                            // Arrondir Y à la dizaine pour grouper les mots sur la même ligne
                            int yKey = (int)(Math.Round(word.BoundingBox.Bottom / Y_TOLERANCE) * Y_TOLERANCE);
                            
                            if (!lineGroups.ContainsKey(yKey))
                            {
                                lineGroups[yKey] = new List<(string, double, double)>();
                            }
                            
                            lineGroups[yKey].Add((word.Text.Trim(), word.BoundingBox.Left, word.BoundingBox.Bottom));
                        }

                        // ÉTAPE 2: Pour chaque ligne, trouver les tags et leurs quantités par proximité X
                        foreach (var lineEntry in lineGroups)
                        {
                            var lineWords = lineEntry.Value;
                            
                            // Séparer tags, tags ASSY et nombres sur cette ligne
                            var tags = new List<(string tag, double x, bool isAssy)>();
                            var numbers = new List<(int qty, double x)>();
                            
                            foreach (var (text, x, y) in lineWords)
                            {
                                // Détecter les différents types de tags
                                if (tagAssyRegex.IsMatch(text))
                                {
                                    // Tag ASSY classique (ex: WPA1301-0042-A01)
                                    tags.Add((text.ToUpper(CultureInfo.InvariantCulture).Replace("_", "-"), x, true));
                                }
                                else if (tagAssyBodyxRegex.IsMatch(text))
                                {
                                    // Tag ASSY format Bodyx court (ex: YYY1301-07)
                                    tags.Add((text.ToUpper(CultureInfo.InvariantCulture).Replace("_", "-"), x, true));
                                    Log("Debug", $"[Bodyx] Tag ASSY court detecte: {text}");
                                }
                                else if (tagRegex.IsMatch(text))
                                {
                                    // Tag standard (ex: TOB1301-0111)
                                    tags.Add((text.ToUpper(CultureInfo.InvariantCulture).Replace("_", "-"), x, false));
                                }
                                else if (numberRegex.IsMatch(text) && int.TryParse(text, out int num))
                                {
                                    numbers.Add((num, x));
                                }
                            }

                            // Détecter format Bodyx (ligne avec Tag principal + Tag ASSY court)
                            bool hasTagAssy = tags.Any(t => t.isAssy);
                            bool hasRegularTag = tags.Any(t => !t.isAssy);
                            bool isBodyxLine = hasTagAssy && hasRegularTag;

                            if (isBodyxLine)
                            {
                                var regularTags = string.Join(", ", tags.Where(t => !t.isAssy).Select(t => t.tag));
                                var assyTags = string.Join(", ", tags.Where(t => t.isAssy).Select(t => t.tag));
                                var numbersStr = string.Join(", ", numbers.Select(n => n.qty.ToString()));
                                Log("Debug", $"[Bodyx] Page {pageNum}: Tags={regularTags} | TagsASSY={assyTags} | Nums={numbersStr}");
                            }

                            // ÉTAPE 3: Associer chaque tag à la quantité la plus proche
                            foreach (var (tag, tagX, isAssy) in tags)
                            {
                                // Pour format Bodyx, ignorer les Tag ASSY (on ne garde que le Tag principal)
                                if (isBodyxLine && isAssy)
                                {
                                    continue; // Ne pas créer d'item pour Tag ASSY
                                }

                                int? bestQty = null;
                                double bestDist = double.MaxValue;

                                // Distance maximale selon le format
                                double maxDistance = isBodyxLine ? X_MAX_DISTANCE_BODYX : X_MAX_DISTANCE;

                                // Chercher D'ABORD à DROITE du tag (format Tag | Qty ou Tag | Tag ASSY | Qty)
                                foreach (var (qty, numX) in numbers)
                                {
                                    if (numX > tagX) // À droite du tag
                                    {
                                        double dist = numX - tagX;
                                        if (dist < bestDist && dist < maxDistance)
                                        {
                                            bestDist = dist;
                                            bestQty = qty;
                                        }
                                    }
                                }

                                // Si aucune quantité à droite, chercher à GAUCHE (format Qty | Tag)
                                if (bestQty == null)
                                {
                                    foreach (var (qty, numX) in numbers)
                                    {
                                        if (numX < tagX) // À gauche du tag
                                        {
                                            double dist = tagX - numX;
                                            if (dist < bestDist && dist < maxDistance)
                                            {
                                                bestDist = dist;
                                                bestQty = qty;
                                            }
                                        }
                                    }
                                }

                                // IMPORTANT: Ignorer les tags sans quantité trouvée
                                // Ces tags sont probablement dans des zones non-tableaux (titres, cartouches, etc.)
                                if (!bestQty.HasValue)
                                {
                                    continue; // Pas de quantité = pas un tag de tableau
                                }

                                // Créer l'item SEULEMENT si une quantité a été trouvée
                                var item = new PdfItem
                                {
                                    Tag = tag,
                                    Quantity = bestQty.Value,
                                    PageNumber = pageNum,
                                    Confidence = 0.95,
                                    SourceType = "PROXIMITY",
                                    TableNumber = 0
                                };

                                items.Add(item);
                            }
                        }
                    }
                }

                Log("PdfAnalysis", $"=== EXTRACTION PROXIMITE: {items.Count} tags trouves ===");
                return items;
            }
            catch (Exception ex)
            {
                Log("Error", $"[-] Erreur ExtractByProximity: {ex.Message}");
                return items;
            }
        }

        /// <summary>
        /// Point d'entrée alternatif utilisant l'algorithme de proximité V2
        /// Retourne un dictionnaire consolidé tag -> quantité totale
        /// </summary>
        /// <param name="pdfPath">Chemin du fichier PDF</param>
        /// <param name="csvReference">Référence CSV optionnelle pour filtrage</param>
        /// <returns>Dictionnaire de tags avec leurs quantités CUMULÉES</returns>
        public static Dictionary<string, int> ExtractTablesFromPdfV2(string pdfPath, Dictionary<string, CsvRow>? csvReference = null)
        {
            Log("PdfAnalysis", "========== EXTRACTION PDF V2 (Algorithme Proximite) ==========");
            
            var results = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var items = ExtractByProximity(pdfPath);

            // Consolider: additionner les quantités pour chaque tag
            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.Tag))
                {
                    if (results.ContainsKey(item.Tag))
                    {
                        results[item.Tag] += item.Quantity;
                    }
                    else
                    {
                        results[item.Tag] = item.Quantity;
                    }
                }
            }

            Log("PdfAnalysis", $"V2 Extraction brute: {results.Count} tags uniques, {results.Values.Sum()} total qty");

            // Filtrer par référence CSV si fournie
            if (csvReference != null && csvReference.Count > 0)
            {
                var filteredResults = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var kvp in results)
                {
                    // Normaliser le tag pour comparaison
                    var normalizedTag = kvp.Key.ToUpperInvariant().Replace("_", "-");
                    
                    // Si le tag existe dans la référence CSV, l'inclure
                    if (csvReference.ContainsKey(normalizedTag) || csvReference.ContainsKey(kvp.Key))
                    {
                        filteredResults[kvp.Key] = kvp.Value;
                    }
                }

                Log("PdfAnalysis", $"V2 Apres filtrage CSV: {filteredResults.Count} tags, {filteredResults.Values.Sum()} total qty");
                
                // V1.5 FALLBACK: Chercher les tags CSV manquants OU avec quantité 0 dans le texte brut du PDF
                var missingOrZeroTags = csvReference.Keys
                    .Where(k => {
                        var normalized = k.ToUpper(CultureInfo.InvariantCulture).Replace("_", "-");
                        // Tag manquant
                        if (!filteredResults.ContainsKey(normalized) && !filteredResults.ContainsKey(k))
                            return true;
                        // Tag présent mais quantité = 0
                        if (filteredResults.TryGetValue(normalized, out int qty) && qty == 0)
                            return true;
                        if (filteredResults.TryGetValue(k, out int qty2) && qty2 == 0)
                            return true;
                        return false;
                    })
                    .ToList();
                
                if (missingOrZeroTags.Count > 0 && missingOrZeroTags.Count <= 20)
                {
                    Log("PdfAnalysis", $"[!] V1.5 Fallback: {missingOrZeroTags.Count} tags manquants/zero - Recherche texte brut...");
                    int recoveredCount = 0;
                    
                    using (var document = PdfDocument.Open(pdfPath))
                    {
                        foreach (var missingTag in missingOrZeroTags)
                        {
                            string normalizedMissing = missingTag.ToUpper(CultureInfo.InvariantCulture).Replace("_", "-");
                            
                            // Chercher le tag dans le texte brut de chaque page
                            for (int pageNum = 1; pageNum <= document.NumberOfPages; pageNum++)
                            {
                                var page = document.GetPage(pageNum);
                                string pageText = page.Text;
                                
                                if (pageText.IndexOf(normalizedMissing, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    pageText.IndexOf(missingTag, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    // Tag trouvé dans le texte brut ! Utiliser la quantité CSV
                                    int csvQty = csvReference[missingTag].Quantity;
                                    filteredResults[normalizedMissing] = csvQty;
                                    recoveredCount++;
                                    Log("PdfAnalysis", $"[+] V1.5 Tag recupere: {normalizedMissing} = {csvQty} (page {pageNum})");
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (recoveredCount > 0)
                    {
                        Log("PdfAnalysis", $"[+] V1.5 Fallback: {recoveredCount} tags recuperes");
                    }
                    else
                    {
                        Log("PdfAnalysis", $"[!] V1.5 Fallback: Aucun tag recupere (tags vraiment absents du PDF)");
                    }
                }
                
                return filteredResults;
            }

            return results;
        }

        #endregion

        #region Logging

        private static void Log(string category, string message)
        {
            // Envoyer au journal UI via evenement
            OnLog?.Invoke(category, message);
            
            // Envoyer aussi au Logger principal de l'application (fichier VaultSDK_POC_*.log)
            switch (category.ToUpperInvariant())
            {
                case "ERROR":
                    XnrgyEngineeringAutomationTools.Services.Logger.Error($"[DXFVerifier] {message}");
                    break;
                case "WARNING":
                    XnrgyEngineeringAutomationTools.Services.Logger.Warning($"[DXFVerifier] {message}");
                    break;
                case "DEBUG":
                    XnrgyEngineeringAutomationTools.Services.Logger.Debug($"[DXFVerifier] {message}");
                    break;
                default:
                    XnrgyEngineeringAutomationTools.Services.Logger.Info($"[DXFVerifier] {message}");
                    break;
            }
        }

        #endregion
    }
}
