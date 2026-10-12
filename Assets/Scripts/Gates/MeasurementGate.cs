using UnityEngine;
using UnityEngine.Rendering;
using QuantumCrossy.Visuals;

namespace QuantumCrossy.Gates
{
    /// <summary>
    /// 진행 방향(X)으로 얇은 판. 판의 실제 부피와 겹치면 측정이다.
    /// 위치는 트랜스폼에 직접 쓴다. 리지드바디 보간을 쓰면 양 끝 순간이동이
    /// 화면 한가운데를 가로지르는 궤적으로 그려진다.
    /// </summary>
    public sealed class MeasurementGate : MonoBehaviour
    {
        Renderer rend;
        Material material;
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
        public float AbsSpeed => Mathf.Abs(signedSpeed);

        public void Initialize(float laneZ, float signedSpeed, float travelHalf, float visibleHalf, Color color)
        {
            LaneZ = laneZ;
            this.signedSpeed = signedSpeed;
            this.travelHalf = travelHalf;
            this.visibleHalf = Mathf.Clamp(visibleHalf, 1f, Mathf.Max(1f, travelHalf - 0.5f));

            Collider col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            rend = GetComponent<Renderer>();
            material = PrimitiveMaterials.Create(color, 1.7f);
            rend.sharedMaterial = material;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            previous = transform.position;
            frameFrom = previous;
            frameTo = previous;
            hasSample = true;
            ApplyVisibility(previous.x);
        }

        void Update()
        {
            Vector3 pos = transform.position;
            pos.x += signedSpeed * Time.deltaTime;
            pos.x = WrapX(pos.x);
            transform.position = pos;
            ApplyVisibility(pos.x);
        }

        public float TimeUntilXOverlap(float targetX, float radius)
        {
            float reach = ThicknessHalf + radius;
            float delta = targetX - transform.position.x;
            float distance = Mathf.Abs(delta);
            if (distance <= reach)
                return 0f;
            if (signedSpeed * delta <= 0f || AbsSpeed < 0.01f)
                return float.PositiveInfinity;
            return (distance - reach) / AbsSpeed;
        }

        public bool ReachesHop(Vector3 fromWorld, Vector3 toWorld, float radius)
        {
            float pad = DepthHalf + radius;
            float minZ = Mathf.Min(fromWorld.z, toWorld.z) - pad;
            float maxZ = Mathf.Max(fromWorld.z, toWorld.z) + pad;
            return LaneZ >= minZ && LaneZ <= maxZ;
        }

