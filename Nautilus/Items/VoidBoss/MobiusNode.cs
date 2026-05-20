using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static MobiusNode MobiusNode = new MobiusNode
        (
            "MobiusNode",
            [ItemTag.Healing, ItemTag.CanBeTemporary],
            ItemTier.VoidBoss
        );
    }

    /// <summary>
    ///     // Ver.1
    /// </summary>
    public class MobiusNode : ItemBase
    {
        public override bool Enabled => MobiusNode_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/NovaOnLowHealth/NovaOnLowHealth.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/mobiusGland.png");
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/GummyClone/matGummyClone.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidSurvivor/matVoidSurvivorCorruptOverlay.mat").WaitForCompletion();
        public BuffDef MobiusBuff;
        private GameObject _ExplodePrefab;
        public GameObject ExplodePrefab
        {
            get
            {
                if (_ExplodePrefab == null)
                {
                    _ExplodePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidMegaCrab/VoidMegaCrabDeathBombExplosion.prefab").WaitForCompletion();
                }
                return _ExplodePrefab;
            }
            set;
        }
        private Material _ExplodeOverlay;
        public Material ExplodeOverlay
        {
            get
            {
                if (_ExplodeOverlay == null)
                {
                    _ExplodeOverlay = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidSurvivor/matVoidBlinkBodyOverlayCorrupted.mat").WaitForCompletion();
                }
                return _ExplodeOverlay;
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

        public MobiusNode(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> MobiusNode_Enabled = new ConfigItem<bool>
        (
            "Void boss: Mobius Node",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> MobiusNode_ShieldAdd = new ConfigItem<float>
        (
            "Void boss: Mobius Node",
            "Added shield",
            "Fraction of shield to grant on first stack.",
            0.2f,
            0f,
            1f,
            0.05f
        );
        public static ConfigItem<float> MobiusNode_Threshold = new ConfigItem<float>
        (
            "Void boss: Mobius Node",
            "Shield regen threshold",
            "Health fraction below which shield regeneration will activate.",
            0.5f,
            0.1f,
            0.9f,
            0.05f
        );
        public static ConfigItem<float> MobiusNode_Timer = new ConfigItem<float>
        (
            "Void boss: Mobius Node",
            "Replenish interval",
            "All stacks of shield regen are replenished every x seconds.",
            60f,
            5f,
            120f,
            1f
        );
        public static ConfigItem<int> MobiusNode_Stacks = new ConfigItem<int>
        (
            "Void boss: Mobius Node",
            "Stacks of shield regen",
            "Maximum shield regen stacks you can hold.",
            2,
            1f,
            5f,
            1f
        );
        public static ConfigItem<int> MobiusNode_StacksStack = new ConfigItem<int>
        (
            "Void boss: Mobius Node",
            "Stacks of shield regen (per stack)",
            "Maximum shield regen stacks you can hold, per additional stack.",
            1,
            1f,
            5f,
            1f
        );
        public static ConfigItem<float> MobiusNode_ExplodeRadius = new ConfigItem<float>
        (
            "Void boss: Mobius Node",
            "Stun radius",
            "Meters radius in which to stun enemies when shield is recharged.",
            40f,
            1f,
            80f,
            1f
        );
        public static ConfigItem<bool> MobiusNode_Recipe = new ConfigItem<bool>
        (
            "Void boss: Mobius Node",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> MobiusNode_Ingredient1 = new ConfigItem<string>
        (
            "Void boss: Mobius Node",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "NovaOnLowHealth"
        );
        public static ConfigItem<string> MobiusNode_Ingredient2 = new ConfigItem<string>
        (
            "Void boss: Mobius Node",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "MissileVoid"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/mobiusNode.prefab");

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
                    MobiusNode_ShieldAdd.Value * 100f,
                    MobiusNode_Threshold.Value * 100f,
                    MobiusNode_Stacks.Value,
                    MobiusNode_StacksStack.Value,
                    MobiusNode_Timer.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            CreateMobiusBuff();

            // Shield boost
            RecalculateStatsAPI.GetStatCoefficients += (orig, self) =>
            {
                int itemCount = GetItemCountEffective(orig);
                if (itemCount > 0)
                {
                    self.baseShieldAdd += orig.maxHealth * MobiusNode_ShieldAdd.Value;
                }
            };

            // Shield color
            IL.RoR2.HealthComponent.GetHealthBarValues += (il) =>
            {
                ILCursor c = new ILCursor(il);

                c.TryGotoNext(x => x.MatchLdfld<HealthComponent.ItemCounts>(nameof(HealthComponent.ItemCounts.missileVoid)));
                bool hit = c.TryGotoNext
                (
                    MoveType.After,
                    x => x.MatchConvR4(),
                    x => x.MatchLdcR4(out _),
                    x => x.MatchCgt()
                );

                if (hit)
                {
                    c.Emit(OpCodes.Ldarg, 0);
                    c.EmitDelegate<Func<HealthComponent, bool>>((hc) =>
                    {
                        if (hc && hc.body && hc.body.inventory)
                        {
                            return hc.body.inventory.GetItemCountEffective(ItemInit.MobiusNode.ItemIndex) > 0;
                        }
                        else
                        {
                            return false;
                        }
                    });
                    c.Emit(OpCodes.Or);
                }
                else
                {
                    Log.Error("IL hook failed for shield color adjustment.");
                }
            };

            // Regeneration
            On.RoR2.HealthComponent.TakeDamageProcess += (orig, self, damageInfo) =>
            {
                orig(self, damageInfo);

                if (self.body && self.body.gameObject.TryGetComponent(out MobiusBehavior mobiusBehavior) && self.IsHealthBelowThreshold(MobiusNode_Threshold.Value))
                {
                    mobiusBehavior.Explode();
                }
            };

            // Add/remove behavior on inventory change
            On.RoR2.CharacterBody.OnInventoryChanged += (orig, self) =>
            {
                orig(self);

                MobiusBehavior behavior = self.gameObject.GetComponent<MobiusBehavior>();
                int itemCount = GetItemCountEffective(self);

                if (GetItemCountEffective(self) > 0 && !behavior)
                {
                    behavior = self.gameObject.AddComponent<MobiusBehavior>();
                }

                if(GetItemCountEffective(self) <= 0 && behavior)
                {
                    UnityEngine.Object.Destroy(self.gameObject.GetComponent<MobiusBehavior>());
                    self.SetBuffCount(MobiusBuff.buffIndex, 0);
                }
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (MobiusNode_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    MobiusNode_Ingredient1.Value,
                    MobiusNode_Ingredient2.Value,
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
        
        public void CreateMobiusBuff()
        {
            BuffDef mobiusBuff = ScriptableObject.CreateInstance<BuffDef>();
            mobiusBuff.buffColor = new Color(1f, 1f, 1f);
            mobiusBuff.canStack = true;
            mobiusBuff.isDebuff = false;
            mobiusBuff.ignoreGrowthNectar = true;
            mobiusBuff.name = "Möbius regen charges";
            mobiusBuff.isHidden = false;
            mobiusBuff.isCooldown = false;
            mobiusBuff.iconSprite = Main.Assets.LoadAsset<Sprite>("Assets/icons/mobiusBuff.png");
            ContentAddition.AddBuffDef(mobiusBuff);

            MobiusBuff = mobiusBuff;
        }
    }

    public class MobiusBehavior : MonoBehaviour
    {
        HealthComponent healthComponent;
        CharacterBody characterBody;
        BuffDef buffDef;
        public float rechargeInterval = 60f;
        public float rechargeTimer = 0f;
        private int _stacks = 0;
        public int stacks
        {
            get
            {
                return _stacks;
            }
            set
            {
                RecheckMaxStacks();
                
                _stacks = value;
                characterBody.SetBuffCount(buffDef.buffIndex, _stacks);
            }
        }
        public int maxStacks = 0;
        
        void Awake()
        {
            healthComponent = gameObject.GetComponent<HealthComponent>();
            characterBody = gameObject.GetComponent<CharacterBody>();
            buffDef = ItemInit.MobiusNode.MobiusBuff;

            if (!healthComponent || !characterBody || !buffDef)
            {
                Destroy(this);
            }

            RecheckMaxStacks();
            stacks = maxStacks;
        }

        void FixedUpdate()
        {
            if (stacks < maxStacks)
            {
                rechargeTimer += Time.fixedDeltaTime;

                if (rechargeTimer >= rechargeInterval)
                {
                    RegenAllStacks();
                    rechargeTimer = 0f;
                }
            }
        }

        public void Explode()
        {
            if (stacks > 0)
            {
                EffectData effectData = new EffectData()
                {
                    origin = characterBody.corePosition
                };
                EffectManager.SpawnEffect(ItemInit.MobiusNode.ExplodePrefab, effectData, true);

                TemporaryOverlay temporaryOverlay = characterBody.gameObject.AddComponent<TemporaryOverlay>();
                temporaryOverlay.duration = 2f;
                temporaryOverlay.animateShaderAlpha = true;
                temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                temporaryOverlay.destroyComponentOnEnd = true;
                temporaryOverlay.originalMaterial = ItemInit.MobiusNode.ExplodeOverlay;
                temporaryOverlay.AddToCharacerModel(characterBody.gameObject.GetComponent<ModelLocator>().modelTransform.GetComponentInParent<CharacterModel>());

                List<Collider> colliders = Physics.OverlapSphere(characterBody.corePosition, MobiusNode.MobiusNode_ExplodeRadius.Value).ToList();
                foreach(Collider collider in colliders)
                {
                    GameObject gameObject = collider.gameObject;
                    if (gameObject.GetComponentInChildren<CharacterBody>())
                    {
                        CharacterBody colliderBody = gameObject.GetComponentInChildren<CharacterBody>();
                        if (colliderBody.healthComponent && colliderBody.teamComponent && colliderBody.teamComponent.teamIndex != characterBody.teamComponent.teamIndex)
                        {
                            colliderBody.healthComponent.gameObject.TryGetComponent(out SetStateOnHurt setStateOnHurt);
                            if (setStateOnHurt)
                            {
                                setStateOnHurt.SetStun(2f);
                            }
                        }
                    }
                }

                healthComponent.RechargeShieldFull();
                stacks -= 1;
            }
        }

        public void RegenAllStacks()
        {
            RecheckMaxStacks();
            stacks = maxStacks;

            TemporaryOverlay temporaryOverlay = characterBody.gameObject.AddComponent<TemporaryOverlay>();
            temporaryOverlay.duration = 1f;
            temporaryOverlay.animateShaderAlpha = true;
            temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            temporaryOverlay.destroyComponentOnEnd = true;
            temporaryOverlay.originalMaterial = ItemInit.MobiusNode.ExplodeOverlay;
            temporaryOverlay.AddToCharacerModel(characterBody.gameObject.GetComponent<ModelLocator>().modelTransform.GetComponentInParent<CharacterModel>());

            Util.PlaySound("Play_Item_proc_medkit", characterBody.gameObject);
        }

        public void RecheckMaxStacks()
        {
            maxStacks = MobiusNode.MobiusNode_Stacks.Value + MobiusNode.MobiusNode_StacksStack.Value * (characterBody.inventory.GetItemCountEffective(ItemInit.MobiusNode.ItemIndex) - 1);
        }
    }
}