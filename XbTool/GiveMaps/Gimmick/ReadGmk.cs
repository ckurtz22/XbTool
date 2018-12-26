using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using GiveMaps.Common;
using GiveMaps.Types;
using static GiveMaps.Program;
using System;

namespace GiveMaps.Gimmick
{
    public static class ReadGmk
    {
        public static MapInfo[] ReadAll(Options options)
        {
			var progress = options.Progress;
            progress?.LogMessage("Reading map info and gimmick sets");
            Dictionary<string, MapInfo> maps = MapInfo.ReadAll("Data/menu/minimap");

            var mapList = options.Tables.FLD_maplist;
            var areaList = options.Tables.MNU_MapInfo;

            foreach (MapInfo mapInfo in maps.Values)
            {
                var map = mapList.FirstOrDefault(x => x.resource == mapInfo.Name && x._nameID != null);
                if (map == null) continue;
                if (map._nameID != null) mapInfo.DisplayName = map._nameID.name;

                foreach (var areaInfo in mapInfo.Areas)
                {
                    MNU_MapInfo area = areaList.FirstOrDefault(x =>
                        x.level_name == areaInfo.Name || x.level_name2 == areaInfo.Name);
                    if (area == null)
                    {
                        progress?.LogMessage($"Found area {areaInfo.Name} that is not in the BDAT tables");
                        continue;
                    }

                    // Some areas use one of 2 maps depending on the game state.
                    // These 2 maps are always the same except one has a small addition or removal.
                    // We want the map with the most objects on it, so we use the second map for
                    // Gormott (ma05a), and the first map for everywhere else.
                    if (!string.IsNullOrWhiteSpace(area.level_name2)
                        && area.level_name == areaInfo.Name
                        && (mapInfo.Name == "ma05a" || mapInfo.Name == "ma41a"))
                    {
                        areaInfo.Priority = 100;
                    }
                    else if (!string.IsNullOrWhiteSpace(area.level_name2)
                             && area.level_name2 == areaInfo.Name
                             && mapInfo.Name != "ma05a" && mapInfo.Name != "ma41a")
                    {
                        areaInfo.Priority = 100;
                    }
                    else
                    {
                        areaInfo.Priority = area.level_priority;
                    }

					/*if (area._disp_name?.name != null) areaInfo.DisplayName = ( area._disp_name.name == "Entire Area" ?
							options.Tables.ma40a_FLD_LandmarkPop.Union(options.Tables.ma41a_FLD_LandmarkPop)
							.FirstOrDefault(x => x._menuMapImage?.Id == area.Id)._menuGroup._disp_name.name : area._disp_name.name); //*/
                }

                var gimmickSet = ReadGimmickSet("Data/gmk", options.Tables, map.Id, options.Type);
				AssignGimmickAreas(gimmickSet, mapInfo, options);
			}

			return maps.Values.ToArray();
        }

        public static Dictionary<string, Lvb> ReadGimmickSet(string gmkDir, BdatCollection tables, int mapId, string gmkType)
        {
            RSC_GmkSetList setBdat = tables.RSC_GmkSetList.First(x => x.mapId == mapId);
            var fieldsDict = setBdat.GetType().GetFields().ToDictionary(x => x.Name, x => x);
            var fields = fieldsDict.Values.Where(x => x.FieldType == typeof(string) && !x.Name.Contains("_bdat") && (x.Name == gmkType || gmkType == "all"));
            var gimmicks = new Dictionary<string, Lvb>();

            foreach (FieldInfo field in fields)
            {
                var value = (string)field.GetValue(setBdat);
                if (value == null) continue;
                string filename = $"{gmkDir}/{value}.lvb";
				if (!File.Exists(filename)) continue;

                byte[] file = File.ReadAllBytes(filename);
                var lvb = new Lvb(new DataBuffer(file, Game.XB2, 0)) { Filename = field.Name };

                string bdatField = field.Name + "_bdat";
                if (fieldsDict.ContainsKey(bdatField))
                {
                    var bdatName = (string)fieldsDict[bdatField].GetValue(setBdat);
                    if (!string.IsNullOrWhiteSpace(bdatName)) lvb.BdatName = bdatName;
                }

                gimmicks.Add(field.Name, lvb);
            }

            return gimmicks;
        }

        public static void AssignGimmickAreas(Dictionary<string, Lvb> set, MapInfo mapInfo, Options options)
        {
            mapInfo.Gimmicks = set;
            foreach (var gmkType in set)
            {
                var type = gmkType.Key;
				if (options.Type != "all" && options.Type != type) continue;
                foreach (var gmk in gmkType.Value.Info)
                {
					if (!IsValidGmk(gmk, options)) continue;
                    MapAreaInfo area = mapInfo.GetContainingArea(gmk.Xfrm.Position);
                    area?.AddGimmick(gmk, type);
                }
            }
        }

