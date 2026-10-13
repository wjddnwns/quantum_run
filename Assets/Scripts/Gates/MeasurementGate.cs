using UnityEngine;

namespace QuantumCrossy.Gates
{
    /// <summary>
    /// 진행 방향(X)으로 움직이는 측정체. 레이저 막은 얇은 판이고, 기둥 둘은 그 양 끝 밖이다.
    /// 판정은 막과 기둥 부피를 따로 본다. 위치는 트랜스폼에 직접 쓴다.
    /// 리지드바디 보간을 쓰면 양 끝 순간이동이 화면 한가운데를 가로지르는 궤적으로 그려진다.
    /// </summary>
    public sealed class MeasurementGate : MonoBehaviour
    {
        Renderer[] renderers = System.Array.Empty<Renderer>();
        Vector3 hitHalf;
        Vector3 hitOffset;
        Vector3 pillarHalf;
        float pillarCenterY;
        float pillarCenterZ;
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
        /// 막 양 끝 기둥. 중심은 게이트 원점 기준이고, 기둥 높이 전체를 판정에 넣는다.
        /// </summary>
        public void SetPillars(Vector3 half, float centerY, float centerZ)
        {
            pillarHalf = half;
            pillarCenterY = centerY;
            pillarCenterZ = centerZ;
            hasPillars = half.x > 0f && half.y > 0f && half.z > 0f && centerZ > 0f;
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
            Vector3 center = gate;
            center.y += pillarCenterY;
            center.z += pillarCenterZ;
            if (SphereHits(player, radius, center, pillarHalf))
                return true;
            center.z = gate.z - pillarCenterZ;
            return SphereHits(player, radius, center, pillarHalf);
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
