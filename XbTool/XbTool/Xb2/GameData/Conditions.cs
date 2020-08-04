using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XbTool.BdatString;
using XbTool.Types;

namespace XbTool.Xb2.GameData
{
    public static class Conditions
    {
        public static string ParseGameCond(BdatStringCollection tables, int condId)
        {
            var condition = tables.Tables["FLD_ConditionList"].Items.FirstOrDefault(x => x.Id == condId);
            string s = "";
            for (int i = 0; i < 8; i++)
            {
                var conditionType = int.Parse(condition[$"ConditionType{i + 1}"].ValueString);
                var conditionEntry = condition[$"Condition{i+1}"];
                if (conditionType == 0)
                    continue;
                if (i >= 1)
                    s += ", ";
                switch ((ConditionType)conditionType)
                {
                    case ConditionType.Party:
                        s += ParsePartyCond(conditionEntry.Reference);
                        break;

                    case ConditionType.Scenario:
                        s += ParseScenarioCond(conditionEntry.Reference);
                        break;

                    case ConditionType.Achievement:
                        s += ParseAchievementCond(conditionEntry.Reference);
                        break;

                    case ConditionType.Quest:
                        s += ParseQuestCond(conditionEntry.Reference);
                        break;

                    case ConditionType.Item:
                        s += ParseItemCond(conditionEntry.Reference);
                        break;

                    case ConditionType.Flag:
                        s += ParseFlagCond(conditionEntry.Reference);
                        break;

                    case ConditionType.FieldSkill:
                        s += ParseFskillCond(conditionEntry.Reference);
                        break;

                    case ConditionType.Environment:
                        s += ParseEnvCond(conditionEntry.Reference);
                        break;

                    default:
                        Console.WriteLine($"unhandled condition type: {conditionType.ToString()}");
                        break;
                }
            }

            return s;
        }

        private static string ParseEnvCond(BdatStringItem env)
        {
            string s = "";
            if (env["TimeRange"].DisplayString != "")
                s += $"Time: {env["TimeRange"].DisplayString}";
            if (env["Weather"].DisplayString != "")
            {
                if (s.Length > 0)
                    s += ", ";
                s += $"Weather: {env["Weather"].DisplayString}";
            }
            if (env["CloudHeight"].DisplayString != "")
            {
                if (s.Length > 0)
                    s += ", ";
                s += $"Cloudsea height: {env["CloudHeight"].DisplayString}";
            }
            return s;
        }

        private static string ParseFskillCond(BdatStringItem skill)
        {
            return $"{skill["FieldSkillID"].DisplayString} is level {skill["Level"].DisplayString}";
        }

        private static string ParseFlagCond(BdatStringItem flag)
        {
            return $"{flag["FlagType"].DisplayString}-bit flag {flag["FlagID"].DisplayString} value between {flag["FlagMin"].DisplayString} and {flag["FlagMax"].DisplayString}";
        }

        private static string ParseItemCond(BdatStringItem item)
        {
            return $"Own {item["Number"].DisplayString} of {item["ItemID"].DisplayString}";
        }

        private static string ParseAchievementCond(BdatStringItem condition)
        {
            var achieve = condition["AchievementSetID"].Reference;
            var blade = achieve.ReferencedBy.FirstOrDefault(x => x.Table.Name == "CHR_Bl");
            var level = condition["Value"].DisplayString;
            string type = "unknown";

            if (blade["KeyAchievement"].Reference == achieve)
            {
                type = "KeyAchievement";
                goto end;
            }

            for (int i = 0; i < 3; i++)
            {
                if (blade[$"ArtsAchievement{i + 1}"].Reference == achieve)
                {
                    type = $"{blade[$"BArts{i + 1}"].DisplayString}";
                    break;
                }
                if (blade[$"SkillAchievement{i + 1}"].Reference == achieve)
                {
                    type = $"{blade[$"BSkill{i + 1}"].DisplayString}";
                    break;
                }
                if (blade[$"FskillAchivement{i + 1}"].Reference == achieve)
                {
                    type = $"{blade[$"FSkill{i + 1}"].DisplayString}";
                    break;
                }
            }

            end:
            return $"{blade["Name"].DisplayString}'s {type} == Lv{level}";
        }

