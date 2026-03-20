using System.Collections.Generic;
using GameCore.PlayerData.Rulesets.Definitions;

namespace GameCore.PlayerData.Rulesets
{
    /// <summary>
    /// Read-only access to loaded ruleset content (JSON under GameData/Rulesets).
    /// Dependency-invert UI and calculators against this instead of concrete <see cref="RulesetContentService"/>.
    /// </summary>
    public interface IRulesetContentQuery
    {
        string RulesetId { get; }

        IReadOnlyCollection<RaceDefinition> GetRaces();
        IReadOnlyCollection<ClassDefinition> GetClasses();
        IReadOnlyCollection<BackgroundDefinition> GetBackgrounds();
        IReadOnlyCollection<SkillDefinition> GetSkills();

        /// <summary>
        /// All spells for this ruleset. First call loads spell JSON from disk (lazy).
        /// </summary>
        IReadOnlyCollection<SpellDefinition> GetSpells();

        IReadOnlyCollection<RuleTopicDefinition> GetRuleTopics();

        bool TryGetRace(string raceId, out RaceDefinition race);
        bool TryGetClass(string classId, out ClassDefinition classDef);
        bool TryGetBackground(string backgroundId, out BackgroundDefinition background);
        bool TryGetSkill(string skillId, out SkillDefinition skill);
        bool TryGetSpell(string spellId, out SpellDefinition spell);
        bool TryGetRuleTopic(string topicId, out RuleTopicDefinition topic);
    }
}
