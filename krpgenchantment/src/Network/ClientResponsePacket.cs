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
    /// <summary>
    /// Used by ResponsePacket during generic request/responses.
    /// </summary>
    public enum EnumNetResponse
    {
        OK = 0,
        Error = 1,
        Warning = 2,
        Info = 3
    }
    /// <summary>
    /// Generic packet for client/server communication. Sent in response to a RequestPacket.
    /// </summary>
    [ProtoContract]
    public class ResponsePacket : IByteSerializable
    {
        [ProtoMember(1)]
        public EnumNetResponse ResponseType;
        [ProtoMember(2)]
        public string Message;

        public void ToBytes(BinaryWriter writer)
        {
            int rt = (int)ResponseType;
            writer.Write(rt);
            writer.Write(Message);
        }
        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            int dt = reader.ReadInt32();
            ResponseType = (EnumNetResponse)dt;
            Message = reader.ReadString();
        }
    }
}
