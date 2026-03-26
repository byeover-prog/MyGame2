using UnityEngine;

/// <summary>
/// 캐릭터 고유 패시브의 공통 인터페이스입니다.
/// 메인 캐릭터 전용이며, 지원 캐릭터는 궁극기 사용 중에만 일시 적용됩니다.
/// </summary>
public abstract class CharacterPassiveBase : MonoBehaviour
{
    /// <summary>이 패시브의 한국어 이름입니다.</summary>
    public abstract string PassiveName { get; }

    /// <summary>이 패시브의 설명 텍스트입니다.</summary>
    public abstract string Description { get; }

    /// <summary>패시브가 현재 활성 상태인지 여부입니다.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// 패시브를 활성화합니다. 이벤트 구독, 초기 설정 등을 수행합니다.
    /// </summary>
    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        OnActivate();
        GameLogger.Log($"[캐릭터 패시브] '{PassiveName}' 활성화");
    }

    /// <summary>
    /// 패시브를 비활성화합니다. 이벤트 구독 해제, 상태 초기화 등을 수행합니다.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        OnDeactivate();
        GameLogger.Log($"[캐릭터 패시브] '{PassiveName}' 비활성화");
    }

    /// <summary>하위 클래스에서 활성화 로직을 구현합니다.</summary>
    protected abstract void OnActivate();

    /// <summary>하위 클래스에서 비활성화 로직을 구현합니다.</summary>
    protected abstract void OnDeactivate();

    private void OnDestroy()
    {
        // 안전장치: 오브젝트 파괴 시 반드시 구독 해제
        if (IsActive) Deactivate();
    }
}