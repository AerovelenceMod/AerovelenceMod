using AerovelenceMod.Common.Systems.Language;

namespace AerovelenceMod.Content.Items.Quest
{
    public class PlatinumCluster : RareOreCluster
    {
        public override int RewardTier => 4;
        protected override int SpriteWidth => 22;
        protected override int SpriteHeight => 20;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Platinum Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Cúmulo de platino")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
