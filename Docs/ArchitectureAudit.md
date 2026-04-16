# Unity 6 2D 전체 구조 감사 보고서

작성일: 2026-04-16  
대상 프로젝트: `/workspace/MyGame2`  
분석 모드: **읽기 전용(코드 동작 변경 없음)**

---

## 1. 조사 범위/근거

### 1-1. 실제 조사 범위
- `Assets` 전체
- `Packages` 전체
- `ProjectSettings` 전체
- 정적 분석 대상 확장자:
  - `.unity`, `.prefab`, `.asset`, `.meta`, `.cs`, `.asmdef`

### 1-2. 파일 수(실측)
- `.cs`: 414
- `.prefab`: 127
- `.unity`: 67
- `.asset`: 320
- `.meta`: 1793
- `.asmdef`: 1

### 1-3. 참조 추적 방식
- YAML 직렬화 참조(`m_Script guid`) 추적
- `.meta` GUID ↔ C# 스크립트 매핑
- 코드 텍스트 내 런타임 결합 패턴 탐색:
  - `FindFirstObjectByType` / `FindObjectOfType`
  - `GameObject.Find`, `FindGameObjectWithTag`
  - `AddComponent`, `Resources.Load`
  - `UnityEvent`, `event/delegate`, `RunSignals`
  - static `Instance` 싱글톤
  - 리플렉션 (`AppDomain`, `Type.GetType`, `GetMethod`)

---

## 2. 핵심 요약 (문제가 큰 순서)

## 문제 A. Scene_Game 중심의 다중 오케스트레이터 충돌

### 현재 구조
- `Scene_Game`에 `StageManager2D`, `Stage0Director`, `EnemySpawner2D`, `EnemySpawnerTimeline2D`, `LevelUpFlowCoordinator`, `HudConnector`, `Ultimate*`, `Squad*`가 동시에 존재합니다.
- Stage 흐름은 `StageManager2D`와 `Stage0Director` 두 경로가 병렬로 존재합니다.

### 왜 문제인지
- 클리어/보상/UI/진행 상태가 단일 소스로 수렴되지 않아 스테이지별 분기 누수 위험이 큽니다.
- Stage 0/1/7/8 데모 우선 슬라이스에서 가장 먼저 사고가 날 가능성이 높은 지점입니다.

### 추천 구조
- 씬당 단일 Composition Root(`StageRuntimeInstaller`)로 진입점 통일.
- Stage0/1/7/8 Presenter를 Installer에서 명시 주입.
- `StageManager2D`는 Stage 상태 Model/Facade로 축소.

### 마이그레이션 위험
- 이벤트 중복 구독으로 클리어 2회 처리 가능성.
- 기존 UnityEvent 기반 퀘스트 연동이 끊길 위험.

---

## 문제 B. Player God Prefab 구조

### 현재 구조
- `Assets/_Game/Prefabs/Characters/Player.prefab`에 런타임 핵심 스크립트 27개가 집중 부착됨.
- 이동/체력/스킬/궁극기/패시브/스쿼드/HUD 연동까지 단일 프리팹 루트에 결합.
- 부착 스크립트 GUID 중 1개는 매핑 불가(누락 스크립트 GUID: `62899f850307741f2a39c98a8b639597`).

### 왜 문제인지
- 변경 반경이 플레이어 프리팹 전체로 확대되어 회귀 비용이 큼.
- `AddComponent`/이벤트/탐색 의존이 혼재되어 초기화 순서가 불안정.
- 누락 GUID는 인스펙터상 “Missing Script” 잠재 리스크.

### 추천 구조
- Player를 MVP 계층으로 분리:
  - Model: `PlayerRuntimeModel`
  - View: `PlayerView`
  - Presenter: Input/Move/Combat/Health/Ultimate/Squad
  - Installer: `PlayerInstaller`

### 마이그레이션 위험
- 기존 Animator/궁극기 연출 타이밍 붕괴 가능성.
- 패시브 AddComponent 동적 부착 경로를 정적 주입으로 바꿀 때 초기 동작 차이 위험.

---

## 문제 C. Enemy 프리팹 과중복 스택

### 현재 구조
- 적 프리팹 9종 중 5종은 동일 스크립트 5개를 거의 복붙 형태로 공유.
- 공통: `EnemyChaser2D`, `EnemyContactDamage2D`, `EnemyHealth2D`, `EnemyRegistryMember2D`
- 일부만 `EnemyAutoDespawn2D` 존재.

