using System;
using System.Collections.Generic;
using AerovelenceMod.Content.Dusts;
using Terraria.GameContent;

namespace AerovelenceMod.Content.Items.Quest
{
    public abstract class RareOreCluster : TranslatableModItem
    {
        public abstract int RewardTier { get; }
        protected abstract int SpriteWidth { get; }
        protected abstract int SpriteHeight { get; }
        internal int RewardSilver => 20 + RewardTier * RewardTier * 5;
        internal int RewardCrystals => 2 + RewardTier;
        protected string EnglishTooltip => $"An unusually pure ore specimen\nTurn in to the Rock Collector for {RewardSilver} silver coins and {RewardCrystals} cavern crystals\nHold the specimen to choose it; favorite it to keep it";
        protected string SpanishTooltip => $"Una muestra de mineral extraordinariamente pura\nEntrégala al Coleccionista de Rocas por {RewardSilver} monedas de plata y {RewardCrystals} cristales de caverna\nSostén la muestra para elegirla; márcala como favorita para conservarla";
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 5;
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.width = SpriteWidth;
            Item.height = SpriteHeight;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = RewardTier >= 10 ? ItemRarities.PrePlantPostMech : RewardTier >= 6 ? ItemRarities.EarlyHardmode : ItemRarities.MidPHM;
            Item.value = Item.sellPrice(silver: Math.Max(1, RewardSilver / 5));
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            DrawGlow(spriteBatch, position, frame, origin, scale, 0, Color.White);
            return true;
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
            => DrawSparkles(spriteBatch, position + (frame.Size() * .5f - origin) * scale, frame.Size(), scale, 0, Color.White);

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Rectangle frame = TextureAssets.Item[Type].Value.Frame();
            DrawGlow(spriteBatch, WorldDrawPosition(frame), frame, frame.Size() * .5f, scale, rotation, Item.GetAlpha(Color.White));
            return true;
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Rectangle frame = TextureAssets.Item[Type].Value.Frame();
            DrawSparkles(spriteBatch, WorldDrawPosition(frame), frame.Size(), scale, rotation, Item.GetAlpha(Color.White));
        }

        private Vector2 WorldDrawPosition(Rectangle frame) => Item.position - Main.screenPosition + new Vector2(Item.width * .5f, Item.height - frame.Height * .5f);

        private void DrawGlow(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Vector2 origin, float scale, float rotation, Color color)
        {
            Texture2D glow = ModContent.Request<Texture2D>(Texture + "_Glowmask").Value;
            Vector2 padding = (glow.Size() - frame.Size()) * .5f;
            float pulse = .5f + .5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 1.6f + Type * .7f);
            spriteBatch.Draw(glow, position, null, color * pulse * .8f, rotation, origin + padding, scale, SpriteEffects.None, 0);
        }

        private void DrawSparkles(SpriteBatch spriteBatch, Vector2 center, Vector2 size, float scale, float rotation, Color color)
        {
            int age = ((int)(Main.GlobalTimeWrappedHourly * 60) + Type * 17) % 180;
            if (age >= GenericSparkle.Lifetime * 2) return;
            int corner = age < GenericSparkle.Lifetime ? -1 : 1;
            Vector2 offset = (size * .3f * corner * scale).RotatedBy(rotation);
            GenericSparkle.Draw(spriteBatch, center + offset, age % GenericSparkle.Lifetime, scale, rotation, color);
        }
    }
}
