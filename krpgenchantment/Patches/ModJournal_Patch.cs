using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;

namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public class ModJournal_Patch
    {
        [HarmonyPatch(typeof(ModJournal), "getNextUndiscoveredChapter")]
        public static bool Prefix(ModJournal __instance, IPlayer plr, JournalAsset asset, ref LoreDiscovery __result, ref Dictionary<string, Dictionary<string, LoreDiscovery>> ___loreDiscoveryiesByPlayerUid, ref ICoreServerAPI ___sapi)
        {
            if (asset.Category == "enchantment")
            {
                ___loreDiscoveryiesByPlayerUid.TryGetValue(plr.PlayerUID, out var value);
                if (value == null)
                {
                    value = (___loreDiscoveryiesByPlayerUid[plr.PlayerUID] = new Dictionary<string, LoreDiscovery>());
                }
    
                if (!value.ContainsKey(asset.Code))
                {
                    __result = new LoreDiscovery
                    {
                        Code = asset.Code,
                        ChapterIds = new List<int> { 0 }
                    };
                }
    
                LoreDiscovery loreDiscovery = value[asset.Code];
                List<int> pieces = new List<int>();
                for (int i = 0; i < asset.Pieces.Length; i++)
                {
                    if (!loreDiscovery.ChapterIds.Contains(i))
                    {
                        pieces.Add(i);
                    }
                }
                if (pieces.Count > 0)
                {
                    int piece = ___sapi.World.Rand.Next(0, pieces.Count + 1);
                    __result = new LoreDiscovery
                    {
                        ChapterIds = new List<int> { pieces[piece] },
                        Code = loreDiscovery.Code
                    };
                }
                __result = null;
                return false;
            }
            else
                return true;
        }
    }
}
