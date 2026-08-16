using System;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using Vintagestory.API.Net;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using KRPGLib.Enchantment.API;
using Vintagestory.GameContent;
using ProtoBuf;
using System.Linq;

namespace KRPGLib.Enchantment.Net
{
    public class KRPGENetSystem : ModSystem
    {
        ICoreServerAPI sApi;
        // Load before anything else, especially before ConfigLib does anything.
        public override double ExecuteOrder()
        {
            return 0;
        }
        public override void StartPre(ICoreAPI api)
        {
            api.Network
                .RegisterChannel("krpgenchantment")
                .RegisterMessageType(typeof(EnchantRegistryPacket))
                .RegisterMessageType(typeof(ResponsePacket))
                .RegisterMessageType(typeof(ModifierRequestPacket))
                .RegisterMessageType(typeof(ModifierResponsePacket))
            ;
        }
        #region Server
        IServerNetworkChannel serverChannel;
        public override void StartServerSide(ICoreServerAPI api)
        {
            sApi = api;

            serverChannel = sApi.Network.GetChannel("krpgenchantment")
                .SetMessageHandler<ResponsePacket>(OnClientResponse)
                .SetMessageHandler<ModifierRequestPacket>(OnModifierRequest)
            ;
        }
        private void OnModifierRequest(IServerPlayer fromPlayer, ModifierRequestPacket packet)
        {
            IEnchantment ench = sApi.EnchantAccessor().GetEnchantment(packet.EnchantCode);
            object obj = ench.Modifiers.TryGetValue(packet.ModifierKey, out obj);
            string s = obj.ToString();
            ModifierResponsePacket response = new ModifierResponsePacket()
            {
                EnchantCode = packet.EnchantCode,
                ModifierValue = s
            };
            serverChannel.SendPacket(response, new IServerPlayer[] { fromPlayer });
        }
        private void OnClientResponse(IPlayer fromPlayer, ResponsePacket packet)
        {
            sApi.Logger.Event(
                "[KRPGEnchantment] Received net response {0}: {1}. from {2}.", packet.ResponseType.ToString(), packet.Message, fromPlayer.PlayerName);
        }
        public void SendEnchantRegistryPacket(IServerPlayer player, string keyCode, EnchantmentProperties props, Type enchantmentType)
        {
            List<string> modKeys = new List<string>();
            List<string> modVals = new List<string>();
            foreach (KeyValuePair<string, object> pair in props.Modifiers)
            {
                modKeys.Add(pair.Key);
                modVals.Add(pair.Value.ToString());
            }
            serverChannel.SendPacket(
                new EnchantRegistryPacket()
                {
                    KeyCode = keyCode,
                    EnchantmentType = enchantmentType.FullName.ToString(),
                    Enabled = props.Enabled,
                    Code = props.Code,
                    Category = props.Category,
                    LoreCode = props.LoreCode,
                    LoreChapterID = props.LoreChapterID,
                    MaxTier = props.MaxTier,
                    ValidToolTypes = props.ValidToolTypes,
                    ModKeys = modKeys,
                    ModVals = modVals,
                    Version = props.Version
                }, 
                [player]
            );
        }
        #endregion
        #region Client
        IClientNetworkChannel clientChannel;
        ICoreClientAPI cApi;
        public override void StartClientSide(ICoreClientAPI api)
        {
            cApi = api;

            clientChannel = api.Network.GetChannel("krpgenchantment")
                .SetMessageHandler<EnchantRegistryPacket>(OnServerERSync)
                .SetMessageHandler<ResponsePacket>(OnServerResponse)
                .SetMessageHandler<ModifierResponsePacket>(OnServerModifierResponse)
            ;
        }
        private void OnServerResponse(ResponsePacket packet)
        {
            cApi.Logger.Event(
                "[KRPGEnchantment] Received net response {0}: {1}. from {2}.", packet.ResponseType.ToString(), packet.Message, cApi.World.Player.PlayerName);
        }
        private void OnServerModifierResponse(ModifierResponsePacket packet)
        {
            cApi.Logger.Event(
                "[KRPGEnchantment] Received net modifier response {0}: {1}. from {2}.", 
                packet.EnchantCode, packet.ModifierValue, cApi.World.Player.PlayerName);

            
        }
        private void OnServerERSync(EnchantRegistryPacket packet)
        {
            cApi.Logger.Event("Received an EnchantRegistryPacket from the server.");
            EnchantModifiers mods = new EnchantModifiers();
            for(int i = 0; i < packet.ModKeys.Count; i++)
            {
                mods.Add(packet.ModKeys[i], packet.ModVals[i]);
            }
            EnchantmentProperties props = new EnchantmentProperties()
            {
                Enabled = packet.Enabled,
                Code = packet.Code,
                Category = packet.Category,
                LoreCode = packet.LoreCode,
                LoreChapterID = packet.LoreChapterID,
                MaxTier = packet.MaxTier,
                ValidToolTypes = packet.ValidToolTypes,
                Modifiers = mods,
                Version = packet.Version
            };
            Type eType = typeof(Enchantment).Assembly.GetType(packet.EnchantmentType);
            bool reg = cApi.EnchantAccessor().RegisterEnchantmentClass(packet.KeyCode, props, eType);
            if (!reg)
            {
                clientChannel.SendPacket(new ResponsePacket()
                {
                    ResponseType = EnumNetResponse.Error,
                    Message = "Failed to register Enchantment!"
                });
            }
            else
            {
                clientChannel.SendPacket(new ResponsePacket()
                {
                    ResponseType = EnumNetResponse.OK,
                    Message = "Enchant Registry Received!"
                });
            }
            
        }
        #endregion
    }
}