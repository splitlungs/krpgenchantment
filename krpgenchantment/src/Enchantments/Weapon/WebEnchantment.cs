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
    public class WebEnchantment : Enchantment
    {
        float PowerMulXZ { get { return Modifiers.GetFloat("MulXZ"); } }
        float PowerMulY { get { return Modifiers.GetFloat("MulY"); } }
        int PowerMulMs { get { return Modifiers.GetInt("MulMs"); } }
        protected ICoreServerAPI sApi;
        protected int webBlockId;
        /// <summary>
        /// Covers the target in Web blocks.
        /// </summary>
        /// <param name="api"></param>
        public WebEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "web";
            Category = "ControlArea";
            LoreCode = "enchantment-web";
            LoreChapterID = 27;
            MaxTier = 5;
            ValidToolTypes = new List<string>() {
                "Spear",
                "Bow", "Sling",
                "Javelin",
                "Crossbow", "Firearm",
                "Wand"
            };
            Modifiers = new EnchantModifiers()
            {
                { "MulXZ", 0.50 }, {"MulY", 0.50 }, { "MulMs", 1000 }
            };
            Version = 1.00f;
            if (!(Api is ICoreServerAPI sapi)) return;
            sApi = sapi;
        }
        public void CreateWebBlock(BlockPos pos, int ttl)
        {
            int? posId = Api.World.BlockAccessor.GetBlock(pos).BlockId;
            if (!posId.Equals(0)) return;
            Api.World.BlockAccessor.SetBlock(webBlockId, pos);
            sApi.World.RegisterCallbackUnique(RemoveWebBlockCallback, pos, ttl);
        }

        public void RemoveWebBlockCallback(IWorldAccessor world, BlockPos pos, float dt)
        {
            if (world.Side != EnumAppSide.Server) return;
            int? posId = Api.World.BlockAccessor.GetBlock(pos).BlockId;
            if (!posId.Equals(webBlockId)) return;
            // Dust to air to dust
            world.BlockAccessor.SetBlock(0, pos);
        }
        public override void OnAttacked(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a {1} enchantment.", enchant.TargetEntity.GetName(), Code);

            Block[] blocks = Api.World.SearchBlocks(new AssetLocation("game:spiderweb"));
            if (blocks?[0] != null) webBlockId = blocks[0].BlockId;
            int ttl = PowerMulMs * enchant.Power;

            BlockPos bpos = enchant.TargetEntity.Pos.AsBlockPos;
            List<Vec3d> pitArea = new List<Vec3d>();

            float mulxz = PowerMulXZ * enchant.Power;
            float muly = PowerMulY * enchant.Power;
            for (int x = 0; x <= mulxz; x++)
            {
                for (int y = 0; y <= muly; y++)
                {
                    for (int z = 0; z <= mulxz; z++)
                    {
                        pitArea.Add(new Vec3d(bpos.X + x, bpos.Y + y, bpos.Z + z));
                        pitArea.Add(new Vec3d(bpos.X - x, bpos.Y + y, bpos.Z - z));
                        pitArea.Add(new Vec3d(bpos.X + x, bpos.Y + y, bpos.Z - z));
                        pitArea.Add(new Vec3d(bpos.X - x, bpos.Y + y, bpos.Z + z));
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
                if (player != null && claims != null)
                {
                    foreach (LandClaim lc in claims)
                    {
                        var response = lc.TestPlayerAccess(player, EnumBlockAccessFlags.BuildOrBreak);
                        if (response < EnumPlayerAccessResult.OkOwner)
                            denied = true;
                    }
                    if (denied != false) continue;
                }
                // Create the web block if passed claim check
                if (denied == false)
                    CreateWebBlock(ipos, ttl);
            }
        }
        public override void OnDamaged(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a {1} enchantment.", enchant.TargetEntity.GetName(), Code);

            Block[] blocks = Api.World.SearchBlocks(new AssetLocation("game:spiderweb"));
            if (blocks?[0] != null) webBlockId = blocks[0].BlockId;
            int ttl = PowerMulMs * enchant.Power;

            BlockPos bpos = enchant.TargetEntity.Pos.AsBlockPos;
            List<Vec3d> pitArea = new List<Vec3d>();

            float mulxz = PowerMulXZ * enchant.Power;
            float muly = PowerMulY * enchant.Power;
            for (int x = 0; x <= mulxz; x++)
            {
                for (int y = 0; y <= muly; y++)
                {
                    for (int z = 0; z <= mulxz; z++)
                    {
                        pitArea.Add(new Vec3d(bpos.X + x, bpos.Y + y, bpos.Z + z));
                        pitArea.Add(new Vec3d(bpos.X - x, bpos.Y + y, bpos.Z - z));
                        pitArea.Add(new Vec3d(bpos.X + x, bpos.Y + y, bpos.Z - z));
                        pitArea.Add(new Vec3d(bpos.X - x, bpos.Y + y, bpos.Z + z));
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
                if (player != null && claims != null)
                {
                    foreach (LandClaim lc in claims)
                    {
                        var response = lc.TestPlayerAccess(player, EnumBlockAccessFlags.BuildOrBreak);
                        if (response < EnumPlayerAccessResult.OkOwner)
                            denied = true;
                    }
                    if (denied != false) continue;
                }
                // Create the web block if passed claim check
                if (denied == false)
                    CreateWebBlock(ipos, ttl);
            }
            
        }
    }
}
