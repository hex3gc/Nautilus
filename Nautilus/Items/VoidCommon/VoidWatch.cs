using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using System.Threading;
using RoR2.Items;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static VoidWatch VoidWatch = new VoidWatch
        (
            "VoidWatch",
            [ItemTag.Damage, ItemTag.CanBeTemporary],
            ItemTier.VoidTier1
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Collector's Appraisal gives you a reason to stay at high health still, but avoids the 'all or nothing' nature of watches by making it unbreakable
    ///     Does not corrupt broken watches, too powerful
    ///     Adds synergy with barrier as it's rare to have a reason to stack barrier items
    ///     // Ver.2
    ///     A more interactive alternative to the previous watch, instead working well with crowbars and large hits
    /// </summary>
    public class VoidWatch : ItemBase
    {
        public override bool Enabled => VoidWatch_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/FragileDamageBonus/FragileDamageBonus.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidSuppressor/matVoidSuppressorStone.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/DLC2/meridian/Assets/matPMGold.mat").WaitForCompletion();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/voidWatch.png");
        public BuffDef VoidWatchBuff;
        public static GameObject itemImpactEffect => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/MissileVoid/VoidImpactEffect.prefab").WaitForCompletion();

        public VoidWatch(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> VoidWatch_Enabled = new ConfigItem<bool>
        (
            "Void common: Collectors Appraisal",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> VoidWatch_Damagev2 = new ConfigItem<float>
        (
            "Void common: Collectors Appraisal",
            "Damage on first hit per buff",
            "Multiplies the first hit's damage this much each buff.",
            0.04f,
            0f,
            1f,
            0.01f
        );
        public static ConfigItem<float> VoidWatch_DamageStackv2 = new ConfigItem<float>
        (
            "Void common: Collectors Appraisal",
            "Damage on first hit per buff (Per stack)",
            "Multiplies the first hit's damage this much each buff, per additional stack.",
            0.04f,
            0f,
            1f,
            0.01f
        );
        public static ConfigItem<int> VoidWatch_MaxBuffsv2 = new ConfigItem<int>
        (
            "Void common: Collectors Appraisal",
            "Maximum buffs",
            "Maximum buffs a wearer can have.",
            12,
            1f,
            24f,
            1f
        );
        public static ConfigItem<bool> VoidWatch_Combat = new ConfigItem<bool>
        (
            "Void common: Collectors Appraisal",
            "Only work out of danger",
            "Enable to make the item use 'out of combat', which applies when the player hasn't attacked enemies, instead of 'out of danger'.",
            false
        );
        public static ConfigItem<bool> VoidWatch_Recipe = new ConfigItem<bool>
        (
            "Void common: Collectors Appraisal",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> VoidWatch_Ingredient1 = new ConfigItem<string>
        (
            "Void common: Collectors Appraisal",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "FragileDamageBonus"
        );
        public static ConfigItem<string> VoidWatch_Ingredient2 = new ConfigItem<string>
        (
            "Void common: Collectors Appraisal",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "BearVoid"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/voidWatch.prefab");

            Material[] materials =
            {
                material0,
                material1,
                material1,
            };
            ret.GetComponentInChildren<MeshRenderer>().SetMaterialArray(materials);

            return ret;
        }

        // Tokens
        public override void FormatDescriptionTokens()
        {
            string descriptionToken = ItemDef.descriptionToken;
            string pickupToken = ItemDef.pickupToken;
            string combatDanger = VoidWatch_Combat.Value == true ? "combat" : "danger";

            LanguageAPI.AddOverlay
            (
                descriptionToken,
                String.Format
                (
                    Language.currentLanguage.GetLocalizedStringByToken(descriptionToken),
                    VoidWatch_Damagev2.Value * 100f,
                    VoidWatch_DamageStackv2.Value * 100f,
                    VoidWatch_MaxBuffsv2.Value,
                    combatDanger
                )
            );

            LanguageAPI.AddOverlay
            (
                pickupToken,
                String.Format
                (
                    Language.currentLanguage.GetLocalizedStringByToken(pickupToken),
                    combatDanger
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            CreateVoidWatchBuff();

            // Damage boost on hit
            On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
            {
                if (!damageInfo.rejected && damageInfo.damage > 0f && damageInfo.procCoefficient > 0f && damageInfo.attacker && damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody) && self.body && attackerBody.TryGetComponent(out VoidWatchBehavior voidWatchBehavior))
                {
                    int itemCount = GetItemCountEffective(attackerBody);
                    int buffCount = attackerBody.GetBuffCount(VoidWatchBuff);
                    
                    if (itemCount > 0 && buffCount > 0)
                    {
                        if (buffCount > VoidWatch_MaxBuffsv2.Value / 2)
                        {
                            EffectData effectData2 = new EffectData()
                            {
                                origin = damageInfo.position
                            };
                            EffectManager.SpawnEffect(itemImpactEffect, effectData2, true);
                        }

                        float totalDamageBoost = buffCount * (VoidWatch_Damagev2.Value + (VoidWatch_DamageStackv2.Value * (itemCount - 1)));
                        damageInfo.damage += damageInfo.damage * totalDamageBoost;
                        attackerBody.SetBuffCount(VoidWatchBuff.buffIndex, 0);
                        attackerBody.RecalculateStats();
                    }
                }

                orig(self, damageInfo);
            };

            // Add/remove behavior on inventory change
            On.RoR2.CharacterBody.OnInventoryChanged += (orig, self) =>
            {
                orig(self);

                VoidWatchBehavior behavior = self.GetComponent<VoidWatchBehavior>();
                int itemCount = GetItemCountEffective(self);

                if (GetItemCountEffective(self) > 0 && !behavior)
                {
                    behavior = self.AddItemBehavior<VoidWatchBehavior>(1);
                    behavior.buffIndex = VoidWatchBuff.buffIndex;
                }

                if(GetItemCountEffective(self) <= 0 && behavior)
                {
                    UnityEngine.Object.Destroy(self.GetComponent<VoidWatchBehavior>());
                }
            };
        }
        
        // Recipes
        public override void AddCorruptionRecipe()
        {
            if (VoidWatch_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    VoidWatch_Ingredient1.Value,
                    VoidWatch_Ingredient2.Value,
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
                        childName = "LowerArmR",
                        localPos = new Vector3(-0.02457F, 0.30852F, -0.14102F),
                        localAngles = new Vector3(290.7905F, 230.8014F, 102.7876F),
                        localScale = new Vector3(0.31854F, 0.31854F, 0.31854F)
                    }
                }
            );
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmR",
                        localPos = new Vector3(-0.02457F, 0.30852F, -0.14102F),
                        localAngles = new Vector3(290.7905F, 230.8014F, 102.7876F),
                        localScale = new Vector3(0.31854F, 0.31854F, 0.31854F)
                    }
                }
            );
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmR",
                        localPos = new Vector3(-0.02457F, 0.30852F, -0.14102F),
                        localAngles = new Vector3(290.7905F, 230.8014F, 102.7876F),
                        localScale = new Vector3(0.31854F, 0.31854F, 0.31854F)
                    }
                }
            );
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmR",
                        localPos = new Vector3(-0.12958F, 3.40309F, 0.94518F),
                        localAngles = new Vector3(283.9275F, 84.74718F, 64.29515F),
                        localScale = new Vector3(1.56946F, 1.56946F, 1.56946F)
                    }
                }
            );
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmR",
                        localPos = new Vector3(-0.02457F, 0.30852F, -0.14102F),
                        localAngles = new Vector3(290.7905F, 230.8014F, 102.7876F),
                        localScale = new Vector3(0.31854F, 0.31854F, 0.31854F)
                    }
                }
            );
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmR",
                        localPos = new Vector3(0.0438F, 0.17509F, 0.18862F),
                        localAngles = new Vector3(291.6444F, 337.9544F, 190.9388F),
                        localScale = new Vector3(0.31854F, 0.31854F, 0.31854F)
                    }
                }
            );
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmR",
                        localPos = new Vector3(-0.02457F, 0.30852F, -0.14102F),
                        localAngles = new Vector3(290.7905F, 230.8014F, 102.7876F),
                        localScale = new Vector3(0.31854F, 0.31854F, 0.31854F)
                    }
                }
            );
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "MuzzleSyringe",
                        localPos = new Vector3(0.00801F, -0.09124F, 0.15755F),
                        localAngles = new Vector3(293.973F, 222.3054F, 112.2281F),
                        localScale = new Vector3(0.56994F, 0.56994F, 0.56994F)
                    }
                }
            );
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "MechLowerArmR",
                        localPos = new Vector3(0.01522F, 0.54796F, -0.24817F),
                        localAngles = new Vector3(292.765F, 219.4528F, 110.5262F),
                        localScale = new Vector3(0.31854F, 0.31854F, 0.31854F)
                    }
                }
            );
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmR",
                        localPos = new Vector3(1.43691F, 3.63456F, 1.00784F),
                        localAngles = new Vector3(296.4076F, 94.46941F, 107.0581F),
                        localScale = new Vector3(2.34403F, 2.34403F, 2.34403F)
                    }
                }
            );
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmR",
                        localPos = new Vector3(-0.03098F, 0.36814F, -0.13694F),
                        localAngles = new Vector3(290.7905F, 230.8014F, 102.7876F),
                        localScale = new Vector3(0.31854F, 0.31854F, 0.31854F)
                    }
                }
            );
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "GunBarrel",
                        localPos = new Vector3(-0.02104F, 0.90413F, -0.01548F),
                        localAngles = new Vector3(343.2699F, 343.8205F, 336.6695F),
                        localScale = new Vector3(0.36454F, 0.36454F, 0.36454F)
                    }
                }
            );
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "CalfL",
                        localPos = new Vector3(-0.01295F, 0.42512F, -0.14371F),
                        localAngles = new Vector3(290.7905F, 230.8014F, 102.7876F),
                        localScale = new Vector3(0.31854F, 0.31854F, 0.31854F)
                    }
                }
            );
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmR",
                        localPos = new Vector3(-0.0495F, 0.28059F, 0.16044F),
                        localAngles = new Vector3(76.02249F, 354.1454F, 334.8496F),
                        localScale = new Vector3(0.31854F, 0.31854F, 0.31854F)
                    }
                }
            );
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmR",
                        localPos = new Vector3(0.14945F, 0.60299F, 0.20539F),
                        localAngles = new Vector3(281.7685F, 48.24983F, 137.1102F),
                        localScale = new Vector3(0.41455F, 0.41455F, 0.41455F)
                    }
                }
            );
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmR",
                        localPos = new Vector3(-0.10782F, -0.07246F, -0.199F),
                        localAngles = new Vector3(21.05138F, 271.56F, 76.94494F),
                        localScale = new Vector3(0.3999F, 0.3999F, 0.3999F)
                    }
                }
            );
            rules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmL",
                        localPos = new Vector3(-0.15221F, 0.06868F, -0.03477F),
                        localAngles = new Vector3(340.3852F, 86.98999F, 300.8787F),
                        localScale = new Vector3(0.31854F, 0.31854F, 0.31854F)
                    }
                }
            );
            rules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "LowerArmR",
                        localPos = new Vector3(-0.28032F, -0.0148F, 0.13817F),
                        localAngles = new Vector3(349.3889F, 243.8624F, 234.1732F),
                        localScale = new Vector3(0.31854F, 0.31854F, 0.31854F)
                    }
                }
            );
            #endregion

            return rules;
        }

        public void CreateVoidWatchBuff()
        {
            BuffDef voidWatchBuff = ScriptableObject.CreateInstance<BuffDef>();
            voidWatchBuff.buffColor = new Color(1f, 1f, 1f);
            voidWatchBuff.canStack = true;
            voidWatchBuff.isDebuff = false;
            voidWatchBuff.ignoreGrowthNectar = false;
            voidWatchBuff.name = "Collector's Appraisal stacks";
            voidWatchBuff.isHidden = false;
            voidWatchBuff.isCooldown = false;
            voidWatchBuff.iconSprite = Main.Assets.LoadAsset<Sprite>("Assets/icons/voidWatchBuff.png");
            ContentAddition.AddBuffDef(voidWatchBuff);

            VoidWatchBuff = voidWatchBuff;
        }

        public class VoidWatchBehavior : CharacterBody.ItemBehavior
        {
            public BuffIndex buffIndex = BuffIndex.None;
            public float buffInterval = 1f;
            public float buffTimer = 0f;

            void FixedUpdate()
            {
                buffTimer += Time.fixedDeltaTime;
                
                int buffCount = body.GetBuffCount(buffIndex);

                if (buffTimer >= buffInterval && buffCount < VoidWatch_MaxBuffsv2.Value && (VoidWatch.VoidWatch_Combat.Value == false && body.outOfDanger) || (VoidWatch.VoidWatch_Combat.Value == true && body.outOfCombat))
                {
                    if (buffCount == VoidWatch_MaxBuffsv2.Value - 1)
                    {
                        Util.PlaySound("Play_bandit2_m1_reload_bullet", body.gameObject);
                    }

                    body.AddBuff(buffIndex);

                    buffTimer = 0f;
                }
            }
        }
    }
}