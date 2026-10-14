using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QuantumCrossy.Input
{
    /// <summary>
    /// 터치 스와이프와 마우스 드래그를 같은 이동 명령으로 바꾼다.
    /// 화면 위쪽이 전진이다.
    /// </summary>
    public sealed class SwipeMoveSource : IMoveSource
    {
        readonly Func<float> thresholdPixels;
        Gesture touch = new Gesture();
        Gesture mouse = new Gesture();

        public SwipeMoveSource(Func<float> thresholdPixels)
        {
            this.thresholdPixels = thresholdPixels;
        }

        public bool TryPoll(out MoveCommand command)
        {
            command = MoveCommand.None;
            Touchscreen screen = Touchscreen.current;
            if (screen != null && TouchHappened(screen))
            {
                var primary = screen.primaryTouch;
                return touch.Read(
                    primary.press.wasPressedThisFrame,
                    primary.press.wasReleasedThisFrame,
                    primary.position.ReadValue(),
                    thresholdPixels(),
                    out command);
            }

            Mouse pointer = Mouse.current;
            if (pointer == null)
                return false;

            return mouse.Read(
                pointer.leftButton.wasPressedThisFrame,
                pointer.leftButton.wasReleasedThisFrame,
                pointer.position.ReadValue(),
                thresholdPixels(),
                out command);
        }

        static bool TouchHappened(Touchscreen screen)
        {
            var press = screen.primaryTouch.press;
            return press.isPressed || press.wasPressedThisFrame || press.wasReleasedThisFrame;
        }

        struct Gesture
        {
            bool tracking;
            Vector2 origin;

            public bool Read(bool pressed, bool released, Vector2 position, float minDistance, out MoveCommand command)
            {
                command = MoveCommand.None;
                if (pressed)
                {
                    tracking = true;
                    origin = position;
                }

                if (!tracking || !released)
                    return false;

                tracking = false;
                Vector2 delta = position - origin;
                if (delta.sqrMagnitude < minDistance * minDistance)
                    return false;

                if (Mathf.Abs(delta.y) >= Mathf.Abs(delta.x))
                    command = delta.y >= 0f ? MoveCommand.Forward : MoveCommand.Back;
                else
                    command = delta.x >= 0f ? MoveCommand.Right : MoveCommand.Left;
                return true;
            }
        }
    }
}
