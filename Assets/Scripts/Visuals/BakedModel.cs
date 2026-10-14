using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace QuantumCrossy.Visuals
{
    /// <summary>
    /// FBX 임포트 계층 대신, 버텍스를 직접 구운 메시를 쓴다.
    /// 프리팹 파일 ID가 실제 노드 이름과 달라도 크기와 노멀이 유지된다.
    /// </summary>
    public sealed class BakedModel : MonoBehaviour
    {
        [SerializeField] string modelKey = "";
        [SerializeField] Material material = null;

        static Dictionary<string, Mesh[]> shared;
        bool built;

        void Awake()
        {
            if (built)
                return;
            built = true;

            Mesh[] meshes = Meshes(modelKey);
            if (meshes == null || meshes.Length == 0)
                return;

            if (meshes.Length == 1)
            {
                Assign(gameObject, meshes[0]);
                return;
            }

            for (int i = 0; i < meshes.Length; i++)
            {
                var child = new GameObject(meshes[i].name);
                child.transform.SetParent(transform, false);
                Assign(child, meshes[i]);
            }
        }

        void Assign(GameObject target, Mesh mesh)
        {
            MeshFilter filter = target.GetComponent<MeshFilter>();
            if (filter == null)
                filter = target.AddComponent<MeshFilter>();
            MeshRenderer renderer = target.GetComponent<MeshRenderer>();
            if (renderer == null)
                renderer = target.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            if (material != null)
                renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        static Mesh[] Meshes(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            if (shared == null)
                shared = Load();
            shared.TryGetValue(key, out Mesh[] meshes);
            return meshes;
        }

        static Dictionary<string, Mesh[]> Load()
        {
            var map = new Dictionary<string, Mesh[]>(8);
            TextAsset asset = Resources.Load<TextAsset>("BakedModels");
            if (asset == null)
            {
                Debug.LogError("BakedModels 텍스트가 없습니다.");
                return map;
            }

            Library library = JsonUtility.FromJson<Library>(asset.text);
            if (library == null || library.models == null)
                return map;

            for (int i = 0; i < library.models.Length; i++)
            {
                ModelData data = library.models[i];
                if (data == null || string.IsNullOrEmpty(data.key) || data.parts == null)
                    continue;
                var meshes = new Mesh[data.parts.Length];
                for (int p = 0; p < data.parts.Length; p++)
                    meshes[p] = Build(data.parts[p]);
                map[data.key] = meshes;
            }

            return map;
        }

        static Mesh Build(PartData part)
        {
            var mesh = new Mesh();
            mesh.name = string.IsNullOrEmpty(part.name) ? "Baked" : part.name;
            int count = part.positions == null ? 0 : part.positions.Length / 3;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var uv = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                vertices[i] = new Vector3(part.positions[i * 3], part.positions[i * 3 + 1], part.positions[i * 3 + 2]);
                if (part.normals != null && part.normals.Length >= (i + 1) * 3)
                    normals[i] = new Vector3(part.normals[i * 3], part.normals[i * 3 + 1], part.normals[i * 3 + 2]);
                if (part.uvs != null && part.uvs.Length >= (i + 1) * 2)
                    uv[i] = new Vector2(part.uvs[i * 2], part.uvs[i * 2 + 1]);
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = part.triangles ?? Array.Empty<int>();
            mesh.RecalculateBounds();
            return mesh;
        }

        [Serializable]
        class Library
        {
            public ModelData[] models = null;
        }

        [Serializable]
        class ModelData
        {
            public string key = null;
            public PartData[] parts = null;
        }

        [Serializable]
        class PartData
        {
            public string name = null;
            public float[] positions = null;
            public float[] normals = null;
            public float[] uvs = null;
            public int[] triangles = null;
        }
    }
}
