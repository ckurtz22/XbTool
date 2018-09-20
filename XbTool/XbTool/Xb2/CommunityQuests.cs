using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XbTool.Scripting;
using XbTool.Types;

namespace XbTool.Xb2
{
	class CommunityQuests
	{
		private Script[] scripts;
		private BdatCollection tables;
		private List<QuestEntry> quests;
		QuestEntry talkToNpc;
		public CommunityQuests(Script[] scripts, BdatCollection tables)
		{
			this.scripts = scripts;
			this.tables = tables;

			GetQuestList();
			foreach (Script script in scripts)
			{
				FindCommunity(script);
			}
		}

		private void GetQuestList()
		{
			var questFlags = tables.EVT_listQst01.Where(x => x.evtName.Contains("qst7") && x.evtName.Substring(x.evtName.Length - 2, 2) == "01")
				.Select(x => Int32.Parse(x.evtName.Substring(3, 4)) + 28769).ToList();
			var questEntries = tables.FLD_QuestListNormalIra.Where(x => questFlags.Contains(x.FlagPRT)).ToList();
			quests = new List<QuestEntry>();
			foreach (var quest in questEntries)
				quests.Add(new QuestEntry(quest, tables));

			talkToNpc = new QuestEntry(new FLD_QuestList(), tables);
			talkToNpc.quest._QuestTitle = new Message { name = "Talk to NPC after requirements are met" };
			quests.Add(talkToNpc);
			}

		public void PrintFile(Options options)
		{
			var sb = new StringBuilder();
			foreach(QuestEntry questSet in quests)
			{
				if(questSet.npcSpawner != null && questSet.quest._Talker?._Name.name != questSet.npcSpawner._NpcID._Name.name)
				{
					var test = 1;
				}
				sb.AppendLine(questSet.quest?._QuestTitle?.name);
				sb.AppendLine($"\tGiven by: {questSet.npcSpawner?._NpcID._Name.name ?? questSet.eventSpawner?.name ?? "N/A"}");
				sb.AppendLine($"\tScenario: {questSet.scenarioFlagMin.ToString() ?? "N/A"}");
				sb.AppendLine($"\tConditions: {questSet.conditions?.Id.ToString() ?? "N/A"}");
				foreach(CommunityNPC npc in questSet.npcs)
				{
					sb.AppendLine($"\t{(npc.add ? "+ " : "- ")}{npc.npcName}");
				}
				sb.AppendLine("");
			}
			File.WriteAllText($"{options.Output}.txt", sb.ToString());
		}



		public void FindCommunity(Script script)
		{
			var questSet = getQst(script.StringPool[0], tables);

			int k;
			for (k = 0; k < script.Plugins.Count(); k++)
				if (script.Plugins[k].Function == "openHitonowa")
					break;

			for (int i = 0; i < script.Code.Count(); i++)
				for (int j = 0; j < script.Code[i].Count(); j++)
					if (script.Code[i][j].Opcode == Opcode.PLUGIN && script.Code[i][j].Operand == k.ToString())
					{
						bool add = (script.Code[i][j - 3].Opcode == Opcode.CONST_1);
						
						if (script.Code[i][j - 2].Opcode != Opcode.CONST_0)
							questSet.AddNpc(add, tables.RSC_NpcList[script.IntPool[Int32.Parse(script.Code[i][j - 2].Operand)]]._Name.name);
						if (script.Code[i][j - 5].Opcode != Opcode.CONST_0)
							questSet.AddNpc(add, tables.RSC_NpcList[script.IntPool[Int32.Parse(script.Code[i][j - 5].Operand)]]._Name.name);
						if (script.Code[i][j - 6].Opcode != Opcode.CONST_0)
							questSet.AddNpc(add, tables.RSC_NpcList[script.IntPool[Int32.Parse(script.Code[i][j - 6].Operand)]]._Name.name);
						if (!quests.Contains(questSet))
							quests.Add(questSet);
					}
		}

		private QuestEntry getQst(string idName, BdatCollection tables)
		{
			switch (idName.Substring(0, 3))
			{
				case "qst":
					var qstNum = idName.Substring(3, 4);
					var flag = Int32.Parse(qstNum) + 28769;
					var questName = tables.FLD_QuestListNormalIra.Where(x => x.FlagPRT == flag).First();
					var questSet = quests.Where(x => x.quest == questName).FirstOrDefault();
					if (questSet == null)
						break;

					
					return questSet;
				default:
					break;
			}
			return talkToNpc;
		}
	}

	public class QuestEntry
	{
		public List<CommunityNPC> npcs;
		public FLD_QuestList quest;
		public RSC_NpcList talker;
		public ma40a_FLD_NpcPop npcSpawner;
		public ma02a_FLD_EventPop eventSpawner;
		public FLD_ConditionList conditions;

		public int flagPRT;
		public int questID;
		public int scenarioFlagMin;

		public QuestEntry(FLD_QuestList quest, BdatCollection tables)
		{
			this.quest = quest;
			talker = quest._Talker;
			flagPRT = quest.FlagPRT;
			questID = flagPRT - 28769;
			npcs = new List<CommunityNPC>();
			SetSpawner(tables);
		}

