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

        [Tooltip("입자 충돌 반경. 양자 도약 판정에도 사용")]
        public float playerRadius = 0.34f;

        [Tooltip("입자가 바닥에 닿아 있을 때의 중심 높이")]
        public float playerBaseY = 0.36f;

        [Header("홉")]
        [Tooltip("한 칸 이동에 걸리는 시간(초)")]
        public float hopDuration = 0.2f;

        public float hopHeight = 0.65f;

        [Tooltip("양자 도약일 때 더 높이 뜨는 궤적")]
        public float quantumHopHeight = 1.45f;

        [Header("양자 도약")]
        [Tooltip("게이트가 착지 칸에 닿기까지 남은 시간이 이 값보다 짧으면 너무 늦음")]
        public float quantumWindowMin = 0.06f;

        [Tooltip("게이트가 착지 칸에 닿기까지 남은 시간이 이 값보다 길면 너무 이름")]
        public float quantumWindowMax = 0.42f;

        [Tooltip("위상을 통과한 뒤 판정을 조금 더 유지하는 여유(초)")]
        public float quantumPhasePadding = 0.08f;

        [Tooltip("양자 도약 성공 시 추가로 얻는 광자 수")]
        public int quantumJumpBonusPhotons = 2;

        [Header("측정 게이트")]
        public float gateSpeed = 5.5f;
        public float gateSpeedPerRow = 0.035f;
        public float maxGateSpeed = 12f;
        public float gateLength = 2.1f;
        public float gateThickness = 0.16f;
        public float gateHeight = 1.35f;
        public float gateTravelHalf = 12f;
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
        public int photonsPerSafeLaneMin = 1;
        public int photonsPerSafeLaneMax = 2;

        [Header("확률 / 점수")]
        [Tooltip("광자 하나가 고득점 쪽으로 분포를 기울이는 세기")]
        public float betaPerPhoton = 0.16f;

        [Tooltip("이 배수 이상인 상태를 고득점으로 친다. 기본 20 = 높음·전설")]
        public float highScoreMinScale = 20f;

        [Tooltip("측정 점수 = 상태 배수 × 진행 칸 × 이 값")]
        public float scorePerDistance = 1f;

        [Header("카메라")]
        public float portraitBack = 6.8f;
        public float portraitHeight = 8.2f;
        public float portraitFov = 50f;
        public float landscapeBack = 10.5f;
        public float landscapeHeight = 12.5f;
        public float landscapeFov = 46f;
        public float cameraLookAhead = 4.2f;
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
            if (playerRadius < 0.05f) playerRadius = 0.34f;
            if (playerBaseY < 0.05f) playerBaseY = 0.36f;
            if (hopDuration < 0.05f) hopDuration = 0.2f;
            if (hopHeight < 0.05f) hopHeight = 0.65f;
            if (quantumHopHeight < hopHeight) quantumHopHeight = 1.45f;
            if (quantumWindowMin < 0f) quantumWindowMin = 0f;
            if (quantumWindowMax <= quantumWindowMin) quantumWindowMax = quantumWindowMin + 0.3f;
            if (quantumPhasePadding < 0f) quantumPhasePadding = 0f;
            if (quantumJumpBonusPhotons < 0) quantumJumpBonusPhotons = 0;
            if (gateSpeed < 0.1f) gateSpeed = 5.5f;
            if (maxGateSpeed < gateSpeed) maxGateSpeed = gateSpeed;
            if (gateLength < 0.2f) gateLength = 2.1f;
            if (gateThickness < 0.02f) gateThickness = 0.16f;
            if (gateHeight < 0.2f) gateHeight = 1.35f;
            if (gateTravelHalf < playHalfWidth) gateTravelHalf = 12f;
            if (minGatesPerLane < 1) minGatesPerLane = 1;
            if (maxGatesPerLane < minGatesPerLane) maxGatesPerLane = minGatesPerLane;
            if (safeStartRows < 1) safeStartRows = 5;
            if (aheadRows < 4) aheadRows = 16;
            if (behindRows < 1) behindRows = 7;
            if (maxConsecutiveGateLanes < 1) maxConsecutiveGateLanes = 2;
            if (photonsPerSafeLaneMin < 0) photonsPerSafeLaneMin = 0;
            if (photonsPerSafeLaneMax < photonsPerSafeLaneMin) photonsPerSafeLaneMax = photonsPerSafeLaneMin;
            if (betaPerPhoton <= 0f) betaPerPhoton = 0.16f;
            if (highScoreMinScale <= 0f) highScoreMinScale = 20f;
            if (scorePerDistance <= 0f) scorePerDistance = 1f;
            if (portraitFov < 10f) portraitFov = 50f;
            if (landscapeFov < 10f) landscapeFov = 46f;
            if (portraitAspect < 0.2f) portraitAspect = 0.5625f;
            if (landscapeAspect <= portraitAspect) landscapeAspect = 1.7778f;
            if (cameraSmoothTime < 0.01f) cameraSmoothTime = 0.14f;
            if (swipeThresholdPixels < 8f) swipeThresholdPixels = 36f;
        }
    }
}
