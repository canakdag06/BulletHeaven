using System;
using UnityEngine;
using BulletHeaven.Core;
using BulletHeaven.Progression;
using BulletHeaven.Weapons;

namespace BulletHeaven.Loadout
{
    /// <summary>
    /// Owns the run's weapons and passives. Weapons are scene objects parented to the player.
    /// Passives are data: each rank replaces that passive's modifiers on <see cref="PlayerStats"/>.
    /// Slots are fixed arrays so taking or leveling an item does not allocate.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    [DisallowMultipleComponent]
    // After WeaponBase.Awake, so the starter's Initialize(level 1) is not overwritten by startLevel.
    [DefaultExecutionOrder(10)]
    public class PlayerLoadout : MonoBehaviour
    {
        public const int MaxWeaponSlots  = 6;
        public const int MaxPassiveSlots = 6;

        [Header("Starting Loadout")]
        [Tooltip("Granted at level 1 on every round. When this matches the WeaponBase already on the player, that component is reused. Leave empty to keep whatever WeaponBase is already on this object.")]
        [SerializeField] private WeaponDefinitionSO startingWeapon;

        private struct WeaponSlot
        {
            public WeaponDefinitionSO definition;
            public WeaponBase         instance;
            public bool               ownedByLoadout;
        }

        private struct PassiveSlot
        {
            public PassiveDefinitionSO definition;
            public int                 level;
        }

        private readonly WeaponSlot[]  _weapons  = new WeaponSlot[MaxWeaponSlots];
        private readonly PassiveSlot[] _passives = new PassiveSlot[MaxPassiveSlots];

        private PlayerStats _stats;
        private WeaponBase  _embeddedWeapon;
        private int         _weaponCount;
        private int         _passiveCount;
        private bool        _missingStarterWarned;

        /// <summary>
        /// Fired after a weapon or passive is gained, leveled, or the loadout is reset.
        /// Read the new state with <see cref="GetLevel"/>, <see cref="GetWeapon"/> and <see cref="GetPassive"/>.
        /// </summary>
        public event Action OnLoadoutChanged;

        public int WeaponCount  => _weaponCount;
        public int PassiveCount => _passiveCount;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _embeddedWeapon = GetComponent<WeaponBase>();

            if (_stats == null)
            {
                Debug.LogError("[PlayerLoadout] PlayerStats is missing.", this);
                return;
            }

            Rebuild();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRoundReset += OnRoundReset;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRoundReset -= OnRoundReset;
        }

        // ── Queries ───────────────────────────────────────────────────────────

        /// <summary>0 when the weapon is not in the loadout.</summary>
        public int GetLevel(WeaponDefinitionSO weapon)
        {
            int index = FindWeapon(weapon);
            return index >= 0 ? GetWeaponLevel(index) : 0;
        }

        /// <summary>0 when the passive is not in the loadout.</summary>
        public int GetLevel(PassiveDefinitionSO passive)
        {
            int index = FindPassive(passive);
            return index >= 0 ? _passives[index].level : 0;
        }

        public WeaponDefinitionSO GetWeapon(int index)
        {
            if (index < 0 || index >= _weaponCount) return null;
            return _weapons[index].definition;
        }

        public int GetWeaponLevel(int index)
        {
            if (index < 0 || index >= _weaponCount) return 0;

            WeaponBase instance = _weapons[index].instance;
            return instance != null ? instance.Level : 0;
        }

        public PassiveDefinitionSO GetPassive(int index)
        {
            if (index < 0 || index >= _passiveCount) return null;
            return _passives[index].definition;
        }

        public int GetPassiveLevel(int index)
        {
            if (index < 0 || index >= _passiveCount) return 0;
            return _passives[index].level;
        }

        /// <summary>
        /// False when the weapon is null, has no ranks, is already maxed,
        /// the weapon bar is full, or a new weapon has no prefab.
        /// </summary>
        public bool CanAccept(WeaponDefinitionSO weapon)
        {
            if (weapon == null || weapon.MaxLevel <= 0) return false;

            int index = FindWeapon(weapon);
            if (index >= 0)
            {
                WeaponBase instance = _weapons[index].instance;
                return instance != null && !instance.IsMaxLevel;
            }

            return _weaponCount < MaxWeaponSlots && weapon.weaponPrefab != null;
        }

        /// <summary>
        /// False when the passive is null, has no ranks, is already maxed, or the passive bar is full.
        /// </summary>
        public bool CanAccept(PassiveDefinitionSO passive)
        {
            if (passive == null || passive.MaxLevel <= 0) return false;

            int index = FindPassive(passive);
            if (index >= 0)
                return _passives[index].level < passive.MaxLevel;

            return _passiveCount < MaxPassiveSlots;
        }

        // ── Apply ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds the weapon at level 1, or levels the owned copy.
        /// A new weapon is parented to the player and <see cref="WeaponBase.Initialize"/> is called.
        /// </summary>
        public bool Apply(WeaponDefinitionSO weapon)
        {
            if (!CanAccept(weapon)) return false;

            int index = FindWeapon(weapon);
            if (index >= 0)
            {
                _weapons[index].instance.LevelUp();
            }
            else
            {
                WeaponBase instance = SpawnWeapon(weapon, 1);
                RegisterWeapon(instance, weapon, ownedByLoadout: true);
            }

            NotifyChanged();
            return true;
        }

