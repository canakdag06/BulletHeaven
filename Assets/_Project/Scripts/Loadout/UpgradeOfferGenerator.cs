using System.Collections.Generic;
using UnityEngine;
using BulletHeaven.Progression;
using BulletHeaven.Weapons;

namespace BulletHeaven.Loadout
{
    /// <summary>
    /// Picks level-up cards from a pool. Always returns the requested number of offers;
    /// empty slots are heals so the picker cannot open with nothing to click.
    /// </summary>
    public class UpgradeOfferGenerator
    {
        private struct Candidate
        {
            public WeaponDefinitionSO Weapon;
            public PassiveDefinitionSO Passive;
            public int Weight;
        }

        public List<UpgradeOffer> Generate(int count, PlayerLoadout loadout, UpgradePoolSO pool)
        {
            var offers = new List<UpgradeOffer>(Mathf.Max(0, count));
            if (count <= 0) return offers;

            var candidates = new List<Candidate>();
            if (loadout != null && pool != null)
                Collect(candidates, loadout, pool);

            for (int i = 0; i < count; i++)
            {
                int index = PickIndex(candidates);
                if (index < 0)
                {
                    offers.Add(MakeFallback(pool));
                    continue;
                }

                offers.Add(ToOffer(candidates[index], loadout));
                candidates.RemoveAt(index);
            }

            return offers;
        }

        // ── Candidates ────────────────────────────────────────────────────────

        private static void Collect(List<Candidate> candidates, PlayerLoadout loadout, UpgradePoolSO pool)
        {
            WeightedWeapon[] weapons = pool.weapons;
            if (weapons != null)
            {
                for (int i = 0; i < weapons.Length; i++)
                {
                    WeightedWeapon entry = weapons[i];
                    if (entry == null) continue;
                    TryAddWeapon(candidates, loadout, entry.item, entry.weight);
                }
            }

            WeightedPassive[] passives = pool.passives;
            if (passives != null)
            {
                for (int i = 0; i < passives.Length; i++)
                {
                    WeightedPassive entry = passives[i];
                    if (entry == null) continue;
                    TryAddPassive(candidates, loadout, entry.item, entry.weight);
                }
            }
        }

        private static void TryAddWeapon(List<Candidate> candidates, PlayerLoadout loadout, WeaponDefinitionSO weapon, int weight)
        {
            if (weapon == null || weight <= 0 || !loadout.CanAccept(weapon)) return;
            if (ContainsWeapon(candidates, weapon)) return;

            candidates.Add(new Candidate
            {
                Weapon = weapon,
                Weight = weight
            });
        }

        private static void TryAddPassive(List<Candidate> candidates, PlayerLoadout loadout, PassiveDefinitionSO passive, int weight)
        {
            if (passive == null || weight <= 0 || !loadout.CanAccept(passive)) return;
            if (ContainsPassive(candidates, passive)) return;

            candidates.Add(new Candidate
            {
                Passive = passive,
                Weight = weight
            });
        }

        private static bool ContainsWeapon(List<Candidate> candidates, WeaponDefinitionSO weapon)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Weapon == weapon)
                    return true;
            }

            return false;
        }

        private static bool ContainsPassive(List<Candidate> candidates, PassiveDefinitionSO passive)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Passive == passive)
                    return true;
            }

            return false;
        }

        // Same walk as WaveStage.PickEnemy, then the caller removes that index.
        private static int PickIndex(List<Candidate> candidates)
        {
            int totalWeight = 0;
            for (int i = 0; i < candidates.Count; i++)
                totalWeight += candidates[i].Weight;

            if (totalWeight <= 0) return -1;

            int roll = Random.Range(0, totalWeight);
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= candidates[i].Weight;
                if (roll < 0) return i;
            }

            return candidates.Count - 1;
        }

        // ── Offers ────────────────────────────────────────────────────────────

        private static UpgradeOffer ToOffer(Candidate candidate, PlayerLoadout loadout)
        {
            if (candidate.Weapon != null)
                return FromWeapon(candidate.Weapon, loadout);

            return FromPassive(candidate.Passive, loadout);
        }

        private static UpgradeOffer FromWeapon(WeaponDefinitionSO weapon, PlayerLoadout loadout)
        {
            int current = loadout.GetLevel(weapon);
            return new UpgradeOffer
            {
                Weapon       = weapon,
                CurrentLevel = current,
                NextLevel    = current + 1,
                DisplayName  = Label(weapon.displayName, weapon.name),
                Icon         = weapon.icon,
                Description  = weapon.description ?? string.Empty
            };
        }

        private static UpgradeOffer FromPassive(PassiveDefinitionSO passive, PlayerLoadout loadout)
        {
            int current = loadout.GetLevel(passive);
            return new UpgradeOffer
            {
                Passive      = passive,
                CurrentLevel = current,
                NextLevel    = current + 1,
                DisplayName  = Label(passive.displayName, passive.name),
                Icon         = passive.icon,
                Description  = passive.description ?? string.Empty
            };
        }

        private static UpgradeOffer MakeFallback(UpgradePoolSO pool)
        {
            float fraction = pool != null ? pool.fallbackHealFraction : 0.3f;
            if (fraction <= 0f) fraction = 0.3f;

            string name = pool != null ? pool.fallbackName : null;
            string description = pool != null ? pool.fallbackDescription : null;

            return new UpgradeOffer
            {
                IsFallback   = true,
                HealFraction = Mathf.Clamp01(fraction),
                DisplayName  = string.IsNullOrEmpty(name) ? "Heal" : name,
                Description  = string.IsNullOrEmpty(description)
                    ? $"Restore {Mathf.RoundToInt(fraction * 100f)}% of max health."
                    : description,
                Icon = pool != null ? pool.fallbackIcon : null
            };
        }

        private static string Label(string displayName, string assetName)
        {
            return string.IsNullOrEmpty(displayName) ? assetName : displayName;
        }
    }
}
