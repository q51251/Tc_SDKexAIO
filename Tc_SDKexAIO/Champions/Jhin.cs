﻿namespace Tc_SDKexAIO.Champions
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;

    using LeagueSharp.SDK.Utils;
    using LeagueSharp.SDK.UI;

    using System;
    using System.Linq;
    using System.Windows.Forms;

    using SharpDX;

    using Common;
    using Config;
    using static Common.Manager;

    using Menu = LeagueSharp.SDK.UI.Menu;

    internal static class Jhin
    {

        private static Menu Menu => PlaySharp.ChampionMenu;
        private static Obj_AI_Hero Player => PlaySharp.Player;
        private static HpBarDraw HpBarDraw = new HpBarDraw();
        private static float LasPing = Variables.TickCount;
        private static string StartR = "JhinR";
        private static string IsJhinRShot = "JhinRShot";
        private static Spell Q, W, E, R;
        private static int LastPingT, LastECast, LastShowNoit;
        private static bool IsAttack;
        private static Vector2 PingLocation;

        internal static void Init()
        {

            Q = new Spell(SpellSlot.Q, 600f);
            W = new Spell(SpellSlot.W, 2500f).SetSkillshot(0.75f, 40, float.MaxValue, false, SkillshotType.SkillshotLine);
            E = new Spell(SpellSlot.E, 750f).SetSkillshot(0.5f, 120, 1600, false, SkillshotType.SkillshotCircle);
            R = new Spell(SpellSlot.R, 3500f).SetSkillshot(0.21f, 80, 5000, false, SkillshotType.SkillshotLine);


            var QMenu = Menu.Add(new Menu("Q", "Q.Set"));
            {
                QMenu.GetSeparator("Q Mode");
                QMenu.Add(new MenuBool("ComboQ", "Combo Q", true));
                QMenu.Add(new MenuSliderButton("HarassQ", "Harass Q | Min Mana", 60));
                QMenu.Add(new MenuSliderButton("LaneClearQ", "LaneClear Q | Min Mana", 40));
                QMenu.Add(new MenuSliderButton("JungleQ", "Jungle Q | Min Mana", 40));
                QMenu.Add(new MenuBool("KillStealQ", "KillSteal Q", true));
            }

            var WMenu = Menu.Add(new Menu("W", "W.Set"));
            {
                WMenu.GetSeparator("W Mode");
                WMenu.Add(new MenuBool("ComboW", "Combo W", true));
                WMenu.Add(new MenuBool("ComboWAA", "Use Combo W | After Attack", true));
                WMenu.Add(new MenuBool("WMO", "W Only Marked Target", true));
                WMenu.Add(new MenuSliderButton("HarassW", "Harass W | Min Mana", 60));
                WMenu.Add(new MenuSliderButton("JungleW", "Jungle W | Min Mana", 40));
                WMenu.Add(new MenuBool("KillStealW", "KillSteal W", true));
                WMenu.Add(new MenuBool("AutoW", "Auto W Target Cant Move", true));
                WMenu.GetSeparator("Misc Mode");
                WMenu.Add(new MenuBool("GapW", "Anti GapCloser W| When target HavePassive", true));
            }

            var EMenu = Menu.Add(new Menu("E", "E.Set"));
            {
                EMenu.GetSeparator("E Mobe");
                EMenu.Add(new MenuBool("ComboE", "Combo E", true));
                EMenu.Add(new MenuSliderButton("HarassE", "Harass E Enemy | Min Mana", 60));
                EMenu.Add(new MenuSliderButton("JungleE", "Jungle E | Min Mana", 40));
                EMenu.Add(new MenuSliderButton("LaneClearE", "LaneClear E | Min Mana", 40));
                EMenu.Add(new MenuSlider("LCminions", "LaneClear Min minion", 5, 3, 8));
                EMenu.GetSeparator("Misc Mode");
                EMenu.Add(new MenuBool("GapE", "Anti GapCloser E| When target HavePassive", true));
                EMenu.Add(new MenuBool("AutoE", "Auto E Target Cant Move", true));
            }

            var RMenu = Menu.Add(new Menu("R", "R.Set"));
            {
                RMenu.GetSeparator("R Mode");
                RMenu.Add(new MenuBool("AutoR", "Auto R", true));
                RMenu.Add(new MenuBool("ComboR", "Combo R", true));
                RMenu.Add(new MenuBool("RCheck", "Use R | Check is Safe Range", true));
                RMenu.Add(new MenuSlider("RMinRange", "Use R Min Range = ", 1000, 500, 2500));
                RMenu.Add(new MenuSlider("RMaxRange", "Use R Max Range = ", 3000, 1500, 3500));
                RMenu.Add(new MenuSlider("RKill", "UseR Min Shot Can Kill = ", 3, 1, 4));
                RMenu.GetSeparator("Misc Mode");
                RMenu.Add(new MenuKeyBind("RKey", "R Key Semi", Keys.T, KeyBindType.Press));
                RMenu.Add(new MenuBool("Rturrent", "R Not Tuttent", true));
                RMenu.Add(new MenuBool("PingKill", "Auto Ping Kill Target", true));
                RMenu.Add(new MenuBool("NormalPingKill", "Normal Ping Kill", true));
                RMenu.Add(new MenuBool("NotificationKill", "Notification Kill Target", true));
            }

            var MiscMenu = Menu.Add(new Menu("Misc", "Misc.Set"));
            {
                MiscMenu.GetSeparator("Misc Mode");
                MiscMenu.Add(new MenuBool("UseYoumuu", "Combo Use Youmuu", true));
                MiscMenu.Add(new MenuBool("UseBotrk", "Combo Use Botrk", true));
                MiscMenu.Add(new MenuBool("UseCutlass", "Combo Use Cutlass", true));
            }

            var DrawMenu = Menu.Add(new Menu("Draw", "Draw"));
            {
                DrawMenu.Add(new MenuBool("Q", "Q Range"));
                DrawMenu.Add(new MenuBool("W", "W Range"));
                DrawMenu.Add(new MenuBool("E", "E Range"));
                DrawMenu.Add(new MenuBool("R", "R Range"));
                DrawMenu.Add(new MenuBool("DrawRMin", "Draw R Range(MinMap)", true));
                DrawMenu.Add(new MenuBool("RDind", "Draw Combo Damage", true));
            }

            PlaySharp.Write(GameObjects.Player.ChampionName + "OK! :)");


            Obj_AI_Base.OnDoCast += OnDoCast;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Events.OnGapCloser += OnGapCloser;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;
            Game.OnUpdate += OnUpdate;

        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
                return;

            var Espell = Player.GetSpellSlot(args.SData.Name);

            if (Espell == SpellSlot.E)
            {
                LastECast = Variables.TickCount;
            }

            if (AutoAttack.IsAutoAttack(args.SData.Name))
            {
                IsAttack = true;
                DelayAction.Add(500, () => IsAttack = false);
            }
        }

        private static void OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe || !AutoAttack.IsAutoAttack(args.SData.Name))
            {
                return;
            }

            switch (Variables.Orbwalker.ActiveMode)
            {
                case OrbwalkingMode.Combo:
                    {
                        var enemy = (Obj_AI_Hero)args.Target;

                        if (enemy != null && !enemy.IsDead && !enemy.IsZombie)
                        {
                            if (Menu["Misc"]["UseYoumuu"] && Items.HasItem(3142) && Items.CanUseItem(3142))
                            {
                                Items.UseItem(3142);
                            }

                            if (Menu["Q"]["ComboQ"] && Q.IsReady() && enemy.IsValidTarget(Q.Range))
                            {
                                Q.CastOnUnit(enemy);
                            }

                            if (Menu["W"]["ComboW"] && W.IsReady())
                            {
                                if (Menu["W"]["ComboWAA"] && enemy.IsValidTarget(W.Range) && enemy.HasBuff("jhinespotteddebuff"))
                                {
                                    CastSpell(W, enemy);
                                }
                            }
                        }
                    }
                    break;
                case OrbwalkingMode.Hybrid:
                    {
                        var hero = sender.Target as Obj_AI_Hero;

                        if (hero != null && !hero.IsDead)
                        {
                            var target = hero;

                            if (Menu["Q"]["HarassQ"].GetValue<MenuSliderButton>().BValue && Q.IsReady())
                            {
                                if (Player.ManaPercent >= Menu["Q"]["HarassQ"].GetValue<MenuSliderButton>().SValue)
                                {
                                    if (target.IsValidTarget(Q.Range))
                                    {
                                        Q.CastOnUnit(target);
                                    }
                                }
                            }

                            if (Menu["W"]["HarassW"].GetValue<MenuSliderButton>().BValue && W.IsReady())
                            {
                                if (Player.ManaPercent >= Menu["W"]["HarassW"].GetValue<MenuSliderButton>().SValue)
                                {
                                    if (target.IsValidTarget(W.Range) && target.HasBuff("jhinespotteddebuff"))
                                    {
                                        CastSpell(W, target);
                                    }
                                }
                            }
                        }
                    }
                    break;                
            }
        }

        private static void OnGapCloser(object sender, Events.GapCloserEventArgs args)
        {
            var enemy = args.Sender;

            if (enemy.Target.IsValidTarget(E.Range) && (args.End.DistanceToPlayer() <= 300 || enemy.DistanceToPlayer() <= 300))
            {
                if (Menu["E"]["GapE"] && E.IsReady() && Variables.TickCount - LastECast > 2500 && !IsAttack)
                {
                    CastSpell(E, enemy);                 
                }
                if (Menu["W"]["GapW"] && W.IsReady() && HasPassive(enemy))
                {
                    CastSpell(E, enemy);            
                }
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }

            foreach (var enemy in GameObjects.EnemyHeroes.Where(e => R.IsReady() && e.IsValidTarget(R.Range) && Player.GetSpellDamage(e, SpellSlot.R) * Menu["R"]["RKill"].GetValue<MenuSlider>().Value > e.Health + e.HPRegenRate * 3))
            {
                if (Menu["R"]["PingKill"])
                {
                    Ping(enemy.Position.To2D());
                }

                if (Menu["R"]["NotificationKill"] && Variables.TickCount - LastShowNoit > 10000)
                {
                    LastShowNoit = Variables.TickCount;
                }
            }

            if (R.Instance.Name == IsJhinRShot)
            {
                Variables.Orbwalker.AttackState = false;
                Variables.Orbwalker.MovementState = false;
            }
            else //if (R.Instance.Name == "JhinR")
            {
                Variables.Orbwalker.AttackState = true;
                Variables.Orbwalker.MovementState = true;
            }

            KillSteal();
            RLogic();
            AutoLogic();

            switch (Variables.Orbwalker.ActiveMode)
            {
                case OrbwalkingMode.Combo:
                    ComboLogic();
                    break;
                case OrbwalkingMode.Hybrid:
                    HarassLogic();
                    break;
                case OrbwalkingMode.LaneClear:
                    LaneClearLogic();
                    JungleLogic();
                    break;
                case OrbwalkingMode.None:
                    break;
            }
        }

        private static void RLogic()
        {
            Obj_AI_Hero target = null;

            if (Variables.TargetSelector.GetSelectedTarget() != null && Variables.TargetSelector.GetSelectedTarget().DistanceToPlayer() <= Menu["R"]["RMaxRange"].GetValue<MenuSlider>().Value)
            {
                target = Variables.TargetSelector.GetSelectedTarget();
            }
            else
            {
                target = GetTarget(R.Range, DamageType.Physical);
            }

            if (R.IsReady() && CheckTarget(target) && R.IsInRange(target))
            {
                if (R.Instance.Name == StartR)
                {
                    if (Menu["R"]["RKey"].GetValue<MenuKeyBind>().Active)
                    {
                        R.Cast(R.GetPrediction(target).UnitPosition);
                    }

                    if (!Menu["R"]["AutoR"])
                    {
                        return;
                    }

                    if (Menu["R"]["RCheck"] && Player.CountEnemyHeroesInRange(800f) > 0)
                    {
                        return;
                    }

                    if (target.DistanceToPlayer() <= Menu["R"]["RMinRange"].GetValue<MenuSlider>().Value)
                    {
                        return;
                    }

                    if (target.DistanceToPlayer() > Menu["R"]["RMaxRange"].GetValue<MenuSlider>().Value)
                    {
                        return;
                    }

                    if (target.Health > Player.GetSpellDamage(target, SpellSlot.R) * Menu["R"]["RKill"].GetValue<MenuSlider>().Value)
                    {
                        return;
                    }
                    R.Cast(R.GetPrediction(target).CastPosition);
                }

                if (R.Instance.Name == IsJhinRShot)
                {
                    if (Menu["R"]["RKey"].GetValue<MenuKeyBind>().Active)
                    {
                        AutoUse(target);
                        R.Cast(R.GetPrediction(target).UnitPosition);
                    }

                    if (Menu["R"]["ComboR"] && Variables.Orbwalker.ActiveMode == OrbwalkingMode.Combo)
                    {
                        AutoUse(target);
                        R.Cast(R.GetPrediction(target).UnitPosition);
                    }

                    if (!Menu["R"]["AutoR"])
                    {
                        return;
                    }
                    AutoUse(target);
                    R.Cast(R.GetPrediction(target).UnitPosition);
                }
            }
        }

        private static void KillSteal()
        {
            if (R.Instance.Name == IsJhinRShot)
                return;

            var WTarget = GetTarget(W.Range, W.DamageType);
            var QTarget = GetTarget(Q.Range, Q.DamageType);

            if (Menu["W"]["KillStealW"] && CheckTarget(WTarget) && Q.IsInRange(WTarget) && W.IsReady() && WTarget.Health < Player.GetSpellDamage(WTarget, SpellSlot.W) && !(Q.IsReady() && WTarget.IsValidTarget(Q.Range) && WTarget.Health < Player.GetSpellDamage(WTarget, SpellSlot.Q)))
            {
                CastSpell(W, WTarget);
                return;
            }

            if (Menu["Q"]["KillStealQ"] && CheckTarget(QTarget) && Q.IsInRange(QTarget) && Q.IsReady() && QTarget.Health < Player.GetSpellDamage(QTarget, SpellSlot.Q))
            {
                Q.CastOnUnit(QTarget);
            }
        }

        private static void AutoLogic()
        {
            if (R.Instance.Name == IsJhinRShot)
                return;

            foreach (var target in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(W.Range) && !enemy.CanMove))
            {
                if (Menu["W"]["AutoW"] && W.IsReady() && target.IsValidTarget(W.Range))
                {
                    CastSpell(W, target);
                }

                if (Menu["E"]["AutoE"] && E.IsReady() && Variables.TickCount - LastECast > 2500 && !IsAttack)
                {
                    CastSpell(E, target);
                }
            }
        }

        private static void ComboLogic()
        {
            if (R.Instance.Name == IsJhinRShot)
                return;

            var WTarget = GetTarget(W.Range, W.DamageType);
            var QTarget = GetTarget(Q.Range, Q.DamageType);
            var ETarget = GetTarget(E.Range, E.DamageType);
            var enemy = Variables.TargetSelector.GetTarget(600, DamageType.Physical);

            if (enemy != null && enemy.IsValidTarget(Player.GetRealAutoAttackRange()))
            {
                if (Menu["Misc"]["UseCutlass"] && Items.HasItem(3144) && Items.CanUseItem(3144))
                {
                    Items.UseItem(3144, enemy);
                }

                if (Menu["Misc"]["UseBotrk"] && Items.HasItem(3153) && Items.CanUseItem(3153) && (enemy.HealthPercent < 80 || Player.HealthPercent < 80))
                {
                    Items.UseItem(3153, enemy);
                }
            }

            if (Menu["W"]["ComboW"] && W.IsReady() && CheckTarget(WTarget) && W.IsInRange(WTarget))
            {
                if (Menu["W"]["WMO"])
                {
                    if (HasPassive(WTarget))
                    {
                        CastSpell(W, WTarget);
                    }
                }
                else
                {
                    CastSpell(W, WTarget);
                }
            }

            if (Menu["Q"]["ComboQ"] && Q.IsReady() && CheckTarget(QTarget) && Q.IsInRange(QTarget) && !Variables.Orbwalker.CanAttack)
            {
                Q.CastOnUnit(QTarget);
            }

            if (Menu["E"]["ComboE"] && E.IsReady() && CheckTarget(ETarget) && Q.IsInRange(ETarget) && Variables.TickCount - LastECast > 2500 && !IsAttack)
            {
                if (!ETarget.CanMove)
                {
                    CastSpell(E, ETarget);
                }
                else
                {
                    if (E.GetPrediction(ETarget).Hitchance >= HitChance.High)
                    {
                        E.Cast(E.GetPrediction(ETarget).UnitPosition);
                    }
                }
            }              
        }

        private static void HarassLogic()
        {
            var WTarget = GetTarget(1500f, W.DamageType);
            var ETarget = GetTarget(E.Range, DamageType.Magical);

            if (Menu["W"]["HarassW"].GetValue<MenuSliderButton>().BValue && CanHarras())
            {
                if (CheckTarget(WTarget) && W.IsReady())
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(W.Range)))
                    {
                        if (Player.ManaPercent >= Menu["W"]["HarassW"].GetValue<MenuSliderButton>().SValue)
                        {
                            if (Menu["W"]["WMO"] && !HasPassive(WTarget))
                            {
                                return;
                            }
                            CastSpell(W, enemy);
                        }
                    }
                }
            }

            if (Menu["E"]["HarassE"].GetValue<MenuSliderButton>().BValue && E.IsReady() && CheckTarget(ETarget) && E.IsInRange(ETarget) && Variables.TickCount - LastECast > 2500 && !IsAttack)
            {
                if (Player.ManaPercent >= Menu["E"]["HarassE"].GetValue<MenuSliderButton>().SValue)
                {
                    CastSpell(E, ETarget);
                }
            }
        }
        
        private static void LaneClearLogic()
        {
            var Qminions = GetMinions(Player.Position, Q.Range);
            var Eminions = GetMinions(Player.Position, E.Range);
            var minion = Qminions.MinOrDefault(x => x.Health);

            if (!Qminions.Any())
            {
                return;
            }

            if (Menu["Q"]["LaneClearQ"].GetValue<MenuSliderButton>().BValue && Q.IsReady() && minion != null && minion.IsValidTarget(Q.Range) && Qminions.Count > 2)
            {
                if (Player.ManaPercent >= Menu["Q"]["LaneClearQ"].GetValue<MenuSliderButton>().SValue)
                {
                    Q.Cast(minion);
                }
            }

            if (Menu["E"]["LaneClearE"].GetValue<MenuSliderButton>().BValue && E.IsReady())
            {
                var FarmPosition = E.GetCircularFarmLocation(Eminions, E.Width);

                if (Player.ManaPercent >= Menu["E"]["LaneClearE"].GetValue<MenuSliderButton>().SValue)
                {
                    if (FarmPosition.MinionsHit >= Menu["E"]["LCminions"].GetValue<MenuSlider>().Value)
                    {
                        E.Cast(FarmPosition.Position);
                    }
                }
            }
        }

        private static void JungleLogic()
        {
            var mobs = GetMobs(Player.Position, Q.Range);
            var minion = mobs.MinOrDefault(x => x.Health);
            var mob = mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini"));

            if (Menu["Q"]["JungleQ"].GetValue<MenuSliderButton>().BValue && Q.IsReady())
            {
                if (Player.ManaPercent >= Menu["Q"]["JungleQ"].GetValue<MenuSliderButton>().SValue)                                    
                {
                    if (minion != null)
                    {
                        Q.Cast(minion);
                    }
                }
            }

            if (Menu["W"]["JungleW"].GetValue<MenuSliderButton>().BValue && W.IsReady())
            {
                if (Player.ManaPercent >= Menu["W"]["JungleW"].GetValue<MenuSliderButton>().SValue)
                {
                    if (mobs.Count() > 2)
                    {
                        W.Cast(mob ?? mobs.FirstOrDefault());
                    }
                }
            }

            if (Menu["E"]["JungleE"].GetValue<MenuSliderButton>().BValue && E.IsReady() && mob.IsValidTarget(E.Range) && Variables.TickCount - LastECast > 2500 && !IsAttack)
            {
                if (Player.ManaPercent >= Menu["E"]["JungleE"].GetValue<MenuSliderButton>().SValue)
                {
                    if (mobs.Count() > 1)
                    {
                        E.Cast(mob ?? mobs.FirstOrDefault());
                    }
                }
            }
        }

        private static void OnEndScene(EventArgs args)
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget() && x.IsEnemy))
            {
                if (Menu["Draw"]["RDind"].GetValue<MenuBool>() && R.Level >= 1)
                {
                    HpBarDraw.Unit = enemy;
                    HpBarDraw.DrawDmg(R.GetDamage(enemy) * 3, Color.Cyan);
                }
            }

            if (!Player.IsDead && !MenuGUI.IsShopOpen && !MenuGUI.IsChatOpen && !MenuGUI.IsScoreboardOpen)
            {
                if (Menu["Draw"]["DrawRMin"] && R.IsReady())
                {
                    Render.Circle.DrawCircle(Player.Position, R.Range, System.Drawing.Color.FromArgb(14, 194, 255), 1);
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (!Player.IsDead && !MenuGUI.IsChatOpen && !MenuGUI.IsChatOpen && !MenuGUI.IsScoreboardOpen)
            {
                if (Menu["Draw"]["Q"])
                {
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.DeepPink);
                    else
                        Render.Circle.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.DeepPink);                   
                }

                if (Menu["Draw"]["W"])
                {
                    if (W.IsReady())
                        Render.Circle.DrawCircle(Player.Position, W.Range, System.Drawing.Color.Blue);
                    else
                        Render.Circle.DrawCircle(Player.Position, W.Range, System.Drawing.Color.Blue);
                }

                if (Menu["Draw"]["E"])
                {
                    if (E.IsReady())
                        Render.Circle.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Cyan);
                    else
                        Render.Circle.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Cyan);
                }

                if (Menu["Draw"]["R"])
                {
                    if (R.IsReady())
                        Render.Circle.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Yellow);
                    else
                        Render.Circle.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Yellow);
                }
            }
        }

        private static bool HasPassive(Obj_AI_Hero target)
        {
            return target.HasBuff("jhinespotteddebuff");
        }

        private static void Ping(Vector2 position)
        {
            if (Variables.TickCount - LastPingT < 30 * 1000)
            {
                return;
            }

            LastPingT = Variables.TickCount;
            PingLocation = position;
            SimplePing();

            DelayAction.Add(150, SimplePing);
            DelayAction.Add(300, SimplePing);
            DelayAction.Add(500, SimplePing);
            DelayAction.Add(800, SimplePing);
        }

        private static void SimplePing()
        {
            Game.ShowPing(Menu["R"]["NormalPingKill"] ? PingCategory.Normal : PingCategory.Fallback, PingLocation, true);
        }

        private static void AutoUse(Obj_AI_Hero target)
        {
            if (Items.HasItem(3363) && Items.CanUseItem(3363))
            {
                Items.UseItem(3363, target.Position);
            }
        }
    }
}