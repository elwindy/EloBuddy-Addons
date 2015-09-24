using EloBuddy;
using EloBuddy.SDK;

// Using the config like this makes your life easier, trust me
using Settings = KurisuDarius_.Config.Modes.Combo;

namespace KurisuDarius_.Modes
{

    public sealed class Combo : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            // Only execute this mode when the orbwalker is on combo mode
            return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo);
        }

        public override void Execute()
        {
            // TODO: Add combo logic here
            // See how I used the Settings.UseQ here, this is why I love my way of using
            // the menu in the Config class!
            if (Settings.UseQ && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                if (target != null)
                {
                    Q.Cast();
                }
                
            }
            if (Settings.UseE && E.IsReady())
            {
                var etarget = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                if (etarget.IsValidTarget())
                {
                    if (etarget.Distance(ObjectManager.Player.ServerPosition) > 270)
                    {
                        if (Q.IsReady() || W.IsReady())
                        {
                            E.Cast(etarget.ServerPosition);
                        }
                    }
                }
            }

            if (Settings.UseW && W.IsReady())
            {
                var wtarget = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                if (wtarget.IsValidTarget())
                {
                    W.Cast();
                }
            }

            if (Settings.UseR && R.IsReady())
            {
                var unit = TargetSelector.GetTarget(E.Range, DamageType.Physical);

                if (unit.IsValidTarget(R.Range) && !unit.IsZombie)
                {
                    int rr = unit.GetBuffCount("dariushemo") <= 0 ? 0 : unit.GetBuffCount("dariushemo");
                    if (!unit.HasBuffOfType(BuffType.Invulnerability) && !unit.HasBuffOfType(BuffType.SpellShield))
                    {
                        if (SpellManager.RDmg(unit, rr)  >= unit.Health + SpellManager.Hemorrhage(unit, 1))
                        {
                            if (!unit.HasBuffOfType(BuffType.Invulnerability)
                                && !unit.HasBuffOfType(BuffType.SpellShield))
                            {
                                R.Cast(unit);
                            }
                        }
                    }
                }
            }

        }

        
    }
}
