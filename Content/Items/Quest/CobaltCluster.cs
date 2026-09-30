using AerovelenceMod.Common.Systems.Language;

namespace AerovelenceMod.Content.Items.Quest
{
    public class CobaltCluster : RareOreCluster
    {
        public override int RewardTier => 6;
        protected override int SpriteWidth => 30;
        protected override int SpriteHeight => 26;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Cobalt Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Cúmulo de cobalto")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
