using System.Collections.Generic;
using UnityEngine;
using QuantumCrossy.Core;
using QuantumCrossy.Gates;
using QuantumCrossy.Input;
using QuantumCrossy.Lanes;
using QuantumCrossy.Rays;
using QuantumCrossy.Scoring;
using QuantumCrossy.UI;

namespace QuantumCrossy.Player
{
    [DefaultExecutionOrder(10)]
    public sealed class PlayerController : MonoBehaviour
    {
        readonly List<MeasurementGate> nearby = new List<MeasurementGate>(8);
        readonly List<MeasurementGate> rewarded = new List<MeasurementGate>(4);
        readonly List<MeasurementGate> phased = new List<MeasurementGate>(4);

        QuantumCrossyConfig config;
        GridHopper hopper;
        WorldGenerator world;
        MoveInput moveInput;
        ScoreDistribution distribution;
        GameFlow flow;
        GameHud hud;
        Vector3 previousPos;
        int pendingClears;
        bool hasPrevious;

        public int MaxForwardRow => hopper != null ? hopper.MaxForwardRow : 0;

        public void Initialize(
            QuantumCrossyConfig config,
            GridHopper hopper,
            WorldGenerator world,
            MoveInput moveInput,
            ScoreDistribution distribution,
            GameFlow flow,
            GameHud hud)
        {
            this.config = config;
            this.hopper = hopper;
            this.world = world;
            this.moveInput = moveInput;
            this.distribution = distribution;
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
            if (pendingClears > 0)
            {
                int photons = config.quantumJumpBonusPhotons * pendingClears;
                pendingClears = 0;
                if (photons > 0)
                {
                    distribution.AddPhotons(photons);
                    if (hud != null)
                        hud.ShowQuantumJump(photons);
                }
            }

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
            Vector3 from = world.CellToWorld(hopper.Cell, config.playerBaseY);
            Vector3 to = world.CellToWorld(target, config.playerBaseY);
            world.CopyGatesNear(hopper.Cell.y, 1, nearby);
            bool over = QuantumJumpRule.ShouldHopOver(
                from,
                to,
                nearby,
                config.quantumWindowMin,
                config.quantumWindowMax,
                config.playerRadius);

            pendingClears = 0;
            hopper.Hop(target, to, over);
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
            float forget = config.cellSize * 1.6f;
            for (int i = rewarded.Count - 1; i >= 0; i--)
            {
                MeasurementGate remembered = rewarded[i];
                if (remembered == null || Mathf.Abs(remembered.transform.position.x - to.x) > forget)
                    rewarded.RemoveAt(i);
            }

            float bodyReach = config.playerRadius + config.gateThickness * 0.5f + 0.08f;
            for (int i = phased.Count - 1; i >= 0; i--)
            {
                MeasurementGate passing = phased[i];
                if (passing == null || passing.ShouldReleasePhase(to, bodyReach))
                    phased.RemoveAt(i);
            }

            bool quantumFrame = hopper != null && hopper.TryConsumeLogicalArc(out float arcHeight, out float arcDuration);

            for (int i = 0; i < nearby.Count; i++)
            {
                MeasurementGate gate = nearby[i];
                if (gate == null)
                    continue;

                bool hit;
                bool cleared;
                if (quantumFrame)
                    gate.EvaluateOverTime(from, to, config.playerRadius, arcHeight, arcDuration, out hit, out cleared);
                else
                    gate.Evaluate(from, to, config.playerRadius, out hit, out cleared);

                if (hit)
                {
                    if (phased.Contains(gate))
                        continue;

                    flow.Collapse();
                    return true;
                }

                if (!cleared || rewarded.Contains(gate))
                    continue;

                rewarded.Add(gate);
                pendingClears++;
                if (quantumFrame && !phased.Contains(gate))
                    phased.Add(gate);
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

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material mat = renderers[i].sharedMaterial;
                if (mat != null)
                    Destroy(mat);
            }
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
