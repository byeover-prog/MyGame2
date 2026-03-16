// UTF-8
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// - 암흑구 폭발이 한 프레임에 몰릴 때 VFX를 전부 다 재생하면 프레임이 멈춘다.
/// - 그래서 "이번 프레임에 폭발 VFX 몇 개까지 허용할지" 예산을 둔다.
/// - 예산을 넘으면 데미지는 정상 처리하고, VFX만 생략한다.
/// </summary>
public static class SkillVFXBudget2D
{
    private static int _frame = -1;
    private static int _darkOrbExplosionCount;

    public static int DarkOrbExplosionPerFrameLimit = 2;

    public static bool TryConsumeDarkOrbExplosion()
    {
        int currentFrame = Time.frameCount;
        if (_frame != currentFrame)
        {
            _frame = currentFrame;
            _darkOrbExplosionCount = 0;
        }

        if (_darkOrbExplosionCount >= Mathf.Max(0, DarkOrbExplosionPerFrameLimit))
            return false;

        _darkOrbExplosionCount++;
        return true;
    }
}