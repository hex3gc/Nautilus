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
            [ItemTag.Damage, ItemTag.AIBlacklist],
            ItemTier.VoidTier2
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     A boost to the collapse mechanic as it was lacking any interaction to make it comparable to bleed; with this item and Sprout I hope to make it DoT with an infectious theme to mirror void corruption
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
            0.05f,
            0.05f,
            1f,
            0.05f
        );
        public static ConfigItem<float> HydraTooth_CollapseChanceStack = new ConfigItem<float>
        (
            "Void uncommon: Tooth Of Hydra",
            "Collapse chance (per stack)",
            "Fractional chance to collapse an enemy on hit, per additional stack.",
            0.05f,
            0f,
            1f,
            0.05f
        );
        public static ConfigItem<float> HydraTooth_CollapseTransferChance = new ConfigItem<float>
        (
            "Void uncommon: Tooth Of Hydra",
            "Collapse transfer chance (hyperbolic)",
            "Chance that a stack of collapse is transferred.",
            0.5f,
            0.05f,
            1f,
            0.05f
        );
        public static ConfigItem<float> HydraTooth_ArmorReduction = new ConfigItem<float>
        (
            "Void uncommon: Tooth Of Hydra",
            "Armor reduction",
            "Collapse reduces enemy armor by this much.",
            20f,
            1f,
            60f,
            1f
        );
        public static ConfigItem<float> HydraTooth_ArmorReductionStack = new ConfigItem<float>
        (
            "Void uncommon: Tooth Of Hydra",
            "Armor reduction (per stack)",
            "Collapse reduces enemy armor by this much, per additional stack.",
            10f,
            0f,
            60f,
            1f
        );
        public static ConfigItem<float> HydraTooth_SpeedReduction = new ConfigItem<float>
        (
            "Void uncommon: Tooth Of Hydra",
            "Speed reduction",
            "Collapse reduces enemy speed by this fraction.",
            0.5f,
            0.1f,
            1f,
            0.1f
        );
        public static ConfigItem<float> HydraTooth_CollapseRadius = new ConfigItem<float>
        (
            "Void uncommon: Tooth Of Hydra",
            "Random collapse radius",
            "Meters radius where a transfer can potentially collapse an enemy.",
            40f,
            1f,
            60f,
            1f
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
                    HydraTooth_CollapseTransferChance.Value * 100f,
                    HydraTooth_CollapseRadius.Value,
                    HydraTooth_ArmorReduction.Value,
                    HydraTooth_ArmorReductionStack.Value,
                    HydraTooth_SpeedReduction.Value * 100f
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

                if (!damageInfo.procChainMask.HasProc(ProcType.FractureOnHit) && !damageInfo.rejected && damageInfo.damage > 0f && damageInfo.attacker && damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody) && attackerBody.master && victimObject.TryGetComponent(out CharacterBody victimBody) && victimBody.healthComponent)
                {
                    int itemCount = GetItemCountEffective(attackerBody);
                    
                    if (itemCount > 0 && attackerBody.teamComponent && victimBody.teamComponent)
                    {
                        if (Util.CheckRoll((HydraTooth_CollapseChance.Value + (HydraTooth_CollapseChanceStack.Value * (itemCount - 1))) * 100f * damageInfo.procCoefficient, attackerBody.master.luck, attackerBody.master))
                        {
                            DotController.DotDef dotDef = DotController.GetDotDef(DotController.DotIndex.Fracture);
                            DotController.InflictDot(victimObject, damageInfo.attacker, damageInfo.inflictedHurtbox, DotController.DotIndex.Fracture, dotDef.interval);
                        }
                    }
                }
            };

            // Armor/speed reduction
            RecalculateStatsAPI.GetStatCoefficients += (orig, self) =>
            {
                if (orig.HasBuff(DebuffDef.buffIndex) && orig.teamComponent && orig.teamComponent.teamIndex != TeamIndex.Player)
                {
                    int itemCount = GetToothsInTeam(TeamIndex.Player);

                    if (itemCount > 0)
                    {
                        self.armorAdd -= HydraTooth_ArmorReduction.Value + (HydraTooth_ArmorReductionStack.Value * (itemCount - 1));
                        self.moveSpeedMultAdd -= HydraTooth_SpeedReduction.Value;
                    }
                }
            };

            // Infect in radius
            On.RoR2.HealthComponent.TakeDamageProcess += (orig, self, damageInfo) =>
            {
                orig(self, damageInfo);

                if (damageInfo.attacker && damageInfo.dotIndex == DotController.DotIndex.Fracture && self.body)
                {   
                    CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    
                    if (attackerBody)
                    {
                        int itemCount = GetItemCountEffective(attackerBody);

                        if (itemCount > 0 && attackerBody.teamComponent && self.body.teamComponent && attackerBody.master && Util.CheckRoll(HydraTooth_CollapseTransferChance.Value * 100f, attackerBody.master.luck, attackerBody.master))
                        {
                            CharacterBody foundBody = null;

                            List<Collider> colliders = Physics.OverlapSphere(self.body.transform.position, HydraTooth_CollapseRadius.Value).ToList();
                            colliders = colliders.OrderBy(i => Guid.NewGuid()).ToList();

                            foreach(Collider collider in colliders)
                            {
                                GameObject gameObject = collider.gameObject;
                                if (gameObject.GetComponentInChildren<CharacterBody>())
                                {
                                    CharacterBody colliderBody = gameObject.GetComponentInChildren<CharacterBody>();
                                    if (colliderBody != self.body && colliderBody.healthComponent && colliderBody.healthComponent.health > 0f && colliderBody.teamComponent && colliderBody.teamComponent.teamIndex != attackerBody.teamComponent.teamIndex)
                                    {
                                        foundBody = colliderBody;
                                    }
                                }
                            }

                            if (foundBody != null)
                            {
                                foundBody.gameObject.AddComponent<CollapseDoNotTransferBehavior>();
                                CollapseInfectOrb.CreateInfectOrb(self.body.corePosition, foundBody.mainHurtBox);

                                DotController.DotDef dotDef = DotController.GetDotDef(DotController.DotIndex.Fracture);
                                DotController.InflictDot(foundBody.gameObject, damageInfo.attacker, foundBody.mainHurtBox, DotController.DotIndex.Fracture, dotDef.interval);
                            }
                        }
                    }
                }
            };
        }
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