		private void SetSpawner(BdatCollection tables)
		{
			var events = tables.EVT_listQst01.Where(z => z.evtName.Contains($"{questID}01"));
			npcSpawner = tables.ma40a_FLD_NpcPop.Where(x => events.Where(y => y.Id == x.EventID).Count() > 0).FirstOrDefault() ??
				tables.ma41a_FLD_NpcPop.Where(x => events.Where(y => y.Id == x.EventID).Count() > 0).FirstOrDefault();
			eventSpawner = tables.ma40a_FLD_EventPop.Where(x => x.name.Contains($"{questID}01")).FirstOrDefault() ??
				tables.ma41a_FLD_EventPop.Where(x => x.name.Contains($"{questID}01")).FirstOrDefault();
			if(flagPRT == 35787)
			{
				var test = 1;
			}
			if(npcSpawner != null)
			{
				conditions = npcSpawner._Condition;
				scenarioFlagMin = npcSpawner.ScenarioFlagMin;
			}
			if(eventSpawner != null)
			{
				conditions = eventSpawner._Condition;
				scenarioFlagMin = eventSpawner.ScenarioFlagMin;
			}
		}

		public void AddNpc(bool add, string name)
		{
			var newNpc = new CommunityNPC(add, name);
			npcs.Add(newNpc);
		}


		public string GetScenarioPoint(int Scenario)
		{
			switch (Scenario - 11000)
			{
				case 001: return "Game start";
				case 029: return "Hugo joins";
				case 031: return "Kill Dispare Ropl";
				case 047: return "Talk to Addam on Hugo's ship";
				case 054: return "Reach Aletta";
				case 056: return "Complete quest 'Feeding an Army'";
				case 057: return "Complete quest 'Feeding an Army'";
				case 059: return "Complete quest 'Lett Bridge Restoration'";
				case 081: return "Reach Auresco";
				case 085: return "Finish Community lvl 2 quest ?";
				case 096: return "Defeat Malos 1 and his Gargoyles, report to the King";
				case 105: return "Finish Community lvl 4 quest ?";
				default: return Scenario.ToString();
			}
		}

		public string GetCondition(int Condition)
		{
			switch (Condition)
			{
				case 2935: return "\n\t\t1bit#50836=0\n\t\tNot completed part of this quest?";
				case 2950: return "\n\t\tNot completed part of this quest?";
				case 2973: return "\n\t\t1bit#50836=0\n\t\tNot completed part of this quest?\n\t\tCommunity lvl 2";
				case 2983: return "\n\t\t1bit#50837=0\n\t\tNot completed part of this quest?";
				case 3009: return "\n\t\tNot completed part of this quest?";
				case 3048: return "\n\t\t1bit#50836=0\n\t\tNot completed part of this quest?";
				case 3062: return "\n\t\tNot completed part of this quest?";
				case 3073: return "\n\t\t1bit#50836=0\n\t\tNot completed part of this quest?\n\t\tCommunity lvl 3\n\t\tCompleted 'An Oasis for All'\n\t\tCompleted 'Lighting the Way'";
				case 3077: return "\n\t\t1bit#50836=0\n\t\tCompleted 'Planning for the Future'\n\t\tQuest not in progress 'Making Up the Numbers'";
				case 3092: return "\n\t\t1bit#50836=0\n\t\tNot completed part of this quest?";
				case 3096: return "\n\t\t1bit#50836=0\n\t\t1bit#50805=1\n\t\tCompleted 'Homegrown Inventor'\n\t\tDo not have 'Hugo's Gold Detector'";
				case 3128: return "\n\t\tCompleted 'The Fish That Could Be'\n\t\tCommunity lvl 3";
				case 3151: return "\n\t\tCommunity lvl 2";
				case 3220: return "\n\t\t1bit#50836=0";
				case 3222: return "\n\t\t1bit#50836=0\n\t\tCommunity lvl 2";
				case 3243: return "\n\t\t1bit#50836=0\n\t\tCommunity lvl 3";
				case 3247: return "\n\t\tDefeated 16 UMs\n\t\tMartha not in community\n\t\t";
				case 3265: return "\n\t\tCompleted 'Where's the Boy Gone?'\n\t\t1bit#50837=0\n\t\tCommunity lvl 2";
				case 3271: return "\n\t\t1bit#50836=0\n\t\tQuest not in progress 'Making Up the Numbers'\n\t\tCommunity lvl 2";
				case 3381: return "\n\t\t1bit#50837=0";
				case 3655: return "\n\t\tCompleted 'Further Driver Coaching'\n\t\tCompleted 'Further Blade Coaching'\n\t\tCommunity lvl 2\n\t\t1bit#50837=0";
				case 3724: return "\n\t\tCommunity lvl 2\n\t\t1bit#50837=0";
				case 3748: return "\n\t\tCompleted 'Passing the Torch'";
				case 3882: return "\n\t\tNot started 'Safety Measures'";
				default: return $"\n\t\t{Condition}";
			}
		}

	}

	public class CommunityNPC
	{
		public bool add;
		public string npcName;
		public CommunityNPC(bool add, string name)
		{
			this.add = add;
			npcName = name;
		}
	}
}
