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

namespace LucianBuddy
{
    

    class Program
    {
        public static AIHeroClient Player { get { return ObjectManager.Player; } }

        public static Spell.Targeted Q;

        public static Spell.Skillshot Q1, W, E, R, R1;

        private static float QMANA = 0;

        private static float WMANA = 0;

        private static float EMANA = 0;

        private static float RMANA = 0;

        private static bool passRdy = false;
        private static float castR = Game.Time;

        public static Menu main, configs, harass, farm, misc;


        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.ChampionName != "Lucian")
            {
                return;
            }

            Q = new Spell.Targeted(SpellSlot.Q, 675);
            Q1 = new Spell.Skillshot(SpellSlot.Q, 1100, SkillShotType.Linear, (int)0.35f, int.MaxValue, (int)25f);
            W = new Spell.Skillshot(SpellSlot.W, 1200, SkillShotType.Linear, (int)0.3f, 1600, 80);
            E = new Spell.Skillshot(SpellSlot.E, 475, SkillShotType.Linear, (int).25f, int.MaxValue, (int)1f);
            R = new Spell.Skillshot(SpellSlot.R, 1400, SkillShotType.Linear, (int).1f, 2800, 110);
            R1 = new Spell.Skillshot(SpellSlot.R, 1400, SkillShotType.Linear, (int).1f, 2800, 110);

            main = MainMenu.AddMenu("LucıanBuddy", "lucianbuddy");
            configs = main.AddSubMenu("Configs", "configs");
            configs.AddGroupLabel("Configs");
            configs.AddLabel("Q Config");
            configs.Add("autoQ", new CheckBox("Auto Q"));
            configs.Add("harasQ", new CheckBox("Use Q on minion"));
            configs.AddSeparator(15);
            configs.AddLabel("W Config");
            configs.Add("autoW", new CheckBox("Auto W"));
            configs.Add("ignoreCol", new CheckBox("Ignore collision"));
            configs.AddSeparator(15);
            configs.AddLabel("E Config");
            configs.Add("autoE", new CheckBox("Auto E"));
            configs.Add("nktdE", new CheckBox("NoKeyToDash"));
            configs.Add("slowE", new CheckBox("Auto SlowBuff E"));
            configs.AddSeparator(15);
            configs.AddLabel("R Config");
            configs.Add("autoR", new CheckBox("Auto R"));
            configs.Add("useR", new KeyBind("Semi-manual cast R key", false, KeyBind.BindTypes.HoldActive, 'T'));

