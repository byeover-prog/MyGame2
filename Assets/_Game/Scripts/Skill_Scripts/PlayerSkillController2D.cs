// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - UI 버튼(카드 선택)에서 호출할 "공개 메서드"를 모아둔다.
// - 스킬은 시작에 장착되지 않고, 카드 선택 시 Grant/Upgrade로 켠다.
[DisallowMultipleComponent]
public sealed class PlayerSkillController2D : MonoBehaviour
{
    [Header("스킬 참조(플레이어에 붙어있는 컴포넌트들을 연결)")]
    [SerializeField] private ShurikenSkill2D shuriken;
    [SerializeField] private HomingMissileSkill2D homing;
    [SerializeField] private OrbitingSwordSkill2D orbitSword;

    // ─────────────────────────────────────
    // UI(Button OnClick)에서 여기 메서드들을 연결해서 사용
    // ─────────────────────────────────────

    public void Card_ShuriKen_GrantOrUpgrade()
    {
        GrantOrUpgrade(shuriken);
    }

    public void Card_Homing_GrantOrUpgrade()
    {
        GrantOrUpgrade(homing);
    }

    public void Card_OrbitSword_GrantOrUpgrade()
    {
        GrantOrUpgrade(orbitSword);
    }

    // 공통 처리
    private static void GrantOrUpgrade(ISkill2D skill)
    {
        if (skill == null) return;

        if (!skill.IsOwned)
            skill.Grant(1, true);
        else
            skill.Upgrade(1);
    }
}