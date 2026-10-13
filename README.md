# 길건너 양자들 (Quantum Crossy)

Crossy Road처럼 한 칸씩 앞으로 건너는 게임이다. 플레이어는 양자 입자이고, 차는 얇은 **측정 게이트**로 바뀌었다. 광자(빛줄기)를 주울수록 고득점 확률이 올라가고, 게이트에 닿으면 그 확률 분포에서 점수를 뽑아 런이 끝난다.

구조(격자 홉, 절차적 레인 생성, 카메라 추적)는 [EasyCastleUNITY의 길건너 친구들 모작 일지](https://easycastleunity.tistory.com/category/3D%20%ED%94%84%EB%A1%9C%EC%A0%9D%ED%8A%B8%20%EA%B0%9C%EB%B0%9C%20%EC%9D%BC%EC%A7%80%28Crossy%20Road%2C%20%ED%95%9C%EA%B8%80%EB%AA%85%3A%20%EA%B8%B8%EA%B1%B4%EB%84%88%20%EC%B9%9C%EA%B5%AC%EB%93%A4%29?page=1)의 큰 흐름을 참고했다. 코드는 그 글을 복사하지 않고 새로 작성했다.

## 실행

1. Unity 6000.3에서 프로젝트를 연다.
2. `Assets/Scenes/QuantumCrossy.unity`가 빌드 첫 씬이다. 이 씬을 열고 Play.
3. 게임 뷰는 **9:16 세로**가 기준이다. 카메라는 오른쪽 위에서 왼쪽 아래를 보는 비스듬한 시선이고, 창이 넓어지면 거리만 늘어난다. 위에서 수직으로 내려다보지 않는다.

모바일(세로, 스와이프)이 목표이고, 지금은 PC에서 키보드와 마우스 드래그로 먼저 확인하면 된다.

## 조작

키보드와 스와이프는 같은 이동 명령(`MoveCommand`)으로 모인다.

| 입력 | 동작 |
| --- | --- |
| W / ↑ / 스페이스, 위로 스와이프 | 전진 |
| S / ↓, 아래로 스와이프 | 후진 |
| A / ←, 왼쪽 스와이프 | 왼쪽 |
| D / →, 오른쪽 스와이프 | 오른쪽 |
| 마우스 드래그 | 스와이프와 동일 (PC에서 터치 확인용) |
| 게이트에 닿은 뒤 R / Enter / 스페이스, 또는 `다시 하기` | 재시작 |

한 칸 홉이 끝나기 전에 다음 입력을 넣으면 한 번만 예약된다. 화면 밖으로 나가는 좌우 이동은 무시된다.

시작 전에 점수 계산을 짧게 보여 준다. 탭하거나 아무 키나 누르면 닫히고 게임이 시작된다. 다시 하기를 해도 같은 안내가 먼저 나온다.

## 규칙

- 배치는 격자다. 한 번 입력에 한 칸. 이동은 낮은 호를 그린다. 이 높이로는 판을 넘지 못한다.
- 앞쪽 레인은 플레이하면서 만들어지고, 뒤쪽은 지워진다. 처음 몇 줄은 항상 안전하다.
- **광자**를 먹으면 점수 분포가 고득점 쪽으로 기운다. 안전 레인에 드물게, 대부분 진행 축(가운데 열)에만 있다.
- **측정 게이트**는 레인을 가로지르는 얇은 판이다. 두께는 이동 방향(X)으로 얇고, 높이와 레인 방향 너비만 있다. 판의 그 부피에 구체가 들어가면 측정이다. 무적 시간은 없다.
- 측정이 일어나면 현재 분포에서 상태(바닥 / 낮음 / 중간 / 높음 / 전설)를 하나 뽑고 런이 끝난다. 게이트는 붕괴 후에도 계속 움직인다.
- 점수 = 상태 배수 × 앞으로 간 칸 수 × `scorePerDistance`.
  - 바닥 1, 낮음 3, 중간 8, 높음 20, 전설 50
- 고득점 확률은 배수 `highScoreMinScale`(기본 20, 높음·전설) 이상인 확률의 합이다. 광자가 없을 때 8%에서 시작한다. 광자 하나마다 `beta`가 `betaPerPhoton`만큼 오르고(상한 6), 상태 i의 가중치는 기본 가중치 × exp(beta × i)이다.
- 게이트는 `gateVisibleHalf` 밖에서는 렌더러를 끄고, `gateTravelHalf`에서 반대편으로 옮긴다. 옮기는 동안은 숨겨 두므로 화면 한가운데를 가로지르며 나타나지 않는다.
- 광자는 물리 트리거가 아니라, 이동 선분과 광자 중심의 수평 거리(`photonPickupRadius`)로 줍는다. 플레이어·게이트·광자 아래에는 바닥에 붙는 그림자가 있다.

광자 수에 따른 고득점 확률(기본 `betaPerPhoton` 0.16):

| 광자 | 0 | 2 | 4 | 6 | 8 | 10 |
| --- | --- | --- | --- | --- | --- | --- |
| 고득점 확률 | 8% | 17% | 30% | 48% | 65% | 78% |

## 튜닝

씬의 `QuantumCrossy` 오브젝트 → `QuantumCrossyConfig`. 플레이 중에 바꾼 값은 저장되지 않는다.

| 필드 | 기본 | 의미 |
| --- | --- | --- |
| `cellSize` | 1 | 한 칸 크기 |
| `playHalfWidth` | 5 | 좌우 한계 (칸) |
| `playerRadius` | 0.26 | 측정에 쓰는 구체 반경 |
| `hopDuration` | 0.26 | 홉 시간(초) |
| `hopHeight` | 0.42 | 홉 높이. 판보다 낮아서 이 높이로는 넘지 못한다 |
| `gateSpeed` / `gateSpeedPerRow` / `maxGateSpeed` | 5.5 / 0.035 / 12 | 게이트 속도와 거리 가속 |
| `gateThickness` / `gateHeight` / `gateDepth` | 0.14 / 0.78 / 0.86 | 판의 X 두께, 높이, Z 너비. 충돌은 이 박스뿐이다 |
| `gateTravelHalf` / `gateVisibleHalf` | 12 / 7.5 | 반대편으로 옮기는 지점 / 이보다 바깥이면 숨김 |
| `minGatesPerLane` / `maxGatesPerLane` | 1 / 2 | 레인당 게이트 수 |
| `safeStartRows` | 5 | 이 행 전까지는 안전 |
| `gateLaneWeight` | 0.58 | 그 다음 게이트 레인이 나올 확률 |
| `maxConsecutiveGateLanes` | 2 | 게이트 레인이 연속으로 나올 수 있는 수 |
| `photonRowChance` | 0.14 | 안전 레인에 광자 하나가 나올 확률 |
| `photonLateralChance` / `photonLateralReach` | 0.05 / 1 | 그 광자가 가운데가 아닐 확률, 그리고 최대 옆 칸 |
| `photonPickupRadius` | 0.45 | 이 수평 거리 안이면 광자를 획득 |
| `betaPerPhoton` | 0.16 | 광자 하나가 분포를 기울이는 세기 |
| `highScoreMinScale` | 20 | 이 배수 이상을 고득점으로 집계 |
| `scorePerDistance` | 1 | 점수 배율 |
| `portraitBack` / `portraitHeight` / `portraitSide` | 7.6 / 5.5 / 2.5 | 세로 화면에서 카메라의 뒤·높이·오른쪽 거리 |
| `landscapeBack` / `landscapeHeight` / `landscapeSide` | 9.6 / 6.8 / 3.3 | 가로로 넓어질 때의 같은 값 |
| `cameraLookAhead` / `cameraLookLeft` | 4.4 / 1.05 | 시선은 앞쪽, 그리고 플레이어보다 왼쪽. 카메라는 오른쪽 위에서 내려다본다 |
| `swipeThresholdPixels` | 36 | 스와이프 최소 거리(dpi 160 기준 픽셀) |

## 폴더

`Assets/Scripts`

- `Input` — `MoveCommand`, 키보드/스와이프 원천, 둘을 모으는 `MoveInput`
- `Player` — 격자 홉, 판과의 충돌
- `Lanes` — 절차적 레인
- `Gates` — 측정 게이트
- `Rays` — 광자
- `Scoring` — 확률 분포와 측정 샘플
- `Camera` — 비율에 맞추는 추적 카메라
- `UI` — 시작 전 점수 안내, 고득점 확률 HUD, 붕괴 화면
- `Core` — 설정, 부트스트랩, 게임 흐름
- `Visuals` — URP 기본 머티리얼

비주얼은 큐브·구체 같은 프리미티브다.

## 라이선스

게임 코드는 이 저장소에서 새로 작성했고, 외부 오픈소스 코드를 재사용하지 않았다.

한글 UI 폰트만 예외다.

- 폰트: [Gothic A1](https://fonts.google.com/specimen/Gothic+A1) Regular (HanYang I&C)
- 라이선스: SIL Open Font License 1.1
- 파일: `Assets/Resources/Fonts/GothicA1-Regular.ttf`
- 전문: `Assets/Resources/Fonts/OFL.txt`, `Assets/ThirdParty/GothicA1/OFL.txt`

폰트 자체는 따로 판매할 수 없고, 이 라이선스 문구를 함께 두어야 한다. 예약된 폰트 이름으로 파생 폰트를 만들 수 없다.
