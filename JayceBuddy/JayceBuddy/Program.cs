using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;
using Color = System.Drawing.Color;

namespace JayceBuddy
{
    using EloBuddy.SDK.Enumerations;
    using EloBuddy.SDK.Events;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;

    class Program
    {

        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        private static Spell.Skillshot Q, QExtend, E;

        private static Spell.Targeted Q2, E2;

        private static Spell.Active W, W2, R;

        public static Menu combo, harass, misc, draw;
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.ChampionName != "Jayce")
            {
                return;
                
            }

            Q = new Spell.Skillshot(SpellSlot.Q, 1050, SkillShotType.Linear,(int)0.25, 1200, 79);
            QExtend = new Spell.Skillshot(SpellSlot.Q, 1650, SkillShotType.Linear, (int)0.35, 1600, 98);
            Q2 = new Spell.Targeted(SpellSlot.Q, 600);
            W = new Spell.Active(SpellSlot.W);
            W2 = new Spell.Active(SpellSlot.W, 350);
            E = new Spell.Skillshot(SpellSlot.E, 650, SkillShotType.Circular, (int)0.1, int.MaxValue, 120);
            E2 = new Spell.Targeted(SpellSlot.E, 240);
            R = new Spell.Active(SpellSlot.R);

            var main = MainMenu.AddMenu("JayceBuddy", "jaycebuddy");
            combo = main.AddSubMenu("Combo", "combo");
            combo.AddGroupLabel("::Combo::");
            combo.Add("UseQCombo", new CheckBox("Use Cannon Q"));
            combo.Add("UseWCombo", new CheckBox("Use Cannon W"));
            combo.Add("UseECombo", new CheckBox("Use Cannon E"));
            combo.Add("UseQComboHam", new CheckBox("Use Hammer Q"));
            combo.Add("UseWComboHam", new CheckBox("Use Hammer W"));
            combo.Add("UseEComboHam", new CheckBox("Use Hammer E"));
            combo.Add("UseRCombo", new CheckBox("Use R to Switch"));

            harass = main.AddSubMenu("Harass", "harass");
            harass.AddGroupLabel("::Harass::");
            harass.Add("UseQHarass", new CheckBox("Use Q"));
            harass.Add("UseWHarass", new CheckBox("Use W"));
            harass.Add("UseEHarass", new CheckBox("Use E"));
            harass.Add("UseQHarassHam", new CheckBox("Use Q Hammer"));
            harass.Add("UseWHarassHam", new CheckBox("Use W Hammer"));
            harass.Add("UseEHarassHam", new CheckBox("Use E Hammer"));
            harass.Add("UseRHarass", new CheckBox("Use R to Switch"));
            harass.Add("Harass", new Slider("if Mana %{0} >", 60));

            misc = main.AddSubMenu("Misc", "misc");
            misc.AddGroupLabel("::Misc");
            misc.Add("UseInt", new CheckBox("Use E to Interrupt"));
            misc.Add("UseGap", new CheckBox("Use E for GapCloser"));
            misc.Add("forceGare", new CheckBox("Force Gate After Q", false));
            misc.Add("gatePlace", new Slider("Gate Distance", 300, 50, 600));
            misc.Add("UseQAlways", new CheckBox("Use Q When E on CD"));
            misc.Add("autoE", new Slider("EPushInCombo HP < %{0}", 20));
            misc.Add("smartKS", new CheckBox("Smart Ks"));

