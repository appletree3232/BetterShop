using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Dungeonator;

namespace BetterShop.Patches
{
    [HarmonyPatch(typeof(BaseShopController), "DoSetup")]
    public class ShopGuaranteePatch
    {
        /// <summary>
        /// 判断是否为"真道具"（主动/被动），排除消耗品和特殊物品。
        /// </summary>
        private static bool IsRealItem(PickupObject item)
        {
            if (item == null || item is Gun) return false;
            if (item is HealthPickup || item is AmmoPickup || item is KeyBulletPickup || item is SilencerItem)
                return false;
            // 排除玻璃滚石(565)、地图(137)、老鼠钥匙(316)
            if (item.PickupObjectId == 565 || item.PickupObjectId == 137 || item.PickupObjectId == 316)
                return false;

            return item is PlayerItem || item is PassiveItem;

        }

        [HarmonyPostfix]
        public static void Postfix(BaseShopController __instance,
            ref List<ShopItemController> ___m_itemControllers,
            ref List<GameObject> ___m_shopItems)
        {
            // 只管 Bello 的基础商店
            if (__instance.baseShopType != BaseShopController.AdditionalShopType.NONE)
                return;

            // 彩虹模式跳过
            if (GameStatsManager.Instance != null && GameStatsManager.Instance.IsRainbowRun)
            {
                Plugin.Log.LogInfo("[BetterShop] 彩虹模式，保底已禁用。");
                return;
            }

            int minGuns = Plugin.MinGuns.Value;
            int minItems = Plugin.MinItems.Value;
            if (minGuns <= 0 && minItems <= 0) return;
            if (___m_itemControllers == null || ___m_itemControllers.Count == 0) return;

            // ═══════════ 统计阶段 ═══════════
            int gunCount = 0;
            int itemCount = 0;
            int group1Count = __instance.spawnPositions != null ? __instance.spawnPositions.Length : 0;

            Plugin.Log.LogInfo("==================================================");
            Plugin.Log.LogInfo("[BetterShop] 商店生成完毕，扫描台面物品...");

            for (int i = 0; i < ___m_itemControllers.Count; i++)
            {
                var ctrl = ___m_itemControllers[i];
                if (ctrl == null || ctrl.item == null) continue;

                string tag = "消耗品/杂物";
                if (ctrl.item is Gun) { gunCount++; tag = "【枪械】"; }
                else if (IsRealItem(ctrl.item)) { itemCount++; tag = "【道具】"; }

                Plugin.Log.LogInfo($"  [{i}] ID:{ctrl.item.PickupObjectId,-4} {tag,-8} {ctrl.item.EncounterNameOrDisplayName}");
            }

            Plugin.Log.LogInfo($"[BetterShop] 枪:{gunCount}/{minGuns}  道具:{itemCount}/{minItems}");

            bool needGun = gunCount < minGuns;
            bool needItem = itemCount < minItems;
            if (!needGun && !needItem)
            {
                Plugin.Log.LogInfo("[BetterShop] 已达标！");
                Plugin.Log.LogInfo("==================================================\n");
                return;
            }

            // ═══════════ 补齐阶段 — 仅在 Group2 区域操作 ═══════════
            // Group2 的 spawnPositions 索引范围
            Transform[] g2Positions = __instance.spawnPositionsGroup2;
            if (g2Positions == null || g2Positions.Length == 0)
            {
                Plugin.Log.LogWarning("[BetterShop] 无 Group2 位置，无法补齐。");
                Plugin.Log.LogInfo("==================================================\n");
                return;
            }

            // 找出 Group2 中的空位 (原版概率未生成) 和消耗品位
            // Group2 的 m_itemControllers 从 group1Count 开始
            List<int> emptyG2Indices = new List<int>();       // 完全空的 Group2 位置索引 (在 spawnPositionsGroup2 中)
            List<int> consumableG2Slots = new List<int>();    // Group2 中已有消耗品的 itemController 索引

            // 先找到所有已占用的 Group2 位置
            HashSet<int> occupiedG2 = new HashSet<int>();
            for (int i = group1Count; i < ___m_itemControllers.Count; i++)
            {
                var ctrl = ___m_itemControllers[i];
                if (ctrl == null) continue;

                // 通过 transform 的 parent 来判断这个 controller 在 Group2 的哪个位置
                for (int g = 0; g < g2Positions.Length; g++)
                {
                    if (ctrl.transform.parent == g2Positions[g])
                    {
                        occupiedG2.Add(g);
                        // 如果是消耗品，记录为可替换
                        if (ctrl.item != null && !(ctrl.item is Gun) && !IsRealItem(ctrl.item))
                        {
                            consumableG2Slots.Add(i);
                        }
                        break;
                    }
                }
            }

            // Group2 中完全空的位置
            for (int g = 0; g < g2Positions.Length; g++)
            {
                if (!occupiedG2.Contains(g))
                    emptyG2Indices.Add(g);
            }

            Plugin.Log.LogInfo($"[BetterShop] Group2 空位:{emptyG2Indices.Count}  消耗品位:{consumableG2Slots.Count}");

            // 用官方 API 生成物品
            PlayerController player = GameManager.Instance.PrimaryPlayer;

            // --- 补枪 ---
            if (needGun)
            {
                if (emptyG2Indices.Count > 0)
                {
                    // 在空位创建新的 ShopItemController
                    int posIdx = emptyG2Indices[0];
                    emptyG2Indices.RemoveAt(0);

                    GameObject gunObj = GameManager.Instance.RewardManager.GetRewardObjectShopStyle(
                        player, true, false, ___m_shopItems);

                    if (gunObj != null)
                    {
                        PickupObject gunPickup = gunObj.GetComponent<PickupObject>();
                        if (gunPickup != null)
                        {
                            SpawnShopItem(gunPickup, g2Positions[posIdx], __instance, ___m_itemControllers, ___m_shopItems);
                            gunCount++;
                            Plugin.Log.LogInfo($"[BetterShop] >>> 空位补枪: [{gunPickup.EncounterNameOrDisplayName}] (ID:{gunPickup.PickupObjectId})");
                        }
                    }
                }
                else if (consumableG2Slots.Count > 0)
                {
                    // 替换消耗品
                    int slotIdx = consumableG2Slots[0];
                    consumableG2Slots.RemoveAt(0);

                    GameObject gunObj = GameManager.Instance.RewardManager.GetRewardObjectShopStyle(
                        player, true, false, ___m_shopItems);

                    if (gunObj != null)
                    {
                        PickupObject gunPickup = gunObj.GetComponent<PickupObject>();
                        if (gunPickup != null)
                        {
                            string oldName = ___m_itemControllers[slotIdx].item?.EncounterNameOrDisplayName ?? "?";
                            ___m_itemControllers[slotIdx].Initialize(gunPickup, __instance);
                            gunCount++;
                            Plugin.Log.LogInfo($"[BetterShop] >>> 替换补枪: [{oldName}] -> [{gunPickup.EncounterNameOrDisplayName}]");
                        }
                    }
                }
            }

            // --- 补道具 ---
            needItem = itemCount < minItems; // 重新检查
            if (needItem)
            {
                if (emptyG2Indices.Count > 0)
                {
                    int posIdx = emptyG2Indices[0];
                    emptyG2Indices.RemoveAt(0);

                    // GetRewardObjectShopStyle(player, false, false, exclude) 会随机生成枪或道具
                    // 我们需要道具，所以多次尝试或用 shopItems 掉落表
                    PickupObject itemPickup = GetGuaranteedItem(__instance, ___m_shopItems);

                    if (itemPickup != null)
                    {
                        SpawnShopItem(itemPickup, g2Positions[posIdx], __instance, ___m_itemControllers, ___m_shopItems);
                        itemCount++;
                        Plugin.Log.LogInfo($"[BetterShop] >>> 空位补道具: [{itemPickup.EncounterNameOrDisplayName}] (ID:{itemPickup.PickupObjectId})");
                    }
                }
                else if (consumableG2Slots.Count > 0)
                {
                    int slotIdx = consumableG2Slots[0];
                    consumableG2Slots.RemoveAt(0);

                    PickupObject itemPickup = GetGuaranteedItem(__instance, ___m_shopItems);

                    if (itemPickup != null)
                    {
                        string oldName = ___m_itemControllers[slotIdx].item?.EncounterNameOrDisplayName ?? "?";
                        ___m_itemControllers[slotIdx].Initialize(itemPickup, __instance);
                        itemCount++;
                        Plugin.Log.LogInfo($"[BetterShop] >>> 替换补道具: [{oldName}] -> [{itemPickup.EncounterNameOrDisplayName}]");
                    }
                }
            }

            Plugin.Log.LogInfo($"[BetterShop] 最终: 枪:{gunCount}  道具:{itemCount}");
            Plugin.Log.LogInfo("==================================================\n");
        }

