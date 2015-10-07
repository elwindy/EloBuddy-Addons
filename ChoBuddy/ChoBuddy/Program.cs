#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

#endregion

namespace ChoBuddy
{
    

    class Program
    {
        
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        public static Spell.Skillshot q, w;

        public static Spell.Targeted r;

        public static HitChance hcq = HitChance.High;
        public static HitChance hcw = HitChance.High;
        public static HitChance hcqharass = HitChance.Medium;
        public static HitChance hcwharass = HitChance.Medium;
        public static Menu main, combo, harass, clear, misc;
        static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != "Chogath")
            {
                return;
            }
            q = new Spell.Skillshot(SpellSlot.Q, 950, SkillShotType.Circular, (int).625f, int.MaxValue, (int)250f);
            w = new Spell.Skillshot(SpellSlot.W, 650, SkillShotType.Cone, (int).25f, int.MaxValue, (int)(30 * 0.5));
            r = new Spell.Targeted(SpellSlot.R, 175);

            main = MainMenu.AddMenu("ChoBuddy", "chobuddy");
            combo = main.AddSubMenu("Combo", "combo");
            combo.AddGroupLabel("Combo Settings");
            combo.Add("UseQCombo", new CheckBox("Use Q"));
            combo.Add("UseWCombo", new CheckBox("Use W"));
            combo.Add("UseRCombo", new CheckBox("Use R"));
            var qhitchance = combo.Add("QComboHitchance", new Slider("Q Hitchance", 2, 0, 2));
            var hitchance = new[] { "Low", "Medium", "High" };
            qhitchance.DisplayName = hitchance[qhitchance.CurrentValue];
            qhitchance.OnValueChange +=
                delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
                {
                    sender.DisplayName = hitchance[changeArgs.NewValue];
                    switch (sender.CurrentValue)
                    {
                        case 0:
                            hcq = HitChance.Low;
                            break;
                        case 1:
                            hcq = HitChance.Medium;
                            break;
                        case 2:
                            hcq = HitChance.High;
                            break;
                    }
                };
            var whitchance = combo.Add("WComboHitchance", new Slider("W Hitchance", 2, 0, 2));
            var hitchance1 = new[] { "Low", "Medium", "High" };
            whitchance.DisplayName = hitchance1[whitchance.CurrentValue];
            whitchance.OnValueChange +=
                delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
                {
                    sender.DisplayName = hitchance1[changeArgs.NewValue];
                    switch (sender.CurrentValue)
                    {
                        case 0:
                            hcw = HitChance.Low;
                            break;
                        case 1:
                            hcw = HitChance.Medium;
                            break;
                        case 2:
                            hcw = HitChance.High;
                            break;
                    }
                };
            harass = main.AddSubMenu("Harass", "harass");
            harass.AddGroupLabel("Harass Settings");
            harass.Add("UseQHarass", new CheckBox("Use Q"));
            harass.Add("UseWHarass", new CheckBox("Use W"));
            var qhitchanceharass = harass.Add("QHarassHitchance", new Slider("Q Hitchance", 2, 0, 2));
            var hitchanceharass = new[] { "Low", "Medium", "High" };
            qhitchanceharass.DisplayName = hitchanceharass[qhitchanceharass.CurrentValue];
            qhitchanceharass.OnValueChange +=
                delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
                {
                    sender.DisplayName = hitchanceharass[changeArgs.NewValue];
                    switch (sender.CurrentValue)
                    {
                        case 0:
                            hcqharass = HitChance.Low;
                            break;
                        case 1:
                            hcqharass = HitChance.Medium;
                            break;
                        case 2:
                            hcqharass = HitChance.High;
                            break;
                    }
                };
            var whitchanceharass = harass.Add("WHarassHitchance", new Slider("W Hitchance", 2, 0, 2));
            var hitchance1Harass = new[] { "Low", "Medium", "High" };
            whitchanceharass.DisplayName = hitchance1Harass[whitchance.CurrentValue];
            whitchanceharass.OnValueChange +=
                delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
                {
                    sender.DisplayName = hitchance1Harass[changeArgs.NewValue];
                    switch (sender.CurrentValue)
                    {
                        case 0:
                            hcwharass = HitChance.Low;
                            break;
                        case 1:
                            hcwharass = HitChance.Medium;
                            break;
                        case 2:
                            hcwharass = HitChance.High;
                            break;
                    }
                };
            harass.Add("ManaManagerHarass", new Slider("Maximum mana usage in percent ({0}%)", 50));
			clear = main.AddSubMenu("Clear", "clear");
			clear.AddGroupLabel("Soon :)");
            misc = main.AddSubMenu("Misc", "misc");
            misc.AddGroupLabel("Misc Settings");
            misc.Add("AutoQ", new CheckBox("Auto Q on Immobile"));
            misc.Add("Q_Gap_Closer", new CheckBox("Use Q On Gap Closer"));
            misc.Add("UseInt", new CheckBox("Use Q/E to Interrupt"));
            var skinslect = misc.Add("skin+", new Slider("Chance Skin", 0, 0, 6));
            ObjectManager.Player.SetSkin(ObjectManager.Player.ChampionName, skinslect.CurrentValue);
            skinslect.OnValueChange += delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
            {
                ObjectManager.Player.SetSkin(ObjectManager.Player.ChampionName, changeArgs.NewValue);
            };
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Game.OnTick += Game_OnTick;
            Chat.Print("Cho'Gath loaded", System.Drawing.Color.BlueViolet);
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (!misc["UseInt"].Cast<CheckBox>().CurrentValue || sender.IsAlly) return;

