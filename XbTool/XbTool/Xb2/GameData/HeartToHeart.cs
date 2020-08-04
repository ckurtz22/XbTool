using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XbTool.BdatString;

namespace XbTool.Xb2.GameData
{
    public static class HeartToHeart
    {
        public static void PrintH2Hs(BdatStringCollection tables)
        {
            foreach (var h2h in tables.Tables["FLD_KizunaTalk"].Items.Where(x => x["name"].DisplayString.Contains("kizuna")))
            {
                int.TryParse(h2h["name"].DisplayString.Substring(7), out int id);
                Console.Write($"{id}\t{h2h["Title"].DisplayString}\t");
                PrintTrust(tables, h2h["EventID"].Reference["evtName"].DisplayString);
                //Console.WriteLine();
            }


        }

        private static void PrintTrust(BdatStringCollection tables, string evt)
        {
            string s = "";
            int last = -1;
            var lines = File.ReadAllLines($"../decompiled_scripts/main_game/{evt}.c");
            bool choice = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("evt_status::change(55, "))
                {
                    var args = Array.ConvertAll(lines[i].Substring(27).Split(')').FirstOrDefault().Split(','), str => int.Parse(str));
                    var blade = tables["CHR_Bl"].Items.FirstOrDefault(x => x.Id == args[0])["Name"].DisplayString;
                    var amount = args[1];


                    if (last != -1 && i - last > 1)
                    {
                        s = "(" + s + ") or (";
                        choice = true;
                    }
                    else if (s.Length > 0)
                        s += ", ";

                    s += $"{blade} +{amount} trust";

                    last = i;
                }
            }
            if (choice)
                s += ")";

            Console.WriteLine(s);
        }

        public static void FindInnEvents(BdatStringCollection tables)
        {
            var h2hs = tables.Tables["EVT_listFev01"].Items.Where(x => x["evtName"].DisplayString.Contains("kizuna"));
            foreach (var inn in h2hs.Where(x => x["evtName"].DisplayString.Contains("kizunainn")))
            {
                int.TryParse(inn["evtName"].DisplayString.Substring(9), out int id);
                var h2h = h2hs.FirstOrDefault(x => x["evtName"].DisplayString == $"kizunatalk{id:D3}").ReferencedBy.FirstOrDefault();
                if (h2h["ConditionID"].Reference["Condition1"].Reference["FlagMin"].DisplayString == "1")
                {
                    var name = h2h["Title"].DisplayString;
                    var fev = inn.Id;

                    Console.WriteLine($"{name}:");
                    Console.WriteLine(FindInnScripts(tables, fev));
                    Console.WriteLine("");
                    // Console.WriteLine($"{name}\t{fev}");
                }
            }

        }

        private static string SearchScriptForEvent(BdatStringCollection tables, int id, string filename)
        {
            string s = "";
            var lines = File.ReadAllLines(filename);

            int next = FindNextScriptEntry(lines, $"evt_status::change(32, {id}", -3);
            next = FindNextScriptEntry(lines, $"return {next}", -4);
            next = FindNextScriptEntry(lines, $"return {next}", -4);

            for (int i = 0; i < lines.Count(); i++)
            {
                if (lines[i].Contains($"return {next}"))
                {
                    for (int j = 3; j <= 9; j += 3)
                    {
                        if (lines[i-j].Contains("evt_status::judge"))
                        {
                            var args = Array.ConvertAll(lines[i - j].Substring(27).Split(')').FirstOrDefault().Split(','), str => int.Parse(str));
                            if (s.Length > 0)
                                s += ", ";
                            s += Conditions.EventJudge(tables, args);
                        }
                    }
                    break;
                }
            }

            return s;
        }

        private static int FindNextScriptEntry(string[] lines, string find, int offset)
        {
            int next = -1;
            for (int i = 0; i < lines.Count(); i++)
            {
                if (lines[i].Contains(find))
                {
                    int.TryParse(lines[i + offset].Substring(12, 2), out next);
                    break;
                }
            }

            return next;
        }

        private static string FindInnScripts(BdatStringCollection tables, int id)
        {
            string s = "";
            foreach (var inn in tables.Tables["MNU_ShopList"].Items.Where(x => x["ShopType"].DisplayString == "Inn").Select(x => x.ReferencedBy.FirstOrDefault(y => y["QuestFlag"].DisplayString == null && y["QuestID"].DisplayString == null)))
            {
                if (inn == null || inn.Id == 40293) continue;
                var evt = inn["EventID"].Reference["evtName"].DisplayString;

                var filename = Directory.GetFiles("../decompiled_scripts/main_game/").FirstOrDefault(x => x.Contains(evt));

                if (!(File.ReadLines(filename).SkipWhile(x => !x.Contains($"evt_status::change(32, {id}")).Take(1).Count() == 0))
                {
                    s = SearchScriptForEvent(tables, id, filename) + $", Rest at {inn["ShopID"].DisplayString}";
                    break;
                }
            }
            return s;
        }
    }
}
