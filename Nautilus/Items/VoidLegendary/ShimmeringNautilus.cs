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
        public static ShimmeringNautilus ShimmeringNautilus = new ShimmeringNautilus
        (
            "ShimmeringNautilus",
            [ItemTag.Damage, ItemTag.Healing, ItemTag.AIBlacklist, ItemTag.ExtractorUnitBlacklist, ItemTag.BrotherBlacklist, ItemTag.CanBeTemporary],
            ItemTier.VoidTier3
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Shimmering Nautilus is a situationally strong tool, massively countering enemies that inflict DOT and giving you damage spikes against bosses.
    ///     Also provides a passive damage resistance that makes it worth picking up even if you're not in it for the damage.
    ///     Antler Shield got a glow-up
    /// </summary>
    public class ShimmeringNautilus : ItemBase
    {
        public override bool Enabled => ShimmeringNautilus_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/ArmorReductionOnHit/ArmorReductionOnHit.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/EliteVoid/matVoidInfestorMetal.mat").WaitForCompletion();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/shimmeringNautilus.png");
        public BuffDef NautilusBuff;
        public Material OverlayMaterial => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidSurvivor/matVoidBlinkBodyOverlay.mat").WaitForCompletion();

        public ShimmeringNautilus(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> ShimmeringNautilus_Enabled = new ConfigItem<bool>
        (
            "Void legendary: Shimmering Nautilus",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> ShimmeringNautilus_DamageResist = new ConfigItem<float>
        (
            "Void legendary: Shimmering Nautilus",
            "Damage resistance",
            "Resist this fraction of all damage.",
            0.1f,
            0f,
            1f,
            0.05f
        );
        public static ConfigItem<int> ShimmeringNautilus_RetaliateHits = new ConfigItem<int>
        (
            "Void legendary: Shimmering Nautilus",
            "Retaliation hits",
            "Retaliation damage requires this many hits from the same enemy to trigger.",
            5,
            1f,
            20f,
            1f
        );
        public static ConfigItem<float> ShimmeringNautilus_RetaliateDamage = new ConfigItem<float>
        (
            "Void legendary: Shimmering Nautilus",
            "Retaliation damage",
            "Base damage percentage of retaliation hits.",
            1800f,
            100f,
            5000f,
            100f
        );
        public static ConfigItem<float> ShimmeringNautilus_RetaliateDamageStack = new ConfigItem<float>
        (
            "Void legendary: Shimmering Nautilus",
            "Retaliation damage (per stack)",
            "Base damage percentage of retaliation hits, per additional stack.",
            1800f,
            100f,
            2500f,
            100f
        );
        public static ConfigItem<float> ShimmeringNautilus_RetaliateProcCoefficient = new ConfigItem<float>
        (
            "Void legendary: Shimmering Nautilus",
            "Retaliation proc coefficient",
            "Proc coefficient of the retaliation projectile.",
            1f,
            0f,
            3f,
            0.1f
        );
        public static ConfigItem<bool> ShimmeringNautilus_Recipe = new ConfigItem<bool>
        (
            "Void legendary: Shimmering Nautilus",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> ShimmeringNautilus_Ingredient1 = new ConfigItem<string>
        (
            "Void legendary: Shimmering Nautilus",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "ArmorReductionOnHit"
        );
        public static ConfigItem<string> ShimmeringNautilus_Ingredient2 = new ConfigItem<string>
        (
            "Void legendary: Shimmering Nautilus",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "ExplodeOnDeathVoid"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/shimmeringNautilus.prefab");

            Material[] materials =
            {
                material0
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
                    ShimmeringNautilus_DamageResist.Value * 100f,
                    ShimmeringNautilus_RetaliateHits.Value,
                    ShimmeringNautilus_RetaliateDamage.Value,
                    ShimmeringNautilus_RetaliateDamageStack.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            CreateNautilusBuff();

            // Damage resist, debuff application and retaliation
            On.RoR2.HealthComponent.TakeDamageProcess += (orig, self, damageInfo) =>
            {
                CharacterBody victimBody = self.body;

                if (damageInfo.attacker && damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody) && victimBody && attackerBody.teamComponent && victimBody.teamComponent)
                {
                    int itemCount = GetItemCountEffective(victimBody);

                    // Damage resist & debuff
                    if (itemCount > 0)
                    {
                        damageInfo.damage *= 1f - ShimmeringNautilus_DamageResist.Value;

                        if (attackerBody.teamComponent.teamIndex != victimBody.teamComponent.teamIndex)
                        {
                            attackerBody.AddBuff(NautilusBuff.buffIndex);
                        }

                        // Retaliation
                        if (attackerBody.GetBuffCount(NautilusBuff) >= ShimmeringNautilus_RetaliateHits.Value)
                        {
                            MissileVoidOrb missileVoidOrb = new MissileVoidOrb();
                            missileVoidOrb.origin = victimBody.aimOrigin;
                            missileVoidOrb.damageValue = victimBody.damage * (ShimmeringNautilus_RetaliateDamage.Value / 100f) + ((ShimmeringNautilus_RetaliateDamageStack.Value / 100f) * (itemCount - 1));
                            missileVoidOrb.teamIndex = victimBody.teamComponent.teamIndex;
                            missileVoidOrb.attacker = victimBody.gameObject;
                            missileVoidOrb.procCoefficient = ShimmeringNautilus_RetaliateProcCoefficient.Value;
                            missileVoidOrb.damageColorIndex = DamageColorIndex.Void;
                            missileVoidOrb.scale = 3f;
                            HurtBox mainHurtBox = attackerBody.mainHurtBox;
                            if ((bool)mainHurtBox)
                            {
                                TemporaryOverlay temporaryOverlay = victimBody.gameObject.AddComponent<TemporaryOverlay>();
                                temporaryOverlay.duration = 1f;
                                temporaryOverlay.animateShaderAlpha = true;
                                temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                                temporaryOverlay.destroyComponentOnEnd = true;
                                temporaryOverlay.originalMaterial = OverlayMaterial;
                                temporaryOverlay.AddToCharacerModel(victimBody.gameObject.GetComponent<ModelLocator>().modelTransform.GetComponentInParent<CharacterModel>());

                                missileVoidOrb.target = mainHurtBox;
                                OrbManager.instance.AddOrb(missileVoidOrb);
                                attackerBody.ClearAllBuffs(NautilusBuff);
                            }
                        }
                    }
                }

                orig(self, damageInfo);
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (ShimmeringNautilus_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    ShimmeringNautilus_Ingredient1.Value,
                    ShimmeringNautilus_Ingredient2.Value,
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
                        localPos = new Vector3(0F, 0.17409F, 0.2061F),
                        localAngles = new Vector3(0F, 180F, 0F),
                        localScale = new Vector3(0.26712F, 0.26712F, 0.26712F)
                    }
                }
            );
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0.00428F, 0.15579F, 0.14781F),
                        localAngles = new Vector3(12.0427F, 188.8803F, 0F),
                        localScale = new Vector3(0.26001F, 0.26001F, 0.26001F)
                    }
                }
            );
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0.00156F, 0.2192F, -0.13947F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(0.33958F, 0.33958F, 0.33958F)
                    }
                }
            );
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "UpperArmL",
                        localPos = new Vector3(-0.61247F, 4.18777F, 0.1862F),
                        localAngles = new Vector3(0F, 101.5618F, 0F),
                        localScale = new Vector3(2.16088F, 2.16088F, 2.16088F)
                    }
                }
            );
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0F, 0.15054F, 0.23648F),
                        localAngles = new Vector3(0F, 180F, 0F),
                        localScale = new Vector3(0.37651F, 0.37651F, 0.37651F)
                    }
                }
            );
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0F, 0.11433F, 0.12015F),
                        localAngles = new Vector3(0F, 180F, 0F),
                        localScale = new Vector3(0.25895F, 0.25895F, 0.25895F)
                    }
                }
            );
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0F, 0.11713F, 0.14196F),
                        localAngles = new Vector3(0F, 180F, 0F),
                        localScale = new Vector3(0.36638F, 0.36638F, 0.36638F)
                    }
                }
            );
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Eye",
                        localPos = new Vector3(0F, 0.83976F, 0.00004F),
                        localAngles = new Vector3(90F, 0F, 0F),
                        localScale = new Vector3(0.50612F, 0.50612F, 0.50612F)
                    }
                }
            );
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0F, 0.1599F, 0.15155F),
                        localAngles = new Vector3(0F, 180F, 0F),
                        localScale = new Vector3(0.41483F, 0.41483F, 0.41483F)
                    }
                }
            );
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0F, 1.29692F, -1.88241F),
                        localAngles = new Vector3(9.64517F, 0F, 0F),
                        localScale = new Vector3(3.26173F, 3.26173F, 3.26173F)
                    }
                }
            );
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0F, 0.15611F, 0.12046F),
                        localAngles = new Vector3(0F, 180F, 0F),
                        localScale = new Vector3(0.47097F, 0.47097F, 0.47097F)
                    }
                }
            );
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Backpack",
                        localPos = new Vector3(-0.00001F, -0.00002F, -0.11468F),
                        localAngles = new Vector3(0F, 0F, 0F),
                        localScale = new Vector3(0.46085F, 0.46085F, 0.46085F)
                    }
                }
            );
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0.00001F, -0.00002F, 0.14938F),
                        localAngles = new Vector3(0F, 180F, 0F),
                        localScale = new Vector3(0.48097F, 0.48097F, 0.48097F)
                    }
                }
            );
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0.0005F, 0.15688F, 0.00698F),
                        localAngles = new Vector3(17.71065F, 184.0952F, 0F),
                        localScale = new Vector3(0.37196F, 0.37196F, 0.37196F)
                    }
                }
            );
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0F, 0.0676F, 0.24136F),
                        localAngles = new Vector3(0F, 180F, 0F),
                        localScale = new Vector3(0.64434F, 0.64434F, 0.64434F)
                    }
                }
            );
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0.36094F, 0.09642F, -0.01731F),
                        localAngles = new Vector3(84.58131F, 0F, 0F),
                        localScale = new Vector3(0.48733F, 0.48733F, 0.48733F)
                    }
                }
            );
            rules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(0F, -0.11362F, 0F),
                        localAngles = new Vector3(274.4669F, 180F, 180F),
                        localScale = new Vector3(0.34046F, 0.34046F, 0.34046F)
                    }
                }
            );
            rules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Chest",
                        localPos = new Vector3(-0.00001F, 0.20203F, -0.08407F),
                        localAngles = new Vector3(53.98211F, 50.47853F, 44.43427F),
                        localScale = new Vector3(0.37223F, 0.37223F, 0.37223F)
                    }
                }
            );
            #endregion

            return rules;
        }

        public void CreateNautilusBuff()
        {
            BuffDef nautilusBuff = ScriptableObject.CreateInstance<BuffDef>();
            nautilusBuff.buffColor = new Color(1f, 0.35f, 0.8f);
            nautilusBuff.canStack = true;
            nautilusBuff.isDebuff = true;
            nautilusBuff.name = "Shimmering Nautilus stacks";
            nautilusBuff.isHidden = false;
            nautilusBuff.isCooldown = false;
            nautilusBuff.iconSprite = Main.Assets.LoadAsset<Sprite>("Assets/icons/nautilusBuff.png");
            ContentAddition.AddBuffDef(nautilusBuff);

            NautilusBuff = nautilusBuff;
        }
    }
}