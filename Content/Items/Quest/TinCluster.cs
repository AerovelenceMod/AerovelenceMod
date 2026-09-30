using AerovelenceMod.Common.Systems.Language;

namespace AerovelenceMod.Content.Items.Quest
{
    public class TinCluster : RareOreCluster
    {
        public override int RewardTier => 1;
        protected override int SpriteWidth => 20;
        protected override int SpriteHeight => 22;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Tin Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Cúmulo de estaño")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
