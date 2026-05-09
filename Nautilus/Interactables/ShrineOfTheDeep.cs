using UnityEngine;
using RoR2;
using RoR2.UI;
using RoR2.ExpansionManagement;
using R2API;
using Nautilus.Configuration;
using BepInEx;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using R2API.Utils;
using Nautilus.Items;
using RoR2.ContentManagement;
using System;

namespace Nautilus.Interactables
{
    public static partial class InteractableInit
    {
        public static ShrineOfTheDeep shrineOfTheDeep = new ShrineOfTheDeep();
    }

    public class ShrineOfTheDeep : InteractableBase
    {
        public ShrineOfTheDeep() : 
        base(){ }

        public static ConfigItem<bool> ShrineOfTheDeep_Enabled = new ConfigItem<bool>
        (
            "Interactable: Shrine of the Deep",
            "Boss drops enabled",
            "Enables both the Shrine Of The Deep and the Void Boss system. To remove the shrine only, set its Director Weight to 0.",
            true
        );
        public static ConfigItem<int> ShrineOfTheDeep_DirectorCost = new ConfigItem<int>
        (
            "Interactable: Shrine of the Deep",
            "Director cost",
            "Cost of this interactable for director spawns (30 = large chest cost)",
            30,
            1f,
            60f,
            1f
        );
        public static ConfigItem<int> ShrineOfTheDeep_DirectorWeight = new ConfigItem<int>
        (
            "Interactable: Shrine of the Deep",
            "Director weight",
            "Weight of this interactable against other spawns (2 = boss printer weight)",
            4,
            1f,
            15f,
            1f
        );
        public static ConfigItem<string> ShrineOfTheDeep_ExtraStages = new ConfigItem<string>
        (
            "Interactable: Shrine of the Deep",
            "Additional stages to spawn",
            "(Separate by commas!) The interactable already has spawn definitions for vanilla stages, but you can define extra modded stages here by using their internal names.",
            "observatory_wormsworms, hollowsummit_wormsworms, hollowsummitnight_wormsworms, foggyswampdownpour"
        );
        public static ConfigItem<bool> ShrineOfTheDeep_ZoeaRework = new ConfigItem<bool>
        (
            "Rework: Newly Hatched Zoea",
            "Zoea as Beetle Queen item",
            "Uses ZoeaRework to make Newly Hatched Zoea a void boss item for the Beetle Queen. If you don't want the conversion, set this to false and disable ZoeaRework in your mod profile.",
            true
        );
        public static ConfigItem<bool> ShrineOfTheDeep_ZoeaConversion = new ConfigItem<bool>
        (
            "Rework: Newly Hatched Zoea",
            "Remove void conversions",
            "If you're not using ZoeaRework, this disables all void conversion for Zoea and leaves it solely as the Void Devastator's drop. False by default.",
            false
        );

