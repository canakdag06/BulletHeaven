using System;
using System.Collections.Generic;
using UnityEngine;

namespace BulletHeaven.Progression
{
    /// <summary>
    /// Holds every stat modifier applied to the player. Base values stay on the systems that own them
    /// (PlayerController, PlayerHealth, weapons…); they ask this component for the final value.
    /// Final value = (base + Σflat) * (1 + Σpercent).
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
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

        // ── Queries ───────────────────────────────────────────────────────────

        public float Apply(StatType type, float baseValue)
        {
            int i = (int)type;
            return (baseValue + _flat[i]) * (1f + _percent[i]);
        }

        /// <summary>Final value for stats without an owner-side base (multipliers start at 1, the rest at 0).</summary>
        public float Get(StatType type) => Apply(type, DefaultBase(type));

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
            StatType.Might           => 1f,
            StatType.Cooldown        => 1f,
            StatType.Area            => 1f,
            StatType.ProjectileSpeed => 1f,
            StatType.Duration        => 1f,
            _                        => 0f
        };
    }
}
