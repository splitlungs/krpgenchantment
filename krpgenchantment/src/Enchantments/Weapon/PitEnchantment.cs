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
using Vintagestory.API.Server;

namespace KRPGLib.Enchantment
{
    public class PitEnchantment : Enchantment
    {
        float MulXZ { get { return Modifiers.GetFloat("MulXZ"); } }
        float MulY { get { return Modifiers.GetFloat("MulY"); } }
        protected IServerAPI sApi;
        public PitEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "pit";
            Category = "ControlArea";
            LoreCode = "enchantment-pit";
            LoreChapterID = 9;
            MaxTier = 5;
            ValidToolTypes = new string[19] {
                "Knife", "Axe",
                "Club", "Sword",
                "Spear",
                "Bow", "Sling",
                "Drill",
                "Halberd", "Mace", "Pike", "Polearm", "Poleaxe", "Staff", "Warhammer",
                "Javelin",
                "Crossbow", "Firearm",
                "Wand" };
            Modifiers = new EnchantModifiers()
            {
                { "MulXZ", 0.50 }, {"MulY", 1.00 }
            };
            sApi = Api as IServerAPI;
        }
        public override void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
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
            IPlayer player = enchant.CauseEntity as IPlayer;
            for (int i = 0; i < pitArea.Count; i++)
            {
                BlockPos ipos = bpos;
                ipos.Set(pitArea[i]);
                LandClaim[] claims = Api.World.Claims.Get(ipos);
                if (claims != null)
                {
                    if (Api.World.Claims.TestAccess(player, ipos, EnumBlockAccessFlags.BuildOrBreak) == EnumWorldAccessResponse.NoPrivilege)
                        continue;
                }
                Block block = Api.World.BlockAccessor.GetBlock(ipos);
                if (block.BlockMaterial == EnumBlockMaterial.Gravel || block.BlockMaterial == EnumBlockMaterial.Soil
                    || block.BlockMaterial == EnumBlockMaterial.Sand || block.BlockMaterial == EnumBlockMaterial.Plant)
                {
                    Api.World.BlockAccessor.BreakBlock(ipos, player);
                }
            }

        }
    }
}
