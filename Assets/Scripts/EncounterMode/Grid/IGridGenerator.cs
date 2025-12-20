using UnityEngine;

namespace GameCore.EncounterMode.Grid
{
    /// <summary>
    /// Interface for grid generation functionality
    /// </summary>
    public interface IGridGenerator
    {
        GridCell[,] Grid { get; }
        float CellSize { get; }
        Vector3 GridOrigin { get; }

        void GenerateGrid(Vector3 origin, int width, int height, float cellSize, LayerMask groundLayer);
        GridCell GetCellAtWorldPosition(Vector3 worldPos);
    }
}

