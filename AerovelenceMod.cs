global using AerovelenceMod.Common.Systems.Language;
global using AerovelenceMod.Common.Utilities;
global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
global using Terraria;
global using Terraria.ID;
global using Terraria.ModLoader;
using System.Collections.Generic;
using AerovelenceMod.Core;
using AerovelenceMod.Backgrounds.Skies;
using AerovelenceMod.Common.Globals.Players;
using AerovelenceMod.Common.IL;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.Localization;
using Terraria.UI;
using ReLogic.Content;
using AerovelenceMod.Common.Globals.SkillStrikes;
using ReLogic.Graphics;
using AerovelenceMod.Common;
using Terraria.GameContent;
using AerovelenceMod.Content.Projectiles;
using AerovelenceMod.Content.Items.Weapons.Misc.Melee;
using AerovelenceMod.Content.Items.Weapons.Starglass;
using AerovelenceMod.Content.Biomes;
using System;
using AerovelenceMod.Common.Interfaces;
using System.Linq;

namespace AerovelenceMod
{
    public class AerovelenceMod : Mod
    {
        public override void HandlePacket(System.IO.BinaryReader reader, int whoAmI)
        {
            byte packet = reader.ReadByte();
            if (packet == Common.Systems.Traversal.RopeSpanSystem.Packet)
                Common.Systems.Traversal.RopeSpanSystem.Receive(reader, whoAmI);
            else if (packet == Content.Items.Misc.BabyCondurtleEgg.HatchPacket)
                Content.Items.Misc.BabyCondurtleEgg.ReceiveHatch(whoAmI);
            else if (packet == Content.NPCs.TownNPC.RockCollector.RockCollectorTrade.RequestPacket)
                Content.NPCs.TownNPC.RockCollector.RockCollectorTrade.TurnIn(whoAmI, reader.ReadInt16(), reader.ReadByte(), reader.ReadInt32());
            else if (packet == Content.NPCs.TownNPC.RockCollector.RockCollectorTrade.ResultPacket && Main.netMode == NetmodeID.MultiplayerClient)
                Content.NPCs.TownNPC.RockCollector.RockCollectorTrade.ShowReward(reader.ReadInt32(), reader.ReadInt32());
            else if (packet == Content.NPCs.TownNPC.BabyCondurtleTownPet.BabyCondurtle.FeedRequestPacket)
                Content.NPCs.TownNPC.BabyCondurtleTownPet.BabyCondurtle.Feed(whoAmI, reader.ReadInt16(), reader.ReadByte());
            else if (packet == Content.NPCs.TownNPC.BabyCondurtleTownPet.BabyCondurtle.FeedResultPacket && Main.netMode == NetmodeID.MultiplayerClient)
                Content.NPCs.TownNPC.BabyCondurtleTownPet.BabyCondurtle.ShowFeed(reader.ReadInt16(), reader.ReadByte(), reader.ReadByte());
        }
        public const string ProjectileAssets = "AerovelenceMod/Assets/Projectiles/";
        public const string CrystalCavernsAssets = "AerovelenceMod/Assets/CrystalCaverns/";

        public const string Abbreviation = "AM";
        public const string AbbreviationPrefix = Abbreviation + ":";

        public const string AssetPath = $"{nameof(AerovelenceMod)}/Assets/";

        internal static AerovelenceMod Instance { get; set; }

        public AerovelenceMod()
        {
            Instance = this;
        }

        public static Effect LegElectricity;
        public static Effect RailgunShader;

        public static Effect DistortShader;
        public static Effect CrystalShine;

        public static Effect Shockwave;

        public static Effect Test2;
        public static Effect BasicTrailShader;
        public static Effect TrailShaderPixelate;
        public static Effect TrailShaderGradient;

        public static Effect SmokeColShader;

        public static Effect fadeShader;

