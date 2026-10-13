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
            Vector3 half = HalfExtents;

            float gateMove = wrapped ? 0f : Mathf.Abs(frameTo.x - frameFrom.x);
            float playerMove = Vector3.Distance(playerFrom, playerTo);
            int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(gateMove, playerMove) / 0.05f), 1, 16);
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 1f : i / (float)steps;
                Vector3 player = Vector3.Lerp(playerFrom, playerTo, t);
                Vector3 gate = wrapped ? frameTo : Vector3.Lerp(frameFrom, frameTo, t);
                if (SphereHits(player, radius, gate, half))
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
            if (rend == null)
                return;

            bool show = Mathf.Abs(x) <= visibleHalf;
            if (rend.enabled != show)
                rend.enabled = show;
        }

        Vector3 HalfExtents =>
            new Vector3(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.y),
                Mathf.Abs(transform.lossyScale.z)) * 0.5f;

        static bool SphereHits(Vector3 center, float radius, Vector3 boxCenter, Vector3 half)
        {
            Vector3 closest = new Vector3(
                Mathf.Clamp(center.x, boxCenter.x - half.x, boxCenter.x + half.x),
                Mathf.Clamp(center.y, boxCenter.y - half.y, boxCenter.y + half.y),
                Mathf.Clamp(center.z, boxCenter.z - half.z, boxCenter.z + half.z));
            return (center - closest).sqrMagnitude <= radius * radius;
        }

        void OnDestroy()
        {
            if (material != null)
                Destroy(material);
        }
    }
}
