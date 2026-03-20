// UTF-8
using UnityEngine;
using System.Collections;

/// <summary>
/// [구현 원리 요약]
/// 일정 주기마다 DarkZone 프리팹을 생성해서
/// 보스 주변 어둠 영역을 전개합니다.
/// </summary>
[DisallowMultipleComponent]
public class GrimReaperDarkZoneSkill : MonoBehaviour
{
    [Header("참조")]

    [Tooltip("DarkZone 프리팹")]
    [SerializeField] private GameObject darkZonePrefab;



    [Header("스킬 설정")]

    [Tooltip("영역 지속시간(초)")]
    [SerializeField] private float activeDuration = 5f;

    [Tooltip("재사용 대기시간(초)")]
    [SerializeField] private float cooldown = 5f;



    private void Start()
    {
        StartCoroutine(SkillRoutine());
    }



    private IEnumerator SkillRoutine()
    {
        while (true)
        {
            // 영역 생성
            GameObject zoneObject = Instantiate(darkZonePrefab, transform.position, Quaternion.identity);

            GrimReaperDarkZone zone = zoneObject.GetComponent<GrimReaperDarkZone>();
            if (zone != null)
            {
                zone.Init(transform);
                zone.ActivateZone();
            }

            // 지속시간 + 쿨타임
            yield return new WaitForSeconds(activeDuration + cooldown);
        }
    }
}