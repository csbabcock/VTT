using UnityEngine;

namespace GameCore.EncounterMode.Grid
{
    /// <summary>
    /// Represents a single cell in the grid system.
    /// Each cell represents a configurable distance (default 5 feet = 1.524 meters).
    /// </summary>
    public class GridCell
    {
        public int X { get; private set; }
        public int Z { get; private set; }
        public Vector3 WorldPosition { get; private set; }
        public bool IsWalkable { get; set; } = true;
        public int ElevationLevel { get; set; } = 0; // 0 = ground level, 1 = 5 feet up, etc.

        public GridCell(int x, int z, Vector3 worldPosition)
        {
            X = x;
            Z = z;
            WorldPosition = worldPosition;
        }

        /// <summary>
        /// Gets the world position at the specified elevation level.
        /// </summary>
        public Vector3 GetPositionAtElevation(float cellSize, int elevationLevel)
        {
            Vector3 pos = WorldPosition;
            pos.y += elevationLevel * cellSize;
            return pos;
        }
    }
}