        private List<IOrderedLoadable> loadCache;
        public override void Load()
        {
            if (!Main.dedServ)
            {
                string shaderName = "AerovelenceMod:CavernCrystalShine";
                string shaderPath = "Effects/CavernCrystalShine";

                var shaderRef = new Ref<Effect>(Instance.Assets.Request<Effect>(shaderPath).Value);
                (Filters.Scene[shaderName] = new Filter(new ScreenShaderData(shaderRef, shaderName + "Pass"), EffectPriority.High)).Load();

            }
            // Literally ripped from SLR
            #region IOrderedLoadable Loading
            loadCache = new List<IOrderedLoadable>();

            foreach (Type type in Code.GetTypes())
            {
                if (!type.IsAbstract && type.GetInterfaces().Contains(typeof(IOrderedLoadable)))
                {
                    object instance = Activator.CreateInstance(type);
                    loadCache.Add(instance as IOrderedLoadable);
                }
            }

            for (int k = 0; k < loadCache.Count; k++)
            {
                loadCache[k].Load();
            }
            #endregion

            //StarglassParticleDetour.Load();
            ModDetours.Load();

            ModContent.GetInstance<CrystalCavernsSurfaceBiome>();

            if (!Main.dedServ)
            {

                string shaderName = "AerovelenceMod:DistortScreen";
                //string shaderPath = "Effects/DistortScreen";

                var shaderRef = new Ref<Effect>(Assets.Request<Effect>("Effects/GlowMisc", AssetRequestMode.ImmediateLoad).Value);
                Filters.Scene[shaderName] = new Filter(new ScreenShaderData(shaderRef, "DistortPass"), EffectPriority.Low);
                Filters.Scene[shaderName].Load();
                //(Filters.Scene[shaderName] = new Filter(new ScreenShaderData(shaderRef, "DistortPass"), EffectPriority.Low)).Load(); //EF.High?


                //Filters.Scene[shaderName] = new Filter(new ScreenShaderData())

                SmokeColShader = Instance.Assets.Request<Effect>("Effects/Particle/SmokeColShader", AssetRequestMode.ImmediateLoad).Value;

                DistortShader = ModContent.Request<Effect>("AerovelenceMod/Effects/DistortScreen", (AssetRequestMode)1).Value;
                CrystalShine = ModContent.Request<Effect>("AerovelenceMod/Effects/CrystalShine", (AssetRequestMode)1).Value;
                Test2 = ModContent.Request<Effect>("AerovelenceMod/Effects/Test2", (AssetRequestMode)1).Value;

                //Shockwave = ModContent.Request<Effect>("AerovelenceMod/Effects/Shockwave", (AssetRequestMode)1).Value;


                Filters.Scene["DistortScreen"] = new Filter(new ScreenShaderData(new Ref<Effect>(ModContent.Request<Effect>("AerovelenceMod/Effects/DistortScreen", AssetRequestMode.ImmediateLoad).Value), "DistortPass"), EffectPriority.VeryHigh);
                Filters.Scene["DistortScreen"].Load();


                Filters.Scene["Shockwave"] = new Filter(new ScreenShaderData(new Ref<Effect>(ModContent.Request<Effect>("AerovelenceMod/Effects/Shockwave", AssetRequestMode.ImmediateLoad).Value), "Shockwave"), EffectPriority.VeryHigh);
                Filters.Scene["Shockwave"].Load();

            }



            if (!Main.dedServ)
            {
                Filters.Scene["AerovelenceMod:FoggyFields"] =
                    new Filter(new ScreenShaderData("FilterMiniTower").UseColor(0.168f, 0.168f, 0.188f).UseOpacity(0.1f), EffectPriority.High);

                //yManager.Instance["AerovelenceMod:FoggyFields"] = new CrystalTorrentSky();

                Filters.Scene["AerovelenceMod:CrystalTorrents"] =
                    new Filter(new CrystalTorrentScreenShaderData("FilterBloodMoon").UseColor(0.0f, 0.5f, 0.0f), EffectPriority.Medium);

                Filters.Scene["AerovelenceMod:DarkNights"] =
                    new Filter(new DarkNightScreenShaderData("FilterBloodMoon").UseColor(0.0f, 0.2f, 0.2f), EffectPriority.Medium);

                SkyManager.Instance["AerovelenceMod:Cyvercry"] = new CyverSky();
                SkyManager.Instance["AerovelenceMod:CrystalCavernsSurface"] = new CrystalCavernsSky();
                SkyManager.Instance["AerovelenceMod:CrystalCaverns"] = new CrystalCavernsSky();

                Overlays.Scene.Load();
                Filters.Scene.Load();
            }

            if (!Main.dedServ)
            {

                Ref<Effect> MiscGlow = new Ref<Effect>(Assets.Request<Effect>("Effects/GlowMisc", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["GlowMisc"] = new MiscShaderData(MiscGlow, "Glow");

                LegElectricity = Instance.Assets.Request<Effect>("Effects/LegElectricity", AssetRequestMode.ImmediateLoad).Value;
                RailgunShader = Instance.Assets.Request<Effect>("Effects/RailgunShader").Value;


                Ref<Effect> LaserShaderRef = new Ref<Effect>(Assets.Request<Effect>("Effects/LaserShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["LaserShader"] = new MiscShaderData(LaserShaderRef, "Aura");

                Ref<Effect> ShittyBallRef = new Ref<Effect>(Assets.Request<Effect>("Effects/FireBallShader", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["FireBallShader"] = new MiscShaderData(ShittyBallRef, "Aura");

                Ref<Effect> CyverAuraRef = new Ref<Effect>(Assets.Request<Effect>("Effects/CyverAura", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["CyverAura"] = new MiscShaderData(CyverAuraRef, "Aura");

                Ref<Effect> DistortMiscRef = new Ref<Effect>(Assets.Request<Effect>("Effects/DistortMisc", AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc["DistortMisc"] = new MiscShaderData(DistortMiscRef, "DistortPass");

                BasicTrailShader = Instance.Assets.Request<Effect>("Effects/TrailShaders/BasicTrailShader", AssetRequestMode.ImmediateLoad).Value;
                TrailShaderGradient = Instance.Assets.Request<Effect>("Effects/TrailShaders/TrailShaderGradient", AssetRequestMode.ImmediateLoad).Value;

                fadeShader = Instance.Assets.Request<Effect>("Effects/FadeShader", AssetRequestMode.ImmediateLoad).Value;


                //Ref<Effect> DarkBeamRef = new Ref<Effect>(Assets.Request<Effect>("Effects/DarkBeam", AssetRequestMode.ImmediateLoad).Value);
                //GameShaders.Misc["DarkBeam"] = new MiscShaderData(DarkBeamRef, "Aura");//.UseImage0("Images/Misc/Perlin");

                //Ref<Effect> RimeLaserRef = new Ref<Effect>(Assets.Request<Effect>("Effects/RimeLaser", AssetRequestMode.ImmediateLoad).Value);
                //GameShaders.Misc["RimeLaser"] = new MiscShaderData(RimeLaserRef,  "Aura");//.UseImage0("Images/Misc/Perlin");

                //putting this here just in case
                //Filters.Scene.Load();

                //TrailShader = Assets.Request<Effect>("Effects/Trail");

                Terraria.Graphics.Effects.On_FilterManager.EndCapture += FilterManager_EndCapture;
                CreateRender();

            }

            if (!Main.dedServ)
            {
                //DiscordRichPresence.Initialize();
                //Main.OnTickForThirdPartySoftwareOnly += DiscordRichPresence.Update;
            }

            LoadDetours();


        }

        public override void Unload()
        {
            Terraria.Graphics.Effects.On_FilterManager.EndCapture -= FilterManager_EndCapture;
            //StarglassParticleDetour.Unload();

            ModDetours.Unload();

            if (!Main.dedServ)
            {
                //DiscordRichPresence.Deinitialize();
                //Main.OnTickForThirdPartySoftwareOnly -= DiscordRichPresence.Update;

                SmokeColShader = null;

                BasicTrailShader = null;
                TrailShaderPixelate = null;
                TrailShaderGradient = null;
            }


            UnloadDetours();
            Instance = null;
            LegElectricity = null;
            RailgunShader = null;

        }

        public override void Close()
        {
            base.Close();
        }

        private void LoadDetours()
        {
            AeroPlayer aeroPlayer = new AeroPlayer();
            //On.Terraria.Player.ItemCheck += aeroPlayer.DetouredItemCheck;
            // IL.Terraria.Main.DoDraw += DrawMoonlordLayer;
        }

        private void UnloadDetours()
        {
            AeroPlayer aeroPlayer = new AeroPlayer();
            //On.Terraria.Player.ItemCheck -= aeroPlayer.DetouredItemCheck;
            // IL.Terraria.Main.DoDraw -= DrawMoonlordLayer;
        }

        //Ripped from Regressus which was ripped from a chinese example mod i think
        public RenderTarget2D render3;
        private void FilterManager_EndCapture(Terraria.Graphics.Effects.On_FilterManager.orig_EndCapture orig, Terraria.Graphics.Effects.FilterManager self,
            RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;

            #region ozoneShredder
            gd.SetRenderTarget(render3);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            sb.End();
            gd.SetRenderTarget(Main.screenTargetSwap);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            foreach (Projectile projectile in Main.projectile)
            {
                //Want to do this first and separate because it will weed out more projectiles first, despite checking again later
                if (projectile.type == ModContent.ProjectileType<DistortProj>())
                {
                    if (projectile.active && projectile.type == ModContent.ProjectileType<DistortProj>())
                    {
                        Texture2D tex = null;
                        float overallScale = 1;

                        if (projectile.ModProjectile is DistortProj distort)
                        {
                            tex = distort.tex;
                            overallScale = distort.scale;
                        }

                        Vector2 toProj = (projectile.Center - Main.player[Main.myPlayer].Center);
                        Main.spriteBatch.Draw(tex, projectile.Center - Main.screenPosition + (toProj * (1 - Main.GameZoomTarget) * -1), null, Color.White, projectile.rotation, tex.Size() / 2, overallScale * projectile.scale * 0.5f * Main.GameZoomTarget, SpriteEffects.None, 0f);
                    }
                }
            }
            sb.End();
            gd.SetRenderTarget(Main.screenTarget);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Test2.CurrentTechnique.Passes[0].Apply();
            Test2.Parameters["tex0"].SetValue(Main.screenTargetSwap);
            Test2.Parameters["i"].SetValue(0.02f);
            sb.Draw(render3, Vector2.Zero, Color.White);
            sb.End();
            #endregion


            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);

        }

        public void CreateRender()
        {
            Main.QueueMainThreadAction(() =>
            {
                render3 = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
            });
        }
        private void Main_OnResolutionChanged(Vector2 obj)
        {
            CreateRender();
        }
    }
}
