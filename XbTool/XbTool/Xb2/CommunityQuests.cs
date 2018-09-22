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

			talkToNpc = new QuestEntry(new FLD_QuestList(), tables) { questName = "Talk to NPC after requirements are met" };
			quests.Add(talkToNpc);
			}

		public void PrintFile(Options options)
		{
			var sb = new StringBuilder();
			foreach(QuestEntry questSet in quests)
			{
				if(questSet.npcSpawner != null && questSet.talker?._Name.name != questSet.npcSpawner._NpcID._Name.name)
				{
					var test = 1;
				}
				sb.AppendLine(questSet.questName);
				sb.AppendLine($"\tGiven by: {questSet.npcSpawner?._NpcID._Name.name ?? questSet.eventSpawner?.name ?? "N/A"}");
				sb.AppendLine($"\tReach Main Scenario point: {questSet.GetScenarioPoint() ?? "N/A"}");
				sb.AppendLine($"\tConditions: \n{questSet.GetCondition()}");
				foreach(CommunityNPC npc in questSet.npcs)
				{
					sb.AppendLine($"\t{(npc.add ? "+ " : "- ")}{npc.npcName}");
				}
				sb.AppendLine("\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");
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
					var quest = tables.FLD_QuestListNormalIra.Where(x => x.FlagPRT == flag).First();
					var questSet = quests.Where(x => x.questName == quest._QuestTitle?.name).FirstOrDefault();
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
		public RSC_NpcList talker;
		public ma40a_FLD_NpcPop npcSpawner;
		public ma02a_FLD_EventPop eventSpawner;
		public FLD_ConditionList conditions;

		public BdatCollection tables;

		public int flagPRT;
		public int questID;
		public ushort scenarioFlagMin;
		public string questName;

		public QuestEntry(FLD_QuestList quest, BdatCollection tables)
		{
			this.tables = tables;
			questName = quest._QuestTitle?.name;
			talker = quest._Talker;
			flagPRT = quest.FlagPRT;
			questID = flagPRT - 28769;
			npcs = new List<CommunityNPC>();
			SetSpawner(tables);
		}

		private void SetSpawner(BdatCollection tables)
		{
			var eventId = tables.EVT_listQst01.Where(z => z.evtName.Contains($"{questID}01")).Select(x => x.Id).FirstOrDefault();

			eventSpawner = tables.ma40a_FLD_EventPop.Union(tables.ma41a_FLD_EventPop).Where(x => x.EventID == eventId).FirstOrDefault();
			npcSpawner = tables.ma40a_FLD_NpcPop.Union(tables.ma41a_FLD_NpcPop).Where(x => x.EventID == eventId).FirstOrDefault();
			if (npcSpawner != null)
			{
				conditions = npcSpawner._Condition;
				scenarioFlagMin = npcSpawner.ScenarioFlagMin;
			}
			if (eventSpawner != null)
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


		public string GetScenarioPoint()
		{
			switch (scenarioFlagMin - 11000)
			{
				case 1: return "Start";
				case 18: return "What Bars the Way";
				case 19: return "What Bars the Way";
				case 29: return "Hugo joins";
				case 31: return "Kill Dispare Ropl";
				case 47: return "Exit Hugo's Ship";
				case 54: return "Reach Aletta";
				case 55: return "Reach Aletta";
				case 56: return "Feeding an Army";
				case 57: return "Feeding an Army";
				case 59: return "Lett Bridge Restoration";
				case 81: return "Reach Auresco";
				case 85: return "Receive community level 2 quest";
				case 96: return "Defeat Malos 1";
				case 105: return "Receive community level 4 quest";
				case 112: return "Reach Titan's interior";
				default: return scenarioFlagMin.ToString();
			}
		}

		public string GetCondition()
		{
			if (conditions == null) return "\t\tN/A";
			var sb = new StringBuilder();
			ushort[] conditionIds = { conditions.Condition1, conditions.Condition2, conditions.Condition3, conditions.Condition4,
				conditions.Condition5, conditions.Condition6, conditions.Condition7, conditions.Condition8 };
			ConditionType[] conditionTypes = { conditions._ConditionType1, conditions._ConditionType2, conditions._ConditionType3, conditions._ConditionType4,
				conditions._ConditionType5, conditions._ConditionType6, conditions._ConditionType7, conditions._ConditionType8 };
			for(int i = 0; i < 8; i++)
			{
				if (conditionIds[i] == 0) continue;
				switch (conditionTypes[i])
				{
					case ConditionType.Flag:
						sb.AppendLine($"\t\t{GetFlagReq(conditionIds[i])}");
						break;
					case ConditionType.Quest:
						sb.AppendLine($"{GetQuestReq(conditionIds[i])}");
						break;
					case ConditionType.Item:
						sb.AppendLine($"\t\tOwn {tables.FLD_ConditionItem[conditionIds[i]].Number} of item {tables.FLD_ConditionItem[conditionIds[i]].ItemID}");
						break;
					default:
						//sb.AppendLine($"\tCondition {i}: Something weird happened Gren @@@@@@@@@@@@@@@@@");
						break;
				}
			}
			if (sb.ToString().Length == 0) return "";
			return sb.ToString().Substring(0, sb.ToString().Length - 2);
		}

		private object GetFlagReq(int id)
		{
			if (tables.FLD_ConditionFlag[id].FlagID == 652)
				return $"Community lvl {tables.FLD_ConditionFlag[id].FlagMin} required";
			return $"Flag ID {tables.FLD_ConditionFlag[id].FlagID} must be between {tables.FLD_ConditionFlag[id].FlagMin} and {tables.FLD_ConditionFlag[id].FlagMax}";
		}

		public string GetQuestReq(int id)
		{
			var condition = tables.FLD_ConditionQuest[id];
			int[] questIds = { condition.QuestFlag1, condition.QuestFlag2, condition.NotQuestFlag1, condition.NotQuestFlag2 };
			int[] minFlags = { condition.QuestFlagMin1, condition.QuestFlagMin2, condition.NotQuestFlagMin1, condition.NotQuestFlagMin2 };
			int[] maxFlags = { condition.QuestFlagMax1, condition.QuestFlagMax2, condition.NotQuestFlagMax1, condition.NotQuestFlagMax2 };

			var sb = new StringBuilder();

			for (int i = 0; i < 4; i++)
			{
				if (questIds[i] == 0) continue;
				sb.AppendLine($"\t\tQuest \"" +
					$"{tables.FLD_QuestListNormalIra[questIds[i]]._QuestTitle?.name ?? tables.FLD_QuestListNormalIra[questIds[i]]._PRTQuestID?._QuestTitle?.name ?? "N/A"}" +
					$"\" must {(i < 2 ? "" : "NOT ")}be {GetCompletionReq(minFlags[i], maxFlags[i])}");
			}
			return sb.ToString().Substring(0, sb.ToString().Length - 2);
		}

		private string GetCompletionReq(int min, int max)
		{
			if (min == 0 && max == 0)
				return "NOT STARTED";
			if (min == 1 && max == 1)
				return "STARTED";
			if (min > 1 && max > 1)
				return "COMPLETED";
			if (min == 0 && max == 1)
				return "NOT COMPLETED";
			if (min == 1 && max > 1)
				return "STARTED OR COMPLETED";
			if (min == 0 && max > 1)
				return "LITERALLY ANYTHING";
			return "I MUST HAVE MISSED ONE";
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