        /// <summary>
        /// 在指定 Group2 位置创建一个新的 ShopItemController。
        /// 复用官方 DoSetup 中创建 Group2 物品的逻辑。
        /// </summary>
        private static void SpawnShopItem(PickupObject pickup, Transform spawnPos,
            BaseShopController shop, List<ShopItemController> controllers, List<GameObject> shopItems)
        {
            GameObject go = new GameObject("Shop guaranteed item");
            go.transform.parent = spawnPos;
            go.transform.localPosition = Vector3.zero;

            // 注册 EncounterTrackable
            EncounterTrackable trackable = pickup.GetComponent<EncounterTrackable>();
            if (trackable != null)
            {
                GameManager.Instance.ExtantShopTrackableGuids.Add(trackable.EncounterGuid);
            }

            ShopItemController itemCtrl = go.AddComponent<ShopItemController>();

            // 朝向：复用 spawnPos 的命名约定
            if (spawnPos.name.Contains("SIDE") || spawnPos.name.Contains("EAST"))
                itemCtrl.itemFacing = DungeonData.Direction.EAST;
            else if (spawnPos.name.Contains("WEST"))
                itemCtrl.itemFacing = DungeonData.Direction.WEST;
            else if (spawnPos.name.Contains("NORTH"))
                itemCtrl.itemFacing = DungeonData.Direction.NORTH;

            // 注册到房间
            RoomHandler room = shop.GetAbsoluteParentRoom();
            if (room != null && !room.IsRegistered(itemCtrl))
            {
                room.RegisterInteractable(itemCtrl);
            }

            itemCtrl.Initialize(pickup, shop);
            controllers.Add(itemCtrl);
            shopItems.Add(pickup.gameObject);
        }

