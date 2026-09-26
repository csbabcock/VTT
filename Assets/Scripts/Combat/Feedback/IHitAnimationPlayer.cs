namespace GameCore.Combat.Feedback
{
    /// <summary>Plays a target's damage reaction without changing combat state.</summary>
    public interface IHitAnimationPlayer
    {
        void PlayHit();
    }
}
