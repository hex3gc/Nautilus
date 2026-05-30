using RoR2;
using Nautilus.Configuration;
using System;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine.PlayerLoop;

namespace Nautilus.Items
{
    public static partial class ItemInit
    {
        public static ProtonPop ProtonPop = new ProtonPop
        (
            "ProtonPop",
            [ItemTag.Utility, ItemTag.CanBeTemporary],
            ItemTier.VoidTier1
        );
    }

    /// <summary>
    ///     // Ver.1
    ///     Speed is most needed early in the game so you can travel through stages efficiently, which Proton Pop gives you a shortcut for
    ///     This void item being consumable means you can still get Energy Drinks later on in the run
    /// </summary>
    public class ProtonPop : ItemBase
    {
        public override bool Enabled => ProtonPop_Enabled.Value;
        public override ItemDef ConversionItemDef => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/SprintBonus/SprintBonus.asset").WaitForCompletion();
        public override GameObject itemPrefab => OverwritePrefabMaterials();
        public Material material0 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/TreasureCacheVoid/matLockboxVoidEgg.mat").WaitForCompletion();
        public Material material1 => Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VendingMachine/matVendingMachineGlass.mat").WaitForCompletion();
        public Material material2 => Addressables.LoadAssetAsync<Material>("RoR2/Base/ShrineBlood/matShrineBloodSandy 1.mat").WaitForCompletion();
        public Material material3 => Addressables.LoadAssetAsync<Material>("RoR2/Base/ShrineBlood/matShrineBloodSandy 1.mat").WaitForCompletion();
        public Material material4 => Addressables.LoadAssetAsync<Material>("RoR2/DLC3/matDroneVendorBeamGlow.mat").WaitForCompletion();
        public override Sprite itemIcon => Main.Assets.LoadAsset<Sprite>("Assets/icons/protonPop.png");
        public BuffDef ProtonPopBuff;

        public ProtonPop(string _name, ItemTag[] _tags, ItemTier _tier, bool _canRemove = true, bool _isConsumed = false, bool _hidden = false) : 
        base(_name, _tags, _tier, _canRemove, _isConsumed, _hidden){}

        // Config
        public static ConfigItem<bool> ProtonPop_Enabled = new ConfigItem<bool>
        (
            "Void common: Proton Pop",
            "Item enabled",
            "Should this item appear in runs?",
            true
        );
        public static ConfigItem<float> ProtonPop_MovementSpeed = new ConfigItem<float>
        (
            "Void common: Proton Pop",
            "Movement speed gain",
            "Fractional movement speed boost, non-stacking.",
            0.34f,
            0.10f,
            0.68f,
            0.01f
        );
        public static ConfigItem<float> ProtonPop_MinutesDuration = new ConfigItem<float>
        (
            "Void common: Proton Pop",
            "Minutes duration",
            "Minutes that a single stack of this item will last.",
            10f,
            1f,
            30f,
            1f
        );
        public static ConfigItem<bool> ProtonPop_Recipe = new ConfigItem<bool>
        (
            "Void common: Proton Pop",
            "Recipe enabled",
            "Should this item have a custom corruption recipe?",
            true
        );
        public static ConfigItem<string> ProtonPop_Ingredient1 = new ConfigItem<string>
        (
            "Void common: Proton Pop",
            "Recipe ingredient 1",
            "First ingredient for corruption recipe",
            "SprintBonus"
        );
        public static ConfigItem<string> ProtonPop_Ingredient2 = new ConfigItem<string>
        (
            "Void common: Proton Pop",
            "Recipe ingredient 2",
            "Second ingredient for corruption recipe",
            "PreModernRations"
        );

        public GameObject OverwritePrefabMaterials()
        {
            GameObject ret = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/protonPop.prefab");

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
            string pickupToken = ItemDef.pickupToken;

            LanguageAPI.AddOverlay
            (
                descriptionToken,
                String.Format
                (
                    Language.currentLanguage.GetLocalizedStringByToken(descriptionToken),
                    ProtonPop_MovementSpeed.Value * 100f,
                    ProtonPop_MinutesDuration.Value
                )
            );

            LanguageAPI.AddOverlay
            (
                pickupToken,
                String.Format
                (
                    Language.currentLanguage.GetLocalizedStringByToken(pickupToken),
                    ProtonPop_MinutesDuration.Value
                )
            );
        }

