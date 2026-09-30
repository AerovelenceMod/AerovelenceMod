using AerovelenceMod.Common.Systems.Language;

namespace AerovelenceMod.Content.Items.Quest
{
    public class TitaniumSuperCluster : RareOreCluster
    {
        public override int RewardTier => 10;
        protected override int SpriteWidth => 38;
        protected override int SpriteHeight => 34;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Titanium Super Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Supercúmulo de titanio")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
