using GameCore.EncounterMode.Grid;

namespace GameCore.EncounterMode.Services
{
    /// <summary>
    /// Pure validation for encounter grid moves. Shared by local play and the server.
    /// </summary>
    public static class EncounterMoveValidator
    {
        public readonly struct MoveRequest
        {
            public MoveRequest(int fromX, int fromZ, int toX, int toZ, int remainingFeet)
            {
                FromX = fromX;
                FromZ = fromZ;
                ToX = toX;
                ToZ = toZ;
                RemainingFeet = remainingFeet;
            }

            public int FromX { get; }
            public int FromZ { get; }
            public int ToX { get; }
            public int ToZ { get; }
            public int RemainingFeet { get; }
        }

        public readonly struct MoveResult
        {
            public MoveResult(bool isValid, int distanceFeet, int remainingFeetAfterMove)
            {
                IsValid = isValid;
                DistanceFeet = distanceFeet;
                RemainingFeetAfterMove = remainingFeetAfterMove;
            }

            public bool IsValid { get; }
            public int DistanceFeet { get; }
            public int RemainingFeetAfterMove { get; }
        }

        public static int CalculateDistanceFeet(int fromX, int fromZ, int toX, int toZ)
        {
            int deltaX = UnityEngine.Mathf.Abs(toX - fromX);
            int deltaZ = UnityEngine.Mathf.Abs(toZ - fromZ);
            int cellsMoved = UnityEngine.Mathf.Max(deltaX, deltaZ);
            return cellsMoved * 5;
        }

        public static MoveResult Validate(MoveRequest request)
        {
            if (request.RemainingFeet <= 0)
                return new MoveResult(false, 0, request.RemainingFeet);

            int distanceFeet = CalculateDistanceFeet(
                request.FromX, request.FromZ, request.ToX, request.ToZ);

            if (distanceFeet > request.RemainingFeet)
                return new MoveResult(false, distanceFeet, request.RemainingFeet);

            return new MoveResult(true, distanceFeet, request.RemainingFeet - distanceFeet);
        }

        public static MoveResult Validate(GridCell fromCell, GridCell toCell, int remainingFeet)
        {
            if (fromCell == null || toCell == null)
                return new MoveResult(false, 0, remainingFeet);

            return Validate(new MoveRequest(
                fromCell.X, fromCell.Z, toCell.X, toCell.Z, remainingFeet));
        }
    }
}
