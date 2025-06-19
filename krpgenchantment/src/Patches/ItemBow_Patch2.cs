using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.GameContent;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace KRPGLib.Enchantment
{
    // Pretend this doesn't exist. It's just for testing.
    //
    // [HarmonyPatch]
    public static class ItemBow_Patch2
    {

        //     // Precompiled with dnSpy
        //  /* 
        //     (70,4)-(70,93) main.cs 
        //     0x00000218 04            IL_0218: ldarg.2
        //     0x00000219 6F????????    IL_0219: callvirt  instance class [VintagestoryAPI]Vintagestory.API.Common.ItemStack [VintagestoryAPI]Vintagestory.API.Common.ItemSlot::get_Itemstack()
        //     0x0000021E 6F????????    IL_021E: callvirt  instance class [VintagestoryAPI]Vintagestory.API.Datastructures.ITreeAttribute [VintagestoryAPI]Vintagestory.API.Common.ItemStack::get_Attributes()
        //     0x00000223 72????????    IL_0223: ldstr     "enchantments"
        //     0x00000228 6F????????    IL_0228: callvirt  instance class [VintagestoryAPI]Vintagestory.API.Datastructures.ITreeAttribute [VintagestoryAPI]Vintagestory.API.Datastructures.ITreeAttribute::GetOrAddTreeAttribute(string)
        //     0x0000022D 1307          IL_022D: stloc.s   bowTree
        //     (71,4)-(71,24) main.cs 
        //     0x0000022F 1107          IL_022F: ldloc.s   bowTree
        //     0x00000231 2C0E          IL_0231: brfalse.s IL_0241
        // 
        //     (73,5)-(73,54) main.cs 
        //     0x00000233 1106          IL_0233: ldloc.s   entityarrow
        //     0x00000235 7B????????    IL_0235: ldfld     class [VintagestoryAPI]Vintagestory.API.Datastructures.SyncedTreeAttribute [VintagestoryAPI]Vintagestory.API.Common.Entities.Entity::WatchedAttributes
        //     0x0000023A 1107          IL_023A: ldloc.s   bowTree
        //     0x0000023C 6F????????    IL_023C: callvirt  instance void [VintagestoryAPI]Vintagestory.API.Datastructures.TreeAttribute::MergeTree(class [VintagestoryAPI]Vintagestory.API.Datastructures.ITreeAttribute)
        //  */
        // 
        // 
        //     // 0x000D0000 1105          IL_01F8: ldloc.s   entityarrow
        //     // 0x000D0002 02            IL_01FA: ldarg.0
        //     // [HarmonyPatch(typeof(ItemBow), "OnHeldInteractStop")]
        //     // static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //     // {
        //     //     var foundMassUsageMethod = false;
        //     //     var startIndex = -1;
        //     //     var endIndex = -1;
        //     // 
        //     //     var codes = new List<CodeInstruction>(instructions);
        //     //     for (var i = 0; i < codes.Count; i++)
        //     //     {
        //     //         if (codes[i].opcode == OpCodes.Ldloc_S)
        //     //         {
        //     //             if (foundMassUsageMethod)
        //     //             {
        //     //                 // Log.Error("END " + i);
        //     // 
        //     //                 endIndex = i; // include current 'ret'
        //     //                 break;
        //     //             }
        //     //             else
        //     //             {
        //     //                 // Log.Error("START " + (i + 1));
        //     // 
        //     //                 startIndex = i + 1; // exclude current 'ret'
        //     // 
        //     //                 for (var j = startIndex; j < codes.Count; j++)
        //     //                 {
        //     //                     if (codes[j].opcode == OpCodes.Ret)
        //     //                         break;
        //     //                     var strOperand = codes[j].operand as string;
        //     //                     if (strOperand == "TooBigCaravanMassUsage")
        //     //                     {
        //     //                         foundMassUsageMethod = true;
        //     //                         break;
        //     //                     }
        //     //                 }
        //     //             }
        //     //         }
        //     //     }
        //     //     if (startIndex > -1 && endIndex > -1)
        //     //     {
        //     //         // we cannot remove the first code of our range since some jump actually jumps to
        //     //         // it, so we replace it with a no-op instead of fixing that jump (easier).
        //     //         codes[startIndex].opcode = OpCodes.Ldarg_0;
        //     //         codes.RemoveRange(startIndex + 1, endIndex - startIndex - 1);
        //     //     }
        //     // 
        //     //     return codes.AsEnumerable();
        //     // }
        // }

        //   static MethodBase TargetMethod()
        //   {
        //       var method = AccessTools.DeclaredMethod(typeof(ItemBow), "OnHeldInteractStop");
        //       // if (method == null)
        //       // {
        //       //     Console.WriteLine("KRPG Enchantment: Could not find ItemBow.OnHeldInteractStop()");
        //       // }
        //       // else
        //       // {
        //       //     Console.WriteLine($"Patching method: {method.DeclaringType}.{method.Name}");
        //       // }
        //       return method;
        //   }
        //   
        //   static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        //   {
        //       var code = new List<CodeInstruction>(instructions);
        // 
        //       int insertionIndex = -1;
        //       Label returnEPLabel = generator.DefineLabel();
        //       for (int i = 0; i < code.Count - 1; i++) // -1 since we will be checking i + 1
        //       {
        //           if (code[i].opcode == OpCodes.Callvirt && code[i].Calls(AccessTools.DeclaredMethod(typeof(EntityProjectile), "SetRotation")) && code[i + 1].opcode == OpCodes.Ldarg_3)
        //           {
        //               insertionIndex = i;
        //               code[i].labels.Add(returnEPLabel);
        //               break;
        //           }
        //       }
        //       var instructionsToInsert = new List<CodeInstruction>();
        //       instructionsToInsert.Add(new(CodeInstruction(OpCodes.Ldloc_S)))
        //       instructionsToInsert.Add(new CodeInstruction(OpCodes.Call,
        //               AccessTools.Method(typeof(ItemBow_Patch2), nameof(InjectEnchantments))));
        //       instructionsToInsert.Add(new CodeInstruction(OpCodes.Brfalse_S, returnEPLabel));
        // 
        // 
        //       if (insertionIndex != -1)
        //       {
        //           code.InsertRange(insertionIndex, instructionsToInsert);
        //       }
        //       return code;
        // 
        //       // Find where entityarrow.DamageTier is set
        //       // for (int i = 0; i < codes.Count; i++)
        //       // {
        //       //     if (codes[i].opcode == OpCodes.Callvirt &&
        //       //         codes[i - 1].opcode == OpCodes.Ldstr &&
        //       //         codes[i - 1].operand.ToString() == "damageTier")
        //       //     {
        //       //         // Insert code after damageTier is set
        //       //         codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_2)); // Load slot
        //       //         codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldarg_1)); // Load entityarrow
        //       // 
        //       //         // Call InjectEnchantments(ItemSlot, EntityProjectile)
        //       //         codes.Insert(i + 3, new CodeInstruction(OpCodes.Call,
        //       //             AccessTools.Method(typeof(ItemBow_Patch2), nameof(InjectEnchantments))));
        //       // 
        //       //         break;
        //       //     }
        //       // }
        //       // 
        //       // return codes;
        //   }
        //   
        //   public static void InjectEnchantments(ItemSlot slot, EntityProjectile entityarrow)
        //   {
        //       entityarrow.Api.Logger.Warning("InjectEnchantments attempting to run!");
        //       ITreeAttribute bowTree = slot.Itemstack.Attributes.GetOrAddTreeAttribute("enchantments");
        //       if (bowTree != null)
        //       {
        //           entityarrow.WatchedAttributes.MergeTree(bowTree);
        //           entityarrow.Api.Logger.Warning("InjectEnchantments applied successfully!");
        //       }
        //   }

        // 
        // // [HarmonyTranspiler]
        // // [HarmonyPatch(typeof(ItemBow), "OnHeldInteractStop")]
        // static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions /*, ILGenerator generator*/)
        // {
        // 
        //     var codeMatcher = new CodeMatcher(instructions /*, ILGenerator generator*/);
        // 
        //     codeMatcher.MatchStartForward(
        //         CodeMatch.Calls(() => default(EntityProjectile.SetRotation(default)))
        //         .InsertAndAdvance(CodeInstruction.Call(() => InjectEnchantments(default, default)));
        //         //.SetOperandAndAdvance("Hello from the wiki!" + Environment.NewLine + "Game Version: ")
        // 
        //     // var code = new List<CodeInstruction>(instructions);
        //     // 
        //     // // Find where `entityarrow.DamageTier = this.Attributes["damageTier"].AsInt(0);` is set
        //     // for (int i = 0; i < code.Count; i++)
        //     // {
        //     //     if (code[i].opcode == OpCodes.Callvirt &&
        //     //         code[i - 1].opcode == OpCodes.Ldstr &&
        //     //         code[i - 1].operand.ToString() == "damageTier")
        //     //     {
        //     //         // Insert the call to our method right after "damageTier" is assigned
        //     //         code.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_2)); // Load "slot"
        //     //         code.Insert(i + 2, new CodeInstruction(OpCodes.Ldarg_1)); // Load "entityarrow"
        //     // 
        //     //         // Call InjectEnchantments(ItemSlot, EntityProjectile)
        //     //         code.Insert(i + 3, new CodeInstruction(OpCodes.Call,
        //     //             AccessTools.Method(typeof(ItemBow_Patch2), nameof(InjectEnchantments))));
        //     // 
        //     //         break;
        //     //     }
        //     // 
        //     //     // var insert = new List<CodeInstruction>();
        //     //     // insert.Add(new CodeInstruction(OpCodes.Ldarg_2)); // Load "slot"
        //     //     // insert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.DeclaredMethod(typeof(ItemBow), "")));
        //     // }
        //     // 
        //     // return code;
        // }

        // Our injected method that gets called with the correct arguments
        // public static void InjectEnchantments(ItemSlot slot, EntityProjectile entityarrow)
        // {
        //     entityarrow.Api.Logger.Warning("InjectEnchantments attempting to run!");
        //     ITreeAttribute bowTree = slot.Itemstack.Attributes.GetOrAddTreeAttribute("enchantments");
        //     if (bowTree != null)
        //     {
        //         entityarrow.WatchedAttributes.MergeTree(bowTree);
        //         entityarrow.Api.Logger.Warning("InjectEnchantments applied successfully!");
        //     }
        // }

        // static MethodBase TargetMethod()
        // {
        //     var method = AccessTools.DeclaredMethod(typeof(ItemBow), "OnHeldInteractStop");
        //     // if (method == null)
        //     // {
        //     //     Console.WriteLine("KRPG Enchantment: Could not find ItemBow.OnHeldInteractStop()");
        //     // }
        //     // else
        //     // {
        //     //     Console.WriteLine($"Patching method: {method.DeclaringType}.{method.Name}");
        //     // }
        //     return method;
        // }
        // [HarmonyPatch]
        // public static bool Prefix(ItemBow __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        // {
        //     if (byEntity.Attributes.GetInt("aimingCancel") == 1)
        //     {
        //         return false;
        //     }
        // 
        //     string aimAnimation = Traverse.Create(typeof(ItemBow)).Field("aimAnimation").GetValue() as string;
        //     byEntity.Attributes.SetInt("aiming", 0);
        //     byEntity.AnimManager.StopAnimation(aimAnimation);
        //     if (byEntity.World.Side == EnumAppSide.Client)
        //     {
        //         slot.Itemstack.TempAttributes.RemoveAttribute("renderVariant");
        //         byEntity.AnimManager.StartAnimation("bowhit");
        //         return false;
        //     }
        // 
        //     slot.Itemstack.Attributes.SetInt("renderVariant", 0);
        //     (byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
        //     if (secondsUsed < 0.65f)
        //     {
        //         return false;
        //     }
        // 
        //     var gna = MethodInfo.GetMethodFromHandle(AccessTools.Method(typeof(ItemBow), "GetNextArrow").MethodHandle);
        //     // var gna = AccessTools.Method(typeof(ItemBow), "GetNextArrow");
        //     // ItemSlot nextArrow = Traverse.Create(typeof(ItemSlot)).Method("GetNextArrow").GetValue<ItemSlot>(byEntity);
        //     // AccessTools.Method(typeof(ItemBow_Patch2), nameof(GetNextArrow), byEntity);
        //     ItemSlot nextArrow = ItemBow_Patch2.GetNextArrow(byEntity);
        //     if (nextArrow != null)
        //     {
        //         float num = 0f;
        //         if (slot.Itemstack.Collectible.Attributes != null)
        //         {
        //             num += slot.Itemstack.Collectible.Attributes["damage"].AsFloat();
        //         }
        // 
        //         if (nextArrow.Itemstack.Collectible.Attributes != null)
        //         {
        //             num += nextArrow.Itemstack.Collectible.Attributes["damage"].AsFloat();
        //         }
        // 
        //         ItemStack itemStack = nextArrow.TakeOut(1);
        //         nextArrow.MarkDirty();
        //         byEntity.World.PlaySoundAt(new AssetLocation("sounds/bow-release"), byEntity, null, randomizePitch: false, 8f);
        //         float num2 = 0.5f;
        //         if (itemStack.ItemAttributes != null)
        //         {
        //             num2 = itemStack.ItemAttributes["breakChanceOnImpact"].AsFloat(0.5f);
        //         }
        // 
        //         EntityProperties entityType = byEntity.World.GetEntityType(new AssetLocation(itemStack.ItemAttributes["arrowEntityCode"].AsString("arrow-" + itemStack.Collectible.Variant["material"])));
        //         EntityProjectile entityProjectile = byEntity.World.ClassRegistry.CreateEntity(entityType) as EntityProjectile;
        //         entityProjectile.FiredBy = byEntity;
        //         entityProjectile.Damage = num;
        //         entityProjectile.DamageTier = __instance.Attributes["damageTier"].AsInt();
        //         entityProjectile.ProjectileStack = itemStack;
        //         entityProjectile.DropOnImpactChance = 1f - num2;
        //         float num3 = Math.Max(0.001f, 1f - byEntity.Attributes.GetFloat("aimingAccuracy"));
        //         double num4 = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1.0) * (double)num3 * 0.75;
        //         double num5 = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1.0) * (double)num3 * 0.75;
        //         Vec3d vec3d = byEntity.ServerPos.XYZ.Add(0.0, byEntity.LocalEyePos.Y, 0.0);
        //         Vec3d pos = (vec3d.AheadCopy(1.0, (double)byEntity.SidedPos.Pitch + num4, (double)byEntity.SidedPos.Yaw + num5) - vec3d) * byEntity.Stats.GetBlended("bowDrawingStrength");
        //         entityProjectile.ServerPos.SetPosWithDimension(byEntity.SidedPos.BehindCopy(0.21).XYZ.Add(0.0, byEntity.LocalEyePos.Y, 0.0));
        //         entityProjectile.ServerPos.Motion.Set(pos);
        //         entityProjectile.Pos.SetFrom(entityProjectile.ServerPos);
        //         entityProjectile.World = byEntity.World;
        //         entityProjectile.SetRotation();
        //         byEntity.World.SpawnEntity(entityProjectile);
        //         ITreeAttribute bowTree = slot.Itemstack.Attributes.GetOrAddTreeAttribute("enchantments");
        //         if (bowTree != null)
        //         {
        //             entityProjectile.WatchedAttributes.GetOrAddTreeAttribute("enchantments");
        //             entityProjectile.WatchedAttributes.MergeTree(bowTree);
        //             entityProjectile.World.Logger.Warning("InjectEnchantments applied successfully!");
        //         }
        //         slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, slot);
        //         slot.MarkDirty();
        //         byEntity.AnimManager.StartAnimation("bowhit");
        //     }
        //     return false;
        // }
        // 
        // public static ItemSlot GetNextArrow(EntityAgent byEntity)
        // {
        //     ItemSlot slot = null;
        //     byEntity.WalkInventory(delegate (ItemSlot invslot)
        //     {
        //         if (invslot is ItemSlotCreative)
        //         {
        //             return true;
        //         }
        // 
        //         ItemStack itemstack = invslot.Itemstack;
        //         if (itemstack != null && itemstack.Collectible != null && itemstack.Collectible.Code.PathStartsWith("arrow-") && itemstack.StackSize > 0)
        //         {
        //             slot = invslot;
        //             return false;
        //         }
        // 
        //         return true;
        //     });
        //     return slot;
        // }
    }
}