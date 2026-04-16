# Stage 0/1/7/8 우선 슬라이스 아키텍처 1차 감사 (2026-04-16)

## 0) 범위와 방법
- 분석 범위: Stage 0 튜토리얼, Stage 1 스쿼드 튜토리얼, Stage 7 보스(두억시니), Stage 8 전환(티저) 관련 코드/에셋 경로.
- 증거 수집 방식:
  - C# 코드 참조
  - `.unity` / `.prefab` / `.asset` YAML GUID 참조
  - 런타임 검색/이벤트 결합(`FindFirstObjectByType`, 태그 검색, 정적 이벤트)
  - asmdef 경계 확인
- 1차 패스 원칙: 대규모 삭제/구조 변경 없이, 동작 보존 중심 문서화 + 위험 식별.

---

## 1) 현재 구조 요약

### 1-1. 스테이지 런타임 진입점(공용)
- `StageManager2D`는 카탈로그(`StageCatalogSO`) 기반 스테이지 로딩을 의도하지만, 현재 Scene_Game에서는 `stageCatalog`가 비어 있습니다.
- `StageManager2D`는 `Awake()`에서 `SessionGameManager2D`, `EnemySpawnerTimeline2D`를 `FindFirstObjectByType`로 자동 탐색합니다.
- `SpawnBoss()`는 플레이어를 태그(`Player`)로 찾아 보스 스폰 위치를 계산합니다.

관련 파일:
- `Assets/_Game/Scripts/Stage/StageManager.cs`
- `Assets/Scenes/Scene_Game.unity`

### 1-2. Stage 0 전용 디렉터(개별 경로)
- `Stage0Director`는 엘리트 스폰 → 퀘스트 → 러시 → 궁극기 사용 시 강제 정리/클리어를 자체적으로 처리합니다.
- `RunSignals.UltimateUsed` 정적 이벤트에 직접 구독합니다.
- 플레이어 참조를 `GameObject.FindGameObjectWithTag("Player")`로 획득합니다.
- Scene_Game에서 `ultimatePopup`이 비연결(`fileID: 0`) 상태입니다.

관련 파일:
- `Assets/_Game/Scripts/Stage/Stage0~3/Stage0director.cs`
- `Assets/_Game/Scripts/Core/RunSignals.cs`
- `Assets/Scenes/Scene_Game.unity`

### 1-3. 스테이지 데이터 SO(의도 구조)
- `StageDefinitionSO`, `StageCatalogSO`는 이미 정의되어 있고 `CreateAssetMenu`도 존재합니다.
- 하지만 현재 프로젝트 내 `.asset`/씬/프리팹에서 해당 SO 스크립트 GUID 참조가 확인되지 않습니다(= 연결되지 않은 설계 자산 상태).

관련 파일:
- `Assets/_Game/Scripts/Stage/StageDefinitionSO.cs`
- `Assets/_Game/Scripts/Stage/StageCatalogSO.cs`

### 1-4. Stage 1(스쿼드 튜토리얼) 관련
- `SquadRuntimeBattleBootstrap2D`는 스쿼드 로드아웃 주입용으로 작성되어 있으나, 현재 씬/프리팹 GUID 참조가 없습니다.
- 즉, Stage 1 튜토리얼 관점에서는 “설계된 컴포넌트는 있으나 연결되지 않은 상태”로 판단됩니다.

관련 파일:
- `Assets/_Game/Scripts/Core/Squad/SquadRuntimeBattleBootstrap2D.cs`

### 1-5. Stage 7(두억시니)/Stage 8(전환)
- `StageDefinitionSO`에는 보스 HP 임계치 클리어(`BossHPThreshold`)와 전환 타입(`FallDown` 포함)이 이미 정의되어 있습니다.
- 현재는 해당 SO가 런타임에 연결되지 않아, Stage 7/8을 공용 파이프라인으로 표현하기 어렵습니다.

