﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Color = System.Drawing.Color;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace MasterOfLeeSin
{
    class LeeSin
    {
        public static Menu Config;
        public static Obj_AI_Turret turretObj = null;
        public static Obj_AI_Hero Player = ObjectManager.Player, targetObj = null, friendlyObj = null, insecObj = null;
        public static TargetSelector ts;
        public static Spell SkillQ, SkillW, SkillE, SkillR;
        public static SpellDataInst QData, WData, EData, RData, FData, SData, IData;
        public static Boolean QReady = false, WReady = false, EReady = false, RReady = false, FReady = false, SReady = false, IReady = false;
        public static Int32 Tiamat = 3077, Hydra = 3074, Blade = 3153, Bilge = 3144, Rand = 3143;
        public static Boolean TiamatReady = false, HydraReady = false, BladeReady = false, BilgeReady = false, RandReady = false;
        public static InventorySlot useSight = null;
        public static Obj_AI_Base lastWard = null, farmMinion = null;
        public static float lastTimeInsec = 0, lastTimeWard = 0, lastTimeJump = 0, lastTimeQ = 0;
        public static Int32 lastSkin = 0;
        public static Boolean PacketCast = false;

        public static System.Diagnostics.Process Proc = System.Diagnostics.Process.GetCurrentProcess();
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        static void OnLoad(EventArgs args)
        {
            if (Player.ChampionName != "LeeSin") return;
            QData = Player.Spellbook.GetSpell(SpellSlot.Q);
            WData = Player.Spellbook.GetSpell(SpellSlot.W);
            EData = Player.Spellbook.GetSpell(SpellSlot.E);
            RData = Player.Spellbook.GetSpell(SpellSlot.R);
            FData = Player.SummonerSpellbook.GetSpell(Player.GetSpellSlot("summonerflash"));
            SData = Player.SummonerSpellbook.GetSpell(Player.GetSpellSlot("summonersmite"));
            IData = Player.SummonerSpellbook.GetSpell(Player.GetSpellSlot("summonerdot"));
            SkillQ = new Spell(QData.Slot, QData.SData.CastRange[0]);
            SkillW = new Spell(WData.Slot, WData.SData.CastRange[0]);
            SkillE = new Spell(EData.Slot, EData.SData.CastRange[0]);
            SkillR = new Spell(RData.Slot, RData.SData.CastRange[0]);
            SkillQ.SetSkillshot(-QData.SData.SpellCastTime, QData.SData.LineWidth, QData.SData.MissileSpeed, true, SkillshotType.SkillshotLine);

            Config = new Menu("Master Of LeeSin", "LeeSinCombo", true);
            Config.AddSubMenu(new Menu("Target Selector", "TSSettings"));
            Config.SubMenu("TSSettings").AddItem(new MenuItem("Focus", "Mode:")).SetValue(new StringList(new[] { "Auto", "Closest", "LessAttack", "LessCast", "LowHP", "MostAD", "MostAP", "NearMouse" }, 7));

            Config.AddSubMenu(new Menu("Orbwalker", "Orbwalk"));
            Config.SubMenu("Orbwalk").AddItem(new MenuItem("HoldPosRadius", "Hold Position Radius").SetValue(new Slider(0, 150, 0)));
            Config.SubMenu("Orbwalk").AddItem(new MenuItem("ExtraWindup", "Extra Windup Time").SetValue(new Slider(80, 200, 0)));

            Config.AddSubMenu(new Menu("Key Bindings", "KeyBindings"));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("scriptActive", "Combo").SetValue(new KeyBind(32, KeyBindType.Press)));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("insecMake", "Insec").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("starActive", "Star Combo").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("harass", "Harass").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("wardJump", "Ward Jump").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("ljclr", "Lane/Jungle Clear").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem("ksbrdr", "Kill Steal Baron/Dragon").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Combo Settings", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem("qusage", "Use Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem("wusage", "Use W").SetValue(false));
            Config.SubMenu("csettings").AddItem(new MenuItem("autowusage", "Use W If Hp").SetValue(new Slider(50, 1)));
            Config.SubMenu("csettings").AddItem(new MenuItem("eusage", "Use E").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem("rusage", "Use R To Finish").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem("ignite", "Auto Ignite If Killable").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem("iusage", "Use Item").SetValue(true));

            Config.AddSubMenu(new Menu("Insec Settings", "insettings"));
            Config.SubMenu("insettings").AddItem(new MenuItem("insecMode", "Mode:").SetValue(new StringList(new[] { "Selected Ally", "Nearest Tower" })));
            string[] enemylist = ObjectManager.Get<Obj_AI_Hero>().Where(i => i != null && i.IsValid && i.IsEnemy).Select(i => i.ChampionName).ToArray();
            Config.SubMenu("insettings").AddItem(new MenuItem("insecenemy", "Insec Target:").SetValue(new StringList(enemylist))).DontSave();
            string[] allylist = ObjectManager.Get<Obj_AI_Hero>().Where(i => i != null && i.IsValid && i.IsAlly && !i.IsMe).Select(i => i.ChampionName).ToArray();
            Config.SubMenu("insettings").AddItem(new MenuItem("insecally", "To Ally:").SetValue(new StringList(allylist))).DontSave();
            Config.SubMenu("insettings").AddItem(new MenuItem("wjump", "Ward Jump To Insec").SetValue(true));
            Config.SubMenu("insettings").AddItem(new MenuItem("wflash", "Flash If Ward Jump Not Ready").SetValue(true));
            Config.SubMenu("insettings").AddItem(new MenuItem("pflash", "Prioritize Flash To Insec").SetValue(false));

            Config.AddSubMenu(new Menu("Misc Settings", "miscs"));
            //Config.SubMenu("miscs").AddItem(new MenuItem("smite", "Auto Smite Collision Minion").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem("skin", "Use Custom Skin").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem("skin1", "Skin Changer").SetValue(new Slider(4, 1, 7)));
            Config.SubMenu("miscs").AddItem(new MenuItem("packetCast", "Use Packet To Cast").SetValue(true));

            Config.AddSubMenu(new Menu("Ultimate Settings", "useUlt"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(i => i != null && i.IsValid && i.IsEnemy))
            {
                Config.SubMenu("useUlt").AddItem(new MenuItem("ult" + enemy.ChampionName, "Use Ultimate On " + enemy.ChampionName).SetValue(true));
            }

            Config.AddSubMenu(new Menu("Lane/Jungle Clear Settings", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem("useClearQ", "Use Q").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem("useClearW", "Use W").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem("useClearE", "Use E").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem("useClearI", "Use Item").SetValue(true));

            Config.AddSubMenu(new Menu("Draw Settings", "DrawSettings"));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("drawInsec", "Insec Line").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("drawKillable", "Killable Text").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("DrawQ", "Q Range").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("DrawW", "W Range").SetValue(true));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("DrawE", "E Range").SetValue(false));
            Config.SubMenu("DrawSettings").AddItem(new MenuItem("DrawR", "R Range").SetValue(false));

            Config.AddToMainMenu();
            if (Config.Item("skin").GetValue<bool>())
            {
                GenModelPacket(Player.ChampionName, Config.Item("skin1").GetValue<Slider>().Value);
                lastSkin = Config.Item("skin1").GetValue<Slider>().Value;
            }
            ts = new TargetSelector(SkillQ.Range, TargetSelector.TargetingMode.NearMouse);
            ts.SetDrawCircleOfTarget(true);
            Game.OnGameUpdate += OnTick;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            GameObject.OnCreate += OnCreateObj;
            Drawing.OnDraw += OnDraw;
            Game.PrintChat("<font color = \"#33CCCC\">[Lee Sin] Master of Insec</font> <font color = \"#fff8e7\">Brian v" + Assembly.GetExecutingAssembly().GetName().Version + "</font>");
        }

        static void Orbwalk(Obj_AI_Base target)
        {
            Orbwalking.Orbwalk(target, Game.CursorPos, Config.Item("ExtraWindup").GetValue<Slider>().Value, Config.Item("HoldPosRadius").GetValue<Slider>().Value);
        }

        static void ModeFocus()
        {
            switch (Config.Item("Focus").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.AutoPriority);
                    break;
                case 1:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.Closest);
                    break;
                case 2:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.LessAttack);
                    break;
                case 3:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.LessCast);
                    break;
                case 4:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.LowHP);
                    break;
                case 5:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.MostAD);
                    break;
                case 6:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.MostAP);
                    break;
                case 7:
                    ts.SetTargetingMode(TargetSelector.TargetingMode.NearMouse);
                    break;
            }
        }

        static InventorySlot wardSlot()
        {
            Int32[] wardIds = { 3340, 3350, 3205, 3207, 2049, 2045, 2044, 3361, 3154, 3362, 3160, 2043 };
            InventorySlot ward = null;
            foreach (var wardId in wardIds)
            {
                ward = Player.InventoryItems.FirstOrDefault(i => i.Id == (ItemId)wardId);
                if (ward != null && Player.Spellbook.Spells.First(i => (Int32)i.Slot == ward.Slot + 4).State == SpellState.Ready) return ward;
            }
            return ward;
        }

        static void OnTick(EventArgs args)
        {
            //Game.PrintChat(Convert.ToInt32(Proc.TotalProcessorTime.TotalSeconds).ToString());
            FReady = (FData != null && FData.Slot != SpellSlot.Unknown && FData.State == SpellState.Ready);
            SReady = (SData != null && SData.Slot != SpellSlot.Unknown && SData.State == SpellState.Ready);
            IReady = (IData != null && IData.Slot != SpellSlot.Unknown && IData.State == SpellState.Ready);
            TiamatReady = Items.CanUseItem(Tiamat);
            HydraReady = Items.CanUseItem(Hydra);
            BladeReady = Items.CanUseItem(Blade);
            BilgeReady = Items.CanUseItem(Bilge);
            RandReady = Items.CanUseItem(Rand);
            ModeFocus();
            if (Player.IsDead) return;
            PacketCast = Config.Item("packetCast").GetValue<bool>();
            useSight = wardSlot();
            targetObj = ts.Target;
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(i => i != null && i.IsValid && i.IsEnemy && i.ChampionName == Config.Item("insecenemy").GetValue<StringList>().SList[Config.Item("insecenemy").GetValue<StringList>().SelectedIndex]))
            {
                if (!enemy.IsDead)
                {
                    insecObj = (targetObj.IsValidTarget() && Player.Distance(enemy) > Player.Distance(targetObj)) ? targetObj : enemy;
                }
                else insecObj = targetObj;
            }
            switch (Config.Item("insecMode").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    float allydist = 9999;
                    foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(i => i != null && i.IsValid && i.IsAlly && !i.IsMe))
                    {
                        if (!ally.IsDead && ally.ChampionName == Config.Item("insecally").GetValue<StringList>().SList[Config.Item("insecally").GetValue<StringList>().SelectedIndex])
                        {
                            friendlyObj = ally;
                        }
                        else if (!ally.IsDead && Player.Distance(ally) < allydist)
                        {
                            allydist = Player.Distance(ally);
                            friendlyObj = ally;
                        }
                    }
                    break;
                case 1:
                    float turretdist = 9999;
                    foreach (var turret in ObjectManager.Get<Obj_AI_Turret>().Where(i => i != null && i.IsValid && i.IsAlly && !i.IsMe))
                    {
                        if (!turret.IsDead && Player.Distance(turret) < turretdist)
                        {
                            turretdist = Player.Distance(turret);
                            turretObj = turret;
                        }
                    }
                    break;
            }
            if (Config.Item("insecMake").GetValue<KeyBind>().Active)
            {
                Insec();
                return;
            }
            if (Config.Item("scriptActive").GetValue<KeyBind>().Active) NormalCombo();
            if (Config.Item("starActive").GetValue<KeyBind>().Active) StarCombo();
            if (Config.Item("wardJump").GetValue<KeyBind>().Active) WardJump(Game.CursorPos, 600);
            if (Config.Item("harass").GetValue<KeyBind>().Active) Harass();
            if (Config.Item("ljclr").GetValue<KeyBind>().Active) LaneJungClear();
            if (Config.Item("ksbrdr").GetValue<KeyBind>().Active) KillStealBrDr();
            if (Config.Item("skin").GetValue<bool>() && skinChanged())
            {
                GenModelPacket(Player.ChampionName, Config.Item("skin1").GetValue<Slider>().Value);
                lastSkin = Config.Item("skin1").GetValue<Slider>().Value;
            }
        }

        static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "BlindMonkQOne") lastTimeQ = Environment.TickCount;
            }
        }

        static void OnCreateObj(GameObject sender, EventArgs args)
        {
            if (Player.IsDead) return;
            if (Config.Item("wardJump").GetValue<KeyBind>().Active || Config.Item("insecMake").GetValue<KeyBind>().Active || Config.Item("wjump").GetValue<bool>() || Config.Item("wflash").GetValue<bool>() || Config.Item("pflash").GetValue<bool>())
            {
                if (sender != null && sender.IsValid && (sender.Name == "VisionWard" || sender.Name == "SightWard")) lastWard = (Obj_AI_Base)sender;
            }
        }

        static void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Config.Item("DrawQ").GetValue<bool>() && SkillQ.Level > 0) Utility.DrawCircle(Player.Position, SkillQ.Range, SkillQ.IsReady() ? Color.Green : Color.Red);
            if (Config.Item("DrawW").GetValue<bool>() && SkillW.Level > 0) Utility.DrawCircle(Player.Position, SkillW.Range, SkillW.IsReady() ? Color.Green : Color.Red);
            if (Config.Item("DrawE").GetValue<bool>() && SkillE.Level > 0) Utility.DrawCircle(Player.Position, SkillE.Range, SkillE.IsReady() ? Color.Green : Color.Red);
            if (Config.Item("DrawR").GetValue<bool>() && SkillR.Level > 0) Utility.DrawCircle(Player.Position, SkillR.Range, SkillR.IsReady() ? Color.Green : Color.Red);
            if (Config.Item("drawInsec").GetValue<bool>() && SkillR.IsReady() && ((SkillW.IsReady() && useSight != null) || FReady))
            {
                Byte validTargets = 0;
                switch (Config.Item("insecMode").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        if (insecObj.IsValidTarget())
                        {
                            Utility.DrawCircle(insecObj.Position, 70, Color.FromArgb(0, 204, 0));
                            validTargets += 1;
                        }
                        if (friendlyObj != null && friendlyObj.IsValid)
                        {
                            Utility.DrawCircle(friendlyObj.Position, 70, Color.FromArgb(0, 204, 0));
                            validTargets += 1;
                        }
                        if (validTargets == 2)
                        {
                            var pos = ReverseVector(insecObj.Position, friendlyObj.Position, insecObj.IsValidTarget(SkillQ.Range) ? 800 : 300);
                            Drawing.DrawLine(Drawing.WorldToScreen(pos), Drawing.WorldToScreen(insecObj.Position), 2, Color.White);
                        }
                        break;
                    case 1:
                        if (insecObj.IsValidTarget())
                        {
                            Utility.DrawCircle(insecObj.Position, 70, Color.FromArgb(0, 204, 0));
                            validTargets += 1;
                        }
                        if (turretObj != null && turretObj.IsValid)
                        {
                            Utility.DrawCircle(turretObj.Position, 70, Color.FromArgb(0, 204, 0));
                            validTargets += 1;
                        }
                        if (validTargets == 2)
                        {
                            var pos = ReverseVector(insecObj.Position, turretObj.Position, insecObj.IsValidTarget(SkillQ.Range) ? 800 : 300);
                            Drawing.DrawLine(Drawing.WorldToScreen(pos), Drawing.WorldToScreen(insecObj.Position), 2, Color.White);
                        }
                        break;
                }
            }
            if (Config.Item("drawKillable").GetValue<bool>())
            {
                foreach (var killableObj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget()))
                {
                    var dmgTotal = Player.GetAutoAttackDamage(killableObj);
                    if (SkillQ.IsReady() && QData.Name == "BlindMonkQOne") dmgTotal += SkillQ.GetDamage(killableObj);
                    if (SkillR.IsReady() && Config.Item("ult" + killableObj.ChampionName).GetValue<bool>()) dmgTotal += SkillR.GetDamage(killableObj);
                    if (SkillE.IsReady() && EData.Name == "BlindMonkEOne") dmgTotal += SkillE.GetDamage(killableObj);
                    if (SkillQ.IsReady() && killableObj.HasBuff("BlindMonkQOne", true)) dmgTotal += GetQ2Dmg(killableObj, dmgTotal);
                    if (killableObj.Health < dmgTotal)
                    {
                        var posText = Drawing.WorldToScreen(killableObj.Position);
                        Drawing.DrawText(posText.X - 20, posText.Y - 5, Color.White, "Killable");
                    }
                }
            }
        }

        static Vector3 ReverseVector(Vector3 from, Vector3 to, float distance)
        {
            var X = from.X + (distance / from.Distance(to)) * (to.X - from.X);
            var Y = from.Y + (distance / from.Distance(to)) * (to.Y - from.Y);
            return new Vector3(X, Y, to.Z);
        }

        static void KillStealBrDr()
        {
            var minionObj = MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.NotAlly).FirstOrDefault(i => i.Name == "Worm12.1.1" || i.Name == "Dragon6.1.1");
            Orbwalk(minionObj);
            if (minionObj == null) return;
            if (SkillQ.IsReady() && !SReady && minionObj.Health < GetQ2Dmg(minionObj, SkillQ.GetDamage(minionObj)))
            {
                if (QData.Name == "BlindMonkQOne")
                {
                    SkillQ.Cast(minionObj, PacketCast);
                }
                else if (minionObj.HasBuff("BlindMonkQOne", true)) SkillQ.Cast();
            }
            if (SkillQ.IsReady() && SReady && minionObj.Health < GetQ2Dmg(minionObj, SkillQ.GetDamage(minionObj) + Player.GetSummonerSpellDamage(minionObj, Damage.SummonerSpell.Smite)))
            {
                if (QData.Name == "BlindMonkQOne")
                {
                    SkillQ.Cast(minionObj, PacketCast);
                }
                else if (minionObj.HasBuff("BlindMonkQOne", true))
                {
                    SkillQ.Cast();
                    Player.SummonerSpellbook.CastSpell(SData.Slot, minionObj);
                }
            }
            if (SReady && minionObj.IsValidTarget(SData.SData.CastRange[0]) && minionObj.Health < Player.GetSummonerSpellDamage(minionObj, Damage.SummonerSpell.Smite)) Player.SummonerSpellbook.CastSpell(SData.Slot, minionObj);
        }

        static bool CheckingCollision(Obj_AI_Hero target)
        {
            //foreach (var minion in MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.Enemy).Where(i => i.Distance(Player) >= 550 && i.Health > SkillQ.GetDamage(i)))
            //{
            //    var collision = SkillQ.GetPrediction(minion).CollisionObjects;
            //    if (collision.Count == 0)
            //    {
            //        return false;
            //    }
            //    else if (collision.Count == 1 && collision.First().IsMinion && minion.ServerPosition.To2D().Distance(Player.Position.To2D(), target.Position.To2D(), true) < 80 && SReady && collision.First().Health <= Player.GetSummonerSpellDamage(collision.First(), Damage.SummonerSpell.Smite))
            //    {
            //        Player.SummonerSpellbook.CastSpell(SData.Slot, collision.First());
            //        return true;
            //    }
            //}
            return false;
        }

        static void Harass()
        {
            Orbwalk(targetObj);
            if (targetObj == null) return;
            if (SkillQ.IsReady())
            {
                if (QData.Name == "BlindMonkQOne")
                {
                    SkillQ.Cast(targetObj, PacketCast);
                }
                else if (targetObj.HasBuff("BlindMonkQOne", true) && SkillW.IsReady() && WData.Name == "BlindMonkWOne" && Player.Mana >= 130 && (Player.Health / Player.MaxHealth) >= 0.3)
                {
                    var nearJumper = (Obj_AI_Base)ObjectManager.Get<Obj_AI_Minion>().Where(i => i.IsValid && i.IsAlly && i.Distance(Player) <= 600 && i.Distance(targetObj) <= SkillW.Range).FirstOrDefault() ?? ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValid && i.IsAlly && !i.IsMe && i.Distance(Player) <= 600 && i.Distance(targetObj) <= SkillW.Range).FirstOrDefault();
                    if (nearJumper != null) SkillQ.Cast();
                }
            }
            if (!SkillQ.IsReady() && SkillE.IsReady() && EData.Name == "BlindMonkEOne" && targetObj.IsValidTarget(SkillE.Range)) SkillE.Cast();
            if (!SkillQ.IsReady() && targetObj.HasBuff("BlindMonkEOne", true) && SkillW.IsReady() && WData.Name == "BlindMonkWOne")
            {
                var jumpObj = ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValid && i.IsAlly && !i.IsMe && i.Distance(Player.ServerPosition) <= SkillW.Range && i.Distance(Player) <= 1000).OrderByDescending(i => i.Distance(targetObj)).FirstOrDefault() ?? (Obj_AI_Base)ObjectManager.Get<Obj_AI_Minion>().Where(i => i.IsValid && i.IsAlly && i.Distance(Player.ServerPosition) <= SkillW.Range && i.Distance(Player) <= 1000).OrderByDescending(i => i.Distance(targetObj)).FirstOrDefault();
                if (jumpObj != null && (Environment.TickCount - lastTimeJump) > 1000)
                {
                    SkillW.Cast(jumpObj, PacketCast);
                    lastTimeJump = Environment.TickCount;
                }
            }
        }

        static void WardJump(Vector3 Pos, float dist)
        {
            if ((SkillW.IsReady() && WData.Name != "BlindMonkWOne") || !SkillW.IsReady())
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Pos);
                return;
            }
            bool Jumped = false;
            if (Player.Distance(Pos) > dist) Pos = ReverseVector(Player.Position, Pos, dist);
            if (lastWard != null && lastWard.IsValid && lastWard.Distance(Pos) <= 200)
            {
                Jumped = true;
                Player.IssueOrder(GameObjectOrder.MoveTo, Pos);
                if (Player.Distance(lastWard) <= SkillW.Range + lastWard.BoundingRadius)
                {
                    if ((Environment.TickCount - lastTimeJump) > 1000)
                    {
                        SkillW.Cast(lastWard, PacketCast);
                        lastTimeJump = Environment.TickCount;
                    }
                }
            }
            if (!Jumped && useSight != null)
            {
                if ((Environment.TickCount - lastTimeWard) > 1000)
                {
                    useSight.UseItem(Pos);
                    lastTimeWard = Environment.TickCount;
                }
            }
        }

        static void Insec()
        {
            Orbwalk(targetObj);
            if (insecObj.IsValidTarget(SkillQ.Range))
            {
                if (SkillQ.IsReady())
                {
                    if (QData.Name == "BlindMonkQOne")
                    {
                        CheckingCollision(targetObj);
                        //if (SkillQ.GetPrediction(insecObj).Hitchance >= HitChance.High)
                        //{
                        SkillQ.Cast(insecObj, PacketCast);
                            return;
                        //}
                        //var prediction = SkillQ.GetPrediction(insecObj);
                        //if (prediction.Hitchance == HitChance.Collision && SReady && Config.Item("smite").GetValue<bool>())
                        //{
                        //    var collision = prediction.CollisionObjects.Where(i => i.NetworkId != Player.NetworkId).OrderBy(i => i.Distance(Player)).FirstOrDefault();
                        //    if (collision.Distance(prediction.UnitPosition) < 200 && collision.IsValidTarget(SData.SData.CastRange[0]) && collision.Health < Damage.GetSummonerSpellDamage(Player, collision, Damage.SummonerSpell.Smite))
                        //    {
                        //        Player.SummonerSpellbook.CastSpell(SData.Slot, collision);
                        //        SkillQ.Cast(prediction.CastPosition, true);
                        //        return;
                        //    }
                        //}
                        //else if (prediction.Hitchance >= HitChance.Medium)
                        //{
                        //    SkillQ.Cast(prediction.CastPosition, true);
                        //    return;
                        //}
                    }
                    else if (insecObj.HasBuff("BlindMonkQOne", true))
                    {
                        lastTimeInsec = Environment.TickCount + 150;
                        SkillQ.Cast();
                        return;
                    }
                }
                if (Config.Item("wjump").GetValue<bool>() && !Config.Item("pflash").GetValue<bool>() && WardJumpInsec()) return;
                if (Config.Item("wjump").GetValue<bool>() && !Config.Item("pflash").GetValue<bool>() && Config.Item("wflash").GetValue<bool>() && FlashInsec()) return;
                if (Config.Item("pflash").GetValue<bool>() && FlashInsec()) return;
                if (Config.Item("pflash").GetValue<bool>() && Config.Item("wjump").GetValue<bool>() && !FReady && WardJumpInsec()) return;
            }
        }

        static bool WardJumpInsec()
        {
            if (insecObj.IsValidTarget(400))
            {
                switch (Config.Item("insecMode").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        if (SkillR.IsReady() && friendlyObj != null && friendlyObj.IsValid)
                        {
                            if (insecObj.IsValidTarget(SkillR.Range))
                            {
                                var pos = ReverseVector(Player.Position, insecObj.Position, insecObj.Distance(Player) + 500);
                                var newDistance = friendlyObj.Distance(insecObj) - friendlyObj.Distance(pos);
                                if (newDistance > 0 && (newDistance / 500) > 0.7)
                                {
                                    SkillR.Cast(insecObj, PacketCast);
                                    return true;
                                }
                            }
                            if (SkillW.IsReady() && WData.Name == "BlindMonkWOne" && Environment.TickCount > lastTimeInsec)
                            {
                                if ((Environment.TickCount - lastTimeJump) < 1000 && (Environment.TickCount - lastTimeJump) >= 10)
                                {
                                    SkillW.Cast(lastWard, PacketCast);
                                    lastTimeInsec = Environment.TickCount + 500;
                                    return true;
                                }
                                else if (useSight != null)
                                {
                                    if ((Environment.TickCount - lastTimeWard) > 350)
                                    {
                                        var targetObj2 = Prediction.GetPrediction(insecObj, 0.25f, 2000).UnitPosition;
                                        var pos = ReverseVector(friendlyObj.Position, targetObj2, targetObj2.Distance(friendlyObj.Position) + 300);
                                        if (Player.Distance(pos) < 600)
                                        {
                                            useSight.UseItem(pos);
                                            lastTimeWard = Environment.TickCount;
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case 1:
                        if (SkillR.IsReady() && turretObj != null && turretObj.IsValid)
                        {
                            if (insecObj.IsValidTarget(SkillR.Range))
                            {
                                var pos = ReverseVector(Player.Position, insecObj.Position, insecObj.Distance(Player) + 500);
                                var newDistance = turretObj.Distance(insecObj) - turretObj.Distance(pos);
                                if (newDistance > 0 && (newDistance / 500) > 0.7)
                                {
                                    SkillR.Cast(insecObj, PacketCast);
                                    return true;
                                }
                            }
                            if (SkillW.IsReady() && WData.Name == "BlindMonkWOne" && Environment.TickCount > lastTimeInsec)
                            {
                                if ((Environment.TickCount - lastTimeJump) < 1000 && (Environment.TickCount - lastTimeJump) >= 10)
                                {
                                    SkillW.Cast(lastWard, PacketCast);
                                    lastTimeInsec = Environment.TickCount + 500;
                                    return true;
                                }
                                else if (useSight != null)
                                {
                                    if ((Environment.TickCount - lastTimeWard) > 350)
                                    {
                                        var targetObj2 = Prediction.GetPrediction(insecObj, 0.25f, 2000).UnitPosition;
                                        var pos = ReverseVector(turretObj.Position, targetObj2, targetObj2.Distance(turretObj.Position) + 300);
                                        if (Player.Distance(pos) < 600)
                                        {
                                            useSight.UseItem(pos);
                                            lastTimeWard = Environment.TickCount;
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }
            return false;
        }

        static bool FlashInsec()
        {
            if (insecObj.IsValidTarget(400) && ((SkillW.IsReady() && useSight == null) || (!SkillW.IsReady() && useSight != null) || (!SkillW.IsReady() && useSight == null)))
            {
                switch (Config.Item("insecMode").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        if (SkillR.IsReady() && friendlyObj != null && friendlyObj.IsValid)
                        {
                            if (insecObj.IsValidTarget(SkillR.Range))
                            {
                                var pos = ReverseVector(Player.Position, insecObj.Position, insecObj.Distance(Player) + 500);
                                var newDistance = friendlyObj.Distance(insecObj) - friendlyObj.Distance(pos);
                                if (newDistance > 0 && (newDistance / 500) > 0.7)
                                {
                                    SkillR.Cast(insecObj, PacketCast);
                                    return true;
                                }
                            }
                            if (FReady && Environment.TickCount > lastTimeInsec)
                            {
                                //var targetObj2 = Prediction.GetPrediction(insecObj, 0.25f, 2000).UnitPosition;
                                //var pos = ReverseVector(friendlyObj.Position, targetObj2, targetObj2.Distance(friendlyObj.Position) + 400);
                                //if (Player.Distance(pos) < FData.SData.CastRange[0])
                                //{
                                //    Player.SummonerSpellbook.CastSpell(FData.Slot, pos);
                                //    lastTimeInsec = Environment.TickCount + 350;
                                //    return true;
                                //}
                                if ((Environment.TickCount - lastTimeJump) < 1000 && (Environment.TickCount - lastTimeJump) >= 10)
                                {
                                    lastTimeInsec = Environment.TickCount + 500;
                                    return true;
                                }
                                else
                                {
                                    var targetObj2 = Prediction.GetPrediction(insecObj, 0.25f, 2000).UnitPosition;
                                    var pos = ReverseVector(friendlyObj.Position, targetObj2, targetObj2.Distance(friendlyObj.Position) + 400);
                                    if (Player.Distance(pos) < FData.SData.CastRange[0])
                                    {
                                        Player.SummonerSpellbook.CastSpell(FData.Slot, pos);
                                        return true;
                                    }
                                }
                            }
                        }
                        break;
                    case 1:
                        if (SkillR.IsReady() && turretObj != null && turretObj.IsValid)
                        {
                            if (insecObj.IsValidTarget(SkillR.Range))
                            {
                                var pos = ReverseVector(Player.Position, insecObj.Position, insecObj.Distance(Player) + 500);
                                var newDistance = turretObj.Distance(insecObj) - turretObj.Distance(pos);
                                if (newDistance > 0 && (newDistance / 500) > 0.7)
                                {
                                    SkillR.Cast(insecObj, PacketCast);
                                    return true;
                                }
                            }
                            if (FReady && Environment.TickCount > lastTimeInsec)
                            {
                                //var targetObj2 = Prediction.GetPrediction(insecObj, 0.25f, 2000).UnitPosition;
                                //var pos = ReverseVector(turretObj.Position, targetObj2, targetObj2.Distance(turretObj.Position) + 400);
                                //if (Player.Distance(pos) < FData.SData.CastRange[0])
                                //{
                                //    Player.SummonerSpellbook.CastSpell(FData.Slot, pos);
                                //    lastTimeInsec = Environment.TickCount + 350;
                                //    return true;
                                //}
                                if ((Environment.TickCount - lastTimeJump) < 1000 && (Environment.TickCount - lastTimeJump) >= 10)
                                {
                                    lastTimeInsec = Environment.TickCount + 500;
                                    return true;
                                }
                                else
                                {
                                    var targetObj2 = Prediction.GetPrediction(insecObj, 0.25f, 2000).UnitPosition;
                                    var pos = ReverseVector(turretObj.Position, targetObj2, targetObj2.Distance(turretObj.Position) + 400);
                                    if (Player.Distance(pos) < FData.SData.CastRange[0])
                                    {
                                        Player.SummonerSpellbook.CastSpell(FData.Slot, pos);
                                        return true;
                                    }
                                }
                            }
                        }
                        break;
                }
            }
            return false;
        }

        static void NormalCombo()
        {
            Orbwalk(targetObj);
            if (targetObj == null) return;
            if (Config.Item("eusage").GetValue<bool>() && SkillE.IsReady() && EData.Name == "BlindMonkEOne" && targetObj.IsValidTarget(SkillE.Range)) SkillE.Cast();
            if (Config.Item("qusage").GetValue<bool>() && SkillQ.IsReady() && QData.Name == "BlindMonkQOne") SkillQ.Cast(targetObj, PacketCast);
            if (Config.Item("qusage").GetValue<bool>() && SkillQ.IsReady() && targetObj.HasBuff("BlindMonkQOne", true))
            {
                if (Player.Distance(targetObj) > 500 || targetObj.Health < SkillQ.GetDamage(targetObj, 1) || (targetObj.HasBuff("BlindMonkEOne", true) && targetObj.IsValidTarget(SkillE.Range)) || (Environment.TickCount - lastTimeQ) > 1500) SkillQ.Cast();
            }
            if (Config.Item("eusage").GetValue<bool>() && SkillE.IsReady() && targetObj.HasBuff("BlindMonkEOne", true) && targetObj.IsValidTarget(SkillE.Range)) SkillE.Cast();
            if (Config.Item("rusage").GetValue<bool>() && Config.Item("ult" + targetObj.ChampionName).GetValue<bool>() && SkillR.IsReady() && targetObj.IsValidTarget(SkillR.Range))
            {
                if (SkillR.IsKillable(targetObj) || (targetObj.Health < GetQ2Dmg(targetObj, SkillR.GetDamage(targetObj)) && targetObj.HasBuff("BlindMonkQOne", true) && SkillQ.IsReady() && Player.Mana >= 50)) SkillR.Cast(targetObj, PacketCast);
            }
            if (Config.Item("wusage").GetValue<bool>() && SkillW.IsReady() && targetObj.IsValidTarget(SkillE.Range) && (Player.Health * 100 / Player.MaxHealth) <= Config.Item("autowusage").GetValue<Slider>().Value)
            {
                if (WData.Name == "BlindMonkWOne")
                {
                    SkillW.Cast(Player, PacketCast);
                }
                else if (!Player.HasBuff("blindmonkwoneshield", true)) SkillW.Cast();
            }
            if (Config.Item("iusage").GetValue<bool>()) UseItem(targetObj);
            if (Config.Item("ignite").GetValue<bool>()) CastIgnite(targetObj);
            //var prediction = SkillQ.GetPrediction(targetObj);
            //if (prediction.Hitchance == HitChance.Collision && SReady && Config.Item("smite").GetValue<bool>())
            //{
            //    var collision = prediction.CollisionObjects.Where(i => i.NetworkId != Player.NetworkId).OrderBy(i => i.Distance(Player)).FirstOrDefault();
            //    if (collision.Distance(prediction.UnitPosition) < 200 && collision.IsValidTarget(SData.SData.CastRange[0]) && collision.Health < Damage.GetSummonerSpellDamage(Player, collision, Damage.SummonerSpell.Smite))
            //    {
            //        Player.SummonerSpellbook.CastSpell(SData.Slot, collision);
            //        SkillQ.Cast(prediction.CastPosition, true);
            //        return;
            //    }
            //}
            //else if (prediction.Hitchance >= HitChance.Medium)
            //{
            //    SkillQ.Cast(prediction.CastPosition, true);
            //    return;
            //}
        }

        static void StarCombo()
        {
            Orbwalk(targetObj);
            if (targetObj == null) return;
            if (SkillE.IsReady() && EData.Name == "BlindMonkEOne" && targetObj.IsValidTarget(SkillE.Range)) SkillE.Cast();
            if (SkillQ.IsReady() && QData.Name == "BlindMonkQOne") SkillQ.Cast(targetObj, PacketCast);
            if (!targetObj.IsValidTarget(SkillR.Range) && SkillR.IsReady() && targetObj.HasBuff("BlindMonkQOne", true) && targetObj.IsValidTarget(SkillW.Range)) WardJump(targetObj.Position, 600);
            UseItem(targetObj);
            if (SkillR.IsReady() && targetObj.HasBuff("BlindMonkQOne", true) && targetObj.IsValidTarget(SkillR.Range) && Player.Mana >= 50) SkillR.Cast(targetObj, PacketCast);
            if (!SkillR.IsReady() && targetObj.HasBuff("BlindMonkQOne", true) && Player.Distance(targetObj) > 350) SkillQ.Cast();
            if (!SkillR.IsReady() && targetObj.HasBuff("BlindMonkEOne", true) && targetObj.IsValidTarget(SkillE.Range)) SkillE.Cast();
            CastIgnite(targetObj);
        }

        static void CastIgnite(Obj_AI_Hero target)
        {
            if (IReady && target.IsValidTarget(IData.SData.CastRange[0]) && target.Health < Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite)) Player.SummonerSpellbook.CastSpell(IData.Slot, target);
        }

        static void UseItem(Obj_AI_Hero target)
        {
            if (BilgeReady && Player.Distance(target) <= 450) Items.UseItem(Bilge, target);
            if (BladeReady && Player.Distance(target) <= 450) Items.UseItem(Blade, target);
            if (TiamatReady && Utility.CountEnemysInRange(350) >= 1) Items.UseItem(Tiamat);
            if (HydraReady && (Utility.CountEnemysInRange(350) >= 2 || (Player.GetAutoAttackDamage(target) < target.Health && Utility.CountEnemysInRange(350) == 1))) Items.UseItem(Hydra);
            if (RandReady && Utility.CountEnemysInRange(450) >= 1) Items.UseItem(Rand);
        }

        static double GetQ2Dmg(Obj_AI_Base target, double dmgPlus)
        {
            Int32[] dmgQ = { 50, 80, 110, 140, 170 };
            return Player.CalcDamage(target, Damage.DamageType.Physical, dmgQ[SkillQ.Level] + 0.9 * Player.FlatPhysicalDamageMod + 0.08 * (target.MaxHealth - target.Health - dmgPlus));
        }

        static void GenModelPacket(string champ, int skinId)
        {
            Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(Player.NetworkId, skinId, champ)).Process();
        }

        static bool skinChanged()
        {
            return (Config.Item("skin1").GetValue<Slider>().Value != lastSkin);
        }

        static void LaneJungClear()
        {
            var minionLaneJung = MinionManager.GetMinions(Player.Position, SkillQ.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
            if (minionLaneJung.Count == 0)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                return;
            }
            Obj_AI_Base minionObj = null;
            foreach (var minion in minionLaneJung)
            {
                if (minionObj == null || Player.Distance(minionObj) > Player.Distance(minion)) minionObj = minion;
            }
            Orbwalk(minionObj);
            var Passive = Player.HasBuff("blindmonkpassive_cosmetic", true);
            if (Config.Item("useClearW").GetValue<bool>() && SkillW.IsReady() && minionObj.IsValidTarget(Orbwalking.GetRealAutoAttackRange(minionObj)))
            {
                if (WData.Name == "BlindMonkWOne")
                {
                    if (!Passive) SkillW.Cast(Player, PacketCast);
                }
                else if (!Passive || (Environment.TickCount - SkillW.LastCastAttemptT) > 2000 || !Player.HasBuff("blindmonkwoneshield", true)) SkillW.Cast();
            }
            if (Config.Item("useClearQ").GetValue<bool>() && SkillQ.IsReady())
            {
                if (QData.Name == "BlindMonkQOne")
                {
                    if (!Passive) SkillQ.Cast(minionObj, PacketCast);
                }
                else if (minionObj.HasBuff("BlindMonkQOne", true) && (!Passive || minionObj.Health < SkillQ.GetDamage(minionObj, 1) || (Environment.TickCount - SkillQ.LastCastAttemptT) > 2000 || Player.Distance(minionObj) >= 400)) SkillQ.Cast();
            }
            if (Config.Item("useClearE").GetValue<bool>() && SkillE.IsReady() && minionObj.IsValidTarget(SkillE.Range))
            {
                if (EData.Name == "BlindMonkEOne")
                {
                    if (!Passive) SkillE.Cast();
                }
                else if (minionObj.HasBuff("BlindMonkQOne", true) && (!Passive || (Environment.TickCount - SkillE.LastCastAttemptT) > 2000)) SkillE.Cast();
            }
            if (Config.Item("useClearI").GetValue<bool>() && Player.Distance(minionObj) <= 350)
            {
                if (TiamatReady) Items.UseItem(Tiamat);
                if (HydraReady) Items.UseItem(Hydra);
            }
        }
    }
}