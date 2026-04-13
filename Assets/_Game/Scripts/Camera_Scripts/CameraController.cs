using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("카메라 목록")]
    [SerializeField] private CinemachineCamera _playerCamera;
    
    // 타겟 목록
    private Transform _player;

    void OnEnable()
    {
        StartCoroutine(LookAtPlayer());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }
    
    // 버츄얼 카메라의 타겟을 설정하는 함수
    private void SetFollowTarget(CinemachineCamera vcam, Transform target)
    {
        vcam.Follow = target;
        vcam.ForceCameraPosition(target.position, target.rotation);
    }
    
    // 플레이어 탐색 코루틴
    private IEnumerator LookAtPlayer()
    {
        while (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
            yield return null;
        }
        SetFollowTarget(_playerCamera, _player);
    }
}
