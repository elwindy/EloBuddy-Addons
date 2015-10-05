using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;
using SpellData = TrustViktorPorted.DamageIndicator.SpellData;

namespace TrustViktorPorted
{
    using EloBuddy.SDK.Enumerations;
    using EloBuddy.SDK.Events;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;
    using EloBuddy.SDK.Rendering;

    static class Program
    {

        public static DamageIndicator.DamageIndicator Indicator;
        public const string CHAMP_NAME = "Viktor";
        private static readonly AIHeroClient player = ObjectManager.Player;

        // Spells
        public static Spell.Targeted Q, Ignite;
        public static Spell.Skillshot W, E, R;
        private static readonly int maxRangeE = 1225;
        private static readonly int lengthE = 700;
        private static readonly int speedE = 1200;
        private static readonly int rangeE = 525;
        private static int lasttick = 0;
        private static Vector3 GapCloserPos;
        private static Menu main, combo, harass, wave, lasthit, misc, draw;
        private static bool AttacksEnabled
        {
            get
            {
                if ((Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) || (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)))
                    return ((!Q.IsReady() || player.Mana < Q.Handle.SData.Mana) && (!E.IsReady() || player.Mana < E.Handle.SData.Mana));

                return true;
            }
        }
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (player.ChampionName != CHAMP_NAME)
                return;

            Q = new Spell.Targeted(SpellSlot.Q, 600);
            Q.CastDelay = (int)0.25f;
            W = new Spell.Skillshot(SpellSlot.W, 700, SkillShotType.Circular, (int)0.5f, int.MaxValue, 300);
            E = new Spell.Skillshot(SpellSlot.E, (uint)rangeE, SkillShotType.Linear, 0, speedE, 80);
            R = new Spell.Skillshot(SpellSlot.R, 700, SkillShotType.Circular, (int)0.25f, int.MaxValue, (int)450f);
            Ignite = new Spell.Targeted(player.GetSpellSlotFromName("summonerdot"), 600);
            

            main = MainMenu.AddMenu("TRUSt in my" + CHAMP_NAME, CHAMP_NAME);
            combo = main.AddSubMenu("Combo", "combo");
            combo.AddGroupLabel("Combo");
            combo.Add("comboUseQ", new CheckBox("Use Q"));
            combo.Add("comboUseW", new CheckBox("Use W"));
            combo.Add("comboUseE", new CheckBox("Use E"));
            combo.Add("comboUseR", new CheckBox("Use R"));
            combo.AddSeparator();
            combo.Add("HitR", new Slider("Ultimate to hit",3,1,5));
            combo.AddSeparator();
            combo.Add("rLastHit", new CheckBox("1 target ulti"));
            combo.Add("AutoFollowR", new CheckBox("Auto Follow R"));
            combo.AddSeparator();
            combo.Add("rTicks", new Slider("Ultimate ticks to count",2,1,14));
            combo.AddSeparator();
            combo.Add("spPriority", new CheckBox("Prioritize kill over dmg"));

            harass = main.AddSubMenu("Harass", "harass");
            harass.AddGroupLabel("Harass");
            harass.Add("harassUseQ", new CheckBox("Use Q"));
            harass.Add("harassUseE", new CheckBox("Use W"));
            harass.AddSeparator();
            harass.Add("harassMana", new Slider("Mana usage in percent (%{0})", 30));

            wave = main.AddSubMenu("WaveClear", "wave");
            wave.AddGroupLabel("WaveClear");
            wave.Add("waveUseQ", new CheckBox("Use Q"));
            wave.Add("waveUseE", new CheckBox("Use E"));
            wave.AddSeparator();
            wave.Add("waveMana", new Slider("Mana usage in percent (%{0})", 30));

            lasthit = main.AddSubMenu("LastHit", "last");
            lasthit.AddGroupLabel("LastHit");
            lasthit.Add("waveUseQLH", new CheckBox("Use Q"));

            misc = main.AddSubMenu("Misc", "misc");
            misc.AddGroupLabel("Misc");
            misc.Add("rInterrupt", new CheckBox("Use R to interrupt dangerous spells"));
            misc.Add("wInterrupt", new CheckBox("Use W to interrupt dangerous spells"));
            misc.Add("autoW", new CheckBox("Use W to continue CC"));
            misc.Add("miscGapcloser", new CheckBox("Use W against gapclosers"));

