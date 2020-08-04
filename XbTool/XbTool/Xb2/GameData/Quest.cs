using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XbTool.BdatString;
using XbTool.Xb2;

namespace XbTool.Xb2.GameData
{
    public static class Quest
    {
        public static void ReadQuests(BdatStringCollection tables)
        {
            var story = tables["FLD_QuestListNormal"].Items.Where(x => x["QuestIcon"].DisplayString != "0" && x["QuestCategory"].DisplayString == "1");
            var normal = tables["FLD_QuestListMini"].Items.Where(x => x["QuestIcon"].DisplayString != "0");
            normal = normal.Concat(tables["FLD_QuestListNormal"].Items.Where(x => x["QuestIcon"].DisplayString != "0" && x["QuestCategory"].DisplayString == "3"));
            var blade = tables["FLD_QuestListBlade"].Items.Where(x => x["QuestIcon"].DisplayString != "0");
            var torna_story = tables["FLD_QuestListNormalIra"].Items.Where(x => x["QuestIcon"].DisplayString != "0" && x["QuestCategory"].DisplayString == "1");
            var torna_normal = tables["FLD_QuestListNormalIra"].Items.Where(x => x["QuestIcon"].DisplayString != "0" && x["QuestCategory"].DisplayString == "3");

            //SearchScripts(tables);
            PrintQuests(tables, normal);
        }

        private static void SearchScripts(BdatStringCollection tables)
        {
            var i = 0;
            var filenames = Directory.GetFiles("../decompiled_scripts/main_game/").Concat(Directory.GetFiles("../decompiled_scripts/aoc/"));
            foreach (var filename in filenames.Where(x=>File.ReadAllLines(x).Any(y=>y.Contains("evt_status::change(2,"))))
            {
                var line = File.ReadAllLines(filename).FirstOrDefault(x => x.Contains("evt_status::change(2,"));
                var quest = FindQuestById(tables, int.Parse(line.Split('(')[1].Split(')')[0].Split(',')[1]));
                if (int.Parse(quest["Visible"].ValueString) == 0 || int.Parse(quest["QuestIcon"].ValueString) == 0)
                    continue; // Hidden Task or Unimplemented

                Console.WriteLine(quest["QuestTitle"].DisplayString);
                var sc = new Scripts(filename);
                if (sc.FindPathsTo("evt_status::change(2,") == 0)
                    Console.WriteLine("uh oh");
                sc.PrintPaths();

                string script = Path.GetFileNameWithoutExtension(filename);
                var evt = tables.Tables.Where(x => x.Key.Contains("EVT_list")).SelectMany(x => x.Value.Items.Where(y => y.Values.ContainsKey("evtName") && y["evtName"].DisplayString == script)).FirstOrDefault();
                if (evt.ReferencedBy.FirstOrDefault(x => x.Values.ContainsKey("name")) == null)
                {
                    filenames.FirstOrDefault(x => File.ReadAllLines(x).Any(y => y.Contains($"evt_status::change(32, {evt.Id}")));
                    Console.WriteLine();
                }
                else
                    Console.WriteLine(evt.ReferencedBy.FirstOrDefault()["name"].DisplayString);

                //Console.WriteLine(line.FirstOrDefault());
            }
            foreach (var events in tables.Tables.Where(x => x.Key.Contains("EVT_chg")).Select(x => x.Value.Items.Where(y => int.Parse(y["chgType"].ValueString) == 2)))
            {
                foreach (var evt in events)
                {
                    var quest = FindQuestById(tables, int.Parse(evt["id"].ValueString));
                    if (quest == null) continue;
                    if (int.Parse(quest["Visible"].ValueString) == 0 || int.Parse(quest["QuestIcon"].ValueString) == 0)
                        continue; // Hidden Task or Unimplemented
                    if (evt.ReferencedBy.Count() == 0 || int.Parse(quest["QuestCategory"].ValueString) == 1)
                        continue;
                    Console.WriteLine(quest["QuestTitle"].DisplayString);
                    i++;
                }
            }

            Console.WriteLine(i);
        }

        private static BdatStringItem FindQuestById(BdatStringCollection tables, int id)
        {
            return tables.Tables.Where(tbl => tbl.Key.Contains("FLD_QuestList")).SelectMany(tbl => tbl.Value.Items).FirstOrDefault(itm => itm.Id == id);
        }

        private static void PrintQuests(BdatStringCollection tables, IEnumerable<BdatStringItem> quests)
        {
            var changeEvents = tables.Tables.Where(tbl => tbl.Key.Contains("EVT_chg")).SelectMany(tbl => tbl.Value.Items).Where(evt => int.Parse(evt["chgType"].ValueString) == 2);
            var scriptFiles = Directory.GetFiles("../decompiled_scripts/main_game/").Concat(Directory.GetFiles("../decompiled_scripts/aoc/"));
            var listEvents = tables.Tables.Where(tbl => tbl.Key.Contains("EVT_list") && !tbl.Key.Contains("listList")).SelectMany(tbl => tbl.Value.Items);

            foreach (var quest in quests)
            {
                string questName = quest["QuestTitle"].DisplayString;
                int questId = quest.Id;
                Console.WriteLine("--------");
                Console.WriteLine(questName);
                Console.WriteLine("--------");
                Console.WriteLine("Start Requirements: ");
                PrintStartEvent(changeEvents, listEvents, scriptFiles, questId);
                Console.WriteLine("--------");
                Console.WriteLine("=======================");

                //Console.WriteLine(FindStartCondition(tables, quest));
            }
        }

        private static void PrintStartEvent(IEnumerable<BdatStringItem> changeEvents, IEnumerable<BdatStringItem> listEvents, IEnumerable<string> scriptFiles, int questId)
        {
            var chgEvent = changeEvents.FirstOrDefault(evt => evt.ReferencedBy.Count() > 0 && int.Parse(evt["id"].ValueString) == questId);

            if (chgEvent != null)
            {
                Conditions.PrintEventConditions(chgEvent.ReferencedBy.FirstOrDefault(), scriptFiles, listEvents);
                return;
            }

            string scriptFile = scriptFiles.FirstOrDefault(fileName => File.ReadLines(fileName).Any(line => line.Contains($"evt_status::change(2, {questId}")));
            if (scriptFile != null)
            {
                Scripts script = new Scripts(scriptFile);
                script.PrintEventTriggerConditions($"evt_status::change(2, {questId}", scriptFiles, listEvents);
            }
            else
                Console.WriteLine("Start event not found");
        }
    }
}
