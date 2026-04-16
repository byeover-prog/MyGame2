# Player Prefab 감사 보고서

대상: `Assets/_Game/Prefabs/Characters/Player.prefab`

---

## 1. 현재 구조 수집 결과

- Player prefab에 연결된 MonoBehaviour 스크립트 GUID 수: 27
- 매핑된 C# 스크립트: 26개 + **누락 GUID 1개**
  - 누락 GUID: `62899f850307741f2a39c98a8b639597`

### 1-1. 핵심 부착 스크립트(요약)
- 이동/생존: `PlayerMover2D`, `PlayerHealth`, `PlayerDashController`, `PlayerSpriteFlip2D`
- 전투/스킬: `WeaponShooterSystem2D`, `PlayerSkillLoadout`, `PlayerStatRuntimeApplier2D`
- 궁극기: `UltimateController2D`, `UltimateExecutor2D`, `UltimatePresenter2D`, `SupportUltimateController2D`, `HayulUltimate*`
- 편성/패시브: `SquadLoadout2D`, `SquadApplier2D`, `CharacterPassiveManager2D`, `BattleBuffController2D`, `AttributeSynergyManager2D`
- 경제/성장: `PlayerExp`, `PlayerCurrency2D`, `PlayerCombatStats2D`

---

## 2. 책임/의존성/중복 책임 분석

## 문제 1. 루트 오브젝트에 책임 과밀 (God Prefab)

### 현재 구조
- Player 루트가 입력, 이동, 전투, 스킬 장착, 궁극기 실행, 스쿼드 적용, 패시브 스위칭, 버프 계산까지 보유.

### 왜 문제인지
- 단일 변경(예: 궁극기 입력 키 수정)도 플레이어 전체 회귀 테스트를 강제.
- Stage 0/1/7/8 데모 우선 흐름에서 장애 전파 범위가 지나치게 큼.

### 추천 구조
- Presenter 단위로 책임 분할 + Installer 조립.

### 마이그레이션 위험
- Animator/타격 판정 타이밍 붕괴 가능성. 기존 실행 순서 고정 필요.

---

## 문제 2. 런타임 결합 방식 혼재

### 현재 구조
- 이벤트 기반: `RunSignals.StageStarted`, `PlayerExp.OnLevelUp`, `OnLoadoutChanged`
- 런타임 생성: `CharacterPassiveManager2D`의 `AddComponent<...>()`
- 탐색/폴백: 일부 스크립트의 `FindFirstObjectByType` fallback

### 왜 문제인지
- 순서 의존성이 생기며, 씬 구성 변경 시 재현성 저하.

### 추천 구조
- Installer에서 명시 주입 + 런타임 생성 최소화.

### 마이그레이션 위험
- 기존 fallback이 제거될 때 참조 누락 초기 오류 가능.

---

## 문제 3. 중복 책임

### 현재 구조
- 스킬 적용 책임이 `SkillRunner`와 `CommonSkillManager2D`로 분산.
- 궁극기 제어가 메인/지원 분기에서 컨트롤러가 이중화.

### 왜 문제인지
- 스킬 레벨/장착 상태와 HUD 반영 사이 동기화 결함이 발생하기 쉬움.

### 추천 구조
- `PlayerCombatPresenter` 하에 스킬/궁극기 조정기(Orchestrator) 통합.

### 마이그레이션 위험
- 기존 카드/레벨업/궁극기 연계 계약 깨짐 위험.

---

## 3. 런타임 연결 방식 상세

| 항목 | 현재 방식 | 위험 |
|---|---|---|
| Find 계열 | 일부 스크립트에서 `FindFirstObjectByType`, `FindGameObjectWithTag` 사용 | 씬 의존, 테스트 어려움 |
| Singleton/static | 각 시스템별 `Instance` 패턴 혼재 | 초기화 순서 충돌 |
| signal/event | `RunSignals` + C# event + UnityEvent 혼재 | 추적 난이도 상승 |
| runtime AddComponent | 패시브 매니저가 런타임 부착 | 타입 확장 시 제어 어려움 |

---

## 4. 왜 God Prefab이 되었는지 (구조적 설명)

1) 프로토타입 단계에서 기능을 Player 루트에 빠르게 누적  
2) Stage/스킬/궁극기/HUD가 “각자 직접 Player 참조”로 연결  
3) 시스템 통합 계층(Installer/Composition Root) 부재  
4) Scene 단위 임시 결합이 유지되며 프리팹이 사실상 “게임 컨테이너”화

---

## 5. ASCII 다이어그램

### 5-1. 현재 구조

```text
Player (Prefab Root)
├─ Movement: PlayerMover2D, PlayerDashController, PlayerSpriteFlip2D
├─ Survival: PlayerHealth, PlayerCombatStats2D, PlayerCurrency2D
├─ Skill: WeaponShooterSystem2D, PlayerSkillLoadout, Skill binders
├─ Ultimate: UltimateController2D, UltimateExecutor2D, SupportUltimateController2D, presenters
├─ Squad: SquadLoadout2D, SquadApplier2D
├─ Passive/Buff: CharacterPassiveManager2D(+AddComponent), BattleBuffController2D, AttributeSynergyManager2D
└─ Meta bridge: PlayerExp, HUD-linked scripts
```

### 5-2. 목표 구조(MVP)

```text
PlayerInstaller
├─ Model: PlayerRuntimeModel
├─ View: PlayerView
├─ Presenter: PlayerInputPresenter
├─ Presenter: PlayerMovementPresenter
├─ Presenter: PlayerCombatPresenter
├─ Presenter: PlayerHealthPresenter
├─ Presenter: PlayerUltimatePresenter
└─ Presenter: PlayerSquadPresenter
```

---

## 6. Player MVP 1차 범위 (데모 우선)

- Stage 0/1/7/8에 직접 영향 큰 축만 우선 분리:
  1. Input(궁극기/대시/이동) Presenter
  2. Combat(무기 발사/타겟팅) Presenter
  3. Health Presenter
  4. Ultimate Presenter
  5. Squad Presenter
- 기존 MonoBehaviour는 Adapter 계층으로 임시 유지.

---

## 7. 즉시 확인 필요 체크리스트
- [ ] Player prefab의 누락 GUID(`62899...`) 실제 Missing Script 여부 확인
- [ ] Stage 0 튜토리얼에서 궁극기 이벤트 경로(`RunSignals`) 단일 실행 보장
- [ ] Stage 1 진입 시 Squad 적용 타이밍(시작 1프레임 지연 포함) 검증
- [ ] Stage 7 보스전에서 Player HUD/Ultimate 쿨다운 동기화 검증

