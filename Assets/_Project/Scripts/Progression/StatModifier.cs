using System;

namespace BulletHeaven.Progression
{
    public enum ModifierKind
    {
        Flat,
        /// <summary>Fraction of the base value: 0.1 = +10%, -0.1 = -10%.</summary>
        Percent
    }

    [Serializable]
    public struct StatModifier
    {
        public StatType     stat;
        public ModifierKind kind;
        public float        value;

        public StatModifier(StatType stat, ModifierKind kind, float value)
        {
            this.stat  = stat;
            this.kind  = kind;
            this.value = value;
        }
    }
}
