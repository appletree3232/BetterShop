using Alexandria.DungeonAPI;
using Dungeonator;
using UnityEngine;
using System.Collections.Generic;

namespace BetterShop.Patches
{
    public static class ExtraNpcInjector
    {
        /// <summary>
        /// 用于在收集时记录每个 NPC 的原始出处，方便调试和日志显示
        /// </summary>
        private struct NpcCandidate
        {
            public ProceduralFlowModifierData Modifier;
            public string SourceName;
        }

        /// <summary>
        /// 不适合作为"额外 NPC"随机注入的关键字黑名单（小写匹配）。
        /// </summary>
        private static readonly string[] AnnotationBlacklist = new string[]
        {
            "shrine",
            "black_market",
            "elevator",
            "exit",
            "entrance",
            "boss",
            "crest",
            "fireplace",
            "muncher",         // 排除 Gun Muncher
            "prayer",          // 排除 PrayerRoom
            "secret",          // 排除秘密房间相关
            // "sewer",        // 保留
            // "abbey",
        };

        public static void Init()
        {
            DungeonHooks.OnPreDungeonGeneration += OnPreDungeonGen;
        }

        // ─────────── 核心收集方法 ───────────

        /// <summary>
        /// 从所有来源收集 NPC 注入数据。
        /// 关键改进：每个 ProceduralFlowModifierData 视为一个 NPC 候选人（而非拆成单个房间），
        /// 这样 Winchester 等只算 1 个候选，而非 12 个。
        /// </summary>
        private static List<NpcCandidate> CollectAllNpcRooms(DungeonFlow flow)
        {
            // 用 annotation（或首房间名）做去重的 key
            HashSet<string> seenKeys = new HashSet<string>();
            List<NpcCandidate> result = new List<NpcCandidate>();

            // ────── 来源 1：Alexandria 预加载的已知 NPC 注入表 ──────
            // 这些表中的每一条 InjectionData 都是一个 NPC，无需做 SPECIAL 类型过滤
            CollectFromNamedSource(StaticInjections.NPC_injections,
                "NPC_injections", seenKeys, result, false);

            CollectFromNamedSource(StaticInjections.Fallback_Subshop_Injections,
                "Fallback_Subshop(Vampire/GunMuncher)", seenKeys, result, false);

            CollectFromNamedSource(StaticReferences.subShopTable,
                "SubShopTable(OldRed/Cursula/Flynt/Trorc/Goopton)", seenKeys, result, false);

            // ────── 来源 2：GlobalInjectionData ──────
            if (GameManager.Instance?.GlobalInjectionData?.entries != null)
            {
                foreach (var entry in GameManager.Instance.GlobalInjectionData.entries)
                {
                    if (entry?.injectionData?.InjectionData == null) continue;
                    CollectFromNamedSource(entry.injectionData,
                        "GlobalInjection/" + (entry.injectionData.name ?? "?"), seenKeys, result, true);
                }
            }

            // ────── 来源 3：当前 Flow 的 sharedInjectionData ──────
            if (flow.sharedInjectionData != null)
            {
                foreach (var shared in flow.sharedInjectionData)
                {
                    if (shared?.InjectionData == null) continue;
                    CollectFromNamedSource(shared,
                        "FlowShared/" + (shared.name ?? "?"), seenKeys, result, true);

                    if (shared.AttachedInjectionData != null)
                    {
                        foreach (var attached in shared.AttachedInjectionData)
                        {
                            if (attached?.InjectionData == null) continue;
                            CollectFromNamedSource(attached,
                                "FlowShared/attached", seenKeys, result, true);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 从一个 SharedInjectionData 中逐条提取 NPC 候选。
        /// requireSpecialCategory: 对于已知 NPC 表设 false（信任这些表就是 NPC），
        ///                         对于未知来源设 true（只取 SPECIAL 类型房间）。
        /// </summary>
        private static void CollectFromNamedSource(
            SharedInjectionData data,
            string sourceName,
            HashSet<string> seenKeys,
            List<NpcCandidate> result,
            bool requireSpecialCategory)
        {
            if (data == null || data.InjectionData == null) return;

            Plugin.Log.LogDebug($"  [来源:{sourceName}] 共 {data.InjectionData.Count} 条");

            foreach (var mod in data.InjectionData)
            {
                // ---- 确定这条 modifier 的去重 key 和显示名 ----
                string key = GetModifierKey(mod);
                if (string.IsNullOrEmpty(key)) continue;
                if (seenKeys.Contains(key)) continue;

                // ---- 验证此条是否为有效 NPC 房间 ----
                if (!IsValidNpc(mod, requireSpecialCategory))
                {
                    Plugin.Log.LogDebug($"    - 跳过: {key} (不满足条件, 来源: {sourceName})");
                    continue;
                }

                // ---- 黑名单 ----
                string annotation = (mod.annotation ?? "").ToLower();
                if (IsAnnotationBlacklisted(key.ToLower(), annotation)) continue;

                seenKeys.Add(key);
                result.Add(new NpcCandidate { Modifier = mod, SourceName = sourceName });
                Plugin.Log.LogDebug($"    + 收集: {key} (来源: {sourceName})");
            }
        }

        /// <summary>
        /// 为一条 ProceduralFlowModifierData 生成去重 key。
        /// 优先使用 annotation，其次使用 exactRoom.name 或 roomTable 中首个房间名。
        /// </summary>
        private static string GetModifierKey(ProceduralFlowModifierData mod)
        {
            // 优先用 annotation 去重（同一个 NPC 的不同 modifier 通常 annotation 相同）
            if (!string.IsNullOrEmpty(mod.annotation))
            {
                if (mod.annotation.StartsWith("CHALLENGE - "))
                    return "CHALLENGE";
                return mod.annotation;
            }

            // 其次用 exactRoom 名
            if (mod.exactRoom != null && !string.IsNullOrEmpty(mod.exactRoom.name))
                return mod.exactRoom.name;

            // 最后用 roomTable 首房间名
            if (mod.roomTable?.includedRooms?.elements != null)
            {
                foreach (var wr in mod.roomTable.includedRooms.elements)
                {
                    if (wr?.room != null && !string.IsNullOrEmpty(wr.room.name))
                        return "table:" + wr.room.name;
                }
            }

            return null;
        }

        /// <summary>
        /// 检查一条 modifier 是否指向有效 NPC 房间。
        /// </summary>
        private static bool IsValidNpc(ProceduralFlowModifierData mod, bool requireSpecialCategory)
        {
            // 有 exactRoom
            if (mod.exactRoom != null)
            {
                if (requireSpecialCategory && mod.exactRoom.category != PrototypeDungeonRoom.RoomCategory.SPECIAL)
                    return false;
                return true;
            }

            // 有 roomTable
            if (mod.roomTable?.includedRooms?.elements != null && mod.roomTable.includedRooms.elements.Count > 0)
            {
                if (requireSpecialCategory)
                {
                    // 只要 roomTable 中有一个 SPECIAL 房间就算有效
                    foreach (var wr in mod.roomTable.includedRooms.elements)
                    {
                        if (wr?.room != null && wr.room.category == PrototypeDungeonRoom.RoomCategory.SPECIAL)
                            return true;
                    }
                    // 也检查嵌套 roomTable
                    if (mod.roomTable.includedRoomTables != null)
                    {
                        foreach (var sub in mod.roomTable.includedRoomTables)
                        {
                            if (sub?.includedRooms?.elements == null) continue;
                            foreach (var wr in sub.includedRooms.elements)
                            {
                                if (wr?.room != null && wr.room.category == PrototypeDungeonRoom.RoomCategory.SPECIAL)
                                    return true;
                            }
                        }
                    }
                    return false;
                }
                return true;
            }

            // 也检查嵌套 roomTable
            if (mod.roomTable?.includedRoomTables != null)
            {
                foreach (var sub in mod.roomTable.includedRoomTables)
                {
                    if (sub?.includedRooms?.elements != null && sub.includedRooms.elements.Count > 0)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 黑名单检查。
        /// </summary>
        private static bool IsAnnotationBlacklisted(string lowerKey, string lowerAnnotation)
        {
            for (int i = 0; i < AnnotationBlacklist.Length; i++)
            {
                string kw = AnnotationBlacklist[i];
                if (lowerKey.Contains(kw) || lowerAnnotation.Contains(kw))
                    return true;
            }
            return false;
        }

        // ─────────── 注入逻辑 ───────────

        private static void OnPreDungeonGen(LoopDungeonGenerator generator, Dungeon dungeon, DungeonFlow flow, int dungeonSeed)
        {
            int extraCount = Plugin.ExtraNpcCount.Value;
            if (extraCount <= 0) return;

            // 排除裂隙层 (Forge)
            if (dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.FORGEGEON)
            {
                Plugin.Log.LogInfo("[BetterShop] 裂隙层已排除，不注入额外 NPC。");
                return;
            }

            // 排除大厅
            if (flow.name == "Foyer Flow" || GameManager.IsReturningToFoyerWithPlayer)
                return;

            Plugin.Log.LogInfo("[BetterShop] 正在从所有注入源收集 NPC...");

            List<NpcCandidate> allNpcs = CollectAllNpcRooms(flow);

            if (allNpcs.Count == 0)
            {
                Plugin.Log.LogWarning("[BetterShop] 未找到任何有效的 NPC 房间！");
                return;
            }

            Plugin.Log.LogInfo($"[BetterShop] 已收集 {allNpcs.Count} 个 NPC 候选：");
            for (int i = 0; i < allNpcs.Count; i++)
            {
                string name = GetModifierKey(allNpcs[i].Modifier) ?? "?";
                string source = allNpcs[i].SourceName;
                Plugin.Log.LogInfo($"   [{i}] {name}  ({source})");
            }

            // 随机选择不重复的 NPC
            int actualCount = Mathf.Min(extraCount, allNpcs.Count);
            List<NpcCandidate> pool = new List<NpcCandidate>(allNpcs);

            for (int i = 0; i < actualCount; i++)
            {
                int idx = Random.Range(0, pool.Count);
                ProceduralFlowModifierData selected = pool[idx].Modifier;
                pool.RemoveAt(idx);

                string npcName = GetModifierKey(selected) ?? "unknown";

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
                    roomTable = selected.roomTable,
                    exactRoom = selected.exactRoom,
                    exactSecondaryRoom = selected.exactSecondaryRoom,
                    framedCombatNodes = 0,
                    IsWarpWing = false,
                    RequiresMasteryToken = false,
                    chanceToLock = 0f,
                    selectionWeight = 300f,
                    chanceToSpawn = 1f,
                    RequiredValidPlaceable = selected.RequiredValidPlaceable,
                    prerequisites = new DungeonPrerequisite[0],
                    CanBeForcedSecret = false,
                    RandomNodeChildMinDistanceFromEntrance = 0,
                };

                SharedInjectionData npcInjection = ScriptableObject.CreateInstance<SharedInjectionData>();
                npcInjection.name = "BetterShop_ExtraNPC_" + npcName;
                npcInjection.UseInvalidWeightAsNoInjection = true;
                npcInjection.PreventInjectionOfFailedPrerequisites = false;
                npcInjection.IsNPCCell = true;
                npcInjection.IgnoreUnmetPrerequisiteEntries = false;
                npcInjection.OnlyOne = false;
                npcInjection.ChanceToSpawnOne = 1f;
                npcInjection.AttachedInjectionData = new List<SharedInjectionData>();
                npcInjection.InjectionData = new List<ProceduralFlowModifierData>() { npcModifier };

                if (flow.sharedInjectionData == null)
                    flow.sharedInjectionData = new List<SharedInjectionData>();

                flow.sharedInjectionData.Add(npcInjection);
                Plugin.Log.LogInfo($"[BetterShop] 额外 NPC 注入成功：{npcName} (层: {dungeon.tileIndices.tilesetId})");
            }

            Plugin.Log.LogInfo($"[BetterShop] 共注入 {actualCount} 个额外 NPC。");
        }
    }
}