        // Hooks
        public override void RegisterHooks()
        {
            CreateProtonPopBuff();

            // Add/remove Proton Pop trackers on inventory change
            On.RoR2.Inventory.GiveItemPermanent_ItemIndex_int += (orig, self, itemIndex, count) =>
            {
                if (!Run.instance || !Run.instance.gameObject)
                {
                    orig(self, itemIndex, count);
                    return;
                }

                ProtonPopTrackingBehavior behavior = Run.instance.gameObject.GetComponent<ProtonPopTrackingBehavior>();
                int pops = 0;
                int popsAfter = 0;
                int difference = 0;

                pops = self.GetItemCountPermanent(ItemIndex);

                orig(self, itemIndex, count);

                popsAfter = self.GetItemCountPermanent(ItemIndex);
                difference = Math.Abs(pops - popsAfter);

                if ((pops > 0 || popsAfter > 0) && !behavior)
                {
                    behavior = Run.instance.gameObject.AddComponent<ProtonPopTrackingBehavior>();
                }

                if (difference > 0)
                {
                    CharacterMaster characterMaster = self.gameObject.GetComponentInChildren<CharacterMaster>();
                    if (characterMaster)
                    {
                        CharacterBody body = characterMaster.gameObject.GetComponent<CharacterBody>();
                        if (body)
                        {
                            body.RecalculateStats();
                        }

                        if (popsAfter > pops)
                        {
                            for (int i = 0; i < difference; i++)
                            {
                                behavior.AddPop(characterMaster);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < difference; i++)
                            {
                                behavior.RemovePop(characterMaster);
                            }
                        }
                    }
                }
            };

            // Speed boost
            RecalculateStatsAPI.GetStatCoefficients += (orig, self) =>
            {
                int itemCount = GetItemCountEffective(orig);
                if (itemCount > 0)
                {
                    self.moveSpeedMultAdd += ProtonPop_MovementSpeed.Value;
                }
            };
        }

        // Recipes
        public override void AddCorruptionRecipe()
        {
            if (ProtonPop_Recipe.Value == true)
            {
                ItemInit.MakeCorruptionRecipe
                (
                    ProtonPop_Ingredient1.Value,
                    ProtonPop_Ingredient2.Value,
                    ItemDef.name
                );
            }
        }

