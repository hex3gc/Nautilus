using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using RoR2.Orbs;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static Xenobacteria Xenobacteria = new Xenobacteria
        (
            "Xenobacteria",
            [ItemTag.Damage, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.ExtractorUnitBlacklist, ItemTag.CanBeTemporary],
            ItemTier.VoidTier3
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Alien Head's flat stat increase with a twist; more of an attack speed boost as Droneman, but applies to ALL minions
    ///     I wanted another attack speed item but didn't want to void the syringe
    ///     // Ver.2
    ///     I've decided to reverse the effect in a different way by making it slow enemies' cooldowns rather than speeding up your own
    ///     This also adds more ways to apply poison or blight, which can help with the new Dreadleech, Death Mark or Noxious Thorns. It's also more thematic to being a xenobacteria
    /// </summary>
    public class Xenobacteria : ItemBase
    {
        public override bool Enabled => Xenobacteria_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/AlienHead/AlienHead.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/gauntlets/matGTVoidTerrain.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/PortalVoid/matPortalVoidCenter.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/Common/Void/matNullifierFracturePortalFull.mat").WaitForCompletion();
        public Material material3 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/GameModes/InfiniteTowerRun/ITAssets/matVoidWhale.mat").WaitForCompletion();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/xenobacteria.png");

        public Xenobacteria(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> Xenobacteria_Enabled = new ConfigItem<bool>
        (
            "Void legendary: Xenobacteria",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<int> Xenobacteria_BlightChance = new ConfigItem<int>
        (
            "Void legendary: Xenobacteria",
            "Blight chance",
            "Percent chance that blight will be applied instead of a debuff. These chance values must add up to a value below 100.",
            20,
            1f,
            100f,
            1f
        );
        public static ConfigItem<int> Xenobacteria_PoisonChance = new ConfigItem<int>
        (
            "Void legendary: Xenobacteria",
            "Poison chance",
            "Percent chance that poison will be applied instead of a debuff. These chance values must add up to a value below 100.",
            5,
            1f,
            100f,
            1f
        );
        public static ConfigItem<int> Xenobacteria_NothingChance = new ConfigItem<int>
        (
            "Void legendary: Xenobacteria",
            "Nothing chance",
            "Percent chance that a debuff will not apply at all. These chance values must add up to a value below 100.",
            50,
            1f,
            100f,
            1f
        );
        public static ConfigItem<float> Xenobacteria_CooldownSpeedReduction = new ConfigItem<float>
        (
            "Void legendary: Xenobacteria",
            "Cooldown and attack speed reduction",
            "Fractional cooldown/attack speed reduction by poison and blight effects on enemies.",
            0.40f,
            0.05f,
            0.80f,
            0.05f
        );
        public static ConfigItem<bool> Xenobacteria_Recipe = new ConfigItem<bool>
        (
            "Void legendary: Xenobacteria",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> Xenobacteria_Ingredient1 = new ConfigItem<string>
        (
            "Void legendary: Xenobacteria",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "AlienHead"
        );
        public static ConfigItem<string> Xenobacteria_Ingredient2 = new ConfigItem<string>
        (
            "Void legendary: Xenobacteria",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "SlowOnHitVoid"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/xenobacteria.prefab");

            Material[] materials =
            {
                material0,
                material1,
                material2,
                material3
            };
            ret.GetComponentInChildren<MeshRenderer>().SetMaterialArray(materials);

            return ret;
        }

        // Tokens
        public override void FormatDescriptionTokens()
        {
            string descriptionToken = ItemDef.descriptionToken;

            LanguageAPI.AddOverlay
            (
                descriptionToken,
                String.Format
                (
                    Language.currentLanguage.GetLocalizedStringByToken(descriptionToken),
                    Xenobacteria_PoisonChance.Value,
                    Xenobacteria_BlightChance.Value,
                    Xenobacteria_CooldownSpeedReduction.Value * 100f,
                    Xenobacteria_NothingChance.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // Alt DOTs
            On.RoR2.DotController.InflictDot_refInflictDotInfo += (orig, ref self) =>
            {
                CharacterBody attackerBody = self.attackerObject?.GetComponent<CharacterBody>();
                CharacterBody victimBody = self.victimObject?.GetComponent<CharacterBody>();
                bool applied = false;

                if (attackerBody && victimBody && GetItemCountEffective(attackerBody) > 0 && attackerBody.teamComponent?.teamIndex != victimBody.teamComponent?.teamIndex)
                {
                    int rollResult = Run.instance.runRNG.RangeInt(1, 100);

                    int poison = Xenobacteria_PoisonChance.Value;
                    int blight = Xenobacteria_BlightChance.Value;
                    int nothing = Xenobacteria_NothingChance.Value;
                    
                    // Priority rolling reduces chance for nothing
                    if (rollResult <= poison)
                    {
                        self.dotIndex = DotController.DotIndex.Poison;
                        self.duration = 10f;
                        self.damageMultiplier = 1f;
                        self.maxStacksFromAttacker = null;
                        self.totalDamage = null;
                        self.preUpgradeDotIndex = null;
                        applied = true;
                    }
                    else if (rollResult <= poison + blight)
                    {
                        self.dotIndex = DotController.DotIndex.Blight;
                        self.duration = 5f;
                        self.damageMultiplier = 1f;
                        self.maxStacksFromAttacker = null;
                        self.totalDamage = null;
                        self.preUpgradeDotIndex = null;
                        applied = true;
                    }
                    else if (rollResult <= poison + blight + nothing)
                    {
                        return;
                    }
                }

                orig(ref self);

                if (applied)
                {
                    victimBody.RecalculateStats();
                }
            };

            // Reduce speed by amount of Xenobacterias
            RecalculateStatsAPI.GetStatCoefficients += (orig, self) =>
            {
                if (orig.HasBuff(RoR2Content.Buffs.Poisoned) || orig.HasBuff(RoR2Content.Buffs.Blight) && orig.teamComponent && orig.teamComponent.teamIndex != TeamIndex.Player)
                {
                    float slowMult = Xenobacteria_CooldownSpeedReduction.Value * GetBacteriasInTeam(TeamIndex.Player);

                    if (slowMult != 0f)
                    {
                        self.attackSpeedReductionMultAdd += slowMult;
                        self.allSkills.cooldownMultAdd += slowMult;
                    }
                }
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (Xenobacteria_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    Xenobacteria_Ingredient1.Value,
                    Xenobacteria_Ingredient2.Value,
                    ItemDef.name
                );
            }
        }

        public int GetBacteriasInTeam(TeamIndex teamIndex)
        {
            int ret = 0;

            foreach (TeamComponent teamComponent in TeamComponent.GetTeamMembers(teamIndex))
            {
                if (teamComponent.body)
                {
                    ret += GetItemCountEffective(teamComponent.body);
                }
            }

            return ret;
        }

        // IDR
        public override ItemDisplayRuleDict AddItemDisplays()
        {
            GameObject ItemDisplayPrefab = Helpers.PrepareItemDisplayModel(PrefabAPI.InstantiateClone(itemPrefab, ItemDef.name + "Display", false));
            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();

            #region IDR
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.07364F, -0.05085F, -0.11275F),
                        localAngles = new Vector3(357.3029F, 358.9558F, 317.6373F),
                        localScale = new Vector3(0.51172F, 0.51172F, 0.51172F)
                    }
                }
            );
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "UpperArmR",
                        localPos = new Vector3(-0.00001F, 0.18977F, -0.06835F),
                        localAngles = new Vector3(0F, 0F, 319.5492F),
                        localScale = new Vector3(0.54849F, 0.54849F, 0.54849F)
                    }
                }
            );
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "MainWeapon",
                        localPos = new Vector3(0.01503F, 0.89177F, -0.00008F),
                        localAngles = new Vector3(0F, 0F, 318.5866F),
                        localScale = new Vector3(0.71667F, 0.71667F, 0.71667F)
                    }
                }
            );
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmL",
                        localPos = new Vector3(0.00003F, 2.40504F, -1.17552F),
                        localAngles = new Vector3(0F, 0F, 318.2521F),
                        localScale = new Vector3(5.68482F, 5.68482F, 5.68482F)
                    }
                }
            );
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.20486F, -0.00005F, -0.13054F),
                        localAngles = new Vector3(0F, 0F, 317.0949F),
                        localScale = new Vector3(0.69812F, 0.69812F, 0.69812F)
                    }
                }
            );
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.12277F, -0.00005F, -0.14566F),
                        localAngles = new Vector3(8.71969F, 352.541F, 317.5491F),
                        localScale = new Vector3(0.55226F, 0.55226F, 0.55226F)
                    }
                }
            );
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.16835F, 0.00342F, -0.12026F),
                        localAngles = new Vector3(354.1508F, 359.1483F, 316.7882F),
                        localScale = new Vector3(0.49168F, 0.49168F, 0.49168F)
                    }
                }
            );
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "FootFrontR",
                        localPos = new Vector3(0.00008F, 0.65419F, -0.11727F),
                        localAngles = new Vector3(0.69541F, 0.05984F, 318.4476F),
                        localScale = new Vector3(1.15644F, 1.15644F, 1.15644F)
                    }
                }
            );
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(-0.00107F, 0.04313F, -0.17652F),
                        localAngles = new Vector3(0F, 0F, 48.43555F),
                        localScale = new Vector3(0.69244F, 0.69244F, 0.69244F)
                    }
                }
            );
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "ThighR",
                        localPos = new Vector3(-1.48719F, 1.0463F, -0.00015F),
                        localAngles = new Vector3(0F, 0F, 319.1104F),
                        localScale = new Vector3(5.53285F, 5.53285F, 5.53285F)
                    }
                }
            );
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmR",
                        localPos = new Vector3(-0.01564F, 0.22502F, -0.10381F),
                        localAngles = new Vector3(0F, 0F, 313.7314F),
                        localScale = new Vector3(0.7152F, 0.7152F, 0.7152F)
                    }
                }
            );
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "GunStock",
                        localPos = new Vector3(0.00109F, -0.26965F, 0.08865F),
                        localAngles = new Vector3(0F, 0F, 318.2469F),
                        localScale = new Vector3(0.46821F, 0.46821F, 0.46821F)
                    }
                }
            );
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(-0.01221F, 0.23783F, -0.32102F),
                        localAngles = new Vector3(282.419F, 355.2956F, 323.103F),
                        localScale = new Vector3(0.62956F, 0.62956F, 0.62956F)
                    }
                }
            );
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pack",
                        localPos = new Vector3(-0.18136F, -0.23143F, -0.11369F),
                        localAngles = new Vector3(354.0385F, 358.4314F, 0.37207F),
                        localScale = new Vector3(0.75967F, 0.75967F, 0.75967F)
                    }
                }
            );
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmL",
                        localPos = new Vector3(0.12795F, 0.27383F, -0.19765F),
                        localAngles = new Vector3(359.8161F, 20.3385F, 315.2839F),
                        localScale = new Vector3(0.97651F, 0.97651F, 0.97651F)
                    }
                }
            );
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(-0.10797F, 0F, 0.2276F),
                        localAngles = new Vector3(9.65744F, 13.54182F, 330.8636F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Backpack",
                        localPos = new Vector3(0.21193F, -0.04826F, -0.16722F),
                        localAngles = new Vector3(0F, 0F, 319.1347F),
                        localScale = new Vector3(0.82088F, 0.82088F, 0.82088F)
                    }
                }
            );
            rules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "BagPocketL",
                        localPos = new Vector3(-0.0683F, -0.02792F, -0.25256F),
                        localAngles = new Vector3(0F, 0F, 321.2156F),
                        localScale = new Vector3(0.77832F, 0.77832F, 0.77832F)
                    }
                }
            );
            #endregion

            return rules;
        }
    }
}