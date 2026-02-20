using System.Collections.Generic;
using UnityEngine;

public sealed class SkillLevelUpOfferBuilder2D : MonoBehaviour
{
    [Header("트랙 DB")]
    [SerializeField] private List<SkillLevelTrackSO> tracks = new List<SkillLevelTrackSO>(16);
    [SerializeField] private WeaponShooterSlotUpgradeApplier2D applier;

    private readonly Dictionary<string, SkillLevelTrackSO> _trackMap = new Dictionary<string, SkillLevelTrackSO>(64);

    private void Awake()
    {
        if (applier == null)
            applier = FindFirstObjectByType<WeaponShooterSlotUpgradeApplier2D>();

        BuildMap();
    }

    private void BuildMap()
    {
        _trackMap.Clear();
        for (int i = 0; i < tracks.Count; i++)
        {
            var t = tracks[i];
            if (t == null) continue;
            if (string.IsNullOrWhiteSpace(t.weaponId)) continue;
            if (!_trackMap.ContainsKey(t.weaponId))
                _trackMap.Add(t.weaponId, t);
        }
    }

    public List<WeaponUpgradeCardSO> BuildOffers(WeaponShooterSystem2D shooter, SkillLevelRuntimeState2D levelState, int offerCount = 3)
    {
        var candidates = new List<WeaponUpgradeCardSO>(64);

        if (shooter == null || levelState == null)
            return candidates;

        // Shooter 슬롯 구조는 Serialized가 아니라 런타임 필드 접근이 필요하므로,
        // 여기서는 applier가 실제 슬롯을 만질 수 있다는 전제(우리가 이미 그렇게 만들었음)로 간다.
        // 후보는 "트랙 + 현재레벨"로 만든다.

        // 슬롯 수 파악: applier 내부 리플렉션으로도 알 수 있지만,
        // 여기선 shooter가 공개 API가 없으니 "업그레이드 후보는 트랙 기반"으로만 만든다.
        // 실제 slotIndex는 카드에 박아서 Picker/Applier가 적용한다.

        // 가장 안전한 방법: tracks를 기준으로 slotIndex를 찾는 게 아니라,
        // shooter slots에서 weaponId를 가져와야 한다.
        // 이 프로젝트에서 weaponId는 WeaponDefinitionSO에 public string weaponId로 확정했으니,
        // shooter의 slots[i].weapon을 리플렉션으로 읽어온다.

        var slots = ShooterSlotReader.ReadSlots(shooter);
        if (slots == null) return candidates;

        for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
        {
            var weaponDef = slots[slotIndex];
            if (weaponDef == null) continue;

            string wid = weaponDef.weaponId;
            if (string.IsNullOrWhiteSpace(wid)) continue;

            if (!_trackMap.TryGetValue(wid, out SkillLevelTrackSO track) || track == null)
                continue;

            int curLevel = levelState.GetLevel(slotIndex); // 0~8
            if (curLevel >= 8) continue;

            int nextLevel = curLevel + 1;

            if (!track.TryGetStep(nextLevel, out var step) || step == null || step.upgrades == null || step.upgrades.Count == 0)
                continue;

            // 다음 레벨 업그레이드 정의들을 카드 후보로 생성
            for (int u = 0; u < step.upgrades.Count; u++)
            {
                var def = step.upgrades[u];
                if (def == null) continue;

                // 슬롯 제한: Enable 카드가 "새로 켜는" 상황이면 후보에서 제거
                if (def.type == WeaponUpgradeType.ToggleEnabled && def.value.toggleBool)
                {
                    if (applier != null && !applier.CanEnableSlot(slotIndex))
                        continue;
                }

                var card = ScriptableObject.CreateInstance<WeaponUpgradeCardSO>();
                card.slotIndex = slotIndex;
                card.weaponId = wid;
                card.weaponNameKr = track.GetDisplayName();
                card.tagsKr = string.IsNullOrWhiteSpace(def.tagsKr) ? "공통" : def.tagsKr;

                card.type = def.type;
                card.value = def.value;

                // 제목: 오버라이드 우선, 없으면 자동
                card.titleKr = string.IsNullOrWhiteSpace(def.titleOverrideKr)
                    ? BuildTitle(card.weaponNameKr, def.type, def.value)
                    : def.titleOverrideKr;

                // 설명은 formatter(한 군데)로만 내보내는 게 안전하니 비움
                card.descKr = "";

                candidates.Add(card);
            }
        }

        // 후보에서 3장 뽑기(슬롯 중복 방지: 같은 slotIndex 카드 2장 나오면 UX 망)
        var picked = WeightedPickWithoutReplacementBySlot(candidates, offerCount);

        return picked;
    }

