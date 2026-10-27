using UnityEngine;

namespace QuantumCrossy.Gates
{
    /// <summary>
    /// 측정 게이트. 레이저는 좌우로 움직이는 방향(X)으로 얇고, 레인(Z)을 길게 가로막는다.
    /// 기둥은 그 막의 앞뒤(Z) 양 끝 밖에 선다. 판정 상자는 메시와 같은 크기와 위치다.
    /// </summary>
    public sealed class MeasurementGateVisual : MonoBehaviour
    {
        public const float PillarWidth = 0.22f;
        public const float PillarDepth = 0.16f;
        const float PillarExtraHeight = 0.24f;
        const float PillarMinHeight = 1.02f;

        [SerializeField] Transform laser = null;
        [SerializeField] Transform pillarPositive = null;
        [SerializeField] Transform pillarNegative = null;

        public Vector3 LaserHalf { get; private set; }
        public Vector3 LaserOffset { get; private set; }
        public Vector3 PillarHalf { get; private set; }
        public Vector3 PillarCenter { get; private set; }

        public void Fit(float thickness, float height, float depth)
        {
            thickness = Mathf.Max(0.02f, thickness);
            height = Mathf.Max(0.02f, height);
            depth = Mathf.Max(0.02f, depth);

            if (laser != null)
            {
                laser.localPosition = new Vector3(0f, height * 0.5f, 0f);
                laser.localScale = new Vector3(thickness, height, depth);
            }

            float pillarHeight = Mathf.Max(PillarMinHeight, height + PillarExtraHeight);
            float centerZ = depth * 0.5f + PillarDepth * 0.5f;
            Vector3 pillarScale = new Vector3(PillarWidth, pillarHeight, PillarDepth);
            Place(pillarPositive, centerZ, pillarHeight, pillarScale);
            Place(pillarNegative, -centerZ, pillarHeight, pillarScale);

            LaserHalf = new Vector3(thickness, height, depth) * 0.5f;
            LaserOffset = new Vector3(0f, height * 0.5f, 0f);
            PillarHalf = pillarScale * 0.5f;
            PillarCenter = new Vector3(0f, pillarHeight * 0.5f, centerZ);
        }

        static void Place(Transform pillar, float z, float height, Vector3 scale)
        {
            if (pillar == null)
                return;

            pillar.localPosition = new Vector3(0f, height * 0.5f, z);
            pillar.localRotation = Quaternion.identity;
            pillar.localScale = scale;
        }
    }
}
