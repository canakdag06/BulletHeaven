using System;
using System.Collections.Generic;
using UnityEngine;

namespace BulletHeaven.Progression
{
    /// <summary>
    /// Single source of the player's stats: base values come from <see cref="PlayerBaseStatsSO"/>,
    /// modifiers are added at runtime. Final value = (base + Σflat) * (1 + Σpercent).
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private PlayerBaseStatsSO baseStats;

        private struct Entry
        {
            public StatModifier modifier;
            public object       source;
        }

        private static readonly int StatCount = Enum.GetValues(typeof(StatType)).Length;

        private readonly List<Entry> _entries = new();
        private readonly float[] _flat    = new float[StatCount];
        private readonly float[] _percent = new float[StatCount];

        /// <summary>Fired once per stat whose modifiers changed.</summary>
        public event Action<StatType> OnStatChanged;

        public PlayerBaseStatsSO BaseStats => baseStats;

        private void Awake()
        {
            if (baseStats == null)
                Debug.LogError("[PlayerStats] PlayerBaseStatsSO is not assigned; falling back to neutral defaults.", this);
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public float Get(StatType type) => Apply(type, GetBase(type));

        public float GetBase(StatType type) => baseStats != null ? baseStats.GetBase(type) : DefaultBase(type);

        /// <summary>Applies this stat's modifiers to a value owned elsewhere (e.g. a weapon's own base damage).</summary>
        public float Apply(StatType type, float baseValue)
        {
            int i = (int)type;
            return (baseValue + _flat[i]) * (1f + _percent[i]);
        }

        // ── Modifiers ─────────────────────────────────────────────────────────

        public void AddModifier(StatModifier modifier, object source)
        {
            _entries.Add(new Entry { modifier = modifier, source = source });
            Recalculate(modifier.stat);
        }

        public void RemoveModifiersFrom(object source)
        {
            Span<bool> changed = stackalloc bool[StatCount];

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (!Equals(_entries[i].source, source)) continue;
                changed[(int)_entries[i].modifier.stat] = true;
                _entries.RemoveAt(i);
            }

            for (int i = 0; i < StatCount; i++)
                if (changed[i]) Recalculate((StatType)i);
        }

        public void ClearAll()
        {
            _entries.Clear();
            for (int i = 0; i < StatCount; i++)
                Recalculate((StatType)i);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void Recalculate(StatType type)
        {
            int   index   = (int)type;
            float flat    = 0f;
            float percent = 0f;

            foreach (Entry entry in _entries)
            {
                if (entry.modifier.stat != type) continue;

                if (entry.modifier.kind == ModifierKind.Flat) flat    += entry.modifier.value;
                else                                          percent += entry.modifier.value;
            }

            bool dirty = !Mathf.Approximately(flat, _flat[index]) || !Mathf.Approximately(percent, _percent[index]);
            _flat[index]    = flat;
            _percent[index] = percent;

            if (dirty) OnStatChanged?.Invoke(type);
        }

        private static float DefaultBase(StatType type) => type switch
        {
            StatType.Damage           => 1f,
            StatType.Cooldown        => 1f,
            StatType.Area            => 1f,
            StatType.ProjectileSpeed => 1f,
            StatType.Duration        => 1f,
            StatType.MaxHealth       => 1f,
            StatType.PickupRadius    => 0.1f,
            _                        => 0f
        };
    }
}