관련 파일:
- `Assets/_Game/Scripts/Stage/StageDefinitionSO.cs`
- `Assets/_Game/Scripts/Stage/StageClearCondition.cs`

---

## 2) 사용 상태 분류 (증거 기반)

## 2-1. 확실히 사용 중 (definitely used)
1. `Assets/_Game/Scripts/Stage/StageManager.cs`
   - Scene_Game/Scene_HJO/Scene_JGM/Scene_UI 등 여러 `.unity`에서 스크립트 GUID 직접 참조 확인.
2. `Assets/_Game/Scripts/Stage/Stage0~3/Stage0director.cs`
   - Scene_Game에서 스크립트 GUID 직접 참조 확인.
3. `Assets/_Game/Scripts/Core/RunSignals.cs`
   - `Ultimateexecutor2d.cs`에서 발행, `Stage0director.cs`에서 구독 확인.

## 2-2. 레거시지만 살아있는 코드 (legacy but still live)
1. `StageManager2D`의 카탈로그 미연결 fallback 경로
   - Scene_Game에서 `stageCatalog: {fileID: 0}`이며, 코드도 “기본 타이머 모드”를 명시.
   - 즉, 새 구조(SO)로 완전 이행 전 과도기 코드가 실제 런타임 경로로 동작 중.
2. `Stage0Director`의 태그/전역 이벤트 기반 제어
   - 빠른 구현용 경로지만 현재 실제 플레이 경로에 붙어 있어 live 상태.

## 2-3. 미사용 후보 (unused candidate, 단정 아님)
> 아래 항목은 “현재 정적 분석에서 연결을 찾지 못한 후보”이며, 즉시 삭제 금지.

1. `Assets/_Game/Scripts/Stage/StageDefinitionSO.cs`
2. `Assets/_Game/Scripts/Stage/StageCatalogSO.cs`
3. `Assets/_Game/Scripts/Core/Squad/SquadRuntimeBattleBootstrap2D.cs`
4. `Assets/_Game/Scripts/UI/ClearUI/ClearBridge.cs`
5. `Assets/_Game/Scripts/Stage/Stage0~3/Ultimatepopup2d.cs`

근거:
- 각 스크립트 GUID가 `.unity/.prefab/.asset`에서 직접 참조되지 않거나,
- 해당 타입에 대한 `AddComponent<T>()`, 리플렉션 생성 문자열 사용 흔적을 찾지 못함.

---

## 3) 숨은 의존성 (Hidden Dependencies)

1. 오브젝트 탐색 결합
- `FindFirstObjectByType<SessionGameManager2D>()`
- `FindFirstObjectByType<EnemySpawnerTimeline2D>()`
- `FindFirstObjectByType<StageManager2D>()`, `FindFirstObjectByType<ClearUIController>()`
- `GameObject.FindGameObjectWithTag("Player")`

2. 정적 이벤트 결합
- `RunSignals.UltimateUsed`를 통한 Stage0 클리어 트리거.
- 이벤트 발행/구독 경로가 씬 구성과 분리되어 있어, 디버깅 시 순서/중복 구독 리스크 존재.

3. 씬 수동 배선 누락
- Scene_Game의 `Stage0Director.ultimatePopup`이 null.
- 동일 씬에 `Stage0PopUp` 프리팹 인스턴스는 존재하나 참조 배선은 되지 않음.

4. asmdef 경계 부재
- 게임플레이 코드 대부분이 `Assembly-CSharp` 단일 경계로 동작.
- 단계적 모듈 분리(Installer/Composition Root) 시 컴파일 경계가 없어 영향 범위가 큼.

---

## 4) 문제 정의

1. Stage 0 경로와 StageManager 경로가 이원화되어 있습니다.
- Stage0Director가 자체 클리어 이벤트를 발행하고,
- StageManager는 별도 클리어 판단/보상 저장 흐름을 가집니다.
- 결과: 보상/저장/클리어 UI 흐름이 스테이지별로 분기될 가능성이 큼.

