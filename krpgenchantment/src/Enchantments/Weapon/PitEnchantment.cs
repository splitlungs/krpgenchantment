using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace KRPGLib.Enchantment
{
    public class PitEnchantment : Enchantment
    {
        public PitEnchantment(ICoreAPI api) : base(api)
        {
            
        }
        public override void OnAttack(EnchantmentSource enchant, ItemSlot slot, ref float? damage)
        {
            int mulXZ = (int)MathF.Floor(Modifiers[0]);
            int mulY = (int)MathF.Floor(Modifiers[1]);

            BlockPos bpos = enchant.TargetEntity.SidedPos.AsBlockPos;
            List<Vec3d> pitArea = new List<Vec3d>();
            for (int x = 0; x <= mulXZ; x++)
            {
                for (int y = 0; y <= mulY; y++)
                {
                    for (int z = 0; z <= mulXZ; z++)
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
                Block block = enchant.CauseEntity.World.BlockAccessor.GetBlock(ipos);
                
                if (block != null && Api.World.Claims.TestAccess((IPlayer)enchant.CauseEntity, ipos, EnumBlockAccessFlags.BuildOrBreak) == EnumWorldAccessResponse.Granted)
                {
                    string blockCode = block.Code.FirstCodePart().ToString();
                    if (blockCode.Contains("soil") || blockCode.Contains("sand") || blockCode.Contains("gravel") || blockCode.Contains("forestfloor"))
                        Api.World.BlockAccessor.BreakBlock(ipos, enchant.CauseEntity as IPlayer);
                }
            }

        }
    }
}
