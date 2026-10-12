using UnityEngine;

namespace QuantumCrossy.Visuals
{
    /// <summary>
    /// 바닥 높이에 붙는 둥근 그림자. 본체가 떠도 XZ만 따라간다.
    /// </summary>
    public sealed class BlobShadow : MonoBehaviour
    {
        static Material shared;

        Transform follow;
        Renderer hideWith;
        Renderer rend;

        public static void Attach(Transform follow, float width, float depth, Renderer hideWith)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "BlobShadow";
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            go.transform.localScale = new Vector3(Mathf.Max(0.08f, width), 0.008f, Mathf.Max(0.08f, depth));
            var blob = go.AddComponent<BlobShadow>();
            blob.follow = follow;
            blob.hideWith = hideWith;
            blob.rend = go.GetComponent<Renderer>();
            blob.rend.sharedMaterial = Shared();
            blob.rend.shadowCastingMode = ShadowCastingMode.Off;
            blob.rend.receiveShadows = false;
            blob.Snap();
        }

        void LateUpdate()
        {
            if (follow == null)
            {
                Destroy(gameObject);
                return;
            }

            Snap();
        }

        void Snap()
        {
            Vector3 pos = follow.position;
            pos.y = 0.016f;
            transform.position = pos;
            transform.rotation = Quaternion.identity;

            if (rend == null)
                return;

            bool show = hideWith == null || hideWith.enabled;
            if (rend.enabled != show)
                rend.enabled = show;
        }

        static Material Shared()
        {
            if (shared != null)
                return shared;

            shared = PrimitiveMaterials.Create(new Color(0.04f, 0.045f, 0.06f), 0f);
            return shared;
        }
    }
}