        private static string ParseScenarioCond(BdatStringItem condition)
        {
            string s = "";
            string concat = "";
            if (condition["ScenarioMin"].DisplayString != "")
            {
                s += $"{concat}ScenarioId >= {condition["ScenarioMin"].DisplayString}";
                concat = ", ";
            }

            if (condition["ScenarioMax"].DisplayString != "")
            {
                s += $"{concat}ScenarioId <= {condition["ScenarioMax"].DisplayString}";
                concat = ", ";
            }

            if (condition["NotScenarioMin"].DisplayString != "")
                s += $"{concat}ScenarioId <= {condition["NotScenarioMin"].DisplayString} or ScenarioId >= {condition["NotScenarioMax"].DisplayString}";

            return s;
        }

        private static string ParseQuestCond(BdatStringItem quest)
        {
            string s = "";
            for (int i = 1; i <= 2; i++)
            {
                if (quest[$"QuestFlag{i}"].DisplayString != null)
                {
                    if (s.Length > 0)
                        s += ", ";
                    s += ParseQuestName(quest[$"QuestFlag{i}"].Reference) + " has progress " + ParseQuestProgress(int.Parse(quest[$"QuestFlagMin{i}"].ValueString), int.Parse(quest[$"QuestFlagMax{i}"].ValueString));
                }
                if (quest[$"NotQuestFlag{i}"].DisplayString != null)
                {
                    if (s.Length > 0)
                        s += ", ";
                    s += ParseQuestName(quest[$"NotQuestFlag{i}"].Reference) + " does not have progress " + ParseQuestProgress(int.Parse(quest[$"NotQuestFlagMin{i}"].ValueString), int.Parse(quest[$"NotQuestFlagMax{i}"].ValueString));
                }
            }

            return s;
        }
        private static string ParsePartyCond(BdatStringItem condition)
        {
            string s = condition["PCID1"].DisplayString;

            if (condition["PCID2"].DisplayString != null)
                s += $", {condition["PCID2"].DisplayString}";

            if (condition["PCID3"].DisplayString != null)
                s += $", {condition["PCID3"].DisplayString}";

            s += $" {condition["Category"].DisplayString}";

            return s;
        }

        public static string EventJudge(BdatStringCollection tables, int[] args) // EVT_judge, int id, int val=0)
        {
            int EVT_judge = args[0];

            int id = 0;
            if (args.Count() > 1)
                id = args[1];

            int val = 0;
            if (args.Count() > 2)
                val = args[2];

            switch (EVT_judge)
            {
                case 1:
                    return $"ScenarioId >= {id}";

                case 15:
                    return $"1bit flag {id + 0xc327} == {val}";

                case 26:
                    return ParseGameCond(tables, id);

                default:
                    break;
            }

            return "";
        }
        public static void PrintEventConditions(BdatStringItem bdatEvent, IEnumerable<string> scriptFiles, IEnumerable<BdatStringItem> listEvents)
        {
            if (bdatEvent == null)
                Console.WriteLine("null bdat event!");
            else if (bdatEvent.ReferencedBy.Any(trigger => trigger.Values.ContainsKey("Title")))        // H2H - not doing probably
            {
                Console.WriteLine("H2H Event");
                var refItem = bdatEvent.ReferencedBy.FirstOrDefault(trigger => trigger.Values.ContainsKey("Title"));

            }
            else if (bdatEvent.ReferencedBy.Any(trigger => trigger.Values.ContainsKey("NpcID")))         // NPC - done
            {
                var refItem = bdatEvent.ReferencedBy.FirstOrDefault(trigger => trigger.Values.ContainsKey("NpcID"));
                Console.Write("Talk to ");
                PrintNpcCondition(refItem);
            }
            else if (bdatEvent.ReferencedBy.Any(trigger=>trigger.Values.ContainsKey("name")))           // EventPop - done
            {
                var refItem = bdatEvent.ReferencedBy.FirstOrDefault(trigger => trigger.Values.ContainsKey("name"));
                Console.Write("Trigger ");
                PrintEventPopConditions(refItem);

            }
            else if (bdatEvent.ReferencedBy.Any(trigger => trigger.Values.ContainsKey("QuestTitle")))   // Quest - done (maybe)
            {
                Console.WriteLine("Quest Event");
                var refItem = bdatEvent.ReferencedBy.FirstOrDefault(trigger => trigger.Values.ContainsKey("QuestTitle"));
                Console.Write("Complete ");
                Console.WriteLine(ParseQuestName(refItem));
            }
            else if (bdatEvent.ReferencedBy.Any(trigger => trigger.Values.ContainsKey("evtName")))      // Event - done
            {
                Console.WriteLine("Linked Event");
                var refItem = bdatEvent.ReferencedBy.FirstOrDefault(trigger => trigger.Values.ContainsKey("evtName"));
                PrintEventConditions(refItem, scriptFiles, listEvents);
            }
            else                                                                                        // Not Found (probably scripts) - done
            {
                string scriptFile = scriptFiles.FirstOrDefault(fileName => File.ReadLines(fileName).Any(line => line.Contains($"evt_status::change(32, {bdatEvent.Id}")));
                Scripts script = new Scripts(scriptFile);
                script.PrintEventTriggerConditions($"evt_status::change(32, {bdatEvent.Id}", scriptFiles, listEvents);
            }
        }

