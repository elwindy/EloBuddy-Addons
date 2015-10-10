using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;
using Color = System.Drawing.Color;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace EkkoBuddy
{
    

    class Program
    {

        public static Obj_AI_Base _ekkoPast;
        public static readonly IDictionary<int, float> _pastStatus = new Dictionary<int, float>();
        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        public static Spell.Skillshot Q, Q2, W, E, R;

        public static Spell.Active R1;

        public static Menu main, spell, combo, harass, lane, flee, misc, drawing;

        internal static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Load;
        }

        private static void Load(EventArgs args)
        {
            if (Player.ChampionName != "Ekko")
            {
                return;
            }

            Q = new Spell.Skillshot(SpellSlot.Q, 800, SkillShotType.Linear, (int).25, 1700, 60);
            Q2 = new Spell.Skillshot(SpellSlot.Q, 1050, SkillShotType.Linear, (int).5f, 1200, 120);
            W = new Spell.Skillshot(SpellSlot.W, 1600, SkillShotType.Circular, (int).5f, int.MaxValue, 350);
            E = new Spell.Skillshot(SpellSlot.E, 352,SkillShotType.Linear);
            R = new Spell.Skillshot(SpellSlot.R, 375, SkillShotType.Circular, (int).1f, int.MaxValue, 375);
            R1 = new Spell.Active(SpellSlot.R);
            main = MainMenu.AddMenu("EkkoBuddy", "ekko");
            spell = main.AddSubMenu("SpellMenu", "spellmenu");
            spell.AddGroupLabel("::SpellMenu::");
            spell.AddLabel("QMenu");
            spell.Add("Auto_Q_Slow", new CheckBox("Auto Q Slow"));
            spell.Add("Auto_Q_Dashing", new CheckBox("Auto Q Dashing"));
            spell.Add("Auto_Q_Immobile", new CheckBox("Auto Q Immobile"));
            spell.AddSeparator(18);
            spell.AddLabel("WMenu");
            spell.Add("W_On_Cc", new CheckBox("W On top of Hard CC"));
            spell.AddSeparator(18);
            spell.AddLabel("EMenu");
            spell.Add(
                "E_If_UnderTurret",
                new KeyBind("E Under Enemy Turret", false, KeyBind.BindTypes.PressToggle, 'H'));
            spell.Add("Do_Not_E", new Slider("Do not E if >= Enemies Around location", 3, 1, 5));
            spell.Add("Do_Not_E_HP", new Slider("Do not E if HP <= %", 20));
            spell.AddSeparator(18);
            spell.AddLabel("RMenu");
            spell.Add("R_Safe_Net", new Slider("R If Player Take % dmg > in Past 4 Seconds", 60));
            spell.Add("R_Safe_Net2", new Slider("R If Player HP <= %", 10));
            spell.Add("R_On_Killable", new CheckBox("Ult Enemy If they are Killable with combo"));
            spell.Add("R_KS", new CheckBox("Smart R KS"));

            combo = main.AddSubMenu("Combo", "combo");
            combo.AddGroupLabel("::Combo::");
            combo.Add("UseQCombo", new CheckBox("Use Q"));
            combo.Add("UseWCombo", new CheckBox("Use W"));
            combo.Add("UseECombo", new CheckBox("Use E"));
            combo.Add("UseRCombo", new CheckBox("Use R"));

            harass = main.AddSubMenu("Harass", "harass");
            harass.AddGroupLabel("::Harass::");
            harass.Add("UseQHarass", new CheckBox("Use Q"));
            harass.Add("UseWHarass", new CheckBox("Use W", false));
            harass.Add("UseEHarass", new CheckBox("Use E"));
            harass.Add("Harass", new Slider("Mana Manager", 50));

            flee = main.AddSubMenu("Flee", "flee");
            flee.AddGroupLabel("::Flee::");
            flee.Add("UseQFlee", new CheckBox("Use Q"));
            flee.Add("UseWFlee", new CheckBox("Use W"));
            flee.Add("UseEFlee", new CheckBox("Use E"));

            misc = main.AddSubMenu("Misc", "misc");
            misc.AddGroupLabel("::Misc::");
            misc.Add("smartKS", new CheckBox("Smart KS"));
            misc.Add("UseInt", new CheckBox("Use W to Interrupt"));
            misc.Add("UseGapQ", new CheckBox("Use Q for GapCloser"));
            misc.Add("UseGapW", new CheckBox("Use W for GapCloser"));

            drawing = main.AddSubMenu("Drawing", "draw");
            drawing.AddGroupLabel("::Drawing::");
            drawing.Add("Draw_Disabled", new CheckBox("Disable All", false));
            drawing.Add("Draw_Q", new CheckBox("Draw Q"));
            drawing.Add("Draw_W", new CheckBox("Draw W"));
            drawing.Add("Draw_E", new CheckBox("Draw E"));
            drawing.Add("Draw_R", new CheckBox("Draw R"));

            AttackableUnit.OnDamage += AIHeroClient_OnDamage;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
        }

        static void Game_OnUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;


            if (_ekkoPast == null && R.IsReady())
                _ekkoPast = ObjectManager.Get<Obj_AI_Base>().FirstOrDefault(x => x.Name == "Ekko" && x.IsAlly);

            UpdateOldStatus();
            SafetyR();

            if (misc["smartKS"].Cast<CheckBox>().CurrentValue)
                CheckKs();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                Flee();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            else
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                    Harass();
            }

            AutoQ();
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (drawing["Draw_Disabled"].Cast<CheckBox>().CurrentValue)
                return;

            if (drawing["Draw_Q"].Cast<CheckBox>().CurrentValue)
                if (Q.Level > 0)
                    EloBuddy.SDK.Rendering.Circle.Draw(new ColorBGRA(Q.IsReady() ? Color.Green.ToArgb() : Color.Red.ToArgb()), Q.Range, Player.Position);

            if (drawing["Draw_W"].Cast<CheckBox>().CurrentValue)
                if (W.Level > 0)
                    EloBuddy.SDK.Rendering.Circle.Draw(new ColorBGRA(W.IsReady() ? Color.Green.ToArgb() : Color.Red.ToArgb()), W.Range, Player.Position);

            if (drawing["Draw_E"].Cast<CheckBox>().CurrentValue)
                if (E.Level > 0)
                    EloBuddy.SDK.Rendering.Circle.Draw(new ColorBGRA(E.IsReady() ? Color.Green.ToArgb() : Color.Red.ToArgb()), E.Range, Player.Position);

            if (drawing["Draw_R"].Cast<CheckBox>().CurrentValue)
                if (R.Level > 0 && _ekkoPast != null)
                    EloBuddy.SDK.Rendering.Circle.Draw(new ColorBGRA(R.IsReady() ? Color.RoyalBlue.ToArgb() : Color.RoyalBlue.ToArgb()), R.Width, _ekkoPast.Position);

            
            if (R.IsReady() && _ekkoPast != null)
            {
                Vector2 wts = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(wts[0] - 20, wts[1], Color.White, "Enemies Hit with R: " + TargetHitWithR());
            }
            
        }

        static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (e.Sender.IsAlly || e.Sender.IsMinion)
            {
                return;
            }
            if (misc["UseGapQ"].Cast<CheckBox>().CurrentValue)
            {
                if (Q.IsReady() && e.Sender.IsValidTarget(Q.Range))
                    Q.Cast(e.Sender);
            }

            if (misc["UseGapW"].Cast<CheckBox>().CurrentValue)
            {
                if (W.IsReady() && e.Sender.IsValidTarget(W.Range))
                    W.Cast(e.Sender);
            }
        }

        static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (e.Sender.IsAlly)
            {
                return;
            }
            if (!spell["UseInt"].Cast<CheckBox>().CurrentValue) return;

            if (Player.Distance(sender.Position) < W.Range && W.IsReady())
            {
                W.Cast(sender);
            }
        }

        static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_AI_Base) || !sender.IsAlly)
                return;

            if (sender.IsAlly && sender.Name == "Ekko")
            {
                _ekkoPast = null;
            }
        }

        static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_AI_Base) || !sender.IsAlly)
                return;

            if (sender.Name == "Ekko")
            {
                _ekkoPast = (Obj_AI_Base)sender;
            }
        }

        static void AIHeroClient_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            if (!sender.IsMe || !R.IsReady() || args.Damage > 45)
                return;

            var safeNet = spell["R_Safe_Net2"].Cast<Slider>().CurrentValue;

            if (Player.HealthPercent <= safeNet)
            {
                R1.Cast();
            }
        }

        private static double PassiveDmg(Obj_AI_Base target)
        {

            return Player.CalculateDamageOnUnit(target, DamageType.Magical,
                15 + (12 * Player.Level) + Player.TotalMagicalDamage * .7f);
        }

        private static double TotalQDmg(Obj_AI_Base target)
        {
            if (Q.Level < 1)
                return 0;

            return Qdmg(target) + Q2Dmg(target);
        }

        private static double Qdmg(Obj_AI_Base target)
        {
            if (Q.Level < 1)
                return 0;

            return Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (float)(new double[] { 60, 75, 90, 105, 120 }[Q.Level - 1] + Player.TotalMagicalDamage * .2f));
        }

        private static double Q2Dmg(Obj_AI_Base target)
        {
            if (Q.Level < 1)
                return 0;

            return Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (float)(new double[] { 60, 85, 110, 135, 160 }[Q.Level - 1] + Player.TotalMagicalDamage * .6f));
        }

        private static double Edmg(Obj_AI_Base target)
        {
            if (E.Level < 1)
                return 0;

            return Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (float)(new double[] { 50, 80, 110, 140, 170 }[E.Level - 1] + Player.TotalMagicalDamage * .2f));
        }


        private static double Rdmg(Obj_AI_Base target)
        {
            if (R.Level < 1)
                return 0;

            return Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (float)(new double[] { 200, 350, 500 }[R.Level - 1] + Player.TotalMagicalDamage * 1.3f));
        }

        private static float GetComboDamage(Obj_AI_Base target)
        {
            double comboDamage = 0;

            comboDamage += PassiveDmg(target);

            if (Q.IsReady())
                comboDamage += TotalQDmg(target);

            if (E.IsReady())
                comboDamage += Edmg(target);

            if (R.IsReady())
                comboDamage += Rdmg(target);

            return (float)(comboDamage + Player.GetAutoAttackDamage(target) * 2);
        }

        private static void Combo()
        {
            UseSpells(combo["UseQCombo"].Cast<CheckBox>().CurrentValue, combo["UseWCombo"].Cast<CheckBox>().CurrentValue,
                combo["UseECombo"].Cast<CheckBox>().CurrentValue, combo["UseRCombo"].Cast<CheckBox>().CurrentValue, "Combo");
        }

        private static void Harass()
        {
            UseSpells(harass["UseQHarass"].Cast<CheckBox>().CurrentValue, harass["UseWHarass"].Cast<CheckBox>().CurrentValue,
                harass["UseEHarass"].Cast<CheckBox>().CurrentValue, false, "Harass");
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR, string source)
        {
            if (source == "Harass" && Player.ManaPercent <= harass["Harass"].Cast<Slider>().CurrentValue)
                return;

            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            if (!target.IsValidTarget(Q.Range))
                return;

            if (useW && W.IsReady())
            {
                var wTarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);

                var pred = Prediction.Position.PredictCircularMissile(wTarget, W.Range, W.Radius, W.CastDelay, W.Speed, Player.ServerPosition);

                if (spell["W_On_Cc"].Cast<CheckBox>().CurrentValue)
                {
                    foreach (var enemies in HeroManager.Enemies.Where(x => x.IsValidTarget(W.Range)))
                    {
                        if (enemies.HasBuffOfType(BuffType.Snare) || enemies.HasBuffOfType(BuffType.Stun) || enemies.HasBuffOfType(BuffType.Fear) || enemies.HasBuffOfType(BuffType.Suppression))
                        {
                            W.Cast(enemies);
                            break;
                        }
                    }
                }

                if (pred.HitChance >= HitChance.High)
                {
                    W.Cast(pred.CastPosition);
                }
            }

            if (useE && E.IsReady())
            {
                var etarget = TargetSelector.GetTarget(E.Range + 425, DamageType.Magical);

                if (etarget.IsValidTarget(E.Range + 425))
                {
                    var vec = Player.ServerPosition.Extend(etarget.ServerPosition, E.Range - 10);

                    if (vec.Distance(target.ServerPosition) < 425 && ShouldE((Vector3)vec))
                    {
                        E.Cast((Vector3)vec);
                        Core.DelayAction(() => EloBuddy.Player.IssueOrder(GameObjectOrder.AttackUnit, etarget), E.CastDelay * 1000 + Game.Ping);
                    }
                }
            }

            if (useQ && Q.IsReady())
            {
                var qTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                var q2Target = TargetSelector.GetTarget(Q2.Range, DamageType.Magical);

                if(qTarget == null || q2Target == null)
                    return;

                var pred = Prediction.Position.PredictLinearMissile(
                    qTarget,
                    Q.Range,
                    Q.Width,
                    Q.CastDelay,
                    Q.Speed,
                    Q.AllowedCollisionCount);
                var pred2 = Prediction.Position.PredictLinearMissile(
                    q2Target,
                    Q2.Range,
                    Q2.Width,
                    Q2.CastDelay,
                    Q2.Speed,
                    Q2.AllowedCollisionCount);
                if (pred.HitChance >= HitChance.High)
                {
                    Q.Cast(pred.CastPosition);
                }
                else if (pred2.HitChance >= HitChance.High)
                {
                    Q2.Cast(pred2.CastPosition);
                }
            }

            if (useR && R.IsReady() && _ekkoPast != null)
            {
                if (spell["R_On_Killable"].Cast<CheckBox>().CurrentValue)
                {
                    if ((from enemie in HeroManager.Enemies.Where(x => x.IsValidTarget()).Where(x => Prediction.Position.PredictCircularMissile(x, R.Range, R.Radius,R.CastDelay,R.Speed).UnitPosition.Distance(_ekkoPast.ServerPosition) < 400)
                         let dmg = GetComboDamage(enemie)
                         where dmg > enemie.Health
                         select enemie).Any())
                    {
                        R1.Cast();
                        return;
                    }
                }

            }
        }

        public static bool UnderTurret(Vector3 position, bool enemyTurretsOnly)
        {
            return
                ObjectManager.Get<Obj_AI_Turret>().Any(turret => IsValidTarget(turret, 950, enemyTurretsOnly, position));
        }

        public static bool IsValidTarget(AttackableUnit unit,
            float range = float.MaxValue,
            bool checkTeam = true,
            Vector3 from = new Vector3())
        {
            if (unit == null || !unit.IsValid || unit.IsDead || !unit.IsVisible || !unit.IsTargetable ||
                unit.IsInvulnerable)
            {
                return false;
            }

            if (checkTeam && unit.Team == ObjectManager.Player.Team)
            {
                return false;
            }

            var @base = unit as Obj_AI_Base;
            var unitPosition = @base != null ? @base.ServerPosition : unit.Position;

            return !(range < float.MaxValue) ||
                   !(Vector2.DistanceSquared(
                       (@from.To2D().IsValid() ? @from : ObjectManager.Player.ServerPosition).To2D(),
                       unitPosition.To2D()) > range * range);
        }

        private static bool ShouldE(Vector3 vec)
        {
            var maxEnemies = spell["Do_Not_E"].Cast<Slider>().CurrentValue;

            if (!spell["E_If_UnderTurret"].Cast<KeyBind>().CurrentValue && UnderTurret(vec,true))
                return false;

            if (Player.HealthPercent <= spell["Do_Not_E_HP"].Cast<Slider>().CurrentValue)
                return false;

            if (vec.CountEnemiesInRange(600) >= maxEnemies)
                return false;

            return true;
        }

        private static void Flee()
        {
            var useQ = flee["UseQFlee"].Cast<CheckBox>().CurrentValue;
            var useW = flee["UseWFlee"].Cast<CheckBox>().CurrentValue;
            var useE = flee["UseEFlee"].Cast<CheckBox>().CurrentValue;

            if (!useQ && !useW)
                return;

            if (useE)
            {
                var vec = Player.ServerPosition.Extend(Game.CursorPos, E.Range);
                E.Cast((Vector3)vec);
            }

            foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(Q.Range)))
            {
                if (Q.IsReady() && useQ)
                    Q.Cast(target);

                if (W.IsReady() && useW)
                {
                    W.Cast((Vector3)Player.ServerPosition.Extend(Game.CursorPos, 400));
                }
            }
        }

        private static void SafetyR()
        {
            var burstHpAllowed = spell["R_Safe_Net"].Cast<Slider>().CurrentValue;

            if (_pastStatus.ContainsKey(Environment.TickCount - 3900))
            {
                float burst = _pastStatus[Environment.TickCount - 3900] - Player.HealthPercent;

                if (burst >= burstHpAllowed)
                {
                    R1.Cast();
                }
            }
        }

        private static int TargetHitWithR()
        {
            if (!R.IsReady() || _ekkoPast == null)
                return 0;

            return HeroManager.Enemies.Where(x => x.IsValidTarget()).Count(x => _ekkoPast.Distance(Prediction.Position.PredictCircularMissile(x, R.Range, R.Radius, R.CastDelay, R.Speed).UnitPosition) < 400);
        }

        private static void AutoQ()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

            if (target != null)
            {
                if (Q.GetPrediction(target).HitChance >= HitChance.High &&
                    (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare)) &&
                    spell["Auto_Q_Immobile"].Cast<CheckBox>().CurrentValue)
                {
                    Q.Cast(target);
                }

                if (target.HasBuffOfType(BuffType.Slow) && spell["Auto_Q_Slow"].Cast<CheckBox>().CurrentValue)
                {
                    Q.Cast(target);
                }

                if (target.IsDashing() && spell["Auto_Q_Dashing"].Cast<CheckBox>().CurrentValue)
                {
                    Q.Cast(target);
                }
            }
        }
       
        private static void CheckKs()
        {
            foreach (AIHeroClient target in ObjectManager.Get<AIHeroClient>().Where(x => x.IsValidTarget(Q2.Range)).OrderByDescending(GetComboDamage))
            {
                //Q
                if (Player.Distance(target) <= Q.Range && Qdmg(target) > target.Health && Q.IsReady())
                {
                    Q.Cast(target);
                    return;
                }

                //Q2
                if (Player.Distance(target) <= Q2.Range && TotalQDmg(target) > target.Health && Q.IsReady() && Q2.GetPrediction(target).HitChance >= HitChance.High)
                {
                    Q2.Cast(target);
                    return;
                }

                //E
                if (Player.Distance(target) <= E.Range + 475 && Edmg(target) > target.Health && E.IsReady())
                {
                    var vec = Player.ServerPosition.Extend(target.ServerPosition, E.Range - 10);
                    E.Cast((Vector3)vec);
                    var target1 = target;
                    Core.DelayAction(() => EloBuddy.Player.IssueOrder(GameObjectOrder.AttackUnit, target1), E.CastDelay * 1000 + Game.Ping);
                    return;
                }

                //R
                if (R.IsReady() && _ekkoPast != null)
                    if (_ekkoPast.Distance(Prediction.Position.PredictCircularMissile(target, R.Range, R.Radius, R.CastDelay, R.Speed).UnitPosition) <= R.Width && Rdmg(target) > target.Health)
                    {
                        R1.Cast();
                        return;
                    }
            }
        }

        private static void UpdateOldStatus()
        {
            if (_pastStatus.Keys.ToList().All(x => x != Environment.TickCount))
            {
                _pastStatus.Add(Environment.TickCount, Player.HealthPercent);
            }

            foreach (var remove in _pastStatus.Keys.Where(x => Environment.TickCount - x > 4000).ToList())
            {
                _pastStatus.Remove(remove);
            }
        }


    }
}
