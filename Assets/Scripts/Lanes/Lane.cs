using System.Collections.Generic;
using UnityEngine;
using QuantumCrossy.Gates;

namespace QuantumCrossy.Lanes
{
    public sealed class Lane : MonoBehaviour
    {
        readonly List<MeasurementGate> gates = new List<MeasurementGate>(3);

        public int Row { get; private set; }
        public bool IsGateLane { get; private set; }
        public IReadOnlyList<MeasurementGate> Gates => gates;

        public void Setup(int row, bool gateLane)
        {
            Row = row;
            IsGateLane = gateLane;
            name = gateLane ? "GateLane_" + row : "SafeLane_" + row;
        }

        public void AddGate(MeasurementGate gate)
        {
            if (gate != null)
                gates.Add(gate);
        }

        public void RetireGates()
        {
            for (int i = 0; i < gates.Count; i++)
            {
                if (gates[i] != null)
                    gates[i].Retire();
            }

            gates.Clear();
        }
    }
}
