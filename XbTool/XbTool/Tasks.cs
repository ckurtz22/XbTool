using System.IO;
using XbTool.Bdat;
using XbTool.Common;
using XbTool.Gimmick;
using XbTool.Serialization;
using XbTool.Types;

namespace XbTool
{
    public static class Tasks
    {
        internal static void RunTask(Options options)
        {
            using (var progress = new ProgressBar())
            {
                options.Progress = progress;
                ReadGimmick(options);
            }
        }
		
        private static BdatTables ReadBdatTables(Options options, bool readMetadata)
        {
            string pattern = "*";
            string[] filenames = Directory.GetFiles($"{options.DataDir}/bdat" , pattern);
            return new BdatTables(filenames, options.Game, readMetadata);
        }
		
        public static BdatCollection GetBdatCollection(Options options)
        {
            BdatTables bdats = ReadBdatTables(options, false);
            BdatCollection tables = Deserialize.DeserializeTables(bdats);
            return tables;
        }

		private static void ReadGimmick(Options options)
		{
			options.Tables = GetBdatCollection(options);

			foreach (string type in Gimmick.Types.GimmickFieldNames)
			{
				if (File.Exists($"{options.DataDir}/../{type}.txt")) ;
				var items = File.ReadAllLines($"{options.DataDir}/../npcs.txt");
				foreach (string name in items)
				{
					options.Filter = name;
					var gimmicks = ReadGmk.ReadAll(options);
					ExportMap.Export(options, gimmicks);
				}
			}
			//ExportMap.ExportCsv(gimmicks, options.Output);
		}
    }
}
