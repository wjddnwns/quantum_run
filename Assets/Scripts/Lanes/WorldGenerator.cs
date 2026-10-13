using System.Collections.Generic;
using UnityEngine;
using QuantumCrossy.Core;
using QuantumCrossy.Gates;
using QuantumCrossy.Rays;
using QuantumCrossy.Scoring;
using QuantumCrossy.Visuals;

namespace QuantumCrossy.Lanes
{
    /// <summary>
    /// 플레이어 앞뒤로 레인을 만들고, 뒤쪽은 지운다.
    /// 안전 구간 뒤에는 게이트 레인과 광자 레인이 섞인다.
    /// </summary>
    public sealed class WorldGenerator : MonoBehaviour
    {
        readonly Dictionary<int, Lane> lanes = new Dictionary<int, Lane>(32);
        readonly List<Lane> laneList = new List<Lane>(32);
        readonly List<int> removeBuffer = new List<int>(8);

        QuantumCrossyConfig config;
        ScoreDistribution distribution;
        GameObject roadPrefab;
        GameObject trailPrefab;
        GameObject treePrefab;
        GameObject gatePrefab;
        GameObject pointPrefab;
        int highestRow = int.MinValue;
        int gateStreak;

        public void Initialize(
            QuantumCrossyConfig config,
            ScoreDistribution distribution,
            GameObject roadPrefab,
            GameObject trailPrefab,
            GameObject treePrefab,
            GameObject gatePrefab,
            GameObject pointPrefab)
        {
            this.config = config;
            this.distribution = distribution;
            this.roadPrefab = roadPrefab;
            this.trailPrefab = trailPrefab;
            this.treePrefab = treePrefab;
            this.gatePrefab = gatePrefab;
            this.pointPrefab = pointPrefab;
            highestRow = config.minRow - 1;
            EnsureAround(0);
        }

        public Lane GetLane(int row)
        {
            lanes.TryGetValue(row, out Lane lane);
            return lane;
        }

        public Vector3 CellToWorld(Vector2Int cell, float y)
        {
            return new Vector3(cell.x * config.cellSize, y, cell.y * config.cellSize);
        }

        public void EnsureAround(int playerRow)
        {
            int target = playerRow + config.aheadRows;
            while (highestRow < target)
            {
                highestRow++;
                CreateLane(highestRow);
            }

            RemoveBehind(playerRow - config.behindRows);
        }

        public void CopyGatesNear(int row, int span, List<MeasurementGate> into)
        {
            into.Clear();
            for (int r = row - span; r <= row + span; r++)
            {
                if (!lanes.TryGetValue(r, out Lane lane))
                    continue;
                IReadOnlyList<MeasurementGate> gates = lane.Gates;
                for (int i = 0; i < gates.Count; i++)
                {
                    if (gates[i] != null)
                        into.Add(gates[i]);
                }
            }
        }

        void CreateLane(int row)
        {
            bool gateLane = RollGateLane(row);
            var root = new GameObject();
            root.transform.SetParent(transform, false);
            var lane = root.AddComponent<Lane>();
            lane.Setup(row, gateLane);
            lanes.Add(row, lane);
            laneList.Add(lane);

            int extra = 1;
            GameObject playPrefab = gateLane ? roadPrefab : trailPrefab;

            for (int x = -config.playHalfWidth - extra; x <= config.playHalfWidth + extra; x++)
            {
                bool edge = Mathf.Abs(x) > config.playHalfWidth;
                CreateTile(root.transform, x, row, edge ? trailPrefab : playPrefab);
                if (edge)
                    CreateTree(root.transform, x, row);
            }

            if (gateLane)
                SpawnGates(lane, row);
            else if (row != 0)
                SpawnPhotons(root.transform, row);
        }

        bool RollGateLane(int row)
        {
            if (row < config.safeStartRows)
            {
                gateStreak = 0;
                return false;
            }

            if (gateStreak >= config.maxConsecutiveGateLanes)
            {
                gateStreak = 0;
                return false;
            }

            bool gate = Random.value < config.gateLaneWeight;
            gateStreak = gate ? gateStreak + 1 : 0;
            return gate;
        }

