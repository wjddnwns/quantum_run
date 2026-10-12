using System.Collections.Generic;
using UnityEngine;

namespace QuantumCrossy.Gates
{
    /// <summary>
    /// 다가오는 게이트 쪽으로 이동했고, 접촉까지 남은 시간이 창 안이면 양자 도약이다.
    /// 성공하면 그 게이트가 지나갈 때까지 그 게이트의 측정을 무시한다.
    /// </summary>
    public static class QuantumJumpRule
    {
        public readonly struct Match
        {
            public readonly MeasurementGate Gate;
            public readonly float PhaseDuration;

            public Match(MeasurementGate gate, float phaseDuration)
            {
                Gate = gate;
                PhaseDuration = phaseDuration;
            }
        }

        public static bool TryMatch(
            Vector3 fromWorld,
            Vector3 targetWorld,
            IReadOnlyList<MeasurementGate> gates,
            float windowMin,
            float windowMax,
            float playerRadius,
            float phasePadding,
            List<Match> results)
        {
            results.Clear();
            if (gates == null || gates.Count == 0)
                return false;

            Vector3 move = targetWorld - fromWorld;
            move.y = 0f;
            if (move.sqrMagnitude < 0.0001f)
                return false;

            const float laneEpsilon = 0.25f;
            for (int i = 0; i < gates.Count; i++)
            {
                MeasurementGate gate = gates[i];
                if (gate == null)
                    continue;

                bool enteringLane = Mathf.Abs(targetWorld.z - gate.LaneZ) <= laneEpsilon
                    && Mathf.Abs(fromWorld.z - gate.LaneZ) > laneEpsilon;
                bool lateralToward = Mathf.Abs(fromWorld.z - gate.LaneZ) <= laneEpsilon
                    && Mathf.Abs(move.x) > 0.01f
                    && Mathf.Sign(move.x) == Mathf.Sign(gate.transform.position.x - fromWorld.x);
                if (!enteringLane && !lateralToward)
                    continue;
                if (!gate.IsApproaching(targetWorld.x, playerRadius))
                    continue;

                float time = gate.TimeUntilOverlap(targetWorld.x, playerRadius);
                if (time < windowMin || time > windowMax)
                    continue;

                float phase = time + gate.PassDuration(playerRadius) + phasePadding;
                results.Add(new Match(gate, phase));
            }

            return results.Count > 0;
        }
    }
}
