using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Rendering;
using EloBuddy.SDK.Utils;
using SharpDX;
using Color = System.Drawing.Color;
using SpellData = Annie___The_Little_Girl.DamageIndicator.SpellData;

namespace Annie___The_Little_Girl
{
    using EloBuddy.SDK.Events;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;

    class Program
    {
        public static DamageIndicator.DamageIndicator Indicator;

        public static AIHeroClient Player { get { return ObjectManager.Player; } }

        public static Spell.Targeted Q;

        public static Spell.Skillshot W, R;

        public static Spell.Active E;

        public static float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;

        public static GameObject Tibbers;

        public static float TibbersTimer = 0;

        public static Menu AllInOne;

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.ChampionName != "Annie")
            {
                return;
            }

            Q = new Spell.Targeted(SpellSlot.Q, (uint)625f);
            W = new Spell.Skillshot(SpellSlot.W, (uint)600f, SkillShotType.Circular, (int)0.50f, 3000, (int)250f);
            E = new Spell.Active(SpellSlot.E);
            R = new Spell.Skillshot(SpellSlot.R, (uint)625f, SkillShotType.Circular, (int)0.20f, int.MaxValue, (int)250f);

            AllInOne = MainMenu.AddMenu("Annie", "Annie");
            AllInOne.AddGroupLabel("Annie - The Little Girl");
            AllInOne.AddLabel("Farm");
            AllInOne.Add("farmQ", new CheckBox("Farm Q"));
            AllInOne.Add("farmW", new CheckBox("Farm W", false));
            AllInOne.Add("Mana", new Slider("LaneClear Mana", 60));
            AllInOne.AddLabel("R Settings");
            AllInOne.Add("rCount", new Slider("Auto R stun x enemies", 3, 0, 5));
            AllInOne.AddLabel("Stun in combo", 15);
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.Team != Player.Team))
            {
                AllInOne.Add("stun" + enemy.ChampionName, new CheckBox(enemy.ChampionName));
            }
            AllInOne.AddLabel("Other Settings");
            AllInOne.Add("autoE", new CheckBox("Auto E stack stun", false));
            AllInOne.Add("sup", new CheckBox("Support mode", false));
            AllInOne.Add("tibers", new CheckBox("TibbersAutoPilot"));
            AllInOne.Add("AACombo", new CheckBox("AA in combo", false));
            AllInOne.AddLabel("Skin Changer");
            var skinslect = AllInOne.Add("skin+", new Slider("Chance Skin", 0, 0, 9));
            ObjectManager.Player.SetSkin(ObjectManager.Player.ChampionName, skinslect.CurrentValue);
            skinslect.OnValueChange += delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
            {
                ObjectManager.Player.SetSkin(ObjectManager.Player.ChampionName, changeArgs.NewValue);
            };

            Indicator = new DamageIndicator.DamageIndicator(); // Set It
            Indicator.Add("Combo", new SpellData(0, DamageType.True, Color.Aqua)); // Add the variables.
            GameObject.OnCreate += GameObject_OnCreate;
            Game.OnUpdate += Game_OnUpdate;
            Chat.Print("Annie - The Little Girl Loaded", Color.Brown);
        }

        static void GameObject_OnCreate(GameObject obj, EventArgs args)
        {
            if (obj.IsValid && obj.Name == "Tibbers")
                Tibbers = obj;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            Indicator.Update("Combo", new SpellData((int)DamageHandler.ComboDamage(), DamageType.Magical, Color.Aqua));
            if (Player.HasBuff("Recall"))
            {
                return;
            }

            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (target.IsValidTarget()
                && (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || AllInOne["stun" + target.ChampionName].Cast<CheckBox>().CurrentValue || !HaveStun))
            {
                if (R.IsReady())
                {
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && HaveStun && target.CountEnemiesInRange(400) > 1 && DamageHandler.R(target) > target.Health)
                        R.Cast(target);
                    else if (HaveStun && AllInOne["rCount"].Cast<Slider>().CurrentValue > 0 && AllInOne["rCount"].Cast<Slider>().CurrentValue >= target.CountEnemiesInRange(300))
                        R.Cast(target);
                    else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && !W.IsReady() && !Q.IsReady()
                        && (target.CountEnemiesInRange(400) > 1 || DamageHandler.R(target) + DamageHandler.Q(target) > target.Health))
                        R.Cast(target);

                }
                if (W.IsReady() && (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)))
                {
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && HaveStun && target.CountEnemiesInRange(250) > 1)
                        W.Cast(target);
                    else if (!Q.IsReady())
                        W.Cast(target);
                    else if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Charm) ||
                    target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Taunt))
                    {
                        W.Cast(target);
                    }
                }
                if (Q.IsReady() && (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)))
                {
                    if (HaveStun && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && target.CountEnemiesInRange(400) > 1 && (W.IsReady() || R.IsReady()))
                    {
                        return;
                    }
                    else
                        Q.Cast(target);
                }
            }
            if (AllInOne["sup"].Cast<CheckBox>().CurrentValue)
            {
                if (Q.IsReady() && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) && Player.Mana > RMANA + QMANA)
                    farmQ();
            }
            else
            {
                if (Q.IsReady() && (!HaveStun || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear)) && (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear)))
                    farmQ();
            }

            SetMana();
            if (AllInOne["autoE"].Cast<CheckBox>().CurrentValue && E.IsReady() && !HaveStun && Player.Mana > RMANA + EMANA + QMANA + WMANA && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                E.Cast();

            
            if (AllInOne["tibers"].Cast<CheckBox>().CurrentValue && HaveTibers)
            {
                var BestEnemy = TargetSelector.GetTarget(3500, DamageType.Magical);
                if (BestEnemy.IsValidTarget(2000) && Game.Time - TibbersTimer > 2)
                {
                    EloBuddy.Player.IssueOrder(GameObjectOrder.MovePet, BestEnemy.Position);
                    R.Cast(BestEnemy);
                    TibbersTimer = Game.Time;
                }
            }
            else
            {
                Tibbers = null;
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear)
                || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                farmQ();
            }


        }

        private static void farmQ()
        {
            if (!AllInOne["farmQ"].Cast<CheckBox>().CurrentValue && !AllInOne["farmW"].Cast<CheckBox>().CurrentValue)
                return;
            var allMinionsQ = EntityManager.GetLaneMinions(
                EntityManager.UnitTeam.Enemy,
                Player.Position.To2D(),
                Q.Range);
            if (Q.IsReady())
            {
                var mobs = EntityManager.GetJungleMonsters(Player.Position.To2D(), Q.Range);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    Q.Cast(mob);
                    if (AllInOne["farmW"].Cast<CheckBox>().CurrentValue
                        && ObjectManager.Player.ManaPercent > AllInOne["Mana"].Cast<Slider>().CurrentValue
                        && W.IsReady())
                    {
                        W.Cast(mob);
                    }
                }
            }

            foreach (var minion in allMinionsQ)
            {
                if (minion.Health > ObjectManager.Player.GetAutoAttackDamage(minion) && minion.Health < DamageHandler.Q(minion))
                {
                    Q.Cast(minion);
                    return;
                }
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) && ObjectManager.Player.ManaPercent > AllInOne["Mana"].Cast<Slider>().CurrentValue && AllInOne["farmW"].Cast<CheckBox>().CurrentValue && ObjectManager.Player.Mana > RMANA + QMANA + EMANA + WMANA * 2)
            {
                var Wfarm = EntityManager.GetLaneMinions(
                    EntityManager.UnitTeam.Enemy,
                    Player.Position.To2D(),
                    W.Range);
                if (Wfarm.Count > 2 && W.IsReady())
                {
                    W.Cast(Wfarm.OrderBy(x => x.IsValid()).FirstOrDefault().ServerPosition);
                }
            }
        }

        private static void SetMana()
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) || Player.HealthPercent < 20)
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

        private static bool HaveStun
        {
            get
            {
                var buffs = Player.Buffs.Where(buff => (buff.Name.ToLower() == "pyromania" || buff.Name.ToLower() == "pyromania_particle"));
                if (buffs.Any())
                {
                    var buff = buffs.First();
                    if (buff.Name.ToLower() == "pyromania_particle")
                        return true;
                    else
                        return false;
                }
                return false;
            }
        }

        private static bool HaveTibers
        {
            get { return ObjectManager.Player.HasBuff("infernalguardiantimer"); }
        }

    }
    #region damge
    internal class DamageHandler
    {

        internal static double ComboDamage()
        {
            var target = TargetSelector.GetTarget(Program.R.Range, DamageType.Magical);
            double dmg = 0;
            if (Program.Q.IsReady())
            {
                dmg += Q(target);
            }
            if (Program.W.IsReady())
            {
                dmg += W(target);
            }
            if (Program.E.IsReady())
            {
                dmg += E(target);
            }
            if (Program.R.IsReady())
            {
                dmg += R(target);
            }
            return dmg;
        }
        public static float Q(Obj_AI_Base target)
        {
            return Program.Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (new float[] { 0, 80, 115, 150, 185, 220 }[Program.Q.Level] + (0.8f * Program.Player.FlatMagicDamageMod)));
        }

        public static float W(Obj_AI_Base target)
        {
            return Program.Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (new float[] { 0, 70, 115, 160, 205, 250 }[Program.W.Level] + (0.85f * Program.Player.FlatMagicDamageMod)));
        }
        public static float E(Obj_AI_Base target)
        {
            return Program.Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (new float[] { 0, 20, 30, 40, 50, 60 }[Program.E.Level] + (0.20f * Program.Player.FlatMagicDamageMod)));
        }
        public static float R(Obj_AI_Base target)
        {
            return Program.Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (new float[] { 0, 175, 300, 425 }[Program.R.Level] + (0.80f * Program.Player.FlatMagicDamageMod)));
        }
    }
#endregion damge
}