2. Stage 1/7/8을 위한 데이터 중심 구조가 코드로만 존재하고 실제 배선이 없습니다.
- `StageDefinitionSO`, `StageCatalogSO`를 도입했지만 씬 연결이 없어 런타임 가치가 없습니다.

3. 런타임 탐색 의존성이 높아 재현성(Deterministic init)이 떨어집니다.
- 씬 오브젝트 이름/태그/활성 순서에 따른 변동 가능성이 있습니다.

---

## 5) 목표 구조 (MVP + Installer/Composition Root)

### 5-1. 대상 구조
- **Composition Root(씬 1개 오브젝트)**
  - Stage slice(0/1/7/8) 공용 참조를 명시적으로 주입.
- **Presenter/Director(MVP P)**
  - Stage0TutorialPresenter
  - Stage1SquadTutorialPresenter
  - Stage7BossPresenter
  - Stage8TeaserTransitionPresenter
- **Model/Config(SO, 읽기 전용)**
  - StageDefinitionSO(런타임 불변)
  - StageCatalogSO(슬라이스만 등록)
  - TutorialStepSO(선택)
- **Runtime State(일반 C# 클래스/MonoBehaviour)**
  - 진행 상태, 타이머, 완료 플래그는 SO에 저장하지 않음.

### 5-2. 유지 원칙
- ScriptableObject에는 런타임 mutable state 저장 금지.
- 핵심 로직에 `Find*`, 태그탐색, `GameObject.Find`, 런타임 `AddComponent` 금지.
- 모든 핵심 참조는 인스펙터에서 명시적으로 배선.

---

## 6) 마이그레이션 단계 (저위험 순서)

### Step 1. 데이터 자산 연결만 먼저 복구 (동작 유지)
1) Stage SO 폴더 생성
- `Assets/_Game/Data/Stages/Definitions/`
- `Assets/_Game/Data/Stages/Catalog/`

2) StageDefinitionSO 생성 (필수 4개 우선)
- Stage0, Stage1, Stage7, Stage8용 4개 에셋.
- Unity 메뉴 경로:
  - `Create > 혼령검 > 스테이지 > 스테이지 정의`

3) StageCatalogSO 생성
- Unity 메뉴 경로:
  - `Create > 혼령검 > 스테이지 > 스테이지 카탈로그`
- 위 4개 StageDefinitionSO를 목록에 등록.

4) Scene_Game 인스펙터 배선
- Hierarchy에서 `StageManager2D`가 붙은 오브젝트 선택.
- `stageCatalog` 슬롯에 생성한 카탈로그 에셋 할당.
- 기존 `currentStageIndex`는 0으로 유지(동작 보존).

### Step 2. Stage0Director와 StageManager 클리어 경로 단일화
1) Stage0Director 클리어 시점에서 `StageManager2D.ForceClear()` 또는 명시 API 호출로 수렴.
2) 보상/저장/클리어 UI는 StageManager 이벤트 하나로 통일.
3) 기존 `OnStageCleared` UnityEvent는 1차 패스에서 유지(역호환).

### Step 3. Stage1/7/8 Presenter 골격 추가
1) 폴더 생성
- `Assets/_Game/Scripts/Stage/Presenters/`
- `Assets/_Game/Scripts/Stage/Installers/`

2) 씬 오브젝트 생성(Hierarchy 순서)
- `StageRuntimeRoot` (빈 GameObject)
- 하위에 `StageRuntimeInstaller`, 각 Presenter 컴포넌트 부착.

3) 인스펙터 배선
- Installer에서 StageManager, UI Presenter, Boss Controller, Transition Controller를 명시 연결.
- `Find*` 제거는 마지막 단계에 진행.

---

## 7) ScriptableObject 설정 가이드 (사용자 친화 순서)

### 7-1. 정확한 파일/폴더 경로
1) 폴더 생성
- `Assets/_Game/Data/Stages/Definitions/`
- `Assets/_Game/Data/Stages/Catalog/`

