#region

using System;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using RyzeBuddy.Activator;
using Item = RyzeBuddy.Activator.Item;

#endregion

namespace RyzeBuddy
{
    using System.Collections.Specialized;
    using System.Linq;

    internal class Program
    {
        public const string ChampionName = "Ryze";

        public static AIHeroClient _Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }



        public static Spell.Skillshot Q;

        public static Spell.Targeted W;

        public static Spell.Targeted E;

        public static Spell.Active R;

        public static Menu menu, ComboMenu, HarassMenu;

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (_Player.ChampionName != ChampionName)
            {
                return;
            }

            Q = new Spell.Skillshot(SpellSlot.Q, 900, SkillShotType.Linear, (int)0.25f, 1700, (int)0.5);
            W = new Spell.Targeted(SpellSlot.W, 600);
            E = new Spell.Targeted(SpellSlot.E, 600);
            R = new Spell.Active(SpellSlot.R);
            Bootstrap.Init(null);
            ItemManager.Init();
            menu = MainMenu.AddMenu("Ryze Buddy", "ryzeBuddy");
            menu.AddGroupLabel("Ryze Buddy");
            menu.AddLabel("made by the Heluder");
            menu.AddSeparator();
            ComboMenu = menu.AddSubMenu("Combo", "ComboRyze");
            ComboMenu.Add("useQCombo", new CheckBox("Use Q"));
            ComboMenu.Add("useWCombo", new CheckBox("Use W"));
            ComboMenu.Add("useECombo", new CheckBox("Use E"));
            ComboMenu.Add("useR1Combo", new CheckBox("Use R"));
            ComboMenu.AddSeparator();
            ComboMenu.Add("useR1ww", new CheckBox("Only R if Target Is Rooted."));
            ComboMenu.AddSeparator();
            HarassMenu = menu.AddSubMenu("Harass", "HarassRyze");
            HarassMenu.Add("useQHarass", new CheckBox("Use Q"));
            HarassMenu.Add("useWHarass", new CheckBox("Use W", false));
            HarassMenu.Add("useEHarass", new CheckBox("Use E", false));
            HarassMenu.AddSeparator();
            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                args.Process =
                    !(Q.IsReady() || W.IsReady() || E.IsReady()
                      || _Player.Distance(target) >= 600);
        }

        private static void Game_OnTick(EventArgs args)
        {
            Orbwalker.ForcedTarget = null;
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) Combo();
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)) Harass();
        }



        private static void Harass()
        {
            var target = TargetSelector.GetTarget(W.Range, DamageType.Magical);
            var qTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);


            if (target != null)
            {
                if (HarassMenu["useWHarass"].Cast<CheckBox>().CurrentValue)
                {
                    W.Cast(target);
                }
                if (HarassMenu["useEHarass"].Cast<CheckBox>().CurrentValue)
                {
                    E.Cast(target);
                }
            }

            if (qTarget != null)
            {
                if (HarassMenu["useQHarass"].Cast<CheckBox>().CurrentValue)
                {
                    Q.Cast(qTarget);
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            //SOON :)
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(W.Range, DamageType.Magical);
            var qSpell = ComboMenu["useQCombo"].Cast<CheckBox>().CurrentValue;
            var wSpell = ComboMenu["useWCombo"].Cast<CheckBox>().CurrentValue;
            var eSpell = ComboMenu["useECombo"].Cast<CheckBox>().CurrentValue;
            var rSpell = ComboMenu["useR1Combo"].Cast<CheckBox>().CurrentValue;
            var rwwSpell = ComboMenu["useR1ww"].Cast<CheckBox>().CurrentValue;
            if (target.IsValidTarget(Q.Range))
            {
                if (GetPassiveBuff <= 2 || !_Player.HasBuff("RyzePassiveStack"))
                {
                    if (target.IsValidTarget(Q.Range) && qSpell && Q.IsReady()) Q.Cast(target);

                    if (target.IsValidTarget(W.Range) && wSpell && W.IsReady()) W.Cast(target);

                    if (target.IsValidTarget(E.Range) && eSpell && E.IsReady()) E.Cast(target);

                    if (R.IsReady() && rSpell)
                    {
                        if (target.IsValidTarget(W.Range))
                        {
                            if (rwwSpell && target.HasBuff("RyzeW")) R.Cast();
                            if (!rwwSpell) R.Cast();
                        }
                    }
                }



                if (GetPassiveBuff == 3)
                {
                    if (Q.IsReady() && target.IsValidTarget(Q.Range)) Q.Cast(target);

                    if (E.IsReady() && target.IsValidTarget(E.Range)) E.Cast(target);

                    if (W.IsReady() && target.IsValidTarget(W.Range)) W.Cast(target);

                    if (R.IsReady() && rSpell)
                    {
                        if (target.IsValidTarget(W.Range))
                        {
                            if (rwwSpell && target.HasBuff("RyzeW")) R.Cast();
                            if (!rwwSpell) R.Cast();
                        }
                    }
                }

                if (GetPassiveBuff == 4)
                {
                    if (target.IsValidTarget(W.Range) && wSpell && W.IsReady()) W.Cast(target);

                    if (target.IsValidTarget(Q.Range) && Q.IsReady() && qSpell) Q.Cast(target);

                    if (target.IsValidTarget(E.Range) && E.IsReady() && eSpell) E.Cast(target);

                    if (R.IsReady() && rSpell)
                    {
                        if (target.IsValidTarget(W.Range))
                        {
                            if (rwwSpell && target.HasBuff("RyzeW")) R.Cast();
                            if (!rwwSpell) R.Cast();
                        }
                    }
                }

                if (_Player.HasBuff("ryzepassivecharged"))
                {
                    if (wSpell && W.IsReady() && target.IsValidTarget(W.Range)) W.Cast(target);

                    if (qSpell && Q.IsReady() && target.IsValidTarget(Q.Range)) Q.Cast(target);

                    if (eSpell && E.IsReady() && target.IsValidTarget(E.Range)) E.Cast(target);

                    if (R.IsReady() && rSpell)
                    {
                        if (target.IsValidTarget(W.Range))
                        {
                            if (rwwSpell && target.HasBuff("RyzeW")) R.Cast();
                            if (!rwwSpell) R.Cast();
                            if (!E.IsReady() && !Q.IsReady() && !W.IsReady()) R.Cast();
                        }
                    }
                }


            }
            else
            {
                if (wSpell
                            && W.IsReady()
                            && target.IsValidTarget(W.Range))
                    W.Cast(target);

                if (qSpell
                    && Q.IsReady()
                    && target.IsValidTarget(Q.Range))
                    Q.Cast(target);

                if (eSpell
                    && E.IsReady()
                    && target.IsValidTarget(E.Range))
                    E.Cast(target);
            }
            if (!R.IsReady() || GetPassiveBuff != 4 || !rSpell) return;

            if (Q.IsReady() || W.IsReady() || E.IsReady()) return;

            R.Cast();

        }

        public static int GetPassiveBuff
        {
            get
            {
                var data = _Player.Buffs.FirstOrDefault(b => b.DisplayName == "RyzePassiveStack");
                return data != null ? data.Count : 0;
            }
        }
    }
}
