using EloBuddy;
using EloBuddy.SDK;

namespace KurisuDarius_
{

    using EloBuddy.SDK.Enumerations;

    public static class SpellManager
    {
        public static Spell.Active Q { get; private set; }
        public static Spell.Active W { get; private set; }
        public static Spell.Skillshot E { get; private set; }
        public static Spell.Targeted R { get; private set; }

        static SpellManager()
        {
            Q = new Spell.Active(SpellSlot.Q, 425);
            W = new Spell.Active(SpellSlot.W,200);
            E = new Spell.Skillshot(SpellSlot.E,550,SkillShotType.Cone);
            R = new Spell.Targeted(SpellSlot.R,460);
        }

        public static void Initialize()
        {
            
        }

        public static float QDmg(Obj_AI_Base unit)
        {
            return
                (float)
                    ObjectManager.Player.CalculateDamageOnUnit(unit, DamageType.Physical, 
                        new[] { 20, 20, 35, 50, 65, 80 }[Q.Level] + (float)
                       (new[] { 1.0, 1.0, 1.1, 1.2, 1.3, 1.4 }[Q.Level] * ObjectManager.Player.FlatPhysicalDamageMod));
        }

        public static float WDmg(Obj_AI_Base unit)
        {
            return
                (float)
                    ObjectManager.Player.CalculateDamageOnUnit(unit, DamageType.Physical, 
                       ObjectManager.Player.TotalAttackDamage + (float)(0.4 * ObjectManager.Player.TotalAttackDamage));
        }

        public static float RDmg(Obj_AI_Base unit, int stackcount)
        {
            var bonus =
                stackcount *
                    (new[] { 20, 20, 40, 60 }[R.Level] + (0.20 * ObjectManager.Player.FlatPhysicalDamageMod));

            return
                (float)(bonus + (ObjectManager.Player.CalculateDamageOnUnit(unit, DamageType.True, 
                        new[] { 100, 100, 200, 300 }[R.Level] + (float)(0.75 * ObjectManager.Player.FlatPhysicalDamageMod))));
        }

        public static float Hemorrhage(Obj_AI_Base unit, int stackcount)
        {
            if (stackcount <= 0)
                stackcount = 1;

            return
                (float)
                    ObjectManager.Player.CalculateDamageOnUnit(unit, DamageType.Physical, 
                        (9 + ObjectManager.Player.Level) + (float)(0.3 * ObjectManager.Player.FlatPhysicalDamageMod)) * stackcount;
        }
    }
}
