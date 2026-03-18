// UTF-8
// Assets/_Game/Scripts/Ultimate/Hayul/TalismanFall.cs
// 부적 낙하 연출용. Presenter가 풀에서 꺼낼 때 Init 호출.
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TalismanFall : MonoBehaviour
{
    private float _speed;
    private float _lifetime;
    private float _age;

    public void Init(float speed, float lifetime)
    {
        _speed = speed;
        _lifetime = lifetime;
        _age = 0f;
    }

    private void Update()
    {
        _age += Time.deltaTime;
        if (_age >= _lifetime)
        {
            gameObject.SetActive(false);
            return;
        }

        // 아래로 낙하 + 약간의 좌우 흔들림
        float sway = Mathf.Sin(_age * 5f) * 0.5f;
        transform.position += new Vector3(sway * Time.deltaTime, -_speed * Time.deltaTime, 0f);

        // 페이드 효과 (마지막 30%에서 투명해짐)
        float fadeStart = _lifetime * 0.7f;
        if (_age > fadeStart)
        {
            float alpha = 1f - ((_age - fadeStart) / (_lifetime - fadeStart));
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }
}