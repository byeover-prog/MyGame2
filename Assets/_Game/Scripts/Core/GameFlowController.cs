// UTF-8
// GameSignals에 실제로 존재하는 이벤트/델리게이트 이름이 뭔지 몰라도,
// "레벨업 열기" / "오퍼 준비" 계열 이벤트를 찾아 자동 구독하고,
// LevelUpOrchestrator의 private 메서드까지 리플렉션으로 호출해 레벨업 패널을 띄운다.

using System;
using System.Reflection;
using UnityEngine;

public sealed class GameFlowController : MonoBehaviour
{
    [Header("레벨업 연결(없으면 자동 탐색)")]
    [SerializeField] private LevelUpOrchestrator levelUpOrchestrator;

    // 리플렉션 캐시
    private Type _signalsType;
    private EventInfo _evtOpen;
    private EventInfo _evtOffersReady;

    private Delegate _subOpen;
    private Delegate _subOffers;

    private MethodInfo _miOpen;
    private MethodInfo _miOffersReady;

    private void Awake()
    {
        if (levelUpOrchestrator == null)
            levelUpOrchestrator = FindFirstObjectByType<LevelUpOrchestrator>();

        if (levelUpOrchestrator == null)
        {
            Debug.LogError("[GameFlowController] LevelUpOrchestrator를 찾지 못했습니다. 씬에 LevelUpOrchestrator가 있어야 합니다.", this);
            return;
        }

        CacheSignals();
        CacheOrchestratorMethods();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        TryUnsubscribe();
    }

    // --------------------
    // 1) GameSignals 이벤트 찾기
    // --------------------
    private void CacheSignals()
    {
        _signalsType = FindTypeByName("GameSignals");
        if (_signalsType == null)
        {
            Debug.LogError("[GameFlowController] GameSignals 타입을 찾지 못했습니다. (파일/클래스명 확인 필요)", this);
            return;
        }

        // 이벤트 후보 이름들(프로젝트마다 다르므로 넓게 잡음)
        _evtOpen = FindEvent(_signalsType,
            "OnLevelUpOpenRequested",
            "LevelUpOpenRequested",
            "OnLevelUpOpen",
            "LevelUpOpen");

        _evtOffersReady = FindEvent(_signalsType,
            "OnOffersReady",
            "OffersReady",
            "OnLevelUpOffersReady",
            "LevelUpOffersReady");

        if (_evtOpen == null)
            GameLogger.LogWarning("[GameFlowController] 레벨업 오픈 이벤트를 GameSignals에서 찾지 못했습니다. (이름이 더 다를 수 있음)", this);

        if (_evtOffersReady == null)
            GameLogger.LogWarning("[GameFlowController] 오퍼 준비 이벤트를 GameSignals에서 찾지 못했습니다. (이름이 더 다를 수 있음)", this);
    }

    // --------------------
    // 2) Orchestrator 메서드 찾기(private 포함)
    // --------------------
    private void CacheOrchestratorMethods()
    {
        if (levelUpOrchestrator == null) return;

        var t = levelUpOrchestrator.GetType();

        // "열기" 계열 메서드 후보
        _miOpen = FindMethod(t,
            "HandleOpen",
            "Open",
            "RequestOpen",
            "OnOpenRequested");

        // "오퍼 결과" 계열 메서드 후보(Offer[] 파라미터)
        _miOffersReady = FindMethodWithOneParam(t, typeof(Offer[]),
            "HandleOffersReady",
            "OnOffersReady",
            "ApplyOffers",
            "PresentOffers");

        if (_miOpen == null)
            Debug.LogError("[GameFlowController] LevelUpOrchestrator에서 '열기' 메서드를 찾지 못했습니다. (HandleOpen/Open 등 이름 확인 필요)", this);

        if (_miOffersReady == null)
            Debug.LogError("[GameFlowController] LevelUpOrchestrator에서 Offer[]를 받는 '오퍼 준비' 메서드를 찾지 못했습니다.", this);
    }

    // --------------------
    // 3) 구독/해제
    // --------------------
    private void TrySubscribe()
    {
        if (_signalsType == null || levelUpOrchestrator == null) return;

        // 오픈 이벤트 구독
        if (_evtOpen != null && _subOpen == null)
        {
            _subOpen = CreateDelegateForEvent(_evtOpen, OnOpenRequested_Routed);
            if (_subOpen != null)
            {
                _evtOpen.AddEventHandler(null, _subOpen);
                GameLogger.Log("[GameFlowController] 레벨업 오픈 이벤트 구독 성공", this);
            }
        }

        // 오퍼 이벤트 구독
        if (_evtOffersReady != null && _subOffers == null)
        {
            _subOffers = CreateDelegateForEvent(_evtOffersReady, OnOffersReady_Routed);
            if (_subOffers != null)
            {
                _evtOffersReady.AddEventHandler(null, _subOffers);
                GameLogger.Log("[GameFlowController] 오퍼 준비 이벤트 구독 성공", this);
            }
        }
    }