            draw = main.AddSubMenu("Drawings", "draw");
            draw.AddGroupLabel("::Drawings::");
            draw.Add("drawcds", new CheckBox("Draw Cooldowns"));

            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            GameObject.OnCreate += GameObject_OnCreate;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
        }

        

        //status
        public static bool HammerTime;
        private static readonly SpellDataInst _qdata = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q);

        //CoolDowns
        private static readonly float[] _cannonQcd = { 8, 8, 8, 8, 8 };
        private static readonly float[] _cannonWcd = { 14, 12, 10, 8, 6 };
        private static readonly float[] _cannonEcd = { 16, 16, 16, 16, 16 };

        private static readonly float[] _hammerQcd = { 16, 14, 12, 10, 8 };
        private static readonly float[] _hammerWcd = { 10, 10, 10, 10, 10 };
        private static readonly float[] _hammerEcd = { 14, 13, 12, 11, 10 };
        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            if (enemy == null)
                return 0;

            var damage = 0d;

            if (CanQcd == 0 && CanEcd == 0 && Q.Level > 0 && E.Level > 0)
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q) * 1.4;
            else if (CanQcd == 0 && Q.Level > 0)
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (_hamQcd == 0 && Q.Level > 0)
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (_hamWcd == 0 && W.Level > 0)
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);

            if (_hamEcd == 0 && E.Level > 0)
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            damage += Player.GetAutoAttackDamage(enemy) * 3;
            return (float)damage;
        }

        private static void Combo()
        {
            UseSpells(combo["UseQCombo"].Cast<CheckBox>().CurrentValue, combo["UseWCombo"].Cast<CheckBox>().CurrentValue,
                combo["UseECombo"].Cast<CheckBox>().CurrentValue, combo["UseQComboHam"].Cast<CheckBox>().CurrentValue, combo["UseWComboHam"].Cast<CheckBox>().CurrentValue,
                combo["UseEComboHam"].Cast<CheckBox>().CurrentValue, combo["UseRCombo"].Cast<CheckBox>().CurrentValue, "Combo");

        }
        private static void Harass()
        {
            UseSpells(harass["UseQHarass"].Cast<CheckBox>().CurrentValue, harass["UseWHarass"].Cast<CheckBox>().CurrentValue,
                harass["UseEHarass"].Cast<CheckBox>().CurrentValue, harass["UseQHarassHam"].Cast<CheckBox>().CurrentValue, harass["UseWHarassHam"].Cast<CheckBox>().CurrentValue,
                harass["UseEHarassHam"].Cast<CheckBox>().CurrentValue, harass["UseRHarass"].Cast<CheckBox>().CurrentValue, "Harass");
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useQ2, bool useW2, bool useE2, bool useR, String source)
        {
            var qTarget = TargetSelector.GetTarget(QExtend.Range, DamageType.Physical);
            var q2Target = TargetSelector.GetTarget(Q2.Range, DamageType.Physical);
            var e2Target = TargetSelector.GetTarget(E2.Range, DamageType.Physical);


            //Main Combo
            if (source == "Combo")
            {

                if (qTarget != null)
                {
                    if (useQ && CanQcd == 0 && Player.Distance(qTarget.Position) <= QExtend.Range && !HammerTime)
                    {
                        CastQCannon(qTarget, useE, source);
                        return;
                    }
                    if (_canWcd == 0 && Player.Distance(qTarget.Position) < 600 && !HammerTime && W.Level > 0 && W.IsReady()
                    && useW)
                    {
                        Orbwalker.ResetAutoAttack();
                        W.Cast();
                    }
                }
                

                if (HammerTime)
                {
                    if (q2Target != null)
                    {
                        if (useW2 && Player.Distance(q2Target.Position) <= 300 && W.IsReady())
                            W.Cast();

                        if (useQ2 && Player.Distance(q2Target.Position) <= Q2.Range + q2Target.BoundingRadius && Q2.IsReady())
                            Q2.Cast(q2Target);
                    }
                    if (e2Target != null)
                    {
                        if (useE2 && ECheck(e2Target, useQ, useW) && Player.Distance(e2Target.Position) <= E2.Range + e2Target.BoundingRadius && E2.IsReady())
                            E2.Cast(q2Target);
                    }
                }

                //form switch check
                if (useR)
                    SwitchFormCheck(q2Target, useQ, useW, useQ2, useW2, useE2);
            }
            else if (source == "Harass" && Player.ManaPercent < harass["Harass"].Cast<Slider>().CurrentValue)
            {
                if (qTarget != null)
                {
                    if (useQ && CanQcd == 0 && Player.Distance(qTarget.Position) <= QExtend.Range && !HammerTime)
                    {
                        CastQCannon(qTarget, useE, source);
                        return;
                    }
                    if (_canWcd == 0 && Player.Distance(qTarget.Position) < 600 && !HammerTime && W.Level > 0 && W.IsReady()
                    && useW)
                    {
                        Orbwalker.ResetAutoAttack();
                        W.Cast();
                    }
                }
                if (HammerTime)
                {
                    if (q2Target != null)
                    {
                        if (useW2 && Player.Distance(q2Target.Position) <= 300 && W.IsReady())
                            W.Cast();

                        if (useQ2 && Player.Distance(q2Target.Position) <= Q2.Range + q2Target.BoundingRadius && Q2.IsReady())
                            Q2.Cast(q2Target);
                    }

                    if (q2Target != null)
                    {
                        if (useE2 && Player.Distance(q2Target.Position) <= E2.Range + e2Target.BoundingRadius && E2.IsReady())
                            E2.Cast(q2Target);
                    }
                }

                //form switch check
                if (useR && q2Target != null)
                    SwitchFormCheck(q2Target, useQ, useW, useQ2, useW2, useE2);
            }

        }

        private static bool ECheck(AIHeroClient target, bool useQ, bool useW)
        {
            if (Player.GetSpellDamage(target, SpellSlot.E) >= target.Health)
            {
                return true;
            }
            if (((CanQcd == 0 && useQ) || (_canWcd == 0 && useW)) && _hamQcd != 0 && _hamWcd != 0)
            {
                return true;
            }
            if (WallStun(target))
            {
                return true;
            }

            var hp = misc["autoE"].Cast<Slider>().CurrentValue;
            if (Player.HealthPercent <= hp)
            {
                return true;
            }

            return false;
        }

        private static bool WallStun(AIHeroClient target)
        {
            Vector3 startPos = default(Vector3);

            if (startPos == default(Vector3))
            {
                startPos = Player.ServerPosition;
            }

            var knockbackPos = startPos.Extend(
                target.ServerPosition,
                startPos.Distance(target.ServerPosition) + 450);

            var flags = NavMesh.GetCollisionFlags(knockbackPos);
            var collision = flags.HasFlag(CollisionFlags.Building) || flags.HasFlag(CollisionFlags.Wall);

            if (collision)
                return true;

            return false;
        }

        private static void KsCheck()
        {
            foreach (AIHeroClient enemy in ObjectManager.Get<AIHeroClient>().Where(x => x.IsValidTarget(QExtend.Range) && x.IsEnemy && !x.IsDead).OrderByDescending(GetComboDamage))
            {
                //Q
                if ((Player.GetSpellDamage(enemy, SpellSlot.Q) - 20) > enemy.Health && CanQcd == 0 && Player.Distance(enemy.ServerPosition) <= Q.Range)
                {
                    if (HammerTime && R.IsReady())
                        R.Cast();

                    if (!HammerTime && Q.IsReady())
                        Q.Cast(enemy);
                }

                //QE
                if ((Player.GetSpellDamage(enemy, SpellSlot.Q) * 1.4 - 20) > enemy.Health && CanQcd == 0 && CanEcd == 0 && Player.Distance(enemy.ServerPosition) <= QExtend.Range)
                {
                    if (HammerTime && R.IsReady())
                        R.Cast();

                    if (!HammerTime)
                        CastQCannon(enemy, true, "Null");
                }

                //Hammer QE
                if ((Player.GetSpellDamage(enemy, SpellSlot.E) + Player.GetSpellDamage(enemy, SpellSlot.Q) - 20) > enemy.Health
                    && _hamEcd == 0 && _hamQcd == 0 && Player.Distance(enemy.ServerPosition) <= Q2.Range + enemy.BoundingRadius)
                {
                    if (!HammerTime && R.IsReady())
                        R.Cast();

                    if (HammerTime && Q2.IsReady() && E2.IsReady())
                    {
                        Q2.Cast(enemy);
                        E2.Cast(enemy);
                        return;
                    }
                }

                //Hammer Q
                if ((Player.GetSpellDamage(enemy, SpellSlot.Q) - 20) > enemy.Health && _hamQcd == 0 && Player.Distance(enemy.ServerPosition) <= Q2.Range + enemy.BoundingRadius)
                {
                    if (!HammerTime && R.IsReady())
                        R.Cast();

                    if (HammerTime && Q2.IsReady())
                    {
                        Q2.Cast(enemy);
                        return;
                    }
                }

                //Hammer E
                if ((Player.GetSpellDamage(enemy, SpellSlot.E) - 20) > enemy.Health && _hamEcd == 0 && Player.Distance(enemy.ServerPosition) <= E2.Range + enemy.BoundingRadius)
                {
                    if (!HammerTime && R.IsReady() && enemy.Health > 80)
                        R.Cast();

                    if (HammerTime && E2.IsReady())
                    {
                        E2.Cast(enemy);
                        return;
                    }
                }
            }
        }

        private static void SwitchFormCheck(AIHeroClient target, bool useQ, bool useW, bool useQ2, bool useW2, bool useE2)
        {
            if (target == null)
                return;

            if (target.Health > 80)
            {
                //switch to hammer
                if ((CanQcd != 0 || !useQ) &&
                    (_canWcd != 0 && !HyperCharged() || !useW) && R.IsReady() &&
                     HammerAllReady() && !HammerTime && Player.Distance(target.ServerPosition) < 650 &&
                     (useQ2 || useW2 || useE2))
                {
                    //Chat.Print("Hammer Time");
                    R.Cast();
                    return;
                }
            }

            //switch to cannon
            if (((CanQcd == 0 && useQ) || (_canWcd == 0 && useW) && R.IsReady())
                && HammerTime)
            {
                //Chat.Print("Cannon Time");
                R.Cast();
                return;
            }

            if (_hamQcd != 0 && _hamWcd != 0 && _hamEcd != 0 && HammerTime && R.IsReady())
            {
                R.Cast();
            }
        }

        private static bool HyperCharged()
        {
            return Player.Buffs.Any(buffs => buffs.Name == "jaycehypercharge");
        }

        private static bool HammerAllReady()
        {
            if (_hamQcd == 0 && _hamWcd == 0 && _hamEcd == 0)
            {
                return true;
            }
            return false;
        }

        private static void CastQCannon(AIHeroClient target, bool useE, string source)
        {
            var gateDis = misc["gatePlace"].Cast<Slider>().CurrentValue;

            var tarPred = QExtend.GetPrediction(target);

            if (tarPred.HitChance >= HitChance.High && CanQcd == 0 && CanEcd == 0 && useE)
            {
                var gateVector = Player.Position + Vector3.Normalize(target.ServerPosition - Player.Position) * gateDis;

                if (Player.Distance(tarPred.CastPosition) < QExtend.Range + 100)
                {
                    if (E.IsReady() && QExtend.IsReady())
                    {
                        E.Cast(gateVector);
                        QExtend.Cast(tarPred.CastPosition);
                        return;
                    }
                }
            }

            if ((misc["UseQAlways"].Cast<CheckBox>().CurrentValue || !useE) && CanQcd == 0 && Q.GetPrediction(target).HitChance >= HitChance.High && Player.Distance(target.ServerPosition) <= Q.Range && Q.IsReady())
            {
                Q.Cast(target);
            }

        }

        public static float CanQcd;
        private static float _canWcd;
        public static float CanEcd;
        private static float _hamQcd;

        private static float _hamWcd;

        private static float _hamEcd;

        private static float _canQcdRem;

        private static float _canWcdRem;

        private static float _canEcdRem;

        private static float _hamQcdRem;

        private static float _hamWcdRem;

        private static float _hamEcdRem;

        private static void ProcessCooldowns()
        {
            CanQcd = ((_canQcdRem - Game.Time) > 0) ? (_canQcdRem - Game.Time) : 0;
            _canWcd = ((_canWcdRem - Game.Time) > 0) ? (_canWcdRem - Game.Time) : 0;
            CanEcd = ((_canEcdRem - Game.Time) > 0) ? (_canEcdRem - Game.Time) : 0;
            _hamQcd = ((_hamQcdRem - Game.Time) > 0) ? (_hamQcdRem - Game.Time) : 0;
            _hamWcd = ((_hamWcdRem - Game.Time) > 0) ? (_hamWcdRem - Game.Time) : 0;
            _hamEcd = ((_hamEcdRem - Game.Time) > 0) ? (_hamEcdRem - Game.Time) : 0;
        }

        private static float CalculateCd(float time)
        {
            return time + (time * Player.PercentCooldownMod);
        }

        private static void GetCooldowns(GameObjectProcessSpellCastEventArgs spell)
        {
            if (HammerTime)
            {
                if (spell.SData.Name == "JayceToTheSkies")
                    _hamQcdRem = Game.Time + CalculateCd(_hammerQcd[Q.Level - 1]);
                if (spell.SData.Name == "JayceStaticField")
                    _hamWcdRem = Game.Time + CalculateCd(_hammerWcd[W.Level - 1]);
                if (spell.SData.Name == "JayceThunderingBlow")
                    _hamEcdRem = Game.Time + CalculateCd(_hammerEcd[E.Level - 1]);
            }
            else
            {
                if (spell.SData.Name == "jayceshockblast")
                    _canQcdRem = Game.Time + CalculateCd(_cannonQcd[Q.Level - 1]);
                if (spell.SData.Name == "jaycehypercharge")
                    _canWcdRem = Game.Time + CalculateCd(_cannonWcd[W.Level - 1]);
                if (spell.SData.Name == "jayceaccelerationgate")
                    _canEcdRem = Game.Time + CalculateCd(_cannonEcd[E.Level - 1]);
            }
        }

        static void Game_OnUpdate(EventArgs args)
        {
            //cd check
            ProcessCooldowns();

            //Check form
            HammerTime = !_qdata.Name.Contains("jayceshockblast");

            //ks check
            if (misc["smartKS"].Cast<CheckBox>().CurrentValue)
                KsCheck();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (draw["drawcds"].Cast<CheckBox>().CurrentValue)
            {
                var wts = Drawing.WorldToScreen(Player.ServerPosition);
                if (HammerTime)
                {

                    if (CanQcd == 0)
                        Drawing.DrawText(wts[0] - 80, wts[1], Color.White, "Q Ready");
                    else
                        Drawing.DrawText(wts[0] - 80, wts[1], Color.Orange, "Q: " + CanQcd.ToString("0.0"));
                    if (_canWcd == 0)
                        Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.White, "W Ready");
                    else
                        Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.Orange, "W: " + _canWcd.ToString("0.0"));
                    if (CanEcd == 0)
                        Drawing.DrawText(wts[0], wts[1], Color.White, "E Ready");
                    else
                        Drawing.DrawText(wts[0], wts[1], Color.Orange, "E: " + CanEcd.ToString("0.0"));

                }
                else
                {
                    if (_hamQcd == 0)
                        Drawing.DrawText(wts[0] - 80, wts[1], Color.White, "Q Ready");
                    else
                        Drawing.DrawText(wts[0] - 80, wts[1], Color.Orange, "Q: " + _hamQcd.ToString("0.0"));
                    if (_hamWcd == 0)
                        Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.White, "W Ready");
                    else
                        Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.Orange, "W: " + _hamWcd.ToString("0.0"));
                    if (_hamEcd == 0)
                        Drawing.DrawText(wts[0], wts[1], Color.White, "E Ready");
                    else
                        Drawing.DrawText(wts[0], wts[1], Color.Orange, "E: " + _hamEcd.ToString("0.0"));
                }
            }
        }

        static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_SpellMissile))
                return;

            var spell = (Obj_SpellMissile)sender;
            var unit = spell.SpellCaster.Name;
            var name = spell.SData.Name;

            if (unit == null)
                return;

            if (unit == Player.Name && name == "JayceShockBlastMis")
            {
                if (misc["forceGate"].Cast<CheckBox>().CurrentValue && CanEcd == 0 && E.IsReady())
                {
                    var vec = spell.Position - Vector3.Normalize(Player.ServerPosition - spell.Position) * 100;
                    E.Cast(vec);
                }
            }
        }

        static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs attack)
        {
            if (unit.IsMe)
            {
                GetCooldowns(attack);
            }
        }

        static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (!misc["UseGap"].Cast<CheckBox>().CurrentValue) return;

            if (_hamEcd == 0 && e.Sender.IsValidTarget(E2.Range + e.Sender.BoundingRadius))
            {
                if (!HammerTime && R.IsReady())
                    R.Cast();

                if (E2.IsReady())
                    E2.Cast(e.Sender);
            }
        }

        static void Interrupter_OnInterruptableSpell(Obj_AI_Base unit, Interrupter.InterruptableSpellEventArgs e)
        {
            if (!misc["UseInt"].Cast<CheckBox>().CurrentValue) return;

            if (unit != null && Player.Distance(unit.Position) < Q2.Range + unit.BoundingRadius && _hamQcd == 0 && _hamEcd == 0)
            {
                if (!HammerTime && R.IsReady())
                    R.Cast();

                if (Q2.IsReady())
                    Q2.Cast(unit);
            }

            if (unit != null && (Player.Distance(unit.Position) < E2.Range + unit.BoundingRadius && _hamEcd == 0))
            {
                if (!HammerTime && R.IsReady())
                    R.Cast();

                if (E2.IsReady())
                    E2.Cast(unit);
            }
        }

        
    }
}
