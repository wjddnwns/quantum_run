using UnityEngine;

namespace QuantumCrossy.Core
{
    /// <summary>
    /// 인스펙터에서 바로 바꾸는 튜닝 값. 씬의 QuantumCrossy 오브젝트에 붙어 있다.
    /// </summary>
    public sealed class QuantumCrossyConfig : MonoBehaviour
    {
        [Header("격자")]
        [Tooltip("한 칸의 월드 크기")]
        public float cellSize = 1f;

        [Tooltip("좌우로 갈 수 있는 칸 수. 5면 x는 -5~5")]
        public int playHalfWidth = 5;

        [Tooltip("입자 충돌 반경. 판과의 판정에도 이 구체를 쓴다")]
        public float playerRadius = 0.26f;

        [Tooltip("입자가 바닥에 닿아 있을 때의 중심 높이")]
        public float playerBaseY = 0.3f;

        [Header("홉")]
        [Tooltip("한 칸 이동에 걸리는 시간(초)")]
        public float hopDuration = 0.26f;

        [Tooltip("일반 홉 높이. 판 높이보다 낮아서, 이 높이로는 판을 넘지 못한다")]
        public float hopHeight = 0.42f;

        [Tooltip("양자 도약의 판정 높이. 화면은 순간이동이고, 이 높이의 논리 궤적으로 판을 넘었는지 본다")]
        public float quantumHopHeight = 1.15f;

        [Tooltip("양자 도약 잔상이 남는 시간(초)")]
        public float quantumGhostDuration = 0.32f;

        [Header("양자 도약")]
        [Tooltip("판이 몸에 닿기까지 남은 시간이 이보다 짧으면 너무 늦다")]
        public float quantumWindowMin = 0.04f;

        [Tooltip("판이 몸에 닿기까지 남은 시간이 이보다 길면 아직 이르다. 이 창 안이면 넘는 홉을 탄다")]
        public float quantumWindowMax = 0.1f;

        [Tooltip("양자 도약으로 한 행을 처음 넘겼을 때 한 번만 얻는 광자 수")]
        public int quantumJumpBonusPhotons = 2;

        [Header("측정 게이트")]
        public float gateSpeed = 5.5f;
        public float gateSpeedPerRow = 0.035f;
        public float maxGateSpeed = 12f;
        [Tooltip("판이 움직이는 방향(X)의 두께")]
        public float gateThickness = 0.14f;

        [Tooltip("판의 높이. 일반 홉보다 높고, 넘는 홉보다는 낮게")]
        public float gateHeight = 0.78f;

        [Tooltip("레인을 막는 방향(Z)의 너비")]
        public float gateDepth = 0.86f;
        public float gateTravelHalf = 12f;

        [Tooltip("이보다 바깥의 게이트는 숨긴다. 반대편으로 옮기는 지점(gateTravelHalf)보다 안쪽이어야 한다")]
        public float gateVisibleHalf = 7.5f;
        public int minGatesPerLane = 1;
        public int maxGatesPerLane = 2;

        [Header("월드 생성")]
        public int minRow = -2;
        [Tooltip("이 행 번호 미만은 항상 안전한 칸")]
        public int safeStartRows = 5;
        public int aheadRows = 16;
        public int behindRows = 7;
        [Range(0f, 1f)]
        [Tooltip("안전 구간 이후, 게이트 레인이 나올 확률")]
        public float gateLaneWeight = 0.58f;
        public int maxConsecutiveGateLanes = 2;

        [Range(0f, 1f)]
        [Tooltip("안전 레인 한 줄에 광자가 하나 나올 확률")]
        public float photonRowChance = 0.14f;

        [Range(0f, 1f)]
        [Tooltip("그 광자가 진행 축을 벗어나 옆에 나올 확률")]
        public float photonLateralChance = 0.05f;

        [Tooltip("옆으로 나갈 때 최대 몇 칸까지. 1이면 바로 옆 칸만")]
        public int photonLateralReach = 1;

        [Tooltip("광자 중심과의 수평 거리가 이하면 획득. 높이와 물리 트리거에 의존하지 않는다")]
        public float photonPickupRadius = 0.45f;

        [Header("확률 / 점수")]
        [Tooltip("광자 하나가 고득점 쪽으로 분포를 기울이는 세기")]
        public float betaPerPhoton = 0.16f;

        [Tooltip("이 배수 이상인 상태를 고득점으로 친다. 기본 20 = 높음·전설")]
        public float highScoreMinScale = 20f;

        [Tooltip("측정 점수 = 상태 배수 × 진행 칸 × 이 값")]
        public float scorePerDistance = 1f;

