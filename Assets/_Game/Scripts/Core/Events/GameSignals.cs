using System;
using UnityEngine;

public static class GameSignals
{
    // 레벨업 열기 요청(게임 -> 오케스트레이터/ UI)
    public static event Action LevelUpOpenRequested;

    // 오퍼 3장 준비됨(오퍼서비스 -> UI)
    public static event Action<Offer[]> OffersReady;

    // 유저가 카드 선택(UI -> 오케스트레이터)
    public static event Action<Offer> OfferPicked;

    // 리롤 요청(UI -> 오퍼서비스)
    public static event Action RerollRequested;

    // 레벨업 종료(오케스트레이터 -> UI/게임)
    public static event Action LevelUpClosed;

    // 스킬 레벨 변경(오케스트레이터/런타임 -> UI/디버그)
    public static event Action<string, int> SkillLevelChanged;

    public static void RaiseLevelUpOpenRequested() => LevelUpOpenRequested?.Invoke();
    public static void RaiseOffersReady(Offer[] offers) => OffersReady?.Invoke(offers);
    public static void RaiseOfferPicked(Offer offer) => OfferPicked?.Invoke(offer);
    public static void RaiseRerollRequested() => RerollRequested?.Invoke();
    public static void RaiseLevelUpClosed() => LevelUpClosed?.Invoke();
    public static void RaiseSkillLevelChanged(string id, int level) => SkillLevelChanged?.Invoke(id, level);
}