        private static void PrintEventPopConditions(BdatStringItem eventPop)
        {
            Console.WriteLine($"Event {eventPop["name"].DisplayString}");
            if (eventPop["ScenarioFlagMin"].DisplayString != "" || eventPop["ScenarioFlagMax"].DisplayString != "")
            {
                Console.Write("Scenario required: ");
                if (eventPop["ScenarioFlagMin"].DisplayString != "")
                    Console.Write($"{eventPop["ScenarioFlagMin"].DisplayString} <= ");
                Console.Write("scenarioFlag");
                if (eventPop["ScenarioFlagMax"].DisplayString != "")
                    Console.Write($" <= {eventPop["ScenarioFlagMax"].DisplayString}");
                Console.WriteLine();
            }
            if (eventPop["QuestFlag"].Reference != null)
            {
                var questFlag = eventPop["QuestFlag"].Reference;
                Console.Write("Required quest progress: ");
                Console.Write(ParseQuestName(questFlag));
                Console.Write(" with progress: ");
                Console.WriteLine(ParseQuestProgress(int.Parse(eventPop["QuestFlagMin"].ValueString), int.Parse(eventPop["QuestFlagMax"].ValueString)));
            }
            if (eventPop["Condition"].DisplayString != null)
            {
                Console.Write("Condition Required: ");
                Console.WriteLine(ParseGameCond(eventPop.Table.Collection, int.Parse(eventPop["Condition"].DisplayString)));
            }
            Console.WriteLine();
        }

        private static void PrintNpcCondition(BdatStringItem npc)
        {
            Console.WriteLine($"NPC {npc["NpcID"].DisplayString}");
            if (npc["ScenarioFlagMin"].DisplayString != "" || npc["ScenarioFlagMax"].DisplayString != "")
            {
                Console.Write("Scenario required: ");
                if (npc["ScenarioFlagMin"].DisplayString != "")
                    Console.Write($"{npc["ScenarioFlagMin"].DisplayString} <= ");
                Console.Write("scenarioFlag");
                if (npc["ScenarioFlagMax"].DisplayString != "")
                    Console.Write($" <= {npc["ScenarioFlagMax"].DisplayString}");
                Console.WriteLine();
            }
            if (npc["QuestFlag"].Reference != null)
            {
                var questFlag = npc["QuestFlag"].Reference;
                Console.Write("Required quest progress: ");
                Console.Write(ParseQuestName(questFlag));
                Console.Write(" with progress: ");
                Console.WriteLine(ParseQuestProgress(int.Parse(npc["QuestFlagMin"].ValueString), int.Parse(npc["QuestFlagMax"].ValueString)));
            }
            if (npc["Condition"].DisplayString != null)
            {
                Console.WriteLine(ParseGameCond(npc.Table.Collection, int.Parse(npc["Condition"].DisplayString)));
            }
            if (npc["TimeRange"].DisplayString != "")
            {
                Console.WriteLine($"Time of day: {npc["TimeRange"].DisplayString}");
            }
            Console.WriteLine();
        }

        private static string ParseQuestName(BdatStringItem quest)
        {
            if (quest["FlagPRT"].DisplayString == "")
            {
                while (quest["PRTQuestID"].Reference == null)
                    quest = quest.Table.Items.FirstOrDefault(itm => itm.Id == quest.Id - 1);
                return $"Step {quest.Id - quest["PRTQuestID"].Reference.Id} of quest {quest["PRTQuestID"].DisplayString}";
            }
            else
            {
                if (quest["QuestTitle"].DisplayString != null)
                    return $"Quest {quest["QuestTitle"].DisplayString}";
                return $"Quest {quest.Id}";
            }
        }

        private static string ParseQuestProgress(int flagMin, int flagMax)
        {
            string s = "";
            string[] options = { "Not Started", "Started", "Finished (A)", "Finished (B)" };
            for (int i = flagMin; i <= flagMax; i++)
            {
                if (s.Length > 0)
                    s += ", ";
                s += options[i];
            }
            return s;
        }
    }
}
