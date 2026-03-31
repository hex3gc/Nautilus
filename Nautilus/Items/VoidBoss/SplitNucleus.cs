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
        public static SplitNucleus SplitNucleus = new SplitNucleus
        (
            "SplitNucleus",
            [ItemTag.Utility],
            ItemTier.VoidBoss
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Makes Defense Nucleus an actual defensive item while keeping the 'on elite kill' condition
    ///     Also provides some additional one-shot protection, which can help significantly with collapse and literally the entire game because the vanilla damage scaling is unbelievable
    ///     Has some utility when it comes to blood shrines, void potentials and void cradles
    /// </summary>
    public class SplitNucleus : ItemBase
    {
        public override bool Enabled => SplitNucleus_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/MinorConstructOnKill/MinorConstructOnKill.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/splitNucleus.png");
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/Common/Void/matNullifierGemPortal3.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/Base/ShrineChance/matShrineChance.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/Common/Void/matNullifierGemPortal3.mat").WaitForCompletion();
        public Material material3 => Addressables.LoadAssetAsync<Material>("RoR2/DLC2/matShrineColossusAccessProngDisabled.mat").WaitForCompletion();
        public Material material4 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidSurvivor/matVoidSurvivorBlasterSphereAreaIndicatorCorrupted.mat").WaitForCompletion();
        public BuffDef NucleusBuff;
        private GameObject _blockPrefab;
        public GameObject BlockPrefab
        {
            get
            {
                if (_blockPrefab == null)
                {
                    _blockPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/MajorAndMinorConstruct/MajorConstructSpawnMinorConstructEffect.prefab").WaitForCompletion();
                }
                return _blockPrefab;
            }
            set;
        }
        private GameObject _blockPrefabFlash;
        public GameObject BlockPrefabFlash
        {
            get
            {
                if (_blockPrefabFlash == null)
                {
                    _blockPrefabFlash = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/MajorAndMinorConstruct/MajorConstructMuzzleflashSpawnMinorConstruct.prefab").WaitForCompletion();
                }
                return _blockPrefabFlash;
            }
            set;
        }
        private Material _blockOverlay;
        public Material BlockOverlay
        {
            get
            {
                if (_blockOverlay == null)
                {
                    _blockOverlay = Addressables.LoadAssetAsync<Material>("RoR2/Base/MagmaWorm/matMagmaWormOverlay.mat").WaitForCompletion();
                }
                return _blockOverlay;
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

        public SplitNucleus(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> SplitNucleus_Enabled = new ConfigItem<bool>
        (
            "Void boss: Split Nucleus",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> SplitNucleus_ActivateFraction = new ConfigItem<float>
        (
            "Void boss: Split Nucleus",
            "Activation health fraction",
            "What fraction of health must an attack take in order to be blocked?",
            0.5f,
            0.05f,
            0.95f,
            0.05f
        );
        public static ConfigItem<int> SplitNucleus_MaxStacks = new ConfigItem<int>
        (
            "Void boss: Split Nucleus",
            "Maximum block stacks",
            "Amount of high-damage attacks that can be blocked.",
            2,
            1f,
            4f,
            1f
        );
        public static ConfigItem<int> SplitNucleus_MaxStacksStack = new ConfigItem<int>
        (
            "Void boss: Split Nucleus",
            "Maximum block stacks (per stack)",
            "Amount of high-damage attacks that can be blocked, per additional stack.",
            2,
            1f,
            4f,
            1f
        );
        public static ConfigItem<int> SplitNucleus_ChargesOnEliteKill = new ConfigItem<int>
        (
            "Void boss: Split Nucleus",
            "Charges regained on elite kill",
            "Number of block charges gained on elite kill.",
            1,
            1f,
            4f,
            1f
        );
        public static ConfigItem<bool> SplitNucleus_Recipe = new ConfigItem<bool>
        (
            "Void boss: Split Nucleus",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> SplitNucleus_Ingredient1 = new ConfigItem<string>
        (
            "Void boss: Split Nucleus",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "MinorConstructOnKill"
        );
        public static ConfigItem<string> SplitNucleus_Ingredient2 = new ConfigItem<string>
        (
            "Void boss: Split Nucleus",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "ChainLightningVoid"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/splitNucleus.prefab");

            Material[] materials =
            {
                material0,
                material1,
                material2,
                material3,
                material4
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
                    SplitNucleus_MaxStacks.Value,
                    SplitNucleus_MaxStacksStack.Value,
                    SplitNucleus_ActivateFraction.Value * 100f,
                    SplitNucleus_ChargesOnEliteKill.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            CreateNucleusBuff();

            // Prevent damage
            On.RoR2.HealthComponent.TakeDamageProcess += (orig, self, damageInfo) =>
            {
                if (self.body && GetItemCountEffective(self.body) > 0 && self.body.gameObject.TryGetComponent(out SplitNucleusBehavior splitNucleusBehavior) && damageInfo.damage >= self.fullCombinedHealth * SplitNucleus_ActivateFraction.Value)
                {
                    if (splitNucleusBehavior.LoseStack())
                    {
                        damageInfo.rejected = true;
                    }
                }

                orig(self, damageInfo);
            };

            // Add/remove behavior on inventory change
            On.RoR2.CharacterBody.OnInventoryChanged += (orig, self) =>
            {
                orig(self);

                SplitNucleusBehavior behavior = self.gameObject.GetComponent<SplitNucleusBehavior>();
                int itemCount = GetItemCountEffective(self);

                if (GetItemCountEffective(self) > 0 && !behavior)
                {
                    behavior = self.gameObject.AddComponent<SplitNucleusBehavior>();
                }

                if (GetItemCountEffective(self) <= 0 && behavior)
                {
                    UnityEngine.Object.Destroy(self.gameObject.GetComponent<SplitNucleusBehavior>());
                    self.SetBuffCount(NucleusBuff.buffIndex, 0);
                }
            };

            // Gain stacks on kill
            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, self, damageReport) =>
            {
                if (damageReport.attackerBody && damageReport.victimBody && GetItemCountEffective(damageReport.attackerBody) > 0 && damageReport.victimIsElite && damageReport.attackerBody.gameObject.TryGetComponent(out SplitNucleusBehavior splitNucleusBehavior))
                {
                    splitNucleusBehavior.GainStacks();
                }

                orig(self, damageReport);
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (SplitNucleus_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    SplitNucleus_Ingredient1.Value,
                    SplitNucleus_Ingredient2.Value,
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

        public void CreateNucleusBuff()
        {
            BuffDef nucleusBuff = ScriptableObject.CreateInstance<BuffDef>();
            nucleusBuff.buffColor = new Color(1f, 0.772f, 0.396f);
            nucleusBuff.canStack = true;
            nucleusBuff.isDebuff = false;
            nucleusBuff.ignoreGrowthNectar = true;
            nucleusBuff.name = "Split Nucleus charges";
            nucleusBuff.isHidden = false;
            nucleusBuff.isCooldown = false;
            nucleusBuff.iconSprite = Main.Assets.LoadAsset<Sprite>("Assets/icons/nucleusBuff.png");
            ContentAddition.AddBuffDef(nucleusBuff);

            NucleusBuff = nucleusBuff;
        }

        public class SplitNucleusBehavior : MonoBehaviour
        {
            HealthComponent healthComponent;
            CharacterBody characterBody;
            BuffDef buffDef;
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
                    if (_stacks > maxStacks)
                    {
                        _stacks = maxStacks;
                    }
                    characterBody.SetBuffCount(buffDef.buffIndex, _stacks);
                }
            }
            public int maxStacks = 0;
            public float pulseTimer = 0f;
            public float pulseInterval = 1f;

            void Awake()
            {
                healthComponent = gameObject.GetComponent<HealthComponent>();
                characterBody = gameObject.GetComponent<CharacterBody>();
                buffDef = ItemInit.SplitNucleus.NucleusBuff;

                if (!healthComponent || !characterBody || !buffDef)
                {
                    Destroy(this);
                }

                RecheckMaxStacks();
                stacks = maxStacks;
            }

            void FixedUpdate()
            {
                pulseTimer += Time.fixedDeltaTime;

                if (pulseTimer > pulseInterval)
                {
                    if (characterBody.gameObject.GetComponent<ModelLocator>() && _stacks > 0)
                    {
                        TemporaryOverlay temporaryOverlay = characterBody.gameObject.AddComponent<TemporaryOverlay>();
                        temporaryOverlay.duration = 0.5f;
                        temporaryOverlay.animateShaderAlpha = true;
                        temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 0.8f, 1f, 0f);
                        temporaryOverlay.destroyComponentOnEnd = true;
                        temporaryOverlay.originalMaterial = ItemInit.SplitNucleus.BlockOverlay;
                        temporaryOverlay.AddToCharacerModel(characterBody.gameObject.GetComponent<ModelLocator>().modelTransform.GetComponentInParent<CharacterModel>());
                    }

                    pulseTimer = 0f;
                }
            }

            public bool LoseStack()
            {
                if (stacks > 0)
                {
                    stacks -= 1;

                    EffectData effectData = new EffectData()
                    {
                        origin = characterBody.corePosition,
                        scale = 0.5f
                    };
                    EffectManager.SpawnEffect(ItemInit.SplitNucleus.BlockPrefab, effectData, true);

                    EffectData effectData2 = new EffectData()
                    {
                        origin = characterBody.corePosition
                    };
                    EffectManager.SpawnEffect(ItemInit.SplitNucleus.BlockPrefabFlash, effectData2, true);

                    Util.PlaySound("Play_majorConstruct_R_pulse", characterBody.gameObject);

                    if (characterBody.gameObject.GetComponent<ModelLocator>())
                    {
                        TemporaryOverlay temporaryOverlay = characterBody.gameObject.AddComponent<TemporaryOverlay>();
                        temporaryOverlay.duration = 2f;
                        temporaryOverlay.animateShaderAlpha = true;
                        temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                        temporaryOverlay.destroyComponentOnEnd = true;
                        temporaryOverlay.originalMaterial = ItemInit.SplitNucleus.BlockOverlay;
                        temporaryOverlay.AddToCharacerModel(characterBody.gameObject.GetComponent<ModelLocator>().modelTransform.GetComponentInParent<CharacterModel>());
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void GainStacks()
            {
                if (stacks < maxStacks)
                {
                    Util.PlaySound(EntityStates.VoidJailer.Weapon.ChargeFire.attackSoundEffect, characterBody.gameObject);

                    if (characterBody.gameObject.GetComponent<ModelLocator>())
                    {
                        TemporaryOverlay temporaryOverlay = characterBody.gameObject.AddComponent<TemporaryOverlay>();
                        temporaryOverlay.duration = 2f;
                        temporaryOverlay.animateShaderAlpha = true;
                        temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                        temporaryOverlay.destroyComponentOnEnd = true;
                        temporaryOverlay.originalMaterial = ItemInit.SplitNucleus.BlockOverlay;
                        temporaryOverlay.AddToCharacerModel(characterBody.gameObject.GetComponent<ModelLocator>().modelTransform.GetComponentInParent<CharacterModel>());
                    }
                }

                stacks += SplitNucleus_ChargesOnEliteKill.Value;
            }

            public void RecheckMaxStacks()
            {
                maxStacks = SplitNucleus_MaxStacks.Value + SplitNucleus_MaxStacksStack.Value * (characterBody.inventory.GetItemCountEffective(ItemInit.SplitNucleus.ItemIndex) - 1);
            }
        }
    }
}