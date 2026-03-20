using System;
using System.Collections.Generic;
using System.IO;
using GameCore.PlayerData.Rulesets.Definitions;
using UnityEngine;

namespace GameCore.PlayerData.Rulesets
{
    /// <summary>
    /// Loads and provides access to ruleset content (races, classes, backgrounds, skills, spells, rule topics) from JSON files.
    /// Content is organized per ruleset under Assets/GameData/Rulesets/{rulesetId}/.
    /// Spells are loaded on first access to <see cref="GetSpells"/> to keep startup light for large lists.
    /// </summary>
    public class RulesetContentService : IRulesetContentQuery
    {
        private readonly string _rulesetId;
        private readonly string _basePath;

        private readonly Dictionary<string, RaceDefinition> _races = new();
        private readonly Dictionary<string, ClassDefinition> _classes = new();
        private readonly Dictionary<string, BackgroundDefinition> _backgrounds = new();
        private readonly Dictionary<string, SkillDefinition> _skills = new();
        private readonly Dictionary<string, RuleTopicDefinition> _ruleTopics = new();

        private readonly Lazy<Dictionary<string, SpellDefinition>> _spellsLazy;

        public string RulesetId => _rulesetId;

        /// <summary>
        /// Creates a new RulesetContentService for the given ruleset.
        /// </summary>
        /// <param name="rulesetId">Ruleset identifier (e.g., "DnD5e").</param>
        /// <param name="basePath">
        /// Optional base path for content. If null or empty, defaults to
        /// Application.dataPath + "/GameData/Rulesets/" + rulesetId.
        /// </param>
        public RulesetContentService(string rulesetId, string basePath = null)
        {
            _rulesetId = rulesetId ?? throw new ArgumentNullException(nameof(rulesetId));
            _basePath = !string.IsNullOrEmpty(basePath)
                ? basePath
                : Path.Combine(Application.dataPath, "GameData", "Rulesets", _rulesetId);

            _spellsLazy = new Lazy<Dictionary<string, SpellDefinition>>(LoadSpells, isThreadSafe: true);

            LoadContent();
        }

        public IReadOnlyCollection<RaceDefinition> GetRaces() => _races.Values;

        public IReadOnlyCollection<ClassDefinition> GetClasses() => _classes.Values;

        public IReadOnlyCollection<BackgroundDefinition> GetBackgrounds() => _backgrounds.Values;

        public IReadOnlyCollection<SkillDefinition> GetSkills() => _skills.Values;

        public IReadOnlyCollection<SpellDefinition> GetSpells() => _spellsLazy.Value.Values;

        public IReadOnlyCollection<RuleTopicDefinition> GetRuleTopics() => _ruleTopics.Values;

        public bool TryGetRace(string raceId, out RaceDefinition race) =>
            _races.TryGetValue(raceId, out race);

        public bool TryGetClass(string classId, out ClassDefinition classDef) =>
            _classes.TryGetValue(classId, out classDef);

        public bool TryGetBackground(string backgroundId, out BackgroundDefinition background) =>
            _backgrounds.TryGetValue(backgroundId, out background);

        public bool TryGetSkill(string skillId, out SkillDefinition skill) =>
            _skills.TryGetValue(skillId, out skill);

        public bool TryGetSpell(string spellId, out SpellDefinition spell) =>
            _spellsLazy.Value.TryGetValue(spellId, out spell);

        public bool TryGetRuleTopic(string topicId, out RuleTopicDefinition topic) =>
            _ruleTopics.TryGetValue(topicId, out topic);

        /// <summary>
        /// Loads all JSON content for this ruleset into memory (except spells, which load lazily).
        /// </summary>
        private void LoadContent()
        {
            if (!Directory.Exists(_basePath))
            {
                Debug.LogWarning($"RulesetContentService: Base path not found for ruleset '{_rulesetId}': {_basePath}");
                return;
            }

            LoadCollection(Path.Combine(_basePath, "races"), _races, d => d.id, "race");
            LoadCollection(Path.Combine(_basePath, "classes"), _classes, d => d.id, "class");
            LoadCollection(Path.Combine(_basePath, "backgrounds"), _backgrounds, d => d.id, "background");
            LoadSkillsFolder(Path.Combine(_basePath, "skills"));
            LoadCollection(Path.Combine(_basePath, "rules"), _ruleTopics, d => d.id, "rule topic");
        }

