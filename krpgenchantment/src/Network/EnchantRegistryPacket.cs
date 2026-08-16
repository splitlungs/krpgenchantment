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

        // public void ToBytes(BinaryWriter writer)
        // {
        //     writer.Write(KeyCode);
        //     writer.Write(EnchantmentType.ToString());
        //     writer.Write(Enabled);
        //     writer.Write(Code);
        //     writer.Write(Category);
        //     writer.Write(LoreCode);
        //     writer.Write(LoreChapterID);
        //     writer.Write(MaxTier);
        //     writer.Write(ValidToolTypes.Count);
        //     foreach (string tool in ValidToolTypes)
        //     {
        //         writer.Write(tool);
        //     }
        //     // writer.Write(Modifiers.Count);
        //     // foreach (KeyValuePair<string, object> pair in Modifiers)
        //     // {
        //     //     writer.Write(pair.Key);
        //     //     writer.Write(pair.Value.ToString());
        //     // }
        //     writer.Write(Version);
        // }
        // public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        // {
        //     KeyCode = reader.ReadString();
        //     EnchantmentType = reader.ReadString();
        //     Enabled = reader.ReadBoolean();
        //     Code = reader.ReadString();
        //     Category = reader.ReadString();
        //     LoreCode = reader.ReadString();
        //     LoreChapterID = reader.ReadInt32();
        //     MaxTier = reader.ReadInt32();
        //     int toolCount = reader.ReadInt32();
        //     ValidToolTypes = new List<string>();
        //     for (int i = 0; i < toolCount; i++)
        //     {
        //         ValidToolTypes.Add(reader.ReadString());
        //     }
        //     // int modCount = reader.ReadInt32();
        //     // Modifiers = new EnchantModifiers();
        //     // for (int i = 0; i < modCount; i++)
        //     // {
        //     //     string key = reader.ReadString();
        //     //     object value = reader.ReadString();
        //     //     Modifiers.Add(key, value);
        //     // }
        //     Version = reader.ReadSingle();
        // }
    }
}
