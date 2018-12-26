using System;
using System.Linq;
using System.IO;
using XbTool.Bdat;
using XbTool.BdatString;
using XbTool.CodeGen;
using XbTool.Common;
using XbTool.Common.Textures;
using XbTool.CreateBlade;
using XbTool.Gimmick;
using XbTool.Salvaging;
using XbTool.Save;
using XbTool.Scripting;
using XbTool.Serialization;
using XbTool.Types;
using XbTool.Xb2;
using System.Collections.Generic;
using System.Text;
using LibHac.IO;

namespace XbTool
{
    public static class Tasks
    {
        internal static void RunTask(Options options)
        {
            using (var progress = new ProgressBar())
            {
                options.Progress = progress;
                switch (options.Task)
                {
                    case Task.ExtractArchive:
                        ExtractArchive(options);
                        break;
                    case Task.DecryptBdat:
                        DecryptBdat(options);
                        break;
                    case Task.BdatCodeGen:
                        BdatCodeGen(options);
                        break;
                    case Task.Bdat2Html:
                        Bdat2Html(options);
                        break;
                    case Task.Bdat2Json:
                        Bdat2Json(options);
                        break;
                    case Task.GenerateData:
                        GenerateData(options);
                        break;
                    case Task.CreateBlade:
                        CreateBlade(options);
                        break;
                    case Task.ExtractWilay:
                        ExtractWilay(options);
                        break;
                    case Task.DescrambleScript:
                        DescrambleScript(options);
                        break;
                    case Task.SalvageRaffle:
                        SalvageRaffle(options);
                        break;
                    case Task.ReadSave:
                        ReadSave(options);
                        break;
                    case Task.DecompressIraSave:
                        DecompressSave(options);
                        break;
                    case Task.CombineBdat:
                        CombineBdat(options);
                        break;
                    case Task.ReadGimmick:
                        ReadGimmick(options);
                        break;
                    case Task.ReadScript:
                        ReadScript(options);
                        break;
                    case Task.DecodeCatex:
                        DecodeCatex(options);
                        break;
                    case Task.ExtractMinimap:
                        ExtractMinimap(options);
                        break;
                    case Task.GenerateSite:
                        GenerateSite(options);
                        break;
                    case Task.ExportQuests:
                        ExportQuests(options);
                        break;
                    case Task.ReplaceArchive:
                        ReplaceArchive(options);
                        break;
					case Task.CommunityQuests:
						CommunityQuests(options);
						break;
					case Task.GenerateTreeReqs:
						GenerateTreeReqs(options);
						break;

					default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

		private static int getCount(FLD_QuestTask task)
		{
			switch (task?._TaskID1.GetType().Name)
			{
				case "FLD_QuestBattle":
					FLD_QuestBattle battle = (FLD_QuestBattle)task._TaskID1;
					return battle.Count;

				case "FLD_QuestCollect":
					FLD_QuestCollect collect = (FLD_QuestCollect)task._TaskID1;
					return collect.Count;

				case "FLD_Achievement":
					FLD_Achievement achieve = (FLD_Achievement)task._TaskID1;
					return (int)achieve.Count;

				case "FLD_QuestUse":
					FLD_QuestUse item = (FLD_QuestUse)task._TaskID1;
					return item.ItemNumber;

				case "FLD_QuestFieldSkillCount":
					return 5;

				case "FLD_QuestCondition":
					FLD_QuestCondition quest = (FLD_QuestCondition)task._TaskID1;
					if (quest._ConditionID.ConditionType1 == 7)
					{
						FLD_ConditionIdea idea = (FLD_ConditionIdea)quest._ConditionID._Condition1;
						return idea.TrustPoint;
					}
					return 0;

				default:
					return 0;
			}
		}

		private static void GenerateTreeReqs(Options options)
		{
			var sb = new StringBuilder();
			BdatCollection tables = GetBdatCollection(options);
			var headerFilename = options.ArhFilename;
			var HeaderFile = File.ReadAllBytes(headerFilename);
			var headerFile = new byte[HeaderFile.Length];
			Array.Copy(HeaderFile, headerFile, HeaderFile.Length);
			FileArchive.DecryptArh(headerFile);
			File.WriteAllBytes("bf2_d.arh", headerFile);
			foreach(var blade in tables.CHR_Bl)
			{
				if (blade._Name == null || blade._BArts3 == null || blade._BSkill3 == null) continue;
				sb.AppendLine(blade._Name.name);
				sb.AppendLine();
				sb.AppendLine("Key Affinity\tRequirement");

				for(int i = 0; i < 5; i++)
				{
					var task = blade._KeyAchievement?._AchievementID[i]._Task?._NextQuestA._PurposeID;
					var count = getCount(task);
					if (task != null)
						sb.AppendLine($"Affinity Level {i + 1}\t" + task._TaskLog1.name.Replace('\n', ' ') + (count == 0 ? "" : $" ({count})"));
					else
						sb.AppendLine();
				}

				sb.AppendLine("Special 1\tRequirement\tSpecial 2\tRequirement\tSpecial 3\tRequirement\t");

				for (int i = 0; i < 5; i++)
				{
					for (int j = 0; j < 3; j++)
					{
						var task = blade._ArtsAchievement[j]?._AchievementID[i]?._Task?._NextQuestA?._PurposeID;
						var count = getCount(task);
						if (task != null)
							sb.Append($"{blade._BArts[j]._Name.name} {i + 1}\t" + task._TaskLog1.name.Replace('\n', ' ') + (count == 0 ? "" : $" ({count})") + "\t");
						else
							sb.Append("\t\t");
					}
					sb.AppendLine();
				}

				sb.AppendLine("Battle Skill 1\tRequirement\tBattle Skill 2\tRequirement\tBattle Skill 3\tRequirement\t");

				for (int i = 0; i < 5; i++)
				{
					for (int j = 0; j < 3; j++)
					{
						var task = blade._SkillAchievement[j]?._AchievementID[i]?._Task?._NextQuestA?._PurposeID;
						var count = getCount(task);
						if (task != null)
							sb.Append($"{blade._BSkill[j]._Name.name} {i + 1}\t" + task._TaskLog1.name.Replace('\n', ' ') + (count == 0 ? "" : $" ({count})") + "\t");
						else
							sb.Append("\t\t");
					}
					sb.AppendLine();
				}

				sb.AppendLine("Field Skill 1\tRequirement\tField Skill 2\tRequirement\tField Skill 3\tRequirement\t");

				for (int i = 0; i < 5; i++)
				{
					for (int j = 0; j < 3; j++)
					{
						var task = blade._FskillAchivement[j]?._AchievementID[i]?._Task?._NextQuestA?._PurposeID;
						var count = getCount(task);
						if (task != null)
							sb.Append($"{blade._FSkill[j]._Name.name} {i + 1}\t" + task._TaskLog1.name.Replace('\n', ' ') + (count == 0 ? "" : $" ({count})") + "\t");
						else
							sb.Append("\t\t");
					}
					sb.AppendLine();
				}
				
				sb.AppendLine();
				sb.AppendLine();

			}
			File.WriteAllText(options.Output, sb.ToString());

		}

		private static void ExtractArchive(Options options)
		{
			using (var archive = new FileArchive(options))
			{
				FileArchive.Extract(archive, options.Output, options.Progress, options.Filter);
			}
		}

		private static void ReplaceArchive(Options options)
        {
            if (options.Input == null) throw new NullReferenceException("No input file was specified.");
            if (options.Output == null) throw new NullReferenceException("No output file was specified.");

            using (var archive = new FileArchive(options))
            {
                var replacement = File.ReadAllBytes(options.Input);
                archive.ReplaceFile(options.Output, replacement);
            }
        }

        private static void DecryptBdat(Options options)
        {
            if (options.Input == null) throw new NullReferenceException("No input file was specified.");

            if (File.Exists(options.Input))
            {
                string output = options.Output ?? options.Input;
                DecryptFile(options.Input, output);
            }

            if (Directory.Exists(options.Input))
            {
                string pattern = options.Filter ?? "*";
                string[] filenames = Directory.GetFiles(options.Input, pattern);
                foreach (string filename in filenames)
                {
                    DecryptFile(filename, filename);
                }
            }

            void DecryptFile(string input, string output)
            {
                var bdat = new DataBuffer(File.ReadAllBytes(input), options.Game, 0);
                BdatTools.DecryptBdat(bdat);
                File.WriteAllBytes(output, bdat.File);
                Console.WriteLine("Finished decrypting");
            }
        }

        private static void BdatCodeGen(Options options)
        {
            if (options.Output == null) throw new NullReferenceException("Output file was not specified.");

            BdatTables bdats = ReadBdatTables(options, true);
            SerializationCode.CreateFiles(bdats, options.Output);
        }

        private static BdatTables ReadBdatTables(Options options, bool readMetadata)
        {
			if (options.Game == Game.XB2 && (options.ArdFilename != null || options.SwitchFsDir != null) && options.BdatDir == null)		//Check if bdat is null to pass in bdats for Torna archive
            {
                using (var archive = new FileArchive(options))
                {
                    return new BdatTables(archive, readMetadata);
                }
            }

            string pattern = options.Filter ?? "*";
            string[] filenames = Directory.GetFiles(options.BdatDir, pattern);
            return new BdatTables(filenames, options.Game, readMetadata);
        }

        private static BdatStringCollection GetBdatStringCollection(Options options)
        {
            BdatTables bdats = ReadBdatTables(options, true);
            BdatStringCollection tables = DeserializeStrings.DeserializeTables(bdats);
            Metadata.ApplyMetadata(tables);
            return tables;
        }

        public static BdatCollection GetBdatCollection(Options options)
        {
            BdatTables bdats = ReadBdatTables(options, false);
            BdatCollection tables = Deserialize.DeserializeTables(bdats);
            return tables;
        }

        private static void Bdat2Html(Options options)
        {
            if (options.Output == null) throw new NullReferenceException("Output directory was not specified.");

            var tables = GetBdatStringCollection(options);
            HtmlGen.PrintSeparateTables(tables, options.Output, options.Progress);
        }

        private static void Bdat2Json(Options options)
        {
            if (options.Output == null) throw new NullReferenceException("Output directory was not specified.");

            var tables = GetBdatStringCollection(options);
            JsonGen.PrintAllTables(tables, options.Output, options.Progress);
        }

        private static void GenerateData(Options options)
        {
            if (options.Output == null) throw new NullReferenceException("Output directory was not specified.");

            var tables = GetBdatCollection(options);
            Directory.CreateDirectory(options.Output);

            var chBtlRewards = ChBtlRewards.PrintHtml(tables);
            File.WriteAllText(Path.Combine(options.Output, "chbtlrewards.html"), chBtlRewards);

            var chBtlRewardsCsv = ChBtlRewards.PrintCsv(tables);
            File.WriteAllText(Path.Combine(options.Output, "chbtlrewards.csv"), chBtlRewardsCsv);

            var salvaging = SalvagingTable.Print(tables);
            File.WriteAllText(Path.Combine(options.Output, "salvaging.html"), salvaging);

            using (var writer = new StreamWriter(Path.Combine(options.Output, "enemies.csv")))
            {
                Enemies.PrintEnemies(tables, writer);
            }

            using (var writer = new StreamWriter(Path.Combine(options.Output, "achievements.csv")))
            {
                Achievements.PrintAchievements(tables, writer);
            }
        }

        private static void CreateBlade(Options options)
        {
            var tables = GetBdatCollection(options);
            Run.PromptCreate(tables);
        }

        private static void ExtractWilay(Options options)
        {
            if (options.Input == null && options.ArdFilename == null) throw new NullReferenceException("Input was not specified.");
            if (options.Output == null) throw new NullReferenceException("Output directory was not specified.");

            if (options.ArdFilename != null || options.SwitchFsDir != null)
            {
                string input = options.Input ?? "/menu/image/";
                using (var archive = new FileArchive(options))
                {
                    Extract.ExtractTextures(archive, input, options.Output, options.Progress);
                }
            }
            else
            {
                if (File.Exists(options.Input))
                {
                    Extract.ExtractTextures(new[] { options.Input }, options.Output, options.Progress);
                }

                if (Directory.Exists(options.Input))
                {
                    string pattern = options.Filter ?? "*";
                    string[] filenames = Directory.GetFiles(options.Input, pattern);
                    Extract.ExtractTextures(filenames, options.Output, options.Progress);
                }
            }
        }

        private static void DescrambleScript(Options options)
        {
            if (options.Input == null) throw new NullReferenceException("No input file was specified.");

            if (File.Exists(options.Input))
            {
                string output = options.Output ?? options.Input;
                DescrambleFile(options.Input, output);
            }

            if (Directory.Exists(options.Input))
            {
                string pattern = options.Filter ?? "*";
                string[] filenames = Directory.GetFiles(options.Input, pattern);
                foreach (string filename in filenames)
                {
                    DescrambleFile(filename, filename);
                }
            }

            void DescrambleFile(string input, string output)
            {
                var script = new DataBuffer(File.ReadAllBytes(input), options.Game, 0);
                ScriptTools.DescrambleScript(script);
                File.WriteAllBytes(output, script.ToArray());
            }
        }

        private static void SalvageRaffle(Options options)
        {
            var tables = GetBdatCollection(options);
            RunRaffle.Run(tables);
        }

        private static void ReadSave(Options options)
        {
            if (options.Input == null) throw new NullReferenceException("No input file was specified.");
            if (options.Output == null) throw new NullReferenceException("No output file was specified.");

            byte[] saveFile = File.ReadAllBytes(options.Input);
            SDataSave saveData = Read.ReadSave(saveFile);
            var newSave = Write.WriteSave(saveData);
            File.WriteAllBytes(options.Input + "_new.sav", newSave);

            BdatCollection tables = GetBdatCollection(options);
            string printout = Print.PrintSave(saveData, tables);
            File.WriteAllText(options.Output, printout);
        }

        private static void DecompressSave(Options options)
        {
            if (options.Input == null) throw new NullReferenceException("No input file was specified.");
            if (options.Output == null) throw new NullReferenceException("No output file was specified.");

            byte[] saveFileComp = File.ReadAllBytes(options.Input);
            var saveFileDecomp = Compression.DecompressSave(saveFileComp);
            File.WriteAllBytes(options.Output, saveFileDecomp);


        }

        private static void CombineBdat(Options options)
        {
            //if (options.Input == null) throw new NullReferenceException("No input file was specified.");
            if (options.Output == null) throw new NullReferenceException("No output file was specified.");

            var tables = ReadBdatTables(options, false).Tables;
            var combined = BdatTools.Combine(tables);
            File.WriteAllBytes(options.Output, combined);
        }

        private static void ReadGimmick(Options options)
		{
			if (options.Output == null) throw new NullReferenceException("No output file was specified.");
			BdatCollection tables = GetBdatCollection(options);

			using (var archive = new FileArchive(options))
            {
                var gimmicks = ReadGmk.ReadAll(archive, tables);
                ExportMap.ExportCsv(gimmicks, options.Output);

                ExportMap.Export(archive, gimmicks, options.Output);
            }
        }

		private static void ReadScript(Options options)
		{
			//if (options.Input == null) throw new NullReferenceException("No input directory was specified.");
			if (options.Output == null) throw new NullReferenceException("No output directory was specified.");

			using (var archive = new FileArchive(options))
			{

				var files = archive.FindFiles("/script/jp/*.sb");
				Directory.CreateDirectory(options.Output);

				options.Progress.SetTotal(files.Count());
				foreach (var name in files)
				{
					if (name == null) continue;
					if (!(name.Contains("bs13a"))) continue;
					var file = archive.ReadFile(name);
					var script = new Script(new DataBuffer(file, options.Game, 0));
					var dump = Export.PrintScript(script);
					//var relativePath = Helpers.GetRelativePath(name, options.Input);

					var output = Path.ChangeExtension(Path.Combine(options.Output, name.Substring(11)), "txt");
					Directory.CreateDirectory(Path.GetDirectoryName(output) ?? "");
					File.WriteAllText(output, dump);
					options.Progress.ReportAdd(1);
				}
			}
		}

		private static void CommunityQuests(Options options)
		{
			BdatCollection tables = GetBdatCollection(options);

			using (var archive = new FileArchive(options))
			{
				var files = archive.FindFiles("/script/jp/*.sb");
				var scripts = new List<Script>();
				foreach (var name in files)
				{
					var file = archive.ReadFile(name);
					var script = new Script(new DataBuffer(file, options.Game, 0));
					if (script.Plugins.Count(x => x.Function == "openHitonowa") > 0)
						scripts.Add(script);
				}
				CommunityQuests comm = new CommunityQuests(scripts.ToArray(), tables);
				comm.PrintFile(options);
			}
		}

        private static void DecodeCatex(Options options)
        {
            if (options.Input == null) throw new NullReferenceException("No input file was specified.");
            if (options.Output == null) throw new NullReferenceException("No output path was specified.");

            if (File.Exists(options.Input))
            {
                Xbx.Textures.Extract.ExtractTextures(new[] { options.Input }, options.Output);
            }

            if (Directory.Exists(options.Input))
            {
                string pattern = options.Filter ?? "*";
                string[] filenames = Directory.GetFiles(options.Input, pattern);
                Xbx.Textures.Extract.ExtractTextures(filenames, options.Output);
            }
        }

        private static void ExtractMinimap(Options options)
        {
            if (options.Game != Game.XBX) throw new NotImplementedException("Xenoblade X minimap only.");
            if (options.Input == null) throw new NullReferenceException("No input file was specified.");
            if (options.Output == null) throw new NullReferenceException("No output path was specified.");
            if (!Directory.Exists(options.Input)) throw new DirectoryNotFoundException($"{options.Input} is not a valid directory.");

            Xbx.Textures.Minimap.ExtractMinimap(options.Input, options.Output);
        }

        private static void GenerateSite(Options options)
        {
            if (options.Xb2Dir == null) throw new NullReferenceException("Must specify XB2 Directory.");
            if (options.Output == null) throw new NullReferenceException("No output path was specified.");
            if (!Directory.Exists(options.Xb2Dir)) throw new DirectoryNotFoundException($"{options.Xb2Dir} is not a valid directory.");

            options.Progress.LogMessage("Reading XB2 directories");
            using (var xb2Fs = new Xb2Fs(options.Xb2Dir))
            {
                Website.Generate.GenerateSite(xb2Fs, options.Output, options.Progress);
            }
        }

        private static void ExportQuests(Options options)
        {
            if (options.Output == null) throw new NullReferenceException("No output path was specified.");

            var tables = GetBdatCollection(options);

            Xb2.Quest.Read.ExportQuests(tables, options.Output);
        }
    }
}
