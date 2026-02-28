using Alexandria.DungeonAPI;
using Dungeonator;
using UnityEngine;
using System.Collections.Generic;

namespace BetterShop.Patches
{
    public static class ExtraNpcInjector
    {
        public static void Init()
        {
            DungeonHooks.OnPreDungeonGeneration += OnPreDungeonGen;
        }

        private static void OnPreDungeonGen(LoopDungeonGenerator generator, Dungeon dungeon, DungeonFlow flow, int dungeonSeed)
        {
            if (!Plugin.ExtraNpcEnabled.Value) return;

            // 排除裂隙层 (Forge)
            if (dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.FORGEGEON)
            {
                Plugin.Log.LogInfo("[BetterShop] 裂隙层已排除，不注入额外 NPC。");
                return;
            }

            // 排除大厅
            if (flow.name == "Foyer Flow" || GameManager.IsReturningToFoyerWithPlayer)
            {
                return;
            }

            Plugin.Log.LogInfo("[BetterShop] 正在为当前楼层注入额外 NPC...");

            // 从全局注入数据动态读取子商店 NPC 表
            // entries[2] 是子商店 NPC 注册表，包含原版 NPC + 所有 mod 通过 ShopAPI 注册的 NPC
            if (GameManager.Instance == null ||
                GameManager.Instance.GlobalInjectionData == null ||
                GameManager.Instance.GlobalInjectionData.entries == null ||
                GameManager.Instance.GlobalInjectionData.entries.Count <= 2)
            {
                Plugin.Log.LogError("[BetterShop] 无法访问 GlobalInjectionData！");
                return;
            }

            SharedInjectionData subShopTable = GameManager.Instance.GlobalInjectionData.entries[2].injectionData;
            if (subShopTable == null || subShopTable.InjectionData == null || subShopTable.InjectionData.Count == 0)
            {
                Plugin.Log.LogError("[BetterShop] GlobalInjectionData.entries[2] 中没有 NPC 数据！");
                return;
            }

            int npcCount = subShopTable.InjectionData.Count;
            Plugin.Log.LogInfo($"[BetterShop] 已发现 {npcCount} 个已注册的子商店 NPC：");
            for (int i = 0; i < npcCount; i++)
            {
                var data = subShopTable.InjectionData[i];
                string name = data.exactRoom != null ? data.exactRoom.name : (data.annotation ?? "未知");
                Plugin.Log.LogInfo($"   [{i}] {name}");
            }

            // 随机选择一个 NPC
            int selectedIndex = Random.Range(0, npcCount);
            ProceduralFlowModifierData selectedNpc = subShopTable.InjectionData[selectedIndex];
            string npcName = selectedNpc.exactRoom != null ? selectedNpc.exactRoom.name : (selectedNpc.annotation ?? "未知NPC");

            if (selectedNpc.exactRoom == null)
            {
                Plugin.Log.LogWarning($"[BetterShop] 选中的 NPC [{npcName}] 没有关联房间，跳过注入。");
                return;
            }

            // 构造注入数据，复制原始 NPC 的关键属性
            ProceduralFlowModifierData npcModifier = new ProceduralFlowModifierData()
            {
                annotation = "BetterShop Extra NPC: " + npcName,
                DEBUG_FORCE_SPAWN = false,
                OncePerRun = false,
                placementRules = new List<ProceduralFlowModifierData.FlowModifierPlacementType>()
                {
                    ProceduralFlowModifierData.FlowModifierPlacementType.END_OF_CHAIN,
                    ProceduralFlowModifierData.FlowModifierPlacementType.HUB_ADJACENT_NO_LINK
                },
                roomTable = selectedNpc.roomTable,
                exactRoom = selectedNpc.exactRoom,
                exactSecondaryRoom = selectedNpc.exactSecondaryRoom,
                framedCombatNodes = 0,
                IsWarpWing = false,
                RequiresMasteryToken = false,
                chanceToLock = 0f,
                selectionWeight = 300f,
                chanceToSpawn = 1f,
                RequiredValidPlaceable = selectedNpc.RequiredValidPlaceable,
                prerequisites = new DungeonPrerequisite[0],
                CanBeForcedSecret = false,
                RandomNodeChildMinDistanceFromEntrance = 0,
            };

            SharedInjectionData npcInjection = ScriptableObject.CreateInstance<SharedInjectionData>();
            npcInjection.name = "BetterShop_ExtraNPC_Injection";
            npcInjection.UseInvalidWeightAsNoInjection = true;
            npcInjection.PreventInjectionOfFailedPrerequisites = false;
            npcInjection.IsNPCCell = true;
            npcInjection.IgnoreUnmetPrerequisiteEntries = false;
            npcInjection.OnlyOne = false;
            npcInjection.ChanceToSpawnOne = 1f;
            npcInjection.AttachedInjectionData = new List<SharedInjectionData>();
            npcInjection.InjectionData = new List<ProceduralFlowModifierData>() { npcModifier };

            // 注入到当前 flow
            if (flow.sharedInjectionData == null)
            {
                flow.sharedInjectionData = new List<SharedInjectionData>();
            }
            flow.sharedInjectionData.Add(npcInjection);

            Plugin.Log.LogInfo($"[BetterShop] 额外 NPC 注入成功：{npcName} (层: {dungeon.tileIndices.tilesetId})");
        }
    }
}
