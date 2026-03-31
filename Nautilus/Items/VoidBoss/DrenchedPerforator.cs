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
        public static DrenchedPerforator DrenchedPerforator = new DrenchedPerforator
        (
            "DrenchedPerforator",
            [ItemTag.Damage, ItemTag.AIBlacklist, ItemTag.ExtractorUnitBlacklist, ItemTag.BrotherBlacklist],
            ItemTier.VoidBoss
        );
    }

    /// <summary>
    ///     // Ver.1
    /// </summary>
    public class DrenchedPerforator : ItemBase
    {
        public override bool Enabled => DrenchedPerforator_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/FireballsOnHit/FireballsOnHit.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/drenchedPerforator.png");
        public ItemDef ConversionItemDefExtra => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/LightningStrikeOnHit/LightningStrikeOnHit.asset").WaitForCompletion();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/TrimSheets/matTrimSheetMetalLightSnow.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/Base/Croco/matCrocoSpine.mat").WaitForCompletion();
        private GameObject _explodePrefab;
        public GameObject explodePrefab
        {
            get
            {
                if (_explodePrefab == null)
                {
                    _explodePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidMegaCrab/VoidMegaCrabDeathBombExplosion.prefab").WaitForCompletion();
                }
                return _explodePrefab;
            }
            set;
        }
        private GameObject _individualExplodePrefab;
        public GameObject individualExplodePrefab
        {
            get
            {
                if (_individualExplodePrefab == null)
                {
                    _individualExplodePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/BleedOnHitVoid/FractureImpactEffect.prefab").WaitForCompletion();
                }
                return _individualExplodePrefab;
            }
            set;
        }
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
        public static ModdedProcType DrenchedPerforatorProcType = ProcTypeAPI.ReserveProcType();

        public DrenchedPerforator(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> DrenchedPerforator_Enabled = new ConfigItem<bool>
        (
            "Void boss: Drenched Perforator",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> DrenchedPerforator_Threshold = new ConfigItem<float>
        (
            "Void boss: Drenched Perforator",
            "Damage threshold",
            "Repeating fractional damage threshold for additional stacks of collapse to be added.",
            6f,
            1f,
            12f,
            0.5f
        );
        public static ConfigItem<int> DrenchedPerforator_Stacks = new ConfigItem<int>
        (
            "Void boss: Drenched Perforator",
            "Collapse stacks",
            "Number of stacks of collapse to add on passing threshold.",
            1,
            1f,
            5f,
            1f
        );
        public static ConfigItem<int> DrenchedPerforator_StacksStack = new ConfigItem<int>
        (
            "Void boss: Drenched Perforator",
            "Collapse stacks (per stack)",
            "Number of stacks of collapse to add on passing threshold, per additional stack.",
            1,
            1f,
            5f,
            1f
        );
        public static ConfigItem<float> DrenchedPerforator_ExplosionRadiusv2 = new ConfigItem<float>
        (
            "Void boss: Drenched Perforator",
            "Explosion radius",
            "Meters radius for the base collapse explosion.",
            12f,
            1f,
            24f,
            1f
        );
        public static ConfigItem<float> DrenchedPerforator_ExplosionRadiusIncreasev2 = new ConfigItem<float>
        (
            "Void boss: Drenched Perforator",
            "Explosion radius increase",
            "Meters radius the collapse explosion expands when additional collapse stacks are added.",
            4f,
            1f,
            8f,
            1f
        );
        public static ConfigItem<float> DrenchedPerforator_ExplosionDamage = new ConfigItem<float>
        (
            "Void boss: Drenched Perforator",
            "Explosion damage",
            "Fractional damage (1 = 100%) dealt by explosion.",
            4f,
            1f,
            12f,
            0.1f
        );
        public static ConfigItem<float> DrenchedPerforator_ExplosionProcCoefficient = new ConfigItem<float>
        (
            "Void boss: Drenched Perforator",
            "Explosion proc coefficient",
            "Proc coefficient of the on-collapse explosion.",
            0.2f,
            0f,
            1f,
            0.05f
        );
        public static ConfigItem<bool> DrenchedPerforator_Recipe = new ConfigItem<bool>
        (
            "Void boss: Drenched Perforator",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> DrenchedPerforator_Ingredient1 = new ConfigItem<string>
        (
            "Void boss: Drenched Perforator",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "FireballsOnHit"
        );
        public static ConfigItem<string> DrenchedPerforator_Ingredient1Alt = new ConfigItem<string>
        (
            "Void boss: Drenched Perforator",
            "Recipe ingredient 1 (alt)",
            "First ingredient for corruption recipe (alt)",
            "LightningStrikeOnHit"
        );
        public static ConfigItem<string> DrenchedPerforator_Ingredient2 = new ConfigItem<string>
        (
            "Void boss: Drenched Perforator",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "HydraTooth"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/drenchedPerforator.prefab");

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

            LanguageAPI.AddOverlay
            (
                descriptionToken,
                String.Format
                (
                    Language.currentLanguage.GetLocalizedStringByToken(descriptionToken),
                    DrenchedPerforator_Threshold.Value * 100f,
                    DrenchedPerforator_Stacks.Value,
                    DrenchedPerforator_StacksStack.Value,
                    DrenchedPerforator_ExplosionDamage.Value * 100f,
                    DrenchedPerforator_ExplosionRadiusv2.Value,
                    DrenchedPerforator_ExplosionRadiusIncreasev2.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // Additional void conversion
            ItemDef.Pair transformation = new()
            {
                itemDef1 = ConversionItemDefExtra,
                itemDef2 = ItemDef
            };
            Main.ItemConversionList.Add(transformation);

            Log.Info(String.Format("Added void conversion from {0} to {1}", ConversionItemDefExtra.name, ItemDef.name));

            // On-hit trigger
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victimObject) =>
            {
                orig(self, damageInfo, victimObject);

                if (!damageInfo.procChainMask.HasProc(ProcType.FractureOnHit) && !ProcTypeAPI.HasModdedProc(damageInfo.procChainMask, DrenchedPerforatorProcType) && !damageInfo.rejected && damageInfo.procCoefficient > 0f && damageInfo.damage > 0f && damageInfo.attacker && damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody) && attackerBody.master && victimObject.TryGetComponent(out CharacterBody victimBody) && victimBody.healthComponent)
                {
                    int itemCount = GetItemCountEffective(attackerBody);
                    
                    if (itemCount > 0 && attackerBody.teamComponent && victimBody.teamComponent)
                    {
                        float damage = damageInfo.crit ? damageInfo.damage * attackerBody.critMultiplier : damageInfo.damage;
                        float damageFraction = damage / attackerBody.damage;
                        if (damageFraction > 0f)
                        {
                            int hits = Convert.ToInt32(Math.Floor(damageFraction / DrenchedPerforator_Threshold.Value));
                            int stacksToInflict = (DrenchedPerforator_Stacks.Value + ((itemCount - 1) * DrenchedPerforator_StacksStack.Value)) * hits;

                            if (stacksToInflict > 0)
                            {
                                DotController.DotDef dotDef = DotController.GetDotDef(DotController.DotIndex.Fracture);
                                for (int i = 0; i < stacksToInflict; i++)
                                {
                                    DotController.InflictDot(victimObject, damageInfo.attacker, damageInfo.inflictedHurtbox, DotController.DotIndex.Fracture, dotDef.interval);
                                }

                                CreateExplosion(attackerBody, victimBody.corePosition, DrenchedPerforator_ExplosionRadiusv2.Value, DrenchedPerforator_ExplosionRadiusIncreasev2.Value * (stacksToInflict - 1), damageInfo);
                            }
                        }
                    }
                }
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (DrenchedPerforator_Recipe.Value == true && ItemInit.HydraTooth.Enabled)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    DrenchedPerforator_Ingredient1.Value,
                    DrenchedPerforator_Ingredient2.Value,
                    ItemDef.name
                );

                ItemInit.MakeCorruptionRecipe
                (
                    DrenchedPerforator_Ingredient1Alt.Value,
                    DrenchedPerforator_Ingredient2.Value,
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

        public void CreateExplosion(CharacterBody attackerBody, Vector3 position, float radius, float extraRadius, DamageInfo origDamageInfo)
        {
            List<Collider> colliders = Physics.OverlapSphere(position, radius + extraRadius).ToList();
            Util.ShuffleList(colliders);

            foreach(Collider collider in colliders)
            {
                GameObject gameObject = collider.gameObject;
                if (gameObject.GetComponentInChildren<CharacterBody>())
                {
                    CharacterBody colliderBody = gameObject.GetComponentInChildren<CharacterBody>();
                    if (colliderBody && colliderBody.teamComponent && colliderBody.teamComponent.teamIndex != attackerBody.teamComponent.teamIndex)
                    {
                        EffectData effectData = new EffectData()
                        {
                            origin = colliderBody.corePosition
                        };
                        EffectManager.SpawnEffect(individualExplodePrefab, effectData, true);
                    }
                }
            }

            origDamageInfo.procChainMask.AddModdedProc(DrenchedPerforatorProcType);

            BlastAttack blastAttack = new BlastAttack
            {
                position = position,
                baseDamage = attackerBody.damage * DrenchedPerforator_ExplosionDamage.Value,
                baseForce = 0f,
                radius = radius + extraRadius,
                attacker = attackerBody.gameObject,
                inflictor = null,
                teamIndex = attackerBody.teamComponent.teamIndex,
                crit = origDamageInfo.crit,
                procChainMask = origDamageInfo.procChainMask,
                procCoefficient = DrenchedPerforator_ExplosionProcCoefficient.Value,
                damageColorIndex = DamageColorIndex.Void,
                falloffModel = BlastAttack.FalloffModel.None,
                damageType = DamageType.AOE,
                attackerFiltering = AttackerFiltering.NeverHitSelf
            };
            blastAttack.Fire();

            float extraEffectSize = 0f;
            if (extraRadius > 0f)
            {
                extraEffectSize = extraRadius / radius;
            }
            else
            {
                extraEffectSize = 0f;
            }

            EffectData effectData2 = new EffectData()
            {
                origin = position,
                scale = 1f + extraEffectSize
            };
            EffectManager.SpawnEffect(explodePrefab, effectData2, true);
        }
    }
}