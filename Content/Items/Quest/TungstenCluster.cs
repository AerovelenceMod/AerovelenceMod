

namespace AerovelenceMod.Content.Items.Quest
{
    public class TungstenCluster : RareOreCluster
    {
        public override int RewardTier => 3;
        protected override int SpriteWidth => 22;
        protected override int SpriteHeight => 24;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Tungsten Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Cúmulo de tungsteno")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
