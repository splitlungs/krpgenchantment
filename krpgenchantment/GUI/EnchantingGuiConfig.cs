using ProtoBuf;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace KRPGLib.Enchantment
{
    public class EnchantingGuiConfig
    {
        public string customFont = "dragon_alphabet.ttf";
        public int rowCount = 3;
        public List<string> enchantNames = null;
        public List<string> enchantNamesEncrypted = null;
        public int selectedEnchant = -1;
        public string outputText = "Enchantment: ";
        public double inputEnchantTime = 0;
        public double maxEnchantTime = 1;
        public bool nowEnchanting = false;

        // Probably not going to convert this to JSON, but idk it's here JIC
        public EnchantingGuiConfig Clone()
        {
            return new EnchantingGuiConfig
            {
                customFont = customFont,
                rowCount = rowCount,
                enchantNames = enchantNames,
                enchantNamesEncrypted = enchantNamesEncrypted,
                selectedEnchant = selectedEnchant,
                outputText = outputText,
                inputEnchantTime = inputEnchantTime,
                maxEnchantTime = maxEnchantTime,
                nowEnchanting = nowEnchanting
            };
        }

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(customFont);
            writer.Write(rowCount);
            writer.Write(enchantNames.Count);
            for (int i = 0; i < enchantNames.Count; i++)
            {
                writer.Write(enchantNames[i]);
            }
            writer.Write(enchantNamesEncrypted.Count);
            for (int i = 0; i < enchantNamesEncrypted.Count; i++)
            {
                writer.Write(enchantNamesEncrypted[i]);
            }
            writer.Write(selectedEnchant);
            writer.Write(outputText);
            writer.Write((double)inputEnchantTime);
            writer.Write((double)maxEnchantTime);
            writer.Write(nowEnchanting);
        }
        public void FromBytes(BinaryReader reader, IWorldAccessor resolver) 
        {
            customFont = reader.ReadString();
            rowCount = reader.ReadInt32();
            int num = reader.ReadInt32();
            enchantNames = new List<string>();
            for (int i = 0; i < num; i++)
            {
                enchantNames.Add(reader.ReadString());
            }
            num = reader.ReadInt32();
            enchantNamesEncrypted = new List<string>();
            for (int i = 0; i < num; i++)
            {
                enchantNamesEncrypted.Add(reader.ReadString());
            }
            selectedEnchant = reader.ReadInt32();
            outputText = reader.ReadString();
            inputEnchantTime = reader.ReadDouble();
            maxEnchantTime = reader.ReadDouble();
            nowEnchanting = reader.ReadBoolean();
        }
    }
}
