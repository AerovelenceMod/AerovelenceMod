using AerovelenceMod.Common.Systems.Language;

namespace AerovelenceMod.Content.Items.Quest
{
    public class PalladiumCluster : RareOreCluster
    {
        public override int RewardTier => 6;
        protected override int SpriteWidth => 28;
        protected override int SpriteHeight => 26;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Palladium Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Cúmulo de paladio")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