		private static bool IsValidGmk(InfoEntry gmk, Options options)
		{
			if (gmk.Name == "") return false;
			switch (options.Type)
			{
				case "collection":
					var items = options.Tables.ITM_CollectionList.Where(x => options.Names.Contains(x._Name?.name));
					var gormottItem = options.Tables.ma41a_FLD_CollectionPopList.FirstOrDefault(x => x.name == gmk.Name)?._CollectionTable;
					var tornaItem = options.Tables.ma40a_FLD_CollectionPopList.FirstOrDefault(x => x.name == gmk.Name)?._CollectionTable;

					if (items.Contains(gormottItem?._itm1ID) || items.Contains(gormottItem?._itm2ID) || items.Contains(gormottItem?._itm3ID) || items.Contains(gormottItem?._itm4ID) ||
						items.Contains(tornaItem?._itm1ID) || items.Contains(tornaItem?._itm2ID) || items.Contains(tornaItem?._itm3ID) || items.Contains(tornaItem?._itm4ID)) return true;

					if (IsCollectableMainGame(items, options)) return true;


					if (options.Names.Contains(options.Tables.ma40a_FLD_CollectionPopList.FirstOrDefault(x => x.name == gmk.Name)?.Id.ToString()))
					{
						return true;
					}


					return false;

				case "enemy":
					var enemies = options.Tables.CHR_EnArrange.Where(x => options.Names.Contains(x._Name?.name));
					var gormottEnemy = options.Tables.ma41a_FLD_EnemyPop.FirstOrDefault(x => x.name == gmk.Name);
					var tornaEnemy = options.Tables.ma40a_FLD_EnemyPop.FirstOrDefault(x => x.name == gmk.Name);

					if (enemies.Contains(gormottEnemy?._ene1ID) || enemies.Contains(gormottEnemy?._ene2ID) || enemies.Contains(gormottEnemy?._ene3ID) || enemies.Contains(gormottEnemy?._ene4ID) ||
						enemies.Contains(tornaEnemy?._ene1ID) || enemies.Contains(tornaEnemy?._ene2ID) || enemies.Contains(tornaEnemy?._ene3ID) || enemies.Contains(tornaEnemy?._ene4ID)) return true;

					return false;

				case "npc":
					var npcs = options.Tables.RSC_NpcList.Where(x => options.Names.Contains(x._Name?.name));
					var npcPop = options.Tables.ma40a_FLD_NpcPop.Union(options.Tables.ma41a_FLD_NpcPop).FirstOrDefault(x => x.name == gmk.Name);

					var npcPop2 = options.Tables.ma08a_FLD_NpcPop.FirstOrDefault(x => x.name == gmk.Name);

					if (npcs.Contains(npcPop?._NpcID)) return true;
					if (npcs.Contains(npcPop2?._NpcID)) return true;
					return false;

				case "all":
					return true;

				default:
					if(options.Names.Contains(gmk.Name)) return true;
					return false;
			}
		
		}

		private static bool IsCollectableMainGame(IEnumerable<ITM_CollectionList> items, Options options)
		{
			var list1 = MainGameList1(options);
			var list2 = MainGameList2(options);

			//list1.


			return false;
		}

		public static List<ma02a_FLD_CollectionPopList> MainGameList1(Options options)
		{
			return options.Tables.ma02a_FLD_CollectionPopList.Union(options.Tables.ma05a_FLD_CollectionPopList.Union(options.Tables.ma08a_FLD_CollectionPopList.Union(options.Tables.ma11a_FLD_CollectionPopList.
				Union(options.Tables.ma10a_FLD_CollectionPopList.Union(options.Tables.ma15a_FLD_CollectionPopList.Union(options.Tables.ma16a_FLD_CollectionPopList.Union(options.Tables.ma17a_FLD_CollectionPopList.
				Union(options.Tables.ma18a_FLD_CollectionPopList.Union(options.Tables.ma20a_FLD_CollectionPopList.Union(options.Tables.ma21a_FLD_CollectionPopList.Union(options.Tables.ma90a_FLD_CollectionPopList))))))))))).ToList();

		}

		public static List<ma07a_FLD_CollectionPopList> MainGameList2(Options options)
		{
			return options.Tables.ma07a_FLD_CollectionPopList.Union(options.Tables.ma13a_FLD_CollectionPopList).ToList();
		}


	}
}
