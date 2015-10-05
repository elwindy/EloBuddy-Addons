using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace BlitzGrab
{
    class Program
    {
        private static Spell.Skillshot Q;

        private static Spell.Active E, R;

        private static AIHeroClient Player = ObjectManager.Player;

        public static HitChance QHitChance = HitChance.High;

        private static Menu _menu, menuK, menuD,  menuMain, menuM;
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.ChampionName != "Blitzcrank")
            {
                return;
            }
            Q = new Spell.Skillshot(SpellSlot.Q, (int)950f, SkillShotType.Linear, (int)0.25f, (int)1800f, (int)70f);
            E = new Spell.Active(SpellSlot.E, (int)150f);
            R = new Spell.Active(SpellSlot.R, (int)550f);

            _menu = MainMenu.AddMenu("BlitzGrab", "blitz");
            menuK = _menu.AddSubMenu(":: Keybinds", "kmenu");
            menuK.AddGroupLabel(":: Keybinds");
            menuK.Add("grabKey", new KeyBind("Grab Key (active)", false, KeyBind.BindTypes.HoldActive, 'T'));
            menuK.Add("combokey", new KeyBind("Combo (active)", false, KeyBind.BindTypes.HoldActive, 32));

            menuD = _menu.AddSubMenu(":: Drawings", "dmenu");
            menuD.AddGroupLabel(":: Drawings");
            menuD.Add("drawQ", new CheckBox("Draw Q"));
            menuD.Add("drawR", new CheckBox("Draw R"));

            menuMain = _menu.AddSubMenu(":: Main Settings", "mmenu");
            menuMain.AddGroupLabel(":: Main Settings");
            menuMain.AddLabel("Q Menu");
            menuMain.Add("usecomboq", new CheckBox("Use in Combo"));
            menuMain.Add("qdashing", new CheckBox("Q on Dashing Enemies"));
            menuMain.Add("qimmobil", new CheckBox("Q on Immobile Enemies"));
            menuMain.Add("interruptq", new CheckBox("Use for Interrupt"));
            menuMain.Add("secureq", new CheckBox("Use for Killsteal", false));
            menuMain.AddSeparator();
            menuMain.AddLabel("E Menu");
            menuMain.Add("usecomboe", new CheckBox("Use in Combo"));
            menuMain.Add("interrupte", new CheckBox("Use for Interrupt"));
            menuMain.Add("securee", new CheckBox("Use for Killsteal", false));
            menuMain.AddSeparator();
            menuMain.AddLabel("R Menu");
            menuMain.Add("usecombor", new CheckBox("Use in Combo"));
            menuMain.Add("interruptr", new CheckBox("Use for Interrupt"));
            menuMain.Add("securer", new CheckBox("Use for Killsteal", false));

            menuM = _menu.AddSubMenu(":: Other/Misc", "bmisc");
            menuM.AddGroupLabel(":: Other/Misc");
            var qhitchance = menuM.Add("hitchanceq", new Slider("Q Hitchance 1-Low, 3-High", 3, 1, 3));
            qhitchance.OnValueChange += delegate
                {
                    switch (qhitchance.CurrentValue)
                    {
                        case 1:
                            QHitChance = HitChance.Low;
                            break;
                        case 2:
                            QHitChance = HitChance.Medium;
                            break;
                        case 3:
                            QHitChance = HitChance.High;
                            break;
                    }
                };
            menuM.Add("mindist", new Slider("Mininum Distance to Q", (int)Q.Range - 100, 0, (int)Q.Range));
            menuM.Add("maxdist", new Slider("Maximum Distance to Q", (int)950f, 0, (int)950f));
            menuM.Add("hnd", new Slider("Dont grab if below health %"));
            var predictedPositions = new Dictionary<int, Tuple<int, PredictionResult>>();
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnTick += Game_OnTick;
        }

        private static void Game_OnTick(EventArgs args)
        {
            Secure(
                menuMain["secureq"].Cast<CheckBox>().CurrentValue,
                menuMain["securee"].Cast<CheckBox>().CurrentValue,
                menuMain["securer"].Cast<CheckBox>().CurrentValue);

            if (Player.HealthPercent >= menuM["hnd"].Cast<Slider>().CurrentValue)
            {
                AutoCast(
                    menuMain["qdashing"].Cast<CheckBox>().CurrentValue,
                    menuMain["qimmobil"].Cast<CheckBox>().CurrentValue);

                if (menuK["combokey"].Cast<KeyBind>().CurrentValue)
                {
                    bool useQ = menuMain["usecomboq"].Cast<CheckBox>().CurrentValue;
                    bool useE = menuMain["usecomboe"].Cast<CheckBox>().CurrentValue;
                    bool useR = menuMain["usecombor"].Cast<CheckBox>().CurrentValue;
                    Combo(useQ, useE, useR);
                }

                if (menuK["grabkey"].Cast<KeyBind>().CurrentValue)
                {
                    Combo(true, false, false);
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var target = TargetSelector.GetTarget(Q.Range * 2, DamageType.Physical);

            if (!Player.IsDead)
            {
                var rcircle = menuD["drawR"].Cast<CheckBox>().CurrentValue;
                var qcircle = menuD["drawQ"].Cast<CheckBox>().CurrentValue;

                if (qcircle)
                {
                    Circle.Draw(new ColorBGRA(Color.IndianRed.ToBgra()), Q.Range, Player.Position);
                }

                if (rcircle)
                {
                    Circle.Draw(new ColorBGRA(Color.IndianRed.ToBgra()), R.Range, Player.Position);
                }

                Circle.Draw(new ColorBGRA(Color.Yellow.ToBgra()), 50, target.Position);
            }
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (!sender.IsEnemy || !sender.IsValid() || sender.Type != Player.Type) return;

            if (menuMain["interruptq"].Cast<CheckBox>().CurrentValue && Q.IsReady()
                && Prediction.Position.PredictLinearMissile(
                    sender,
                    Q.Range,
                    Q.Width,
                    Q.CastDelay,
                    Q.Speed,
                    int.MaxValue).HitChance >= HitChance.Low)
            {
                if (sender.Distance(Player.ServerPosition, true) <= Q.RangeSquared)
                {
                    Q.Cast(sender);
                }
            }

            if (menuMain["interruptr"].Cast<CheckBox>().CurrentValue && E.IsReady())
            {
                if (sender.Distance(Player.ServerPosition, true) <= R.RangeSquared)
                {
                    R.Cast();
                }
            }

            if (menuMain["interrupte"].Cast<CheckBox>().CurrentValue && E.IsReady())
            {
                if (sender.Distance(Player.ServerPosition, true) <= E.RangeSquared)
                {
                    E.Cast();
                }
            }
        }

        private static void AutoCast(bool dashing, bool immobile)
        {
            if (Q.IsReady())
            {
                foreach (var ii in
                    ObjectManager.Get<AIHeroClient>()
                        .Where(x => x.IsValidTarget(menuM["maxdist"].Cast<Slider>().CurrentValue) && !x.IsAlly))
                {
                    if (dashing)
                    {
                        if (ii.Distance(Player.ServerPosition) > menuM["mindist"].Cast<Slider>().CurrentValue
                            && Prediction.Position.PredictLinearMissile(
                                ii,
                                Q.Range,
                                Q.Width,
                                Q.CastDelay,
                                Q.Speed,
                                int.MaxValue).HitChance == HitChance.Dashing)
                        {
                            Q.Cast(ii);
                        }
                    }

                    if (immobile)
                    {
                        if (ii.Distance(Player.ServerPosition) > menuM["mindist"].Cast<Slider>().CurrentValue + 100
                            && Prediction.Position.PredictLinearMissile(
                                ii,
                                Q.Range,
                                Q.Width,
                                Q.CastDelay,
                                Q.Speed,
                                int.MaxValue).HitChance == HitChance.Immobile)
                        {
                            Q.Cast(ii);
                        }
                    }
                }
            }
        }

        private static void Combo(bool useq, bool usee, bool user)
        {
            if (useq && Q.IsReady())
            {
                var qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (qtarget.IsValidTarget(menuM["maxdist"].Cast<Slider>().CurrentValue))
                {
                    if (qtarget.Distance(Player.ServerPosition) > menuM["mindist"].Cast<Slider>().CurrentValue)
                    {
                        if (Prediction.Position.PredictLinearMissile(
                                qtarget,
                                Q.Range,
                                Q.Width,
                                Q.CastDelay,
                                Q.Speed,
                                int.MaxValue).HitChance >= QHitChance)
                        {
                            Q.Cast(qtarget);
                        }
                    }
                }
            }

            if (usee && E.IsReady())
            {
                var etarget = TargetSelector.GetTarget(350, DamageType.Physical);
                if (etarget.IsValidTarget())
                {
                    E.Cast();
                }

                var qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (qtarget.IsValidTarget(menuM["maxdist"].Cast<Slider>().CurrentValue))
                {
                    if (qtarget.HasBuff("rocketgrab2"))
                    {
                        E.Cast();
                    }
                }
            }

            if (user && R.IsReady())
            {
                var rtarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);
                if (rtarget.IsValidTarget())
                {
                    if (!E.IsReady() && rtarget.HasBuffOfType(BuffType.Knockup))
                    {
                        if (rtarget.Health > rtarget.GetSpellDamage(rtarget, SpellSlot.R))
                        {
                            R.Cast();
                        }
                    }
                }

            }
        }

        private static void Secure(bool useq, bool usee, bool user)
        {
            if (user && R.IsReady())
            {
                var rtarget = ObjectManager.Get<AIHeroClient>().FirstOrDefault(h => h.IsEnemy);
                if (rtarget.IsValidTarget(R.Range) && rtarget != null)
                {
                    if (Player.GetSpellDamage(rtarget, SpellSlot.R) >= rtarget.Health)
                        R.Cast();
                }
            }

            if (usee && E.IsReady())
            {
                var etarget = ObjectManager.Get<AIHeroClient>().FirstOrDefault(h => h.IsEnemy);
                if (etarget.IsValidTarget(E.Range) && etarget != null)
                {
                    if (Player.GetSpellDamage(etarget, SpellSlot.E) >= etarget.Health)
                        E.Cast(Player);
                }
            }

            if (useq && Q.IsReady())
            {
                var qtarget = ObjectManager.Get<AIHeroClient>().FirstOrDefault(h => h.IsEnemy);
                if (qtarget.IsValidTarget(menuM["maxdist"].Cast<Slider>().CurrentValue) && qtarget != null)
                {
                    if (Player.GetSpellDamage(qtarget, SpellSlot.Q) >= qtarget.Health && Prediction.Position.PredictLinearMissile(
                                qtarget,
                                Q.Range,
                                Q.Width,
                                Q.CastDelay,
                                Q.Speed,
                                int.MaxValue).HitChance >= HitChance.High)
                    {
                        Q.Cast(qtarget);
                    }
                }
            }
        }
    }
}
