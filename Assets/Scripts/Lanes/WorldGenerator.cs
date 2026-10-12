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
        Material safeDark;
        Material safeLight;
        Material border;
        Material gateTile;
        Material photonMat;
        int highestRow = int.MinValue;
        int gateStreak;

        public void Initialize(QuantumCrossyConfig config, ScoreDistribution distribution)
        {
            this.config = config;
            this.distribution = distribution;
            safeDark = PrimitiveMaterials.Create(new Color(0.09f, 0.24f, 0.30f), 0.05f);
            safeLight = PrimitiveMaterials.Create(new Color(0.07f, 0.18f, 0.28f), 0.04f);
            border = PrimitiveMaterials.Create(new Color(0.04f, 0.05f, 0.08f), 0f);
            gateTile = PrimitiveMaterials.Create(new Color(0.18f, 0.07f, 0.16f), 0.08f);
            photonMat = PrimitiveMaterials.Create(new Color(1f, 0.92f, 0.55f), 2.6f);
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
            Material playMat = gateLane
                ? gateTile
                : ((row & 1) == 0 ? safeDark : safeLight);

            for (int x = -config.playHalfWidth - extra; x <= config.playHalfWidth + extra; x++)
            {
                bool edge = Mathf.Abs(x) > config.playHalfWidth;
                CreateTile(root.transform, x, row, edge ? border : playMat);
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

        void CreateTile(Transform parent, int x, int row, Material material)
        {
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "Tile";
            tile.transform.SetParent(parent, false);
            float size = config.cellSize * 0.94f;
            tile.transform.localScale = new Vector3(size, 0.2f, size);
            tile.transform.position = new Vector3(x * config.cellSize, -0.1f, row * config.cellSize);
            Collider col = tile.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
            Renderer renderer = tile.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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
            Color color = new Color(1f, 0.28f, 0.58f);

            for (int i = 0; i < count; i++)
            {
                float x = -config.gateTravelHalf + offset + i * spacing;
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "MeasurementGate";
                go.transform.SetParent(lane.transform, true);
                go.transform.localScale = new Vector3(config.gateThickness, config.gateHeight, config.gateDepth);
                go.transform.position = new Vector3(x, config.gateHeight * 0.5f, row * config.cellSize);
                var gate = go.AddComponent<MeasurementGate>();
                gate.Initialize(row * config.cellSize, speed, config.gateTravelHalf, config.gateVisibleHalf, color);
                lane.AddGate(gate);
                Renderer gateRenderer = go.GetComponent<Renderer>();
                BlobShadow.Attach(
                    go.transform,
                    Mathf.Max(0.36f, config.gateThickness * 2.4f),
                    config.gateDepth * 0.92f,
                    gateRenderer);
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

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Photon";
            go.transform.SetParent(parent, true);
            go.transform.position = new Vector3(x * config.cellSize, 0.55f, row * config.cellSize);
            go.transform.localScale = new Vector3(0.14f, 0.28f, 0.14f);
            go.transform.rotation = Quaternion.Euler(58f, Random.Range(0f, 180f), 12f);
            Collider col = go.GetComponent<Collider>();
            col.isTrigger = true;
            Renderer photonRenderer = go.GetComponent<Renderer>();
            photonRenderer.sharedMaterial = photonMat;
            photonRenderer.shadowCastingMode = ShadowCastingMode.Off;
            BlobShadow.Attach(go.transform, 0.34f, 0.34f, null);
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

        void OnDestroy()
        {
            DestroyMat(safeDark);
            DestroyMat(safeLight);
            DestroyMat(border);
            DestroyMat(gateTile);
            DestroyMat(photonMat);
        }

        static void DestroyMat(Material mat)
        {
            if (mat != null)
                Destroy(mat);
        }
    }
}