        void CreateTile(Transform parent, int x, int row, GameObject prefab)
        {
            if (prefab == null)
                return;

            GameObject tile = Instantiate(prefab, parent);
            tile.name = "Tile";
            tile.transform.localRotation = Quaternion.identity;
            tile.transform.localPosition = new Vector3(x * config.cellSize, 0f, row * config.cellSize);
        }

        void CreateTree(Transform parent, int x, int row)
        {
            if (treePrefab == null)
                return;

            GameObject tree = Instantiate(treePrefab, parent);
            tree.name = "Tree";
            tree.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            tree.transform.localPosition = new Vector3(x * config.cellSize, 0f, row * config.cellSize);
            Renderer renderer = tree.GetComponentInChildren<Renderer>();
            BlobShadow.Attach(tree.transform, 0.72f, 0.72f, renderer);
        }

        void SpawnGates(Lane lane, int row)
        {
            int count = Random.Range(config.minGatesPerLane, config.maxGatesPerLane + 1);
            float speed = config.gateSpeed + Mathf.Max(0, row) * config.gateSpeedPerRow;
            speed = Mathf.Clamp(speed, 0.5f, config.maxGateSpeed);
            speed *= Random.Range(0.88f, 1.12f);
            if ((row & 1) == 0)
                speed = -speed;

            float span = config.gateTravelHalf * 2f;
            float spacing = span / count;
            float offset = Random.Range(0f, spacing);

            for (int i = 0; i < count; i++)
            {
                float x = -config.gateTravelHalf + offset + i * spacing;
                GameObject go = gatePrefab != null
                    ? Instantiate(gatePrefab)
                    : new GameObject();
                go.name = "MeasurementGate";
                go.transform.SetParent(lane.transform, true);
                go.transform.position = new Vector3(x, 0f, row * config.cellSize);

                Vector3 hitHalf = new Vector3(config.gateThickness, config.gateHeight, config.gateDepth) * 0.5f;
                Vector3 hitOffset = new Vector3(0f, config.gateHeight * 0.5f, 0f);
                var visual = go.GetComponent<MeasurementGateVisual>();
                if (visual != null)
                {
                    visual.Fit(config.gateThickness, config.gateHeight, config.gateDepth);
                    hitHalf = visual.LaserHalf;
                    hitOffset = visual.LaserOffset;
                }

                var gate = go.AddComponent<MeasurementGate>();
                gate.Initialize(row * config.cellSize, speed, config.gateTravelHalf, config.gateVisibleHalf, hitHalf, hitOffset);
                if (visual != null)
                    gate.SetPillars(visual.PillarHalf, visual.PillarCenter);
                lane.AddGate(gate);
            }
        }

        void SpawnPhotons(Transform parent, int row)
        {
            if (row <= 0 || Random.value > config.photonRowChance)
                return;

            int x = 0;
            if (Random.value < config.photonLateralChance)
            {
                int reach = Mathf.Max(1, config.photonLateralReach);
                int mag = Random.Range(1, reach + 1);
                x = Random.value < 0.5f ? -mag : mag;
                x = Mathf.Clamp(x, -config.playHalfWidth, config.playHalfWidth);
            }

            GameObject go = pointPrefab != null ? Instantiate(pointPrefab) : new GameObject();
            go.name = "Photon";
            go.transform.SetParent(parent, true);
            go.transform.position = new Vector3(x * config.cellSize, 0.55f, row * config.cellSize);
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.12f;
            Renderer photonRenderer = go.GetComponentInChildren<Renderer>();
            BlobShadow.Attach(go.transform, 0.34f, 0.34f, photonRenderer);
            var ray = go.AddComponent<PhotonRay>();
            ray.Initialize(() => distribution.AddPhotons(1));
        }

        void RemoveBehind(int minKeep)
        {
            removeBuffer.Clear();
            foreach (KeyValuePair<int, Lane> pair in lanes)
            {
                if (pair.Key < minKeep)
                    removeBuffer.Add(pair.Key);
            }

            for (int i = 0; i < removeBuffer.Count; i++)
            {
                int row = removeBuffer[i];
                Lane lane = lanes[row];
                lanes.Remove(row);
                laneList.Remove(lane);
                if (lane != null)
                    Destroy(lane.gameObject);
            }
        }

    }
}
