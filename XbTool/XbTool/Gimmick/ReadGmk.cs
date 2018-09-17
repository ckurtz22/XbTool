using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using XbTool.Common;
using XbTool.Types;

namespace XbTool.Gimmick
{
    public static class ReadGmk
    {
        public static MapInfo[] ReadAll(Options options, IProgressReport progress = null)
        {
            progress?.LogMessage("Reading map info and gimmick sets");
            Dictionary<string, MapInfo> maps = MapInfo.ReadAll($"{options.DataDir}/menu/minimap");

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
                        && mapInfo.Name == "ma05a")
                    {
                        areaInfo.Priority = int.MaxValue;
                    }
                    else if (!string.IsNullOrWhiteSpace(area.level_name2)
                             && area.level_name2 == areaInfo.Name
                             && mapInfo.Name != "ma05a")
                    {
                        areaInfo.Priority = int.MaxValue;
                    }
                    else
                    {
                        areaInfo.Priority = area.level_priority;
                    }

                    if (area._disp_name?.name != null) areaInfo.DisplayName = area._disp_name.name;
                }

                var gimmickSet = ReadGimmickSet($"{options.DataDir}/gmk", options.Tables, map.Id);
				//AssignGimmickAreas(gimmickSet, mapInfo);
				//AssignGimmickCollectionAreas(gimmickSet, mapInfo, options);
				//AssignGimmickEnemyAreas(gimmickSet, mapInfo, options);
				AssignGimmickNpcAreas(gimmickSet, mapInfo, options);
			}

			return maps.Values.ToArray();
        }

        public static Dictionary<string, Lvb> ReadGimmickSet(string gmkDir, BdatCollection tables, int mapId)
        {
            RSC_GmkSetList setBdat = tables.RSC_GmkSetList.First(x => x.mapId == mapId);
            var fieldsDict = setBdat.GetType().GetFields().ToDictionary(x => x.Name, x => x);
            var fields = fieldsDict.Values.Where(x => x.FieldType == typeof(string) && !x.Name.Contains("_bdat"));
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

        public static void AssignGimmickAreas(Dictionary<string, Lvb> set, MapInfo mapInfo)
        {
            mapInfo.Gimmicks = set;
            foreach (var gmkType in set)
            {
                var type = gmkType.Key;
                foreach (var gmk in gmkType.Value.Info)
                {
                    MapAreaInfo area = mapInfo.GetContainingArea(gmk.Xfrm.Position);
                    area?.AddGimmick(gmk, type);
                }
            }
        }

		public static void AssignGimmickCollectionAreas(Dictionary<string, Lvb> set, MapInfo mapInfo, Options options)
		{
			mapInfo.Gimmicks = set;

			foreach (var gmkType in set)
			{
				var type = gmkType.Key;
				if (type != "collection") continue;
				foreach (var gmk in gmkType.Value.Info)
				{
					if (gmk.Name == "") continue;
					var items = options.Tables.ITM_CollectionList.Where(x => x._Name?.name == options.Filter);
					var gormottItem = options.Tables.ma41a_FLD_CollectionPopList.Where(x => x.name == gmk.Name);
					var tornaItem = options.Tables.ma40a_FLD_CollectionPopList.Where(x => x.name == gmk.Name);
					
					if (gormottItem.Count() > 0 && !(items.Contains(gormottItem.First()._CollectionTable._itm1ID) || items.Contains(gormottItem.First()._CollectionTable._itm2ID) || 
						items.Contains(gormottItem.First()._CollectionTable._itm3ID) || items.Contains(gormottItem.First()._CollectionTable._itm4ID))) { continue; }
					if (tornaItem.Count() > 0 && !(items.Contains(tornaItem.First()._CollectionTable._itm1ID) || items.Contains(tornaItem.First()._CollectionTable._itm2ID) || 
						items.Contains(tornaItem.First()._CollectionTable._itm3ID) || items.Contains(tornaItem.First()._CollectionTable._itm4ID))) { continue; }
					if (tornaItem.Count() == 0 && gormottItem.Count() == 0) continue;

					MapAreaInfo area = mapInfo.GetContainingArea(gmk.Xfrm.Position);
					area?.AddGimmick(gmk, type);
				}
			}
		}

		public static void AssignGimmickEnemyAreas(Dictionary<string, Lvb> set, MapInfo mapInfo, Options options)
		{
			mapInfo.Gimmicks = set;

			foreach (var gmkType in set)
			{
				var type = gmkType.Key;
				if (type != "enemy") continue;
				foreach (var gmk in gmkType.Value.Info)
				{
					if (gmk.Name == "") continue;
					var enemies = options.Tables.CHR_EnArrange.Where(x => x._Name?.name == options.Filter);
					var gormottEnemy = options.Tables.ma41a_FLD_EnemyPop.Where(x => x.name == gmk.Name);
					var tornaEnemy = options.Tables.ma40a_FLD_EnemyPop.Where(x => x.name == gmk.Name);

					if (gormottEnemy.Count() > 0 && !(enemies.Contains(gormottEnemy.First()._ene1ID) || enemies.Contains(gormottEnemy.First()._ene2ID) || 
						enemies.Contains(gormottEnemy.First()._ene3ID) || enemies.Contains(gormottEnemy.First()._ene4ID))) { continue; }
					if (tornaEnemy.Count() > 0 && !(enemies.Contains(tornaEnemy.First()._ene1ID) || enemies.Contains(tornaEnemy.First()._ene2ID) ||
						enemies.Contains(tornaEnemy.First()._ene3ID) || enemies.Contains(tornaEnemy.First()._ene4ID))) { continue; }
					if (tornaEnemy.Count() == 0 && gormottEnemy.Count() == 0) continue;

					MapAreaInfo area = mapInfo.GetContainingArea(gmk.Xfrm.Position);
					area?.AddGimmick(gmk, type);

				}
			}
		}

		public static void AssignGimmickNpcAreas(Dictionary<string, Lvb> set, MapInfo mapInfo, Options options)
		{
			mapInfo.Gimmicks = set;

			foreach (var gmkType in set)
			{
				var type = gmkType.Key;
				if (type != "npc") continue;
				foreach (var gmk in gmkType.Value.Info)
				{
					if (gmk.Name == "") continue;
					var enemies = options.Tables.RSC_NpcList.Where(x => x._Name?.name == options.Filter);
					var gormottNpc = options.Tables.ma41a_FLD_NpcPop.Where(x => x.name == gmk.Name);
					var tornaNpc = options.Tables.ma40a_FLD_NpcPop.Where(x => x.name == gmk.Name);

					if (gormottNpc.Count() > 0 && !enemies.Contains(gormottNpc.First()._NpcID)) { continue; }
					if (tornaNpc.Count() > 0 && !enemies.Contains(tornaNpc.First()._NpcID)) { continue; }
					if (tornaNpc.Count() == 0 && gormottNpc.Count() == 0) continue;

					MapAreaInfo area = mapInfo.GetContainingArea(gmk.Xfrm.Position);
					area?.AddGimmick(gmk, type);

				}
			}
		}

		private static bool isItemGmk(InfoEntry gmk, Options options)
		{


			return true;
		}
	}
}
