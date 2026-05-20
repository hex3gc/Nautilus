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
        public static ObserversEye ObserversEye = new ObserversEye
        (
            "ObserversEye",
            [ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.CannotSteal, ItemTag.ExtractorUnitBlacklist, ItemTag.CanBeTemporary],
            ItemTier.VoidBoss
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Functional Coupler can be preferable when you want the utility of multiple equipments (e.g using a Recycler with an Eccentric Vase)
    ///     Observer's Optics acts as a good opposite of Coupler since it lets you maximize a single equipment, and forces you to save your charges for important moments
    /// </summary>
    public class ObserversEye : ItemBase
    {
        public override bool Enabled => ObserversEye_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC3/Items/ExtraEquipment/ExtraEquipment.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/observersEye.png");
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/Base/BonusGoldPackOnKill/matTomeGold.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VendingMachine/matVendingMachineGlass.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/Base/ShrineBoss/matShrineBoss.mat").WaitForCompletion();
        public Material material3 => Addressables.LoadAssetAsync<Material>("RoR2/DLC3/Items/ShockDamageAura/matDroneShockDamageGlass.mat").WaitForCompletion();
        public Material material4 => Addressables.LoadAssetAsync<Material>("RoR2/DLC2/Seeker/matSeekerGlass.mat").WaitForCompletion();
        public Material material5 => Addressables.LoadAssetAsync<Material>("RoR2/DLC2/Items/AttackSpeedPerNearbyAllyOrEnemy/matRageCrystalGlass.mat").WaitForCompletion();
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

        public ObserversEye(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) :
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden)
        { }

        // Config
        public static ConfigItem<bool> ObserversEye_Enabled = new ConfigItem<bool>
        (
            "Void boss: Observers Optics",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<int> ObserversEye_Activations = new ConfigItem<int>
        (
            "Void boss: Observers Optics",
            "Additional equipment activations",
            "How many extra times is your equipment activated?",
            2,
            1f,
            5f,
            1f
        );
        public static ConfigItem<int> ObserversEye_ActivationsStack = new ConfigItem<int>
        (
            "Void boss: Observers Optics",
            "Additional equipment activations (per stack)",
            "How many extra times is your equipment activated, per additional stack?",
            2,
            1f,
            5f,
            1f
        );
        public static ConfigItem<float> ObserversEye_EquipCooldownMult = new ConfigItem<float>
        (
            "Void boss: Observers Optics",
            "Equipment cooldown mult",
            "What multiplier should be used for equipment cooldown?",
            2f,
            0.1f,
            5f,
            0.1f
        );
        public static ConfigItem<bool> ObserversEye_Recipe = new ConfigItem<bool>
        (
            "Void boss: Observers Optics",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> ObserversEye_Ingredient1 = new ConfigItem<string>
        (
            "Void boss: Observers Optics",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "ExtraEquipment"
        );
        public static ConfigItem<string> ObserversEye_Ingredient2 = new ConfigItem<string>
        (
            "Void boss: Observers Optics",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "EquipmentMagazineVoid"
        );
        public static ConfigItem<int> ObserversEye_DeployablesFMP = new ConfigItem<int>
        (
            "Void boss: Observers Optics",
            "Deployable limit (Forgive Me Please)",
            "Maximum number of Forgive Me Please deployable at a time. Vanilla = 3",
            10,
            1f,
            20f,
            1f
        );
        public static ConfigItem<int> ObserversEye_DeployablesGummy = new ConfigItem<int>
        (
            "Void boss: Observers Optics",
            "Deployable limit (Goobo Jr)",
            "Maximum number of Goobo Jr. deployable at a time. Vanilla = 3",
            10,
            1f,
            20f,
            1f
        );
        public static ConfigItem<int> ObserversEye_DeployablesVending = new ConfigItem<int>
        (
            "Void boss: Observers Optics",
            "Deployable limit (Remote Caffeinator)",
            "Maximum number of Remote Caffeinators deployable at a time. Vanilla = 1",
            10,
            1f,
            20f,
            1f
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/observersEye.prefab");

            Material[] materials =
            {
                material0,
                material1,
                material2,
                material3,
                material4,
                material5
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
                    ObserversEye_Activations.Value,
                    ObserversEye_ActivationsStack.Value,
                    ObserversEye_EquipCooldownMult.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            // Add/remove behavior on inventory change
            On.RoR2.CharacterBody.OnInventoryChanged += (orig, self) =>
            {
                orig(self);

                ObserversEyeBehavior behavior = self.GetComponent<ObserversEyeBehavior>();
                int itemCount = GetItemCountEffective(self);

                if (GetItemCountEffective(self) > 0 && !behavior)
                {
                    behavior = self.AddItemBehavior<ObserversEyeBehavior>(itemCount);
                    behavior.activations = ObserversEye_Activations.Value;
                    behavior.activationsStack = ObserversEye_ActivationsStack.Value;
                }

                if (behavior)
                {
                    behavior.stack = itemCount;
                }

                if (GetItemCountEffective(self) <= 0 && behavior)
                {
                    UnityEngine.Object.Destroy(self.GetComponent<ObserversEyeBehavior>());
                }
            };

            // Equipment multi trigger
            On.RoR2.EquipmentSlot.OnEquipmentExecuted_byte_byte_EquipmentIndex += (orig, self, slot, set, index) =>
            {
                orig(self, slot, set, index);

                ObserversEyeBehavior behavior = self.GetComponent<ObserversEyeBehavior>();

                if (behavior)
                {
                    behavior.AddToQueue(new Tuple<EquipmentSlot, EquipmentIndex>(self, index));
                }
            };

            // Cooldown increase
            On.RoR2.Inventory.CalculateEquipmentCooldownScale += (orig, self) =>
            {
                float result = orig(self);

                if (self.GetItemCountEffective(ItemIndex) > 0)
                {
                    result = Mathf.Pow(ObserversEye_EquipCooldownMult.Value, self.GetItemCountEffective(ItemIndex));
                }

                return result;
            };

            // Increase limit for deployable equipment
            On.RoR2.CharacterMaster.GetDeployableSameSlotLimit += (orig, self, slot) =>
            {
                switch (slot)
                {
                    case DeployableSlot.DeathProjectile:
                        return ObserversEye_DeployablesFMP.Value;
                    case DeployableSlot.GummyClone:
                        return ObserversEye_DeployablesGummy.Value;
                    case DeployableSlot.VendingMachine:
                        return ObserversEye_DeployablesVending.Value;
                }

                return orig(self, slot);
            };
        }

        // Recipe
        public override void AddCorruptionRecipe()
        {
            if (ObserversEye_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    ObserversEye_Ingredient1.Value,
                    ObserversEye_Ingredient2.Value,
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

    public class ObserversEyeBehavior : CharacterBody.ItemBehavior
    {
        public int activations = 2;
        public int activationsStack = 2;
        public float equipInterval = 0.5f;
        public float bfgInterval = 2.2f;
        public float equipTimer = 0f;
        public List<Tuple<EquipmentSlot, EquipmentIndex>> equipQueue = new();

        public void AddToQueue(Tuple<EquipmentSlot, EquipmentIndex> tuple)
        {
            int timesToAdd = activations + (activationsStack * (stack - 1));

            for (int i = 0; i < timesToAdd; i++)
            {
                equipQueue.Add(tuple);
            }
        }

        void FixedUpdate()
        {
            if (equipQueue.Count > 0)
            {
                equipTimer += Time.fixedDeltaTime;
                
                if (equipQueue.First().Item2 == RoR2Content.Equipment.BFG.equipmentIndex)
                {
                    if (equipTimer > bfgInterval)
                    {
                        var pair = equipQueue.First();
                        if (equipQueue.Count >= 1)
                        {
                            Transform transform = pair.Item1.FindActiveEquipmentDisplay();
                            if ((bool)transform)
                            {
                                Animator componentInChildren = transform.GetComponentInChildren<Animator>();
                                if ((bool)componentInChildren)
                                {
                                    componentInChildren.SetTrigger("Fire");
                                }
                            }
                        }

                        pair.Item1.PerformEquipmentAction(EquipmentCatalog.GetEquipmentDef(pair.Item2));
                        equipQueue.RemoveAt(0);

                        equipTimer = 0f;
                    }
                }
                else
                {
                    if (equipTimer > equipInterval)
                    {
                        var pair = equipQueue.First();
                        pair.Item1.PerformEquipmentAction(EquipmentCatalog.GetEquipmentDef(pair.Item2));
                        equipQueue.RemoveAt(0);

                        equipTimer = 0f;
                    }
                }
            }
        }
    }
}