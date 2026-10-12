using System;
using UnityEngine;

namespace QuantumCrossy.Rays
{
    public sealed class PhotonRay : MonoBehaviour
    {
        Action onCollected;
        Vector3 origin;
        bool collected;

        public void Initialize(Action collectedCallback)
        {
            onCollected = collectedCallback;
            origin = transform.position;
        }

        void Update()
        {
            float wave = Mathf.Sin(Time.time * 3.2f + origin.x * 1.7f);
            Vector3 pos = origin;
            pos.y += wave * 0.12f;
            transform.position = pos;
            transform.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
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
    }
}
