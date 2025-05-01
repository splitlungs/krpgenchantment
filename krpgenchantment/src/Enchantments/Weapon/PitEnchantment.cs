using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using KRPGLib.Enchantment.API;

namespace KRPGLib.Enchantment
{
    public class PitEnchantment : Enchantment
    {
        int MulXZ { get { return (int)MathF.Floor((float)Modifiers.GetValueOrDefault("MulXZ", 0.5)); } }
        int MulY { get { return (int)MathF.Floor((float)Modifiers.GetValueOrDefault("MulY", 1)); } }

        public PitEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "pit";
            Category = "Weapon";
            LoreCode = "enchantment-pit";
            LoreChapterID = 9;
            MaxTier = 5;
            Modifiers = new Dictionary<string, object>()
            {
                { "MulXZ", 0.5 }, {"MulY", 1 }
            };
        }
        public override void OnAttack(EnchantmentSource enchant, ItemSlot slot, ref float? damage)
        {
            BlockPos bpos = enchant.TargetEntity.SidedPos.AsBlockPos;
            List<Vec3d> pitArea = new List<Vec3d>();
            for (int x = 0; x <= MulXZ; x++)
            {
                for (int y = 0; y <= MulY; y++)
                {
                    for (int z = 0; z <= MulXZ; z++)
                    {
                        pitArea.Add(new Vec3d(bpos.X + x, bpos.Y - y, bpos.Z + z));
                        pitArea.Add(new Vec3d(bpos.X - x, bpos.Y - y, bpos.Z - z));
                        pitArea.Add(new Vec3d(bpos.X + x, bpos.Y - y, bpos.Z - z));
                        pitArea.Add(new Vec3d(bpos.X - x, bpos.Y - y, bpos.Z + z));
                    }
                }
            }

            for (int i = 0; i < pitArea.Count; i++)
            {
                BlockPos ipos = bpos;
                ipos.Set(pitArea[i]);
                Block block = Api.World.BlockAccessor.GetBlock(ipos);
                if (block.BlockMaterial == EnumBlockMaterial.Gravel || block.BlockMaterial == EnumBlockMaterial.Soil
                    || block.BlockMaterial == EnumBlockMaterial.Sand || block.BlockMaterial == EnumBlockMaterial.Plant)
                {
                    // if (Api.World.Claims.TestAccess(player, ipos, EnumBlockAccessFlags.BuildOrBreak) != EnumWorldAccessResponse.Granted) continue;
                    Api.World.BlockAccessor.BreakBlock(ipos, enchant.CauseEntity as IPlayer);
                }
            }

        }
    }
}
