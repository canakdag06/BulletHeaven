using UnityEngine;

namespace BulletHeaven.Enemy
{
    [CreateAssetMenu(menuName = "BulletHeaven/Enemy Definition", fileName = "NewEnemyDefinition")]
    public class EnemyDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private EnemyType type = EnemyType.Slime;
        [SerializeField, Min(1)] private int tier = 1;

        [Header("Stats")]
        [SerializeField, Min(1)]    private int   maxHealth      = 10;
        [SerializeField, Min(0)]    private int   damage         = 5;
        [SerializeField, Min(0f)]   private float moveSpeed      = 1f;
        [SerializeField, Min(0.1f)] private float attackInterval = 1f;

        [Header("Visuals")]
        [SerializeField] private AnimatedMeshScriptableObject[] animationSet;
        [SerializeField] private Material[] materials;
        [SerializeField, Min(0.01f)] private float scale = 1f;

        [Header("Animation Names")]
        [SerializeField] private string walkAnimName  = "Enemy_Walking";
        [SerializeField] private string deathAnimName = "Enemy_Dying";

        public EnemyType Type         => type;
        public int    Tier           => tier;
        public int    MaxHealth      => maxHealth;
        public int    Damage         => damage;
        public float  MoveSpeed      => moveSpeed;
        public float  AttackInterval => attackInterval;

        public AnimatedMeshScriptableObject[] AnimationSet => animationSet;
        public Material[] Materials => materials;
        public float      Scale     => scale;

        public string WalkAnimName  => walkAnimName;
        public string DeathAnimName => deathAnimName;
    }

    public enum EnemyType
    {
        Slime = 0,
        Bat = 1,

    }
}
