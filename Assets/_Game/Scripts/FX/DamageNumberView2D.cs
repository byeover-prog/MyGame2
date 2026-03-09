// UTF-8
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamageNumberView2D : MonoBehaviour
{
    [Header("표시")]
    [SerializeField] private TMP_Text text;

    [Header("연출")]
    [SerializeField] private float floatSpeed = 1.2f;
    [SerializeField] private float lifeSeconds = 0.6f;
    [SerializeField] private float randomX = 0.25f;

    private float _t;
    private Vector3 _vel;

    private void Awake()
    {
        if (text == null) text = GetComponentInChildren<TMP_Text>(true);
        _vel = new Vector3(Random.Range(-randomX, randomX), floatSpeed, 0f);
    }

    public void Play(int damage)
    {
        if (text != null) text.text = damage.ToString();
    }

    private void Update()
    {
        _t += Time.deltaTime;
        transform.position += _vel * Time.deltaTime;

        if (_t >= lifeSeconds)
            Destroy(gameObject);
    }
}