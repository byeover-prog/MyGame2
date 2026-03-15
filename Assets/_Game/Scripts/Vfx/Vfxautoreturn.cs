using System.Collections;
using UnityEngine;

/// <summary>
/// VFX 인스턴스에 자동 부착. 수명 후 풀 반환. Update 없이 코루틴으로 동작.
/// </summary>
public class VFXAutoReturn : MonoBehaviour
{
    [HideInInspector] public GameObject sourcePrefab;
    [HideInInspector] public float maxLifetime = 3f;

    private Coroutine _co;

    private void OnEnable()
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(WaitAndReturn());
    }

    private void OnDisable()
    {
        if (_co != null) { StopCoroutine(_co); _co = null; }
    }

    private IEnumerator WaitAndReturn()
    {
        yield return new WaitForSeconds(maxLifetime);
        _co = null;
        if (sourcePrefab != null) VFXPool.Return(sourcePrefab, gameObject);
        else { gameObject.SetActive(false); Destroy(gameObject, 0.1f); }
    }
}