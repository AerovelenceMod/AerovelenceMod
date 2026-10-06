

namespace AerovelenceMod.Content.Items.Quest
{
    public class SilverCluster : RareOreCluster
    {
        public override int RewardTier => 3;
        protected override int SpriteWidth => 24;
        protected override int SpriteHeight => 18;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Silver Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Cúmulo de plata")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
