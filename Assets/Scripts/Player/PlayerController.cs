using System.Collections.Generic;
using UnityEngine;
using QuantumCrossy.Core;
using QuantumCrossy.Gates;
using QuantumCrossy.Input;
using QuantumCrossy.Lanes;
using QuantumCrossy.Rays;
using QuantumCrossy.Scoring;
using QuantumCrossy.UI;
using QuantumCrossy.Visuals;

namespace QuantumCrossy.Player
{
    [DefaultExecutionOrder(10)]
    public sealed class PlayerController : MonoBehaviour
    {
        readonly List<QuantumJumpRule.Match> matches = new List<QuantumJumpRule.Match>(4);
        readonly PhaseTracker phases = new PhaseTracker();

        QuantumCrossyConfig config;
        GridHopper hopper;
        WorldGenerator world;
        MoveInput moveInput;
        ScoreDistribution distribution;
        GameFlow flow;
        GameHud hud;
        Renderer glow;
        bool glowing;

        static readonly Color IdleColor = new Color(0.35f, 0.95f, 1f);
        static readonly Color PhaseColor = new Color(1f, 0.92f, 0.55f);

        public int MaxForwardRow => hopper != null ? hopper.MaxForwardRow : 0;

        public void Initialize(
            QuantumCrossyConfig config,
            GridHopper hopper,
            WorldGenerator world,
            MoveInput moveInput,
            ScoreDistribution distribution,
            GameFlow flow,
            GameHud hud,
            Renderer glow)
        {
            this.config = config;
            this.hopper = hopper;
            this.world = world;
            this.moveInput = moveInput;
            this.distribution = distribution;
            this.flow = flow;
            this.hud = hud;
            this.glow = glow;
            hopper.Landed += OnLanded;
            SetGlow(false);
        }

        void Update()
        {
            if (hopper == null || world == null)
                return;

            world.UpdateTelegraphs(hopper.Cell);
            SetGlow(phases.AnyActive || hopper.IsQuantumArc);

            if (flow == null || !flow.IsPlaying || moveInput == null)
                return;

            if (moveInput.TryConsume(out MoveCommand command))
                TryMove(command);
        }

        void OnLanded()
        {
            if (flow == null || !flow.IsPlaying)
                return;

            world.EnsureAround(hopper.Cell.y);
            if (hud != null)
                hud.SetDistance(hopper.MaxForwardRow);
            Physics.SyncTransforms();
            if (OverlapsUnphasedGate())
            {
                flow.Collapse();
                return;
            }

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
            Lane lane = world.GetLane(target.y);
            bool quantum = lane != null && lane.IsGateLane && QuantumJumpRule.TryMatch(
                from,
                to,
                lane.Gates,
                config.quantumWindowMin,
                config.quantumWindowMax,
                config.playerRadius,
                config.quantumPhasePadding,
                matches);

            if (quantum)
            {
                for (int i = 0; i < matches.Count; i++)
                    phases.Add(matches[i].Gate, matches[i].PhaseDuration);
                distribution.AddPhotons(config.quantumJumpBonusPhotons);
                if (hud != null)
                    hud.ShowQuantumJump(config.quantumJumpBonusPhotons);
            }

            hopper.Hop(target, to, quantum);
        }

        public void PlayCollapseVisual()
        {
            hopper.Flatten();
            SetGlow(false);
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
            HandleContact(other);
        }

        void OnTriggerStay(Collider other)
        {
            HandleContact(other);
        }

        void HandleContact(Collider other)
        {
            if (flow == null || !flow.IsPlaying || other == null)
                return;

            if (other.TryGetComponent(out PhotonRay ray))
            {
                ray.Collect();
                return;
            }

            if (!other.TryGetComponent(out MeasurementGate gate))
                return;
            if (phases.IsPhasing(gate))
                return;

            flow.Collapse();
        }

        bool OverlapsUnphasedGate()
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                config.playerRadius,
                ~0,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hits.Length; i++)
            {
                if (!hits[i].TryGetComponent(out MeasurementGate gate))
                    continue;
                if (!phases.IsPhasing(gate))
                    return true;
            }

            return false;
        }

        void SetGlow(bool phase)
        {
            if (glow == null || glowing == phase)
                return;
            glowing = phase;
            PrimitiveMaterials.Apply(glow.sharedMaterial, phase ? PhaseColor : IdleColor, phase ? 3.2f : 2.2f);
        }
    }
}
