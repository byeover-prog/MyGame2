using UnityEngine;

/// <summary>
/// 스킬 비주얼 공통 인터페이스입니다.
/// </summary>
public interface ISkillVisual
{
    /// <summary>비주얼 재생 시작입니다.</summary>
    void Play(Vector3 pos);

    /// <summary>지속형 비주얼 위치 갱신입니다.</summary>
    void UpdatePosition(Vector3 pos);

    /// <summary>비주얼 크기 갱신입니다.</summary>
    void UpdateScale(float scaleMul);

    /// <summary>일회성 임팩트 연출입니다.</summary>
    void PlayImpact(Vector3 pos);

    /// <summary>비주얼 정지입니다.</summary>
    void Stop();
}