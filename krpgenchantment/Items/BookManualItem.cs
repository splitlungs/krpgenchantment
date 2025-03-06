﻿using System;
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
            handling = EnumHandHandling.PreventDefault;

            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            dsc.Append(Lang.Get("krpgenchantment:loretype-enchantment") + "\n" + "\n");

            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            string @string = base.GetHeldItemName(itemStack);
            var cID = itemStack.Attributes["chapterIds"] as IntArrayAttribute;
            if (cID != null)
            {
                foreach (KeyValuePair<string, int> keyValuePair in EnchantingConfigLoader.Config.LoreIDs)
                {
                    if (cID.value.Contains(keyValuePair.Value))
                        @string += ": " + Lang.Get("krpgenchantment:" + keyValuePair.Key);
                }
            }

            return @string;
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