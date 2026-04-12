// UTF-8
using System;
using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamagePopup2D : MonoBehaviour
{
    [Header("표시(TMP)")]
    [SerializeField, InspectorName("텍스트(TMP)")]
    [Tooltip("비워두면 자식에서 자동 탐색합니다.")]
    private TMP_Text text;

    [Header("애니메이션")]
    [SerializeField, InspectorName("생존 시간(초)")]
    private float lifeSeconds = 0.8f;

    [SerializeField, InspectorName("위로 이동량")]
    private Vector3 moveOffset = new Vector3(0f, 0.8f, 0f);

    [SerializeField, InspectorName("좌우 랜덤")]
    [Tooltip("여러 숫자가 겹칠 때 살짝 퍼지게 합니다.")]
    private float randomX = 0.18f;

    private Action<DamagePopup2D> _onDone;
    private Vector3 _startPos;
    private Vector3 _startScale;

    private bool _useGradient;
    private Color _baseColor;
    private VertexGradient _baseGradient;
    private float _baseOpacity = 1f;

    private void Awake()
    {
        if (text == null) text = GetComponentInChildren<TMP_Text>(true);
        _startScale = transform.localScale;
    }

    public void Play(int amount, DamageElement2D element, float opacity, Vector3 worldPos, Action<DamagePopup2D> onDone)
    {
        _onDone = onDone;
        _baseOpacity = Mathf.Clamp01(opacity);

        _startPos = worldPos + new Vector3(UnityEngine.Random.Range(-randomX, randomX), 0f, 0f);
        transform.position = _startPos;
        transform.localScale = _startScale;

        ApplyElementStyle(element);
        SetAmount(amount);

        gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(CoPlay());
    }

    private void SetAmount(int amount)
    {
        if (text == null) return;
        text.text = amount.ToString("N0");
    }

    private void ApplyElementStyle(DamageElement2D element)
    {
        if (text == null) return;

        _useGradient = false;
        text.enableVertexGradient = false;

        // 타 게임에서 흔히 쓰는 계열로 맞춘 값들(필요하면 여기만 조정)
        switch (element)
        {
            default:
            case DamageElement2D.Physical: _baseColor = Color.white; break;                         // 물리: 흰색
            case DamageElement2D.Ice:      _baseColor = new Color(0.55f, 0.85f, 1.00f, 1f); break; // 빙결: 하늘색 (겨울왕국 느낌)
            case DamageElement2D.Fire:     _baseColor = new Color(1.00f, 0.45f, 0.10f, 1f); break; // 화염: 주황 (원신/디아블로)
            case DamageElement2D.Electric: _baseColor = new Color(1.00f, 0.95f, 0.30f, 1f); break; // 전기: 밝은 노랑 (포켓몬/LoL)
            case DamageElement2D.Earth:    _baseColor = new Color(0.82f, 0.60f, 0.32f, 1f); break; // 땅: 황토색
            case DamageElement2D.Wind:     _baseColor = new Color(0.50f, 0.92f, 0.65f, 1f); break; // 바람: 연초록 (원신 풍원소)
            case DamageElement2D.Water:    _baseColor = new Color(0.20f, 0.55f, 1.00f, 1f); break; // 물: 진한 파랑
            case DamageElement2D.Light:
                _useGradient = true;
                text.enableVertexGradient = true;
                Color lightTop = new Color(1.00f, 1.00f, 0.85f, 1f);    // 위: 밝은 크림
                Color lightBot = new Color(1.00f, 0.80f, 0.20f, 1f);    // 아래: 따뜻한 금색
                _baseGradient = new VertexGradient(lightTop, lightTop, lightBot, lightBot);
                text.colorGradient = _baseGradient;
                _baseColor = lightTop;
                break;
            case DamageElement2D.Dark:
                _useGradient = true;
                text.enableVertexGradient = true;
                Color top = new Color(0.75f, 0.45f, 1.00f, 1f);    // 위: 보라
                Color bottom = new Color(0.30f, 0.10f, 0.50f, 1f); // 아래: 짙은 보라 (음 속성 그라데이션)
                _baseGradient = new VertexGradient(top, top, bottom, bottom);
                text.colorGradient = _baseGradient;
                _baseColor = top;
                break;
        }

        if (!_useGradient)
        {
            text.color = _baseColor;
        }
    }

    private void ApplyOpacity(float a)
    {
        if (text == null) return;

        if (_useGradient)
        {
            Color tl = _baseGradient.topLeft;     tl.a = a;
            Color tr = _baseGradient.topRight;    tr.a = a;
            Color bl = _baseGradient.bottomLeft;  bl.a = a;
            Color br = _baseGradient.bottomRight; br.a = a;
            text.colorGradient = new VertexGradient(tl, tr, bl, br);

            // 글자 전체 알파가 필요할 때를 대비한 곱(대부분 문제 없음)
            text.color = new Color(1f, 1f, 1f, a);
        }
        else
        {
            Color c = _baseColor;
            c.a = a;
            text.color = c;
        }
    }

    private IEnumerator CoPlay()
    {
        // 구현 원리: 위로 이동 + 알파 페이드아웃 후 풀로 반납
        float t = 0f;

        while (t < lifeSeconds)
        {
            float n = (lifeSeconds <= 0.0001f) ? 1f : (t / lifeSeconds);

            transform.position = Vector3.Lerp(_startPos, _startPos + moveOffset, n);

            float a = (1f - n) * _baseOpacity;
            ApplyOpacity(a);

            t += Time.deltaTime;
            yield return null;
        }

        _onDone?.Invoke(this);
    }
}