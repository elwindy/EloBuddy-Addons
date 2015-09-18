#region

using System;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using RyzeBuddy.Activator;
using Item = RyzeBuddy.Activator.Item;

#endregion

namespace RyzeBuddy
{
    using System.Collections.Specialized;

    internal class Program
    {
        public const string ChampionName = "Ryze";

        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }

        public static Dictionary<SpellSlot, Spell.SpellBase> Spells = new Dictionary<SpellSlot, Spell.SpellBase>()
        {
            {SpellSlot.Q, new Spell.Skillshot(SpellSlot.Q,900,SkillShotType.Linear,250,1700,50)},
            {SpellSlot.W, new Spell.Targeted(SpellSlot.W, 600)},
            {SpellSlot.E, new Spell.Targeted(SpellSlot.E, 600)},
            {SpellSlot.R, new Spell.Active(SpellSlot.R)}
        };

        public static Menu menu, ComboMenu, HarassMenu;

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if(_Player.ChampionName != ChampionName) {return;}

            Bootstrap.Init(null);
            Hacks.AntiAFK = true;
            ItemManager.Init();
            menu = MainMenu.AddMenu("Ryze Buddy", "ryzeBuddy");
            menu.AddGroupLabel("Ryze Buddy");
            menu.AddLabel("made by the elwindy");
            menu.AddSeparator();
            ComboMenu = menu.AddSubMenu("Combo", "ComboRyze");
            ComboMenu.Add("useQCombo", new CheckBox("Use Q"));
            ComboMenu.Add("useWCombo", new CheckBox("Use W"));
            ComboMenu.Add("useECombo", new CheckBox("Use E"));
            ComboMenu.Add("useRCombo", new CheckBox("Only R if Target Is Rooted"));
            ComboMenu.AddSeparator();
            HarassMenu = menu.AddSubMenu("Harass", "HarassRyze");
            HarassMenu.Add("useQHarass", new CheckBox("Use Q"));
            HarassMenu.Add("useWHarass", new CheckBox("Use W",false));
            HarassMenu.Add("useEHarass", new CheckBox("Use E",false));
            HarassMenu.AddSeparator();
            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
        }

        static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                args.Process = !(Spells[SpellSlot.Q].IsReady() || Spells[SpellSlot.W].IsReady()
                               || Spells[SpellSlot.E].IsReady() || _Player.Distance(target) >= 600);
        }

        private static void Game_OnTick(EventArgs args)
        {
            Orbwalker.ForcedTarget = null;
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) Combo();
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)) Harass();
        }

       

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Spells[SpellSlot.W].Range, DamageType.Magical);
            var qTarget = TargetSelector.GetTarget(Spells[SpellSlot.Q].Range, DamageType.Magical);

            
            if (target != null)
            {
                if (HarassMenu["useWHarass"].Cast<CheckBox>().CurrentValue) { Spells[SpellSlot.W].Cast(target); }
                if (HarassMenu["useEHarass"].Cast<CheckBox>().CurrentValue) { Spells[SpellSlot.E].Cast(target); }
            }

            if (qTarget != null)
            {
                if (HarassMenu["useQHarass"].Cast<CheckBox>().CurrentValue) { Spells[SpellSlot.Q].Cast(qTarget); }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            //SOON :)
        }
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Spells[SpellSlot.W].Range, DamageType.Magical);
            var qTarget = TargetSelector.GetTarget(Spells[SpellSlot.Q].Range, DamageType.Magical);

            if (target != null)
            {
                if (ComboMenu["useWCombo"].Cast<CheckBox>().CurrentValue){Spells[SpellSlot.W].Cast(target);}
                if (ComboMenu["useECombo"].Cast<CheckBox>().CurrentValue){Spells[SpellSlot.E].Cast(target);}
            }

            if (qTarget != null)
            {
                if (ComboMenu["useQCombo"].Cast<CheckBox>().CurrentValue) {Spells[SpellSlot.Q].Cast(qTarget);}
            }
            if ((target.HasBuff("RyzeW") || qTarget.HasBuff("RyzeW")) && ComboMenu["useRCombo"].Cast<CheckBox>().CurrentValue)
            {
                Spells[SpellSlot.R].Cast();
            }
            
        }
        

    }
}
