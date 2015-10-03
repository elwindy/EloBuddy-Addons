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
    using EloBuddy.SDK.Constants;
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

        public static GameObject Tibbers;

        public static Menu AllInOne;

        public static List<Obj_AI_Turret> Turrets = new List<Obj_AI_Turret>();

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
            W = new Spell.Skillshot(SpellSlot.W, (uint)600f, SkillShotType.Cone, (int)0.50f, 3000, (int)250f);
            E = new Spell.Active(SpellSlot.E);
            R = new Spell.Skillshot(SpellSlot.R, (uint)625f, SkillShotType.Circular, (int)0.20f, int.MaxValue, (int)250f);
            AllInOne = MainMenu.AddMenu("Annie", "Annie");
            AllInOne.AddGroupLabel("Annie - The Little Girl");
            AllInOne.AddLabel("Combo");
            AllInOne.Add("useqcombo", new CheckBox("Use [Q]"));
            AllInOne.Add("usewcombo", new CheckBox("Use [W]"));
            AllInOne.Add("useecombo", new CheckBox("Use [E]"));
            AllInOne.Add("usercombo", new CheckBox("Use [R]"));
            AllInOne.Add("usersmart", new CheckBox("Smart [R] 1v1 Logic"));
            AllInOne.Add("userhit", new Slider("R If Hit X Enemies", 3, 1, 5));
            AllInOne.AddLabel("Passive Settings");
            AllInOne.Add("ebefore", new CheckBox("Use E Before Q To Gain Stun"));
            AllInOne.Add("estack", new CheckBox("Use E To Stack Stun", false));
            AllInOne.Add("wstack", new CheckBox("Use W To Stack Stun", false));
            AllInOne.AddLabel("Tibbers Settings");
            AllInOne.Add("tibbersmove", new CheckBox("TibbersAutoPilot"));
            AllInOne.AddLabel("Harass Settings");
            AllInOne.Add("useqharass", new CheckBox("Use [Q]"));
            AllInOne.Add("usewharass", new CheckBox("Use [W]"));
            AllInOne.AddLabel("Lane Settings");
            AllInOne.Add("keepstunlane", new CheckBox("Keep Stun"));
            AllInOne.Add("useqlane", new CheckBox("Use [Q]"));
            AllInOne.Add("useqlanelast", new CheckBox("Use [Q] to Last Hit"));
            AllInOne.Add("usewlane", new Slider("Use [W]", 3, 1, 20));
            AllInOne.Add("Mana", new Slider("Min Mana >=", 60));
            AllInOne.AddLabel("Jungle Settings");
            AllInOne.Add("useqjung", new CheckBox("Use [Q]"));
            AllInOne.Add("usewjung", new CheckBox("Use [W]"));
            AllInOne.AddLabel("Last Hit Settings");
            AllInOne.Add("keepstunlast", new CheckBox("Keep Stun"));
            AllInOne.Add("useqxlast", new CheckBox("Use [Q] to Last Hit"));

            AllInOne.AddLabel("Skin Changer");
            var skinslect = AllInOne.Add("skin+", new Slider("Chance Skin", 0, 0, 9));
            ObjectManager.Player.SetSkin(ObjectManager.Player.ChampionName, skinslect.CurrentValue);
            skinslect.OnValueChange += delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
            {
                ObjectManager.Player.SetSkin(ObjectManager.Player.ChampionName, changeArgs.NewValue);
            };

            Indicator = new DamageIndicator.DamageIndicator(); 
            Indicator.Add("Combo", new SpellData(0, DamageType.True, Color.Aqua)); 
            Chat.Print("Annie - The Little Girl Loaded", Color.White);
            Obj_AI_Base.OnProcessSpellCast += SpellCast;
            GameObject.OnCreate += GameObject_OnCreate;
            Game.OnTick += Game_OnTick;

        }

        static void Game_OnTick(EventArgs args)
        {
            Indicator.Update("Combo", new SpellData((int)DamageHandler.ComboDamage(), DamageType.Magical, Color.Aqua));
            if (Player.HasBuff("Recall"))
            {
                return;
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                JungleClear();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                LastHit();
            }

            if (AllInOne["tibbersmove"].Cast<CheckBox>().CurrentValue)
            {
                Tibbersmove();
            }

            Stack();
        }

        private static void LastHit()
        {
            var useql = AllInOne["useqxlast"].Cast<CheckBox>().CurrentValue;
            var usestun = AllInOne["keepstunlast"].Cast<CheckBox>().CurrentValue;
            if (usestun && Player.HasBuff("pyromania_particle"))
                return;
            var minionCount =
                EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Position.To2D(), Q.Range)
                    .FirstOrDefault();

            if (minionCount == null)
                return;

            var minion = minionCount;
            var minionhp = minion.Health;

            if (minionhp <= DamageHandler.Q(minion) && useql && Q.IsReady())
            {
                Q.Cast(minion);
            }
        }

        private static void JungleClear()
        {
            var useq = AllInOne["useqjung"].Cast<CheckBox>().CurrentValue;
            var usew = AllInOne["usewjung"].Cast<CheckBox>().CurrentValue;

            var minionCount = EntityManager.GetJungleMonsters(Player.Position.To2D(), Q.Range).FirstOrDefault();

            if (minionCount == null)
                return;
            var minion = minionCount;

            if (useq && Q.IsReady() && minion.IsValidTarget(Q.Range))
            {
                Q.Cast(minion);
            }

            if (usew && W.IsReady() && minion.IsValidTarget(W.Range))
            {
                W.Cast(minion);
            }
        }

        private static void LaneClear()
        {
            var useq = AllInOne["useqlane"].Cast<CheckBox>().CurrentValue;
            var usestun = AllInOne["keepstunlane"].Cast<CheckBox>().CurrentValue;
            var useql = AllInOne["useqlanelast"].Cast<CheckBox>().CurrentValue;
            var usewslider = AllInOne["usewlane"].Cast<Slider>().CurrentValue;
            var minMana = AllInOne["Mana"].Cast<Slider>().CurrentValue;
            if (usestun && Player.HasBuff("pyromania_particle"))
                return;
            var minionCount =
                EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Position.To2D(), Q.Range)
                    .FirstOrDefault();

            if (minionCount == null)
                return;
            var minion = minionCount;
            var minionhp = minionCount.Health;


            if (useql && Q.IsReady() && minion.IsValidTarget(Q.Range) && minionhp <= DamageHandler.Q(minion) && minionhp > Player.GetAutoAttackDamage(minion))
            {
                Q.Cast(minion);
            }

            if (useq && Q.IsReady() && minion.IsValidTarget(Q.Range) && Player.ManaPercent >= minMana)
            {
                Q.Cast(minion);
            }

            var Wfarm = EntityManager.GetLaneMinions(
                    EntityManager.UnitTeam.Enemy,
                    Player.Position.To2D(),
                    W.Range);
            if (Wfarm == null)
                return;


            if (W.IsReady() && minion.IsValidTarget(W.Range) && Wfarm.Count >= usewslider &&
                    Player.ManaPercent >= minMana)
                {
                    W.Cast(Wfarm.OrderBy(x => x.IsValid()).FirstOrDefault().ServerPosition);
                }
            
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (target == null)
                return;

            var useq = AllInOne["useqharass"].Cast<CheckBox>().CurrentValue;
            var usew = AllInOne["usewharass"].Cast<CheckBox>().CurrentValue;

            if (useq && Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                Q.Cast(target);
            }

            if (usew && W.IsReady() && target.IsValidTarget(W.Range))
            {
                W.Cast(target);
            }
        }

        private static void Stack()
        {
            var usee = AllInOne["estack"].Cast<CheckBox>().CurrentValue;
            var usew = AllInOne["wstack"].Cast<CheckBox>().CurrentValue;

            if (Player.HasBuff("pyromania_particle"))
                return;

            if (usee && E.IsReady())
            {
                E.Cast();
            }

            if (usew && W.IsReady())
            {
                W.Cast(Game.CursorPos);
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (target == null)
                return;

            var useq = AllInOne["useqcombo"].Cast<CheckBox>().CurrentValue;
            var usew = AllInOne["usewcombo"].Cast<CheckBox>().CurrentValue;
            var usee = AllInOne["useecombo"].Cast<CheckBox>().CurrentValue;
            var user = AllInOne["usercombo"].Cast<CheckBox>().CurrentValue;
            var usersmart = AllInOne["usersmart"].Cast<CheckBox>().CurrentValue;
            var useebefore = AllInOne["ebefore"].Cast<CheckBox>().CurrentValue;
            var userslider = AllInOne["userhit"].Cast<Slider>().CurrentValue;


            if (Q.IsReady() && target.IsValidTarget(Q.Range) && useq)
            {
                if (useebefore)
                {
                    if (GetPassiveBuff == 3 && E.IsReady() && !Player.HasBuff("summonerteleport"))
                    {
                        E.Cast();
                    }

                    if (!R.IsReady())
                    {
                        Q.Cast(target);
                    }
                    else
                    {
                        if (Player.HasBuff("pyromania_particles") && usersmart)
                            return;
                        Q.Cast(target);
                    }
                }

                if (!R.IsReady())
                {
                    Q.Cast(target);
                }
                else
                {
                    if (Player.HasBuff("pyromania_particles") && usersmart)
                        return;
                    Q.Cast(target);
                }
            }

            if (usee && E.IsReady() && !Player.HasBuff("pyromania_particles") && !Player.HasBuff("summonerteleport"))
            {
                if (GetPassiveBuff == 3)
                {
                    E.Cast();
                }
            }

            if (W.IsReady() && target.IsValidTarget(W.Range) && usew && !Player.HasBuff("summonerteleport"))
            {
                if (Player.HasBuff("pyromania_particles") && R.IsReady() && usersmart)
                    return;
                W.Cast(target);
            }

            if (R.IsReady()
                && user && target.IsValidTarget(R.Range) && !Player.HasBuff("summonerteleport") && Player.HasBuff("pyromania_particle"))
            {
                foreach (var rhit in
                        ObjectManager.Get<AIHeroClient>()
                            .Where(enemy => enemy.IsValidTarget() && userslider >= enemy.CountEnemiesInRange(400))
                            .Select(x => R.GetPrediction(x))
                            .Where(pred => pred.HitChance >= HitChance.Medium))
                {
                    R.Cast(rhit.CastPosition);
                }
                if (target.Health >= DamageHandler.Q(target) + DamageHandler.W(target))
                {
                    R.Cast(target.Position);
                }
            }
        }

        public static int GetPassiveBuff
        {
            get
            {
                var data = Player.Buffs.FirstOrDefault(b => b.DisplayName == "Pyromania");
                return data != null ? data.Count : 0;
            }
        }

        public static Obj_AI_Turret GetTurrets()
        {
            var turri =
                Turrets.OrderBy(x => x.Distance(Tibbers.Position) <= 500 && !x.IsAlly && !x.IsDead)
                    .FirstOrDefault();
            return turri;
        }

        static void GameObject_OnCreate(GameObject obj, EventArgs args)
        {
            if (obj.IsValid && obj.Name == "Tibbers")
                Tibbers = obj;
        }

        private static void SpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if(!AllInOne["useecombo"].Cast<CheckBox>().CurrentValue) return;
            if (sender.IsEnemy
                && sender.Type == Player.Type
                && args.SData.IsAutoAttack()
                && args.Target.IsMe)
            {
                E.Cast();
            }
        }

        public static void Tibbersmove()
        {
            var target = TargetSelector.GetTarget(2000, DamageType.Magical);

            if (Player.HasBuff("infernalguardiantime") && Player.HasBuff("infernalguardiantimer"))
            {
                EloBuddy.Player.IssueOrder(GameObjectOrder.MovePet,
                    target.IsValidTarget(1500) ? target.Position : GetTurrets().Position);
            }
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
