using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EXP -> LevelUp -> 카드 3장 제시 -> 선택 적용(무기/공통스킬)까지 end-to-end로 연결하는 최소 시스템.
/// 
/// 이 프로젝트에서 중요하게 지키는 것
/// 1) 컴파일 가능한 상태(누락 타입/전제 없는 코드 금지)
/// 2) Awake 순서 의존 금지(Root는 Start에서 읽는다)
/// 3) 카드 UI는 '레벨 표기 없음' + '레벨1 공격 방식 설명 반드시 포함'
/// 
/// 복잡도
/// - 후보 생성: O(무기트랙 후보 수 + 공통스킬 후보 수)
/// - 선택: O(count * n)
/// </summary>
[DisallowMultipleComponent]
public sealed class LevelUpSystem2D : MonoBehaviour
{
    [Header("레벨업 소스")]
    [SerializeField] private PlayerExp playerExp;

    [Header("UI")]
    [SerializeField] private LevelUpCardPicker picker;

    [Header("무기(슬롯 기반)")]
    [SerializeField] private WeaponShooterSystem2D shooter;
    [SerializeField] private SkillLevelRuntimeState2D levelState;
    [SerializeField] private SkillLevelUpOfferBuilder2D weaponOfferBuilder;
    [SerializeField] private WeaponShooterSlotUpgradeApplier2D weaponApplier;

    [Header("공통 스킬")]
    [SerializeField] private CommonSkillManager2D commonSkillManager;
    [SerializeField] private CommonSkillCardPoolSO commonSkillPool;

    [Header("설정(필요하면 Root(LevelUpRootSO)에서 덮어씀)")]
    [SerializeField] private int offerCount = 3;
    [SerializeField] private bool pauseGameWhileOpen = true;
    [SerializeField] private float openIntervalRealtime = 0.25f;
    [SerializeField] private KeyCode debugOpenKey = KeyCode.F1;

    [Header("가중치(상대 비율)")]
    [SerializeField] private int weaponWeightMultiplier = 1;
    [SerializeField] private int commonSkillWeightMultiplier = 10;

    // ----------------------------
    // 내부 상태
    // ----------------------------

    private int _pendingOpens;
    private bool _processing;

    private float _prevTimeScale = 1f;
    private bool _pausedByMe;

    private readonly List<Candidate> _candidates = new List<Candidate>(128);
    private readonly List<ILevelUpCardData> _offers = new List<ILevelUpCardData>(3);

    private struct Candidate
    {
        public ILevelUpCardData data;
        public int weight;
        public string key;
    }

    private void Reset()
    {
        // Inspector 자동 채우기(편의)
        playerExp = FindFirstObjectByType<PlayerExp>();
        picker = FindFirstObjectByType<LevelUpCardPicker>();

        shooter = FindFirstObjectByType<WeaponShooterSystem2D>();
        levelState = FindFirstObjectByType<SkillLevelRuntimeState2D>();
        weaponOfferBuilder = FindFirstObjectByType<SkillLevelUpOfferBuilder2D>();
        weaponApplier = FindFirstObjectByType<WeaponShooterSlotUpgradeApplier2D>();

        commonSkillManager = FindFirstObjectByType<CommonSkillManager2D>();
    }

    private void Start()
    {
        ResolveReferencesFromRoot();
        SeedWeaponLevelsFromShooter();

        if (picker != null)
            picker.OnClosed += HandlePickerClosed;

        if (playerExp != null)
            playerExp.OnLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (picker != null)
            picker.OnClosed -= HandlePickerClosed;

        if (playerExp != null)
            playerExp.OnLevelUp -= HandleLevelUp;
    }

    private void Update()
    {
        if (debugOpenKey != KeyCode.None && Input.GetKeyDown(debugOpenKey))
            RequestOpen();
    }

    private void HandleLevelUp(int newLevel)
    {
        RequestOpen();
    }

