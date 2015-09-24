using System;
using EloBuddy;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace KurisuDarius_
{
    using System.Linq;

    using EloBuddy.SDK;

    public static class Program
    {
       
        public const string ChampName = "Darius";

        public static void Main(string[] args)
        {
            
            Loading.OnLoadingComplete += OnLoadingComplete;
        }

        private static void OnLoadingComplete(EventArgs args)
        {
           
            if (Player.Instance.ChampionName != ChampName)
            {
                
                return;
            }

            
            Config.Initialize();
            SpellManager.Initialize();
            ModeManager.Initialize();

            
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            var hero = target as AIHeroClient;
            if (hero != null && hero.Type == GameObjectType.AIHeroClient)
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    if (!hero.HasBuffOfType(BuffType.Slow))
                    {
                        SpellManager.W.Cast();
                    }

                    if (Item.CanUseItem(3077) ||
                        Item.CanUseItem(3074) ||
                        Item.CanUseItem(3748))
                    {
                        Item.UseItem(3074);
                        Item.UseItem(3077);
                        Item.CanUseItem(3748);
                    }
                }
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "DariusCleave")
            {
                Core.DelayAction(Orbwalker.ResetAutoAttack, Game.Ping + 800);
            }

            if (sender.IsMe && args.SData.Name == "DariusAxeGrabCone")
            {
                Core.DelayAction(Orbwalker.ResetAutoAttack, Game.Ping + 100);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            
            Circle.Draw(Color.Red, SpellManager.Q.Range, Player.Instance.Position);
            Circle.Draw(Color.Red, SpellManager.E.Range, Player.Instance.Position);
            Circle.Draw(Color.DarkRed, SpellManager.R.Range, Player.Instance.Position);
            foreach (var enemy in HeroManager.Enemies.Where(ene => ene.IsValidTarget() && !ene.IsZombie))
            {
                var enez = Drawing.WorldToScreen(enemy.Position);
                if (enemy.GetBuffCount("dariushemo") > 0)
                {
                    Drawing.DrawText(
                        enez[0] - 50,
                        enez[1],
                        System.Drawing.Color.OrangeRed,
                        "Stack Count: " + enemy.GetBuffCount("dariushemo"));
                }
            }
        }
    }
}
