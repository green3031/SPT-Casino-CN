using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Casino.Client
{
    /// <summary>
    /// SPT Casino: three tables behind one door.
    ///
    /// Blackjack, Poker and Roulette were three mods with three plugins, three
    /// task-bar tabs and three Harmony patches on the same method. This is all three,
    /// with one tab that opens a lobby you pick a table from.
    ///
    /// **The tables themselves are unchanged.** Their panels are compiled in from where
    /// they already live, not copied here, and not a line of them moved -- see the
    /// project file. That was possible because none of them ever referenced the task
    /// bar, the menu icon or the escape key; the only thing they reached outside
    /// themselves for was a log and a MonoBehaviour, which <c>Shims.cs</c> now provides.
    ///
    /// Each game still talks to its own server mod on its own routes. The money paths
    /// are untouched by this merge, deliberately: they are the part that took longest
    /// to get right and the part where a mistake costs somebody roubles.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class CasinoPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.mybutthasarash.sptcasino";
        public const string PluginName = "SPT Casino";
        public const string PluginVersion = "1.2.6";

        internal static ManualLogSource Log;

        /// <summary>
        /// The plugin itself, so code that is not a MonoBehaviour can start a coroutine
        /// and find the art beside the DLL. Every table asks for this.
        /// </summary>
        internal static CasinoPlugin Instance;

        /// <summary>
        /// Whether CASINO appears on the bar along the bottom of the menu.
        ///
        /// On by default, and it is the only way in: the bar is on every out-of-raid
        /// screen, which is the difference between reaching a table from the hideout
        /// and backing out of it first.
        /// </summary>
        internal static ConfigEntry<bool> ShowTaskBarTab;

        /// <summary>
        /// Which end of the bar the tab sits on. Left by default, with MAIN MENU and
        /// HIDEOUT -- those are places you go, which is what the casino is.
        /// </summary>
        internal static ConfigEntry<bool> TabOnRight;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            // Where the art is and where to log, for the shared drawing code.
            Casino.Shared.Host.Plugin = this;
            Casino.Shared.Host.Log = Logger;

            // The tables were written against their own plugins. One plugin now, so it
            // answers to all three names. See Shims.cs.
            Roulette.Client.RouletteClientPlugin.Instance = this;
            Roulette.Client.RouletteClientPlugin.Log = Logger;
            Poker.Client.PokerClientPlugin.Instance = this;
            Poker.Client.PokerClientPlugin.Log = Logger;
            Blackjack.Client.BlackjackClientPlugin.Instance = this;
            Blackjack.Client.BlackjackClientPlugin.Log = Logger;
            SlotMachine.Client.SlotClientPlugin.Instance = this;
            SlotMachine.Client.SlotClientPlugin.Log = Logger;

            ShowTaskBarTab = Config.Bind(
                "菜单",
                "显示任务栏选项卡",
                true,
                "在菜单底部的任务栏加上「CASINO」，这样在藏身处、 "
                + "跳蚤市场或商人界面也能打开这些牌桌，而不只是主菜单。");

            TabOnRight = Config.Bind(
                "菜单",
                "把选项卡放在右侧",
                false,
                "让选项卡排在「角色」那一侧，而不是紧挨着「主菜单」和「藏身处」。 "
                + "改动之后选项卡会在一两秒才移动位置。");

            Poker.Client.PokerClientPlugin.BuyIn = Config.Bind(
                "扑克",
                "买入",
                1_000_000,
                new ConfigDescription(
                    "在扑克牌桌坐下需要多少卢布，以及 "
                    + "这些钱能换成多大一堆筹码。无论这里设成多少， "
                    + "盲注都固定为 10,000 / 20,000，所以买入越小只是筹码越浅、 "
                    + "牌局越热闹，而不是越便宜。括号里的数字表示 "
                    + "这样买入之后你相当于有多少个大盲注的深度。",
                    new AcceptableValueList<int>(
                        200_000,      // 10 big blinds
                        500_000,      // 25
                        1_000_000,    // 50
                        1_500_000,    // 75
                        2_000_000,    // 100
                        3_000_000,    // 150
                        4_000_000,    // 200
                        5_000_000))); // 250

            SlotMachine.Client.SlotClientPlugin.NoStakeCap = Config.Bind(
                "老虎机",
                "取消下注上限",
                false,
                "默认关闭。老虎机把单次旋转限制在 50,000 卢布（或 500 "
                + "美元/欧元）以内，因为它的赔付最高可达下注额的一千倍， "
                + "而封顶后的奖金依然有 50,000,000。开启此项即可 "
                + "随意下注。最低下注额不受影响。");

            Blackjack.Client.BlackjackClientPlugin.EnforceTableMaximum = Config.Bind(
                "21点",
                "强制牌桌上限",
                true,
                "拒绝超过牌桌限额的赌注，而不是悄悄削减。");

            try
            {
                new Harmony(PluginGuid).PatchAll(typeof(CasinoEscape));
                CasinoEscape.Applied = true;
            }
            catch (System.Exception ex)
            {
                // Escape still works without this -- Update below watches for the key.
                // What is lost is swallowing it, so the screen underneath backs out too.
                Log.LogError("[Casino] escape will also close the screen behind the casino: " + ex.Message);
            }

            // The tab is not a patch. It watches for the bar instead, because the bar
            // has to be found again after every raid and after any mod that rebuilds
            // the row, and a poll notices both without naming a method that could be
            // renamed.
            StartCoroutine(CasinoTab.Heartbeat());

            Log.LogInfo($"[赌场] 客户端已加载 -- {Games.All.Count} 张牌桌");

            WarnAboutRetiredMods();
        }

        /// <summary>
        /// The mods this one replaced, by the GUID each registered under.
        ///
        /// Blackjack and Poker were both released standalone before the merge, so installs
        /// of them are out there. **Extracting the casino over one does not remove it** --
        /// an archive cannot delete anything, and `pack.ps1`, which retires those folders,
        /// is a build script that has never shipped. What the player is left with is a
        /// task-bar tab per leftover plugin and a second copy of the same server routes,
        /// with nothing anywhere saying why.
        ///
        /// Saying so is all this does, deliberately. A mod that deleted another mod's
        /// files would be a worse thing to ship than the duplicate tab it tidied up.
        /// </summary>
        private static readonly string[][] RetiredMods =
        {
            new[] { "com.mybutthasarash.blackjack", "Blackjack" },
            new[] { "com.mybutthasarash.poker", "Poker" },
            new[] { "com.mybutthasarash.roulette", "Roulette" },
        };

        private static void WarnAboutRetiredMods()
        {
            var found = new System.Collections.Generic.List<string>();

            foreach (var mod in RetiredMods)
            {
                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(mod[0]))
                {
                    found.Add(mod[1]);
                }
            }

            if (found.Count == 0)
            {
                return;
            }

            var names = string.Join(", ", found.ToArray());

            // An error rather than a warning: this is the state an upgrade is most likely
            // to go wrong in, and it is the line somebody will be asked to go and find.
            Log.LogError(
                $"[Casino] {names} {(found.Count == 1 ? "is" : "are")} still installed as "
                + $"{(found.Count == 1 ? "a separate mod" : "separate mods")}, and this one "
                + $"already includes {(found.Count == 1 ? "that table" : "those tables")}. "
                + "Expect a duplicate task-bar tab for each. Delete them from BepInEx/plugins "
                + "and from SPT_Runtime/user/mods, then restart. Nothing is lost by doing so: "
                + "anything the house owes you is read out of the old folder and paid anyway.");
        }

        private void Update()
        {
            if (!CasinoEscape.Applied && Input.GetKeyDown(KeyCode.Escape) && CasinoLobby.Anything)
            {
                CasinoEscape.Back();
            }

            // Shut at the first hint of a raid. The panels sit at a high sorting order
            // behind a nearly opaque backdrop that swallows every click, so a table
            // that outlives the menu locks the player out of their own raid.
            if (CasinoLobby.Anything && CasinoTab.InRaid)
            {
                CasinoLobby.CloseEverything();
            }
        }
    }
}
