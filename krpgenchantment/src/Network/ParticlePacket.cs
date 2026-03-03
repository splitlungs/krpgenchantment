using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace KRPGLib.Enchantment
{
    [ProtoContract]
    public class ParticlePacket : IByteSerializable
    {
        [ProtoMember(1)]
        public float Amount;
        [ProtoMember(2)]
        public EnumDamageType DamageType = EnumDamageType.Injury;

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(Amount);
            int dt = (int)DamageType;
            writer.Write(dt);
        }
        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            Amount = reader.ReadSingle();
            int dt = reader.ReadInt32();
            DamageType = (EnumDamageType)dt;
        }
    }
}