    private void RequestOpen()
    {
        _pendingOpens++;

        if (!_processing)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        _processing = true;

        while (_pendingOpens > 0)
        {
            // picker가 없다면 더 진행 불가
            if (picker == null)
            {
                _pendingOpens = 0;
                break;
            }

            // 이미 열려있으면 닫힐 때까지 대기
            if (picker.IsOpen)
            {
                yield return null;
                continue;
            }

            // 연속 레벨업 시 오픈 간격(Realtime)
            if (openIntervalRealtime > 0f)
                yield return new WaitForSecondsRealtime(openIntervalRealtime);

            bool opened = TryOpenOnce();

            // 레벨업 1회 소모
            _pendingOpens = Mathf.Max(0, _pendingOpens - 1);

            if (!opened)
                continue;

            // 닫힐 때까지 대기
            while (picker.IsOpen)
                yield return null;
        }

        _processing = false;
    }

    private bool TryOpenOnce()
    {
        int clampedOfferCount = Mathf.Clamp(offerCount, 1, 3);

        BuildCandidates();
        if (_candidates.Count == 0)
            return false;

        // 1차: key 기준으로 중복 방지(같은 슬롯/같은 스킬이 2장 뜨는 UX 방지)
        var picked = WeightedOfferPicker.Pick(
            _candidates,
            clampedOfferCount,
            c => Mathf.Max(0, c.weight),
            c => c.key);

        _offers.Clear();

        // picked는 Candidate 리스트
        for (int i = 0; i < picked.Count; i++)
        {
            var c = picked[i];
            if (c.data != null)
                _offers.Add(c.data);
        }

        // 2차: 부족하면 남은 후보에서 채우기(중복 허용)
        if (_offers.Count < clampedOfferCount)
        {
            FillRemainingOffers(clampedOfferCount);
        }

        if (_offers.Count == 0)
            return false;

        if (pauseGameWhileOpen)
            PauseGame();

        picker.OpenCards(_offers);
        return true;
    }

