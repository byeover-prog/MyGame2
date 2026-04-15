using System.Collections;
using Unity.Entities;
using UnityEngine;

public class PlayerBridge : MonoBehaviour
{
    // 플레이어 정보를 ECS 데이터로 공유하기 위한 브릿지
    [Header("참조")]
    [Tooltip("플레이어를 할당해 주세요")]
    [SerializeField] private Transform _player;
    private Entity _playerEntity;
    
    void Start()
    {
        _playerEntity = ECSCore.EM.CreateEntity(typeof(PlayerData));
        
        if (_player == null) StartCoroutine(FindPlayer());
    }

    // Update is called once per frame
    void Update()
    {
        if (_player == null) return;
        
        // 데이터 갱신
        ECSCore.EM.SetComponentData(_playerEntity, new PlayerData
            { Position = _player.position });
    }
    
    //탐색 코루틴
    private IEnumerator FindPlayer()
    {
        while (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
            yield return null;
        }
    }
}
