using System;
using System.Collections;
using UnityEngine;
using QuantumCrossy.Core;
using QuantumCrossy.Input;

namespace QuantumCrossy.Player
{
    /// <summary>
    /// 한 칸씩 호를 그리며 이동한다. 입력은 홉이 끝날 때까지 하나만 보관한다.
    /// </summary>
    public sealed class GridHopper : MonoBehaviour
    {
        QuantumCrossyConfig config;
        Transform visual;
        Rigidbody body;
        Coroutine hopRoutine;
        MoveCommand buffered;
        bool hasBuffer;
        bool flattened;
        Vector3 baseScale = Vector3.one;

        public Vector2Int Cell { get; private set; }
        public int MaxForwardRow { get; private set; }
        public bool IsHopping { get; private set; }
        public event Action Landed;

        public void Initialize(QuantumCrossyConfig config, Vector2Int cell, Transform visual, Rigidbody body)
        {
            this.config = config;
            this.visual = visual;
            this.body = body;
            if (visual != null)
                baseScale = visual.localScale;
            Cell = cell;
            MaxForwardRow = Mathf.Max(0, cell.y);
        }

        public void Buffer(MoveCommand command)
        {
            if (command == MoveCommand.None)
                return;
            buffered = command;
            hasBuffer = true;
        }

        public bool ConsumeBuffer(out MoveCommand command)
        {
            command = buffered;
            bool had = hasBuffer;
            hasBuffer = false;
            buffered = MoveCommand.None;
            return had && command != MoveCommand.None;
        }

        public void Hop(Vector2Int target, Vector3 worldTarget, bool quantum)
        {
            if (IsHopping)
                return;

            if (hopRoutine != null)
                StopCoroutine(hopRoutine);
            hopRoutine = StartCoroutine(HopRoutine(target, worldTarget, quantum));
        }

        public void Flatten()
        {
            flattened = true;
            if (visual != null)
                visual.localScale = new Vector3(baseScale.x * 1.45f, baseScale.y * 0.25f, baseScale.z * 1.45f);
        }

        IEnumerator HopRoutine(Vector2Int target, Vector3 worldTarget, bool quantum)
        {
            IsHopping = true;
            Vector3 start = transform.position;
            float duration = Mathf.Max(0.05f, config.hopDuration);
            float height = quantum ? config.quantumHopHeight : config.hopHeight;
            float t = 0f;

            if (visual != null)
            {
                Vector3 flat = worldTarget - start;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.001f)
                    visual.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
            }

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                Vector3 pos = Vector3.Lerp(start, worldTarget, u);
                pos.y = Mathf.Lerp(start.y, worldTarget.y, u) + Mathf.Sin(u * Mathf.PI) * height;
                ApplyPosition(pos);
                if (visual != null && !flattened)
                {
                    float squash = quantum ? 1.15f : 1f;
                    float yScale = Mathf.Lerp(0.82f, squash, Mathf.Sin(u * Mathf.PI));
                    visual.localScale = new Vector3(baseScale.x, baseScale.y * yScale, baseScale.z);
                }

                yield return null;
            }

            ApplyPosition(worldTarget);
            if (visual != null && !flattened)
                visual.localScale = baseScale;

            Cell = target;
            if (Cell.y > MaxForwardRow)
                MaxForwardRow = Cell.y;

            IsHopping = false;
            hopRoutine = null;
            Landed?.Invoke();
        }

        void ApplyPosition(Vector3 pos)
        {
            transform.position = pos;
            if (body != null)
                body.position = pos;
        }
    }
}