    private void BuildCandidates()
    {
        _candidates.Clear();

        // ----------------------------
        // 무기 업그레이드 후보(트랙 기반)
        // ----------------------------
        if (weaponOfferBuilder != null && shooter != null && levelState != null)
        {
            var cards = weaponOfferBuilder.BuildCandidates(shooter, levelState);
            if (cards != null)
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    var card = cards[i];
                    if (card == null) continue;

                    WeaponDefinitionSO weaponDef = TryResolveWeaponDef(card.slotIndex);

                    var data = new WeaponUpgradeCardData2D(card, shooter, weaponDef, levelState, weaponApplier);
                    if (!data.CanPick())
                        continue;

                    int baseWeight = weaponDef != null ? weaponDef.weight : 100;
                    int w = Mathf.Max(1, baseWeight) * Mathf.Max(0, weaponWeightMultiplier);

                    _candidates.Add(new Candidate
                    {
                        data = data,
                        weight = w,
                        key = $"WEP_SLOT_{card.slotIndex}" // 같은 슬롯 카드 중복 방지
                    });
                }
            }
        }

        // ----------------------------
        // 공통 스킬 후보(카드풀 기반)
        // ----------------------------
        if (commonSkillPool != null && commonSkillManager != null)
        {
            var cards = commonSkillPool.cards;
            if (cards != null)
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    var card = cards[i];
                    if (card == null || card.skill == null) continue;

                    if (commonSkillManager.IsMaxLevel(card.skill))
                        continue;

                    var data = new CommonSkillCardData2D(commonSkillManager, card.skill);
                    if (!data.CanPick())
                        continue;

                    int w = Mathf.Max(1, card.weight) * Mathf.Max(0, commonSkillWeightMultiplier);

                    _candidates.Add(new Candidate
                    {
                        data = data,
                        weight = w,
                        key = $"CS_{(int)card.skill.kind}" // 같은 스킬 중복 방지
                    });
                }
            }
        }
    }

    private void FillRemainingOffers(int targetCount)
    {
        // 이미 선택된 데이터는 제외하고 랜덤으로 채움
        int safety = 256;

        while (_offers.Count < targetCount && safety-- > 0)
        {
            if (_candidates.Count == 0)
                break;

            int idx = UnityEngine.Random.Range(0, _candidates.Count);
            var c = _candidates[idx];

            if (c.data == null)
                continue;

            bool already = false;
            for (int i = 0; i < _offers.Count; i++)
            {
                if (ReferenceEquals(_offers[i], c.data))
                {
                    already = true;
                    break;
                }
            }

            if (already)
                continue;

            _offers.Add(c.data);
        }
    }

    private void HandlePickerClosed()
    {
        ResumeGame();
    }

    private void PauseGame()
    {
        if (_pausedByMe) return;

        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _pausedByMe = true;
    }

    private void ResumeGame()
    {
        if (!_pausedByMe) return;

        Time.timeScale = _prevTimeScale;
        _pausedByMe = false;
    }

    private WeaponDefinitionSO TryResolveWeaponDef(int slotIndex)
    {
        if (shooter == null) return null;

        var slots = shooter.SlotsReadOnly;
        if (slots == null) return null;

        if (slotIndex < 0 || slotIndex >= slots.Count) return null;

        var s = slots[slotIndex];
        return s != null ? s.weapon : null;
    }

    private void SeedWeaponLevelsFromShooter()
    {
        // "이미 활성화된 기본 무기"가 1레벨로 취급되도록 levelState를 초기화.
        // (이걸 안 하면 첫 레벨업 때 '활성화 카드'가 다시 뜨는 식으로 꼬일 수 있음)
        if (shooter == null || levelState == null) return;

        var slots = shooter.SlotsReadOnly;
        if (slots == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s == null || s.weapon == null) continue;

            int lv = s.enabled ? Mathf.Max(1, s.level) : 0;
            levelState.SetLevel(i, lv);
        }
    }

    private void ResolveReferencesFromRoot()
    {
        // 1) Root에서 설정/데이터를 주입 (있으면 Root 우선)
        var root = RootBootstrapper.Instance;
        if (root != null)
        {
            // LevelUpRootSO
            if (root.LevelUpRoot != null)
            {
                offerCount = root.LevelUpRoot.offerCount;
                pauseGameWhileOpen = root.LevelUpRoot.pauseGameWhileOpen;
                openIntervalRealtime = root.LevelUpRoot.openIntervalRealtime;
                debugOpenKey = root.LevelUpRoot.debugOpenKey;

                weaponWeightMultiplier = root.LevelUpRoot.weaponWeightMultiplier;
                commonSkillWeightMultiplier = root.LevelUpRoot.commonSkillWeightMultiplier;
            }

            // SkillRootSO
            if (root.SkillRoot != null)
            {
                if (commonSkillPool == null)
                    commonSkillPool = root.SkillRoot.commonSkillCardPool;

                if (weaponOfferBuilder != null && root.SkillRoot.weaponSkillTracks != null && root.SkillRoot.weaponSkillTracks.Count > 0)
                    weaponOfferBuilder.SetTracks(root.SkillRoot.weaponSkillTracks);
            }
        }

        // 2) Inspector 미연결이면 씬에서 자동 탐색(편의)
        if (playerExp == null) playerExp = FindFirstObjectByType<PlayerExp>();
        if (picker == null) picker = FindFirstObjectByType<LevelUpCardPicker>();

        if (shooter == null) shooter = FindFirstObjectByType<WeaponShooterSystem2D>();
        if (levelState == null) levelState = FindFirstObjectByType<SkillLevelRuntimeState2D>();
        if (weaponOfferBuilder == null) weaponOfferBuilder = FindFirstObjectByType<SkillLevelUpOfferBuilder2D>();
        if (weaponApplier == null) weaponApplier = FindFirstObjectByType<WeaponShooterSlotUpgradeApplier2D>();

        if (commonSkillManager == null) commonSkillManager = FindFirstObjectByType<CommonSkillManager2D>();


        // 최종 체크(경고만)
        if (picker == null)
            Debug.LogWarning("[LevelUpSystem2D] LevelUpCardPicker가 없습니다. 레벨업 UI가 열리지 않습니다.", this);
        if (playerExp == null)
            Debug.LogWarning("[LevelUpSystem2D] PlayerExp가 없습니다. 레벨업 이벤트를 받지 못합니다.", this);
    }
}
