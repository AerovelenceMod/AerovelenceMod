

namespace AerovelenceMod.Content.Items.Quest
{
    public class TitaniumCluster : RareOreCluster
    {
        public override int RewardTier => 8;
        protected override int SpriteWidth => 24;
        protected override int SpriteHeight => 28;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Titanium Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Cúmulo de titanio")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
