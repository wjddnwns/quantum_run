using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using QuantumCrossy.CameraRig;
using QuantumCrossy.Input;
using QuantumCrossy.Lanes;
using QuantumCrossy.Player;
using QuantumCrossy.Scoring;
using QuantumCrossy.UI;
using QuantumCrossy.Visuals;

namespace QuantumCrossy.Core
{
    /// <summary>
    /// 씬에 있는 설정과 카메라를 받아 플레이에 필요한 오브젝트를 만든다.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] QuantumCrossyConfig config;
        [SerializeField] GameObject qubitPrefab = null;
        [SerializeField] GameObject measurementGatePrefab = null;
        [SerializeField] GameObject roadPrefab = null;
        [SerializeField] GameObject trailPrefab = null;
        [SerializeField] GameObject treePrefab = null;
        [SerializeField] GameObject pointPrefab = null;
        InputSystemUIInputModule ownedUi;

        void Awake()
        {
            if (config == null)
                config = GetComponent<QuantumCrossyConfig>();
            if (config == null)
            {
                Debug.LogError("QuantumCrossyConfig가 없습니다.");
                return;
            }

            config.Repair();
            EnsureEventSystem();

            var distribution = new ScoreDistribution(config.betaPerPhoton, config.highScoreMinScale);

            var worldGo = new GameObject("World");
            worldGo.transform.SetParent(transform, false);
            var world = worldGo.AddComponent<WorldGenerator>();
            world.Initialize(
                config,
                distribution,
                roadPrefab,
                trailPrefab,
                treePrefab,
                measurementGatePrefab,
                pointPrefab);

            var input = gameObject.AddComponent<MoveInput>();
            input.Use(
                new KeyboardMoveSource(),
                new SwipeMoveSource(SwipeThreshold));

            var hud = gameObject.AddComponent<GameHud>();
            var flow = gameObject.AddComponent<GameFlow>();
            hud.Build(flow.Restart);
            input.SetAccepting(false);

            PlayerController player = CreatePlayer(world, input, flow, hud);
            BindCamera(player.transform);

            distribution.Changed += () =>
            {
                hud.SetProbability(distribution.HighScoreProbability);
                hud.SetPhotons(distribution.PhotonCount);
            };
            hud.SetProbability(distribution.HighScoreProbability);
            hud.SetDistance(0);
            hud.SetPhotons(0);

            var briefingGo = new GameObject("ScoreBriefing");
            var briefing = briefingGo.AddComponent<ScoreBriefing>();
            briefing.Show(config, distribution, () =>
            {
                input.SetAccepting(true);
                flow.Begin(config, distribution, player, hud, input);
            });
        }

        void OnDestroy()
        {
            if (ownedUi != null)
                ownedUi.UnassignActions();
        }

        float SwipeThreshold()
        {
            float scale = 1f;
            if (Screen.dpi > 1f)
                scale = Mathf.Clamp(Screen.dpi / 160f, 0.8f, 3f);
            return config.swipeThresholdPixels * scale;
        }

        PlayerController CreatePlayer(
            WorldGenerator world,
            MoveInput input,
            GameFlow flow,
            GameHud hud)
        {
            var playerGo = new GameObject("QuantumParticle");
            playerGo.transform.SetParent(transform, false);

            var body = playerGo.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var sphere = playerGo.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = config.playerRadius;

            Transform visualTransform = null;
            if (qubitPrefab != null)
            {
                GameObject visual = Instantiate(qubitPrefab, playerGo.transform);
                visual.name = "Visual";
                visual.transform.localPosition = new Vector3(0f, -config.playerBaseY, 0f);
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                visualTransform = visual.transform;
            }
            else
            {
                visualTransform = playerGo.transform;
            }

            Renderer glow = visualTransform.GetComponentInChildren<Renderer>();
            BlobShadow.Attach(playerGo.transform, 0.62f, 0.62f, glow);

            Vector2Int cell = Vector2Int.zero;
            Vector3 start = world.CellToWorld(cell, config.playerBaseY);
            playerGo.transform.position = start;
            body.position = start;

            var hopper = playerGo.AddComponent<GridHopper>();
            hopper.Initialize(config, cell, visualTransform, body);
            var controller = playerGo.AddComponent<PlayerController>();
            controller.Initialize(config, hopper, world, input, flow, hud);
            return controller;
        }

        void BindCamera(Transform target)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.025f, 0.06f);
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 200f;

            var follow = cam.GetComponent<FollowCamera>();
            if (follow == null)
                follow = cam.gameObject.AddComponent<FollowCamera>();
            follow.Initialize(target, config);
        }

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            ownedUi = es.AddComponent<InputSystemUIInputModule>();
        }
    }
}
