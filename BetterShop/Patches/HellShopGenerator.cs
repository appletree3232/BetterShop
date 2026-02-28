using Alexandria.DungeonAPI;
using Dungeonator;
using UnityEngine;
using System.Collections.Generic;

namespace BetterShop.Patches
{
    public static class HellShopGenerator
    {
        public static void Init()
        {
            DungeonHooks.OnPreDungeonGeneration += InjectHellShopViaInjection;
        }

        private static void InjectHellShopViaInjection(LoopDungeonGenerator generator, Dungeon dungeon, DungeonFlow flow, int dungeonSeed)
        {
            if (!Plugin.BulletHellShopEnabled.Value) return;

            // 目标：第六层 (HELLGEON)
            if (dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.HELLGEON)
            {
                Plugin.Log.LogInfo("[BetterShop] 正在适配注入 Bello 商店至第六层...");

                // 1. 从第一层加载商店原型
                Dungeon castle = DungeonDatabase.GetOrLoadByName("Base_Castle");
                PrototypeDungeonRoom shopRoomTemplate = null;
                string foundVia = null;

                // 遍历第一层的所有流图寻找商店
                foreach (var f in castle.PatternSettings.flows)
                {
                    // 策略1: 搜索 SharedInjectionData（商店通常通过注入数据添加）
                    if (f.sharedInjectionData != null)
                    {
                        foreach (var injection in f.sharedInjectionData)
                        {
                            if (injection == null || injection.InjectionData == null) continue;
                            foreach (var data in injection.InjectionData)
                            {
                                if (data.exactRoom != null && data.exactRoom.name.ToLower().Contains("shop"))
                                {
                                    shopRoomTemplate = data.exactRoom;
                                    foundVia = "SharedInjectionData";
                                    break;
                                }
                            }
                            if (shopRoomTemplate != null) break;
                        }
                    }
                    if (shopRoomTemplate != null) break;

                    // 策略2: 搜索 flow node 的 overrideExactRoom
                    foreach (DungeonFlowNode node in f.AllNodes)
                    {
                        if (node.overrideExactRoom != null && node.overrideExactRoom.name.ToLower().Contains("shop"))
                        {
                            shopRoomTemplate = node.overrideExactRoom;
                            foundVia = "FlowNode.overrideExactRoom";
                            break;
                        }
                    }
                    if (shopRoomTemplate != null) break;

                    // 策略3: 搜索 flow node 的 overrideRoomTable
                    foreach (DungeonFlowNode node in f.AllNodes)
                    {
                        if (node.overrideRoomTable != null && node.overrideRoomTable.includedRooms != null)
                        {
                            foreach (var weightedRoom in node.overrideRoomTable.includedRooms.elements)
                            {
                                if (weightedRoom.room != null && weightedRoom.room.name.ToLower().Contains("shop"))
                                {
                                    shopRoomTemplate = weightedRoom.room;
                                    foundVia = "FlowNode.overrideRoomTable";
                                    break;
                                }
                            }
                        }
                        if (shopRoomTemplate != null) break;
                    }
                    if (shopRoomTemplate != null) break;
                }

                if (shopRoomTemplate == null)
                {
                    Plugin.Log.LogError("[BetterShop] 无法在 Castle 资源中找到商店模板！（已搜索 SharedInjectionData / FlowNode / RoomTable）");
                    return;
                }

                Plugin.Log.LogInfo($"[BetterShop] 找到商店模板: {shopRoomTemplate.name} (来源: {foundVia})");

                // 2. 构造符合你程序集定义的注入数据
                ProceduralFlowModifierData shopModifier = new ProceduralFlowModifierData()
                {
                    annotation = "Bello Shop Injector",
                    DEBUG_FORCE_SPAWN = false,
                    OncePerRun = false,
                    placementRules = new List<ProceduralFlowModifierData.FlowModifierPlacementType>()
                    {
                        ProceduralFlowModifierData.FlowModifierPlacementType.END_OF_CHAIN,
                        ProceduralFlowModifierData.FlowModifierPlacementType.HUB_ADJACENT_NO_LINK
                    },
                    roomTable = null,
                    exactRoom = shopRoomTemplate,
                    exactSecondaryRoom = null,
                    framedCombatNodes = 0,
                    IsWarpWing = false,
                    RequiresMasteryToken = false,
                    chanceToLock = 0f,
                    selectionWeight = 10000f,
                    chanceToSpawn = 1f,
                    RequiredValidPlaceable = null,
                    prerequisites = new DungeonPrerequisite[0],
                    CanBeForcedSecret = false,
                    RandomNodeChildMinDistanceFromEntrance = 0,
                };

                // 3. 构造 SharedInjectionData
                SharedInjectionData hellShopInjection = ScriptableObject.CreateInstance<SharedInjectionData>();
                hellShopInjection.name = "BetterShop_Hell_Injection";
                hellShopInjection.UseInvalidWeightAsNoInjection = true;
                hellShopInjection.PreventInjectionOfFailedPrerequisites = false;
                hellShopInjection.IsNPCCell = false;
                hellShopInjection.IgnoreUnmetPrerequisiteEntries = false;
                hellShopInjection.OnlyOne = false;
                hellShopInjection.ChanceToSpawnOne = 1f;
                hellShopInjection.AttachedInjectionData = new List<SharedInjectionData>();
                hellShopInjection.InjectionData = new List<ProceduralFlowModifierData>() { shopModifier };

                // 4. 注入到当前的 Flow
                if (flow.sharedInjectionData == null)
                {
                    flow.sharedInjectionData = new List<SharedInjectionData>();
                }
                flow.sharedInjectionData.Add(hellShopInjection);

                Plugin.Log.LogInfo("[BetterShop] 注入成功：商店已加入第六层生成队列。");
            }
        }
    }
}