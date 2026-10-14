using System.Collections.Generic;
using UnityEngine;
using QuantumCrossy.Gates;

namespace QuantumCrossy.Lanes
{
    public sealed class Lane : MonoBehaviour
    {
        readonly List<MeasurementGate> gates = new List<MeasurementGate>(3);
        readonly HashSet<int> blocked = new HashSet<int>();

        public int Row { get; private set; }
        public bool IsGateLane { get; private set; }
        public IReadOnlyList<MeasurementGate> Gates => gates;

        public bool Blocks(int x)
        {
            return blocked.Contains(x);
        }

        public void Block(int x)
        {
            blocked.Add(x);
        }

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
    }
}
