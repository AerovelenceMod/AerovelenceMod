

namespace AerovelenceMod.Content.Items.Quest
{
    public class LeadCluster : RareOreCluster
    {
        public override int RewardTier => 2;
        protected override int SpriteWidth => 22;
        protected override int SpriteHeight => 22;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Lead Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Cúmulo de plomo")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
