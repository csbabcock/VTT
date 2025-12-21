namespace GameCore.EncounterMode
{
    /// <summary>
    /// Constants for encounter movement system.
    /// Centralizes magic numbers to improve maintainability.
    /// </summary>
    public static class EncounterMovementConstants
    {
        // Arrival thresholds
        public const float HORIZONTAL_THRESHOLD_MULTIPLIER = 0.5f;
        public const float MIN_HORIZONTAL_THRESHOLD = 0.5f;
        public const float GROUND_LEVEL_VERTICAL_THRESHOLD = 0.02f;
        public const float ELEVATED_VERTICAL_THRESHOLD = 0.5f;
        
        // Movement detection
        public const float MIN_MOVEMENT_DISTANCE = 0.05f;
        public const float MOVEMENT_TOLERANCE = 0.1f;
        
        // Vertical movement
        public const float SIGNIFICANT_VERTICAL_THRESHOLD_MULTIPLIER = 0.5f;
        public const float VERTICAL_DISTANCE_CLOSE_THRESHOLD = 0.1f;
        public const float VERTICAL_DISTANCE_GROUND_CLOSE = 0.2f;
        public const float VERTICAL_DISTANCE_MIN_FOR_FALLING = 0.2f;
        
        // Velocity thresholds
        public const float MIN_VERTICAL_VELOCITY_FOR_ANIMATION = 0.1f;
        public const float FORCE_DESCENT_SPEED_MULTIPLIER = 0.3f;
        
        // Speed multipliers
        public const float BASE_VERTICAL_SPEED_MULTIPLIER = 0.7f;
        public const float MIN_SPEED_MULTIPLIER = 0.4f;
        public const float MAX_SPEED_MULTIPLIER = 1.0f;
        public const float CLOSE_TO_GROUND_SPEED_MULTIPLIER = 0.5f;
        
        // Velocity smoothing
        public const float VELOCITY_SMOOTHING = 10f;
        public const float MAX_VERTICAL_DISTANCE_FOR_CALCULATION = 2f;
        
        // Animation thresholds
        public const float ANIMATION_SIGNIFICANT_THRESHOLD_MULTIPLIER = 0.5f;
    }
}

