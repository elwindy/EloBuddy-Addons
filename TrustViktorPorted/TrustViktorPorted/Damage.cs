using EloBuddy;
using EloBuddy.SDK;

namespace TrustViktorPorted
{
    internal class Damage
    {
        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }

        public static float QDamage(Obj_AI_Base target)
        {
            return _Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (float)(new float[] { 40, 60, 80, 100, 120 }[Program.Q.Level - 1] + 0.2 * _Player.FlatMagicDamageMod));
        }

        public static float EDamage(Obj_AI_Base target)
        {
            return _Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (float)(new float[] { 70, 115, 160, 205, 250 }[Program.E.Level - 1] + 0.7 * _Player.FlatMagicDamageMod));
        }

        public static float EDamage1(Obj_AI_Base target)
        {
            return _Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (float)(new float[] { 98, 161, 224, 287, 350 }[Program.E.Level - 1] + 0.98 * _Player.FlatMagicDamageMod));
        }
        // summon dmg
        public static float RDamage(Obj_AI_Base target)
        {
            return _Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (float)(new float[] { 150, 250, 350 }[Program.R.Level - 1] + 0.55 * _Player.FlatMagicDamageMod));
        }
        // per bolt
        public static float RDamage1(Obj_AI_Base target)
        {
            return _Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (float)(new float[] { 15, 30, 45 }[Program.R.Level - 1] + 0.1 * _Player.FlatMagicDamageMod));
        }
    }
}
