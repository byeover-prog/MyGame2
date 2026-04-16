# Player MVP Inspector 설정 가이드 (1차)

대상 씬: `Assets/Scenes/Scene_Game.unity` (동일 구조 씬에 동일 적용 가능)
대상 프리팹: `Assets/_Game/Prefabs/Characters/Player.prefab`

---

## 0. 준비

1) Project 창에서 신규 스크립트 경로 확인
- `Assets/02.Scripts/Runtime/Battle/PlayerMVP/...`

2) Scene에서 Player 오브젝트 선택
- Hierarchy에서 `Player` 선택

---

## 1. 어떤 오브젝트를 선택하는가

- 기본 선택 오브젝트: **Player 루트 오브젝트**
- HUD 바인딩 대상 오브젝트:
  - `PlayerHPUI`가 붙어 있는 HUD 오브젝트
  - HP 텍스트(TMP)가 있는 오브젝트

---

## 2. 어떤 컴포넌트를 추가하는가

Player 루트에 아래 컴포넌트를 추가:
1) `PlayerRuntimeModel`
2) `PlayerView`
3) `LegacyPlayerMovementAdapter`
4) `LegacyPlayerHealthAdapter`
5) `PlayerMovementPresenter`
6) `PlayerHealthPresenter`
7) `PlayerCombatPresenter`
8) `PlayerInstaller`

HUD 쪽(또는 Player 루트)에 추가:
9) `PlayerHudBindingAdapter`

---

## 3. 어떤 필드를 어디에 연결하는가

## 3-1. PlayerView
- `Root` → Player Transform
- `Rb` → Player의 `Rigidbody2D`
- `Animator` → 기존 Player 애니메이터
- `Sprite Renderer` → 기존 Player 스프라이트 렌더러

## 3-2. LegacyPlayerMovementAdapter
- `Legacy Mover` → 기존 `PlayerMover2D`
- `Rb` → Player `Rigidbody2D`

## 3-3. LegacyPlayerHealthAdapter
- `Legacy Health` → 기존 `PlayerHealth`

## 3-4. PlayerMovementPresenter
- `Runtime Model` → `PlayerRuntimeModel`
- `View` → `PlayerView`
- `Legacy Adapter` → `LegacyPlayerMovementAdapter`

## 3-5. PlayerHealthPresenter
- `Runtime Model` → `PlayerRuntimeModel`
- `View` → `PlayerView`
- `Legacy Adapter` → `LegacyPlayerHealthAdapter`

## 3-6. PlayerCombatPresenter
- `Runtime Model` → `PlayerRuntimeModel`

## 3-7. PlayerInstaller
- `Runtime Model` → `PlayerRuntimeModel`
- `Player View` → `PlayerView`
- `Movement Presenter` → `PlayerMovementPresenter`
- `Health Presenter` → `PlayerHealthPresenter`
- `Combat Presenter` → `PlayerCombatPresenter`
- `Legacy Movement Adapter` → `LegacyPlayerMovementAdapter`
- `Legacy Health Adapter` → `LegacyPlayerHealthAdapter`
- `Hud Binding Adapter` → `PlayerHudBindingAdapter`

## 3-8. PlayerHudBindingAdapter
- `Runtime Model` → `PlayerRuntimeModel`
- `Hp UI` → 기존 `PlayerHPUI`
- `Hp Text` → 기존 HP 텍스트 TMP

---

## 4. 누락 시 증상

| 누락 필드 | 증상 |
|---|---|
| PlayerView.Rb | 사망 시 속도 0 강제 동작이 반영되지 않음 |
| LegacyPlayerMovementAdapter.LegacyMover | 이동 상태/방향이 모델에 반영되지 않음 |
| LegacyPlayerHealthAdapter.LegacyHealth | 체력 모델이 0/기본값으로 고정됨 |
| PlayerMovementPresenter.RuntimeModel | 이동 상태 이벤트 미발행 |
| PlayerHealthPresenter.RuntimeModel | HUD 바인딩 갱신 안 됨 |
| PlayerInstaller 핵심 참조 | 초기화 경고 로그 + MVP 동기화 비활성 |
| PlayerHudBindingAdapter.RuntimeModel | HP 바/텍스트가 MVP 이벤트를 받지 못함 |
| PlayerHudBindingAdapter.HpUI | HP 텍스트만 바뀌고 HP 바가 안 움직임 |

---

## 5. 최소 동작 확인 순서

1) Play 진입 후 이동 입력
- 캐릭터가 기존처럼 이동해야 함

2) 적 피격
- 기존 PlayerHealth로 HP 감소
- HP 바/텍스트가 갱신되어야 함

3) 사망 처리
- 기존 사망 로직 동작 유지
- 모델의 dead 상태도 동기화되어야 함

