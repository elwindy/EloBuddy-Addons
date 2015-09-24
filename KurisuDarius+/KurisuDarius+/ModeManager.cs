using System;
using System.Collections.Generic;
using KurisuDarius_.Modes;
using EloBuddy;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Utils;

namespace KurisuDarius_
{
    using System.Linq;

    using EloBuddy.SDK;

    public static class ModeManager
    {
        private static List<ModeBase> Modes { get; set; }

        static ModeManager()
        {
            
            Modes = new List<ModeBase>();

            
            Modes.AddRange(new ModeBase[]
            {
                new PermaActive(),
                new Combo(),
                new Harass(),
                new LaneClear(),
                new JungleClear(),
                new LastHit(),
                new Flee()
            });

            
            Game.OnTick += OnTick;
        }

        public static void Initialize()
        {
            
        }

        private static void OnTick(EventArgs args)
        {
            // Execute all modes
            Modes.ForEach(mode =>
            {
                try
                {
                    
                    if (mode.ShouldBeExecuted())
                    {
                        
                        mode.Execute();
                    }
                }
                catch (Exception e)
                {
                    
                    Logger.Log(LogLevel.Error, "Error executing mode '{0}'\n{1}", mode.GetType().Name, e);
                }
            });

            if (SpellManager.R.IsReady())
            {
                foreach (var unit in HeroManager.Enemies.Where(ene => ene.IsValidTarget(SpellManager.R.Range) && !ene.IsZombie))
                {
                    int rr = unit.GetBuffCount("dariushemo") <= 0 ? 0 : unit.GetBuffCount("dariushemo");
                    if (unit.CountEnemiesInRange(1200) <= 1)
                    {
                        if (ObjectManager.Player.Distance(unit.ServerPosition) > 265)
                        {
                            if (SpellManager.RDmg(unit, rr) + 0 + SpellManager.Hemorrhage(unit, rr) >= unit.Health)
                            {
                                if (!unit.HasBuffOfType(BuffType.Invulnerability) &&
                                    !unit.HasBuffOfType(BuffType.SpellShield))
                                {
                                    SpellManager.R.Cast(unit);
                                }
                            }
                        }
                    }

                    if (SpellManager.RDmg(unit, rr) + 0 >= unit.Health + SpellManager.Hemorrhage(unit, 1))
                    {
                        if (!unit.HasBuffOfType(BuffType.Invulnerability) &&
                            !unit.HasBuffOfType(BuffType.SpellShield))
                        {
                            SpellManager.R.Cast(unit);
                        }
                    }
                }
            }
        }
    }
}
