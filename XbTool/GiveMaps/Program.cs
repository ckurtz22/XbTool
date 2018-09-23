using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GiveMaps.Bdat;
using GiveMaps.Common;
using GiveMaps.Gimmick;
using GiveMaps.Serialization;
using GiveMaps.Types;

namespace GiveMaps
{
    public static class Program
    {
        public static void Main(string[] args)
        {
			Options options = new Options
			{
				Game = Common.Game.XB2,
				DataDir = Directory.GetCurrentDirectory() + "/Data",
				Output = Directory.GetCurrentDirectory() + "/Maps"
			};
			ReadGimmick(options);
        }

		public class Options
		{
			public Game Game { get; set; }
			public string DataDir { get; set; }
			public string Input { get; set; }
			public string Output { get; set; }
			public List<string> Names { get; set; }
			public string Type { get; set; }
			public BdatCollection Tables { get; set; }
			public IProgressReport Progress { get; set; }
		}

		private static void ReadGimmick(Options options)
		{
			string[] filenames = Directory.GetFiles($"Data/bdat", "*");
			BdatTables bdats = new BdatTables(filenames, options.Game, false);
			options.Tables = Deserialize.DeserializeTables(bdats);


			foreach (string type in Gimmick.Types.GimmickFieldNames)
			{
				if (!File.Exists($"{type}.txt")) continue;
				options.Names = new List<string>(File.ReadAllLines($"{type}.txt"));
				options.Type = type;
				var gimmicks = ReadGmk.ReadAll(options);
				//ExportMap.Export(options, gimmicks);
				ExportMap.MakeMap(options, gimmicks);
			}
			//ExportMap.ExportCsv(gimmicks, options.Output);*/
		}
	}




    public static class Stuff
    {
        public static string ReadUTF8(this BinaryReader reader, int size)
        {
            return Encoding.UTF8.GetString(reader.ReadBytes(size), 0, size);
        }

        public static void CopyStream(this Stream input, Stream output, int length)
        {
            int remaining = length;
            byte[] buffer = new byte[32768];
            int read;
            while ((read = input.Read(buffer, 0, Math.Min(buffer.Length, remaining))) > 0)
            {
                output.Write(buffer, 0, read);
                remaining -= read;
            }
        }

        public static string GetUTF8Z(byte[] value, int offset)
        {
            var length = 0;

            while (value[offset + length] != 0)
            {
                length++;
            }

            return Encoding.UTF8.GetString(value, offset, length);
        }

    }
}
