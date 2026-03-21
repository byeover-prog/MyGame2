// UTF-8
using UnityEngine;
using System.Collections;

/// <summary>
/// [구현 원리 요약]
/// 저승사자 어둠 장판 스킬
/// - 시작 지연
/// - 지속 시간
/// - 재사용 대기시간
/// 모두 인스펙터에서 설정 가능
/// </summary>
[DisallowMultipleComponent]
public class GrimReaperDarkZoneSkill : MonoBehaviour
{
    [Header("참조")]

    [Tooltip("어둠 장판 프리팹")]
    [SerializeField] private GameObject darkZonePrefab;



    [Header("패턴 타이밍")]

    [Tooltip("게임 시작 후 첫 발동까지 대기시간")]
    [SerializeField] private float startDelay = 3f;

    [Tooltip("패턴 유지 시간")]
    [SerializeField] private float duration = 5f;

    [Tooltip("패턴 재사용 대기시간")]
    [SerializeField] private float cooldown = 5f;



    private Coroutine skillCoroutine;



    private void Start()
    {
        skillCoroutine = StartCoroutine(SkillRoutine());
    }



    private IEnumerator SkillRoutine()
    {
        // 🔥 1. 시작 대기
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            // 🔥 2. 장판 생성
            GameObject zoneObj = Instantiate(darkZonePrefab, transform.position, Quaternion.identity);

            GrimReaperDarkZone zone = zoneObj.GetComponent<GrimReaperDarkZone>();

            if (zone != null)
            {
                zone.Init(transform);
                zone.ActivateZone(duration); // duration 전달
            }

            // 🔥 3. 유지 시간
            yield return new WaitForSeconds(duration);

            // 🔥 4. 쿨타임
            yield return new WaitForSeconds(cooldown);
        }
    }
}