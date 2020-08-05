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
                    var gmkItem = options.Bdats.Tables.Where(tbl => tbl.Key.Contains("FLD_CollectionPopList")).SelectMany(tbl => tbl.Value.Items).FirstOrDefault(itm => itm["name"].DisplayString == gmk.Name);
                    if (gmkItem == null) return false;
                    if (options.Names.Intersect(gmkItem.Values.Select(itm => itm.Value.DisplayString)).Any())
                        return true;
                    if (gmkItem.Values.ContainsKey("CollectionTable") && gmkItem["CollectionTable"].Reference != null)
                        if (options.Names.Intersect(gmkItem["CollectionTable"].Reference.Values.Select(itm => itm.Value.DisplayString)).Any())
                            return true;
                    return false;

				case "enemy":
                    var enemies = options.Bdats.Tables["CHR_EnArrange"].Items.Where(enemy => options.Names.Contains(enemy["Name"].DisplayString));
                    var mapEnemies = options.Bdats.Tables.Where(tbl => tbl.Key.Contains("FLD_EnemyPop")).SelectMany(tbl => tbl.Value.Items).FirstOrDefault(itm => itm["name"].DisplayString == gmk.Name);
                    if (mapEnemies == null) return false;
                    foreach (var enemy in mapEnemies.Values.Where(val=>val.Key.StartsWith("ene") && val.Key.EndsWith("ID")))
                        if (enemies.Any(en => en["Name"].DisplayString == enemy.Value.DisplayString))
                            return true;
					return false;

				case "npc":
                    var npcPop = options.Bdats.Tables.Where(tbl => tbl.Key.Contains("FLD_NpcPop")).SelectMany(tbl => tbl.Value.Items).FirstOrDefault(itm => itm["name"].DisplayString == gmk.Name);
                    return npcPop != null && options.Names.Any(name => name == npcPop["NpcID"].DisplayString);

				case "all":
					return true;

				default:
					if(options.Names.Contains(gmk.Name)) return true;
					return false;
			}
		
		}


	}
}
