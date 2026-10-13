using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace QuantumCrossy.Visuals
{
    /// <summary>
    /// 가져온 모델에 머티리얼을 입히고, 윗면이 아래를 보면 감싼 면을 뒤집는다.
    /// 반투명은 그림자를 끄고, 겹치는 조각은 아래쪽부터 그린다.
    /// </summary>
    public sealed class FittedModel : MonoBehaviour
    {
        static readonly Dictionary<int, Mesh> FacingCache = new Dictionary<int, Mesh>();

        [SerializeField] Material material = null;
        [SerializeField] bool transparent = false;
        [SerializeField] bool castShadows = false;

        readonly List<Material> owned = new List<Material>(2);

        void Awake()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            System.Array.Sort(renderers, CompareHeight);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter != null)
                    filter.sharedMesh = FaceTopUp(filter.sharedMesh);

                if (material != null)
                {
                    if (transparent && renderers.Length > 1)
                    {
                        Material copy = new Material(material);
                        copy.renderQueue = 3000 + i;
                        renderer.sharedMaterial = copy;
                        owned.Add(copy);
                    }
                    else
                    {
                        renderer.sharedMaterial = material;
                    }
                }

                renderer.shadowCastingMode = !transparent && castShadows
                    ? ShadowCastingMode.On
                    : ShadowCastingMode.Off;
                renderer.receiveShadows = !transparent;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        void OnDestroy()
        {
            for (int i = 0; i < owned.Count; i++)
            {
                if (owned[i] != null)
                    Destroy(owned[i]);
            }
        }

        static int CompareHeight(Renderer a, Renderer b)
        {
            float ay = a == null ? 0f : a.bounds.center.y;
            float by = b == null ? 0f : b.bounds.center.y;
            return ay.CompareTo(by);
        }

        /// <summary>
        /// 가장 높은 면이 아래를 향하면 모든 면을 뒤집고 노멀 부호를 맞춘다.
        /// 원본 메시는 바꾸지 않는다.
        /// </summary>
        public static Mesh FaceTopUp(Mesh source)
        {
            if (source == null)
                return null;

            int id = source.GetInstanceID();
            if (FacingCache.TryGetValue(id, out Mesh cached))
                return cached;

            Mesh result = source;
            try
            {
                Vector3[] vertices = source.vertices;
                if (vertices == null || vertices.Length == 0 || source.subMeshCount == 0)
                {
                    FacingCache[id] = source;
                    return source;
                }

                int[] first = source.GetTriangles(0);
                float bestY = float.NegativeInfinity;
                float bestNy = 1f;
                for (int i = 0; i + 2 < first.Length; i += 3)
                {
                    Vector3 a = vertices[first[i]];
                    Vector3 b = vertices[first[i + 1]];
                    Vector3 c = vertices[first[i + 2]];
                    float y = (a.y + b.y + c.y) / 3f;
                    if (y <= bestY)
                        continue;
                    bestY = y;
                    bestNy = Vector3.Cross(b - a, c - a).y;
                }

                if (bestNy < 0f)
                {
                    Mesh copy = Instantiate(source);
                    copy.name = source.name + " Facing";
                    for (int sub = 0; sub < copy.subMeshCount; sub++)
                    {
                        int[] triangles = copy.GetTriangles(sub);
                        for (int i = 0; i + 2 < triangles.Length; i += 3)
                        {
                            int swap = triangles[i + 1];
                            triangles[i + 1] = triangles[i + 2];
                            triangles[i + 2] = swap;
                        }

                        copy.SetTriangles(triangles, sub);
                    }

                    Vector3[] normals = copy.normals;
                    if (normals != null && normals.Length == copy.vertexCount)
                    {
                        for (int i = 0; i < normals.Length; i++)
                            normals[i] = -normals[i];
                        copy.normals = normals;
                    }
                    else
                    {
                        copy.RecalculateNormals();
                    }

                    result = copy;
                }
            }
            catch (System.Exception)
            {
                result = source;
            }

            FacingCache[id] = result;
            return result;
        }
    }
}
