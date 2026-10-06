

namespace AerovelenceMod.Content.Items.Quest
{
    public class OrichalcumCluster : RareOreCluster
    {
        public override int RewardTier => 7;
        protected override int SpriteWidth => 28;
        protected override int SpriteHeight => 20;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Orichalcum Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Cúmulo de oricalco")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
