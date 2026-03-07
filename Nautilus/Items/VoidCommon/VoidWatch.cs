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
            [ItemTag.Damage],
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
        /*
        public static ConfigItem<float> VoidWatch_Damage = new ConfigItem<float>
        (
            "Void common: Collectors Appraisal",
            "Damage boost",
            "Grants a damage boost with this multiplier.",
            0.10f,
            0f,
            1f,
            0.05f
        );
        public static ConfigItem<float> VoidWatch_DamageStack = new ConfigItem<float>
        (
            "Void common: Collectors Appraisal",
            "Damage boost (Per stack)",
            "Grants a damage boost with this multiplier per additional stack.",
            0.10f,
            0f,
            1f,
            0.05f
        );
        public static ConfigItem<float> VoidWatch_HealthThreshold = new ConfigItem<float>
        (
            "Void common: Collectors Appraisal",
            "Damage health threshold",
            "Health must be above this fraction for the damage boost to apply.",
            0.9f,
            0f,
            1f,
            0.05f
        );
        public static ConfigItem<bool> VoidWatch_BarrierMult = new ConfigItem<bool>
        (
            "Void common: Collectors Appraisal",
            "Damage scaling with barrier",
            "Adds up to 2x multiplier to the damage bonus scaling with barrier.",
            true
        );
        public static ConfigItem<bool> VoidWatch_HideBuff = new ConfigItem<bool>
        (
            "Void common: Collectors Appraisal",
            "Hide buff",
            "Do not display the buff for this item.",
            false
        );
        */

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

            LanguageAPI.AddOverlay
            (
                descriptionToken,
                String.Format
                (
                    Language.currentLanguage.GetLocalizedStringByToken(descriptionToken),
                    VoidWatch_Damagev2.Value * 100f,
                    VoidWatch_DamageStackv2.Value * 100f,
                    VoidWatch_MaxBuffsv2.Value
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
            /*
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
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
                        childName = "Pelvis",
                        localPos = new Vector3(0F, 0F, 0F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(1F, 1F, 1F)
                    }
                }
            );
            */
            #endregion

            return rules;
        }

        public void CreateVoidWatchBuff()
        {
            BuffDef voidWatchBuff = ScriptableObject.CreateInstance<BuffDef>();
            voidWatchBuff.buffColor = new Color(0.76f, 0.3f, 0.92f);
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

                if (buffTimer >= buffInterval && body.outOfDanger && buffCount < VoidWatch_MaxBuffsv2.Value)
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