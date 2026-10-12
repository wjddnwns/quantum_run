using System.Collections.Generic;
using UnityEngine;

namespace QuantumCrossy.Input
{
    /// <summary>
    /// 등록된 입력 원천을 모아 이동 명령을 한 줄로 내보낸다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class MoveInput : MonoBehaviour
    {
        readonly List<IMoveSource> sources = new List<IMoveSource>(2);
        readonly Queue<MoveCommand> pending = new Queue<MoveCommand>(4);
        bool accepting = true;

        public void Use(params IMoveSource[] moveSources)
        {
            sources.Clear();
            for (int i = 0; i < moveSources.Length; i++)
            {
                if (moveSources[i] != null)
                    sources.Add(moveSources[i]);
            }
        }

        public void SetAccepting(bool value)
        {
            accepting = value;
            if (!value)
                pending.Clear();
        }

        public bool TryConsume(out MoveCommand command)
        {
            if (pending.Count == 0)
            {
                command = MoveCommand.None;
                return false;
            }

            command = pending.Dequeue();
            return command != MoveCommand.None;
        }

        void Update()
        {
            if (!accepting)
                return;

            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i].TryPoll(out MoveCommand command))
                    Enqueue(command);
            }
        }

        void Enqueue(MoveCommand command)
        {
            if (command == MoveCommand.None || pending.Count >= 2)
                return;
            pending.Enqueue(command);
        }
    }
}