### 왜 문제인지
- 프리팹별 차이가 “데이터 차이”가 아니라 “컴포넌트 조합 차이”로 표현되어 유지보수 난이도 증가.
- 스폰/스탯/보상 정책이 SO 중심으로 완전히 수렴되지 않음.

### 추천 구조
- Enemy MVP + Definition 중심:
  - `EnemyDefinitionSO`에 정적 데이터 집중
  - `EnemyRuntimeModel`로 런타임 상태 분리
  - `EnemyPresenter`/`EnemyView` 표준화
  - `EnemySpawnDirector`가 스폰·등록·해제 단일 책임

### 마이그레이션 위험
- 풀링(`EnemyPool2D`, `EnemyPoolTag`)과 Registry의 수명주기 충돌 가능성.

---

## 문제 D. Find/Signal/Singleton 기반 숨은 결합 누적

### 현재 구조
- 프로젝트 전체에서 탐색/동적 결합 사용량(정적 계수):
  - `Find*`: 61 matches
  - `FindGameObjectWithTag`: 26 matches
  - `GameObject.Find`: 5 matches
  - `AddComponent`: 54 matches
  - `Resources.Load`: 25 matches
  - `UnityEvent`: 13 matches
  - `event/delegate`: 67 matches
  - `static Instance`: 18 matches

### 왜 문제인지
- 씬 구성/활성 순서에 따라 런타임 동작 변동성이 큼.
- 데모 우선 슬라이스(Stage 0/1/7/8)에서 재현성 저하.

### 추천 구조
- Installer에서 전부 명시 참조 연결.
- 신호 허브는 경계별 1개로 제한(예: CombatSignalBus, UISignalBus).

### 마이그레이션 위험
- 기존 디버그 도구/임시 편의 코드가 초기엔 불편해질 수 있음.

---

## 문제 E. 스테이지 데이터 설계와 런타임 연결 불일치

### 현재 구조
- `StageDefinitionSO`, `StageCatalogSO`는 존재하지만 실제 Scene_Game 연결이 비어있는 경로가 존재.
- Stage0 전용 디렉터 경로와 공용 StageManager 경로가 혼재.

### 왜 문제인지
- Stage 7(두억시니)와 Stage 8(티저 전환)을 공용 데이터 파이프라인으로 확장하기 어려움.

### 추천 구조
- Stage 0/1/7/8 정의를 SO로 확정하고 카탈로그를 단일 소스로 사용.
- Stage 연출 Presenter가 StageManager 이벤트를 구독하는 방향으로 통합.

### 마이그레이션 위험
- 기존 직접 참조 UnityEvent 연결이 누락되면 진행 불능 가능성.

---

## 3. 씬별 구조 차이 (주요 씬 비교)

| 씬 | 역할(현재) | 특징 | 충돌/위험 포인트 |
|---|---|---|---|
| `Assets/Scenes/Scene_Game.unity` | 메인 인게임 | 시스템이 가장 많이 밀집(스테이지/스폰/레벨업/HUD/궁극기/스쿼드) | 오케스트레이터 중복, Stage0 전용 흐름 혼재 |
| `Assets/Scenes/Scene_HJO.unity` | 인게임 변형 A | Scene_Game과 유사한 매니저 다수 + HUD 변형 | 씬별 HUD 스크립트 조합 차이로 기능 편차 위험 |
| `Assets/Scenes/Scene_JGM.unity` | 인게임 변형 B | 일부 HUD/레벨업 패널 구성이 다름 | 같은 기능의 구현 경로가 씬마다 다름 |
| `Assets/Scenes/Scene_UI.unity` | UI 중심 테스트/통합 | 인게임 핵심 일부 + UI 확인용 구성 | 테스트 씬과 실플레이 씬 간 동작 불일치 가능 |
| `Assets/Scenes/Scene_Boot.unity` | 부트/진입 | RootBootstrapper + 스쿼드 UI 진입 | Boot↔Lobby↔Game 전환 계약 불명확 |
| `Assets/Scenes/Scene_Lobby.unity` | 로비 | 상대적으로 단순 | 실제 전투 진입 시점 데이터 전달 경계 취약 |

---

## 4. 기능 흐름 점검

### 4-1. 레벨업
- `PlayerExp.OnLevelUp` → `LevelUpOpenOnPlayerExp` → `LevelUpFlowCoordinator.RequestLevelUp()` → `LevelUpPanelController`.
- 시간 정지는 `GamePauseGate2D`를 사용.
- 위험: Coordinator/Panel 참조 일부가 `FindFirstObjectByType` fallback 사용.

