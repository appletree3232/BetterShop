using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using BetterShop.Utils;

namespace BetterShop.Patches
{
    [HarmonyPatch(typeof(BaseShopController), "DoSetup")]
    public class ShopGuaranteePatch
    {

        private static bool IsRealItem(PickupObject item)
        {
            if (item == null || item is Gun) return false;

            // 排除消耗品
            if (item is HealthPickup || item is AmmoPickup || item is KeyBulletPickup || item is SilencerItem)
                return false;

            // 排除玻璃滚石(565)、地图(137)以及被啃咬的钥匙/老鼠钥匙(316)等特殊物品
            if (item.PickupObjectId == 565 || item.PickupObjectId == 137 || item.PickupObjectId == 316)
                return false;

            return item is PlayerItem || item is PassiveItem;
        }

        [HarmonyPostfix]
        public static void Postfix(BaseShopController __instance, ref List<ShopItemController> ___m_itemControllers)
        {
            // 只管 Bello 的基础商店
            if (__instance.baseShopType != BaseShopController.AdditionalShopType.NONE)
                return;
            // --- 新增：彩虹模式拦截 ---
            if (GameStatsManager.Instance != null && GameStatsManager.Instance.IsRainbowRun)
            {
                Plugin.Log.LogInfo("[BetterShop] 检测到当前为彩虹模式，保底逻辑已自动禁用。");
                return;
            }

            int minGuns = Plugin.MinGuns.Value;
            int minItems = Plugin.MinItems.Value;
            if (minGuns <= 0 && minItems <= 0) return;

            if (___m_itemControllers == null || ___m_itemControllers.Count == 0) return;

            int gunCount = 0;
            int itemCount = 0;

            Plugin.Log.LogInfo("==================================================");
            Plugin.Log.LogInfo($"[BetterShop] Bello商店生成完毕，正在检查台面物品...");

            for (int i = 0; i < ___m_itemControllers.Count; i++)
            {
                var slot = ___m_itemControllers[i];
                if (slot.item == null) continue;

                string itemName = slot.item.EncounterNameOrDisplayName;
                int itemId = slot.item.PickupObjectId;
                string itemTypeStr = "消耗品/杂物";

                if (slot.item is Gun)
                {
                    gunCount++;
                    itemTypeStr = "【枪械】";
                }
                else if (IsRealItem(slot.item))
                {
                    itemCount++;
                    itemTypeStr = "【真道具】";
                }
                // 对老鼠钥匙(316)做特殊日志标记，方便调试查看
                else if (itemId == 316)
                {
                    itemTypeStr = "【老鼠钥匙】";
                }

                Plugin.Log.LogInfo($"   槽位[{i}] | ID: {itemId,-4} | 类型: {itemTypeStr,-8} | 名称: {itemName}");
            }

            Plugin.Log.LogInfo($"[BetterShop] 统计结果 -> 枪械: {gunCount}/{minGuns}把，道具: {itemCount}/{minItems}个");

            if (gunCount >= minGuns && itemCount >= minItems)
            {
                Plugin.Log.LogInfo($"[BetterShop] 商店已达标，无需修改！");
                Plugin.Log.LogInfo("==================================================\n");
                return;
            }

            Plugin.Log.LogInfo($"[BetterShop] 未达标！开始寻找替换位...");

            int area1Count = __instance.spawnPositions != null ? __instance.spawnPositions.Length : 0;
            int area2Count = __instance.spawnPositionsGroup2 != null ? __instance.spawnPositionsGroup2.Length : 0;

            // --- 核心修复 2：按严格的优先级顺序构建替换队列，互斥条件彻底干掉 Contains 检查和 <>c ---
            List<ShopItemController> replaceableSlots = new List<ShopItemController>();

            // 优先级 1：Group2（随机物品区）的消耗品
            for (int i = area1Count; i < area1Count + area2Count && i < ___m_itemControllers.Count; i++)
            {
                var slot = ___m_itemControllers[i];
                if (slot.item == null || slot.item.PickupObjectId == 316) continue;
                if (!(slot.item is Gun) && !IsRealItem(slot.item))
                    replaceableSlots.Add(slot);
            }

            // 优先级 2：Group1（基础物品区）的 非钥匙 消耗品（如红心、弹药）
            for (int i = 0; i < area1Count && i < ___m_itemControllers.Count; i++)
            {
                var slot = ___m_itemControllers[i];
                if (slot.item == null || slot.item.PickupObjectId == 316) continue;
                if (!(slot.item is Gun) && !IsRealItem(slot.item) && !(slot.item is KeyBulletPickup))
                    replaceableSlots.Add(slot);
            }

            // 优先级 3：Group1（基础物品区）的 钥匙
            for (int i = 0; i < area1Count && i < ___m_itemControllers.Count; i++)
            {
                var slot = ___m_itemControllers[i];
                if (slot.item == null || slot.item.PickupObjectId == 316) continue;
                if (!(slot.item is Gun) && !IsRealItem(slot.item) && (slot.item is KeyBulletPickup))
                    replaceableSlots.Add(slot);
            }

            // 优先级 4：超出保底数量的多余枪械或多余道具 (作为最后兜底)
            for (int i = 0; i < ___m_itemControllers.Count; i++)
            {
                var slot = ___m_itemControllers[i];
                if (slot.item == null || slot.item.PickupObjectId == 316) continue;

                if (slot.item is Gun && gunCount > minGuns)
                    replaceableSlots.Add(slot);
                else if (IsRealItem(slot.item) && itemCount > minItems)
                    replaceableSlots.Add(slot);
            }

            // 开始替换逻辑
            foreach (var oldSlot in replaceableSlots)
            {
                bool needGun = gunCount < minGuns;
                bool needItem = itemCount < minItems;

                if (!needGun && !needItem) break;

                // --- 核心修复 3：绝对防呆锁！如果这把枪/道具已经是保底的命根子了，绝对不换 ---
                if (oldSlot.item is Gun && gunCount <= minGuns) continue;
                if (IsRealItem(oldSlot.item) && itemCount <= minItems) continue;

                PickupObject newItem = null;

                if (needGun)
                {
                    newItem = LootTableHelper.GetRandomGun();
                    if (newItem != null)
                    {
                        // 动态更新计数：如果旧的是多余的真道具，被换成枪了，道具计数要-1
                        if (IsRealItem(oldSlot.item)) itemCount--;
                        gunCount++;
                    }
                }
                else if (needItem)
                {
                    int retries = 10;
                    while (retries > 0)
                    {
                        newItem = LootTableHelper.GetRandomItem();
                        if (IsRealItem(newItem)) break;
                        retries--;
                    }
                    if (newItem != null && IsRealItem(newItem))
                    {
                        // 动态更新计数：如果旧的是多余的枪，被换成道具了，枪计数要-1
                        if (oldSlot.item is Gun) gunCount--;
                        itemCount++;
                    }
                }

                if (newItem != null)
                {
                    string oldName = oldSlot.item != null ? oldSlot.item.EncounterNameOrDisplayName : "空位";
                    int oldId = oldSlot.item != null ? oldSlot.item.PickupObjectId : -1;

                    Plugin.Log.LogInfo($"[BetterShop] >>> 触发替换：把 [{oldName}] (ID:{oldId}) 替换成了 [{newItem.EncounterNameOrDisplayName}] (ID:{newItem.PickupObjectId})");

                    // 极其安全的原生重置
                    oldSlot.Initialize(newItem, __instance);
                }
            }

            Plugin.Log.LogInfo($"[BetterShop] 替换完成！");
            Plugin.Log.LogInfo("==================================================\n");
        }
    }
}