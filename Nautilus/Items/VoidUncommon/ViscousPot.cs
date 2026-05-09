using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using RoR2.Orbs;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static ViscousPot ViscousPot = new ViscousPot
        (
            "ViscousPot",
            [ItemTag.Healing, ItemTag.Damage],
            ItemTier.VoidTier2
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Defensive alternative to Luminous Shot. I wanted an option for reducing barrier decay, and it gives you somewhat of a reason to take it over luminous shot. Boosts void watch as well
    ///     // Ver.2
    ///     While still keeping the 'barrier green item' niche, I decided to make this one require a more aggressive approach to get the benefit, and have some damage utility
    /// </summary>
    public class ViscousPot : ItemBase
    {
        public override bool Enabled => ViscousPot_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC2/Items/IncreasePrimaryDamage/IncreasePrimaryDamage.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/EliteVoid/matVoidInfestorMetal.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Clay/matClayBubble.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/voidstage/matVoidAsteroid.mat").WaitForCompletion();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/viscousPot.png");
        private GameObject _gooExplodePrefab;
        public GameObject gooExplodePrefab
        {
            get
            {
                if (_gooExplodePrefab == null)
                {
                    _gooExplodePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/goolake/ClayGooOrbImpact.prefab").WaitForCompletion();
                }
                return _gooExplodePrefab;
            }
            set;
        }

        public ViscousPot(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> ViscousPot_Enabled = new ConfigItem<bool>
        (
            "Void uncommon: Viscous Pot",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> ViscousPot_DecayReduction = new ConfigItem<float>
        (
            "Void uncommon: Viscous Pot",
            "Barrier decay reduction",
            "Fraction for barrier decay reduction.",
            0.2f,
            0f,
            1f,
            0.05f
        );
        /*
        public static ConfigItem<float> ViscousPot_BarrierAddv2 = new ConfigItem<float>
        (
            "Void uncommon: Viscous Pot",
            "Barrier on hit",
            "Fraction of barrier added when a viscous orb hits an enemy.",
            0.05f,
            0f,
            1f,
            0.05f
        );
        */
        public static ConfigItem<float> ViscousPot_BarrierAddFlatv2 = new ConfigItem<float>
        (
            "Void uncommon: Viscous Pot",
            "Barrier on hit",
            "Flat amount of barrier added when a viscous orb hits an enemy.",
            15f,
            0f,
            50f,
            1f
        );
        public static ConfigItem<int> ViscousPot_OrbAmountv2 = new ConfigItem<int>
        (
            "Void uncommon: Viscous Pot",
            "Viscous orb amount",
            "Amount of orbs launched per secondary skill.",
            3,
            1f,
            6f,
            1f
        );
        public static ConfigItem<int> ViscousPot_OrbAmountStackv2 = new ConfigItem<int>
        (
            "Void uncommon: Viscous Pot",
            "Viscous orb amount (per stack)",
            "Amount of orbs launched per secondary skill, per additional stack.",
            1,
            1f,
            6f,
            1f
        );
        public static ConfigItem<float> ViscousPot_OrbRadiusv2 = new ConfigItem<float>
        (
            "Void uncommon: Viscous Pot",
            "Viscous orb radius",
            "Meters radius where enemies can be targeted by orbs.",
            20f,
            1f,
            60f,
            1f
        );
        public static ConfigItem<float> ViscousPot_OrbRadiusStackv2 = new ConfigItem<float>
        (
            "Void uncommon: Viscous Pot",
            "Viscous orb radius (per stack)",
            "Meters radius where enemies can be targeted by orbs, per additional stack.",
            8f,
            1f,
            60f,
            1f
        );
        public static ConfigItem<float> ViscousPot_OrbDamagev2 = new ConfigItem<float>
        (
            "Void uncommon: Viscous Pot",
            "Viscous orb damage",
            "Fractional damage from viscous orbs.",
            2f,
            0f,
            6f,
            0.1f
        );
        public static ConfigItem<float> ViscousPot_OrbProcCoefficient = new ConfigItem<float>
        (
            "Void uncommon: Viscous Pot",
            "Viscous orb proc coefficient",
            "Proc coefficient of viscous orbs.",
            0.2f,
            0f,
            1f,
            0.05f
        );
        public static ConfigItem<bool> ViscousPot_Recipe = new ConfigItem<bool>
        (
            "Void uncommon: Viscous Pot",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> ViscousPot_Ingredient1 = new ConfigItem<string>
        (
            "Void uncommon: Viscous Pot",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "IncreasePrimaryDamage"
        );
        public static ConfigItem<string> ViscousPot_Ingredient2 = new ConfigItem<string>
        (
            "Void uncommon: Viscous Pot",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "Wellies"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/viscousPot.prefab");

            Material[] materials =
            {
                material0,
                material1,
                material2
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
                    ViscousPot_DecayReduction.Value * 100f,
                    ViscousPot_OrbAmountv2.Value,
                    ViscousPot_OrbAmountStackv2.Value,
                    ViscousPot_OrbRadiusv2.Value,
                    ViscousPot_OrbRadiusStackv2.Value,
                    ViscousPot_OrbDamagev2.Value * 100f,
                    ViscousPot_BarrierAddFlatv2.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // Barrier decay reduction
            RecalculateStatsAPI.GetStatCoefficients += (orig, self) =>
            {
                int itemCount = GetItemCountEffective(orig);
                if (itemCount > 0)
                {
                    self.barrierDecayMult *= 1f - ViscousPot_DecayReduction.Value;
                }
            };

            // Orbs on skill
            On.RoR2.CharacterBody.OnSkillActivated += (orig, self, genericSkill) =>
            {
                orig(self, genericSkill);
                
                if (GetItemCountEffective(self) <= 0 || !self.healthComponent)
                {
                    return;
                }

                int itemCount = GetItemCountEffective(self);

                if (self.bodyIndex == BodyCatalog.SpecialCases.RailGunner())
                {
                    if ((object)self.skillLocator.primary == genericSkill && self.canAddIncrasePrimaryDamage)
                    {
                        FireOrbs(self, itemCount);
                    }
                }
                else if ((genericSkill.skillDef.autoHandleLuminousShot || self.canAddIncrasePrimaryDamage) && (object)self.skillLocator.secondary == genericSkill)
                {
                    FireOrbs(self, itemCount);
                }
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (ViscousPot_Recipe.Value == true /*&& ItemInit.Wellies.Enabled*/)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    ViscousPot_Ingredient1.Value,
                    ViscousPot_Ingredient2.Value,
                    ItemDef.name
                );
            }
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
                        localPos = new Vector3(0.23243F, -0.01495F, 0.10609F),
                        localAngles = new Vector3(0F, 0F, 181.5702F),
                        localScale = new Vector3(0.73856F, 0.73856F, 0.73856F)
                    }
                }
            );
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "FootR",
                        localPos = new Vector3(0F, 0.09763F, -0.07923F),
                        localAngles = new Vector3(274.1588F, 0.00001F, 2.5506F),
                        localScale = new Vector3(1.30692F, 1.30692F, 1.30692F)
                    }
                }
            );
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.24255F, -0.00993F, -0.03103F),
                        localAngles = new Vector3(0F, 0F, 177.32F),
                        localScale = new Vector3(0.76163F, 0.76163F, 0.76163F)
                    }
                }
            );
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0F, 1.86914F, 0.01515F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(6.73994F, 6.73994F, 6.73994F)
                    }
                }
            );
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(-0.30395F, 0.13843F, -0.00856F),
                        localAngles = new Vector3(0.82139F, 180F, 180F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "FootL",
                        localPos = new Vector3(0.00051F, 0.05496F, 0.03331F),
                        localAngles = new Vector3(315.4312F, 146.2849F, 210.6754F),
                        localScale = new Vector3(1.08864F, 1.08864F, 1.08864F)
                    }
                }
            );
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0F, 0.1795F, -0.00001F),
                        localAngles = new Vector3(0F, 0F, 180F),
                        localScale = new Vector3(1.67378F, 1.67378F, 1.67378F)
                    }
                }
            );
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "PlatformBase",
                        localPos = new Vector3(-0.76272F, -0.08788F, -0.30847F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1.65139F, 1.65139F, 1.65139F)
                    }
                }
            );
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0F, 0.13543F, 0.29333F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1.10422F, 1.10422F, 1.10422F)
                    }
                }
            );
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "FootR",
                        localPos = new Vector3(0.00001F, 0.50056F, -1.04024F),
                        localAngles = new Vector3(348.8859F, 180F, 180F),
                        localScale = new Vector3(11.03721F, 11.03721F, 14.89955F)
                    }
                }
            );
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.29921F, -0.10334F, -0.19508F),
                        localAngles = new Vector3(0F, 0F, 180.8385F),
                        localScale = new Vector3(1.1213F, 1.1213F, 1.1213F)
                    }
                }
            );
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "GunRoot",
                        localPos = new Vector3(0F, -0.56949F, -0.12557F),
                        localAngles = new Vector3(359.8944F, 180F, 180F),
                        localScale = new Vector3(0.63613F, 0.63613F, 0.63613F)
                    }
                }
            );
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.0038F, 0.07978F, 0F),
                        localAngles = new Vector3(0F, 0F, 180.4983F),
                        localScale = new Vector3(1.52951F, 1.52951F, 1.52951F)
                    }
                }
            );
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pack",
                        localPos = new Vector3(-0.31313F, 0.21783F, -0.22959F),
                        localAngles = new Vector3(0F, 0F, 47.0003F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "HandR",
                        localPos = new Vector3(-0.00001F, 0.29194F, -0.04805F),
                        localAngles = new Vector3(3.72109F, 180F, 180F),
                        localScale = new Vector3(1.63274F, 1.63274F, 1.63274F)
                    }
                }
            );
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.09855F, 0.29608F, 0.00934F),
                        localAngles = new Vector3(0F, 0F, 98.57596F),
                        localScale = new Vector3(1.57669F, 1.57669F, 1.57669F)
                    }
                }
            );
            rules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.36535F, -0.02339F, 0F),
                        localAngles = new Vector3(0F, 0F, 271.7016F),
                        localScale = new Vector3(1.11821F, 1.11821F, 1.11821F)
                    }
                }
            );
            rules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "BagPocketL",
                        localPos = new Vector3(-0.22659F, -0.02006F, 0.0012F),
                        localAngles = new Vector3(86.2076F, 25.61749F, 25.6665F),
                        localScale = new Vector3(1.24465F, 1.24465F, 1.24465F)
                    }
                }
            );
            #endregion

            return rules;
        }

        public void FireOrbs(CharacterBody body, int itemCount)
        {
            List<Collider> colliders = Physics.OverlapSphere(body.corePosition, ViscousPot_OrbRadiusv2.Value + (ViscousPot_OrbRadiusStackv2.Value * (itemCount - 1))).ToList();
            Util.ShuffleList(colliders);
            
            int orbCount = ViscousPot_OrbAmountv2.Value + (ViscousPot_OrbAmountStackv2.Value * (itemCount - 1));
            bool exploded = false;

            foreach(Collider collider in colliders)
            {
                if (orbCount <= 0)
                {
                    break;
                }

                GameObject gameObject = collider.gameObject;
                if (gameObject.GetComponentInChildren<CharacterBody>())
                {
                    CharacterBody colliderBody = gameObject.GetComponentInChildren<CharacterBody>();
                    if (colliderBody.healthComponent && colliderBody.healthComponent.health > 0f && colliderBody.teamComponent && colliderBody.teamComponent.teamIndex != body.teamComponent.teamIndex)
                    {
                        if (!exploded)
                        {
                            EffectData effectData = new EffectData()
                            {
                                origin = body.corePosition
                            };
                            EffectManager.SpawnEffect(gooExplodePrefab, effectData, true);
                            
                            exploded = true;
                        }

                        ViscousPotOrb viscousPotOrb = new ViscousPotOrb();
                        viscousPotOrb.attacker = body.gameObject;
                        viscousPotOrb.target = colliderBody.mainHurtBox;
                        viscousPotOrb.teamIndex = body.teamComponent.teamIndex;
                        viscousPotOrb.origin = body.corePosition;

                        OrbManager.instance.AddOrb(viscousPotOrb);

                        orbCount--;
                    }
                }
            }
        }

        public class ViscousPotOrb : RoR2.Orbs.SquidOrb
        {
            public override void OnArrival()
            {
                if (!target)
                {
                    return;
                }

                CharacterBody attackerBody = attacker.GetComponent<CharacterBody>();

                if (target.healthComponent && attackerBody.healthComponent && attackerBody.teamComponent && target.teamIndex != attackerBody.teamComponent.teamIndex)
                {
                    DamageInfo damageInfo = new DamageInfo();
                    damageInfo.damage = attackerBody.damage * ViscousPot_OrbDamagev2.Value;
                    damageInfo.attacker = attacker;
                    damageInfo.inflictor = null;
                    damageInfo.force = Vector3.zero;
                    damageInfo.crit = false;
                    damageInfo.procChainMask = procChainMask;
                    damageInfo.procCoefficient = ViscousPot_OrbProcCoefficient.Value;
                    damageInfo.position = target.transform.position;
                    damageInfo.damageColorIndex = DamageColorIndex.Void;
                    damageInfo.damageType = damageType;
                    damageInfo.inflictedHurtbox = target;
                    target.healthComponent.TakeDamage(damageInfo);
                    GlobalEventManager.instance.OnHitEnemy(damageInfo, target.healthComponent.gameObject);
                    GlobalEventManager.instance.OnHitAll(damageInfo, target.healthComponent.gameObject);

                    attackerBody.healthComponent.AddBarrier(ViscousPot_BarrierAddFlatv2.Value);
                }
            }
        }
    }
}