using UnityEngine;

namespace QuantumCrossy.Gates
{
    /// <summary>
    /// X로 움직이는 측정체. 레이저는 이동 방향(X)으로 얇고 레인(Z)으로 길며, 기둥은 막의 앞뒤 양 끝이다.
    /// 판정은 막과 기둥 부피를 따로 본다. 위치는 트랜스폼에 직접 쓴다.
    /// 리지드바디 보간을 쓰면 양 끝 순간이동이 화면 한가운데를 가로지르는 궤적으로 그려진다.
    /// </summary>
    public sealed class MeasurementGate : MonoBehaviour
    {
        Renderer[] renderers = System.Array.Empty<Renderer>();
        Vector3 hitHalf;
        Vector3 hitOffset;
        Vector3 pillarHalf;
        Vector3 pillarCenter;
        bool hasPillars;
        Vector3 frameFrom;
        Vector3 frameTo;
        Vector3 previous;
        float signedSpeed;
        float travelHalf;
        float visibleHalf;
        int capturedFrame = -1;
        bool hasSample;
        bool wrapped;

        public float LaneZ { get; private set; }

        public void Initialize(float laneZ, float signedSpeed, float travelHalf, float visibleHalf, Vector3 hitHalf, Vector3 hitOffset)
        {
            LaneZ = laneZ;
            this.signedSpeed = signedSpeed;
            this.travelHalf = travelHalf;
            this.visibleHalf = Mathf.Clamp(visibleHalf, 1f, Mathf.Max(1f, travelHalf - 0.5f));
            this.hitHalf = hitHalf;
            this.hitOffset = hitOffset;

            Collider[] cols = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
                cols[i].enabled = false;

            renderers = GetComponentsInChildren<Renderer>(true);
            previous = transform.position;
            frameFrom = previous;
            frameTo = previous;
            hasSample = true;
            ApplyVisibility(previous.x);
        }

        /// <summary>
        /// 막 한쪽 기둥의 로컬 중심. 반대쪽은 X를 뒤집어 같은 크기로 판정한다.
        /// </summary>
        public void SetPillars(Vector3 half, Vector3 localCenter)
        {
            pillarHalf = half;
            pillarCenter = localCenter;
            hasPillars = half.x > 0f && half.y > 0f && half.z > 0f;
        }

        void Update()
        {
            if (!enabled)
                return;

            Vector3 pos = transform.position;
            pos.x += signedSpeed * Time.deltaTime;
            pos.x = WrapX(pos.x);
            transform.position = pos;
            ApplyVisibility(pos.x);
        }

        /// <summary>
        /// 같은 시각의 플레이어 위치와 판 위치를 나눠, 구체가 판 부피에 들어갔는지 본다.
        /// </summary>
        public bool Hits(Vector3 playerFrom, Vector3 playerTo, float radius)
        {
            CaptureFrame();

            float gateMove = wrapped ? 0f : Mathf.Abs(frameTo.x - frameFrom.x);
            float playerMove = Vector3.Distance(playerFrom, playerTo);
            int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(gateMove, playerMove) / 0.05f), 1, 16);
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 1f : i / (float)steps;
                Vector3 player = Vector3.Lerp(playerFrom, playerTo, t);
                Vector3 gate = wrapped ? frameTo : Vector3.Lerp(frameFrom, frameTo, t);
                if (SphereHits(player, radius, gate + hitOffset, hitHalf))
                    return true;
                if (hasPillars && PillarHits(player, radius, gate))
                    return true;
            }

            return false;
        }

        void CaptureFrame()
        {
            if (capturedFrame == Time.frameCount)
                return;

            capturedFrame = Time.frameCount;
            Vector3 now = transform.position;
            if (!hasSample)
                previous = now;
            frameFrom = previous;
            frameTo = now;
            wrapped = Mathf.Abs(frameTo.x - frameFrom.x) > 2f;
            previous = now;
            hasSample = true;
        }

        float WrapX(float x)
        {
            float span = travelHalf * 2f;
            if (span < 0.01f)
                return x;

            float shifted = x + travelHalf;
            shifted -= Mathf.Floor(shifted / span) * span;
            return shifted - travelHalf;
        }

        void ApplyVisibility(float x)
        {
            bool show = Mathf.Abs(x) <= visibleHalf;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null && renderer.enabled != show)
                    renderer.enabled = show;
            }
        }

        bool PillarHits(Vector3 player, float radius, Vector3 gate)
        {
            if (SphereHits(player, radius, gate + pillarCenter, pillarHalf))
                return true;
            Vector3 other = new Vector3(-pillarCenter.x, pillarCenter.y, -pillarCenter.z);
            return SphereHits(player, radius, gate + other, pillarHalf);
        }

        static bool SphereHits(Vector3 center, float radius, Vector3 boxCenter, Vector3 half)
        {
            Vector3 closest = new Vector3(
                Mathf.Clamp(center.x, boxCenter.x - half.x, boxCenter.x + half.x),
                Mathf.Clamp(center.y, boxCenter.y - half.y, boxCenter.y + half.y),
                Mathf.Clamp(center.z, boxCenter.z - half.z, boxCenter.z + half.z));
            return (center - closest).sqrMagnitude <= radius * radius;
        }
    }
}