        /// <summary>
        /// 用官方 API 获取一个"真道具"（主动/被动），最多重试 10 次。
        /// 优先使用 RewardManager，它会按品质权重和当前层级来选择。
        /// </summary>
        private static PickupObject GetGuaranteedItem(BaseShopController shop, List<GameObject> exclude)
        {
            PlayerController player = GameManager.Instance.PrimaryPlayer;

            for (int tries = 0; tries < 10; tries++)
            {
                GameObject obj = GameManager.Instance.RewardManager.GetRewardObjectShopStyle(
                    player, false, false, exclude);
                if (obj == null) continue;

                PickupObject pickup = obj.GetComponent<PickupObject>();
                if (pickup != null && IsRealItem(pickup))
                    return pickup;
            }

            // 兜底：从商店自身的掉落表中选
            GameObject fallback = shop.shopItems.SelectByWeightWithoutDuplicatesFullPrereqs(
                exclude, null, GameManager.Instance.IsSeeded);
            if (fallback != null)
            {
                PickupObject pickup = fallback.GetComponent<PickupObject>();
                if (pickup != null && IsRealItem(pickup))
                    return pickup;
            }

            Plugin.Log.LogWarning("[BetterShop] 无法找到合适的道具进行补齐。");
            return null;
        }
    }
}