        private EliteDef _voidEliteDef;
        public EliteDef voidEliteDef
        {
            get
            {
                if (_voidEliteDef == null)
                {
                    _voidEliteDef = Addressables.LoadAssetAsync<EliteDef>("RoR2/DLC1/EliteVoid/edVoid.asset").WaitForCompletion();
                }
                return _voidEliteDef;
            }
            set;
        }
        private static ExplicitPickupDropTable _explicitPickupDropTable;
        public static ExplicitPickupDropTable explicitPickupDropTable
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
                            pickupDef = DLC1Content.Items.VoidMegaCrabItem,
                            pickupWeight = 1
                        }
                    };
                }

                _explicitPickupDropTable.Regenerate(Run.instance);
                return _explicitPickupDropTable;
            }
            set;
        }

        public static GameObject shrinePrefab = null;
        public static InteractableSpawnCard interactableSpawnCard = null;
        public static DirectorCard directorCard = null;

        public override void Init()
        {
            if (ShrineOfTheDeep_Enabled.Value == false)
            {
                return;
            }

            // Shrine interactable
            GameObject gameObject = Main.Assets.LoadAsset<GameObject>("Assets/prefabs/shrineOfTheDeep.prefab");

            Material material = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/voidstage/matVoidAsteroid.mat").WaitForCompletion();
            Material material1 = Addressables.LoadAssetAsync<Material>("RoR2/Base/Gateway/matGatewaySphere.mat").WaitForCompletion();
            Material material2 = Addressables.LoadAssetAsync<Material>("RoR2/Base/Imp/matImpBossPortal.mat").WaitForCompletion();

            gameObject.AddComponent<ShrineOfTheDeepBehavior>();

            gameObject.AddComponent<NetworkIdentity>();
            
            MeshRenderer meshRenderer = gameObject.transform.Find("hand").GetComponent<MeshRenderer>();
            meshRenderer.SetMaterial(material);

            MeshRenderer meshRenderer1 = gameObject.transform.Find("homer simpson").GetComponent<MeshRenderer>();
            meshRenderer1.SetMaterial(material1);

            MeshRenderer meshRenderer2 = gameObject.transform.Find("marge simpson").GetComponent<MeshRenderer>();
            meshRenderer2.SetMaterial(material2);
            
            ExpansionRequirementComponent expansionRequirementComponent = gameObject.AddComponent<ExpansionRequirementComponent>();
            expansionRequirementComponent.requiredExpansion = Main.Expansion;

            InspectDef inspectDef = ScriptableObject.CreateInstance<InspectDef>();
            InspectInfo inspectInfo = inspectDef.Info = new RoR2.UI.InspectInfo();
            inspectInfo.Visual = Main.Assets.LoadAsset<Sprite>("Assets/icons/expansion.png");
            inspectInfo.TitleToken = "NT_INTERACTABLE_SHRINEOFTHEDEEP_NAME";
            inspectInfo.DescriptionToken = "NT_INTERACTABLE_SHRINEOFTHEDEEP_DESCRIPTION";
            inspectDef.Info = inspectInfo;

            GenericInspectInfoProvider genericInspectInfoProvider = gameObject.AddComponent<GenericInspectInfoProvider>();
            genericInspectInfoProvider.InspectInfo = inspectDef;
            
            Highlight highlight = gameObject.AddComponent<Highlight>();
            highlight.pickupState = UniquePickup.none;
            highlight.targetRenderer = meshRenderer;
            highlight.strength = 1f;
            highlight.highlightColor = Highlight.HighlightColor.interactive;

            PurchaseInteraction purchaseInteraction = gameObject.AddComponent<PurchaseInteraction>();
            purchaseInteraction.displayNameToken = "NT_INTERACTABLE_SHRINEOFTHEDEEP_NAME";
            purchaseInteraction.contextToken = "NT_INTERACTABLE_SHRINEOFTHEDEEP_CONTEXT";
            purchaseInteraction.costType = CostTypeIndex.None;
            purchaseInteraction.setUnavailableOnTeleporterActivated = true;
            purchaseInteraction.isShrine = true;

            // PingInfoProvider pingInfoProvider = gameObject.AddComponent<PingInfoProvider>();
            
            EntityLocator entityLocator = gameObject.GetComponentInChildren<CapsuleCollider>().gameObject.AddComponent<EntityLocator>();
            entityLocator.entity = gameObject;

            ModelLocator modelLocator = gameObject.AddComponent<ModelLocator>();
            modelLocator.modelTransform = gameObject.transform.Find("hand");
            modelLocator.modelBaseTransform = modelLocator.modelTransform;
            modelLocator.dontDetatchFromParent = true;
            modelLocator.autoUpdateModelTransform = true;

            Transform fireworksTransform = gameObject.transform;
            fireworksTransform.position = new Vector3(fireworksTransform.position.x, fireworksTransform.position.y + 1.6f, fireworksTransform.position.z);

            ChildLocator childLocator = gameObject.AddComponent<ChildLocator>();
            childLocator.transformPairs = new ChildLocator.NameTransformPair[]
            {
                new ChildLocator.NameTransformPair()
                {
                    name = "FireworkOrigin",
                    transform = fireworksTransform
                }
            };

            shrinePrefab = gameObject;
            PrefabAPI.RegisterNetworkPrefab(shrinePrefab);

            // Shrine spawn card
            interactableSpawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
            interactableSpawnCard.name = "iscShrineOfTheDeep";
            interactableSpawnCard.prefab = shrinePrefab;
            interactableSpawnCard.sendOverNetwork = true;
            interactableSpawnCard.hullSize = HullClassification.Golem;
            interactableSpawnCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
            interactableSpawnCard.requiredFlags = RoR2.Navigation.NodeFlags.None;
            interactableSpawnCard.forbiddenFlags = RoR2.Navigation.NodeFlags.NoShrineSpawn;
            interactableSpawnCard.directorCreditCost = ShrineOfTheDeep_DirectorCost.Value;
            interactableSpawnCard.occupyPosition = true;
            interactableSpawnCard.orientToFloor = false;
            interactableSpawnCard.skipSpawnWhenSacrificeArtifactEnabled = false;
            interactableSpawnCard.maxSpawnsPerStage = 1;

            directorCard = new DirectorCard
            {
                selectionWeight = ShrineOfTheDeep_DirectorWeight.Value,
                spawnCard = interactableSpawnCard,
                minimumStageCompletions = 1,
            };

            var directorCardHolder = new DirectorAPI.DirectorCardHolder
            {
                Card = directorCard,
                InteractableCategory = DirectorAPI.InteractableCategory.Shrines
            };

            // Stage 2
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.AbandonedAqueduct);
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.WetlandAspect);
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.AphelianSanctuary);
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.PretendersPrecipice);
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.ReformedAltar);

            // Stage 3
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.RallypointDelta);
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.ScorchedAcres);
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.SulfurPools);
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.IronAlluvium);
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.IronAuroras);
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.TreebornColony);
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.GoldenDieback);

            // Stage 4
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.AbyssalDepths);
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.SirensCall);
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.SunderedGrove);
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.RepurposedCrater);
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.ConduitCanyon);

            // Stage 5
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.SkyMeadow);
            DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.HelminthHatchery);

            List<string> stageInternalNames = new();
            if (!ShrineOfTheDeep_ExtraStages.Value.IsNullOrWhiteSpace())
            {
                List<string> customStageInternalNames = ShrineOfTheDeep_ExtraStages.Value.Split(',').ToList();
                stageInternalNames.AddRange(customStageInternalNames);
            }

            foreach (string name in stageInternalNames)
            {
                string trimmedName = name.Trim();
                DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, DirectorAPI.Stage.Custom, trimmedName);
            }

            // Remove Zoea corruption relationships
            if (ShrineOfTheDeep_ZoeaConversion.Value == true)
            {
                ItemRelationshipType voidConversionType = Addressables.LoadAssetAsync<ItemRelationshipType>("RoR2/DLC1/Common/ContagiousItem.asset").WaitForCompletion();
                ItemDef zoeaItemDef = Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/VoidMegaCrabItem.asset").WaitForCompletion();

                On.RoR2.ItemCatalog.SetItemRelationships += (orig, self) =>
                {
                    orig(self);

                    List<ItemDef.Pair> newList = new List<ItemDef.Pair>();
                    foreach (ItemDef.Pair oldPair in ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem])
                    {
                        if (oldPair.itemDef2 == zoeaItemDef || oldPair.itemDef2 == zoeaItemDef)
                        {
                            continue;
                        }
                        else
                        {
                            newList.Add(oldPair);
                        }
                    }
                    ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = newList.ToArray();
                };
            }
            
            // Override boss rewards
            On.RoR2.BossGroup.DropRewards += (orig, self) =>
            {
                if ((self.bossDrops != null || self.bossDropTables != null) && TeleporterInteraction.instance != null && self.forceTier3Reward != true)
                {
                    if (TeleporterInteraction.instance.GetComponent<ShrineOfTheDeepActivationBehavior>())
                    {
                        self.bossDrops = ConvertBossDrops(self.bossDrops);
                        self.bossDropTables = ConvertBossDropTables(self.bossDropTables, self.rng);
                        self.bonusRewardCount += 1;
                        self.bossDropChance = 0.2f;
                    }
                    else if (TeleporterInteraction.instance.GetComponent<RegularVoidActivationBehavior>())
                    {
                        self.bossDrops = ConvertBossDrops(self.bossDrops);
                        self.bossDropTables = ConvertBossDropTables(self.bossDropTables, self.rng);
                    }
                }

                orig(self);
            };

            // Override boss tricorn
            On.RoR2.EquipmentSlot.FireBossHunter += (orig, self) =>
            {
                HurtBox hurtBox = self.currentTarget.hurtBox;
                CharacterBody body = hurtBox?.healthComponent?.body;
                DeathRewards deathRewards = hurtBox?.healthComponent?.body?.gameObject?.GetComponent<DeathRewards>();

                if (body && deathRewards)
                {
                    if (deathRewards.bossDropTable != null && body.HasBuff(DLC1Content.Buffs.EliteVoid))
                    {
                        deathRewards.bossDropTable = ConvertBossDropTables(new List<PickupDropTable>(){deathRewards.bossDropTable}, self.rng).First();
                    }
                }

                return orig(self);
            };

            // Override boss drop tables with void versions if an enemy is corrupted during the fight
            On.RoR2.Inventory.SetEquipmentIndexForSlot_EquipmentIndex_uint_uint += (orig, self, newEquipmentIndex, slot, set) =>
            {
                orig(self, newEquipmentIndex, slot, set);

                if (newEquipmentIndex == DLC1Content.Equipment.EliteVoidEquipment.equipmentIndex)
                {
                    CharacterMaster characterMaster = self.gameObject.GetComponentInChildren<CharacterMaster>();
                    if (characterMaster && characterMaster.GetBody() && characterMaster.isBoss && TeleporterInteraction.instance != null)
                    {
                        TeleporterInteraction.instance.gameObject.AddComponent<RegularVoidActivationBehavior>();
                    }
                }
            };

            Log.Info("Added interactable Shrine of the Deep");
        }

        public static List<PickupDropTable> ConvertBossDropTables(List<PickupDropTable> bossDropTables, Xoroshiro128Plus rng)
        {
            List<PickupDropTable> ret = new();

            foreach (PickupDropTable pickupDropTable in bossDropTables)
            {
                UniquePickup uniquePickup = pickupDropTable.GeneratePickup(rng);

                if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.FireballsOnHit.itemIndex) && DrenchedPerforator.DrenchedPerforator_Enabled.Value)
                {
                    ret.Add(ItemInit.DrenchedPerforator.explicitPickupDropTable);
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.LightningStrikeOnHit.itemIndex) && DrenchedPerforator.DrenchedPerforator_Enabled.Value)
                {
                    ret.Add(ItemInit.DrenchedPerforator.explicitPickupDropTable);
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.Knurl.itemIndex) && OsmiumShackles.OsmiumShackles_Enabled.Value)
                {
                    ret.Add(ItemInit.OsmiumShackles.explicitPickupDropTable);
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.BleedOnHitAndExplode.itemIndex) && TenebralGland.TenebralGland_Enabled.Value)
                {
                    ret.Add(ItemInit.TenebralGland.explicitPickupDropTable);
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.ParentEgg.itemIndex) && Rebirth.Rebirth_Enabled.Value)
                {
                    ret.Add(ItemInit.Rebirth.explicitPickupDropTable);
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.NovaOnLowHealth.itemIndex) && MobiusNode.MobiusNode_Enabled.Value)
                {
                    ret.Add(ItemInit.MobiusNode.explicitPickupDropTable);
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.SprintWisp.itemIndex) && RebelSoul.RebelSoul_Enabled.Value)
                {
                    ret.Add(ItemInit.RebelSoul.explicitPickupDropTable);
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(DLC1Content.Items.MinorConstructOnKill.itemIndex) && SplitNucleus.SplitNucleus_Enabled.Value)
                {
                    ret.Add(ItemInit.SplitNucleus.explicitPickupDropTable);
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.SiphonOnLowHealth.itemIndex) && EffigyOfRot.EffigyOfRot_Enabled.Value)
                {
                    ret.Add(ItemInit.EffigyOfRot.explicitPickupDropTable);
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(DLC3Content.Items.ExtraEquipment.itemIndex) && ObserversEye.ObserversEye_Enabled.Value)
                {
                    ret.Add(ItemInit.ObserversEye.explicitPickupDropTable);
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(DLC3Content.Items.ShockDamageAura.itemIndex) && DeepVoidSignal.DeepVoidSignal_Enabled.Value)
                {
                    ret.Add(ItemInit.DeepVoidSignal.explicitPickupDropTable);
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.RoboBallBuddy.itemIndex) && ApathyCore.ApathyCore_Enabled.Value)
                {
                    ret.Add(ItemInit.ApathyCore.explicitPickupDropTable);
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.BeetleGland.itemIndex) && ShrineOfTheDeep_ZoeaRework.Value == true)
                {
                    ret.Add(explicitPickupDropTable);
                }
                else
                {
                    ret.Add(pickupDropTable);
                }
            }

            return ret;
        }

        public static List<UniquePickup> ConvertBossDrops(List<UniquePickup> bossDrops)
        {
            List<UniquePickup> ret = new();

            foreach (UniquePickup uniquePickup in bossDrops)
            {
                if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.FireballsOnHit.itemIndex) && DrenchedPerforator.DrenchedPerforator_Enabled.Value)
                {
                    ret.Add(new UniquePickup(PickupCatalog.FindPickupIndex(ItemInit.DrenchedPerforator.ItemIndex)));
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.LightningStrikeOnHit.itemIndex) && DrenchedPerforator.DrenchedPerforator_Enabled.Value)
                {
                    ret.Add(new UniquePickup(PickupCatalog.FindPickupIndex(ItemInit.DrenchedPerforator.ItemIndex)));
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.Knurl.itemIndex) && OsmiumShackles.OsmiumShackles_Enabled.Value)
                {
                    ret.Add(new UniquePickup(PickupCatalog.FindPickupIndex(ItemInit.OsmiumShackles.ItemIndex)));
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.BleedOnHitAndExplode.itemIndex) && TenebralGland.TenebralGland_Enabled.Value)
                {
                    ret.Add(new UniquePickup(PickupCatalog.FindPickupIndex(ItemInit.TenebralGland.ItemIndex)));
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.ParentEgg.itemIndex) && Rebirth.Rebirth_Enabled.Value)
                {
                    ret.Add(new UniquePickup(PickupCatalog.FindPickupIndex(ItemInit.Rebirth.ItemIndex)));
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.NovaOnLowHealth.itemIndex) && MobiusNode.MobiusNode_Enabled.Value)
                {
                    ret.Add(new UniquePickup(PickupCatalog.FindPickupIndex(ItemInit.MobiusNode.ItemIndex)));
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.SprintWisp.itemIndex) && RebelSoul.RebelSoul_Enabled.Value)
                {
                    ret.Add(new UniquePickup(PickupCatalog.FindPickupIndex(ItemInit.RebelSoul.ItemIndex)));
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(DLC1Content.Items.MinorConstructOnKill.itemIndex) && SplitNucleus.SplitNucleus_Enabled.Value)
                {
                    ret.Add(new UniquePickup(PickupCatalog.FindPickupIndex(ItemInit.SplitNucleus.ItemIndex)));
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.SiphonOnLowHealth.itemIndex) && EffigyOfRot.EffigyOfRot_Enabled.Value)
                {
                    ret.Add(new UniquePickup(PickupCatalog.FindPickupIndex(ItemInit.EffigyOfRot.ItemIndex)));
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(DLC3Content.Items.ExtraEquipment.itemIndex) && ObserversEye.ObserversEye_Enabled.Value)
                {
                    ret.Add(new UniquePickup(PickupCatalog.FindPickupIndex(ItemInit.ObserversEye.ItemIndex)));
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(DLC3Content.Items.ShockDamageAura.itemIndex) && DeepVoidSignal.DeepVoidSignal_Enabled.Value)
                {
                    ret.Add(new UniquePickup(PickupCatalog.FindPickupIndex(ItemInit.DeepVoidSignal.ItemIndex)));
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.RoboBallBuddy.itemIndex) && ApathyCore.ApathyCore_Enabled.Value)
                {
                    ret.Add(new UniquePickup(PickupCatalog.FindPickupIndex(ItemInit.ApathyCore.ItemIndex)));
                }
                else if (uniquePickup.pickupIndex == PickupCatalog.FindPickupIndex(RoR2Content.Items.BeetleGland.itemIndex) && ShrineOfTheDeep_ZoeaRework.Value)
                {
                    ret.Add(new UniquePickup(PickupCatalog.FindPickupIndex(DLC1Content.Items.VoidMegaCrabItem.itemIndex)));
                }
                else
                {
                    ret.Add(uniquePickup);
                }
            }

            return ret;
        }
    }

    public class ShrineOfTheDeepBehavior : NetworkBehaviour
    {
        public static GameObject explodeEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/CritGlassesVoid/CritGlassesVoidExecuteEffect.prefab").WaitForCompletion();

        public void Start()
        {
            PurchaseInteraction purchaseInteraction = gameObject.GetComponent<PurchaseInteraction>();
            if (NetworkServer.active && Run.instance)
            {
                purchaseInteraction.SetAvailableTrue();
            }
            purchaseInteraction.onPurchase.AddListener(PurchaseAction);
        }

        public void PurchaseAction(Interactor interactor)
        {
            if (TeleporterInteraction.instance)
            {
                TeleporterInteraction.instance.gameObject.AddComponent<ShrineOfTheDeepActivationBehavior>();

                if (TeleporterInteraction.instance.bossDirector && TeleporterInteraction.instance.gameObject.GetComponent<ShrineOfTheDeepActivationBehavior>())
                {
                    TeleporterInteraction.instance.bossDirector.ActiveEliteDefOverride = InteractableInit.shrineOfTheDeep.voidEliteDef;
                    TeleporterInteraction.instance.bossDirector.eliteBias = 0f;

                    if (TeleporterInteraction.instance.bonusDirector)
                    {
                        TeleporterInteraction.instance.bonusDirector.ActiveEliteDefOverride = InteractableInit.shrineOfTheDeep.voidEliteDef;
                    }
                }

                if (interactor.TryGetComponent(out CharacterBody body))
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage{baseToken = "NT_INTERACTABLE_SHRINEOFTHEDEEP_MESSAGE"});
                }

                EffectData effectData = new EffectData()
                {
                    origin = gameObject.transform.position
                };
                EffectManager.SpawnEffect(explodeEffect, effectData, true);

                Destroy(gameObject);
            }
        }
    }

    public class ShrineOfTheDeepActivationBehavior : NetworkBehaviour
    {
        
    }

    public class RegularVoidActivationBehavior : NetworkBehaviour
    {

    }
}