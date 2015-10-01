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

        public static Menu main, configs, harass,  misc;


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
            LogicQ();
            LogicW();
            LogicE();
            LogicR();
            

            


        }

        private static double GetRDmg(AIHeroClient target)
        {
            var shot = (int)(7.5 + new[] { 7.5, 7.5, 9, 10.5 }[R.Level] * 1 / Player.AttackDelay);
            var maxShot = new[] { 26, 26, 30, 33 }[R.Level];
            return Player.CalculateDamageOnUnit(
                target, DamageType.Physical, (float)
                (new[] { 40, 40, 50, 60 }[R.Level] + 0.25 * Player.FlatPhysicalDamageMod + (float)
                 0.1 * Player.FlatMagicDamageMod) * (shot > maxShot ? maxShot : shot));
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
            var rDmg = GetRDmg(t);
            if (rDmg >= t.Health * 0.7 && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                if (Item.CanUseItem((int)ItemId.Youmuus_Ghostblade))
                {
                    Item.UseItem((int)ItemId.Youmuus_Ghostblade);
                }
            }

            if (configs["autoR"].Cast<CheckBox>().CurrentValue && R.IsReady() && t.IsValidTarget(R.Range) && !Player.HasBuff("LucianR") && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && R.GetPrediction(t).HitChance >= HitChance.Medium)
            {
                Chat.Print(rDmg);
                if (rDmg >= t.Health * 0.7)
                {
                    if (Item.CanUseItem((int)ItemId.Youmuus_Ghostblade))
                    {
                        Item.UseItem((int)ItemId.Youmuus_Ghostblade);
                    }
                    R.Cast(t);
                    
                }
                return;
            }
        }

        public static bool IsWall(Vector2 vector)
        {
            return NavMesh.GetCollisionFlags(vector.X, vector.Y).HasFlag(CollisionFlags.Wall);
        }

        private static void LogicE()
        {

            var target = TargetSelector.GetTarget(Player.GetAutoAttackRange() - 30, DamageType.Physical);
            if (configs["autoE"].Cast<CheckBox>().CurrentValue && target.IsValidTarget(270) && target.IsMelee && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                E.Cast((Vector3)Player.ServerPosition.Extend(target.ServerPosition, -E.Range));

            else if (configs["autoE"].Cast<CheckBox>().CurrentValue &&  Player.IsInAutoAttackRange(target) && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                E.Cast((Vector3)Player.ServerPosition.Extend(target.ServerPosition, E.Range));
            }
        }

        static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (target.IsMe)
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