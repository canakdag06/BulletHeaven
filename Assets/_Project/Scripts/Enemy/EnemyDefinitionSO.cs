using System;
using UnityEngine;
using BulletHeaven.Collectibles;

namespace BulletHeaven.Enemy
{
    [Serializable]
    public struct ExpDropEntry
    {
        public ExpTier Tier;
        [Min(0)] public int Weight;
    }

    [CreateAssetMenu(menuName = "BulletHeaven/Enemy Definition", fileName = "NewEnemyDefinition")]
    public class EnemyDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private EnemyType type = EnemyType.Spider;
        [SerializeField, Min(1)] private int tier = 1;

        [Header("Experience")]
        [SerializeField] private ExpDropEntry[] expDrops =
        {
            new() { Tier = ExpTier.LowEXP, Weight = 100 },
        };

        [Header("Stats")]
        [SerializeField, Min(1)]    private int   maxHealth      = 10;
        [SerializeField, Min(0)]    private int   damage         = 5;
        [SerializeField, Min(0f)]   private float moveSpeed      = 1f;
        [SerializeField, Min(0.1f)] private float attackInterval = 1f;

        [Header("Visuals")]
        [SerializeField] private AnimatedMeshScriptableObject[] animationSet;
        [SerializeField] private Material[] materials;

        [Header("Model Transform")]
        [Tooltip("Local euler rotation applied to the Model child.")]
        [SerializeField] private Vector3 modelRotation = Vector3.zero;
        [Tooltip("Applied to the Model child. Visual only; does not affect collider or agent.")]
        [SerializeField] private Vector3 modelScale    = Vector3.one;

        [Header("Collider")]
        [SerializeField] private Vector3 colliderCenter = new(0f, 0.9f, 0f);
        [SerializeField] private Vector3 colliderSize   = new(0.6f, 1.8f, 0.6f);

        [Header("Navigation")]
        [SerializeField, Min(0.01f)] private float agentRadius = 0.28f;

        [Header("Animation Speed")]
        [Tooltip("Movement speed (units/sec) at which the walk animation plays at 1x.")]
        [SerializeField, Min(0.01f)] private float walkAnimReferenceSpeed = 1f;
        [Tooltip("Upper limit for the walk animation speed multiplier.")]
        [SerializeField, Min(0.1f)]  private float maxWalkAnimSpeed = 3f;

        public EnemyType Type         => type;
        public int    Tier           => tier;

        public ExpTier RollExpTier()
        {
            if (expDrops == null || expDrops.Length == 0)
                return ExpTier.LowEXP;

            int total = 0;
            for (int i = 0; i < expDrops.Length; i++)
                total += expDrops[i].Weight;

            if (total <= 0)
                return ExpTier.LowEXP;

            int roll       = UnityEngine.Random.Range(0, total);
            int cumulative = 0;

            for (int i = 0; i < expDrops.Length; i++)
            {
                cumulative += expDrops[i].Weight;
                if (roll < cumulative)
                    return expDrops[i].Tier;
            }

            return expDrops[expDrops.Length - 1].Tier;
        }
        public int    MaxHealth      => maxHealth;
        public int    Damage         => damage;
        public float  MoveSpeed      => moveSpeed;
        public float  AttackInterval => attackInterval;

        public AnimatedMeshScriptableObject[] AnimationSet => animationSet;
        public Material[] Materials => materials;

        public Vector3 ModelRotation  => modelRotation;
        public Vector3 ModelScale     => modelScale;
        public Vector3 ColliderCenter => colliderCenter;
        public Vector3 ColliderSize   => colliderSize;
        public float   AgentRadius    => agentRadius;

        public float WalkAnimReferenceSpeed => walkAnimReferenceSpeed;
        public float MaxWalkAnimSpeed       => maxWalkAnimSpeed;
    }

    public enum EnemyType
    {
        Spider = 0,
        Mushroom = 1,
        Orc = 2,
        Pigman = 3,
        EyeBat = 4,
    }
}
