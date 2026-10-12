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
            world.Initialize(config, distribution);

            var input = gameObject.AddComponent<MoveInput>();
            input.Use(
                new KeyboardMoveSource(),
                new SwipeMoveSource(SwipeThreshold));

            var hud = gameObject.AddComponent<GameHud>();
            var flow = gameObject.AddComponent<GameFlow>();
            hud.Build(flow.Restart);

            PlayerController player = CreatePlayer(world, input, distribution, flow, hud);
            BindCamera(player.transform);

            distribution.Changed += () =>
            {
                hud.SetProbability(distribution.HighScoreProbability);
                hud.SetPhotons(distribution.PhotonCount);
            };
            hud.SetProbability(distribution.HighScoreProbability);
            hud.SetDistance(0);
            hud.SetPhotons(0);
            flow.Begin(config, distribution, player, hud, input);
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
            ScoreDistribution distribution,
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

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(playerGo.transform, false);
            visual.transform.localScale = Vector3.one * (config.playerRadius * 2f);
            DisableCollider(visual);
            Renderer glow = visual.GetComponent<Renderer>();
            glow.shadowCastingMode = ShadowCastingMode.Off;
            glow.sharedMaterial = PrimitiveMaterials.Create(new Color(0.35f, 0.95f, 1f), 2.2f);
            BlobShadow.Attach(playerGo.transform, 0.62f, 0.62f, null);

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose";
            nose.transform.SetParent(visual.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.05f, 0.72f);
            nose.transform.localScale = new Vector3(0.22f, 0.22f, 0.48f);
            DisableCollider(nose);
            Renderer noseRenderer = nose.GetComponent<Renderer>();
            noseRenderer.shadowCastingMode = ShadowCastingMode.Off;
            noseRenderer.sharedMaterial = PrimitiveMaterials.Create(new Color(0.9f, 1f, 1f), 1.5f);

            Vector2Int cell = Vector2Int.zero;
            Vector3 start = world.CellToWorld(cell, config.playerBaseY);
            playerGo.transform.position = start;
            body.position = start;

            var hopper = playerGo.AddComponent<GridHopper>();
            hopper.Initialize(config, cell, visual.transform, body);
            var controller = playerGo.AddComponent<PlayerController>();
            controller.Initialize(config, hopper, world, input, distribution, flow, hud);
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

        static void DisableCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
        }
    }
}
