using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace KRPGLib.Enchantment.Net
{
    /// <summary>
    /// Server to client packet to be converted to Enchantment Properties when registered to a client's Enchantment Registry.
    /// </summary>
    [ProtoContract]
    public class EnchantRegistryPacket
    {
        [ProtoMember(1)]
        public string KeyCode;
        [ProtoMember(2)]
        public string EnchantmentType;
        [ProtoMember(3)]
        public bool Enabled = true;
        [ProtoMember(4)]
        public string Code;
        [ProtoMember(5)]
        public string Category;
        [ProtoMember(6)]
        public string LoreCode;
        [ProtoMember(7)]
        public int LoreChapterID;
        [ProtoMember(8)]
        public int MaxTier;
        [ProtoMember(9)]
        public List<string> ValidToolTypes;
        [ProtoMember(10)]
        public List<string> ModKeys;
        [ProtoMember(11)]
        public List<string> ModVals;
        [ProtoMember(12)]
        public float Version;
    }
}
