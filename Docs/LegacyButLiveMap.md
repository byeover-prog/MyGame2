# Legacy but Live 맵

목적: 구조적으로 구식이지만 현재 런타임에서 실제 동작 중인 코드 경로를 분리.

---

## 1. Stage/게임 진행 축

| 대상 | 현재 구조 | 왜 Legacy인지 | Live 근거 | 위험 |
|---|---|---|---|---|
| `Assets/_Game/Scripts/Stage/Stage0~3/Stage0director.cs` | Stage0 전용 흐름(엘리트/러시/궁극기 클리어) | Stage별 하드코딩 디렉터 | `Scene_Game.unity`에 직접 부착 | StageManager와 경로 충돌 |
| `Assets/_Game/Scripts/Stage/StageManager.cs` | 공용 스테이지 매니저 | SO 카탈로그 연결 전제이나 fallback 유지 | `Scene_Game/Scene_HJO/Scene_JGM/Scene_UI` 참조 | fallback 경로 장기화 |
| `Assets/_Game/Scripts/Core/RunSignals.cs` | 정적 시그널 허브 | 글로벌 정적 이벤트 패턴 | 궁극기 발행/스테이지 구독 경로 존재 | 구독 누수/추적 난이도 |

---

## 2. Enemy 스폰 축

| 대상 | 현재 구조 | 왜 Legacy인지 | Live 근거 | 위험 |
|---|---|---|---|---|
| `Assets/_Game/Scripts/Enemy/EnemySpawner2D.cs` | 레거시 + 타임라인 혼합 스포너 | 단일 책임 위반 | 주요 인게임 씬에 다수 배치 | 설정에 따라 모드 혼선 |
| `Assets/_Game/Scripts/Enemy/EnemySpawnerTimeline2D.cs` | 타임라인 전용 스포너 | 기존 스포너와 중복 | 주요 인게임 씬에 동시 존재 | 이원화 운영 비용 |
| `Assets/_Game/Scripts/Enemy/EnemyDespawnManager.cs` | 중앙 거리 정리 매니저 | 태그 탐색 기반 | Scene_Game 등에서 사용 | 플레이어 참조 취약 |

---

## 3. Player 전투/성장 축

| 대상 | 현재 구조 | 왜 Legacy인지 | Live 근거 | 위험 |
|---|---|---|---|---|
| `Assets/_Game/Scripts/Skill/SkillRunner.cs` | 스킬 프리팹 장착+레벨 적용 | 기능 누적형 매니저 | 인게임 씬/코드 호출 경로 존재 | 다른 스킬 매니저와 책임 중복 |
| `Assets/_Game/Scripts/Skill/CommonSkill/CommonSkillManager2D.cs` | 공통 스킬 획득/업글 | 기존 스킬 흐름과 이중 축 | HUD/카드 시스템 연계 | 상태 동기화 분산 |
| `Assets/_Game/Scripts/Player/Charater/Characterpassivemanager2d.cs` | 메인 캐릭터 패시브 스위처 | 런타임 AddComponent 방식 | Player.prefab 부착 | 타입 확장 시 취약 |
| `Assets/_Game/Scripts/LevelUp/Levelupflowcoordinator.cs` | 레벨업 큐/시간정지 조정 | 집중형 조정기 | 레벨업 진입 중심 | 참조 누락 시 큐 정지 |

---

## 4. UI/HUD 축

| 대상 | 현재 구조 | 왜 Legacy인지 | Live 근거 | 위험 |
|---|---|---|---|---|
| `Assets/_Game/Scripts/Core/Hudconnector.cs` | HP/타이머/스킬/패시브 통합 브릿지 | Presenter 분리 전의 집중형 허브 | 인게임 씬에 부착 | 스코프 비대화 |
| `Assets/_Game/Scripts/UI/InGameHudUI.cs` | 슬롯 초기화/플레이스홀더 | 실제 데이터 바인딩 분리 미흡 | HUD 루트에서 사용 | 임시 로직 상시화 |

---

## 5. 우선 대응 순서 (데모 우선)
1) Stage 흐름 단일화 (`StageManager2D` ↔ `Stage0Director`)  
2) Enemy 스포너 단일화 (`EnemySpawner2D`/`EnemySpawnerTimeline2D`)  
3) Player 패시브/궁극기 런타임 조립 축소  
4) HUD 브릿지 분해 (기능별 Presenter)  
5) 공용 신호 허브(`RunSignals`) 경계 축소