            harass = main.AddSubMenu("Harass", "harass");
            harass.AddGroupLabel("Harass Settings");
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.IsEnemy))
            {
                harass.Add("harras" + enemy.ChampionName, new CheckBox(enemy.ChampionName));
            }

            misc = main.AddSubMenu("Misc", "misc");
            misc.AddGroupLabel("Misc Settings");
            var skinslect = misc.Add("skin+", new Slider("Chance Skin", 0, 0, 6));
            ObjectManager.Player.SetSkin(ObjectManager.Player.ChampionName, skinslect.CurrentValue);
            skinslect.OnValueChange += delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
            {
                ObjectManager.Player.SetSkin(ObjectManager.Player.ChampionName, changeArgs.NewValue);
            };

            farm = main.AddSubMenu("Farm", "farm");
            farm.AddGroupLabel("Farm Settings");
            farm.Add("farmQ", new CheckBox("LaneClear + jungle Q"));
            farm.Add("farmW", new CheckBox("LaneClear + jungle W"));
            farm.Add("Mana", new Slider("LaneClear + jungle  Mana", 80));

            Game.OnTick += Game_OnTick;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
        }

        public static void SetMana()
        {
            if (Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Q.Handle.SData.Mana;
            WMANA = W.Handle.SData.Mana;
            EMANA = E.Handle.SData.Mana;

            if (!R.IsReady())
                RMANA = QMANA - Player.PARRegenRate * Q.Handle.Cooldown;
            else
                RMANA = R.Handle.SData.Mana;
        }

        private static bool SpellLock
        {
            get
            {
                if (Player.HasBuff("lucianpassivebuff"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private static double NumShots()
        {
            double num = 7.5;
            if (R.Level == 1)
                num += 7.5 * Player.AttackSpeedMod * 0.5;
            else if (R.Level == 2)
                num += 9 * Player.AttackSpeedMod * 0.5;
            else if (R.Level == 3)
                num += 10.5 * Player.AttackSpeedMod * 0.5;
            return num;
        }

        private static double AaDamage(AIHeroClient target)
        {
            if (Player.Level > 12)
            {
                return Player.GetAutoAttackDamage(target) * 1.3;
            }
            else if (Player.Level > 6)
            {
                return Player.GetAutoAttackDamage(target) * 1.4;
            }
            else if (Player.Level > 0)
            {
                return Player.GetAutoAttackDamage(target) * 1.5;
            }
            return 0;
        }

        static void Game_OnTick(EventArgs args)
        {
            if (R1.IsReady() && Game.Time - castR > 5 && configs["useR"].Cast<KeyBind>().CurrentValue)
            {
                var t = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                if (t.IsValidTarget(R1.Range))
                {
                    R1.Cast(t);
                    return;
                }
            }
            SetMana();
            if (Q.IsReady() && !passRdy && !SpellLock)
            {
                LogicQ();
            }
            if (W.IsReady() && !passRdy && !SpellLock && configs["autoW"].Cast<CheckBox>().CurrentValue)
            {
                LogicW();
            }
            if (E.IsReady())
            {
                LogicE();
            }
            if (R.IsReady() && Game.Time - castR > 5 && configs["autoR"].Cast<CheckBox>().CurrentValue)
            {
                LogicR();
            }

            if (!passRdy && !SpellLock)
            {
                farmmode();
            }
            

        }

        private static void farmmode()
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                var mobs = EntityManager.GetJungleMonsters(Player.Position.To2D(), Q.Range);
                if (mobs.Count > 0 && Player.Mana > RMANA + WMANA + EMANA + QMANA)
                {
                    var mob = mobs[0];
                    if (Q.IsReady() && farm["farmQ"].Cast<CheckBox>().CurrentValue)
                    {
                        Q.Cast(mob);
                        return;
                    }

                    if (W.IsReady() && farm["farmW"].Cast<CheckBox>().CurrentValue)
                    {
                        W.Cast(mob);
                        return;
                    }
                }

                if (Player.ManaPercent > farm["Mana"].Cast<Slider>().CurrentValue)
                {
                    var minions = EntityManager.GetLaneMinions(
                        EntityManager.UnitTeam.Enemy,
                        Player.Position.To2D(),
                        Q1.Range);
                    if (Q.IsReady() && farm["farmQ"].Cast<CheckBox>().CurrentValue)
                    {
                        foreach (var minion in minions)
                        {
                            var poutput = Q1.GetPrediction(minion);
                            var col = poutput.CollisionObjects;

                            if (col.Count() > 2)
                            {
                                var minionQ = col.First();
                                if (minionQ.IsValidTarget(Q.Range))
                                {
                                    Q.Cast(minion);
                                    return;
                                }
                            }
                        }
                    }
                    if (W.IsReady() && farm["farmW"].Cast<CheckBox>().CurrentValue)
                    {
                        var Wminions = EntityManager.GetLaneMinions(
                        EntityManager.UnitTeam.Enemy,
                        Player.Position.To2D(),
                        W.Range);
                        foreach (var minion in minions)
                        {
                            var poutput = W.GetPrediction(minion);
                            var col = poutput.CollisionObjects;

                            if (col.Count() > 3)
                            {
                                var minionW = col.First();
                                if (minionW.IsValidTarget(W.Range))
                                {
                                    W.Cast(minion);
                                    return;
                                }
                            }
                        }
                        
                    }
                }
            }
        }

        private static void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            var t1 = TargetSelector.GetTarget(Q1.Range, DamageType.Physical);
            if (t.IsValidTarget(Q.Range))
            {
                if (Player.GetSpellDamage(t, SpellSlot.Q) + AaDamage(t) > t.Health)
                {
                    Q.Cast(t);
                }
                else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && Player.Mana > RMANA + QMANA)
                {
                    Q.Cast(t);
                }
                else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear)
                         && harass["harras" + t.ChampionName].Cast<CheckBox>().CurrentValue
                         && Player.Mana > RMANA + QMANA + EMANA + WMANA)
                {
                    Q.Cast(t);
                }
            }
            else if ((Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) && configs["harasQ"].Cast<CheckBox>().CurrentValue && t1.IsValidTarget(Q1.Range) && harass["harras" + t1.ChampionName].Cast<CheckBox>().CurrentValue && Player.Distance(t1.ServerPosition) > Q.Range + 100)
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && Player.Mana < RMANA + QMANA)
                    return;
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) && Player.Mana < RMANA + QMANA + EMANA + WMANA)
                    return;
                
                var prepos = Q1.GetPrediction(t1);
                if ((int)prepos.HitChance < 3)
                    return;
                var distance = Player.Distance(prepos.CastPosition);
                var minions = EntityManager.GetLaneMinions(
                    EntityManager.UnitTeam.Enemy,
                    Player.ServerPosition.To2D(),
                    Q.Range);

                foreach (var minion in minions.Where(minion => minion.IsValidTarget(Q.Range)))
                {
                    if (prepos.CastPosition.Distance(Player.Position.Extend(minion.Position, distance)) < 25)
                    {
                        Q.Cast(minion);
                        return;
                    }
                }
            }
        }

        private static void LogicW()
        {
            var t = TargetSelector.GetTarget(W.Range, DamageType.Physical);
            if (t.IsValidTarget())
            {
                if (configs["ignoreCol"].Cast<CheckBox>().CurrentValue && Player.IsInAutoAttackRange(t))
                {
                    WCollision = false;
                }
                else
                {
                    WCollision = true;
                }

                var qDmg = Player.GetSpellDamage(t, SpellSlot.Q);
                var wDmg = Player.GetSpellDamage(t, SpellSlot.W);
                if (Player.IsInAutoAttackRange(t))
                {
                    qDmg += (float)AaDamage(t);
                    wDmg += (float)AaDamage(t);
                }
                if (wDmg > t.Health)
                {
                    W.Cast(t);
                }
                else if (wDmg + qDmg > t.Health && Q.IsReady() && Player.Mana > RMANA + WMANA + QMANA)
                {
                    W.Cast(t);
                }
                else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)
                         && Player.Mana > RMANA + WMANA + EMANA + QMANA)
                {
                    W.Cast(t);
                }
                else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear)
                         && harass["harras" + t.ChampionName].Cast<CheckBox>().CurrentValue && Player.Mana > Player.MaxMana * 0.8
                         && Player.Mana > RMANA + WMANA + EMANA + QMANA + WMANA)
                {
                    W.Cast(t);
                }
                else if ((Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear)) && Player.Mana > RMANA + WMANA + EMANA)
                {
                    foreach (var enemy in
                        HeroManager.Enemies.Where(enemy => enemy.IsValidTarget(W.Range)))
                    {
                        W.Cast(enemy);
                    }
                }
            }
        }



        private static void LogicR()
        {
            var t = TargetSelector.GetTarget(R.Range, DamageType.Physical);

            if (t.IsValidTarget(R.Range) && t.CountEnemiesInRange(500) == 0 && !Player.IsInAutoAttackRange(t))
            {
                var rDmg = Player.GetSpellDamage(t, SpellSlot.R) * NumShots();

                var tDis = Player.Distance(t.ServerPosition);
                if (rDmg * 0.8 > t.Health && tDis < 800 && !Q.IsReady())
                    R.Cast(t);
                else if (rDmg * 0.7 > t.Health && tDis < 900)
                    R.Cast(t);
                else if (rDmg * 0.6 > t.Health && tDis < 1000)
                    R.Cast(t);
                else if (rDmg * 0.5 > t.Health && tDis < 1100)
                    R.Cast(t);
                else if (rDmg * 0.4 > t.Health && tDis < 1200)
                    R.Cast(t);
                else if (rDmg * 0.3 > t.Health && tDis < 1300)
                    R.Cast(t);
                return;
            }
        }

        public static bool IsWall(Vector2 vector)
        {
            return NavMesh.GetCollisionFlags(vector.X, vector.Y).HasFlag(CollisionFlags.Wall);
        }

        private static void LogicE()
        {

            var dashPosition = Player.Position.Extend(Game.CursorPos, E.Range);
            if (IsWall(dashPosition) || dashPosition.CountEnemiesInRange(800) > 2)
                return;
            if (Game.CursorPos.Distance(Player.Position) > Player.AttackRange + Player.BoundingRadius * 2 && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && configs["nktdE"].Cast<CheckBox>().CurrentValue && Player.Mana > RMANA + EMANA - 10)
            {
                if (!passRdy && !SpellLock)
                    E.Cast(Game.CursorPos);
                else if (!Orbwalker.GetTarget().IsValidTarget())
                    E.Cast(Game.CursorPos);
            }

            if (Player.Mana < RMANA + EMANA || !configs["autoE"].Cast<CheckBox>().CurrentValue || passRdy || SpellLock)
                return;

            foreach (var target in HeroManager.Enemies.Where(target => target.IsValidTarget(270) && target.IsMelee))
            {
                if (target.Position.Distance(Game.CursorPos) > target.Position.Distance(Player.Position))
                {
                    E.Cast(dashPosition.To3D());
                }
            }

            if (configs["slowE"].Cast<CheckBox>().CurrentValue && Player.HasBuffOfType(BuffType.Slow))
            {
                E.Cast(dashPosition.To3D());
            }
        }

        static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (!target.IsMe)
            {
                return;
            }
        }

        static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "LucianW" || args.SData.Name == "LucianE" || args.SData.Name == "LucianQ")
                {
                    passRdy = true;
                    Core.DelayAction(() => Orbwalker.ResetAutoAttack(), 450);
                }
                else
                {
                    passRdy = false;
                }

                if (args.SData.Name == "LucianR")
                {
                    castR = Game.Time;
                }
            }
        }

        static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.Q || args.Slot == SpellSlot.W || args.Slot == SpellSlot.E)
            {
                passRdy = true;
            }
        }

        static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (E.IsReady() && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(400) < 3)
            {
                var Target = (AIHeroClient)e.Sender;
                if (Target.IsValidTarget(E.Range))
                {
                    E.Cast((Vector3)ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range));
                }
            }
            return;
        }




        public static bool WCollision { get; set; }
    }
}
