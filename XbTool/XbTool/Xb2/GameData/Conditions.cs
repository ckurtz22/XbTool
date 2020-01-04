using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XbTool.BdatString;

namespace XbTool.Xb2.GameData
{
    public static class Conditions
    {
        private static String EnglishifyPartyCategory(String category, int count)
        {
            String isare = count > 1 ? "are" : "is";
            String s = (count > 1 ? "'s" : "");
            switch (category)
            {
                case "BattleParty":
                    return $" {isare} in the battle party";
                case "DriverInParty":
                    return $"'s driver{s} {isare} in the party";
                case "InActiveParty":
                    return $" {isare} in the active party";
                case "InParty":
                    return $" {isare} in the party";
                case "InPartyOrMerc":
                    return $" {isare} in the party or on a merc mission";
                case "IsLeader":
                    return $" {isare} the party leader{s}";
                case "IsMercTeamLeader":
                    return $" {isare} the merc team leader{s}";
                default:
                    return $"If you ever see this PLEASE TELL GREN. ParseConditionParty T8";
            }
        }

        private static String ParseConditionParty(List<BdatStringItem> set)
        {
            var types = new Dictionary<String, List<String>>();
            foreach (var item in set) 
            {
                var key = item["Category"].DisplayString;
                if (!types.ContainsKey(key))
                {
                    types[key] = new List<String>();
                }
                
                for (int i = 0; i < 3; ++i)
                {
                    if (item[$"PCID{i + 1}"].Reference != null)
                    {
                        types[key].Add(item[$"PCID{i + 1}"].DisplayString);
                    }
                }
            }

            var sb = new StringBuilder();
            foreach (KeyValuePair<String, List<String>> pair in types)
            {
                sb.Append(ListToString(pair.Value, "and"));
                sb.Append(EnglishifyPartyCategory(pair.Key, pair.Value.Count()));
                sb.Append("; ");
            }

            return sb.ToString();
        }

        private static String ParseConditionScenario(List<BdatStringItem> set)
        {
            var sb = new StringBuilder();
            if (set.Count() > 1)
            {
                Console.WriteLine($"THERE ARE {set.Count()} SCENARIO CONDITIONS");
            }
            foreach (var scenario in set) 
            {
                if (scenario["NotScenarioMin"].Reference != null)
                {
                    Console.WriteLine("NOTSCENARIO CONDITION FOUND");
                }
                sb.Append($"{scenario["ScenarioMin"].DisplayString} <= ScenarioID <= {scenario["ScenarioMax"].DisplayString}; ");
            }

            return sb.ToString();
        }
        
        private static string ParseConditionQuest(List<BdatStringItem> set)
        {
            var sb = new StringBuilder();

            String[] progress = 
            {
                "Not Started",
                "In progress",
                "Completed (Route A)",
                "Completed (Route B)"
            };

            foreach (var quest in set)
            {
                String[] list = {"", "Not"};
                foreach (var not in list)
                {
                    for (int i = 0; i < 2; ++i)
                    {
                        if (quest[$"{not}QuestFlag{i + 1}"].Reference == null) continue;
                        
                        sb.Append($"Progress in quest {quest[$"{not}QuestFlag{i + 1}"].DisplayString} is {not} of the following: ");
                        int min = Int32.Parse(quest[$"{not}QuestFlagMin{i + 1}"].ValueString);
                        int max = Int32.Parse(quest[$"{not}QuestFlagMax{i + 1}"].ValueString);
                        var progresses = new List<String>();
                        for (int j = min; j < max + 1; ++j)
                        {
                            progresses.Add(progress[j]);
                        }
                        sb.Append($"{ListToString(progresses, "or")}; ");
                    }
                }
            }
            return sb.ToString().Replace("  ", " ");
        }

        private static string ParseConditionAchievement(List<BdatStringItem> set)
        {
            var sb = new StringBuilder();
            foreach (var achievement in set)
            {
                var achievementId = achievement["AchievementSetID"].Reference;
                var blade = achievementId.ReferencedBy.FirstOrDefault(x=>x.Table.Name == "CHR_Bl");
                var val = blade.Values.FirstOrDefault(x => x.Value.Reference == achievementId);
                // TODO: Possibly make this grab the name of field skill etc
                sb.Append($"{blade["Name"].DisplayString}'s {val.Key} must be at least level {achievement["Value"].DisplayString}; ");
            }
            
            return sb.ToString();
        }

