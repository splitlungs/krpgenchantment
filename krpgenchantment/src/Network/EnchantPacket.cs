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
    /// <summary>
    /// Contract for transferring basic information on server-configured Enchantments.
    /// </summary>
    [ProtoContract]
    public class EnchantPacket : IByteSerializable
    {
        [ProtoMember(1)]
        public string Code;
        [ProtoMember(2)]
        public int Power;
        [ProtoMember(3)]
        public EnchantModifiers Modifiers = new EnchantModifiers();

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(Code);
            writer.Write(Power);
            int count = Modifiers.Count;
            writer.Write(count);
            foreach (KeyValuePair<string, object> pair in Modifiers)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value.ToString());
            }
        }
        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            Code = reader.ReadString();
            Power = reader.ReadInt32();
            Modifiers.Clear();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string s = reader.ReadString();
                object o = (object)reader.ReadString();
                Modifiers.Add(s, o);
            }
        }
    }
}
