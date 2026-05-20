using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static CoralCrust CoralCrust = new CoralCrust
        (
            "CoralCrust",
            [ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.ExtractorUnitBlacklist, ItemTag.BrotherBlacklist, ItemTag.CanBeTemporary],
            ItemTier.VoidTier1
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Coral Crust makes bossfights less threatening, which can really help with final bosses who do big hits like Mithrix
    ///     Provides a simple choice between defense and offense, depending on what you need
    /// </summary>
    public class CoralCrust : ItemBase
    {
        public override bool Enabled => CoralCrust_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/BossDamageBonus/BossDamageBonus.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/voidstage/matVoidCoral.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/TreasureCacheVoid/matKeyVoid.mat").WaitForCompletion();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/coralCrust.png");
        public BuffDef CoralCrustBuff;
        private GameObject _blockPrefab;
        public GameObject BlockPrefab
        {
            get
            {
                if (_blockPrefab == null)
                {
                    _blockPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/GummyClone/GummyCloneExplosion.prefab").WaitForCompletion();
                }
                return _blockPrefab;
            }
            set;
        }

        public CoralCrust(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) :
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden)
        { }

        // Config
        public static ConfigItem<bool> CoralCrust_Enabled = new ConfigItem<bool>
        (
            "Void common: Coral Crust",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<int> CoralCrust_HitsEffective = new ConfigItem<int>
        (
            "Void common: Coral Crust",
            "Hits reduced per stack",
            "Number of hits that are reduced in damage per stack.",
            10,
            1f,
            30f,
            1f
        );
        public static ConfigItem<float> CoralCrust_DamageReduction = new ConfigItem<float>
        (
            "Void common: Coral Crust",
            "Damage reduction",
            "Fraction of damage reduced when blocked by Coral Crust.",
            0.5f,
            0.01f,
            0.99f,
            0.01f
        );
        public static ConfigItem<bool> CoralCrust_Recipe = new ConfigItem<bool>
        (
            "Void common: Coral Crust",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> CoralCrust_Ingredient1 = new ConfigItem<string>
        (
            "Void common: Coral Crust",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "BossDamageBonus"
        );
        public static ConfigItem<string> CoralCrust_Ingredient2 = new ConfigItem<string>
        (
            "Void common: Coral Crust",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "BearVoid"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/coralCrust.prefab");

            Material[] materials =
            {
                material0,
                material1
            };
            ret.GetComponentInChildren<MeshRenderer>().SetMaterialArray(materials);

            return ret;
        }


        // Tokens
        public override void FormatDescriptionTokens()
        {
            string descriptionToken = ItemDef.descriptionToken;
            string pickupToken = ItemDef.pickupToken;

            LanguageAPI.AddOverlay
            (
                descriptionToken,
                String.Format
                (
                    Language.currentLanguage.GetLocalizedStringByToken(descriptionToken),
                    CoralCrust_HitsEffective.Value,
                    CoralCrust_DamageReduction.Value * 100f
                )
            );

            LanguageAPI.AddOverlay
            (
                pickupToken,
                String.Format
                (
                    Language.currentLanguage.GetLocalizedStringByToken(pickupToken),
                    CoralCrust_HitsEffective.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            CreateCoralCrustBuff();

            // Add/remove behavior on inventory change
            On.RoR2.CharacterBody.OnInventoryChanged += (orig, self) =>
            {
                orig(self);

                CoralCrustBehavior behavior = self.GetComponent<CoralCrustBehavior>();
                int itemCount = GetItemCountEffective(self);

                if (GetItemCountEffective(self) > 0 && !behavior)
                {
                    behavior = self.AddItemBehavior<CoralCrustBehavior>(itemCount);
                }

                if (behavior)
                {
                    behavior.stack = itemCount;
                    behavior.UpdateCharges();
                }

                if (GetItemCountEffective(self) <= 0 && behavior)
                {
                    UnityEngine.Object.Destroy(self.GetComponent<CoralCrustBehavior>());
                }
            };

            // Taking damage from a boss
            On.RoR2.HealthComponent.TakeDamageProcess += (orig, self, damageInfo) =>
            {
                if (damageInfo.attacker && self.body && !damageInfo.rejected && damageInfo.procCoefficient > 0f)
                {
                    CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    CoralCrustBehavior coralCrustBehavior = self.body.GetComponent<CoralCrustBehavior>();

                    if (attackerBody && attackerBody.isBoss && coralCrustBehavior)
                    {
                        bool reduceDamage = coralCrustBehavior.TryConsumecharge();

                        if (reduceDamage)
                        {
                            damageInfo.damage *= CoralCrust_DamageReduction.Value;
                        }
                    }
                }

                orig(self, damageInfo);
            };
        }

        // Recipes
        public override void AddCorruptionRecipe()
        {
            if (CoralCrust_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    CoralCrust_Ingredient1.Value,
                    CoralCrust_Ingredient2.Value,
                    ItemDef.name
                );
            }
        }

        public void CreateCoralCrustBuff()
        {
            BuffDef coralCrustBuff = ScriptableObject.CreateInstance<BuffDef>();
            coralCrustBuff.buffColor = new Color(1f, 1f, 1f);
            coralCrustBuff.canStack = true;
            coralCrustBuff.isDebuff = false;
            coralCrustBuff.ignoreGrowthNectar = true;
            coralCrustBuff.name = "Coral Crust charges";
            coralCrustBuff.isHidden = false;
            coralCrustBuff.isCooldown = false;
            coralCrustBuff.iconSprite = Main.Assets.LoadAsset<Sprite>("Assets/icons/coralCrustBuff.png");
            ContentAddition.AddBuffDef(coralCrustBuff);

            CoralCrustBuff = coralCrustBuff;
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
                        childName = "UpperArmR",
                        localPos = new Vector3(-0.10331F, 0.04841F, -0.02772F),
                        localAngles = new Vector3(6.57383F, 76.07455F, 75.05128F),
                        localScale = new Vector3(0.49954F, 0.49954F, 0.49954F)
                    }
                }
            );
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "UpperArmR",
                        localPos = new Vector3(0.02079F, 0.13803F, -0.06908F),
                        localAngles = new Vector3(38.41159F, 47.549F, 179.556F),
                        localScale = new Vector3(0.48282F, 0.48282F, 0.48282F)
                    }
                }
            );
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Hat",
                        localPos = new Vector3(0.11397F, 0.02804F, -0.00392F),
                        localAngles = new Vector3(0F, 234.9308F, 0F),
                        localScale = new Vector3(0.39902F, 0.39902F, 0.39902F)
                    }
                }
            );
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "UpperArmR",
                        localPos = new Vector3(0.36072F, -0.00009F, 0.00016F),
                        localAngles = new Vector3(14.30757F, 310.4308F, 196.5085F),
                        localScale = new Vector3(3.29348F, 3.29348F, 3.29348F)
                    }
                }
            );
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0.02969F, 0.1201F, 0.28754F),
                        localAngles = new Vector3(346.5579F, 166.9641F, 0F),
                        localScale = new Vector3(0.66077F, 0.66077F, 0.66077F)
                    }
                }
            );
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmL",
                        localPos = new Vector3(0.00001F, 0.08641F, -0.00002F),
                        localAngles = new Vector3(0F, 330.4417F, 0F),
                        localScale = new Vector3(0.74697F, 0.74697F, 0.74697F)
                    }
                }
            );
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "SwordBase",
                        localPos = new Vector3(-0.01503F, 0.25924F, -0.05364F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(0.73333F, 0.73333F, 0.73333F)
                    }
                }
            );
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "PlatformBase",
                        localPos = new Vector3(-0.69528F, 0.31934F, 0.63796F),
                        localAngles = new Vector3(0F, 100.9973F, 0F),
                        localScale = new Vector3(1.7507F, 1.7507F, 1.7507F)
                    }
                }
            );
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.13373F, 0.17547F, 0.00822F),
                        localAngles = new Vector3(0F, 246.1764F, 0F),
                        localScale = new Vector3(0.48557F, 0.48557F, 0.48557F)
                    }
                }
            );
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "UpperArmR",
                        localPos = new Vector3(1.70797F, -0.10058F, 0.57285F),
                        localAngles = new Vector3(22.18276F, 294.0827F, 194.6708F),
                        localScale = new Vector3(4.8187F, 4.8187F, 4.8187F)
                    }
                }
            );
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.01168F, 0.15742F, 0.17112F),
                        localAngles = new Vector3(0F, 146.5373F, 0F),
                        localScale = new Vector3(0.32628F, 0.32628F, 0.32628F)
                    }
                }
            );
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.11671F, 0.13387F, -0.00919F),
                        localAngles = new Vector3(0F, 238.9204F, 0F),
                        localScale = new Vector3(0.32914F, 0.32914F, 0.32914F)
                    }
                }
            );
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "ShoulderR",
                        localPos = new Vector3(-0.01288F, 0.52347F, -0.01986F),
                        localAngles = new Vector3(60.75994F, 13.39671F, 290.2954F),
                        localScale = new Vector3(0.62347F, 0.62347F, 0.62347F)
                    }
                }
            );
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "UpperArmL",
                        localPos = new Vector3(0.01106F, 0.04018F, -0.07887F),
                        localAngles = new Vector3(23.75951F, 17.71719F, 175.2518F),
                        localScale = new Vector3(0.55782F, 0.55782F, 0.55782F)
                    }
                }
            );
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.01632F, 0.36753F, -0.13048F),
                        localAngles = new Vector3(338.1574F, 318.8398F, 4.71709F),
                        localScale = new Vector3(1.27945F, 1.27945F, 1.27945F)
                    }
                }
            );
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "UpperArmL",
                        localPos = new Vector3(-0.13727F, 0.03837F, -0.12717F),
                        localAngles = new Vector3(37.17414F, 303.6869F, 43.23228F),
                        localScale = new Vector3(1.01146F, 1.01146F, 1.01146F)
                    }
                }
            );
            rules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.17314F, 0.03344F, -0.05624F),
                        localAngles = new Vector3(59.26595F, 180F, 180F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            #endregion

            return rules;
        }
    }

    public class CoralCrustBehavior : CharacterBody.ItemBehavior
    {
        private BuffDef buffDef;
        private int chargesConsumed;
        
        void Awake()
        {
            buffDef = ItemInit.CoralCrust.CoralCrustBuff;
        }

        public bool TryConsumecharge()
        {
            bool ret = false;

            if (chargesConsumed < stack * CoralCrust.CoralCrust_HitsEffective.Value)
            {
                EffectData effectData = new EffectData()
                {
                    origin = body.corePosition,
                    scale = 1f
                };
                EffectManager.SpawnEffect(ItemInit.CoralCrust.BlockPrefab, effectData, true);

                chargesConsumed++;
                ret = true;
                UpdateCharges();
            }

            return ret;
        }

        public void UpdateCharges()
        {
            body.SetBuffCount(buffDef.buffIndex, (stack * CoralCrust.CoralCrust_HitsEffective.Value) - chargesConsumed);
        }
    }
}