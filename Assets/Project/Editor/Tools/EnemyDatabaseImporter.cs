#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using PowerMath.Gameplay.Combat;
using PowerMath.Gameplay.Combat.Unity;

namespace PowerMath.Editor.Tools
{
    public sealed class EnemyDatabaseImporter : EditorWindow
    {
        public const string DefaultCsvPath = "Assets/Project/Data/EnemyDatabase.csv";
        public const string DefaultOutputFolder = "Assets/Project/Resources/StageMapContent";
        public const string DefaultGoogleSheetUrl =
            "https://docs.google.com/spreadsheets/d/1DdH5ZZzZDkbMSatpaPaAByNsSjLQiWEXfv5AbWwlj18/export?format=csv&gid=1801810648";

        private string _csvPath = DefaultCsvPath;
        private string _sheetUrl = DefaultGoogleSheetUrl;
        private bool _updateBiomeDefinitions = true;
        private Vector2 _scrollPos;
        private string _logOutput = string.Empty;

        [MenuItem("Tools/PowerMath/Enemy Database Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<EnemyDatabaseImporter>("Enemy Importer");
            window.minSize = new Vector2(560, 480);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Enemy Database Importer (Upsert Engine)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Upsert Engine: Compares against existing template assets first to preserve GUIDs and inspector sprites.\n" +
                "Creates new EnemyDefinition assets when no match exists.\n" +
                "Supports both EN and TH Display Names.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            GUILayout.Label("1. Data Source", EditorStyles.boldLabel);
            _csvPath = EditorGUILayout.TextField("Local CSV Path", _csvPath);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import from Local CSV", GUILayout.Height(28)))
            {
                if (File.Exists(_csvPath))
                {
                    string csv = File.ReadAllText(_csvPath);
                    RunUpsert(csv, _updateBiomeDefinitions, dryRun: false);
                }
                else
                {
                    EditorUtility.DisplayDialog("File Not Found", $"Could not find CSV at: {_csvPath}", "OK");
                }
            }

            if (GUILayout.Button("Preview (Dry Run)", GUILayout.Height(28)))
            {
                if (File.Exists(_csvPath))
                {
                    string csv = File.ReadAllText(_csvPath);
                    RunUpsert(csv, _updateBiomeDefinitions, dryRun: true);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            GUILayout.Label("2. Direct Google Sheet Sync", EditorStyles.boldLabel);
            _sheetUrl = EditorGUILayout.TextField("Google Sheet URL", _sheetUrl);

            if (GUILayout.Button("Fetch and Upsert from Google Sheet", GUILayout.Height(28)))
            {
                FetchFromUrlAndUpsert(_sheetUrl, _updateBiomeDefinitions);
            }

            EditorGUILayout.Space(8);
            GUILayout.Label("3. Options", EditorStyles.boldLabel);
            _updateBiomeDefinitions = EditorGUILayout.ToggleLeft(
                "Link imported Normal Monsters into BiomeDefinition assets",
                _updateBiomeDefinitions);

            if (!string.IsNullOrEmpty(_logOutput))
            {
                EditorGUILayout.Space(10);
                GUILayout.Label("Log & Summary:", EditorStyles.boldLabel);
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200));
                EditorGUILayout.TextArea(_logOutput, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        private void FetchFromUrlAndUpsert(string url, bool updateBiomes)
        {
            EditorUtility.DisplayProgressBar("Google Sheets Sync", "Downloading latest CSV...", 0.3f);
            var req = UnityWebRequest.Get(url);
            var op = req.SendWebRequest();
            op.completed += _ =>
            {
                EditorUtility.ClearProgressBar();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    EditorUtility.DisplayDialog("Download Error", req.error, "OK");
                    req.Dispose();
                    return;
                }

                string csv = req.downloadHandler.text;
                req.Dispose();

                // Also update local copy
                if (!string.IsNullOrEmpty(_csvPath))
                {
                    string dir = Path.GetDirectoryName(_csvPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(_csvPath, csv);
                }

                RunUpsert(csv, updateBiomes, dryRun: false);
            };
        }

        public static UpsertResult RunUpsert(string csvText, bool updateBiomes, bool dryRun)
        {
            var result = new UpsertResult();
            var rows = ParseEnemyCsv(csvText);

            if (rows.Count == 0)
            {
                result.Logs.Add("Error: No valid enemy rows parsed from CSV.");
                return result;
            }

            // Ensure destination directory exists
            if (!Directory.Exists(DefaultOutputFolder))
            {
                Directory.CreateDirectory(DefaultOutputFolder);
                AssetDatabase.Refresh();
            }

            // Index all existing EnemyDefinition assets in StageMapContent
            string[] guids = AssetDatabase.FindAssets("t:EnemyDefinition", new[] { DefaultOutputFolder });
            var existingAssets = new List<ExistingAssetEntry>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
                if (asset != null)
                {
                    existingAssets.Add(new ExistingAssetEntry
                    {
                        Asset = asset,
                        Path = path,
                        FileName = Path.GetFileNameWithoutExtension(path),
                        IsClaimed = false
                    });
                }
            }

            result.Logs.Add($"Found {existingAssets.Count} existing EnemyDefinition assets in {DefaultOutputFolder}.");
            result.Logs.Add($"Processing {rows.Count} rows from CSV (DryRun: {dryRun})...\n");

            var normalMonstersByBiome = new Dictionary<int, List<EnemyDefinition>>();

            if (!dryRun)
            {
                AssetDatabase.StartAssetEditing();
            }

            try
            {
                foreach (EnemyCsvRow row in rows)
                {
                    ExistingAssetEntry matchedEntry = FindBestMatch(row, existingAssets);
                    EnemyDefinition targetAsset;
                    string targetPath;

                    if (matchedEntry != null)
                    {
                        matchedEntry.IsClaimed = true;
                        targetAsset = matchedEntry.Asset;
                        targetPath = matchedEntry.Path;
                        result.UpdatedCount++;
                        result.Logs.Add($"[UPDATE] {row.Id} ({row.Name}) -> matched existing '{matchedEntry.FileName}'");
                    }
                    else
                    {
                        string cleanName = SanitizeFileName(row.Name);
                        string fileName = $"{row.Id}_{cleanName}.asset";
                        targetPath = $"{DefaultOutputFolder}/{fileName}";

                        if (!dryRun)
                        {
                            targetAsset = ScriptableObject.CreateInstance<EnemyDefinition>();
                            AssetDatabase.CreateAsset(targetAsset, targetPath);
                        }
                        else
                        {
                            targetAsset = ScriptableObject.CreateInstance<EnemyDefinition>();
                        }

                        result.CreatedCount++;
                        result.Logs.Add($"[CREATE] {row.Id} ({row.Name}) -> new asset '{fileName}'");
                    }

                    if (!dryRun)
                    {
                        ApplyProperties(targetAsset, row);
                    }

                    if (row.EncounterKind == StageEncounterKind.NormalMonster)
                    {
                        if (!normalMonstersByBiome.ContainsKey(row.BiomeNumber))
                            normalMonstersByBiome[row.BiomeNumber] = new List<EnemyDefinition>();
                        normalMonstersByBiome[row.BiomeNumber].Add(targetAsset);
                    }
                }

                if (!dryRun && updateBiomes)
                {
                    UpdateBiomeDefinitions(normalMonstersByBiome, result);
                }
            }
            finally
            {
                if (!dryRun)
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }

            result.Logs.Add($"\nSummary: {result.CreatedCount} created, {result.UpdatedCount} updated, {rows.Count} total.");
            string summaryText = string.Join("\n", result.Logs);

            var win = Resources.FindObjectsOfTypeAll<EnemyDatabaseImporter>().FirstOrDefault();
            if (win != null) win._logOutput = summaryText;

            if (!dryRun)
            {
                EditorUtility.DisplayDialog("Upsert Complete",
                    $"Enemy Database Upsert Complete!\nCreated: {result.CreatedCount}\nUpdated: {result.UpdatedCount}",
                    "OK");
            }

            return result;
        }

        private static ExistingAssetEntry FindBestMatch(EnemyCsvRow row, List<ExistingAssetEntry> pool)
        {
            var unclaimed = pool.Where(x => !x.IsClaimed).ToList();

            // 1. Direct ID match on enemyId
            var idMatch = unclaimed.FirstOrDefault(x => string.Equals(x.Asset.EnemyId, row.Id, StringComparison.OrdinalIgnoreCase));
            if (idMatch != null) return idMatch;

            // 2. Direct name match on DisplayName (English)
            var nameMatch = unclaimed.FirstOrDefault(x => string.Equals(x.Asset.DisplayName, row.Name, StringComparison.OrdinalIgnoreCase));
            if (nameMatch != null) return nameMatch;

            // 3. Filename match on exact row ID or slug
            string slug = Slugify(row.Name);
            var fileMatch = unclaimed.FirstOrDefault(x =>
                x.FileName.IndexOf(row.Id, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (!string.IsNullOrEmpty(slug) && x.FileName.IndexOf(slug, StringComparison.OrdinalIgnoreCase) >= 0));
            if (fileMatch != null) return fileMatch;

            // 3b. Fuzzy/Token match for known variations (e.g. King-Puru, Toxic-Slime, Pocket-Goblin in Biome 1)
            if (row.BiomeNumber == 1 && row.EncounterKind == StageEncounterKind.NormalMonster)
            {
                if (row.Id == "B1-N03" || row.Name.IndexOf("tox", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var match = unclaimed.FirstOrDefault(x => x.FileName.IndexOf("Toxic-Slime", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (match != null) return match;
                }
                if (row.Id == "B1-N04" || row.Name.IndexOf("puru", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var match = unclaimed.FirstOrDefault(x => x.FileName.IndexOf("King-Puru", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (match != null) return match;
                }
                if (row.Id == "B1-N05" || row.Name.IndexOf("goblin", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var match = unclaimed.FirstOrDefault(x => x.FileName.IndexOf("Pocket-Goblin", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (match != null) return match;
                }
            }

            // 4. Biome template match (e.g. Biome02_Normal, Biome02_MiniBoss, Biome02_BigBoss, FinalBoss)
            if (row.BiomeNumber >= 1 && row.BiomeNumber <= 7)
            {
                if (row.EncounterKind == StageEncounterKind.FinalBoss || (row.BiomeNumber == 7 && row.EncounterKind == StageEncounterKind.BigBoss))
                {
                    var finalBoss = unclaimed.FirstOrDefault(x => x.FileName.Equals("FinalBoss", StringComparison.OrdinalIgnoreCase));
                    if (finalBoss != null) return finalBoss;
                }

                if (row.EncounterKind == StageEncounterKind.BigBoss)
                {
                    string bossFileName = $"Biome{row.BiomeNumber:D2}_BigBoss";
                    var boss = unclaimed.FirstOrDefault(x => x.FileName.Equals(bossFileName, StringComparison.OrdinalIgnoreCase));
                    if (boss != null) return boss;
                }

                if (row.EncounterKind == StageEncounterKind.MiniBoss)
                {
                    string miniFileName = $"Biome{row.BiomeNumber:D2}_MiniBoss";
                    var mini = unclaimed.FirstOrDefault(x => x.FileName.Equals(miniFileName, StringComparison.OrdinalIgnoreCase));
                    if (mini != null) return mini;
                }

                if (row.EncounterKind == StageEncounterKind.NormalMonster && row.Id.EndsWith("-N01"))
                {
                    string normalFileName = $"Biome{row.BiomeNumber:D2}_Normal";
                    var normal = unclaimed.FirstOrDefault(x => x.FileName.Equals(normalFileName, StringComparison.OrdinalIgnoreCase));
                    if (normal != null) return normal;
                }
            }

            return null;
        }

        private static void ApplyProperties(EnemyDefinition asset, EnemyCsvRow row)
        {
            var so = new SerializedObject(asset);

            so.FindProperty("enemyId").stringValue = row.Id;
            so.FindProperty("displayName").stringValue = row.Name;

            var thaiProp = so.FindProperty("thaiDisplayName");
            if (thaiProp != null)
            {
                thaiProp.stringValue = row.ThaiName ?? string.Empty;
            }

            so.FindProperty("biomeId").stringValue = row.BiomeId;
            so.FindProperty("encounterKind").enumValueIndex = (int)row.EncounterKind;
            so.FindProperty("baseHp").intValue = Mathf.Max(1, row.Hp);
            so.FindProperty("maximumCooldown").intValue = Mathf.Max(1, row.EnemyActionCooldown);
            so.FindProperty("hpMultiplierBasisPoints").intValue = row.HpMultiplierBasisPoints;

            // Note: enemySprite is purposefully untouched here, preserving any inspector assignments!

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void UpdateBiomeDefinitions(Dictionary<int, List<EnemyDefinition>> normalMonstersByBiome, UpsertResult result)
        {
            // Load all enemy definitions in folder to locate bosses and minibosses
            string[] guids = AssetDatabase.FindAssets("t:EnemyDefinition", new[] { DefaultOutputFolder });
            var allEnemies = new List<EnemyDefinition>();
            foreach (string guid in guids)
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (enemy != null) allEnemies.Add(enemy);
            }

            for (int biomeNum = 1; biomeNum <= 7; biomeNum++)
            {
                string biomePath = $"{DefaultOutputFolder}/Biome{biomeNum:D2}.asset";
                var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(biomePath);
                if (biome == null) continue;

                var so = new SerializedObject(biome);

                // 1. Populate Normal Monsters (clean, valid, no nulls)
                List<EnemyDefinition> monsters = null;
                if (!normalMonstersByBiome.TryGetValue(biomeNum, out monsters) || monsters == null || monsters.Count == 0)
                {
                    monsters = allEnemies.Where(e =>
                        e != null &&
                        e.BiomeId == $"biome-{biomeNum}" &&
                        e.EncounterKind == StageEncounterKind.NormalMonster).ToList();
                }
                else
                {
                    monsters = monsters.Where(e => e != null && e.EncounterKind == StageEncounterKind.NormalMonster).ToList();
                }

                // Ensure monsters are sorted by ID
                monsters.Sort((a, b) => string.Compare(a.EnemyId, b.EnemyId, StringComparison.OrdinalIgnoreCase));

                var normalProp = so.FindProperty("normalMonsters");
                if (normalProp != null && normalProp.isArray)
                {
                    normalProp.ClearArray();
                    for (int i = 0; i < monsters.Count; i++)
                    {
                        normalProp.InsertArrayElementAtIndex(i);
                        normalProp.GetArrayElementAtIndex(i).objectReferenceValue = monsters[i];
                    }
                }

                // 2. Populate Boss Bindings for protected stages
                string bNumStr = biomeNum.ToString();
                var miniBoss = allEnemies.FirstOrDefault(e =>
                    e != null &&
                    e.BiomeId == $"biome-{biomeNum}" &&
                    e.EncounterKind == StageEncounterKind.MiniBoss &&
                    e.EnemyId.StartsWith($"B{bNumStr}-E", StringComparison.OrdinalIgnoreCase))
                    ?? allEnemies.FirstOrDefault(e =>
                        e != null &&
                        e.BiomeId == $"biome-{biomeNum}" &&
                        e.EncounterKind == StageEncounterKind.MiniBoss);

                var bigBoss = allEnemies.FirstOrDefault(e =>
                    e != null &&
                    e.BiomeId == $"biome-{biomeNum}" &&
                    (e.EncounterKind == StageEncounterKind.BigBoss || e.EncounterKind == StageEncounterKind.FinalBoss) &&
                    e.EnemyId.StartsWith($"B{bNumStr}-B01", StringComparison.OrdinalIgnoreCase))
                    ?? allEnemies.FirstOrDefault(e =>
                        e != null &&
                        e.BiomeId == $"biome-{biomeNum}" &&
                        (e.EncounterKind == StageEncounterKind.BigBoss || e.EncounterKind == StageEncounterKind.FinalBoss));

                var finalBoss = allEnemies.FirstOrDefault(e =>
                    e != null &&
                    e.EncounterKind == StageEncounterKind.FinalBoss) ?? bigBoss;

                var bossBindingsProp = so.FindProperty("bossBindings");
                if (bossBindingsProp != null && bossBindingsProp.isArray)
                {
                    bossBindingsProp.ClearArray();
                    int bindingIdx = 0;
                    for (int stage = biome.FirstStage; stage <= biome.LastStage; stage++)
                    {
                        var stageId = new StageId(stage);
                        if (!StageClassificationPolicy.IsProtected(stageId)) continue;
                        StageEncounterKind requiredKind = StageClassificationPolicy.Classify(stageId);

                        EnemyDefinition selectedBoss = null;
                        if (requiredKind == StageEncounterKind.FinalBoss)
                            selectedBoss = finalBoss;
                        else if (requiredKind == StageEncounterKind.BigBoss)
                            selectedBoss = bigBoss;
                        else if (requiredKind == StageEncounterKind.MiniBoss)
                            selectedBoss = miniBoss;

                        if (selectedBoss != null)
                        {
                            bossBindingsProp.InsertArrayElementAtIndex(bindingIdx);
                            var elem = bossBindingsProp.GetArrayElementAtIndex(bindingIdx);
                            elem.FindPropertyRelative("stage").intValue = stage;
                            elem.FindPropertyRelative("monster").objectReferenceValue = selectedBoss;
                            bindingIdx++;
                        }
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(biome);
                result.Logs.Add($"[BIOME LINK] Updated 'Biome{biomeNum:D2}.asset' with {monsters.Count} normal monsters and valid boss bindings.");
            }
        }

        public static List<EnemyCsvRow> ParseEnemyCsv(string text)
        {
            var list = new List<EnemyCsvRow>();
            if (string.IsNullOrWhiteSpace(text)) return list;

            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int startRow = 1; // Default skip header row

            for (int i = startRow; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] cols = SplitCsvLine(line);
                if (cols.Length < 3) continue;

                string id = cols[0].Trim();
                if (string.Equals(id, "id", StringComparison.OrdinalIgnoreCase) || id.StartsWith("Monster Database"))
                    continue;

                string sprite = cols.Length > 1 ? cols[1].Trim() : string.Empty;
                string name = cols.Length > 2 ? cols[2].Trim() : string.Empty;
                string thaiName = cols.Length > 3 ? cols[3].Trim() : string.Empty;
                string hpStr = cols.Length > 4 ? cols[4].Trim() : "30";
                string typeStr = cols.Length > 5 ? cols[5].Trim() : "Normal";
                string actionStr = cols.Length > 6 ? cols[6].Trim() : "4";
                string biomeStr = cols.Length > 7 ? cols[7].Trim() : "1";

                int.TryParse(hpStr, out int hp);
                if (hp <= 0) hp = 30;

                int.TryParse(actionStr, out int action);
                if (action <= 0) action = 4;

                int.TryParse(biomeStr, out int biomeNum);
                if (biomeNum <= 0) biomeNum = 1;

                StageEncounterKind kind = StageEncounterKind.NormalMonster;
                int hpMultiplier = 10000;

                if (typeStr.IndexOf("elite", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    kind = StageEncounterKind.MiniBoss;
                }
                else if (typeStr.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (biomeNum == 7 && id.IndexOf("B01", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        kind = StageEncounterKind.FinalBoss;
                    }
                    else
                    {
                        kind = StageEncounterKind.BigBoss;
                    }
                }

                list.Add(new EnemyCsvRow
                {
                    Id = id,
                    Sprite = sprite,
                    Name = name,
                    ThaiName = thaiName,
                    Hp = hp,
                    EncounterKind = kind,
                    HpMultiplierBasisPoints = hpMultiplier,
                    EnemyActionCooldown = action,
                    BiomeNumber = biomeNum,
                    BiomeId = $"biome-{biomeNum}"
                });
            }

            return list;
        }

        private static string[] SplitCsvLine(string line)
        {
            var matches = Regex.Matches(line, @"(?<=^|,)(?:""(?<val>(?:[^""]|"""")*)""|(?<val>[^,]*))");
            var result = new List<string>();
            foreach (Match match in matches)
            {
                if (match.Index == line.Length && string.IsNullOrEmpty(match.Value)) continue;
                string v = match.Groups["val"].Value.Replace("\"\"", "\"");
                result.Add(v);
            }
            return result.ToArray();
        }

        private static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return Regex.Replace(text.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        }

        private static string SanitizeFileName(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "Unnamed";
            string clean = Regex.Replace(text.Trim(), @"[\\/:*?""<>|,\.]+", "-");
            clean = Regex.Replace(clean, @"\s+", "-");
            clean = Regex.Replace(clean, @"-+", "-");
            return clean.Trim('-');
        }

        public sealed class EnemyCsvRow
        {
            public string Id;
            public string Sprite;
            public string Name;
            public string ThaiName;
            public int Hp;
            public StageEncounterKind EncounterKind;
            public int HpMultiplierBasisPoints;
            public int EnemyActionCooldown;
            public int BiomeNumber;
            public string BiomeId;
        }

        private sealed class ExistingAssetEntry
        {
            public EnemyDefinition Asset;
            public string Path;
            public string FileName;
            public bool IsClaimed;
        }

        public sealed class UpsertResult
        {
            public int CreatedCount;
            public int UpdatedCount;
            public List<string> Logs = new List<string>();
        }
    }
}
#endif
