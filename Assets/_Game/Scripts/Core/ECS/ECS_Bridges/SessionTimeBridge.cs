using _Game.Scripts.Core.Session;
using System.Collections;
using Unity.Entities;
using UnityEngine;

public class SessionTimeBridge : MonoBehaviour
{
    // 게임 시간을 ECS 데이터로 공유하기 위한 브릿지
    [Header("참조")]
    [Tooltip("게임 시간을 관리하는 SessionGameManager2D를 할당해주세요")]
    [SerializeField] private SessionGameManager2D _sessionManager;
    
    private Entity _timeEntity;
    void Start()
    {
        // 엔티티 생성
        _timeEntity = ECSCore.EM.CreateEntity(typeof(SessionTimeData));
        
        // 세션 매니져가 할당 되지 않았을 때 보험처리
        if (_sessionManager == null) StartCoroutine(FindSessionManager());
    }

    void Update()
    {
        if(_sessionManager == null) return;

        // 데이터 갱신
        ECSCore.EM.SetComponentData(_timeEntity, new SessionTimeData
            { Time = _sessionManager.SessionTime });
    }

    void OnDestroy()
    {
        // 브릿지가 터져도 월드가 잔존할 경우 엔티티 파괴
        if (World.DefaultGameObjectInjectionWorld != null)
        {
            ECSCore.EM.DestroyEntity(_timeEntity);
        }
    }
    
    // 탐색 코루틴
    private IEnumerator FindSessionManager()
    {
        while (_sessionManager == null)
        {
            _sessionManager = FindAnyObjectByType<SessionGameManager2D>();
            yield return null;
        }
    }
}
