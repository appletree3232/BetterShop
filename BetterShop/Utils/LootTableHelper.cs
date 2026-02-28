using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterShop.Utils
{

    public static class LootTableHelper
    {

        public static PickupObject GetRandomGun()
        {
            try
            {
                List<PickupObject> allGuns = new List<PickupObject>();
                List<PickupObject> preferredGuns = new List<PickupObject>();

                for (int i = 0; i < PickupObjectDatabase.Instance.Objects.Count; i++)
                {
                    PickupObject obj = PickupObjectDatabase.Instance.Objects[i];
                    if (obj == null) continue;
                    if (!(obj is Gun)) continue;
                    if (obj.quality == PickupObject.ItemQuality.EXCLUDED) continue;

                    allGuns.Add(obj);

                    // D~S 品质的枪优先选择（包含 S 级）
                    if (obj.quality >= PickupObject.ItemQuality.D &&
                        obj.quality <= PickupObject.ItemQuality.S)
                    {
                        preferredGuns.Add(obj);
                    }
                }

                if (allGuns.Count == 0)
                {
                    Plugin.Log.LogWarning("[LootTable] 枪械库为空");
                    return null;
                }


                List<PickupObject> pool = preferredGuns.Count > 0 ? preferredGuns : allGuns;
                return pool[UnityEngine.Random.Range(0, pool.Count)];
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("[LootTable] 获取随机枪失败: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 从游戏的道具库中随机获取一个被动道具或主动道具。
        /// </summary>
        public static PickupObject GetRandomItem()
        {
            try
            {
                List<PickupObject> allItems = new List<PickupObject>();
                List<PickupObject> preferredItems = new List<PickupObject>();

                for (int i = 0; i < PickupObjectDatabase.Instance.Objects.Count; i++)
                {
                    PickupObject obj = PickupObjectDatabase.Instance.Objects[i];
                    if (obj == null) continue;
                    if (!(obj is PassiveItem) && !(obj is PlayerItem)) continue;
                    if (obj.quality == PickupObject.ItemQuality.EXCLUDED) continue;

                    allItems.Add(obj);

                    if (obj.quality >= PickupObject.ItemQuality.D &&
                        obj.quality <= PickupObject.ItemQuality.S)
                    {
                        preferredItems.Add(obj);
                    }
                }

                if (allItems.Count == 0)
                {
                    Plugin.Log.LogWarning("[LootTable] 道具库为空");
                    return null;
                }

                List<PickupObject> pool = preferredItems.Count > 0 ? preferredItems : allItems;
                return pool[UnityEngine.Random.Range(0, pool.Count)];
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("[LootTable] 获取随机道具失败: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 获取指定品质范围内的随机枪械。
        /// </summary>
        public static PickupObject GetRandomGunOfQuality(
            PickupObject.ItemQuality minQuality,
            PickupObject.ItemQuality maxQuality)
        {
            try
            {
                List<PickupObject> candidates = new List<PickupObject>();

                for (int i = 0; i < PickupObjectDatabase.Instance.Objects.Count; i++)
                {
                    PickupObject obj = PickupObjectDatabase.Instance.Objects[i];
                    if (obj == null) continue;
                    if (!(obj is Gun)) continue;
                    if (obj.quality < minQuality || obj.quality > maxQuality) continue;
                    if (obj.quality == PickupObject.ItemQuality.EXCLUDED) continue;

                    candidates.Add(obj);
                }

                if (candidates.Count == 0) return null;
                return candidates[UnityEngine.Random.Range(0, candidates.Count)];
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("[LootTable] 获取指定品质枪失败: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 获取指定品质范围内的随机道具。
        /// </summary>
        public static PickupObject GetRandomItemOfQuality(
            PickupObject.ItemQuality minQuality,
            PickupObject.ItemQuality maxQuality)
        {
            try
            {
                List<PickupObject> candidates = new List<PickupObject>();

                for (int i = 0; i < PickupObjectDatabase.Instance.Objects.Count; i++)
                {
                    PickupObject obj = PickupObjectDatabase.Instance.Objects[i];
                    if (obj == null) continue;
                    if (!(obj is PassiveItem) && !(obj is PlayerItem)) continue;
                    if (obj.quality < minQuality || obj.quality > maxQuality) continue;
                    if (obj.quality == PickupObject.ItemQuality.EXCLUDED) continue;

                    candidates.Add(obj);
                }

                if (candidates.Count == 0) return null;
                return candidates[UnityEngine.Random.Range(0, candidates.Count)];
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("[LootTable] 获取指定品质道具失败: " + ex.Message);
                return null;
            }
        }
    }
}
