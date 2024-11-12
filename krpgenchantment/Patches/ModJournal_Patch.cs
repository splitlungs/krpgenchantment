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

// namespace KRPGLib.Enchantment
// {
//     [HarmonyPatch]
//     public class ModJournal_Patch
//     {
//         [HarmonyPatch(typeof(ModJournal), "DidDiscoverLore")]
//         public static bool Postfix(ModJournal __instance, string playerUid, string code, int chapterId)
//         {
//             return true;
//         }
//     }
// }
