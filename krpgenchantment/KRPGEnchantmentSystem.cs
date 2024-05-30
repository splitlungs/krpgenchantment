using Vintagestory.API.Common;
using HarmonyLib;
using System.Reflection;

namespace KRPGLib.Enchantment
{
    public class KRPGEnchantmentSystem : ModSystem
    {
        ICoreAPI Api;
        
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            Api = api;

            api.RegisterEntity("EnchantedEntityProjectile", typeof(EnchantedEntityProjectile));
            api.RegisterCollectibleBehaviorClass("EnchantmentBehavior", typeof(EnchantmentBehavior));
            api.RegisterEntityBehaviorClass("EnchantmentEntityBehavior", typeof(EnchantmentEntityBehavior));
            api.RegisterBlockClass("EnchantingBlock", typeof(EnchantingBlock));
            api.RegisterBlockEntityClass("EnchantingBE", typeof(EnchantingBE));
            api.RegisterItemClass("ReagentItem", typeof(ReagentItem));
            DoHarmonyPatch(api);
            Api.Logger.Event("KRPG Enchantment loaded.");
        }
        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);

        }
        private static void DoHarmonyPatch(ICoreAPI api)
        {
            if (KRPGEnchantmentSystem.harmony == null)
            {
                KRPGEnchantmentSystem.harmony = new Harmony("KRPGEnchantmentPatch");
                KRPGEnchantmentSystem.harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
        }

        private static Harmony harmony;
    }
}
