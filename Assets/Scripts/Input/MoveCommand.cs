using UnityEngine;

namespace QuantumCrossy.Input
{
    public enum MoveCommand
    {
        None = 0,
        Forward = 1,
        Back = 2,
        Left = 3,
        Right = 4
    }

    public static class MoveCommandExtensions
    {
        public static Vector2Int ToDelta(this MoveCommand command)
        {
            switch (command)
            {
                case MoveCommand.Forward: return new Vector2Int(0, 1);
                case MoveCommand.Back: return new Vector2Int(0, -1);
                case MoveCommand.Left: return new Vector2Int(-1, 0);
                case MoveCommand.Right: return new Vector2Int(1, 0);
                default: return Vector2Int.zero;
            }
        }
    }
}