        private void LoadSkillsFolder(string folderPath)
        {
            _skills.Clear();

            if (!Directory.Exists(folderPath))
            {
                Debug.LogWarning($"RulesetContentService: skill folder not found: {folderPath}");
                return;
            }

            string[] files = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    if (string.IsNullOrWhiteSpace(json))
                        continue;

                    var manifest = JsonUtility.FromJson<SkillManifestDefinition>(json);
                    if (manifest == null)
                        continue;

                    if (manifest.skills != null && manifest.skills.Count > 0)
                    {
                        foreach (SkillDefinition s in manifest.skills)
                        {
                            AddSkillEntry(s, file);
                        }
                    }
                    else if (!string.IsNullOrEmpty(manifest.id) &&
                             manifest.id.StartsWith("skill.", StringComparison.Ordinal))
                    {
                        AddSkillEntry(new SkillDefinition
                        {
                            id = manifest.id,
                            name = manifest.name,
                            ability = manifest.ability
                        }, file);
                    }
                    else
                    {
                        var single = JsonUtility.FromJson<SkillDefinition>(json);
                        if (single != null && !string.IsNullOrEmpty(single.id))
                            AddSkillEntry(single, file);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"RulesetContentService: Error loading skill from {file}: {ex.Message}");
                }
            }
        }

        private void AddSkillEntry(SkillDefinition s, string sourceFile)
        {
            if (s == null || string.IsNullOrEmpty(s.id))
            {
                Debug.LogWarning($"RulesetContentService: skill entry in {sourceFile} has no id; skipping.");
                return;
            }

            if (_skills.ContainsKey(s.id))
            {
                Debug.LogWarning($"RulesetContentService: Duplicate skill id '{s.id}' in {sourceFile}; skipping.");
                return;
            }

            _skills[s.id] = s;
        }

        private Dictionary<string, SpellDefinition> LoadSpells()
        {
            var target = new Dictionary<string, SpellDefinition>(StringComparer.Ordinal);
            string folderPath = Path.Combine(_basePath, "spells");

            if (!Directory.Exists(folderPath))
                return target;

            string[] files = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    if (string.IsNullOrWhiteSpace(json))
                        continue;

                    SpellDefinition def = JsonUtility.FromJson<SpellDefinition>(json);
                    if (def == null || string.IsNullOrEmpty(def.id))
                    {
                        Debug.LogWarning($"RulesetContentService: spell in {file} has no id; skipping.");
                        continue;
                    }

                    if (target.ContainsKey(def.id))
                    {
                        Debug.LogWarning($"RulesetContentService: Duplicate spell id '{def.id}' in {file}; skipping.");
                        continue;
                    }

                    target[def.id] = def;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"RulesetContentService: Error loading spell from {file}: {ex.Message}");
                }
            }

            return target;
        }

        private static void LoadCollection<TDef>(
            string folderPath,
            IDictionary<string, TDef> target,
            Func<TDef, string> getId,
            string contentType)
            where TDef : class
        {
            if (target == null)
                return;

            target.Clear();

            if (!Directory.Exists(folderPath))
            {
                Debug.LogWarning($"RulesetContentService: {contentType} folder not found: {folderPath}");
                return;
            }

            string[] files = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    if (string.IsNullOrWhiteSpace(json))
                        continue;

                    TDef definition = JsonUtility.FromJson<TDef>(json);
                    if (definition == null)
                    {
                        Debug.LogWarning($"RulesetContentService: Failed to parse {contentType} JSON: {file}");
                        continue;
                    }

                    string id = getId(definition);
                    if (string.IsNullOrEmpty(id))
                    {
                        Debug.LogWarning($"RulesetContentService: {contentType} in {file} has no id; skipping.");
                        continue;
                    }

                    if (target.ContainsKey(id))
                    {
                        Debug.LogWarning($"RulesetContentService: Duplicate {contentType} id '{id}' in {file}; skipping.");
                        continue;
                    }

                    target[id] = definition;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"RulesetContentService: Error loading {contentType} from {file}: {ex.Message}");
                }
            }
        }
    }
}
