using System;
using System.Collections;
using UnityEngine;
using QuantumCrossy.Core;
using QuantumCrossy.Input;

namespace QuantumCrossy.Player
{
    /// <summary>
    /// 일반 이동은 호를 그린다. 양자 도약은 한 칸을 즉시 건너뛰고 출발 자리에 잔상을 남긴다.
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
        bool hasLogicalArc;
        float logicalHeight;
        float logicalDuration;
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

        /// <summary>
        /// 양자 도약 프레임에 한 번. 화면은 이미 도착해 있으므로, 판정만 예전 홉 궤적을 쓴다.
        /// </summary>
        public bool TryConsumeLogicalArc(out float height, out float duration)
        {
            height = logicalHeight;
            duration = logicalDuration;
            if (!hasLogicalArc)
                return false;

            hasLogicalArc = false;
            return true;
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
            Face(start, worldTarget);

            if (quantum)
            {
                LeaveGhost(start, worldTarget);
                ApplyPosition(worldTarget);
                logicalHeight = config.quantumHopHeight;
                logicalDuration = Mathf.Max(0.05f, config.hopDuration);
                hasLogicalArc = true;
                yield return null;
            }
            else
            {
                float duration = Mathf.Max(0.05f, config.hopDuration);
                float height = config.hopHeight;
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / duration;
                    float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                    Vector3 pos = Vector3.Lerp(start, worldTarget, u);
                    pos.y = Mathf.Lerp(start.y, worldTarget.y, u) + Mathf.Sin(u * Mathf.PI) * height;
                    ApplyPosition(pos);
                    if (visual != null && !flattened)
                    {
                        float yScale = Mathf.Lerp(0.82f, 1f, Mathf.Sin(u * Mathf.PI));
                        visual.localScale = new Vector3(baseScale.x, baseScale.y * yScale, baseScale.z);
                    }

                    yield return null;
                }
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

        void Face(Vector3 start, Vector3 worldTarget)
        {
            if (visual == null)
                return;

            Vector3 flat = worldTarget - start;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.001f)
                visual.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
        }

        void LeaveGhost(Vector3 start, Vector3 worldTarget)
        {
            float life = Mathf.Max(0.05f, config.quantumGhostDuration);
            Vector3 scale = visual != null ? visual.lossyScale : Vector3.one * 0.52f;
            Quaternion rotation = visual != null ? visual.rotation : Quaternion.identity;
            Afterimage.Spawn(start, rotation, scale, life, new Color(0.45f, 0.95f, 1f));

            Vector3 mid = Vector3.Lerp(start, worldTarget, 0.32f);
            mid.y = start.y;
            Afterimage.Spawn(mid, rotation, scale * 0.55f, life * 0.72f, new Color(0.75f, 1f, 1f));
        }

        void ApplyPosition(Vector3 pos)
        {
            transform.position = pos;
            if (body != null)
                body.position = pos;
        }
    }
}
