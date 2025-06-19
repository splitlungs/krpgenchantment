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
using Vintagestory.GameContent;

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
            ValidToolTypes = new List<string>() {
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
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a Pit enchantment.", enchant.TargetEntity.GetName());

            BlockPos bpos = enchant.TargetEntity.SidedPos.AsBlockPos;
            List<Vec3d> pitArea = new List<Vec3d>();

            float mulxz = MulXZ * enchant.Power;
            float muly = MulY * enchant.Power;
            for (int x = 0; x <= mulxz; x++)
            {
                for (int y = 0; y <= muly; y++)
                {
                    for (int z = 0; z <= mulxz; z++)
                    {
                        pitArea.Add(new Vec3d(bpos.X + x, bpos.Y - y, bpos.Z + z));
                        pitArea.Add(new Vec3d(bpos.X - x, bpos.Y - y, bpos.Z - z));
                        pitArea.Add(new Vec3d(bpos.X + x, bpos.Y - y, bpos.Z - z));
                        pitArea.Add(new Vec3d(bpos.X - x, bpos.Y - y, bpos.Z + z));
                    }
                }
            }
            Entity entity = enchant.GetCauseEntity();
            IPlayer player = null;
            if (entity is EntityPlayer ep)
            {
                player = Api?.World.PlayerByUid(ep.PlayerUID);
            }

            for (int i = 0; i < pitArea.Count; i++)
            {
                // Check for claims
                BlockPos ipos = bpos;
                ipos.Set(pitArea[i]);

                LandClaim[] claims = Api.World.Claims.Get(ipos);
                bool denied = false;
                if (player != null)
                {
                    foreach (LandClaim lc in claims)
                    {
                        var response = lc.TestPlayerAccess(player, EnumBlockAccessFlags.BuildOrBreak);
                        if (response < EnumPlayerAccessResult.OkOwner)
                            denied = true;
                    }
                    if (denied != false) continue;
                }
                // Break the block only if claim check passed
                if (denied == false)
                {
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
}
