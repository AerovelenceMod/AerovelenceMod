using AerovelenceMod.Common.Systems.Language;

namespace AerovelenceMod.Content.Items.Quest
{
    public class IronCluster : RareOreCluster
    {
        public override int RewardTier => 2;
        protected override int SpriteWidth => 22;
        protected override int SpriteHeight => 22;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Iron Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Cúmulo de hierro")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
