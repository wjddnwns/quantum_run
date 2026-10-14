using System.Collections.Generic;
using UnityEngine;
using QuantumCrossy.Core;
using QuantumCrossy.Gates;
using QuantumCrossy.Input;
using QuantumCrossy.Lanes;
using QuantumCrossy.Rays;
using QuantumCrossy.UI;

namespace QuantumCrossy.Player
{
    [DefaultExecutionOrder(10)]
    public sealed class PlayerController : MonoBehaviour
    {
        readonly List<MeasurementGate> nearby = new List<MeasurementGate>(8);

        QuantumCrossyConfig config;
        GridHopper hopper;
        WorldGenerator world;
        MoveInput moveInput;
        GameFlow flow;
        GameHud hud;
        Vector3 previousPos;
        bool hasPrevious;

        public int MaxForwardRow => hopper != null ? hopper.MaxForwardRow : 0;

        public void Initialize(
            QuantumCrossyConfig config,
            GridHopper hopper,
            WorldGenerator world,
            MoveInput moveInput,
            GameFlow flow,
            GameHud hud)
        {
            this.config = config;
            this.hopper = hopper;
            this.world = world;
            this.moveInput = moveInput;
            this.flow = flow;
            this.hud = hud;
            previousPos = transform.position;
            hasPrevious = true;
            hopper.Landed += OnLanded;
        }

        void Update()
        {
            if (hopper == null || world == null || flow == null || !flow.IsPlaying || moveInput == null)
                return;

            if (moveInput.TryConsume(out MoveCommand command))
                TryMove(command);
        }

        void LateUpdate()
        {
            if (!hasPrevious)
            {
                previousPos = transform.position;
                hasPrevious = true;
                return;
            }

            Vector3 now = transform.position;
            if (flow != null && flow.IsPlaying)
                ResolveSegment(previousPos, now);
            previousPos = now;
        }

        void OnLanded()
        {
            if (flow == null || !flow.IsPlaying)
                return;

            if (ResolveSegment(previousPos, transform.position))
                return;

            previousPos = transform.position;
            world.EnsureAround(hopper.Cell.y);
            if (hud != null)
                hud.SetDistance(hopper.MaxForwardRow);

            if (hopper.ConsumeBuffer(out MoveCommand command))
                TryMove(command);
        }

        void TryMove(MoveCommand command)
        {
            if (flow == null || !flow.IsPlaying || hopper == null)
                return;

            if (hopper.IsHopping)
            {
                hopper.Buffer(command);
                return;
            }

            Vector2Int delta = command.ToDelta();
            Vector2Int target = hopper.Cell + delta;
            if (Mathf.Abs(target.x) > config.playHalfWidth || target.y < config.minRow)
                return;

            world.EnsureAround(target.y);
            if (world.IsBlocked(target))
                return;
            Vector3 to = world.CellToWorld(target, config.playerBaseY);
            hopper.Hop(target, to);
        }

        /// <summary>
        /// 구간 안에서 판에 닿으면 측정. true면 런이 끝났다.
        /// </summary>
        bool ResolveSegment(Vector3 from, Vector3 to)
        {
            if (world == null || config == null)
                return false;

            PhotonRay.CollectAlong(from, to, config.photonPickupRadius);

            int row = Mathf.RoundToInt(to.z / config.cellSize);
            world.CopyGatesNear(row, 1, nearby);

            for (int i = 0; i < nearby.Count; i++)
            {
                MeasurementGate gate = nearby[i];
                if (gate == null)
                    continue;

                if (!gate.Hits(from, to, config.playerRadius))
                    continue;

                flow.Collapse();
                return true;
            }

            return false;
        }

        public void PlayCollapseVisual()
        {
            hopper.Flatten();
        }

        void OnDestroy()
        {
            if (hopper != null)
                hopper.Landed -= OnLanded;
        }

        void OnTriggerEnter(Collider other)
        {
            if (flow == null || !flow.IsPlaying || other == null)
                return;
            if (other.TryGetComponent(out PhotonRay ray))
                ray.Collect();
        }
    }
}
