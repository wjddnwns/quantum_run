using UnityEngine;
using UnityEngine.Rendering;

namespace QuantumCrossy.Gates
{
    /// <summary>
    /// 반투명 가산 레이저. 색은 밝기로 숨 쉬고, 줄무늬는 위로 흐른다. 그림자는 그리지 않는다.
    /// </summary>
    public sealed class GateLaser : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

        [SerializeField] Color glow = new Color(0.2f, 0.95f, 1f, 0.8f);
        [SerializeField] float pulseSpeed = 2.4f;
        [SerializeField] float pulseAmount = 0.28f;
        [SerializeField] float scrollSpeed = 0.45f;
        [SerializeField] Vector2 textureScale = new Vector2(1f, 4f);

        Renderer target;
        MaterialPropertyBlock block;
        float phase;

        void Awake()
        {
            target = GetComponent<Renderer>();
            if (target != null)
            {
                target.shadowCastingMode = ShadowCastingMode.Off;
                target.receiveShadows = false;
            }

            block = new MaterialPropertyBlock();
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        void Update()
        {
            if (target == null)
                return;

            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed + phase);
            float pulse = 1f - pulseAmount * (1f - wave);
            Color color = glow * pulse;
            color.a = glow.a * pulse;

            float offset = Mathf.Repeat(Time.time * scrollSpeed + phase * 0.1f, 1f);
            target.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            block.SetVector(BaseMapStId, new Vector4(textureScale.x, textureScale.y, 0f, offset));
            target.SetPropertyBlock(block);
        }
    }
}
