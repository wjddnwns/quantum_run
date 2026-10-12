using System.Collections.Generic;
using UnityEngine;

namespace QuantumCrossy.Gates
{
    /// <summary>
    /// 얇은 판이 홉 도중에 몸 아래를 지날 타이밍이면, 그 홉은 판을 넘는 높은 궤적이다.
    /// 맞아도 무적이 되지는 않는다. 판에 닿으면 측정이다.
    /// </summary>
    public static class QuantumJumpRule
    {
        public static bool ShouldHopOver(
            Vector3 fromWorld,
            Vector3 targetWorld,
            IReadOnlyList<MeasurementGate> gates,
            float windowMin,
            float windowMax,
            float playerRadius)
        {
            if (gates == null || gates.Count == 0)
                return false;

            float sampleX = Mathf.Lerp(fromWorld.x, targetWorld.x, 0.5f);
            for (int i = 0; i < gates.Count; i++)
            {
                MeasurementGate gate = gates[i];
                if (gate == null || !gate.ReachesHop(fromWorld, targetWorld, playerRadius))
                    continue;

                float time = gate.TimeUntilXOverlap(sampleX, playerRadius);
                if (time >= windowMin && time <= windowMax)
                    return true;
            }

            return false;
        }
    }
}