2) 생성될 에셋 파일 예시
- `Assets/_Game/Data/Stages/Definitions/StageDefinition_Stage0.asset`
- `Assets/_Game/Data/Stages/Definitions/StageDefinition_Stage1.asset`
- `Assets/_Game/Data/Stages/Definitions/StageDefinition_Stage7.asset`
- `Assets/_Game/Data/Stages/Definitions/StageDefinition_Stage8.asset`
- `Assets/_Game/Data/Stages/Catalog/StageCatalog.asset`

### 7-2. Unity 생성 메뉴 경로
1) StageDefinitionSO:
- `Project 창 우클릭 > Create > 혼령검 > 스테이지 > 스테이지 정의`

2) StageCatalogSO:
- `Project 창 우클릭 > Create > 혼령검 > 스테이지 > 스테이지 카탈로그`

### 7-3. Inspector 할당 절차
1) `StageCatalog.asset` 선택.
2) `Stages` 리스트 Size를 최소 4로 설정.
3) Stage0/1/7/8 정의 에셋을 순서대로 드래그.
4) 각 StageDefinition에서 아래 필수값 입력:
- `StageIndex`
- `DisplayName`
- `ClearCondition`
- `BossPrefab`/`BossSpawnTime`(Stage7)
- `TransitionType`(Stage8은 Teaser 목적에 맞는 값)

### 7-4. Hierarchy 오브젝트 절차
1) `Scene_Game` 열기.
2) `StageManager2D` 컴포넌트가 붙은 오브젝트 선택.
3) `stageCatalog`에 `StageCatalog.asset` 할당.
4) (선택) `Stage0` 오브젝트 선택 후 `ultimatePopup` 슬롯에 `Stage0PopUp`의 `UltimatePopup2D` 컴포넌트를 연결.

---

## 8) 리스크 포인트
1. Stage0Director와 StageManager 이벤트 중복으로 클리어 2회 처리 가능성.
2. Stage0PopUp 미배선 상태가 유지되면 궁극기 유도 UX 누락.
3. Stage7 BossHP 임계치 경로는 `ReportBossHP()` 호출 주체가 불명확하여 연출 트리거 누락 위험.
4. Stage8 전환은 enum/필드만 존재하고 실행 주체(Transition Presenter) 부재.

---

## 9) 검증 체크리스트

### 공통
- [ ] Scene_Game Play 시 예외 로그 없음.
- [ ] StageManager 시작 로그에 카탈로그 기반 스테이지 이름 출력.
- [ ] 클리어 시 저장/보상/클리어 UI가 단일 경로로 실행.

### Stage0
- [ ] 120초 엘리트 스폰.
- [ ] 퀘스트 클리어 후 러시 스폰.
- [ ] 궁극기 사용 시 러시/잔여 적 정리 + 클리어.
- [ ] 궁극기 팝업 표시/숨김 정상.

### Stage1 (스쿼드 튜토리얼)
- [ ] 메인/지원 캐릭터 편성 값이 전투 진입 직후 반영.
- [ ] 편성 미설정 시 안전 fallback 동작.

### Stage7 (두억시니)
- [ ] 보스 경고(등장 5초 전) 노출.
- [ ] 보스 스폰 시점/위치 deterministic 동작.
- [ ] HP 임계치 도달 시 연출 트리거.

### Stage8 (티저 전환)
- [ ] 클리어 직후 지정 전환 타입 1회 실행.
- [ ] 전환 후 다음 씬/상태로 정확히 이동.

---

## 10) 이번 1차 패스 결론
- 코드 삭제/대규모 리팩터 없이, 현재 실행 경로와 미연결 설계를 분리해 확인했습니다.
- 다음 실제 코드 변경은 **Stage SO 연결 복구 + Stage0/StageManager 클리어 경로 단일화**를 최소 단위로 권장합니다.