        [Header("카메라")]
        public float portraitBack = 7.6f;
        public float portraitHeight = 5.5f;
        public float portraitSide = 2.5f;
        public float portraitFov = 48f;
        public float landscapeBack = 9.6f;
        public float landscapeHeight = 6.8f;
        public float landscapeSide = 3.3f;
        public float landscapeFov = 42f;
        public float cameraLookAhead = 4.4f;
        [Tooltip("시선이 플레이어보다 왼쪽으로 가는 정도. 카메라는 오른쪽 위에 있다")]
        public float cameraLookLeft = 1.05f;
        public float cameraSmoothTime = 0.14f;
        public float portraitAspect = 0.5625f;
        public float landscapeAspect = 1.7778f;

        [Header("입력")]
        [Tooltip("스와이프로 인정할 최소 이동(픽셀, dpi 160 기준)")]
        public float swipeThresholdPixels = 36f;

        void Awake()
        {
            Repair();
        }

        /// <summary>
        /// 직렬화 누락으로 0이 들어온 경우 플레이가 가능하게 기본값을 채운다.
        /// </summary>
        public void Repair()
        {
            if (cellSize < 0.1f) cellSize = 1f;
            if (playHalfWidth < 1) playHalfWidth = 5;
            if (playerRadius < 0.05f) playerRadius = 0.26f;
            if (playerBaseY < 0.05f) playerBaseY = 0.3f;
            if (hopDuration < 0.05f) hopDuration = 0.26f;
            if (hopHeight < 0.05f) hopHeight = 0.42f;
            if (quantumHopHeight < hopHeight) quantumHopHeight = 1.15f;
            if (quantumGhostDuration < 0.05f) quantumGhostDuration = 0.32f;
            if (quantumWindowMin < 0f) quantumWindowMin = 0f;
            if (quantumWindowMax <= quantumWindowMin) quantumWindowMax = quantumWindowMin + 0.08f;
            if (quantumJumpBonusPhotons < 0) quantumJumpBonusPhotons = 0;
            if (gateSpeed < 0.1f) gateSpeed = 5.5f;
            if (maxGateSpeed < gateSpeed) maxGateSpeed = gateSpeed;
            if (gateThickness < 0.02f) gateThickness = 0.14f;
            if (gateHeight < 0.2f) gateHeight = 0.78f;
            if (gateDepth < 0.05f) gateDepth = 0.86f;
            if (gateTravelHalf < playHalfWidth) gateTravelHalf = 12f;
            if (gateVisibleHalf < 1f) gateVisibleHalf = 7.5f;
            float minVisible = playHalfWidth * cellSize + 1.25f;
            if (gateVisibleHalf < minVisible) gateVisibleHalf = minVisible;
            if (gateVisibleHalf > gateTravelHalf - 1f) gateVisibleHalf = Mathf.Max(minVisible, gateTravelHalf - 1f);
            if (minGatesPerLane < 1) minGatesPerLane = 1;
            if (maxGatesPerLane < minGatesPerLane) maxGatesPerLane = minGatesPerLane;
            if (safeStartRows < 1) safeStartRows = 5;
            if (aheadRows < 4) aheadRows = 16;
            if (behindRows < 1) behindRows = 7;
            if (maxConsecutiveGateLanes < 1) maxConsecutiveGateLanes = 2;
            if (photonRowChance < 0f) photonRowChance = 0.14f;
            if (photonLateralChance < 0f) photonLateralChance = 0f;
            if (photonLateralReach < 1) photonLateralReach = 1;
            if (photonPickupRadius < 0.05f) photonPickupRadius = 0.45f;
            if (betaPerPhoton <= 0f) betaPerPhoton = 0.16f;
            if (highScoreMinScale <= 0f) highScoreMinScale = 20f;
            if (scorePerDistance <= 0f) scorePerDistance = 1f;
            if (portraitFov < 10f) portraitFov = 48f;
            if (landscapeFov < 10f) landscapeFov = 42f;
            if (portraitSide < 0.2f) portraitSide = 2.5f;
            if (landscapeSide < 0.2f) landscapeSide = 3.3f;
            if (cameraLookLeft < 0.05f) cameraLookLeft = 1.05f;
            if (portraitAspect < 0.2f) portraitAspect = 0.5625f;
            if (landscapeAspect <= portraitAspect) landscapeAspect = 1.7778f;
            if (cameraSmoothTime < 0.01f) cameraSmoothTime = 0.14f;
            if (swipeThresholdPixels < 8f) swipeThresholdPixels = 36f;
        }
    }
}
