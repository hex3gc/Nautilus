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
            [ItemTag.Damage, ItemTag.Healing, ItemTag.AIBlacklist, ItemTag.ExtractorUnitBlacklist, ItemTag.BrotherBlacklist],
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