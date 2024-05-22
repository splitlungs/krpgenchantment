using ProtoBuf;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class RecipeUpload
    {
        public List<string> eValues;
    }

    public class RecipeUploadSystem : ModSystem
    {
        #region Client
        IClientNetworkChannel clientChannel;
        ICoreClientAPI cApi;

        public override void StartClientSide(ICoreClientAPI api)
        {
            cApi = api;

            clientChannel =
                api.Network.RegisterChannel("enchantingrecipes")
                .RegisterMessageType(typeof(RecipeUpload))
                .SetMessageHandler<RecipeUpload>(OnServerMessage)
            ;
        }
        private void OnServerMessage(RecipeUpload networkMessage)
        {
            if (!cApi.PlayerReadyFired) return;
            cApi.Logger.VerboseDebug("Received Enchanting recipes from server");
            List<EnchantingRecipe> eRecipes = new List<EnchantingRecipe>();

            if (networkMessage.eValues != null)
            {
                foreach (string eRec in networkMessage.eValues)
                {
                    using (MemoryStream ms = new MemoryStream(Ascii85.Decode(eRec)))
                    {
                        BinaryReader reader = new BinaryReader(ms);

                        EnchantingRecipe retr = new EnchantingRecipe();
                        retr.FromBytes(reader, cApi.World);

                        eRecipes.Add(retr);
                    }
                }
            }

            EnchantingRecipeRegistry.Registry.EnchantingRecipes = eRecipes;

            System.Diagnostics.Debug.WriteLine(EnchantingRecipeRegistry.Registry.EnchantingRecipes.Count + " enchanting recipes loaded to client.");
        }
        #endregion
        #region Server
        IServerNetworkChannel serverChannel;
        ICoreServerAPI sApi;
        RecipeUpload cachedMessage;

        public override void StartServerSide(ICoreServerAPI api)
        {
            sApi = api;

            serverChannel =
                api.Network.RegisterChannel("enchantingrecipes")
                .RegisterMessageType(typeof(RecipeUpload))
            ;

            api.Event.PlayerNowPlaying += (player) => { SendRecepies(player); };
        }

        private void SendRecepies(IServerPlayer player, bool allowCache = true)
        {
            SendRecepies(new IServerPlayer[] { player }, allowCache);
        }

        private void SendRecepies(IServerPlayer[] players, bool allowCache = true)
        {
            if (!allowCache || cachedMessage == null)
                cachedMessage = GetRecipeUploadMessage();

            serverChannel.SendPacket(cachedMessage, players);
        }

        private RecipeUpload GetRecipeUploadMessage()
        {
            List<string> eRecipes = new List<string>();

            foreach (EnchantingRecipe eRec in EnchantingRecipeRegistry.Registry.EnchantingRecipes)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);

                    eRec.ToBytes(writer);

                    string value = Ascii85.Encode(ms.ToArray());
                    eRecipes.Add(value);
                }
            }

            return new RecipeUpload()
            {
                eValues = eRecipes
            };
        }
        #endregion
    }
}