### 4-2. 스킬
- `SkillRunner`가 스킬 프리팹 장착/레벨 적용 책임.
- `CommonSkillManager2D`도 별도 획득/업그레이드 경로를 가짐.
- 위험: 유사 책임 2개 축(공통스킬 매니저 vs 러너)이 공존.

### 4-3. 궁극기
- 입력: `UltimateController2D`(R), `SupportUltimateController2D`(T)
- 실행: `UltimateExecutor2D` + resolver/presenter
- Stage0 연동: `RunSignals.RaiseUltimateUsed()`를 `Stage0Director`가 구독.
- 위험: 게임 규칙과 연출 규칙이 정적 신호로 강결합.

### 4-4. 패시브
- `CharacterPassiveManager2D`가 `AddComponent`로 패시브 컴포넌트를 런타임 부착.
- 위험: 타입 확장 시 런타임 조립 비용 증가 + 테스트 어려움.

### 4-5. 스쿼드
- `SquadLoadout2D` + `SquadApplier2D` 이벤트 기반 반영.
- 일부 경로는 런타임 브릿지(`SquadLoadoutRuntime`)를 통해 저장값과 동기화.
- 위험: 씬 전환 경계에서 소스 오브 트루스 분산.

### 4-6. HUD
- `HudConnector`가 HP/타이머/스킬/패시브까지 광범위 브릿지 역할.
- 위험: Presenter가 아닌 “집중형 동기화 스크립트”로 비대화.

### 4-7. 스폰
- `EnemySpawner2D`(레거시+타임라인 혼합)와 `EnemySpawnerTimeline2D`(타임라인 전용)가 병존.
- 위험: 어느 스포너가 주도권을 갖는지 씬 설정에 따라 달라짐.

---

## 5. 사용 상태 분류 정책

자세한 목록은 아래 문서를 기준으로 관리:
- `Docs/LegacyButLiveMap.md`
- `Docs/UnusedCodeCandidates.md`
- `Docs/ScenePrefabScriptMatrix.csv`

분류 기준:
- **Used**: YAML 직렬화 참조 + 코드 경로가 모두 확인됨
- **Legacy but live**: 구조적으로 구식이나 실제 씬/런타임에서 아직 동작
- **Unused candidate**: 현재 연결 미확인(단정 금지, 위험도 포함)

---

## 6. 결론 (요청된 5개 항목)

### 6-1. 당장 손대야 하는 구조 5개
1) `StageManager2D` vs `Stage0Director` 이중 스테이지 흐름 통합  
2) Player God Prefab 분해(핵심 책임 Presenter 분리)  
3) Enemy 프리팹 중복 스택을 Definition 중심으로 전환  
4) `Find*`/정적 신호/싱글톤 의존 축소 및 Installer 명시 주입화  
5) 레벨업/스킬/궁극기/HUD 이벤트 경로 단일화

### 6-2. Player MVP 1차 마이그레이션 범위
- 범위: Stage 0/1/7/8 데모에 직접 영향 큰 영역만 우선
  - `PlayerInputPresenter`
  - `PlayerMovementPresenter`
  - `PlayerCombatPresenter`
  - `PlayerUltimatePresenter`
  - `PlayerHealthPresenter`
  - `PlayerInstaller`
- 기존 컴포넌트는 어댑터로 유지하여 동작 보존.

### 6-3. Enemy MVP 1차 마이그레이션 범위
- 공통 9종 적을 동일 파이프라인으로 수렴:
  - `EnemyDefinitionSO`
  - `EnemyRuntimeModel`
  - `EnemyPresenter`
  - `EnemyView`
  - `EnemySpawnDirector`
- 우선: 스폰/등록/피격/보상/디스폰의 단일 라이프사이클 정렬.

### 6-4. 레벨업/스킬/이벤트 시스템 통합 우선순위
1) 레벨업 진입점 단일화 (`PlayerExp -> LevelUpCoordinator`)  
2) 스킬 장착/레벨 반영 책임 통합 (`SkillRunner`/`CommonSkillManager2D` 경계 정리)  
3) 궁극기/스테이지 연동은 정적 신호 대신 명시 인터페이스 이벤트로 전환  
4) HUD 갱신 경로를 기능별 Presenter로 분해

### 6-5. 안전하게 삭제 가능한 후보 목록
- 즉시 삭제는 본 보고서에서 권고하지 않음(리스크 제어 목적).  
- **안전 삭제 후보는 `Docs/UnusedCodeCandidates.md`에서 `삭제 가능(낮은 위험)` 등급만 추출하여 별도 검증 후 진행**.

