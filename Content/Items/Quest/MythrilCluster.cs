using AerovelenceMod.Common.Systems.Language;

namespace AerovelenceMod.Content.Items.Quest
{
    public class MythrilCluster : RareOreCluster
    {
        public override int RewardTier => 7;
        protected override int SpriteWidth => 26;
        protected override int SpriteHeight => 22;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Mythril Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Cúmulo de mitrilo")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
