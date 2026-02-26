// UTF-8
using UnityEngine;
using UnityEngine.Rendering;

// [구현 원리 요약]
// - 오브젝트(및 자식들)의 Renderer 정렬을 강제로 맞춘다.
// - 몬스터보다 스킬이 위에 보이게 Sorting Layer/Order를 일괄 적용한다.
[DisallowMultipleComponent]
public sealed class AutoSorting2D : MonoBehaviour
{
    [Header("정렬(스킬이 몬스터보다 위로)")]
    [Tooltip("Sorting Layer 이름 (예: SkillFX). Project Settings > Tags and Layers에서 만들어야 합니다.")]
    [SerializeField] private string sortingLayerName = "SkillFX";

    [Tooltip("Order in Layer 값. 숫자가 클수록 위에 보입니다.")]
    [SerializeField] private int orderInLayer = 50;

    [Tooltip("자식 오브젝트까지 같이 적용")]
    [SerializeField] private bool includeChildren = true;

    private void OnEnable()
    {
        ApplyNow();
    }

    [ContextMenu("Apply Now")]
    public void ApplyNow()
    {
        // SortingGroup이 있으면 같이 맞춰준다(여러 스프라이트 묶음 정렬용)
        var groups = includeChildren
            ? GetComponentsInChildren<SortingGroup>(true)
            : GetComponents<SortingGroup>();

        for (int i = 0; i < groups.Length; i++)
        {
            var g = groups[i];
            if (g == null) continue;
            g.sortingLayerName = sortingLayerName;
            g.sortingOrder = orderInLayer;
        }

        // 모든 Renderer(스프라이트/파티클/트레일 등) 정렬 통일
        var renderers = includeChildren
            ? GetComponentsInChildren<Renderer>(true)
            : GetComponents<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            r.sortingLayerName = sortingLayerName;
            r.sortingOrder = orderInLayer;
        }
    }
}