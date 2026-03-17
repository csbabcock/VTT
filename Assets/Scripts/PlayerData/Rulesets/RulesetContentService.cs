using System;
using System.Collections.Generic;
using System.IO;
using GameCore.PlayerData.Rulesets.Definitions;
using UnityEngine;

namespace GameCore.PlayerData.Rulesets
{
    /// <summary>
    /// Loads and provides access to ruleset content (races, classes, backgrounds, skills) from JSON files.
    /// Content is organized per ruleset under Assets/GameData/Rulesets/{rulesetId}/.
    /// </summary>
    public class RulesetContentService
    {
        private readonly string _rulesetId;
        private readonly string _basePath;

        private readonly Dictionary<string, RaceDefinition> _races = new();
        private readonly Dictionary<string, ClassDefinition> _classes = new();
        private readonly Dictionary<string, BackgroundDefinition> _backgrounds = new();
        private readonly Dictionary<string, SkillDefinition> _skills = new();

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

            LoadContent();
        }

        /// <summary>
        /// Gets all available races for the current ruleset.
        /// </summary>
        public IReadOnlyCollection<RaceDefinition> GetAvailableRaces()
        {
            return _races.Values;
        }

        /// <summary>
        /// Gets all available classes for the current ruleset.
        /// </summary>
        public IReadOnlyCollection<ClassDefinition> GetAvailableClasses()
        {
            return _classes.Values;
        }

        /// <summary>
        /// Gets all available backgrounds for the current ruleset.
        /// </summary>
        public IReadOnlyCollection<BackgroundDefinition> GetAvailableBackgrounds()
        {
            return _backgrounds.Values;
        }

        /// <summary>
        /// Gets all available skills for the current ruleset.
        /// </summary>
        public IReadOnlyCollection<SkillDefinition> GetAvailableSkills()
        {
            return _skills.Values;
        }

        public bool TryGetRace(string raceId, out RaceDefinition race)
        {
            return _races.TryGetValue(raceId, out race);
        }

        public bool TryGetClass(string classId, out ClassDefinition classDef)
        {
            return _classes.TryGetValue(classId, out classDef);
        }

        public bool TryGetBackground(string backgroundId, out BackgroundDefinition background)
        {
            return _backgrounds.TryGetValue(backgroundId, out background);
        }

        public bool TryGetSkill(string skillId, out SkillDefinition skill)
        {
            return _skills.TryGetValue(skillId, out skill);
        }

        /// <summary>
        /// Loads all JSON content for this ruleset into memory.
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
            LoadCollection(Path.Combine(_basePath, "skills"), _skills, d => d.id, "skill");
        }

        private static void LoadCollection<TDef>(
            string folderPath,
            IDictionary<string, TDef> target,
            Func<TDef, string> getId,
            string contentType)
            where TDef : class
        {
            if (target == null)
            {
                return;
            }

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
                    {
                        continue;
                    }

                    // Use Unity's JsonUtility to keep dependencies minimal
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

