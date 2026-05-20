using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using System.Collections.Generic;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static TenebralGland TenebralGland = new TenebralGland
        (
            "TenebralGland",
            [ItemTag.Damage, ItemTag.CanBeTemporary],
            ItemTier.VoidBoss
        );
    }

    /// <summary>
    ///     // Ver.1
    /// </summary>
    public class TenebralGland : ItemBase
    {
        public override bool Enabled => TenebralGland_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/BleedOnHitAndExplode/BleedOnHitAndExplode.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/tenebralGland.png");
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/Base/artifactworld/matArtifactGem.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matBloodClayLarge.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/Base/artifactworld/matArtifactGem.mat").WaitForCompletion();
        public Material material3 => Addressables.LoadAssetAsync<Material>("RoR2/DLC3/Items/TransferDebuffOnHit/matTransferDebuffOverlay.mat").WaitForCompletion();
        public BuffDef TenebralBuff;
        private ExplicitPickupDropTable _explicitPickupDropTable;
        public ExplicitPickupDropTable explicitPickupDropTable
        {
            get
            {
                if (_explicitPickupDropTable == null)
                {
                    _explicitPickupDropTable = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();
                    _explicitPickupDropTable.pickupEntries = new ExplicitPickupDropTable.PickupDefEntry[]
                    {
                        new ExplicitPickupDropTable.PickupDefEntry
                        {
                            pickupDef = ItemDef,
                            pickupWeight = 1
                        }
                    };
                }

                _explicitPickupDropTable.Regenerate(Run.instance);
                return _explicitPickupDropTable;
            }
            set;
        }

        public TenebralGland(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> TenebralGland_Enabled = new ConfigItem<bool>
        (
            "Void boss: Tenebral Gland",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> TenebralGland_SuperBleedChance = new ConfigItem<float>
        (
            "Void boss: Tenebral Gland",
            "Hemorrhage chance",
            "Percent chance that, upon critting, hemorrhage will be applied.",
            5f,
            1f,
            20f,
            0.5f
        );
        public static ConfigItem<float> TenebralGland_SuperBleedChanceStack = new ConfigItem<float>
        (
            "Void boss: Tenebral Gland",
            "Hemorrhage chance (per stack)",
            "Percent chance that, upon critting, hemorrhage will be applied, per additional stack.",
            2.5f,
            1f,
            20f,
            0.5f
        );
        public static ConfigItem<float> TenebralGland_CritChanceBuff = new ConfigItem<float>
        (
            "Void boss: Tenebral Gland",
            "Buff crit chance",
            "Fractional crit chance granted by each stack of the buff.",
            0.1f,
            0.01f,
            0.5f,
            0.01f
        );
        public static ConfigItem<float> TenebralGland_CritDamageBuff = new ConfigItem<float>
        (
            "Void boss: Tenebral Gland",
            "Buff crit damage",
            "Fractional crit damage granted by each stack of the buff.",
            0.1f,
            0.01f,
            0.5f,
            0.01f
        );
        public static ConfigItem<float> TenebralGland_BuffLength = new ConfigItem<float>
        (
            "Void boss: Tenebral Gland",
            "Buff length",
            "Length of the buff in seconds, refreshing upon gaining a new stack.",
            10f,
            1f,
            30f,
            1f
        );
        public static ConfigItem<int> TenebralGland_BuffMaxStacks = new ConfigItem<int>
        (
            "Void boss: Tenebral Gland",
            "Buff max stacks (per stack)",
            "Max amount of buffs to gain per stack of this item.",
            10,
            1f,
            30f,
            1f
        );
        public static ConfigItem<bool> TenebralGland_Recipe = new ConfigItem<bool>
        (
            "Void boss: Tenebral Gland",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> TenebralGland_Ingredient1 = new ConfigItem<string>
        (
            "Void boss: Tenebral Gland",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "BleedOnHitAndExplode"
        );
        public static ConfigItem<string> TenebralGland_Ingredient2 = new ConfigItem<string>
        (
            "Void boss: Tenebral Gland",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "Dreadleech"
        );


        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/tenebralGland.prefab");

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
                    TenebralGland_SuperBleedChance.Value,
                    TenebralGland_SuperBleedChanceStack.Value,
                    TenebralGland_CritChanceBuff.Value * 100f, // I will not bother with custom formatting
                    TenebralGland_BuffLength.Value,
                    TenebralGland_BuffMaxStacks.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            CreateTenebralBuff();

            // On-hit trigger
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victimObject) =>
            {
                orig(self, damageInfo, victimObject);

                if (!damageInfo.rejected && damageInfo.crit && damageInfo.damage > 0f && damageInfo.attacker && damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody) && attackerBody.master && victimObject.TryGetComponent(out CharacterBody victimBody))
                {
                    int itemCount = GetItemCountEffective(attackerBody);
                    
                    if (itemCount > 0 && attackerBody.teamComponent && victimBody.teamComponent && attackerBody.teamComponent.teamIndex != victimBody.teamComponent.teamIndex)
                    {
                        if (Util.CheckRoll((TenebralGland_SuperBleedChance.Value + (TenebralGland_SuperBleedChanceStack.Value * (itemCount - 1))) * damageInfo.procCoefficient, attackerBody.master.luck, attackerBody.master))
                        {
                            DotController.DotDef dotDef = DotController.GetDotDef(DotController.DotIndex.SuperBleed);
                            DotController.InflictDot(victimObject, damageInfo.attacker, damageInfo.inflictedHurtbox, DotController.DotIndex.SuperBleed, 15f);
                            
                            if (!victimObject.GetComponent<TenebralMarkBehavior>())
                            {
                                victimObject.AddComponent<TenebralMarkBehavior>();
                            }

                            victimObject.GetComponent<TenebralMarkBehavior>().inflictorBodies.Add(attackerBody);
                        }
                    }
                }
            };

            // Buff lost behavior for both superbleed and tenebral
            On.RoR2.CharacterBody.OnBuffFinalStackLost += (orig, self, buffDef) =>
            {
                orig(self, buffDef);

                if (buffDef.buffIndex == RoR2Content.Buffs.SuperBleed.buffIndex && self.GetComponent<TenebralMarkBehavior>())
                {
                    UnityEngine.Object.Destroy(self.GetComponent<TenebralMarkBehavior>());
                }

                if (buffDef.buffIndex == TenebralBuff.buffIndex)
                {
                    self.RecalculateStats();
                }
            };

            On.RoR2.CharacterBody.OnDeathStart += (orig, self) =>
            {
                if (self.gameObject.TryGetComponent(out TenebralMarkBehavior tenebralMarkBehavior))
                {
                    foreach (CharacterBody body in tenebralMarkBehavior.inflictorBodies)
                    {
                        int itemCount = GetItemCountEffective(body);

                        if (itemCount > 0)
                        {
                            if (body.GetBuffCount(TenebralBuff) < TenebralGland_BuffMaxStacks.Value * itemCount)
                            {
                                body.SetTimedBuffDurationIfPresent(TenebralBuff, TenebralGland_BuffLength.Value, true);
                                body.AddTimedBuff(TenebralBuff, TenebralGland_BuffLength.Value);

                                TemporaryOverlay temporaryOverlay = body.gameObject.AddComponent<TemporaryOverlay>();
                                temporaryOverlay.duration = 1f;
                                temporaryOverlay.animateShaderAlpha = true;
                                temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                                temporaryOverlay.destroyComponentOnEnd = true;
                                temporaryOverlay.originalMaterial = ItemInit.MobiusNode.ExplodeOverlay;
                                temporaryOverlay.AddToCharacerModel(body.gameObject.GetComponent<ModelLocator>().modelTransform.GetComponentInParent<CharacterModel>());
                            }
                        }
                    }
                }

                orig(self);
            };
            
            // Crit boost
            RecalculateStatsAPI.GetStatCoefficients += (orig, self) =>
            {
                int itemCount = GetItemCountEffective(orig);
                if (itemCount > 0)
                {
                    int buffCount = orig.GetBuffCount(TenebralBuff);
                    self.critAdd += TenebralGland_CritChanceBuff.Value * buffCount;
                    self.critDamageMultAdd += TenebralGland_CritDamageBuff.Value * buffCount;
                }
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (TenebralGland_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    TenebralGland_Ingredient1.Value,
                    TenebralGland_Ingredient2.Value,
                    ItemDef.name
                );
            }
        }

        // IDR
        public override ItemDisplayRuleDict AddItemDisplays()
        {
            GameObject ItemDisplayPrefab = Helpers.PrepareItemDisplayModel(PrefabAPI.InstantiateClone(itemPrefab, ItemDef.name + "Display", false));
            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();

            /*
            #region IDR
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
            #endregion
            */

            return rules;
        }

        public void CreateTenebralBuff()
        {
            BuffDef tenebralBuff = ScriptableObject.CreateInstance<BuffDef>();
            tenebralBuff.buffColor = new Color(1f, 1f, 1f);
            tenebralBuff.canStack = true;
            tenebralBuff.isDebuff = false;
            tenebralBuff.ignoreGrowthNectar = false;
            tenebralBuff.name = "Tenebral rush";
            tenebralBuff.isHidden = false;
            tenebralBuff.isCooldown = false;
            tenebralBuff.iconSprite = Main.Assets.LoadAsset<Sprite>("Assets/icons/tenebralBuff.png");
            ContentAddition.AddBuffDef(tenebralBuff);

            TenebralBuff = tenebralBuff;
        }

        public class TenebralMarkBehavior : MonoBehaviour
        {
            public List<CharacterBody> inflictorBodies = new();
        }
    }
}