    private static string BuildTitle(string weaponName, WeaponUpgradeType type, UpgradeValue v)
    {
        // UI 용어 고정: 공속/발사간격 금지. "쿨타임/틱간격"은 type로 분기해서 처리할 것.
        switch (type)
        {
            case WeaponUpgradeType.ToggleEnabled:
                return $"{weaponName} 활성화";

            case WeaponUpgradeType.DamageAdd:
                return $"{weaponName} 피해 +{v.addInt}";

            case WeaponUpgradeType.CooldownMul:
                {
                    float pctDown = (1f - v.mulFloat) * 100f;
                    if (pctDown >= 0f) return $"{weaponName} 쿨타임 {pctDown:0}% 감소";
                    return $"{weaponName} 쿨타임 {(-pctDown):0}% 증가";
                }

            case WeaponUpgradeType.RangeAdd:
                return $"{weaponName} 사거리 +{v.addFloat:0.#}";

            case WeaponUpgradeType.UpgradeStateAddShotCount:
                return $"{weaponName} 투사체 +{v.addInt}";

            case WeaponUpgradeType.UpgradeStateAddPierce:
                return $"{weaponName} 관통 +{v.addInt}";

            case WeaponUpgradeType.UpgradeStateAddSplit:
                return $"{weaponName} 분열 +{v.addInt}";

            case WeaponUpgradeType.UpgradeStateEnableHoming:
                return $"{weaponName} 호밍 {(v.toggleBool ? "활성화" : "비활성화")}";

            default:
                return $"{weaponName} 업그레이드";
        }
    }

    private static List<WeaponUpgradeCardSO> WeightedPickWithoutReplacementBySlot(List<WeaponUpgradeCardSO> candidates, int count)
    {
        // 지금은 weight를 트랙/무기 SO까지 붙이기 전 단계라,
        // 일단 균등 랜덤 + slotIndex 중복 방지로 간다.
        // (가중치 필요하면 WeaponDefinitionSO.weight를 읽어서 여기서 반영하면 됨)

        var temp = new List<WeaponUpgradeCardSO>(candidates);
        for (int i = 0; i < temp.Count; i++)
        {
            int j = Random.Range(i, temp.Count);
            var t = temp[i];
            temp[i] = temp[j];
            temp[j] = t;
        }

        var result = new List<WeaponUpgradeCardSO>(count);
        var usedSlots = new HashSet<int>();

        for (int i = 0; i < temp.Count && result.Count < count; i++)
        {
            var c = temp[i];
            if (c == null) continue;
            if (usedSlots.Add(c.slotIndex))
                result.Add(c);
        }

        // 부족하면 슬롯 중복 허용으로 채움
        for (int i = 0; i < temp.Count && result.Count < count; i++)
        {
            var c = temp[i];
            if (c == null) continue;
            if (!result.Contains(c))
                result.Add(c);
        }

        return result;
    }

    // shooter slots에서 WeaponDefinitionSO를 읽기 위한 최소 리플렉션 리더
    private static class ShooterSlotReader
    {
        private static System.Reflection.FieldInfo _slotsField;
        private static System.Reflection.FieldInfo _weaponField;

        public static List<WeaponDefinitionSO> ReadSlots(WeaponShooterSystem2D shooter)
        {
            if (shooter == null) return null;

            if (_slotsField == null)
            {
                var t = shooter.GetType();
                _slotsField = t.GetField("slots", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (_slotsField == null) return null;
            }

            object slotsObj = _slotsField.GetValue(shooter);
            if (!(slotsObj is System.Collections.IList list)) return null;

            var result = new List<WeaponDefinitionSO>(list.Count);

            for (int i = 0; i < list.Count; i++)
            {
                object elem = list[i];
                if (elem == null)
                {
                    result.Add(null);
                    continue;
                }

                if (_weaponField == null)
                {
                    var et = elem.GetType();
                    _weaponField = et.GetField("weapon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (_weaponField == null)
                    {
                        result.Add(null);
                        continue;
                    }
                }

                var weaponObj = _weaponField.GetValue(elem) as WeaponDefinitionSO;
                result.Add(weaponObj);
            }

            return result;
        }
    }
}
