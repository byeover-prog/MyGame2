// UTF-8
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PassiveStatBoard2D : MonoBehaviour
{
    public static PassiveStatBoard2D Instance { get; private set; }

    private readonly List<PassiveStatModifier2D> _mods = new List<PassiveStatModifier2D>(32);
    private readonly Dictionary<StatId2D, float> _add = new Dictionary<StatId2D, float>(32);
    private readonly Dictionary<StatId2D, float> _mul = new Dictionary<StatId2D, float>(32);
    private bool _dirty = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Register(PassiveStatModifier2D mod)
    {
        if (mod == null) return;
        if (_mods.Contains(mod)) return;
        _mods.Add(mod);
        _dirty = true;
    }

    public void Unregister(PassiveStatModifier2D mod)
    {
        if (mod == null) return;
        if (_mods.Remove(mod))
            _dirty = true;
    }

    public void MarkDirty()
    {
        _dirty = true;
    }

    public float GetAdd(StatId2D stat)
    {
        Ensure();
        return _add.TryGetValue(stat, out var v) ? v : 0f;
    }

    // Mul은 “0.1 = +10%” 같은 델타 방식으로 합성한다. (최종 multiplier = Π(1+delta))
    public float GetMul(StatId2D stat)
    {
        Ensure();
        return _mul.TryGetValue(stat, out var v) ? v : 1f;
    }

    private void Ensure()
    {
        if (!_dirty) return;
        Rebuild();
        _dirty = false;
    }

    private void Rebuild()
    {
        _add.Clear();
        _mul.Clear();

        for (int i = 0; i < _mods.Count; i++)
        {
            var m = _mods[i];
            if (m == null) continue;

            var entries = m.Entries;
            int lv = m.Level;

            for (int e = 0; e < entries.Length; e++)
            {
                var en = entries[e];
                float val = en.GetValue(lv);

                if (en.op == StatOp2D.Add)
                {
                    _add[en.stat] = (_add.TryGetValue(en.stat, out var cur) ? cur : 0f) + val;
                }
                else // Mul(delta)
                {
                    float curMul = _mul.TryGetValue(en.stat, out var cur) ? cur : 1f;
                    _mul[en.stat] = curMul * (1f + val);
                }
            }
        }
    }
}