        private static string ParseConditionFlag(List<BdatStringItem> set)
        {
            // Console.WriteLine("Flag");
            var sb = new StringBuilder();
            
            String[] progress = 
            {
                "Not Started",
                "Inn viewed",
                "Finished"
            };

            foreach (var flag in set)
            {
                var flagId = Int32.Parse(flag["FlagID"].ValueString);
                switch (flag["FlagType"].DisplayString)
                {
                    case "2":
                        if (flagId < 84) 
                        {
                            var h2hTable = flag.Table.Collection.Tables["FLD_KizunaTalk"];
                            var h2h = h2hTable.Items.FirstOrDefault(x => x.Id == flagId);
                            sb.Append($"Progress in H2H {h2h["Title"].DisplayString} is of the following: ");
                            int min = Int32.Parse(flag["FlagMin"].ValueString);
                            int max = Int32.Parse(flag["FlagMax"].ValueString);
                            var list = new List<String>();
                            for (int i = min; i < max + 1; ++i)
                            {
                                list.Add(progress[i]);
                            }
                            sb.Append($"{ListToString(list, "or")}; ");
                        }
                        else
                        {
                            Console.WriteLine("FLAG 2 ABOVE 83 UNHANDLED");
                        }
                        break;

                    default:
                        Console.WriteLine($"UNHANDLED FLAG TYPE: {flag["FlagType"].DisplayString}");
                        break;
                }
                // Console.Write($"{flag["FlagType"].DisplayString} bits, ID: {flag["FlagID"].DisplayString}: [{flag["FlagMin"].DisplayString},{flag["FlagMax"].DisplayString}]");
            }
            return sb.ToString();
        }

        public static String ParseConditionSet(String type, List<BdatStringItem> set) 
        {
            switch (type) 
            {
                case "Party":
                    return ParseConditionParty(set);

                case "Scenario":
                    return ParseConditionScenario(set);

                case "Flag":
                    return ParseConditionFlag(set);

                case "Achievement":
                    return ParseConditionAchievement(set);
                
                case "Quest":
                    return ParseConditionQuest(set);


                default:
                    Console.WriteLine($"UKNOWN CONDITION TYPE: {type}");
                    return "";
            }
        }

        public static String ParseConditionList(BdatStringItem condList)
        {
            String premise = condList["Premise"].DisplayString;

            var conds = new Dictionary<string, List<BdatStringItem>>();
            for (int i = 0; i < 8; ++i) 
            {
                if (condList[$"Condition{i+1}"].Reference != null)
                {
                    var key = condList[$"ConditionType{i + 1}"].DisplayString;

                    if (!conds.ContainsKey(key))
                        conds[key] = new List<BdatStringItem>();

                    conds[key].Add(condList[$"Condition{i + 1}"].Reference);
                }
            }

            var condStrings = new List<String>();
            foreach(KeyValuePair<String, List<BdatStringItem>> entry in conds)
            {
                condStrings.Add(ParseConditionSet(entry.Key, entry.Value));
            }

            if (condList["Premise"].DisplayString != "AND") 
            {
                Console.WriteLine($"ERROR: UNKNOWN PREMISE: {condList["Premise"].DisplayString}");
                return "";
            }

            char[] toTrim = {' ', ';'};
            return $"\"{String.Join("", condStrings).TrimEnd(toTrim)}\"";
        }

        public static string ListToString(this IEnumerable<string> collection, String finalDelimiter)
        {
            var output = string.Empty;
            if (collection == null) return null;

            var list = collection.ToList();
            if (!list.Any()) return output;
            if (list.Count == 1) return list.First();

            var delimited = string.Join(", ", list.Take(list.Count - 1));
            output = string.Concat(delimited, $" {finalDelimiter} ", list.LastOrDefault());
            return output;
        }
    }
}