            if (ObjectManager.Player.Distance(sender.Position) < w.Range && w.IsReady())
            {
                w.Cast(sender);
                return;
            }

            if (ObjectManager.Player.Distance(sender.Position) < q.Range && q.IsReady())
            {
                q.Cast(sender.ServerPosition);
            }
        }

        static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (!misc["Q_Gap_Closer"].Cast<CheckBox>().CurrentValue || sender.IsAlly) return;

            if (q.IsReady() && e.Sender.Distance(ObjectManager.Player.Position) < 500)
                q.Cast(e.Sender.ServerPosition);
        }

        static void Game_OnTick(EventArgs args)
        {
            AutoQ();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
        }

        

        private static float GetRealRRange(AIHeroClient target)
        {
            return r.Range + ObjectManager.Player.BoundingRadius + target.BoundingRadius;
        }

        private static void Harass()
        {
            var useq = harass["UseQHarass"].Cast<CheckBox>().CurrentValue;
            var usew = harass["UseWHarass"].Cast<CheckBox>().CurrentValue;
            var mana = harass["ManaManagerHarass"].Cast<Slider>().CurrentValue;

            if (ObjectManager.Player.ManaPercent < mana)
            {
                return;
            }

            if (useq && q.IsReady())
            {
                var target = TargetSelector.GetTarget(q.Range, DamageType.Magical);
                if (
                    Prediction.Position.PredictCircularMissile(target, q.Range, q.Radius, q.CastDelay, q.Speed)
                        .HitChance >= hcqharass)
                {
                    q.Cast(target);
                }
            }

            if (usew && w.IsReady())
            {
                var target = TargetSelector.GetTarget(w.Range, DamageType.Magical);
                if (target.IsValidTarget(q.Range) && Prediction.Position.PredictConeSpell(target, w.Range, w.ConeAngleDegrees, w.CastDelay, w.Speed).HitChance >= hcwharass)
                {
                    w.Cast(target);
                }
            }
        }

        private static void Combo()
        {
            var useq = combo["UseQCombo"].Cast<CheckBox>().CurrentValue;
            var usew = combo["UseWCombo"].Cast<CheckBox>().CurrentValue;
            var user = combo["UseRCombo"].Cast<CheckBox>().CurrentValue;

            if (useq && q.IsReady())
            {
                var target = TargetSelector.GetTarget(q.Range, DamageType.Magical);
                if (
                    Prediction.Position.PredictCircularMissile(target, q.Range, q.Radius, q.CastDelay, q.Speed)
                        .HitChance >= hcq)
                {
                    q.Cast(target);
                }
            }

            if (usew && w.IsReady())
            {
                var target = TargetSelector.GetTarget(w.Range, DamageType.Magical);
                if (target.IsValidTarget(q.Range) && Prediction.Position.PredictConeSpell(target, q.Range, q.ConeAngleDegrees, q.CastDelay, q.Speed).HitChance >= hcw)
                {
                    w.Cast(target);
                }
            }

            if (user && r.IsReady())
            {
                foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(x => x.IsValidTarget(GetRealRRange(x))))
                {
                    
                    if (RDmg(enemy) > enemy.Health)
                    {
                        r.Cast(enemy);
                    }
                }
            }
        }

        public static float RDmg(Obj_AI_Base unit)
        {
            return
                    ObjectManager.Player.CalculateDamageOnUnit(unit, DamageType.True,
                        new[] { 300, 300 , 475 , 650 }[r.Level] + (ObjectManager.Player.FlatMagicDamageMod * 70) / 100);
        }

        private static void AutoQ()
        {
            if (!misc["AutoQ"].Cast<CheckBox>().CurrentValue)
            {
                return;
            }

            var autoQTarget =
                HeroManager.Enemies.FirstOrDefault(
                    x =>
                    x.HasBuffOfType(BuffType.Charm) || x.HasBuffOfType(BuffType.Knockup)
                    || x.HasBuffOfType(BuffType.Stun) || x.HasBuffOfType(BuffType.Suppression)
                    || x.HasBuffOfType(BuffType.Snare));

            if (autoQTarget != null)
            {
                q.Cast(autoQTarget);
            }
        }
    }
}
