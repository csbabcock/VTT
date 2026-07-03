namespace GameCore.Interaction.ScreenPick
{
    /// <summary>Tunable thresholds for cursor-over-renderer screen picking.</summary>
    public readonly struct ScreenSpacePickSettings
    {
        public static ScreenSpacePickSettings Default { get; } = new ScreenSpacePickSettings(0.1f, 12f);

        public ScreenSpacePickSettings(float boundsInsetFraction, float maxPixelDistance)
        {
            BoundsInsetFraction = boundsInsetFraction;
            MaxPixelDistance = maxPixelDistance;
        }

        public float BoundsInsetFraction { get; }

        public float MaxPixelDistance { get; }
    }
}
