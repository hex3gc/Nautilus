using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using RoR2.Orbs;
using System.Collections.Generic;
using System.Linq;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static HydraTooth HydraTooth = new HydraTooth
        (
            "HydraTooth",
            [ItemTag.Damage],
            ItemTier.VoidTier2
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     A boost to the collapse mechanic as it was lacking any interaction to make it comparable to bleed; with this item and Sprout I hope to make it DoT with an infectious theme to mirror void corruption
    ///     // Ver.2
    ///     Hydra Tooth felt awkward to use and its effects were hard to control; this version is a new much-needed AOE tool
    ///     Feels much more satisfying to use with Skullsprout
    /// </summary>
    public class HydraTooth : ItemBase
    {
        public override bool Enabled => HydraTooth_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC2/Items/TriggerEnemyDebuffs/TriggerEnemyDebuffs.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/TreasureCacheVoid/matKeyVoid.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/Base/artifactworld/matArtifactBloody.mat").WaitForCompletion();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/hydraTooth.png");
        public BuffDef DebuffDef => Addressables.LoadAssetAsync<BuffDef>("RoR2/DLC1/BleedOnHitVoid/bdFracture.asset").WaitForCompletion();
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

        public HydraTooth(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> HydraTooth_Enabled = new ConfigItem<bool>
        (
            "Void uncommon: Tooth Of Hydra",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> HydraTooth_CollapseChance = new ConfigItem<float>
        (
            "Void uncommon: Tooth Of Hydra",
            "Collapse chance",
            "Fractional chance to collapse an enemy on hit.",
            0.1f,
            0.1f,
            1f,
            0.05f
        );
        public static ConfigItem<float> HydraTooth_CollapseChanceStack = new ConfigItem<float>
        (
            "Void uncommon: Tooth Of Hydra",
            "Collapse chance (per stack)",
            "Fractional chance to collapse an enemy on hit, per additional stack.",
            0.1f,
            0f,
            1f,
            0.05f
        );
        public static ConfigItem<float> HydraTooth_CollapseExplosionDamageStack = new ConfigItem<float>
        (
            "Void uncommon: Tooth Of Hydra",
            "Collapse explosion damage per debuff stack",
            "Fractional damage dealt by the explosion per stack of collapse consumed",
            1f,
            0.1f,
            4f,
            0.1f
        );
        public static ConfigItem<float> HydraTooth_CollapseExplosionRadius = new ConfigItem<float>
        (
            "Void uncommon: Tooth Of Hydra",
            "Collapse explosion radius",
            "Meters radius of the collapse explosion",
            12f,
            0.1f,
            24f,
            0.1f
        );
        public static ConfigItem<float> HydraTooth_CollapseExplosionRadiusStack = new ConfigItem<float>
        (
            "Void uncommon: Tooth Of Hydra",
            "Collapse explosion radius (per stack)",
            "Meters radius of the collapse explosion, per additional stack",
            2.4f,
            0.1f,
            24f,
            0.1f
        );
        public static ConfigItem<float> HydraTooth_CollapseExplosionProcCoefficient = new ConfigItem<float>
        (
            "Void uncommon: Tooth Of Hydra",
            "Collapse explosion proc coefficient",
            "Proc coefficient of the collapse explosion",
            0.1f,
            0f,
            1f,
            0.01f
        );
        public static ConfigItem<float> HydraTooth_CollapseExplosionStunDuration = new ConfigItem<float>
        (
            "Void uncommon: Tooth Of Hydra",
            "Collapse explosion stun duration",
            "Duration of the collapse explosion's stun in seconds",
            1f,
            0f,
            4f,
            0.01f
        );
        public static ConfigItem<bool> HydraTooth_Recipe = new ConfigItem<bool>
        (
            "Void uncommon: Tooth Of Hydra",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> HydraTooth_Ingredient1 = new ConfigItem<string>
        (
            "Void uncommon: Tooth Of Hydra",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "TriggerEnemyDebuffs"
        );
        public static ConfigItem<string> HydraTooth_Ingredient2 = new ConfigItem<string>
        (
            "Void uncommon: Tooth Of Hydra",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "BleedOnHitVoid"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/hydraTooth.prefab");

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
                    HydraTooth_CollapseChance.Value * 100f,
                    HydraTooth_CollapseChanceStack.Value * 100f,
                    HydraTooth_CollapseExplosionDamageStack.Value * 100f,
                    HydraTooth_CollapseExplosionRadius.Value,
                    HydraTooth_CollapseExplosionRadiusStack.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // On-hit trigger
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victimObject) =>
            {
                if (!damageInfo.procChainMask.HasProc(ProcType.FractureOnHit) && !damageInfo.rejected && damageInfo.damage > 0f && damageInfo.attacker && damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody) && attackerBody.master && victimObject.TryGetComponent(out CharacterBody victimBody) && victimBody.healthComponent)
                {
                    int itemCount = GetItemCountEffective(attackerBody);
                    
                    if (itemCount > 0 && attackerBody.teamComponent && victimBody.teamComponent)
                    {
                        if (Util.CheckRoll((HydraTooth_CollapseChance.Value + (HydraTooth_CollapseChanceStack.Value * (itemCount - 1))) * 100f * damageInfo.procCoefficient, attackerBody.master.luck, attackerBody.master))
                        {
                            // TODO does this still overlap and have a chance to add 2 stacks?
                            DotController.DotDef dotDef = DotController.GetDotDef(DotController.DotIndex.Fracture);
                            DotController.InflictDot(victimObject, damageInfo.attacker, damageInfo.inflictedHurtbox, DotController.DotIndex.Fracture, dotDef.interval);
                        }
                    }
                }

                orig(self, damageInfo, victimObject);
            };

            // Create explosion on collapse damage
            On.RoR2.HealthComponent.TakeDamageProcess += (orig, self, damageInfo) =>
            {
                if (damageInfo.dotIndex == DotController.DotIndex.Fracture && !damageInfo.rejected && damageInfo.damage > 0f && damageInfo.attacker)
                {
                    CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    if (attackerBody && GetItemCountEffective(attackerBody) > 0)
                    {
                        // Default explosion damage = 1 / 4 (1 stack = 100%, 4 stacks = 400%, etc.)
                        float explosionDamage = (HydraTooth_CollapseExplosionDamageStack.Value / 4f) * damageInfo.damage;
                        float radius = HydraTooth_CollapseExplosionRadius.Value + (HydraTooth_CollapseExplosionRadiusStack.Value * (GetItemCountEffective(attackerBody) - 1));
                        ProcChainMask procChainMask = new ProcChainMask();
                        procChainMask.AddProc(ProcType.FractureOnHit);

                        BlastAttack blastAttack = new BlastAttack
                        {
                            position = damageInfo.position,
                            baseDamage = explosionDamage,
                            baseForce = 0f,
                            radius = radius,
                            attacker = damageInfo.attacker,
                            inflictor = null,
                            teamIndex = TeamComponent.GetObjectTeam(damageInfo.attacker),
                            crit = attackerBody.RollCrit(),
                            procChainMask = procChainMask,
                            procCoefficient = HydraTooth_CollapseExplosionProcCoefficient.Value,
                            damageColorIndex = DamageColorIndex.Void,
                            falloffModel = BlastAttack.FalloffModel.None,
                            damageType = DamageType.AOE,
                            attackerFiltering = AttackerFiltering.NeverHitSelf
                        };
                        BlastAttack.Result affectedEnemies = blastAttack.Fire();
                        foreach (BlastAttack.HitPoint hitpoint in affectedEnemies.hitPoints)
                        {
                            if (hitpoint.hurtBox && hitpoint.hurtBox.healthComponent && hitpoint.hurtBox.healthComponent.body)
                            {
                                if (hitpoint.hurtBox.healthComponent.gameObject.TryGetComponent(out SetStateOnHurt hurtState))
                                {
                                    float radiusSquare = (float)Math.Pow(radius, 2);
                                    float stunDurationProportion = hitpoint.distanceSqr / radiusSquare;
                                    if (stunDurationProportion < 1)
                                    {
                                        hurtState.SetStun(HydraTooth_CollapseExplosionStunDuration.Value);
                                    }
                                }
                            }
                        }

                        float scaleProportion = radius / HydraTooth_CollapseExplosionRadius.Value;
                        EffectData effectData = new EffectData
                        {
                            origin = damageInfo.position,
                            scale = scaleProportion * 8
                        };
                        EffectManager.SpawnEffect(explodePrefab, effectData, true);
                    }
                }

                orig(self, damageInfo);
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (HydraTooth_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    HydraTooth_Ingredient1.Value,
                    HydraTooth_Ingredient2.Value,
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
                        childName = "Chest",
                        localPos = new Vector3(0.19615F, 0.41993F, -0.07579F),
                        localAngles = new Vector3(18.38553F, 30.75567F, 309.8916F),
                        localScale = new Vector3(0.30668F, 0.30668F, 0.30668F)
                    }
                }
            );
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.01384F, 0.08245F, 0.13035F),
                        localAngles = new Vector3(311.4166F, 265.4389F, 245.1299F),
                        localScale = new Vector3(0.20255F, 0.20255F, 0.20255F)
                    }
                }
            );
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Hat",
                        localPos = new Vector3(-0.00727F, 0.08001F, 0.12739F),
                        localAngles = new Vector3(7.18305F, 44.60707F, 336.7971F),
                        localScale = new Vector3(0.20509F, 0.20509F, 0.20509F)
                    }
                }
            );
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(1.80855F, 2.41185F, -0.13005F),
                        localAngles = new Vector3(17.98998F, 45.18383F, 346.3629F),
                        localScale = new Vector3(2.52552F, 2.52552F, 2.52552F)
                    }
                }
            );
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "FootL",
                        localPos = new Vector3(-0.01151F, 0.20096F, -0.12813F),
                        localAngles = new Vector3(312.9743F, 65.56718F, 270.9045F),
                        localScale = new Vector3(0.20587F, 0.20587F, 0.20587F)
                    }
                }
            );
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pelvis",
                        localPos = new Vector3(0.26691F, 0.07741F, 0.04157F),
                        localAngles = new Vector3(20.5455F, 22.258F, 91.12227F),
                        localScale = new Vector3(0.1939F, 0.1939F, 0.19327F)
                    }
                }
            );
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "SwordBase",
                        localPos = new Vector3(-0.01956F, 0.34844F, -0.003F),
                        localAngles = new Vector3(12.06424F, 41.59543F, 338.6489F),
                        localScale = new Vector3(0.39487F, 0.39487F, 0.39487F)
                    }
                }
            );
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Base",
                        localPos = new Vector3(0.15767F, 0.87197F, -1.45617F),
                        localAngles = new Vector3(321.9741F, 74.32614F, 256.6255F),
                        localScale = new Vector3(0.52041F, 0.52041F, 0.52041F)
                    }
                }
            );
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.01763F, 0.29012F, 0.06423F),
                        localAngles = new Vector3(9.31793F, 39.21895F, 338.5111F),
                        localScale = new Vector3(0.26194F, 0.26194F, 0.26194F)
                    }
                }
            );
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "MouthMuzzle",
                        localPos = new Vector3(-0.52581F, 2.6205F, 3.95378F),
                        localAngles = new Vector3(17.13763F, 41.48186F, 342.577F),
                        localScale = new Vector3(2.38907F, 2.38907F, 2.38907F)
                    }
                }
            );
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(-0.02084F, 0.33586F, 0.0584F),
                        localAngles = new Vector3(6.14586F, 37.77752F, 335.9771F),
                        localScale = new Vector3(0.25304F, 0.25304F, 0.25304F)
                    }
                }
            );
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "ToeR",
                        localPos = new Vector3(-0.08888F, 0.17111F, 0.01675F),
                        localAngles = new Vector3(314.7864F, 169.9855F, 253.5239F),
                        localScale = new Vector3(0.29485F, 0.29485F, 0.29485F)
                    }
                }
            );
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Head",
                        localPos = new Vector3(0.1682F, 0.17862F, 0.13788F),
                        localAngles = new Vector3(319.7149F, 316.2584F, 119.7615F),
                        localScale = new Vector3(0.24985F, 0.24985F, 0.24985F)
                    }
                }
            );
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Pack",
                        localPos = new Vector3(0.107F, -0.08567F, -0.34887F),
                        localAngles = new Vector3(49.90333F, 291.6018F, 4.64734F),
                        localScale = new Vector3(0.27392F, 0.27392F, 0.27392F)
                    }
                }
            );
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(-0.45698F, 0.16417F, -0.38749F),
                        localAngles = new Vector3(281.3585F, 102.0705F, 281.1522F),
                        localScale = new Vector3(0.61926F, 0.61926F, 0.61926F)
                    }
                }
            );
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Cleaver",
                        localPos = new Vector3(-0.16585F, 0.42729F, -0.01543F),
                        localAngles = new Vector3(26.70289F, 325.1594F, 348.7908F),
                        localScale = new Vector3(0.76697F, 0.76697F, 0.76697F)
                    }
                }
            );
            rules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "ClawRightTip",
                        localPos = new Vector3(0.07737F, 0.1517F, 0.02069F),
                        localAngles = new Vector3(30.80137F, 138.6705F, 359.241F),
                        localScale = new Vector3(0.47248F, 0.47248F, 0.47248F)
                    }
                }
            );
            rules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(-0.212F, -0.11479F, -0.30873F),
                        localAngles = new Vector3(302.9277F, 131.4791F, 242.4093F),
                        localScale = new Vector3(0.32932F, 0.32932F, 0.32932F)
                    }
                }
            );
            #endregion

            return rules;
        }

        public int GetToothsInTeam(TeamIndex teamIndex)
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
    }

    public class CollapseDoNotTransferBehavior : MonoBehaviour
    {

    }
}