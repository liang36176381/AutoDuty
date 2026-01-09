using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using System.Collections.Generic;
using System.Linq;

namespace AutoDuty.Helpers
{
    using Data;
    using IPC;
    using static Data.Classes;

    internal static class LevelingHelper
    {
        private static Content[] levelingDuties = [];

        public static void ResetLevelingDuties() =>
            levelingDuties = [];

        public static readonly uint[] levelingList =
        [
            1036u, // 15 Sastasha
            1037u, // 16 TamTara Deepcroft
            1039u, // 24 The Thousand Maws of Toto-Rak
            1041u, // 32 Brayflox's Longstop
            1042u, // 41 Stone Vigil
            
            1142u, // 61 Sirensong Sea
            1144u, // 67 Doma Castle
            1145u, // 69 Castrum Abania
            837u,  // 71 Holminster
            823u,  // 75 Qitana
            822u,  // 79 Mt. Gulg
            952u,  // 81 Tower of Zot
            969u,  // 83 Tower of Babil
            974u,  // 87 Ktisis Hyperboreia
            1167u, // 91 Ihuykatumu
            1193u, // 93 Worqor Zormor
            1194u, // 95 The Skydeep Cenote
            1198u, // 97 Vanguard
            1208u, // 99 Origenics
        ];

        public static readonly uint[] levelingListExperimental =
        [
            821u, // 73 Dohn Mheg
            836u, // 77 Malikah's Well
        ];

        internal static Content[] LevelingDuties
        {
            get
            {
                if (levelingDuties.Length <= 0)
                {
                    IEnumerable<uint> ids = levelingList;

                    if (IPCSubscriber_Common.IsReady("SkipCutscene"))
                    {
                        ids = ids.Concat([
                            1048u, // 45 Porta Decumana
                        ]);
                    }
                    else
                    {
                        ids = ids.Concat([
                            1043u, // 50 Castrum Meridianum
                            1064u, // 53 Sohm Al
                            1065u, // 55 The Aery
                            1066u, // 57 The Vault
                            1109u, // 59 The Great Gubal Library])
                        ]);
                    }

                    if (Plugin.Configuration.LevelingListExperimentalEntries)
                        ids = ids.Concat(levelingListExperimental);

                    levelingDuties = [.. ids.Select(id => ContentHelper.DictionaryContent.GetValueOrDefault(id)).Where(c => c != null).Cast<Content>().OrderBy(x => x.ClassJobLevelRequired).ThenBy(x => x.ItemLevelRequired).ThenBy(x => x.ExVersion).ThenBy(x => x.DawnIndex)];
                }
                return levelingDuties;
            }
        }

        internal static Content? SelectHighestLevelingRelevantDuty(bool trust = false)
        {
            Content? curContent = null;
            var lvl = PlayerHelper.GetCurrentLevelFromSheet();
            Svc.Log.Debug($"Leveling Mode: Searching for highest relevant leveling duty, Player Level: {lvl}");
            CombatRole combatRole = Player.Object.GetRole();
            if (trust)
            {
                if (TrustHelper.Members.All(tm => !tm.Value.LevelIsSet))
                {
                    Svc.Log.Debug($"Leveling Mode: All trust members levels are not set, returning");
                    return null;
                }

                TrustMember?[] memberTest = new TrustMember?[3];

                foreach ((TrustMemberName _, TrustMember member) in TrustHelper.Members)
                {

                    if (member.Level < lvl && member.Level < member.LevelCap && member.LevelIsSet && memberTest.CanSelectMember(member, combatRole))
                        lvl = (short)member.Level;
                    Svc.Log.Debug($"Leveling Mode: Checking {member.Name} level which is {member.Level}, lowest level is now {lvl}");
                }
            }

            if ((lvl < 15 && !trust) || (trust && lvl < 71) || combatRole == CombatRole.NonCombat || lvl >= 100)
            {
                Svc.Log.Debug($"Leveling Mode: Lowest level is out of range (support<15 and trust<71) at {lvl} or we are not on a combat role {combatRole} or we (support) or we and all trust members are capped, returning");
                return null;
            }
            LevelingDuties.Each(x => Svc.Log.Debug($"Leveling Mode: Duties: {x.Name} CanRun: {x.CanRun(lvl, false)}{(trust ? $"CanTrustRun : {x.CanTrustRun()}" : "")}"));
            curContent = LevelingDuties.LastOrDefault(x => x.CanRun(lvl, trust));

            Svc.Log.Debug($"Leveling Mode: We found {curContent?.Name ?? "no duty"} to run");

            if (trust && curContent != null)
            {
                if (!TrustHelper.SetLevelingTrustMembers(curContent))
                {
                    Svc.Log.Debug($"Leveling Mode: We were unable to set our LevelingTrustMembers");
                    curContent = null;
                }
            }

            return curContent;
        }
    }
}
