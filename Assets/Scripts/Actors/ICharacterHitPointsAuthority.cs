namespace GameCore.Actors
{
    /// <summary>
    /// Ruleset-agnostic authority for reading and requesting changes to a character's
    /// current hit points. Implemented by networked player components in production and
    /// can be mocked in tests. UI code depends on this interface rather than Netcode types.
    /// </summary>
    public interface ICharacterHitPointsAuthority
    {
        /// <summary>Current hit points after the latest authoritative update.</summary>
        int CurrentHitPoints { get; }

        /// <summary>Maximum hit points used for clamping (ruleset-derived display max).</summary>
        int MaxHitPoints { get; }

        /// <summary>Sets current hit points to an absolute value (clamped server-side).</summary>
        void RequestSetCurrentHitPoints(int value);

        /// <summary>Adjusts current hit points by a delta (clamped server-side).</summary>
        void RequestAdjustCurrentHitPoints(int delta);
    }
}
