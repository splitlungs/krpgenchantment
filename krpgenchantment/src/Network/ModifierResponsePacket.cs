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
    public class ModifierResponsePacket : IByteSerializable
    {
        [ProtoMember(1)]
        public string EnchantCode;
        [ProtoMember(2)]
        public string ModifierValue;

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(EnchantCode);
            writer.Write(ModifierValue);
        }
        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            EnchantCode = reader.ReadString();
            ModifierValue = reader.ReadString();
        }
    }
}
