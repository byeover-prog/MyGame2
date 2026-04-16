# Enemy Prefab 감사 보고서

대상 폴더: `Assets/_Game/Prefabs/Enemies/`

---

## 1. 프리팹 수집 결과

- 확인된 적 프리팹: 9종
  - `ArmyGhost`, `BachelorGhost`, `Boar`, `Boar_Elite`, `JuniorDokebi`, `Raven`, `StoneGhost`, `VirginGhost`, `Wolf`

### 1-1. 공통 부착 스택
- 전 프리팹 공통(4종):
  - `EnemyChaser2D`
  - `EnemyContactDamage2D`
  - `EnemyHealth2D`
  - `EnemyRegistryMember2D`

### 1-2. 프리팹별 차이
- `EnemyAutoDespawn2D` 유무 차이:
  - 있음: ArmyGhost/BachelorGhost/JuniorDokebi/Raven/StoneGhost/VirginGhost
  - 없음: Boar/Boar_Elite/Wolf

---

## 2. 스폰/등록/디스폰/피격/보상 흐름

```text
EnemySpawner2D 또는 EnemySpawnerTimeline2D
  -> EnemyPool2D.Get(prefab)
    -> EnemyRegistryMember2D.OnEnable(Register)
    -> EnemyAutoDespawn2D.OnEnable(Manager.Register) [일부 프리팹]
    -> EnemyChaser2D.FixedUpdate(타겟 추적)
    -> EnemyContactDamage2D.OnTriggerStay2D(플레이어 접촉 피해)
    -> EnemyHealth2D.TakeDamage/Die
       -> KillCountSource 증가
       -> ExpOrbPool에서 보상 오브 생성
       -> EnemyPoolTag.ReturnToPool
```

---

## 3. 공통 스택과 중복 구조의 문제

## 문제 1. 프리팹 조합 중복

### 현재 구조
- 거의 동일한 컴포넌트 구성을 9개 프리팹에 반복.
- 차이는 주로 값(체력/공격력/이속)인데, 구조 자체를 프리팹 조합으로 표현.

### 왜 문제인지
- 스펙 변경 시 9개 프리팹 동시 점검 필요.
- 누락/불일치(예: AutoDespawn 부착 여부)가 기능 편차를 만듦.

### 추천 구조
- `EnemyDefinitionSO` 중심으로 데이터화.
- Presenter/View는 공통 프리팹 1~2종으로 표준화.

### 마이그레이션 위험
- 기존 프리팹별 애니메이션/콜라이더 셋업이 달라 일괄 치환 시 시각 오류 가능.

---

## 문제 2. 스포너 이원화

### 현재 구조
- `EnemySpawner2D`(레거시+타임라인 혼합)와 `EnemySpawnerTimeline2D`(타임라인 전용) 병존.

### 왜 문제인지
- 씬별 설정 차이로 실제 스폰 규칙이 달라질 수 있음.

### 추천 구조
- `EnemySpawnDirector` 단일 진입.
- 레거시 모드는 어댑터로만 유지 후 폐기.

### 마이그레이션 위험
- 기존 Trial/Debug 스폰 스크립트와 계약 충돌 가능.

---

## 문제 3. 런타임 탐색/정적 캐시 결합

### 현재 구조
- `EnemyChaser2D`가 플레이어를 태그 탐색 + static 캐시.
- `EnemyDespawnManager`는 태그 기반 플레이어 참조.

### 왜 문제인지
- 씬 전환/재시작에서 stale 캐시 가능성.

### 추천 구조
- Installer에서 `IPlayerTargetProvider`를 명시 주입.

### 마이그레이션 위험
- 초기엔 참조 누락 시 적 정지 현상 발생 가능.

---

## 4. 왜 과중복 구조가 되었는지 (구조적 설명)

1) 적 종류 증가를 “새 프리팹 복제”로 해결  
2) 공통 규칙을 데이터가 아닌 컴포넌트 조합으로 표현  
3) 스포너가 단일 표준으로 수렴되지 못함  
4) 결과적으로 “구조 중복 + 데이터 분산”이 동시 발생

---

## 5. ASCII 다이어그램

### 5-1. 현재 구조

```text
Enemy Prefab A..I
├─ EnemyChaser2D
├─ EnemyContactDamage2D
├─ EnemyHealth2D
├─ EnemyRegistryMember2D
└─ (optional) EnemyAutoDespawn2D

Spawner side:
EnemySpawner2D (legacy/timeline mixed)
EnemySpawnerTimeline2D (timeline only)
```

### 5-2. 목표 구조(MVP)

```text
EnemySpawnDirector
  -> EnemyFactory
     -> EnemyView(prefab)
     -> EnemyPresenter
     -> EnemyRuntimeModel
     <- EnemyDefinitionSO (readonly config)
```

---

## 6. SO vs 런타임 Model 분리 원칙 (Enemy)

### SO에 넣을 것
- 고정 데이터: 기본 HP, 이동속도, 접촉공격, 등급, 드랍 테이블, VFX/애니메이션 참조
- 스폰 가중치/웨이브 정의

### 런타임 Model에 둘 것
- 현재 HP, 상태이상, 타겟, 현재 AI 상태, 쿨다운, 누적 피해량
- 풀링 활성 상태/생성 시각 등 세션 상태

---

## 7. 1차 우선 정리 범위 (Stage 0/1/7/8 영향 기준)

1) `EnemySpawner2D`와 `EnemySpawnerTimeline2D` 주도권 단일화  
2) `EnemyAutoDespawn2D` 누락 프리팹(Boar/Boar_Elite/Wolf) 정책 통일  
3) 보상/사망 처리(`EnemyHealth2D`)를 공통 이벤트로 노출  
4) Stage 7 보스(두억시니) 전용은 일반 적 파이프라인과 분리된 보스 디렉터로 관리

