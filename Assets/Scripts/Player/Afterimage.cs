using UnityEngine;
using QuantumCrossy.Visuals;

namespace QuantumCrossy.Player
{
    /// <summary>
    /// 양자 도약 출발 자리에 잠깐 남는 잔상. 줄어들다 사라진다.
    /// </summary>
    public sealed class Afterimage : MonoBehaviour
    {
        Material material;
        Vector3 baseScale;
        float baseY;
        float life;
        float duration;

        public static void Spawn(Vector3 position, Quaternion rotation, Vector3 scale, float duration, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "QuantumGhost";
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = scale;
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            var ghost = go.AddComponent<Afterimage>();
            ghost.Begin(duration, color);
        }

        void Begin(float duration, Color color)
        {
            this.duration = Mathf.Max(0.05f, duration);
            life = this.duration;
            baseScale = transform.localScale;
            baseY = transform.position.y;

            Renderer rend = GetComponent<Renderer>();
            rend.shadowCastingMode = ShadowCastingMode.Off;
            material = PrimitiveMaterials.Create(color, 1.6f);
            rend.sharedMaterial = material;
        }

        void Update()
        {
            life -= Time.deltaTime;
            float u = duration <= 0f ? 0f : Mathf.Clamp01(life / duration);
            transform.localScale = baseScale * Mathf.Lerp(0.15f, 1f, u * u);
            Vector3 pos = transform.position;
            pos.y = baseY + (1f - u) * 0.16f;
            transform.position = pos;

            if (life > 0f)
                return;

            if (material != null)
                Destroy(material);
            Destroy(gameObject);
        }
    }
}
