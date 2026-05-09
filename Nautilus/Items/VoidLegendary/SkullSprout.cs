using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using RoR2.Orbs;
using System.Linq;
using System.Collections.Generic;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static SkullSprout SkullSprout = new SkullSprout
        (
            "Skullsprout",
            [ItemTag.Damage],
            ItemTier.VoidTier3
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Adds randomness to collapse effects as a 'downside', but it's really an upside since you can far more easily farm cooldown reduction off of it
    ///     Potentially far more consistent than Brainstalks, want to make sure it doesn't outshine it too much
    /// </summary>
    public class SkullSprout : ItemBase
    {
        public override bool Enabled => SkullSprout_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/KillEliteFrenzy/KillEliteFrenzy.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/Base/artifactworld/matArtifactBloody.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/SlowOnHitVoid/BaubleVoid.mat").WaitForCompletion();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/skullSprout.png");

        public SkullSprout(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> SkullSprout_Enabled = new ConfigItem<bool>
        (
            "Void legendary: Skullsprout",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> SkullSprout_CollapseChance = new ConfigItem<float>
        (
            "Void legendary: Skullsprout",
            "Collapse chance",
            "Fractional chance to collapse a random enemy within a radius on hit.",
            0.05f,
            0.05f,
            1f,
            0.05f
        );
        public static ConfigItem<float> SkullSprout_CollapseRadius = new ConfigItem<float>
        (
            "Void legendary: Skullsprout",
            "Random collapse radius",
            "Meters radius where a hit can potentially collapse an enemy.",
            40f,
            1f,
            60f,
            1f
        );
        public static ConfigItem<float> SkullSprout_CollapseCooldownReduction = new ConfigItem<float>
        (
            "Void legendary: Skullsprout",
            "Collapse cooldown reduction",
            "Collapse reduces your cooldowns by this many seconds on trigger.",
            0.4f,
            0.1f,
            2f,
            0.1f
        );
        public static ConfigItem<float> SkullSprout_CollapseCooldownReductionStack = new ConfigItem<float>
        (
            "Void legendary: Skullsprout",
            "Collapse cooldown reduction (per stack)",
            "Collapse reduces your cooldowns by this many seconds on trigger, per additional stack.",
            0.4f,
            0.1f,
            2f,
            0.1f
        );
        public static ConfigItem<bool> SkullSprout_Recipe = new ConfigItem<bool>
        (
            "Void legendary: Skullsprout",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> SkullSprout_Ingredient1 = new ConfigItem<string>
        (
            "Void legendary: Skullsprout",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "KillEliteFrenzy"
        );
        public static ConfigItem<string> SkullSprout_Ingredient2 = new ConfigItem<string>
        (
            "Void legendary: Skullsprout",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "HydraTooth"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/skullSprout.prefab");

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
                    SkullSprout_CollapseChance.Value * 100f,
                    SkullSprout_CollapseRadius.Value,
                    SkullSprout_CollapseCooldownReduction.Value,
                    SkullSprout_CollapseCooldownReductionStack.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // On-hit trigger
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victimObject) =>
            {
                orig(self, damageInfo, victimObject);

                if (!damageInfo.procChainMask.HasProc(ProcType.FractureOnHit) && !damageInfo.rejected && damageInfo.attacker && damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody) && attackerBody.master && victimObject.TryGetComponent(out CharacterBody victimBody) && victimBody.healthComponent)
                {
                    int itemCount = GetItemCountEffective(attackerBody);
                    
                    if (itemCount > 0 && attackerBody.teamComponent && victimBody.teamComponent)
                    {
                        if (Util.CheckRoll(SkullSprout_CollapseChance.Value * 100f * damageInfo.procCoefficient, attackerBody.master.luck, attackerBody.master))
                        {
                            DotController.DotDef dotDef = DotController.GetDotDef(DotController.DotIndex.Fracture);
                            DotController.InflictDot(victimBody.gameObject, damageInfo.attacker, victimBody.mainHurtBox, DotController.DotIndex.Fracture, dotDef.interval);
                        }
                    }
                }
            };

            // On receipt of fracture damage, reduce attacker's cooldowns
            On.RoR2.HealthComponent.TakeDamageProcess += (orig, self, damageInfo) =>
            {
                if (damageInfo.attacker && damageInfo.dotIndex == DotController.DotIndex.Fracture)
                {
                    CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    
                    if (attackerBody && attackerBody.skillLocator)
                    {
                        int itemCount = GetItemCountEffective(attackerBody);
                        if (itemCount > 0)
                        {
                            attackerBody.skillLocator.DeductCooldownFromAllSkillsServer(SkullSprout_CollapseCooldownReduction.Value + (SkullSprout_CollapseCooldownReductionStack.Value * (itemCount - 1)));
                        }
                    }
                }

                orig(self, damageInfo);   
            };

            // Fracture randomness
            On.RoR2.DotController.InflictDot_refInflictDotInfo += (orig, ref self) =>
            {
                CharacterBody attackerBody = self.attackerObject.GetComponent<CharacterBody>();
                CharacterBody victimBody = self.victimObject.GetComponent<CharacterBody>();
                CollapseDoNotTransferBehavior collapseDoNotTransferBehavior = victimBody.gameObject.GetComponent<CollapseDoNotTransferBehavior>();
                bool transfer = self.dotIndex == DotController.DotIndex.Fracture && GetItemCountEffective(attackerBody) > 0 && attackerBody.teamComponent && victimBody.teamComponent && attackerBody.teamComponent.teamIndex != victimBody.teamComponent.teamIndex;
                
                if (transfer && !collapseDoNotTransferBehavior)
                {
                    CharacterBody foundBody = null;

                    List<Collider> colliders = Physics.OverlapSphere(self.hitHurtBox.transform.position, SkullSprout_CollapseRadius.Value).ToList();
                    Util.ShuffleList(colliders);
                    
                    foreach(Collider collider in colliders)
                    {
                        GameObject gameObject = collider.gameObject;
                        if (gameObject.GetComponentInChildren<CharacterBody>())
                        {
                            CharacterBody colliderBody = gameObject.GetComponentInChildren<CharacterBody>();
                            if (colliderBody == victimBody)
                            {
                                break;
                            }

                            if (colliderBody.healthComponent && colliderBody.healthComponent.health > 0f && colliderBody.teamComponent && colliderBody.teamComponent.teamIndex != attackerBody.teamComponent.teamIndex && colliderBody != victimBody && colliderBody != attackerBody)
                            {
                                foundBody = colliderBody;
                            }
                        }
                    }

                    if (foundBody)
                    {
                        InflictDotInfo newDot = new InflictDotInfo();
                        newDot.attackerObject = self.attackerObject;
                        newDot.damageMultiplier = self.damageMultiplier;
                        newDot.dotIndex = self.dotIndex;
                        newDot.duration = self.duration;
                        newDot.hitHurtBox = foundBody.mainHurtBox;
                        newDot.maxStacksFromAttacker = self.maxStacksFromAttacker;
                        newDot.totalDamage = self.totalDamage;
                        newDot.victimObject = foundBody.gameObject;

                        foundBody.gameObject.AddComponent<CollapseDoNotTransferBehavior>();
                        CollapseInfectOrb.CreateInfectOrb(victimBody.corePosition, foundBody.mainHurtBox);

                        DotController.InflictDot(ref newDot);

                        return;
                    }
                }

                if (collapseDoNotTransferBehavior)
                {
                    UnityEngine.Object.Destroy(self.victimObject.GetComponent<CollapseDoNotTransferBehavior>());
                }

                orig(ref self);
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (SkullSprout_Recipe.Value == true /*&& ItemInit.HydraTooth.Enabled*/)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    SkullSprout_Ingredient1.Value,
                    SkullSprout_Ingredient2.Value,
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
    }
}