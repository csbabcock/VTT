using System;
using GameCore.Actors;

namespace GameCore.Combat.Adapters
{
    public sealed class CharacterDamageableAdapter : IDamageable
    {
        private readonly ICharacterHitPointsAuthority _readHitPoints;
        private readonly IActor _targetActor;
        private readonly IActor _attackerActor;
        private readonly Func<int> _armorClassProvider;

        public CharacterDamageableAdapter(
            ICharacterHitPointsAuthority readHitPoints,
            IActor targetActor,
            IActor attackerActor,
            Func<int> armorClassProvider,
            string displayName)
        {
            _readHitPoints = readHitPoints ?? throw new ArgumentNullException(nameof(readHitPoints));
            _targetActor = targetActor;
            _attackerActor = attackerActor;
            _armorClassProvider = armorClassProvider ?? throw new ArgumentNullException(nameof(armorClassProvider));
            DisplayName = displayName ?? string.Empty;
        }

        public string DisplayName { get; }

        public int ArmorClass => _armorClassProvider();

        public int CurrentHitPoints => _readHitPoints.CurrentHitPoints;

        public int MaxHitPoints => _readHitPoints.MaxHitPoints;

        public bool IsDestroyed => CurrentHitPoints <= 0;

        public void ApplyDamage(int amount)
        {
            if (amount <= 0 || IsDestroyed || _targetActor == null || _attackerActor == null)
                return;

            Services.CombatSheetMutator.TryApplyDamage(_targetActor, _attackerActor, amount);
        }
    }
}
