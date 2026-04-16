# Player MVP 1차 구현 노트

작성일: 2026-04-16
범위: 이동 + 체력 + 기본 전투 상태 + HUD 바인딩 골격

---

## 1. 이번 턴에서 추가한 구조

신규 코드 루트:
- `Assets/02.Scripts/Runtime/Battle/PlayerMVP/Models`
- `Assets/02.Scripts/Runtime/Battle/PlayerMVP/Views`
- `Assets/02.Scripts/Runtime/Battle/PlayerMVP/Presenters`
- `Assets/02.Scripts/Runtime/Battle/PlayerMVP/Installers`
- `Assets/02.Scripts/Runtime/Battle/PlayerMVP/Adapters`

핵심 구성:
1) `PlayerRuntimeModel`
2) `PlayerStatModel`
3) `PlayerHealthModel`
4) `PlayerView`
5) `PlayerMovementPresenter`
6) `PlayerHealthPresenter`
7) `PlayerCombatPresenter` (골격)
8) `PlayerInstaller`
9) `LegacyPlayerMovementAdapter`, `LegacyPlayerHealthAdapter`
10) `PlayerHudBindingAdapter`

---

## 2. 병행 전략 (기존 플레이 보존)

- 기존 `PlayerMover2D`, `PlayerHealth`는 제거하지 않음.
- 새 MVP는 **레거시 값을 어댑터로 읽어 모델에 반영**하는 방식.
- 즉, 기존 동작이 먼저 실행되고 MVP는 상태 동기화/확장 포인트를 제공.

### 유지한 레거시 축
- 이동 실제 처리: `PlayerMover2D`
- 체력 실제 처리: `PlayerHealth`
- 기존 HUD 브릿지: `HudConnector`

### 새 구조 역할
- Model 계층: Presenter/HUD가 공통으로 읽는 상태 소스
- Presenter 계층: 레거시와 신규 로직의 접점
- Adapter 계층: 레거시 컴포넌트 접근 캡슐화

---

## 3. 이벤트/바인딩 골격

- `PlayerHealthModel.OnHealthChanged(int current, int max)`
- `PlayerHealthModel.OnDead()`
- `PlayerRuntimeModel.OnCombatStateChanged(bool)`
- `PlayerRuntimeModel.OnDamageGateChanged(bool)`
- `PlayerHudBindingAdapter`가 `OnHealthChanged`를 수신하여
  `PlayerHPUI`/`TMP_Text`에 반영.

---

## 4. 이번 턴 의도적 비이식 범위

아래 항목은 이번 턴에서 완전 이식하지 않음:
- 무기 전체
- 궁극기 전체
- 레벨업 전체
- 스쿼드 전체

사유:
- 플레이 가능성 보존이 우선이며, MVP 골격 안정화 후 단계 전환 필요.

---

## 5. 다음 턴 권장 확장 순서

1) `PlayerMovementPresenter`를 입력 포트 기반으로 전환 (레거시 의존 축소)
2) `PlayerCombatPresenter`에 피격/공격 이벤트 연계
3) `HudConnector`의 HP 경로를 MVP 이벤트 기반으로 치환

