using ReLogic.Content;

namespace AerovelenceMod.Common
{
    public static class CommonTextures
    {
        #region OrbsFolder
        public static readonly string OrbLoc = "AerovelenceMod/Assets/Orbs/";

        public static readonly Asset<Texture2D> circle_05 = (Main.dedServ ? null : ModContent.Request<Texture2D>(OrbLoc + "circle_05", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> feather_circle128PMA = (Main.dedServ ? null : ModContent.Request<Texture2D>(OrbLoc + "feather_circle128PMA", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> flare_12 = (Main.dedServ ? null : ModContent.Request<Texture2D>(OrbLoc + "flare_12", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> GlowCircleFlare = (Main.dedServ ? null : ModContent.Request<Texture2D>(OrbLoc + "GlowCircleFlare", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> SoftGlow = (Main.dedServ ? null : ModContent.Request<Texture2D>(OrbLoc + "SoftGlow", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> SoftGlow64 = (Main.dedServ ? null : ModContent.Request<Texture2D>(OrbLoc + "SoftGlow64", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> SolidBloom = (Main.dedServ ? null : ModContent.Request<Texture2D>(OrbLoc + "SolidBloom", AssetRequestMode.ImmediateLoad));

        #endregion

        #region PixelFolder
        public static readonly string PixelLoc = "AerovelenceMod/Assets/Pixel/";

        public static readonly Asset<Texture2D> AnotherLineGlow = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "AnotherLineGlow", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> CrispStarPMA = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "CrispStarPMA", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> DiamondGlowPMA = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "DiamondGlowPMA", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> Extra_89 = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "Extra_89", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> Extra_91 = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "Extra_91", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> FireBallBlur = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "FireBallBlur", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> Flare = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "Flare", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> FlareLineHalf = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "FlareLineHalf", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> GlowingFlare = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "GlowingFlare", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> GlowingStar = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "GlowingStar", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> Medusa_Gray = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "Medusa_Gray", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> Nightglow = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "Nightglow", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> PartiGlowPMA = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "PartiGlowPMA", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> PixelSwirl = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "PixelSwirl", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> Projectile_540 = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "Projectile_540", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> RainbowRod = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "RainbowRod", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> Starlight = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "Starlight", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> Twinkle = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "Twinkle", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> SoulSpike = (Main.dedServ ? null : ModContent.Request<Texture2D>(PixelLoc + "SoulSpike", AssetRequestMode.ImmediateLoad));
        #endregion

        #region TrailFolder
        public static readonly string TrailLoc = "AerovelenceMod/Assets/Trails/";

        public static readonly Asset<Texture2D> EnergyTex = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "EnergyTex", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> Extra_196_Black = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "Extra_196_Black", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> FireTrailGamma = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "FireTrailGamma", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> FlamesTextureButBlack = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "FlamesTextureButBlack", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> FlameTrail = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "FlameTrail", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> FlashLightBeamBlack = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "FlashLightBeamBlack", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> GlowTrail = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "GlowTrail", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> Laser1 = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "Laser1", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> LavaTrailV1 = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "LavaTrailV1", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> LintyTrail = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "LintyTrail", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> s06sBloom = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "s06sBloom", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> spark_06 = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "spark_06", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> spark_07_Black = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "spark_07_Black", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> TextureLaser = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "TextureLaser", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> ThinGlowLine = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "ThinGlowLine", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> ThinnerGlowTrail = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "ThinnerGlowTrail", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> Trail5Loop = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "Trail5Loop", AssetRequestMode.ImmediateLoad));

        public static readonly Asset<Texture2D> Trail7 = (Main.dedServ ? null : ModContent.Request<Texture2D>(TrailLoc + "Trail7", AssetRequestMode.ImmediateLoad));
        #endregion
    }

}
