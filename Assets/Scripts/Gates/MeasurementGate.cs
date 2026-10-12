using UnityEngine;
using QuantumCrossy.Visuals;

namespace QuantumCrossy.Gates
{
    /// <summary>
    /// 진행 방향(X)으로 얇은 판. 판의 실제 부피와 겹치면 측정이다.
    /// </summary>
    public sealed class MeasurementGate : MonoBehaviour
    {
        Rigidbody body;
        Material material;
        Vector3 frameFrom;
        Vector3 frameTo;
        Vector3 previous;
        float signedSpeed;
        float travelHalf;
        int capturedFrame = -1;
        bool hasSample;
        bool wrapped;

        public float LaneZ { get; private set; }
        public float AbsSpeed => Mathf.Abs(signedSpeed);

        public void Initialize(float laneZ, float signedSpeed, float travelHalf, Color color)
        {
            LaneZ = laneZ;
            this.signedSpeed = signedSpeed;
            this.travelHalf = travelHalf;

            Collider col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            material = PrimitiveMaterials.Create(color, 1.7f);
            GetComponent<Renderer>().sharedMaterial = material;
            previous = transform.position;
            frameFrom = previous;
            frameTo = previous;
            hasSample = true;
        }

        void FixedUpdate()
        {
            if (body == null)
                return;

            Vector3 pos = body.position;
            pos.x += signedSpeed * Time.fixedDeltaTime;
            float span = travelHalf * 2f;
            if (pos.x > travelHalf)
                pos.x -= span;
            else if (pos.x < -travelHalf)
                pos.x += span;
            body.MovePosition(pos);
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
                if (SphereHits(player, radius, gate, half))
                {
                    hit = true;
                    cleared = false;
                    return;
                }

                if (PassedUnder(player, radius, gate, half))
                    cleared = true;
            }
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

        float ThicknessHalf => Mathf.Abs(transform.lossyScale.x) * 0.5f;
        float DepthHalf => Mathf.Abs(transform.lossyScale.z) * 0.5f;
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
