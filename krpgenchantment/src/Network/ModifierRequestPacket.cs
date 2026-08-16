using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace KRPGLib.Enchantment.Net
{
    [ProtoContract]
    public class ModifierRequestPacket : IByteSerializable
    {
        [ProtoMember(1)]
        public string EnchantCode;
        [ProtoMember(2)]
        public string ModifierKey;

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(EnchantCode);
            writer.Write(ModifierKey);
        }
        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            EnchantCode = reader.ReadString();
            ModifierKey = reader.ReadString();
        }
    }
}
