using System;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace KRPGLib.Enchantment
{
    [ProtoContract]
    public class EnchantingGuiPacket
    {
        [ProtoMember(1)]
        public int SelectedEnchant;
    }
}
