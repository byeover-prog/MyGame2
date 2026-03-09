// UTF-8
using UnityEngine;

namespace _Game.Scripts.Stage.Spawn
{
    [DisallowMultipleComponent]
    public sealed class SpawnArea2D : MonoBehaviour
    {
        [Header("스폰 영역(필수)")]
        [Tooltip("스폰 가능한 영역을 정의하는 Collider2D 입니다.\n" +
                 "- BoxCollider2D: 직사각형 맵\n" +
                 "- PolygonCollider2D: 울퉁불퉁한 맵(나무 라인 등)")]
        [SerializeField] private Collider2D areaCollider;

        [Header("안전 마진(권장)")]
        [Tooltip("영역 가장자리(나무/벽)에 너무 붙어서 스폰되지 않게 안쪽으로 조금 밀어줍니다(월드 유닛).")]
        [Min(0f)]
        [SerializeField] private float innerMargin = 0.5f;

        [Header("샘플링 시도 횟수")]
        [Tooltip("랜덤 점을 뽑았는데 바깥이면 다시 뽑습니다. 폴리곤이 복잡할수록 횟수를 늘리세요.")]
        [Min(10)]
        [SerializeField] private int maxTries = 80;

        private void Reset()
        {
            areaCollider = GetComponent<Collider2D>();
        }

        private void Awake()
        {
            if (areaCollider == null)
                areaCollider = GetComponent<Collider2D>();

            if (areaCollider == null)
                Debug.LogError("[SpawnArea2D] Collider2D가 없습니다. SpawnArea 오브젝트에 Box/PolygonCollider2D를 추가하세요.", this);
        }

        /// <summary>
        /// [구현 원리 요약]
        /// - 콜라이더 bounds 안에서 랜덤 점을 찍는다.
        /// - areaCollider.OverlapPoint로 '영역 내부'인지 검사한다.
        /// - 내부면 반환, 아니면 재시도.
        /// </summary>
        public Vector2 GetRandomPointInside()
        {
            if (areaCollider == null)
                return transform.position;

            var b = areaCollider.bounds;

            for (int i = 0; i < maxTries; i++)
            {
                // bounds 안에서 랜덤 점
                float x = Random.Range(b.min.x, b.max.x);
                float y = Random.Range(b.min.y, b.max.y);
                Vector2 p = new Vector2(x, y);

                // 콜라이더 내부인지 검사
                if (!areaCollider.OverlapPoint(p))
                    continue;

                // 가장자리 마진: 중심 방향으로 조금 밀어준다(너무 가장자리 스폰 방지)
                if (innerMargin > 0f)
                {
                    Vector2 center = (Vector2)b.center;
                    Vector2 dirToCenter = (center - p).normalized;
                    p += dirToCenter * innerMargin;

                    // 마진 적용 후에도 내부인지 확인
                    if (!areaCollider.OverlapPoint(p))
                        continue;
                }

                return p;
            }

            // 실패 시: 그냥 중심 반환(디버그로 쉽게 티나게)
            Debug.LogWarning("[SpawnArea2D] 내부 랜덤 포인트 추출 실패. 콜라이더가 너무 얇거나 마진/시도 횟수를 확인하세요.", this);
            return areaCollider.bounds.center;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (areaCollider == null) return;
            Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
            Gizmos.DrawCube(areaCollider.bounds.center, areaCollider.bounds.size);
        }
#endif
    }
}