// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Vintagestory.API.Client;
// using Vintagestory.API.Common;
// using Vintagestory.API.Config;
// using Vintagestory.API.Datastructures;
// using Vintagestory.API.Server;
// using Vintagestory.API.Util;
// using Vintagestory.GameContent;
// 
// namespace KRPGLib.Enchantment
// {
//     /// <summary>
//     /// This exists as a temporary measure until the Lore updates are complete. It should only read legacy lore.
//     /// </summary>
//     public class LegacyModJournal : ModSystem
//     {
//         private ICoreServerAPI sapi;
// 
//         private Dictionary<string, Journal> journalsByPlayerUid = new Dictionary<string, Journal>();
// 
//         private Dictionary<string, Dictionary<string, LoreDiscovery>> loreDiscoveryiesByPlayerUid = new Dictionary<string, Dictionary<string, LoreDiscovery>>();
// 
//         private Dictionary<string, JournalAsset> journalAssetsByCode;
// 
//         private IServerNetworkChannel serverChannel;
// 
//         private ICoreClientAPI capi;
// 
//         private IClientNetworkChannel clientChannel;
// 
//         private Journal ownJournal = new Journal();
// 
//         private GuiDialogJournal dialog;
// 
//         public override bool ShouldLoad(EnumAppSide side)
//         {
//             return true;
//         }
// 
//         public override void Start(ICoreAPI api)
//         {
//             base.Start(api);
//         }
// 
//         public override void StartClientSide(ICoreClientAPI api)
//         {
//             capi = api;
//         }
// 
//         public override void StartServerSide(ICoreServerAPI api)
//         {
//             sapi = api;
//             api.Event.PlayerJoin += OnPlayerJoin;
//             api.Event.SaveGameLoaded += OnSaveGameLoaded;
//         }
// 
//         private void OnSaveGameLoaded()
//         {
//             try
//             {
//                 byte[] data = sapi.WorldManager.SaveGame.GetData("journalItemsByPlayerUid");
//                 if (data != null)
//                 {
//                     journalsByPlayerUid = SerializerUtil.Deserialize<Dictionary<string, Journal>>(data);
//                 }
//             }
//             catch (Exception e)
//             {
//                 sapi.World.Logger.Error("Failed loading journalItemsByPlayerUid. Resetting.");
//                 sapi.World.Logger.Error(e);
//             }
// 
//             if (journalsByPlayerUid == null)
//             {
//                 journalsByPlayerUid = new Dictionary<string, Journal>();
//             }
// 
//             try
//             {
//                 byte[] data2 = sapi.WorldManager.SaveGame.GetData("loreDiscoveriesByPlayerUid");
//                 if (data2 != null)
//                 {
//                     loreDiscoveryiesByPlayerUid = SerializerUtil.Deserialize<Dictionary<string, Dictionary<string, LoreDiscovery>>>(data2);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 sapi.World.Logger.Error("Failed loading loreDiscoveryiesByPlayerUid. Resetting. Exception: {0}", ex);
//             }
// 
//             if (loreDiscoveryiesByPlayerUid == null)
//             {
//                 loreDiscoveryiesByPlayerUid = new Dictionary<string, Dictionary<string, LoreDiscovery>>();
//             }
//         }
// 
//         private void OnPlayerJoin(IServerPlayer byPlayer)
//         {
//             // if (journalsByPlayerUid.TryGetValue(byPlayer.PlayerUID, out var value))
//             // {
//             //     serverChannel.SendPacket(value, byPlayer);
//             // }
//         }
// 
//         public bool DidDiscoverLore(string playerUid, string code, int chapterId)
//         {
//             if (!journalsByPlayerUid.TryGetValue(playerUid, out var value))
//             {
//                 return false;
//             }
// 
//             for (int i = 0; i < value.Entries.Count; i++)
//             {
//                 if (!(value.Entries[i].LoreCode == code))
//                 {
//                     continue;
//                 }
// 
//                 JournalEntry journalEntry = value.Entries[i];
//                 for (int j = 0; j < journalEntry.Chapters.Count; j++)
//                 {
//                     if (journalEntry.Chapters[j].ChapterId == chapterId)
//                     {
//                         return true;
//                     }
//                 }
// 
//                 break;
//             }
// 
//             return false;
//         }
// 
//         private bool DidDiscover(string playerUid, string loreCode, int chapterId)
//         {
//             if (!loreDiscoveryiesByPlayerUid.TryGetValue(playerUid, out var value))
//             {
//                 return false;
//             }
// 
//             if (!value.TryGetValue(loreCode, out var value2))
//             {
//                 return false;
//             }
// 
//             if (!value2.ChapterIds.Contains(chapterId))
//             {
//                 return false;
//             }
// 
//             return true;
//         }
// 
//         private void ensureJournalAssetsLoaded()
//         {
//             if (journalAssetsByCode == null)
//             {
//                 journalAssetsByCode = new Dictionary<string, JournalAsset>();
//                 JournalAsset[] array = sapi.World.AssetManager.GetMany<JournalAsset>(sapi.World.Logger, "config/lore/").Values.ToArray();
//                 foreach (JournalAsset journalAsset in array)
//                 {
//                     journalAssetsByCode[journalAsset.Code] = journalAsset;
//                 }
//             }
//         }
//     }
// }