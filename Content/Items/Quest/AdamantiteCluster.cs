

namespace AerovelenceMod.Content.Items.Quest
{
    public class AdamantiteCluster : RareOreCluster
    {
        public override int RewardTier => 8;
        protected override int SpriteWidth => 28;
        protected override int SpriteHeight => 24;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Adamantite Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Cúmulo de adamantita")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
