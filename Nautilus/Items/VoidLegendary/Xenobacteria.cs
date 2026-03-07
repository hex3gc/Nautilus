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
            [ItemTag.Damage, ItemTag.AIBlacklist],
            ItemTier.VoidTier3
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Alien Head's flat stat increase with a twist; more of an attack speed boost as Droneman, but applies to ALL minions
    ///     I wanted another attack speed item but didn't want to void the syringe
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
        public static ConfigItem<float> Xenobacteria_AttackSpeedBoost = new ConfigItem<float>
        (
            "Void legendary: Xenobacteria",
            "Attack speed increase",
            "Boost your and your allies' attack speed by this fraction.",
            0.3f,
            0f,
            1f,
            0.05f
        );
        public static ConfigItem<float> Xenobacteria_AttackSpeedBoostStack = new ConfigItem<float>
        (
            "Void legendary: Xenobacteria",
            "Attack speed increase (per stack)",
            "Boost your and your allies' attack speed by this fraction, per additional stack.",
            0.3f,
            0f,
            1f,
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
                    Xenobacteria_AttackSpeedBoost.Value * 100f,
                    Xenobacteria_AttackSpeedBoostStack.Value * 100f
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // Attack speed boost
            RecalculateStatsAPI.GetStatCoefficients += (orig, self) =>
            {
                if (orig.teamComponent && orig.teamComponent.teamIndex == TeamIndex.Player)
                {
                    int stack = GetBacteriasInTeam(TeamIndex.Player);
                    
                    if (stack > 0)
                    {
                        self.attackSpeedMultAdd += Xenobacteria_AttackSpeedBoost.Value + (Xenobacteria_AttackSpeedBoostStack.Value * (stack - 1));
                    }
                }
            };
        }

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
    }
}