// UTF-8
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SkillSlotVisibilityToggle2D : MonoBehaviour
{
    [SerializeField, InspectorName("슬롯 루트")]
    [Tooltip("여기에 '스킬 슬롯 8개'가 들어있는 패널 오브젝트를 넣으세요.")]
    private GameObject skillSlotRoot;

    [SerializeField, InspectorName("토글 키")]
    private KeyCode toggleKey = KeyCode.I;

    private void Start()
    {
        if (skillSlotRoot == null) return;

        bool visible = true;
        if (GameSettingsRuntime.HasInstance)
            visible = GameSettingsRuntime.Instance.SkillSlotsVisible;

        skillSlotRoot.SetActive(visible);

        if (GameSettingsRuntime.HasInstance)
            GameSettingsRuntime.Instance.OnSkillSlotsVisibleChanged += OnVisibleChanged;
    }

    private void OnDestroy()
    {
        if (GameSettingsRuntime.HasInstance)
            GameSettingsRuntime.Instance.OnSkillSlotsVisibleChanged -= OnVisibleChanged;
    }

    private void Update()
    {
        if (skillSlotRoot == null) return;

        if (Input.GetKeyDown(toggleKey))
        {
            bool next = !skillSlotRoot.activeSelf;
            if (GameSettingsRuntime.HasInstance)
                GameSettingsRuntime.Instance.SkillSlotsVisible = next;
            else
                skillSlotRoot.SetActive(next);
        }
    }

    private void OnVisibleChanged(bool visible)
    {
        if (skillSlotRoot == null) return;
        skillSlotRoot.SetActive(visible);
    }
}