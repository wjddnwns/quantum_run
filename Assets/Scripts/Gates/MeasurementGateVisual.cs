using UnityEngine;

namespace QuantumCrossy.Gates
{
    /// <summary>
    /// 측정 게이트. 카메라는 +Z를 보므로 레이저는 Z로 얇고 X로 길다.
    /// 기둥은 그 막의 좌우(X) 양 끝 밖에 선다.
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

            // depth가 눈에 보이는 폭(X), thickness가 진행 방향 두께(Z)다.
            float span = depth;
            float thin = thickness;
            if (laser != null)
            {
                laser.localPosition = new Vector3(0f, height * 0.5f, 0f);
                laser.localScale = new Vector3(span, height, thin);
            }

            float pillarHeight = Mathf.Max(PillarMinHeight, height + PillarExtraHeight);
            float centerX = span * 0.5f + PillarDepth * 0.5f;
            Vector3 pillarScale = new Vector3(PillarDepth, pillarHeight, PillarWidth);
            Place(pillarPositive, centerX, pillarHeight, pillarScale);
            Place(pillarNegative, -centerX, pillarHeight, pillarScale);

            LaserHalf = new Vector3(span, height, thin) * 0.5f;
            LaserOffset = new Vector3(0f, height * 0.5f, 0f);
            PillarHalf = pillarScale * 0.5f;
            PillarCenter = new Vector3(centerX, pillarHeight * 0.5f, 0f);
        }

        static void Place(Transform pillar, float x, float height, Vector3 scale)
        {
            if (pillar == null)
                return;

            pillar.localPosition = new Vector3(x, height * 0.5f, 0f);
            pillar.localRotation = Quaternion.identity;
            pillar.localScale = scale;
        }
    }
}
