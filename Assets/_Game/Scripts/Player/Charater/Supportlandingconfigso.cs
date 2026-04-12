using UnityEngine;

/// <summary>
/// 지원 캐릭터 등장/퇴장 연출 설정입니다.
/// 캐릭터 성격에 따라 낙하 속도, 착지 강도, VFX를 달리합니다.
///
/// [생성 방법]
/// Project 창 > 우클릭 > Create > 혼령검/연출/지원 착지 설정
/// 윤설/하율/하린 각각 1개씩 만들어 CharacterDefinitionSO에 연결합니다.
///
/// [Inspector 예시]
/// 윤설 — 가볍고 빠른 낙하: dropHeight=6, dropDuration=0.22, impactPause=0.03
/// 하린 — 무게감 있는 낙하: dropHeight=8, dropDuration=0.28, impactPause=0.06
/// 하율 — 부적 소환형: dropHeight=5, dropDuration=0.25, impactPause=0.04
/// </summary>
[CreateAssetMenu(menuName = "혼령검/연출/지원 착지 설정", fileName = "SupportLandingConfig_")]
public sealed class SupportLandingConfigSO : ScriptableObject
{
    [Header("호출 예고 (A단계)")]
    [Tooltip("착지 위치에 그림자가 미리 표시되는 시간(초)입니다.")]
    [Min(0f)] public float presignDuration = 0.08f;

    [Tooltip("착지 예고 그림자 프리팹입니다. 없으면 기본 원형 그림자를 사용합니다.")]
    public GameObject shadowPrefab;

    [Header("낙하 (B단계)")]
    [Tooltip("캐릭터가 최종 위치 위 몇 유닛에서 시작하는지입니다.")]
    [Min(1f)] public float dropHeight = 7f;

    [Tooltip("낙하에 걸리는 시간(초)입니다.")]
    [Min(0.05f)] public float dropDuration = 0.25f;

    [Tooltip("낙하 커브입니다. 초반 느리고 후반 가속하면 중력감이 납니다.")]
    public AnimationCurve dropCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(0.4f, 0.05f, 0.2f, 0.2f),
        new Keyframe(1f, 1f, 3f, 3f)
    );

    [Header("착지 임팩트 (C단계)")]
    [Tooltip("착지 순간 히트스톱 시간(초)입니다. 무게감을 줍니다.")]
    [Min(0f)] public float impactPauseDuration = 0.04f;

    [Tooltip("착지 VFX 프리팹입니다. (먼지, 충격 링 등) 없으면 생략합니다.")]
    public GameObject landVfxPrefab;

    [Header("Squash & Stretch")]
    [Tooltip("낙하 중 Stretch 스케일입니다. (가로, 세로)")]
    public Vector2 airStretchScale = new Vector2(0.96f, 1.08f);

    [Tooltip("착지 순간 Squash 스케일입니다.")]
    public Vector2 landSquashScale = new Vector2(1.10f, 0.88f);

    [Tooltip("Squash에서 원래 크기로 복귀하는 시간(초)입니다.")]
    [Min(0.01f)] public float squashRecoverDuration = 0.08f;

    [Header("안정화 (D단계)")]
    [Tooltip("착지 후 궁극기 시전 전까지의 여유 시간(초)입니다.")]
    [Min(0f)] public float settleDuration = 0.10f;

    [Header("퇴장")]
    [Tooltip("퇴장 시 상승할 높이입니다.")]
    [Min(1f)] public float exitRiseHeight = 5f;

    [Tooltip("퇴장에 걸리는 시간(초)입니다.")]
    [Min(0.05f)] public float exitDuration = 0.35f;

    [Tooltip("퇴장 커브입니다. 초반 빠르고 후반 느려지면 점프감이 납니다.")]
    public AnimationCurve exitCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 3f, 3f),
        new Keyframe(0.6f, 0.85f, 1f, 1f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    [Tooltip("퇴장 중 페이드아웃 여부입니다.")]
    public bool fadeOutOnExit = true;

    [Tooltip("퇴장 VFX 프리팹입니다. (잔상, 연기 등) 없으면 생략합니다.")]
    public GameObject exitVfxPrefab;

    // ─── 기본값 ────────────────────────────────────────

    /// <summary>Inspector에 SO가 없을 때 사용할 기본 설정입니다.</summary>
    private static SupportLandingConfigSO _runtimeDefault;

    public static SupportLandingConfigSO GetDefault()
    {
        if (_runtimeDefault == null)
        {
            _runtimeDefault = CreateInstance<SupportLandingConfigSO>();
            _runtimeDefault.hideFlags = HideFlags.DontSave;
            _runtimeDefault.name = "SupportLandingConfig_Default";
        }
        return _runtimeDefault;
    }
}