            draw = main.AddSubMenu("Draw", "draw");
            draw.AddGroupLabel("Draw");
            draw.Add("drawRangeQ", new CheckBox("Q range", false));
            draw.Add("drawRangeW", new CheckBox("W range", false));
            draw.Add("drawRangeE", new CheckBox("E range"));
            draw.Add("drawRangeEMax", new CheckBox("E max range"));
            draw.Add("drawRangeR", new CheckBox("R range"));

            Indicator = new DamageIndicator.DamageIndicator();
            Indicator.Add("Combo", new SpellData(0, DamageType.True, System.Drawing.Color.Aqua)); 

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Orbwalker.OnUnkillableMinion += Orbwalker_OnUnkillableMinion;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            Chat.Print("TrustViktorPorted loaded!", Color.Indigo);
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (draw["drawRangeQ"].Cast<CheckBox>().CurrentValue && Q.IsReady())
            {
                Circle.Draw(new ColorBGRA(Color.IndianRed.ToBgra()), Q.Range, player.Position);
            }
            if (draw["drawRangeW"].Cast<CheckBox>().CurrentValue && W.IsReady())
            {
                Circle.Draw(new ColorBGRA(Color.IndianRed.ToBgra()), W.Range, player.Position);
            }
            if (draw["drawRangeE"].Cast<CheckBox>().CurrentValue && E.IsReady())
            {
                Circle.Draw(new ColorBGRA(Color.DarkRed.ToBgra()), E.Range, player.Position);
            }
            if (draw["drawRangeEMax"].Cast<CheckBox>().CurrentValue && E.IsReady())
            {
                Circle.Draw(new ColorBGRA(Color.OrangeRed.ToBgra()), maxRangeE, player.Position);
            }
            if (draw["drawRangeR"].Cast<CheckBox>().CurrentValue && R.IsReady())
            {
                Circle.Draw(new ColorBGRA(Color.Red.ToBgra()), R.Range, player.Position);
            }
        }

        static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;

            if (args.SData.Name.ToLower().Contains("viktorpowertransferreturn"))
                Core.DelayAction(Orbwalker.ResetAutoAttack, 250);
        }

        static void Game_OnUpdate(EventArgs args)
        {
            var target = TargetSelector.GetTarget(maxRangeE, DamageType.Magical);
            Indicator.Update("Combo", new SpellData((int)GetComboDamage(target), DamageType.Magical, System.Drawing.Color.Aqua));
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                OnCombo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                OnHarass();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                OnWaveClear();
            }

            // Ultimate follow
            if (R.Handle.Name != "ViktorChaosStorm" && combo["AutoFollowR"].Cast<CheckBox>().CurrentValue && Environment.TickCount - lasttick > 0)
            {
                var stormT = TargetSelector.GetTarget(1100, DamageType.Magical);
                if (stormT != null)
                {
                    R.Cast(stormT.ServerPosition);
                    lasttick = Environment.TickCount + 500;
                }
            }

            AutoW();

        }

        private static void OnWaveClear()
        {
            // Mana check
            if ((player.Mana / player.MaxMana) * 100 < wave["waveMana"].Cast<Slider>().CurrentValue)
                return;

            bool useQ = wave["waveUseQ"].Cast<CheckBox>().CurrentValue && Q.IsReady();
            bool useE = wave["waveUseE"].Cast<CheckBox>().CurrentValue && E.IsReady();

            if (useQ)
            {
                foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(a => a.IsEnemy).Where(a => !a.IsDead).Where(a => a.Distance(player) < player.AttackRange))
                {
                    if (Damage.QDamage(minion) >= minion.Health && minion.BaseSkinName.Contains("Siege"))
                    {
                        QLastHit(minion);
                        break;
                    }
                }
            }

            if (useE)
            {
                var firstMinion = ObjectManager.Get<Obj_AI_Minion>().Where(a => a.IsEnemy).Where(a => !a.IsDead).Where(a => a.Distance(player) < maxRangeE).FirstOrDefault();
                var lasttMinion = ObjectManager.Get<Obj_AI_Minion>().Where(a => a.IsEnemy).Where(a => !a.IsDead).Where(a => a.Distance(player) < E.Range).LastOrDefault();
                if (firstMinion != null && lasttMinion != null)
                {
                    CastE(firstMinion.Position, lasttMinion.Position);
                }
            }
        }

        private static void OnHarass()
        {
            // Mana check
            if ((player.Mana / player.MaxMana) * 100 < harass["harassMana"].Cast<Slider>().CurrentValue)
                return;
            bool useE = harass["harassUseE"].Cast<CheckBox>().CurrentValue && E.IsReady();
            bool useQ = harass["harassUseQ"].Cast<CheckBox>().CurrentValue && Q.IsReady();
            if (useQ)
            {
                var qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (qtarget != null)
                    Q.Cast(qtarget);
            }
            if (useE)
            {
                var target = TargetSelector.GetTarget(maxRangeE, DamageType.Magical);

                if (target != null)
                    PredictCastE(target);
            }
        }

        public static void CastE(Vector3 pos1, Vector3 pos2)
        {
            Player.CastSpell(SpellSlot.E, (Vector3)pos1, pos2);
        }

        private static float TotalDmg(Obj_AI_Base enemy, bool useQ, bool useE, bool useR, bool qRange)
        {
            var qaaDmg = new Double[] { 20, 25, 30, 35, 40, 45, 50, 55, 60, 70, 80, 90, 110, 130, 150, 170, 190, 210 };
            var damage = 0d;
            var rTicks = combo["rTicks"].Cast<Slider>().CurrentValue;
            bool inQRange = ((qRange && player.IsInAutoAttackRange(enemy)) || qRange == false);
            //Base Q damage
            if (useQ && Q.IsReady() && inQRange)
            {
                damage += Damage.QDamage(enemy);
                damage += player.CalculateDamageOnUnit(enemy, DamageType.Magical, (float)(qaaDmg[player.Level >= 18 ? 18 - 1 : player.Level - 1] + (player.TotalMagicalDamage * .5) + player.TotalAttackDamage));
            }

            // Q damage on AA
            if (useQ && !Q.IsReady() && player.HasBuff("viktorpowertransferreturn") && inQRange)
            {
                damage += player.CalculateDamageOnUnit(enemy, DamageType.Magical,
                    (float)(qaaDmg[player.Level >= 18 ? 18 - 1 : player.Level - 1] +
                            (player.TotalMagicalDamage * .5) + player.TotalAttackDamage));
            }

            //E damage
            if (useE && E.IsReady())
            {
                if (player.HasBuff("viktoreaug") || player.HasBuff("viktorqeaug") || player.HasBuff("viktorqweaug"))
                {
                    damage += Damage.EDamage1(enemy);
                }
                else
                {
                    damage += Damage.EDamage(enemy);
                }
            }

            //R damage + 2 ticks
            if (useR && R.Level > 0 && R.IsReady() && R.Handle.Name == "ViktorChaosStorm")
            {
                damage += Damage.RDamage1(enemy) * rTicks;
                damage += Damage.RDamage(enemy);
            }

            // Ludens Echo damage
            if (Item.HasItem(3285))
                damage += player.CalculateDamageOnUnit(enemy, DamageType.Magical, (float)(100 + player.FlatMagicDamageMod * 0.1));

            //sheen damage
            if (Item.HasItem(3057))
                damage += player.CalculateDamageOnUnit(enemy, DamageType.Physical, (float)(0.5 * player.BaseAttackDamage));

            //lich bane dmg
            if (Item.HasItem(3100))
                damage += player.CalculateDamageOnUnit(enemy, DamageType.Magical, (float)(0.5 * player.FlatMagicDamageMod + 0.75 * player.BaseAttackDamage));

            return (float)damage;
        }
        private static float GetComboDamage(Obj_AI_Base enemy)
        {

            return TotalDmg(enemy, true, true, true, false);
        }

        private static void AutoW()
        {
            if (!W.IsReady() || !misc["autoW"].Cast<CheckBox>().CurrentValue)
                return;

            var tPanth = HeroManager.Enemies.Find(h => h.IsValidTarget(W.Range) && h.HasBuff("Pantheon_GrandSkyfall_Jump"));
            if (tPanth != null)
            {
                W.Cast(tPanth);
                    return;
            }

            foreach (var enemy in HeroManager.Enemies.Where(h => h.IsValidTarget(W.Range)))
            {
                if (enemy.HasBuff("rocketgrab2"))
                {
                    var t = HeroManager.Allies.Find(h => h.BaseSkinName.ToLower() == "blitzcrank" && h.Distance((AttackableUnit)player) < W.Range);
                    if (t != null)
                    {
                        W.Cast(enemy);
                            return;
                    }
                }
                if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                         enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                         enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Suppression) ||
                         enemy.IsRecalling())
                {
                    W.Cast(enemy);
                        return;
                }
                if (W.GetPrediction(enemy).HitChance == HitChance.Immobile)
                {
                    W.Cast(enemy);
                        return;
                }
            }
        }

        private static void OnCombo()
        {
            bool useQ = combo["comboUseQ"].Cast<CheckBox>().CurrentValue && Q.IsReady();
            bool useW = combo["comboUseW"].Cast<CheckBox>().CurrentValue && W.IsReady();
            bool useE = combo["comboUseE"].Cast<CheckBox>().CurrentValue && E.IsReady();
            bool useR = combo["comboUseR"].Cast<CheckBox>().CurrentValue && R.IsReady();
            bool killpriority = combo["spPriority"].Cast<CheckBox>().CurrentValue && R.IsReady();
            bool rKillSteal = combo["rLastHit"].Cast<CheckBox>().CurrentValue;
            var Etarget = TargetSelector.GetTarget(maxRangeE, DamageType.Magical);
            var Qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var RTarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            if ((Qtarget.Health <= 50 + 20 * player.Level - (Qtarget.HPRegenRate / 5 * 3)) || (R.IsInRange(Qtarget) &&R.IsReady() && Damage.RDamage(Qtarget) + 50 + 20 * player.Level - (Qtarget.HPRegenRate / 5 * 3) >= Qtarget.Health))
            {
                Ignite.Cast(Qtarget);
            }
            if (killpriority && Qtarget != null & Etarget != null && Etarget != Qtarget && ((Etarget.Health > TotalDmg(Etarget, false, true, false, false)) || (Etarget.Health > TotalDmg(Etarget, false, true, true, false) && Etarget == RTarget)) && Qtarget.Health < TotalDmg(Qtarget, true, true, false, false))
            {
                Etarget = Qtarget;
            }

            if (RTarget != null && rKillSteal && useR)
            {
                if (TotalDmg(RTarget, true, true, false, false) < RTarget.Health && TotalDmg(RTarget, true, true, true, true) > RTarget.Health)
                {
                    R.Cast(RTarget.ServerPosition);
                }
            }


            if (useE)
            {
                if (Etarget != null)
                    PredictCastE(Etarget);
            }
            if (useQ)
            {

                if (Qtarget != null)
                    Q.Cast(Qtarget);
            }
            if (useW)
            {
                var t = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (t != null)
                {
                    if (t.Path.Count() < 2)
                    {
                        if (t.HasBuffOfType(BuffType.Slow))
                        {
                            if (W.GetPrediction(t).HitChance >= HitChance.High)
                            {
                                W.Cast(t);
                                return;
                            }
                        }
                        if (t.CountEnemiesInRange(250) > 2)
                        {
                            if (W.GetPrediction(t).HitChance >= HitChance.High)
                            {
                                W.Cast(t);
                                    return;
                            }
                        }
                        if (player.Position.Distance(t.ServerPosition) < player.Position.Distance(t.Position))
                        {
                            W.Cast(t);
                                return;
                        }
                        else
                        {
                            W.Cast(t);
                                return;
                        }
                    }
                }
            }
            if (useR && R.Handle.Name == "ViktorChaosStorm" && player.CanCast && !player.Spellbook.IsCastingSpell)
            {

                foreach (var unit in HeroManager.Enemies.Where(h => h.IsValidTarget(R.Range) && h.CountEnemiesInRange(R.Range) >= combo["HitR"].Cast<Slider>().CurrentValue))
                {
                    R.Cast(unit);

                }
            }

        }

        private static void PredictCastE(AIHeroClient target)
        {
            if (target.Distance(player) > maxRangeE) return;

            var posInicial = target.Position;
            if (player.CountEnemiesInRange(E.Range) >= 2)
            {
                var firstTarget = ObjectManager.Get<AIHeroClient>().Where(a => a.IsEnemy).Where(a => !a.IsDead).Where(a => a.Distance(player) < maxRangeE).FirstOrDefault();
                var lasttTarget = ObjectManager.Get<AIHeroClient>().Where(a => a.IsEnemy).Where(a => !a.IsDead).Where(a => a.Distance(player) < E.Range).LastOrDefault();
                if (firstTarget != null && lasttTarget != null)
                {
                    CastE(firstTarget.Position, lasttTarget.Position);
                }
            }
            else if (target.Distance(player) < maxRangeE)
            {
                var maxPosition = target.Position.Extend(player, player.Distance(target) - rangeE);

                var pred = new PredictionResult(maxPosition.To3D(), target.Position, 70, null, Int32.MaxValue);

                if (pred.HitChance >= HitChance.Medium)
                    CastE(target.Position, maxPosition.Extend(player.Position.To2D(), 30).To3D());

            }
            else if (E.IsInRange(target))
            {
                posInicial = posInicial.Extend(target.Position, 100);

                var pred = E.GetPrediction(target);

                if (pred.HitChance == HitChance.High)
                {
                    CastE(posInicial, pred.UnitPosition);
                }
            }
        }

        static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (target.Type == GameObjectType.AIHeroClient)
            {
                args.Process = AttacksEnabled;
            }
            else
                args.Process = true;
        }

        static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (e.DangerLevel >= DangerLevel.High)
            {
                var useW = misc["wInterrupt"].Cast<CheckBox>().CurrentValue;
                var useR = misc["rInterrupt"].Cast<CheckBox>().CurrentValue;

                if (useW && W.IsReady() && sender.IsValidTarget(W.Range) &&
                    (Game.Time + 1.5 + W.CastDelay) >= e.EndTime)
                {
                    W.Cast(sender);
                    return;
                }
                else if (useR && sender.IsValidTarget(R.Range) && R.Handle.Name == "ViktorChaosStorm")
                {
                    R.Cast(sender);
                }
            }
        }

        static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (misc["miscGapcloser"].Cast<CheckBox>().CurrentValue && W.IsInRange(e.End))
            {
                GapCloserPos = e.End;
                if (Distance((Vector2)e.Start, (Vector2)e.End)
                    > e.Sender.Spellbook.GetSpell(e.Slot).SData.CastRangeDisplayOverride
                    && e.Sender.Spellbook.GetSpell(e.Slot).SData.CastRangeDisplayOverride > 100)
                {
                    GapCloserPos = Extend(e.Start, e.End, e.Sender.Spellbook.GetSpell(e.Slot).SData.CastRangeDisplayOverride);

                }
                W.Cast(GapCloserPos);
            }
        }
        
        public static float Distance(this Vector2 v, Vector2 to, bool squared = false)
        {
            return squared ? Vector2.DistanceSquared(v, to) : Vector2.Distance(v, to);
        }

        public static float Distance(this Obj_AI_Base unit, AttackableUnit anotherUnit, bool squared = false)
        {
            return unit.ServerPosition.To2D().Distance(anotherUnit.Position.To2D(), squared);
        }

        public static Vector3 Extend(this Vector3 v, Vector3 to, float distance)
        {
            return v + distance * (to - v).Normalized();
        }

        static void Orbwalker_OnUnkillableMinion(Obj_AI_Base target, Orbwalker.UnkillableMinionArgs args)
        {
            QLastHit(target);
        }

        private static void QLastHit(Obj_AI_Base minion)
        {
            bool castQ = ((lasthit["waveUseQLH"].Cast<CheckBox>().CurrentValue) || wave["waveUseQ"].Cast<CheckBox>().CurrentValue && (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit)));
            if (castQ)
            {
                var distance = Distance(player, minion);
                var t = 250 + (int)distance / 2;
                var predHealth = Prediction.Health.GetPrediction(minion, t);
                Console.WriteLine(" Distance: " + distance + " timer : " + t + " health: " + predHealth);
                if (predHealth > 0)
                {
                    Q.Cast(minion);
                }
            }
        }

        private static bool KillableWithAA(Obj_AI_Base target)
        {
            var qaaDmg = new Double[] { 20, 25, 30, 35, 40, 45, 50, 55, 60, 70, 80, 90, 110, 130, 150, 170, 190, 210 };
            if (player.HasBuff("viktorpowertransferreturn") && Orbwalker.CanAutoAttack && (player.CalculateDamageOnUnit(target, DamageType.Magical,
                    (float)(qaaDmg[player.Level >= 18 ? 18 - 1 : player.Level - 1] +
                            (player.TotalMagicalDamage * .5) + player.TotalAttackDamage)) > target.Health))
            {
                Console.WriteLine("killable with aa");
                return true;
            }
            else
                return false;
        }
    }
}
