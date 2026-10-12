using UnityEngine;
using QuantumCrossy.Visuals;

namespace QuantumCrossy.Gates
{
    /// <summary>
    /// 레인을 가로지르는 얇은 측정 게이트. 위상을 통과 중이 아닐 때 닿으면 측정이 일어난다.
    /// </summary>
    public sealed class MeasurementGate : MonoBehaviour
    {
        Rigidbody body;
        Material material;
        Color baseColor;
        float signedSpeed;
        float travelHalf;
        float length;
        bool telegraph;

        public float LaneZ { get; private set; }
        public float Length => length;
        public float AbsSpeed => Mathf.Abs(signedSpeed);

        public void Initialize(float laneZ, float signedSpeed, float travelHalf, float length, Color color)
        {
            LaneZ = laneZ;
            this.signedSpeed = signedSpeed;
            this.travelHalf = travelHalf;
            this.length = length;
            baseColor = color;

            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            material = PrimitiveMaterials.Create(color, 1.7f);
            GetComponent<Renderer>().sharedMaterial = material;
        }

        public void SetTelegraph(bool on)
        {
            if (telegraph == on || material == null)
                return;

            telegraph = on;
            Color color = on ? new Color(1f, 0.95f, 0.75f) : baseColor;
            PrimitiveMaterials.Apply(material, color, on ? 3.4f : 1.7f);
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

        public bool IsApproaching(float targetX, float radius)
        {
            float delta = targetX - transform.position.x;
            float reach = length * 0.5f + radius;
            if (Mathf.Abs(delta) <= reach)
                return false;
            return signedSpeed * delta > 0f;
        }

        public float TimeUntilOverlap(float targetX, float radius)
        {
            float reach = length * 0.5f + radius;
            float delta = targetX - transform.position.x;
            float distance = Mathf.Abs(delta);
            if (distance <= reach)
                return 0f;
            if (signedSpeed * delta <= 0f || AbsSpeed < 0.01f)
                return float.PositiveInfinity;
            return (distance - reach) / AbsSpeed;
        }

        public float PassDuration(float radius)
        {
            return (length + radius * 2f) / Mathf.Max(0.01f, AbsSpeed);
        }

        void OnDestroy()
        {
            if (material != null)
                Destroy(material);
        }
    }
}