    private void TryUnsubscribe()
    {
        if (_evtOpen != null && _subOpen != null)
        {
            _evtOpen.RemoveEventHandler(null, _subOpen);
            _subOpen = null;
        }

        if (_evtOffersReady != null && _subOffers != null)
        {
            _evtOffersReady.RemoveEventHandler(null, _subOffers);
            _subOffers = null;
        }
    }

    // --------------------
    // 4) 라우팅 핸들러
    // --------------------
    private void OnOpenRequested_Routed()
    {
        if (_miOpen == null || levelUpOrchestrator == null) return;

        try
        {
            _miOpen.Invoke(levelUpOrchestrator, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameFlowController] Open 호출 실패: {e.Message}", this);
        }
    }

    private void OnOffersReady_Routed(Offer[] offers)
    {
        if (_miOffersReady == null || levelUpOrchestrator == null) return;

        try
        {
            _miOffersReady.Invoke(levelUpOrchestrator, new object[] { offers });
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameFlowController] OffersReady 호출 실패: {e.Message}", this);
        }
    }

    // --------------------
    // 리플렉션 유틸
    // --------------------
    private static Type FindTypeByName(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName);
            if (t != null) return t;

            // 네임스페이스가 붙은 케이스도 대비
            foreach (var tt in asm.GetTypes())
            {
                if (tt.Name == typeName) return tt;
            }
        }
        return null;
    }

    private static EventInfo FindEvent(Type t, params string[] names)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        foreach (var n in names)
        {
            var e = t.GetEvent(n, flags);
            if (e != null) return e;
        }

        // 이름이 완전히 다르면: "LevelUp" + "Open" 포함 이벤트를 하나 더 탐색
        foreach (var e in t.GetEvents(flags))
        {
            string name = e.Name.ToLowerInvariant();
            if (name.Contains("levelup") && name.Contains("open")) return e;
        }

        return null;
    }

    private static MethodInfo FindMethod(Type t, params string[] names)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        foreach (var n in names)
        {
            var m = t.GetMethod(n, flags);
            if (m != null && m.GetParameters().Length == 0) return m;
        }
        return null;
    }

    private static MethodInfo FindMethodWithOneParam(Type t, Type paramType, params string[] names)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        foreach (var n in names)
        {
            var m = t.GetMethod(n, flags);
            if (m == null) continue;

            var ps = m.GetParameters();
            if (ps.Length == 1 && ps[0].ParameterType == paramType) return m;
        }

        // 이름 후보로 못 찾으면, Offer[] 1개 받는 메서드를 전체에서 탐색
        foreach (var m in t.GetMethods(flags))
        {
            var ps = m.GetParameters();
            if (ps.Length == 1 && ps[0].ParameterType == paramType)
            {
                string mn = m.Name.ToLowerInvariant();
                if (mn.Contains("offer")) return m;
            }
        }

        return null;
    }

    private static Delegate CreateDelegateForEvent(EventInfo evt, Action handler0)
    {
        try
        {
            var handlerType = evt.EventHandlerType;
            if (handlerType == null) return null;

            // Action(파라미터 0) 형태인 경우
            if (handlerType == typeof(Action))
                return Delegate.CreateDelegate(handlerType, handler0.Target, handler0.Method);

            // 다른 델리게이트 타입이지만 파라미터 0이면 시그니처 맞춰 생성 시도
            var invoke = handlerType.GetMethod("Invoke");
            if (invoke != null && invoke.GetParameters().Length == 0)
                return Delegate.CreateDelegate(handlerType, handler0.Target, handler0.Method);

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static Delegate CreateDelegateForEvent(EventInfo evt, Action<Offer[]> handler1)
    {
        try
        {
            var handlerType = evt.EventHandlerType;
            if (handlerType == null) return null;

            // Action<Offer[]> 형태인 경우
            if (handlerType == typeof(Action<Offer[]>))
                return Delegate.CreateDelegate(handlerType, handler1.Target, handler1.Method);

            // 다른 델리게이트 타입이지만 파라미터 1개(Offer[])면 시그니처 맞춰 생성 시도
            var invoke = handlerType.GetMethod("Invoke");
            if (invoke != null)
            {
                var ps = invoke.GetParameters();
                if (ps.Length == 1 && ps[0].ParameterType == typeof(Offer[]))
                    return Delegate.CreateDelegate(handlerType, handler1.Target, handler1.Method);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}