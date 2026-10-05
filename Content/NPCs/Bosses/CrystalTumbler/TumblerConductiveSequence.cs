using Microsoft.Xna.Framework;

namespace AerovelenceMod.Content.NPCs.Bosses.CrystalTumbler
{
    internal static class TumblerConductiveSequence
    {
        internal const int ChargeEnd = 90;
        internal const int Rest = 180;
        internal const int AimTime = 70;
        internal const int ShotCount = 3;
        internal static int WarningTime(int shot) => ChargeEnd + Rest + shot * 40;
        internal static int Side(int shot) => shot == 1 ? 1 : -1;

        internal static bool Supplying(float timer, int side)
        {
            if (timer >= 45f && timer < ChargeEnd)
                return true;
            for (int shot = 0; shot < ShotCount; shot++)
                if (Side(shot) == side && timer >= WarningTime(shot) && timer < WarningTime(shot) + AimTime)
                    return true;
            return false;
        }

        internal static float Charge(float timer, int side)
        {
            if (timer < ChargeEnd)
                return MathHelper.Clamp((timer - 45f) / 45f, 0f, 1f);
            for (int shot = ShotCount - 1; shot >= 0; shot--)
                if (Side(shot) == side && timer >= WarningTime(shot) && timer < WarningTime(shot) + AimTime)
                    return MathHelper.Lerp(0.35f, 1f, (timer - WarningTime(shot)) / AimTime);
            return timer < WarningTime(ShotCount - 1) + AimTime ? 0.35f : 0f;
        }
    }
}
