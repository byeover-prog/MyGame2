# 미사용 후보 코드 목록 (근거 + 위험도)

주의: 본 문서는 **정적 분석 1차 결과**입니다.  
규칙: “미사용 단정”이 아닌 **후보 분류**이며, 삭제 전 PlayMode 검증 필수.

분류 단계:
- **삭제 가능**: YAML 참조 없음 + 코드 참조 없음 + 데모 범위 영향 낮음
- **검증 후 삭제**: YAML 참조 없음이나 코드 참조/에디터 메뉴 가능성 있음
- **보류**: 현재 미연결처럼 보이나 핵심 아키텍처 전환 후보

---

## 1. 삭제 가능 (낮은 위험)

| 파일 경로 | 근거 | 위험도 |
|---|---|---|
| `Assets/_Game/Editor/Balsilevelautofill.cs` | YAML 참조 없음 + 코드 참조 흔적 없음 | 낮음 (Editor 전용) |
| `Assets/_Game/Editor/Removemissingscripts.cs` | YAML 참조 없음 + 코드 참조 흔적 없음 | 낮음 (수동 툴 가능성만 확인) |
| `Assets/_Game/Scripts/Combat/Damagechainguard.cs` | YAML 참조 없음 + 코드 참조 없음 | 낮음 |
| `Assets/_Game/Scripts/Combat/Elementattachedvfxhost2d.cs` | YAML 참조 없음 + 코드 참조 없음 | 낮음 |
| `Assets/_Game/Scripts/Core/Combatlog.cs` | YAML 참조 없음 + 코드 참조 없음 | 낮음 |

---

## 2. 검증 후 삭제 (중간 위험)

| 파일 경로 | 근거 | 위험도 |
|---|---|---|
| `Assets/_Game/Scripts/Core/Squad/SquadRuntimeBattleBootstrap2D.cs` | 씬/프리팹 참조 미확인, 코드 텍스트 언급은 존재 | 중간 (향후 연결 가능성) |
| `Assets/_Game/Scripts/UI/ClearUI/ClearBridge.cs` | 씬/프리팹 참조 미확인 | 중간 (클리어 UI 브릿지 후보) |
| `Assets/_Game/Scripts/Stage/Stage0~3/Ultimatepopup2d.cs` | Scene 연결 미확인, Stage0Director 타입 필드만 존재 | 중간 (수동 배선 누락 가능) |
| `Assets/_Game/Scripts/Enemy/Questkillreporter.cs` | YAML 참조 없음 | 중간 (퀘스트 시스템 연동 가능성) |
| `Assets/_Game/Scripts/Enemy/Elitedrophandler.cs` | YAML 참조 없음 | 중간 (엘리트 보상 실험 코드 가능성) |

---

## 3. 보류 (삭제 금지, 구조 전환 후보)

| 파일 경로 | 보류 사유 | 위험도 |
|---|---|---|
| `Assets/_Game/Scripts/Stage/StageDefinitionSO.cs` | 현재 참조 미약하나 Stage 0/1/7/8 데이터 중심 전환 핵심 | 높음 |
| `Assets/_Game/Scripts/Stage/StageCatalogSO.cs` | StageManager 통합 시 필수 카탈로그 축 | 높음 |
| `Assets/_Game/Scripts/Meta/*` 일부 미연결 파일 | Outgame/상점/진척도 확장 준비 자산 | 중간~높음 |
| `Assets/_Game/Scripts/Equipment/*` 일부 | 향후 장비 시스템 회귀 가능성 | 중간 |

---

## 4. 판정 근거 상세

- 총 C# 414개 중, YAML(`.unity/.prefab/.asset`) 직접 GUID 참조가 없는 스크립트가 다수 확인됨.
- 단, Editor 스크립트/툴 메뉴/리플렉션 경로는 YAML에 나타나지 않으므로 오탐 가능.
- 따라서 최종 삭제는 다음 순서로만 허용:
  1) 후보를 별도 브랜치에서 비활성 처리
  2) Stage 0/1/7/8 + HUD/결과UI + 메인메뉴 + 스킬트리 + 상점 UI PlayMode 검증
  3) 빌드 테스트 후 삭제 커밋

---

## 5. 안전 삭제 후보 목록 (요청 항목)

현재 기준 “안전하게 삭제 가능한 후보”는 **1번 섹션의 5개 파일**로 제한 권고.
그 외는 반드시 검증 후 삭제 또는 보류.

