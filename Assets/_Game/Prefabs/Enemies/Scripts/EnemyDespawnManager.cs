using System.Collections.Generic;
using UnityEngine;

// 적 자동 정리 매니저 — 개별 Update 완전 제거.

[DisallowMultipleComponent]
public sealed class EnemyDespawnManager : MonoBehaviour
{
    [Header("정리 거리")]
    [Min(1f)]
    [SerializeField] private float despawnDistance = 25f;

    [Header("성능")]
    [Tooltip("프레임당 거리 체크할 적 수. 적 300마리 / 50 = 6프레임에 1바퀴.")]
    [Min(1)]
    [SerializeField] private int enemiesPerFrame = 50;

    // 런타임
    private Transform _player;
    private float _despawnDistSqr;

    // 등록된 적 (고정 크기 배열 + 카운트 — List보다 GC 0)
    private Transform[] _enemies = new Transform[512];
    private GameObject[] _roots = new GameObject[512];
    private int _count;
    private int _cursor; // 현재 체크 위치

    public static EnemyDespawnManager Instance { get; private set; }

    private void Awake()
    {
        _despawnDistSqr = despawnDistance * despawnDistance;
    }

    private void OnEnable()
    {
        Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }
    
    //  등록/해제 API — 적 OnEnable/OnDisable에서 호출

    public void Register(Transform enemyTransform, GameObject rootObject)
    {
        if (enemyTransform == null) return;

        // 배열 확장 (드물게 발생)
        if (_count >= _enemies.Length)
        {
            int newSize = _enemies.Length * 2;
            var newArr = new Transform[newSize];
            var newRoots = new GameObject[newSize];
            System.Array.Copy(_enemies, newArr, _count);
            System.Array.Copy(_roots, newRoots, _count);
            _enemies = newArr;
            _roots = newRoots;
        }

        _enemies[_count] = enemyTransform;
        _roots[_count] = rootObject;
        _count++;
    }

    public void Unregister(Transform enemyTransform)
    {
        if (enemyTransform == null) return;

        for (int i = 0; i < _count; i++)
        {
            if (_enemies[i] == enemyTransform)
            {
                // SwapBack O(1) 제거
                int last = _count - 1;
                if (i < last)
                {
                    _enemies[i] = _enemies[last];
                    _roots[i] = _roots[last];
                }
                _enemies[last] = null;
                _roots[last] = null;
                _count--;

                // 커서가 제거된 위치 이후면 조정
                if (_cursor > i) _cursor--;
                if (_cursor >= _count) _cursor = 0;
                return;
            }
        }
    }
    
    //  Update — 프레임당 enemiesPerFrame개만 체크

    private void Update()
    {
        if (_player == null || _count == 0) return;

        Vector2 playerPos = _player.position;
        int checks = Mathf.Min(enemiesPerFrame, _count);

        for (int c = 0; c < checks; c++)
        {
            if (_cursor >= _count) _cursor = 0;

            Transform t = _enemies[_cursor];

            // 무효 참조 정리
            if (t == null || _roots[_cursor] == null)
            {
                RemoveAtCursor();
                continue;
            }

            float sqr = ((Vector2)t.position - playerPos).sqrMagnitude;

            if (sqr >= _despawnDistSqr)
            {
                // 정리 거리 초과 → 풀 반환
                GameObject root = _roots[_cursor];
                RemoveAtCursor();
                EnemyPoolTag.ReturnToPool(root);
                continue;
            }

            _cursor++;
        }
    }

    private void RemoveAtCursor()
    {
        int last = _count - 1;
        if (_cursor < last)
        {
            _enemies[_cursor] = _enemies[last];
            _roots[_cursor] = _roots[last];
        }
        _enemies[last] = null;
        _roots[last] = null;
        _count--;
        // 커서 안 올림 — 방금 swap된 새 항목을 다음에 체크
    }
}
