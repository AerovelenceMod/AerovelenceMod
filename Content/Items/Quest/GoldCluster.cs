

namespace AerovelenceMod.Content.Items.Quest
{
    public class GoldCluster : RareOreCluster
    {
        public override int RewardTier => 4;
        protected override int SpriteWidth => 26;
        protected override int SpriteHeight => 22;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Gold Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Cúmulo de oro")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
