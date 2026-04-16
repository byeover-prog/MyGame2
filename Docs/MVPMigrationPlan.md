# MVP 마이그레이션 계획 (Stage 0/1/7/8 우선)

원칙:
- 동작 보존 우선
- 1차는 연결/분리만 수행, 대량 삭제 금지
- ScriptableObject에는 런타임 상태 저장 금지

---

## 1. 목표 아키텍처

## 1-1. Player MVP
- Model: `PlayerRuntimeModel`
- View: `PlayerView`
- Presenter:
  - `PlayerInputPresenter`
  - `PlayerMovementPresenter`
  - `PlayerCombatPresenter`
  - `PlayerHealthPresenter`
  - `PlayerUltimatePresenter`
  - `PlayerSquadPresenter`
- Installer:
  - `PlayerInstaller`

## 1-2. Enemy MVP
- SO: `EnemyDefinitionSO`
- Model: `EnemyRuntimeModel`
- View: `EnemyView`
- Presenter: `EnemyPresenter`
- Spawn 조정기: `EnemySpawnDirector`

## 1-3. Stage/HUD
- `StageRuntimeInstaller`가 Stage0/1/7/8 Presenter를 명시 주입
- HUD를 기능별 Presenter로 분리
  - `HudHpPresenter`, `HudTimerPresenter`, `HudSkillPresenter`, `HudResultPresenter`

---

## 2. SO와 런타임 Model 경계

## 2-1. SO에 넣을 것 (정적 설정)
- Stage: 제한시간, 클리어조건, 보스 경고 텍스트, 전환 타입
- Player: 캐릭터 정의, 궁극기 정의, 기본 스킬/아이콘
- Enemy: 기본 HP/속도/공격/드랍/애니메이션 레퍼런스

## 2-2. 런타임 Model에 둘 것 (변동 상태)
- Player: 현재 HP/EXP/쿨다운/버프/현재 편성 적용 상태
- Enemy: 현재 HP/AI 상태/현재 타겟/풀링 활성 상태
- Stage: 경과시간/클리어 여부/현재 웨이브 인덱스/보스 스폰 플래그

---

## 3. 단계별 전환

## 3-1. 1단계 (연결 복구, 동작 유지)
- Stage 카탈로그(SO) 연결 복구
- Scene별 핵심 참조를 Installer 오브젝트로 집약
- `Find*` fallback은 남기되 경고 로그 추가

## 3-2. 2단계 (Presenter 분해)
- Player God Prefab에서 입력/이동/전투/궁극기/체력/스쿼드 Presenter 분리
- Enemy 스폰 단일화(`EnemySpawnDirector`)

## 3-3. 3단계 (정리)
- 레거시 브릿지 제거
- 이벤트 버스 축소
- 미사용 후보 삭제(검증 완료분만)

---

## 4. 우선순위 (펀딩/데모 반영)
1) Stage 0 튜토리얼 안정화  
2) Stage 1 스쿼드 튜토리얼 연결 안정화  
3) Stage 7 두억시니 보스 플로우 고정  
4) Stage 8 티저 전환 단일 경로  
5) HUD/결과 UI/메인 메뉴/스킬 트리/상점 UI 동기화

---

## 5. 문제별 제안 포맷

## 문제: Stage 오케스트레이터 중복
### 현재 구조
- `StageManager2D` + `Stage0Director` 병렬
### 왜 문제인지
- 클리어/보상/UI 경로 중복
### 추천 구조
- `StageRuntimeInstaller` + Stage별 Presenter
### 마이그레이션 위험
- 기존 UnityEvent 연결 누락

## 문제: Player God Prefab
### 현재 구조
- 루트에 20+ 스크립트 집중
### 왜 문제인지
- 회귀 범위 과도
### 추천 구조
- Player MVP + Installer
### 마이그레이션 위험
- 애니메이션/궁극기 타이밍 차이

## 문제: Enemy 중복 프리팹
### 현재 구조
- 동일 스택 반복
### 왜 문제인지
- 변경 비용 증가
### 추천 구조
- DefinitionSO + RuntimeModel + Presenter
### 마이그레이션 위험
- 풀링/등록 생명주기 충돌

---

## 6. 초보자용 적용 순서 (SO 이해 지원)

## 6-1. Tool > 혼령검 > SO > 필수 폴더 만들기
1) Project 창에서 폴더 생성
- `Assets/_Game/Data/Stages/Definitions/`
- `Assets/_Game/Data/Stages/Catalog/`
- `Assets/_Game/Data/Enemies/Definitions/`
- `Assets/_Game/Data/Players/Definitions/`

2) SO 생성 메뉴
- Stage 정의: `Create > 혼령검 > 스테이지 > 스테이지 정의`
- Stage 카탈로그: `Create > 혼령검 > 스테이지 > 스테이지 카탈로그`
- (Enemy/Player는 실제 코드 메뉴명에 맞춰 추가)

## 6-2. Hierarchy 생성 순서
1) `Scene_Game` 열기
2) 빈 오브젝트 생성: `StageRuntimeRoot`
3) `StageRuntimeRoot` 하위에:
- `StageRuntimeInstaller`
- `HudRuntimeInstaller`
- `PlayerInstaller`
- `EnemySpawnDirector`(또는 Installer에서 생성)

## 6-3. Inspector 연결 순서
1) `StageRuntimeInstaller`
- StageCatalog SO 연결
- Stage0/1/7/8 Presenter 연결

2) `PlayerInstaller`
- PlayerView(애니메이터/스프라이트/이펙트 루트)
- Input/Move/Combat/Health/Ultimate/Squad Presenter 연결

3) `EnemySpawnDirector`
- EnemyDefinitionSO 목록 연결
- 스폰 영역/풀/등록기 연결

4) `HudRuntimeInstaller`
- HP/Timer/Skill/Result Presenter 연결

## 6-4. 마지막 검증 순서
1) Stage0 시작 ~ 클리어  
2) Stage1 스쿼드 편성 반영  
3) Stage7 보스 경고/스폰/임계치 연출  
4) Stage8 티저 전환  
5) HUD/결과UI/메뉴/UI 툴킷 동기화 확인

