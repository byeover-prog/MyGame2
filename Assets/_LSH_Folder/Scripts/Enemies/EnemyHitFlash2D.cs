using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class EnemyHitFlash2D : MonoBehaviour
{
    [Header("참조 설정")]
    [Tooltip("적의 체력을 관리하는 스크립트입니다. 비워두면 자동으로 찾아서 연결합니다.")]
    [SerializeField] private EnemyHealth2D health;
    
    [Tooltip("색상을 변경할 스프라이트 렌더러입니다. 비워두면 자동으로 찾아서 연결합니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("피격 반짝임 설정")]
    [Tooltip("피격 시 변경될 색상입니다. 기본값은 눈에 잘 띄는 살짝 밝은 붉은색입니다.")]
    [SerializeField] private Color flashColor = new Color(1f, 0.6f, 0.6f, 1f);
    
    [Tooltip("색상이 유지되는 시간(초)입니다. 짧을수록 타격감이 빠릿해집니다.")]
    [SerializeField][Min(0.01f)] private float flashDuration = 0.08f;
    
    [Header("디버그")]
    [Tooltip("체크하면 피격 시 콘솔창에 체력 변화 로그를 띄웁니다. 테스트용으로 씁니다.")]
    [SerializeField] private bool debugLog = false;
    
    private Color _originalColor;
    private int _previousHp;
    private Coroutine _flashRoutine;

    private void Reset()
    {
        health = GetComponent<EnemyHealth2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (health == null) health = GetComponent<EnemyHealth2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (health == null || spriteRenderer == null)
        {
            Debug.LogWarning(
                "[EnemyHitFlash2D] EnemyHealth2D 또는 SpriteRenderer를 찾지 못해 비활성화됩니다.",
                this);
            enabled = false;
            return;
        }
        
        _originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        if (!enabled) return;
        
        _previousHp = health.CurrentHp;
        RestoreColor();
    }
    
    private void OnDisable()
    {
        StopFlashRoutine();
        RestoreColor();
    }

    private void LateUpdate()
    {
        int currentHp = health.CurrentHp;

        if (currentHp < _previousHp)
        {
            PlayFlash();

#if UNITY_EDITOR
            if (debugLog)
            {
                Debug.Log(
                    $"[EnemyHitFlash2D] 피격 감지: {_previousHp} -> {currentHp}",
                    this);
            }
#endif
        }
        
        _previousHp = currentHp;
    }

    private void PlayFlash()
    {
        if (!isActiveAndEnabled)
            return;

        StopFlashRoutine();
        _flashRoutine = StartCoroutine(FlashRoutine());
    }
    
    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        RestoreColor();
        _flashRoutine = null;
    }
    
    private void StopFlashRoutine()
    {
        if (_flashRoutine == null)
            return;

        StopCoroutine(_flashRoutine);
        _flashRoutine = null;
    }
    
    private void RestoreColor()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = _originalColor;
    }
}