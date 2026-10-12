using System.Collections.Generic;
using UnityEngine;
using QuantumCrossy.Gates;

namespace QuantumCrossy.Player
{
    /// <summary>
    /// 양자 도약으로 통과 중인 게이트. 그 게이트가 몸을 지나갈 때까지만 측정을 무시한다.
    /// </summary>
    public sealed class PhaseTracker
    {
        struct Entry
        {
            public MeasurementGate Gate;
            public float Until;
        }

        readonly List<Entry> entries = new List<Entry>(4);

        public void Clear()
        {
            entries.Clear();
        }

        public void Add(MeasurementGate gate, float duration)
        {
            if (gate == null)
                return;
            entries.Add(new Entry { Gate = gate, Until = Time.time + duration });
        }

        public bool IsPhasing(MeasurementGate gate)
        {
            if (gate == null)
                return false;

            float now = Time.time;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Gate == gate && now <= entries[i].Until)
                    return true;
            }

            return false;
        }

        public bool AnyActive
        {
            get
            {
                float now = Time.time;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (now <= entries[i].Until)
                        return true;
                }

                return false;
            }
        }
    }
}
