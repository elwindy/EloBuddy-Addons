using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace ThreshBuddy
{
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;

    using EloBuddy.SDK.Rendering;

    using SharpDX;

    /// <summary>
    /// The program.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Gets the player.
        /// </summary>
        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        /// <summary>
        /// The q,w,e.
        /// </summary>
        private static Spell.Skillshot Q, W, E;

        /// <summary>
        /// The q2,r.
        /// </summary>
        public static Spell.Active Q2, R;

        public static Menu menu;

        public static Menu ComboMenu;

        public static Menu HarassMenu;

        public static Menu lanternMenu;

        public static Menu flayMenu;

        public static Menu hookMenu;

        public static Menu drawMenu, pull_push;

        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        
        internal static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        /// <summary>
        /// The loading_ on loading complete.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.ChampionName != "Thresh")
            {
                return;
            }
            Bootstrap.Init(null);
            Q = new Spell.Skillshot(SpellSlot.Q,1075,SkillShotType.Linear,(int)0.35f,1200,60);
            Q2 = new Spell.Active(SpellSlot.Q,1075);
            W = new Spell.Skillshot(SpellSlot.W,950,SkillShotType.Circular,(int)0.25f,1750,300);
            E = new Spell.Skillshot(SpellSlot.E, 500,SkillShotType.Linear,1,2000,110);
            R = new Spell.Active(SpellSlot.R,350);
            menu = MainMenu.AddMenu("ThreshBuddy", "threshBuddy");
            menu.AddGroupLabel("ThreshBuddy");
            menu.AddLabel("made by the Heluder");
            menu.AddSeparator();
            drawMenu = menu.AddSubMenu("Draw", "drawThresh");
            drawMenu.Add("drawq", new CheckBox("Draw Q"));
            ComboMenu = menu.AddSubMenu("Combo", "ComboThresh");
            ComboMenu.Add("UseQCombo", new CheckBox("Use Q"));
            ComboMenu.Add("UseWCombo", new CheckBox("Use W"));
            ComboMenu.Add("UseECombo", new CheckBox("Use E"));
            ComboMenu.Add("UseRCombo", new CheckBox("Use R"));
            ComboMenu.Add("UseRComboEnemies", new Slider("R Min Enemies >=", 2, 1, 5));
            ComboMenu.AddSeparator();
            HarassMenu = menu.AddSubMenu("Harass", "HarassThresh");
            HarassMenu.Add("UseQ1Harass", new CheckBox("Use Q1 (Hook)"));
            HarassMenu.Add("UseQ2Harass", new CheckBox("Use Q2 (Fly)", false));
            HarassMenu.Add("UseEHarass", new CheckBox("Use E"));
            HarassMenu.AddSeparator();
            lanternMenu = menu.AddSubMenu("Lantern", "WSettings");
            lanternMenu.Add("WLowAllies", new CheckBox("W Low Allies"));
            lanternMenu.Add("WAllyPercent", new Slider("Ally Health Percent", 30));
            lanternMenu.AddSeparator();
            flayMenu = menu.AddSubMenu("Flay", "Flay");
            flayMenu.Add("EDash", new CheckBox("E on Dash (Smart)"));
            flayMenu.Add("EInterrupt", new CheckBox("E to Interrupt"));
            flayMenu.Add("EGapcloser", new CheckBox("E on Incoming Gapcloser"));
            flayMenu.AddSeparator();
            hookMenu = menu.AddSubMenu("Hook", "Hook");
            hookMenu.Add("QDash", new CheckBox("Q on Dash (Smart)"));
            hookMenu.Add("QInterrupt", new CheckBox("Q to Interrupt"));
            hookMenu.Add("QImmobile", new CheckBox("Q on Immobile"));
            hookMenu.AddSeparator();
            pull_push = menu.AddSubMenu("Pull/Push Keybinds", "ppkeys");
            pull_push.Add("push", new KeyBind("Push Enemy", false, KeyBind.BindTypes.HoldActive, 88));
            pull_push.Add("pull", new KeyBind("Pull Enemy", false, KeyBind.BindTypes.HoldActive, 90));

            Chat.Print("<font color=\"#7CFC00\"><b>ThreshBuddy:</b></font> by Heluder loaded");

            Game.OnTick += Game_OnTick;
            Obj_AI_Base.OnNewPath += Obj_AI_Base_OnNewPath;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Gapcloser.OnGapcloser += Gapcloser_OnGapCloser;
            AIHeroClient.OnProcessSpellCast += AIHeroClient_OnProcessSpellCast;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (drawMenu["drawq"].Cast<CheckBox>().CurrentValue) Circle.Draw(new ColorBGRA(Color.GreenYellow.ToBgra()), Q.Range, Player.Position);
        }

        static void AIHeroClient_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            
            if (!sender.IsEnemy || sender.IsMinion || sender.IsAlly || sender.IsValidTarget(Q.Range)) return;
            // flash
            if(args.SData.Name == "summonerflash")
            {
                if (Prediction.Position.PredictLinearMissile(
                    sender,
                    Q.Range,
                    Q.Width,
                    Q.CastDelay,
                    Q.Speed,
                    int.MaxValue,
                    args.End).HitChance >= HitChance.High)
                {
                    Q.Cast(args.End);
                }
            }
                // ezreal e
            else if (args.SData.Name == "EzrealArcaneShift")
            {
                if (Prediction.Position.PredictLinearMissile(
                    sender,
                    Q.Range,
                    Q.Width,
                    Q.CastDelay,
                    Q.Speed,
                    int.MaxValue,
                    args.End).HitChance >= HitChance.High)
                {
                    Q.Cast(args.End);
                }
            }
                //shaco q
            else if (args.SData.Name == "Deceive")
            {
                if (Prediction.Position.PredictLinearMissile(
                    sender,
                    Q.Range,
                    Q.Width,
                    Q.CastDelay,
                    Q.Speed,
                    int.MaxValue,
                    args.End).HitChance >= HitChance.High)
                {
                    Q.Cast(args.End);
                }
            }
            
            
        }

        /// <summary>
        /// The gapcloser_ on gap closer.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
        private static void Gapcloser_OnGapCloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (!e.Sender.IsValidTarget() || !flayMenu["EGapcloser"].Cast<CheckBox>().CurrentValue || sender.IsAlly)
            {
                return;
            }

            E.Cast(e.Sender);
        }

        /// <summary>
        /// The ınterrupter_ on ınterruptable spell.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (!sender.IsValidTarget(Q.Range) || e.DangerLevel != DangerLevel.High || sender.IsAlly)
            {
                return;
            }

            if (E.IsReady() && E.IsInRange(sender) && flayMenu["EInterrupt"].Cast<CheckBox>().CurrentValue)
            {
                E.Cast(sender);
            }
            else if (Q.IsReady() && hookMenu["QInterrupt"].Cast<CheckBox>().CurrentValue && Prediction.Position.PredictLinearMissile(
                    sender,
                    Q.Range,
                    Q.Width,
                    Q.CastDelay,
                    Q.Speed,
                    int.MaxValue).HitChance >= HitChance.Low)
            {
                Q.Cast(sender);
            }
        }

        /// <summary>
        /// The auto q.
        /// </summary>
        private static void AutoQ()
        {
            if (!hookMenu["QImmobile"].Cast<CheckBox>().CurrentValue)
            {
                return;
            }

            var autoQTarget =
                HeroManager.Enemies.FirstOrDefault(
                    x =>
                    x.HasBuffOfType(BuffType.Charm) || x.HasBuffOfType(BuffType.Knockup)
                    || x.HasBuffOfType(BuffType.Stun) || x.HasBuffOfType(BuffType.Suppression)
                    || x.HasBuffOfType(BuffType.Snare));

            if (autoQTarget != null && !autoQTarget.HasBuff("ThreshQ") && Prediction.Position.PredictLinearMissile(
                    autoQTarget,
                    Q.Range,
                    Q.Width,
                    Q.CastDelay,
                    Q.Speed,
                    int.MaxValue).HitChance >= HitChance.High)
            {
                Q.Cast(autoQTarget);
            }
        }

        /// <summary>
        /// The auto w.
        /// </summary>
        private static void AutoW()
        {
            var lanternLowAllies = lanternMenu["WLowAllies"].Cast<CheckBox>().CurrentValue;
            var lanternHealthPercent = lanternMenu["WAllyPercent"].Cast<Slider>().CurrentValue;

            if (lanternLowAllies)
            {
                var ally =
                    HeroManager.Allies.Where(
                        x => x.IsValidTarget(W.Range) && x.HealthPercent < lanternHealthPercent)
                        .FirstOrDefault();

                if (ally != null && ally.CountEnemiesInRange(700) >= 1)
                {
                    W.Cast(W.GetPrediction(ally).CastPosition);
                }
            }
        }

        /// <summary>
        /// The obj_ a ı_ base_ on new path.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
        private static void Obj_AI_Base_OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if (!sender.IsValid() || !args.IsDash || !sender.IsValidTarget(Q.Range) || sender.IsAlly || !sender.IsMinion)
            {
                return;
            }
            if (Q.IsReady() && !E.IsInRange(sender) && hookMenu["QDash"].Cast<CheckBox>().CurrentValue)
            {
                var endPosition = args.Path.Last();

                if (Prediction.Position.PredictLinearMissile(
                    sender,
                    Q.Range,
                    Q.Width,
                    Q.CastDelay,
                    Q.Speed,
                    int.MaxValue,
                    endPosition).HitChance != HitChance.High)
                {
                    return;
                }

                Q.Cast(endPosition);
                
            }

            else if (E.IsReady() && E.IsInRange(sender) && flayMenu["EDash"].Cast<CheckBox>().CurrentValue)
            {
                var endPosition = args.Path.Last();
                var isFleeing = endPosition.Distance(Player.ServerPosition) > Player.Distance(sender);

                var prediction = E.GetPrediction(sender);

                if (prediction.HitChance != HitChance.High)
                {
                    return;
                }

                var x = Player.ServerPosition.X - endPosition.X;
                var y = Player.ServerPosition.Y - endPosition.Y;

                var vector = new Vector3(
                    Player.ServerPosition.X + x,
                    Player.ServerPosition.Y + y,
                    Player.ServerPosition.Z);

                E.Cast(
                    !isFleeing
                        ? prediction.CastPosition
                        : vector);
            }
        }

        /// <summary>
        /// The game_ on tick.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Game_OnTick(EventArgs args)
        {
            Orbwalker.ForcedTarget = null;
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }

            AutoW();
            AutoQ();
            if (pull_push["push"].Cast<KeyBind>().CurrentValue)
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                if (target != null)
                Push(target);
            }
            if (pull_push["pull"].Cast<KeyBind>().CurrentValue)
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                if (target != null)
                Pull(target);
            }
        }

        private static void Pull(AIHeroClient target)
        {
            if (Player.Distance(target) <= E.Range)
            {
                var pX = Player.Position.X + (Player.Position.X - target.Position.X);
                var pY = Player.Position.Y + (Player.Position.Y - target.Position.Y);
                E.Cast(new Vector3(new Vector2(pX), pY));

            }
        }

        private static void Push(AIHeroClient target)
        {
            if (Player.Distance(target) <= E.Range)
            {
                E.Cast(target.ServerPosition);
            }
        }

        /// <summary>
        /// The harass.
        /// </summary>
        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

            if (!target.IsValidTarget())
            {
                return;
            }

            var useQ1 = HarassMenu["UseQ1Harass"].Cast<CheckBox>().CurrentValue;
            var useQ2 = HarassMenu["UseQ2Harass"].Cast<CheckBox>().CurrentValue;
            var useE = HarassMenu["UseEHarass"].Cast<CheckBox>().CurrentValue;

            if (Q.IsReady())
            {
                if (useQ1 && !target.HasBuff("ThreshQ") && Prediction.Position.PredictLinearMissile(
                    target,
                    Q.Range,
                    Q.Width,
                    Q.CastDelay,
                    Q.Speed,
                    int.MaxValue).HitChance >= HitChance.High)
                {
                    Q.Cast(target);
                }

                if (useQ2 && target.HasBuff("ThreshQ") && !target.IsMinion)
                {
                    Q2.Cast();
                }
            }

            if (useE && E.IsReady() || !target.HasBuff("ThreshQ"))
            {
                var isFleeing = Player.Distance(target) < target.Distance(Game.CursorPos);
                var prediction = E.GetPrediction(target);

                if (prediction.HitChance != HitChance.High)
                {
                    return;
                }

                var x = Player.ServerPosition.X - target.ServerPosition.X;
                var y = Player.ServerPosition.Y - target.ServerPosition.Y;

                var vector = new Vector3(
                    Player.ServerPosition.X + x,
                    Player.ServerPosition.Y + y,
                    Player.ServerPosition.Z);

                E.Cast(
                    isFleeing
                        ? prediction.CastPosition
                        : vector);
            }
        }

        /// <summary>
        /// The combo.
        /// </summary>
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

            if (!target.IsValidTarget())
            {
                return;
            }

            var useQ = ComboMenu["UseQCombo"].Cast<CheckBox>().CurrentValue;
            var useW = ComboMenu["UseWCombo"].Cast<CheckBox>().CurrentValue;
            var useE = ComboMenu["UseECombo"].Cast<CheckBox>().CurrentValue;
            

            if (useQ && Q.IsReady())
            {
                if (target.HasBuff("ThreshQ") && !target.IsMinion)
                {
                    Q2.Cast();
                }
                else if (!target.HasBuff("ThreshQ") && Prediction.Position.PredictLinearMissile(
                    target,
                    Q.Range,
                    Q.Width,
                    Q.CastDelay,
                    Q.Speed,
                    int.MaxValue).HitChance >= HitChance.High)
                {
                    Q.Cast(target);
                }
            }

            if (useW && W.IsReady() && target.HasBuff("ThreshQ"))
            {
                var ally =
                    HeroManager.Allies.Where(x => !x.IsMe && x.IsValidTarget(W.Range) && x.Distance(Player) > 300)
                        .FirstOrDefault();

                if (ally != null)
                {
                    W.Cast(W.GetPrediction(ally).CastPosition);
                }
                else if (Player.HealthPercent < 50)
                {
                    W.Cast(W.GetPrediction(Player).CastPosition);
                }
            }

            if (useE && E.IsReady() || !target.HasBuff("ThreshQ"))
            {
                var isFleeing = Player.Distance(target) < target.Distance(Game.CursorPos);
                var prediction = E.GetPrediction(target);

                if (prediction.HitChance == HitChance.High)
                {
                    var x = Player.ServerPosition.X - target.ServerPosition.X;
                    var y = Player.ServerPosition.Y - target.ServerPosition.Y;

                    var vector = new Vector3(
                        Player.ServerPosition.X + x,
                        Player.ServerPosition.Y + y,
                        Player.ServerPosition.Z);

                    E.Cast(
                        isFleeing
                            ? prediction.CastPosition
                            : vector);
                }
            }
            var useR = ComboMenu["UseRCombo"].Cast<CheckBox>().CurrentValue;
            var ultEnemies = ComboMenu["UseRComboEnemies"].Cast<Slider>().CurrentValue;
            if (target.IsValidTarget(R.Range) && useR && R.IsReady() && Player.CountEnemiesInRange(R.Range) >= ultEnemies)
            {
                R.Cast();
            }
            
        }
    }
}
