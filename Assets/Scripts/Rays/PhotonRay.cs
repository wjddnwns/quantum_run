using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumCrossy.Rays
{
    public sealed class PhotonRay : MonoBehaviour
    {
        static readonly List<PhotonRay> active = new List<PhotonRay>(32);

        Action onCollected;
        Vector3 origin;
        bool collected;

        public void Initialize(Action collectedCallback)
        {
            onCollected = collectedCallback;
            origin = transform.position;
        }

        void OnEnable()
        {
            if (!active.Contains(this))
                active.Add(this);
        }

        void OnDisable()
        {
            active.Remove(this);
        }

        void Update()
        {
            float wave = Mathf.Sin(Time.time * 3.2f + origin.x * 1.7f);
            Vector3 pos = origin;
            pos.y += wave * 0.08f;
            transform.position = pos;
            transform.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
        }

        /// <summary>
        /// 이동 선분과의 수평 거리로 줍는다. 홉 높이, 순간이동, 물리 프레임에 의존하지 않는다.
        /// </summary>
        public static void CollectAlong(Vector3 from, Vector3 to, float radius)
        {
            float limit = radius * radius;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                PhotonRay ray = active[i];
                if (ray == null)
                {
                    active.RemoveAt(i);
                    continue;
                }

                if (ray.collected)
                    continue;

                if (SegmentDistanceSqrXZ(ray.origin, from, to) <= limit)
                    ray.Collect();
            }
        }

        public void Collect()
        {
            if (collected)
                return;

            collected = true;
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
            onCollected?.Invoke();
            Destroy(gameObject);
        }

        static float SegmentDistanceSqrXZ(Vector3 point, Vector3 from, Vector3 to)
        {
            Vector2 p = new Vector2(point.x, point.z);
            Vector2 a = new Vector2(from.x, from.z);
            Vector2 b = new Vector2(to.x, to.z);
            Vector2 ab = b - a;
            float lengthSqr = ab.sqrMagnitude;
            float t = lengthSqr < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSqr);
            return (p - (a + ab * t)).sqrMagnitude;
        }
    }
}