        public void CreateProtonPopBuff()
        {
            BuffDef protonPopBuff = ScriptableObject.CreateInstance<BuffDef>();
            protonPopBuff.buffColor = new Color(1f, 1f, 1f);
            protonPopBuff.canStack = true;
            protonPopBuff.isDebuff = false;
            protonPopBuff.ignoreGrowthNectar = true;
            protonPopBuff.name = "Proton Pop minutes remaining";
            protonPopBuff.isHidden = false;
            protonPopBuff.isCooldown = false;
            protonPopBuff.iconSprite = Main.Assets.LoadAsset<Sprite>("Assets/icons/protonPopBuff.png");
            ContentAddition.AddBuffDef(protonPopBuff);

            ProtonPopBuff = protonPopBuff;
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
                        childName = "Root",
                        localPos = new Vector3(-0.50291F, 1.0241F, -0.02445F),
                        localAngles = new Vector3(1.12361F, 357.7826F, 0.08149F),
                        localScale = new Vector3(0.44898F, 0.44898F, 0.44898F)
                    }
                }
            );
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Base",
                        localPos = new Vector3(-0.70911F, 0.13947F, 0.12403F),
                        localAngles = new Vector3(273.005F, 356.2288F, 1.55436F),
                        localScale = new Vector3(0.44898F, 0.44898F, 0.44898F)
                    }
                }
            );
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "ROOT",
                        localPos = new Vector3(-0.50291F, 1.0241F, -0.02445F),
                        localAngles = new Vector3(1.12361F, 357.7826F, 0.08149F),
                        localScale = new Vector3(0.44898F, 0.44898F, 0.44898F)
                    }
                }
            );
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Base",
                        localPos = new Vector3(4.0507F, 0.61227F, 0.14396F),
                        localAngles = new Vector3(1.12361F, 357.7826F, 0.08149F),
                        localScale = new Vector3(2.766F, 2.766F, 2.766F)
                    }
                }
            );
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Base",
                        localPos = new Vector3(-0.7008F, 0.21449F, -0.04105F),
                        localAngles = new Vector3(270.6327F, 350.39F, 7.39149F),
                        localScale = new Vector3(0.44898F, 0.44898F, 0.44898F)
                    }
                }
            );
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Base",
                        localPos = new Vector3(-0.63293F, -0.14847F, -0.05261F),
                        localAngles = new Vector3(271.1349F, 181.8962F, 175.884F),
                        localScale = new Vector3(0.44898F, 0.44898F, 0.44898F)
                    }
                }
            );
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Base",
                        localPos = new Vector3(-0.71436F, 0.03677F, -0.05209F),
                        localAngles = new Vector3(270.2447F, 338.3846F, 19.39666F),
                        localScale = new Vector3(0.44898F, 0.44898F, 0.44898F)
                    }
                }
            );
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Base",
                        localPos = new Vector3(-1.41519F, 0.20846F, -0.07572F),
                        localAngles = new Vector3(271.8097F, 180.3604F, 177.4193F),
                        localScale = new Vector3(0.65265F, 0.65265F, 0.65265F)
                    }
                }
            );
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Base",
                        localPos = new Vector3(-0.7817F, 0.00953F, 0.07237F),
                        localAngles = new Vector3(271.0155F, 353.1818F, 4.59997F),
                        localScale = new Vector3(0.44898F, 0.44898F, 0.44898F)
                    }
                }
            );
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Base",
                        localPos = new Vector3(-5.24421F, 0.44096F, -0.20175F),
                        localAngles = new Vector3(89.33855F, 4.85567F, 7.07514F),
                        localScale = new Vector3(3.58952F, 3.58952F, 3.58952F)
                    }
                }
            );
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Base",
                        localPos = new Vector3(-0.57687F, -0.31699F, -0.05345F),
                        localAngles = new Vector3(270.7039F, 351.1367F, 6.64478F),
                        localScale = new Vector3(0.44898F, 0.44898F, 0.44898F)
                    }
                }
            );
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Root",
                        localPos = new Vector3(-0.6383F, 1.02351F, -0.01095F),
                        localAngles = new Vector3(1.12361F, 357.7826F, 0.08149F),
                        localScale = new Vector3(0.44898F, 0.44898F, 0.44898F)
                    }
                }
            );
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Base",
                        localPos = new Vector3(-0.50075F, 0.03139F, -0.04387F),
                        localAngles = new Vector3(70.85766F, 358.7066F, 358.5321F),
                        localScale = new Vector3(0.44898F, 0.44898F, 0.44898F)
                    }
                }
            );
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Base",
                        localPos = new Vector3(-0.50291F, 1.0241F, -0.02445F),
                        localAngles = new Vector3(1.12361F, 357.7826F, 0.08149F),
                        localScale = new Vector3(0.44898F, 0.44898F, 0.44898F)
                    }
                }
            );
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Root",
                        localPos = new Vector3(1.17173F, 1.73062F, 0.45485F),
                        localAngles = new Vector3(1.12361F, 357.7826F, 0.08149F),
                        localScale = new Vector3(0.64846F, 0.64846F, 0.64846F)
                    }
                }
            );
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Root",
                        localPos = new Vector3(0.78279F, 1.02805F, -0.08314F),
                        localAngles = new Vector3(1.12361F, 357.7826F, 0.08149F),
                        localScale = new Vector3(0.64648F, 0.64648F, 0.64648F)
                    }
                }
            );
            rules.Add("mdlDroneTech", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Root",
                        localPos = new Vector3(-0.48705F, 1.02892F, -0.26952F),
                        localAngles = new Vector3(1.12361F, 357.7826F, 0.08149F),
                        localScale = new Vector3(0.51644F, 0.51644F, 0.51644F)
                    }
                }
            );
            rules.Add("mdlDrifter", new RoR2.ItemDisplayRule[]{new RoR2.ItemDisplayRule{
                        ruleType = ItemDisplayRuleType.ParentedPrefab,
                        followerPrefab = ItemDisplayPrefab,
                        childName = "Base",
                        localPos = new Vector3(-0.30506F, 0.10867F, -0.54551F),
                        localAngles = new Vector3(22.92687F, 34.85313F, 161.5925F),
                        localScale = new Vector3(0.5647F, 0.5647F, 0.5647F)
                    }
                }
            );
            #endregion

            return rules;
        }
    }

    public class ProtonPopTrackingBehavior : NetworkBehaviour
    {
        // Attach to a Run.Instance
        // List of tuples with a CharacterMaster and a Run.FixedTimeStamp
        // When a Proton Pop is added, create a new tuple with a 10 minute timestamp
        // When a Proton Pop is removed, remove the earliest timestamp
        // Update every 5 seconds: Set buff count to minutes remaining in the earliest timestamp, if 0 time remaining, remove a Proton Pop

        BuffDef buffDef;
        public List<Tuple<CharacterMaster, Run.FixedTimeStamp>> timestamps = new();
        private List<int> indicesToRemove = new();
        private List<CharacterMaster> alreadyTouchedMasters = new();
        float checkInterval = 2f;
        float checkTimer = 0f;

        public static int Tix2Minutes(float t)
        {
            float floatMins = t / 60;
            return (int)Math.Ceiling(floatMins);
        }

        private int GetTimestampIndexFromMaster(CharacterMaster characterMaster)
        {
            foreach (Tuple<CharacterMaster, Run.FixedTimeStamp> timestamp in timestamps)
            {
                if (timestamp.Item1 == characterMaster)
                {
                    return timestamps.IndexOf(timestamp);
                }
            }

            return -1;
        }

        public void RemovePop(CharacterMaster characterMaster)
        {
            int index = GetTimestampIndexFromMaster(characterMaster);
            if (index >= 0)
            {
                timestamps.RemoveAt(index);
            }

            int firstRemainingIndex = GetTimestampIndexFromMaster(characterMaster);
            if (firstRemainingIndex >= 0)
            {
                SetMasterBuff(characterMaster, Tix2Minutes(timestamps[firstRemainingIndex].Item2.timeUntil));
            }
            else
            {
                SetMasterBuff(characterMaster, 0);
            }
        }

        public void AddPop(CharacterMaster characterMaster)
        {
            timestamps.Add(new Tuple<CharacterMaster, Run.FixedTimeStamp>(characterMaster, Run.FixedTimeStamp.now + ProtonPop.ProtonPop_MinutesDuration.Value * 60));
        }

        public void SetMasterBuff(CharacterMaster characterMaster, int minutes)
        {
            CharacterBody body = characterMaster.GetBody();
            if (body)
            {
                body.SetBuffCount(buffDef.buffIndex, minutes);
            }
        }

        void Awake()
        {
            buffDef = ItemInit.ProtonPop.ProtonPopBuff;
        }

        void FixedUpdate()
        {
            checkTimer += Time.fixedDeltaTime;
            if (checkTimer >= checkInterval)
            {
                // Update pops
                for (int i = 0; i < timestamps.Count; i++)
                {
                    if (timestamps[i].Item2.hasPassed)
                    {
                        indicesToRemove.Add(i);
                    }
                }
                foreach(int index in indicesToRemove)
                {
                    if (timestamps[index].Item1 != null)
                    {
                        CharacterMasterNotificationQueue.SendTransformNotification(timestamps[index].Item1, ItemInit.ProtonPop.ItemIndex, ItemInit.ProtonPopConsumed.ItemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                        timestamps[index].Item1.inventory.GiveItemPermanent(ItemInit.ProtonPopConsumed.ItemIndex);
                        timestamps[index].Item1.inventory.RemoveItemPermanent(ItemInit.ProtonPop.ItemIndex);
                    }
                    else
                    {
                        timestamps.RemoveAt(index);
                    }
                }
                indicesToRemove.Clear();

                // Set buff counts
                foreach (Tuple<CharacterMaster, Run.FixedTimeStamp> timestamp in timestamps)
                {
                    if (!alreadyTouchedMasters.Contains(timestamp.Item1))
                    {
                        alreadyTouchedMasters.Add(timestamp.Item1);
                        SetMasterBuff(timestamp.Item1, Tix2Minutes(timestamp.Item2.timeUntil));
                    }
                }
                alreadyTouchedMasters.Clear();

                checkTimer = 0f;
            }
        }
    }
}