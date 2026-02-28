using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BetterShop.Patches;
using HarmonyLib;

namespace BetterShop
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    // 声明硬依赖 Alexandria
    [BepInDependency("alexandria.etgmod.alexandria", BepInDependency.DependencyFlags.HardDependency)]

    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.bettershop.etg";
        public const string PLUGIN_NAME = "BetterShop";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static ManualLogSource Log;
        internal static Plugin Instance;

        // ── 配置项 ──
        internal static ConfigEntry<int> MinGuns;
        internal static ConfigEntry<int> MinItems;
        internal static ConfigEntry<bool> ExtraNpcEnabled;
        internal static ConfigEntry<bool> BulletHellShopEnabled;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            // 绑定配置
            MinGuns = Config.Bind("商店保底", "MinGuns", 1, "商店中至少包含的枪械数量");
            MinItems = Config.Bind("商店保底", "MinItems", 1, "商店中至少包含的道具数量");
            ExtraNpcEnabled = Config.Bind("额外NPC", "Enabled", true, "是否在每层额外生成一个 NPC");
            BulletHellShopEnabled = Config.Bind("枪弹地狱", "ShopEnabled", true, "是否在枪弹地狱（第六层）生成商人");

            // 应用所有 Harmony 补丁
            var harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll();

            HellShopGenerator.Init();
            ExtraNpcInjector.Init();

            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} 已加载！Alexandria 依赖注入成功！");
        }
    }
}