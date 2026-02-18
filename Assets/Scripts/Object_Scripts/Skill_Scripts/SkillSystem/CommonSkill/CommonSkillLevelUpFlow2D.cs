using UnityEngine;

[DisallowMultipleComponent]
public sealed class CommonSkillLevelUpFlow2D : MonoBehaviour
{
    [SerializeField] private PlayerExp playerExp;
    [SerializeField] private CommonSkillCardPicker2D picker;

    private int pending;

    private void Awake()
    {
        if (playerExp == null) playerExp = FindFirstObjectByType<PlayerExp>();
        if (picker == null) picker = FindFirstObjectByType<CommonSkillCardPicker2D>();
    }

    private void OnEnable()
    {
        if (playerExp != null)
            playerExp.OnLevelUp += OnLevelUp;
    }

    private void OnDisable()
    {
        if (playerExp != null)
            playerExp.OnLevelUp -= OnLevelUp;
    }

    private void OnLevelUp(int newLevel)
    {
        pending++;
    }

    private void Update()
    {
        if (pending <= 0) return;
        if (picker == null) return;
        if (picker.IsOpen) return;

        pending--;
        picker.Open();
    }
}