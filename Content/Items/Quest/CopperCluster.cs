

namespace AerovelenceMod.Content.Items.Quest
{
    public class CopperCluster : RareOreCluster
    {
        public override int RewardTier => 1;
        protected override int SpriteWidth => 24;
        protected override int SpriteHeight => 24;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Copper Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Cúmulo de cobre")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