        /// <summary>
        /// Adds the passive at level 1, or raises it one rank.
        /// The previous modifiers from this asset are removed, then the new rank is applied.
        /// Ranks are absolute: level 2 replaces level 1, it does not stack on top of it.
        /// </summary>
        public bool Apply(PassiveDefinitionSO passive)
        {
            if (_stats == null || !CanAccept(passive)) return false;

            int index = FindPassive(passive);
            int level;

            if (index >= 0)
            {
                level = _passives[index].level + 1;
                _passives[index].level = level;
            }
            else
            {
                level = 1;
                _passives[_passiveCount++] = new PassiveSlot
                {
                    definition = passive,
                    level      = level
                };
            }

            ApplyPassiveModifiers(passive, level);
            NotifyChanged();
            return true;
        }

        // ── Round ─────────────────────────────────────────────────────────────

        private void OnRoundReset() => Rebuild();

        /// <summary>
        /// Drops every passive and every stat modifier, destroys weapons gained this round,
        /// and equips the starting weapon at level 1.
        /// </summary>
        private void Rebuild()
        {
            if (_stats == null) return;

            ClearWeapons();
            ClearPassives();
            EquipStartingWeapon();
            NotifyChanged();
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void ApplyPassiveModifiers(PassiveDefinitionSO passive, int level)
        {
            _stats.RemoveModifiersFrom(passive);

            PassiveLevelData data = passive.GetLevel(level);
            StatModifier[] modifiers = data != null ? data.modifiers : null;
            if (modifiers == null) return;

            for (int i = 0; i < modifiers.Length; i++)
                _stats.AddModifier(modifiers[i], passive);
        }

        private void EquipStartingWeapon()
        {
            WeaponDefinitionSO embeddedDefinition = _embeddedWeapon != null ? _embeddedWeapon.Definition : null;
            WeaponDefinitionSO starter = startingWeapon != null ? startingWeapon : embeddedDefinition;

            if (starter == null)
            {
                WarnMissingStarter();
                return;
            }

            // The player prefab already carries its starter (ProjectileWeapon on the root).
            // Reuse it so a second copy is not spawned on top.
            if (_embeddedWeapon != null && embeddedDefinition == starter)
            {
                _embeddedWeapon.enabled = true;
                _embeddedWeapon.Initialize(starter, 1);
                RegisterWeapon(_embeddedWeapon, starter, ownedByLoadout: false);
                return;
            }

            if (starter.weaponPrefab == null)
            {
                Debug.LogError($"[PlayerLoadout] '{starter.name}' has no weapon prefab assigned.", this);
                return;
            }

            if (_embeddedWeapon != null)
                _embeddedWeapon.enabled = false;

            WeaponBase instance = SpawnWeapon(starter, 1);
            RegisterWeapon(instance, starter, ownedByLoadout: true);
        }

        private WeaponBase SpawnWeapon(WeaponDefinitionSO weapon, int level)
        {
            WeaponBase instance = Instantiate(weapon.weaponPrefab, transform);
            instance.Initialize(weapon, level);
            return instance;
        }

        private void RegisterWeapon(WeaponBase instance, WeaponDefinitionSO definition, bool ownedByLoadout)
        {
            _weapons[_weaponCount++] = new WeaponSlot
            {
                definition     = definition,
                instance       = instance,
                ownedByLoadout = ownedByLoadout
            };
        }

        private void ClearWeapons()
        {
            for (int i = 0; i < _weaponCount; i++)
            {
                WeaponBase instance = _weapons[i].instance;
                bool owned = _weapons[i].ownedByLoadout;
                _weapons[i] = default;

                if (!owned || instance == null || instance == _embeddedWeapon) continue;

                instance.gameObject.SetActive(false);
                Destroy(instance.gameObject);
            }

            _weaponCount = 0;
        }

        private void ClearPassives()
        {
            for (int i = 0; i < _passiveCount; i++)
                _passives[i] = default;

            _passiveCount = 0;
            _stats.ClearAll();
        }

        private int FindWeapon(WeaponDefinitionSO definition)
        {
            if (definition == null) return -1;

            for (int i = 0; i < _weaponCount; i++)
            {
                if (_weapons[i].definition == definition)
                    return i;
            }

            return -1;
        }

        private int FindPassive(PassiveDefinitionSO definition)
        {
            if (definition == null) return -1;

            for (int i = 0; i < _passiveCount; i++)
            {
                if (_passives[i].definition == definition)
                    return i;
            }

            return -1;
        }

        private void WarnMissingStarter()
        {
            if (_missingStarterWarned) return;

            _missingStarterWarned = true;
            Debug.LogWarning("[PlayerLoadout] No starting weapon. Assign Starting Weapon, or leave a WeaponBase on the player.", this);
        }

        private void NotifyChanged() => OnLoadoutChanged?.Invoke();
    }
}
