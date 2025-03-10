using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class EnchantersManualItem : ItemBook
    {
        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            string @string = itemslot.Itemstack.Attributes.GetString("category");
            if (byEntity.World.Side == EnumAppSide.Server && @string != null)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] Attempting to push enchantingLoreDiscovery event.");
                IPlayer player = null;
                if (byEntity is EntityPlayer)
                {
                    player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                }

                if (!(player is IServerPlayer))
                {
                    return;
                }

                TreeAttribute treeAttribute = new TreeAttribute();
                treeAttribute.SetString("playeruid", player?.PlayerUID);
                treeAttribute.SetString("category", @string);
                treeAttribute.SetItemstack("itemstack", itemslot.Itemstack.Clone());
                api.Event.PushEvent("loreDiscovery", treeAttribute);
            }

            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            handling = EnumHandHandling.PreventDefault;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            dsc.Append(Lang.Get("krpgenchantment:loretype-enchantment") + "\n" + "\n");

            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            if (Code == null)
            {
                return "Invalid block, id " + Id;
            }
            
            string text = ItemClass.Name();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(Lang.GetMatching(Code?.Domain + ":" + text + "-" + Code?.Path));
            // var cID = itemStack.Attributes["chapterIds"] as IntArrayAttribute;
            var cID = itemStack.Attributes["textCodes"] as StringArrayAttribute;
            if (cID != null)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] Enchanter's Manual has been read, and should append its enchantment to its name.");

                char[] prefixes = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.' };
                string s = Lang.Get(cID.value[0]).TrimStart(prefixes);
                stringBuilder.Append(":" + s);
                return stringBuilder.ToString();
            }

            return base.GetHeldItemName(itemStack);
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[1]
            {
            new WorldInteraction
            {
                ActionLangCode = "heldhelp-addtojournal",
                MouseButton = EnumMouseButton.Right
            }
            }.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}