        /// <summary>
        /// 같은 시각의 플레이어 위치와 판 위치를 나눠 비교한다.
        /// hit는 구체가 판 부피 안에 들어간 것, cleared는 판이 발 아래로 지나간 것이다.
        /// </summary>
        public void Evaluate(Vector3 playerFrom, Vector3 playerTo, float radius, out bool hit, out bool cleared)
        {
            CaptureFrame();
            hit = false;
            cleared = false;
            Vector3 half = HalfExtents;

            float gateMove = wrapped ? 0f : Mathf.Abs(frameTo.x - frameFrom.x);
            float playerMove = Vector3.Distance(playerFrom, playerTo);
            int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(gateMove, playerMove) / 0.05f), 1, 16);
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 1f : i / (float)steps;
                Vector3 player = Vector3.Lerp(playerFrom, playerTo, t);
                Vector3 gate = wrapped ? frameTo : Vector3.Lerp(frameFrom, frameTo, t);
                if (Reject(player, radius, gate, half, ref hit, ref cleared))
                    return;
            }
        }

        /// <summary>
        /// 양자 도약은 한 프레임에 건너뛰므로, 예전 홉 시간 동안의 논리 궤적과
        /// 그 사이 판의 이동을 한 번에 검사한다. 줄바꿈 지점은 보간하지 않는다.
        /// </summary>
        public void EvaluateOverTime(
            Vector3 playerFrom,
            Vector3 playerTo,
            float radius,
            float arcHeight,
            float duration,
            out bool hit,
            out bool cleared)
        {
            hit = false;
            cleared = false;
            Vector3 half = HalfExtents;
            float span = Mathf.Max(0.05f, duration);
            int steps = Mathf.Clamp(Mathf.CeilToInt(span / 0.016f), 6, 32);
            float originX = transform.position.x;
            float y = transform.position.y;
            float z = transform.position.z;

            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 1f : i / (float)steps;
                float u = Mathf.SmoothStep(0f, 1f, t);
                Vector3 player = Vector3.Lerp(playerFrom, playerTo, u);
                player.y = Mathf.Lerp(playerFrom.y, playerTo.y, u) + Mathf.Sin(u * Mathf.PI) * arcHeight;
                float x = WrapX(originX + signedSpeed * (t * span));
                var gate = new Vector3(x, y, z);
                if (Reject(player, radius, gate, half, ref hit, ref cleared))
                {
                    NoteSample();
                    return;
                }
            }

            NoteSample();
        }

        /// <summary>
        /// 양자 도약으로 지나간 판을, 몸이 있는 칸에서 빠져나갈 때까지 구분한다.
        /// 멀리 떨어지거나 진행 방향 뒤쪽으로 나가면 유예를 끝낸다.
        /// </summary>
        public bool ShouldReleasePhase(Vector3 player, float bodyReach)
        {
            if (Mathf.Abs(LaneZ - player.z) > 1.5f)
                return true;

            float dx = transform.position.x - player.x;
            float abs = Mathf.Abs(dx);
            if (abs > 3f)
                return true;
            if (abs <= bodyReach)
                return false;
            return dx * signedSpeed > 0f;
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

        void NoteSample()
        {
            Vector3 now = transform.position;
            previous = now;
            frameFrom = now;
            frameTo = now;
            wrapped = false;
            hasSample = true;
            capturedFrame = Time.frameCount;
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
            if (rend == null)
                return;

            bool show = Mathf.Abs(x) <= visibleHalf;
            if (rend.enabled != show)
                rend.enabled = show;
        }

        float ThicknessHalf => Mathf.Abs(transform.lossyScale.x) * 0.5f;
        float DepthHalf => Mathf.Abs(transform.lossyScale.z) * 0.5f;
        Vector3 HalfExtents =>
            new Vector3(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.y),
                Mathf.Abs(transform.lossyScale.z)) * 0.5f;

        static bool Reject(Vector3 center, float radius, Vector3 boxCenter, Vector3 half, ref bool hit, ref bool cleared)
        {
            if (SphereHits(center, radius, boxCenter, half))
            {
                hit = true;
                cleared = false;
                return true;
            }

            if (PassedUnder(center, radius, boxCenter, half))
                cleared = true;
            return false;
        }

        static bool SphereHits(Vector3 center, float radius, Vector3 boxCenter, Vector3 half)
        {
            Vector3 closest = new Vector3(
                Mathf.Clamp(center.x, boxCenter.x - half.x, boxCenter.x + half.x),
                Mathf.Clamp(center.y, boxCenter.y - half.y, boxCenter.y + half.y),
                Mathf.Clamp(center.z, boxCenter.z - half.z, boxCenter.z + half.z));
            return (center - closest).sqrMagnitude <= radius * radius;
        }

        static bool PassedUnder(Vector3 center, float radius, Vector3 boxCenter, Vector3 half)
        {
            float top = boxCenter.y + half.y;
            if (center.y - radius <= top + 0.02f)
                return false;
            float dx = Mathf.Abs(center.x - boxCenter.x);
            float dz = Mathf.Abs(center.z - boxCenter.z);
            return dx <= half.x + radius && dz <= half.z + radius;
        }

        void OnDestroy()
        {
            if (material != null)
                Destroy(material);
        }
    }
}
