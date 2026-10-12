using UnityEngine.InputSystem;

namespace QuantumCrossy.Input
{
    public sealed class KeyboardMoveSource : IMoveSource
    {
        public bool TryPoll(out MoveCommand command)
        {
            command = MoveCommand.None;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            if (Pressed(keyboard.upArrowKey) || Pressed(keyboard.wKey) || Pressed(keyboard.spaceKey))
                command = MoveCommand.Forward;
            else if (Pressed(keyboard.downArrowKey) || Pressed(keyboard.sKey))
                command = MoveCommand.Back;
            else if (Pressed(keyboard.leftArrowKey) || Pressed(keyboard.aKey))
                command = MoveCommand.Left;
            else if (Pressed(keyboard.rightArrowKey) || Pressed(keyboard.dKey))
                command = MoveCommand.Right;

            return command != MoveCommand.None;
        }

        static bool Pressed(UnityEngine.InputSystem.Controls.KeyControl key)
        {
            return key != null && key.wasPressedThisFrame;
        }
    }
}
