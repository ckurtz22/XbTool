﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using XbTool.Common;
using XbTool.Types;

namespace XbTool.BdatString
{
    public static class BdatStringTools
    {
        public static string GetEnhanceCaption(BdatStringValue value)
        {
            BdatStringItem item = value.Parent;
            BdatStringCollection tables = item.Table.Collection;

            int captionId = int.Parse(value.ValueString);
            string caption = tables["btl_enhance_cap"][captionId]?["name"].ValueString;
            if (caption == null) return null;

            var sb = new StringBuilder(caption);

            List<BdatTag> tags = ParseTags(caption);

            foreach (BdatTag tag in tags.OrderByDescending(x => x.Start))
            {
                if (tag.SubType != "Enhance") continue;
                string replace = string.Empty;

                if (tag.Values.Count <= 0)
                {
                    int effectId = int.Parse(item["EnhanceEffect"].ValueString);
                    replace = tables["BTL_EnhanceEff"][effectId]["Param"].ValueString;
                }
                else if (tag.Values.TryGetValue("kind", out string field))
                {
                    replace = item[field].ValueString;
                }

                sb.Remove(tag.Start, tag.Length);
                sb.Insert(tag.Start, replace);
            }

            return sb.ToString();
        }

        public static string GetEnhanceCaptionXbx(BdatStringValue value)
        {
            BdatStringItem item = value.Parent;
            BdatStringCollection tables = item.Table.Collection;

            int captionId = int.Parse(value.ValueString);
            string caption = tables["BTL_Enhance_ms"][captionId]?["name"].ValueString;

            if (caption == null) return null;

            caption = caption
                .Replace("<<enp0>>", item["ratio"].DisplayString)
                .Replace("<<enp1>>", item["param1"].DisplayString)
                .Replace("<<enp2>>", item["param2"].DisplayString);

            return caption;
        }

        public static string GetItemTableXb1(ItemTypeXb1 type)
        {
            switch (type)
            {
                case ItemTypeXb1.Weapon: return "ITM_wpnlist";
                case ItemTypeXb1.Gem: return "BTL_skilllist";
                case ItemTypeXb1.HeadArmor: return "ITM_equiplist";
                case ItemTypeXb1.BodyArmor: return "ITM_equiplist";
                case ItemTypeXb1.ArmArmor: return "ITM_equiplist";
                case ItemTypeXb1.LegArmor: return "ITM_equiplist";
                case ItemTypeXb1.FootArmor: return "ITM_equiplist";
                case ItemTypeXb1.Crystal: return "ITM_crystallist";
                case ItemTypeXb1.Collectable: return "ITM_collectlist";
                case ItemTypeXb1.Material: return "ITM_materiallist";
                case ItemTypeXb1.KeyItem: return "ITM_valuablelist";
                case ItemTypeXb1.ArtBook: return "ITM_artslist";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static string GetQuestJournalTableXb1(int id) => $"JNL_quest{GetQuestTableSuffixXb1(id)}";
        public static string GetQuestMenuTableXb1(int id) => $"MNU_qt{GetQuestTableSuffixXb1(id)}";

        private static string GetQuestTableSuffixXb1(int id)
        {
            if (id > 1200) return "2601";
            if (id > 1000) return "2501";
            if (id > 960) return "2201";
            if (id > 920) return "2101";
            if (id > 890) return "2001";
            if (id > 850) return "1901";
            if (id > 849) return "1801";
            if (id > 810) return "1701";
            if (id > 809) return "1602";
            if (id > 750) return "1601";
            if (id > 720) return "1501";
            if (id > 680) return "1401";
            if (id > 640) return "1301";
            if (id > 625) return "1202";
            if (id > 610) return "1201";
            if (id > 535) return "1101";
            if (id > 495) return "1001";
            if (id > 465) return "0901";
            if (id > 464) return "0801";
            if (id > 350) return "0701";
            if (id > 310) return "0601";
            if (id > 275) return "0501";
            if (id > 260) return "0402";
            if (id > 173) return "0401";
            if (id > 115) return "0301";
            if (id > 85) return "0201";
            return "0101";
        }

        public static string GetEnemyTableXb1(int id) => $"BTL_enelist{GetEnemyTableSuffixXb1(id)}";

        private static string GetEnemyTableSuffixXb1(int id)
        {
            if (id > 3500) return "6001";
            if (id > 3450) return "5901";
            if (id > 3400) return "5801";
            if (id > 3350) return "5701";
            if (id > 3300) return "5601";
            if (id > 3250) return "5501";
            if (id > 3200) return "5401";
            if (id > 3150) return "5301";
            if (id > 3100) return "5201";
            if (id > 3050) return "5101";
            if (id > 2900) return "2601";
            if (id > 2700) return "2501";
            if (id > 2600) return "2401";
            if (id > 2500) return "2301";
            if (id > 2400) return "2201";
            if (id > 2300) return "2101";
            if (id > 2200) return "2001";
            if (id > 2100) return "1901";
            if (id > 1900) return "1701";
            if (id > 1700) return "1601";
            if (id > 1600) return "1501";
            if (id > 1500) return "1401";
            if (id > 1400) return "1301";
            if (id > 1300) return "1202";
            if (id > 1200) return "1201";
            if (id > 1100) return "1101";
            if (id > 1000) return "1001";
            if (id > 900) return "0901";
            if (id > 700) return "0701";
            if (id > 600) return "0601";
            if (id > 500) return "0501";
            if (id > 400) return "0402";
            if (id > 300) return "0401";
            if (id > 200) return "0301";
            if (id > 100) return "0201";
            return "0101";
        }

        public static string GetItemTableXbx(ItemTypeXbx type)
        {
            switch (type)
            {
                case ItemTypeXbx.Lot: return "DRP_PcWpnLotTable";
                case ItemTypeXbx.HeadArmor:
                case ItemTypeXbx.BodyArmor:
                case ItemTypeXbx.ArmArmorR:
                case ItemTypeXbx.ArmArmorL:
                case ItemTypeXbx.LegArmor: return "AMR_PcList";
                case ItemTypeXbx.RangedWeapon: return "WPN_PcList";
                case ItemTypeXbx.MeleeWeapon: return "WPN_PcList";
                case ItemTypeXbx.Item8: return null;
                case ItemTypeXbx.Item9: return null;
                case ItemTypeXbx.SkellHeadArmor:
                case ItemTypeXbx.SkellBodyArmor:
                case ItemTypeXbx.SkellArmArmorR:
                case ItemTypeXbx.SkellArmArmorL:
                case ItemTypeXbx.SkellLegArmor: return "AMR_DlList";
                case ItemTypeXbx.SkellWeaponTypeA:
                case ItemTypeXbx.SkellWeaponTypeB:
                case ItemTypeXbx.SkellWeaponTypeC:
                case ItemTypeXbx.SkellWeaponTypeD:
                case ItemTypeXbx.SkellWeaponTypeE: return "WPN_DlList";
                case ItemTypeXbx.Item20: return null;
                case ItemTypeXbx.AugmentGround: return "BTL_ItemSkill_inner";
                case ItemTypeXbx.Item22: return null;
                case ItemTypeXbx.Item23: return null;
                case ItemTypeXbx.AugmentSkell: return "BTL_ItemSkill_doll";
                case ItemTypeXbx.Item25: return null;
                case ItemTypeXbx.Item26: return null;
                case ItemTypeXbx.Item27: return null;
                case ItemTypeXbx.Item28: return null;
                case ItemTypeXbx.Item29: return null;
                case ItemTypeXbx.Item30: return null;
                case ItemTypeXbx.Item31: return null;
                case ItemTypeXbx.Item32: return null;
                case ItemTypeXbx.Item33: return null;
                case ItemTypeXbx.Item34: return null;
                case ItemTypeXbx.Item35: return null;
                case ItemTypeXbx.Item36: return null;
                case ItemTypeXbx.Item37: return null;
                case ItemTypeXbx.Item38: return null;
                case ItemTypeXbx.Item39: return null;
                case ItemTypeXbx.Item40: return null;
                case ItemTypeXbx.Item41: return null;
                case ItemTypeXbx.Item42: return null;
                case ItemTypeXbx.Item43: return null;
                case ItemTypeXbx.Item44: return null;
                case ItemTypeXbx.Item45: return null;
                case ItemTypeXbx.Item46: return null;
                case ItemTypeXbx.Item47: return null;
                case ItemTypeXbx.Item48: return null;
                case ItemTypeXbx.Item49: return null;
                case ItemTypeXbx.Item50: return null;
                case ItemTypeXbx.Item51: return null;
                case ItemTypeXbx.Item52: return null;
                case ItemTypeXbx.Item53: return null;
                case ItemTypeXbx.Item54: return null;
                case ItemTypeXbx.Item55: return null;
                case ItemTypeXbx.Item56: return null;
                case ItemTypeXbx.Item57: return null;
                case ItemTypeXbx.Item58: return null;
                case ItemTypeXbx.Item59: return null;
                case ItemTypeXbx.Item60: return null;
                case ItemTypeXbx.Item61: return null;
                case ItemTypeXbx.Item62: return null;
                case ItemTypeXbx.Item63: return null;
                case ItemTypeXbx.Holofigure: return "ITM_FigList";
                case ItemTypeXbx.Schematic: return "ITM_Blueprint";
                case ItemTypeXbx.Item66: return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static string GetItemTableXb2(int id)
        {
            if (id > 61000) return "ITM_EtherCrystal";
            if (id > 60000) return "ITM_HanaAssist";
            if (id > 59000) return "ITM_HanaNArtsSet";
            if (id > 58000) return "ITM_HanaArtsEnh";
            if (id > 57000) return "ITM_HanaAtr";
            if (id > 56000) return "ITM_HanaRole";
            if (id > 50000) return "ITM_BoosterList";
            if (id > 45000) return "ITM_CrystalList";
            if (id > 40000) return "ITM_FavoriteList";
            if (id > 35000) return "ITM_TresureList";
            if (id > 30000) return "ITM_CollectionList";
            if (id > 27000) return "ITM_EventList";
            if (id > 26000) return "ITM_InfoList";
            if (id > 25000) return "ITM_PreciousList";
            if (id > 20000) return "ITM_SalvageList";
            if (id > 17000) return "ITM_OrbEquip";
            if (id > 14000) return "ITM_Orb";
            if (id > 10000) return "ITM_PcWpnChip";
            if (id > 5000) return "ITM_PcWpn";
            return "ITM_PcEquip";
        }

        public static string GetEventTable(int id)
        {
            if (id > 60000) return "EVT_listDeb01";
            if (id > 40000) return "EVT_listTlk01";
            if (id > 30000) return "EVT_listFev01";
            if (id > 20000) return "EVT_listQst01";
            if (id > 19000) return "EVT_listBl";
            return "EVT_listBf";
        }

        public static string GetEventSetupTable(int id)
        {
            if (id > 60000) return "EVT_setupDeb01";
            if (id > 50000) return "EVT_setupTlk01";
            if (id > 40000) return "EVT_setupFev01";
            if (id > 30000) return "EVT_setupQst01";
            if (id > 29000) return "EVT_setupBf70";
            if (id > 28000) return "EVT_setupBf71";
            if (id > 21000) return "EVT_setupBf11";
            if (id > 20000) return "EVT_setupBf10";
            if (id > 19000) return "EVT_setupBf09";
            if (id > 18000) return "EVT_setupBf08";
            if (id > 17000) return "EVT_setupBf07";
            if (id > 16000) return "EVT_setupBf06";
            if (id > 15000) return "EVT_setupBf05";
            if (id > 14000) return "EVT_setupBf04";
            if (id > 13000) return "EVT_setupBf03";
            if (id > 12000) return "EVT_setupBf02";
            return "EVT_setupBf01";
        }

        public static string GetChangeTable(int id)
        {
            if (id > 60000) return "EVT_listDeb01";
            if (id > 40000) return "EVT_listTlk01";
            if (id > 30000) return "EVT_listFev01";
            if (id > 20000) return "EVT_listQst01";
            if (id > 19000) return "EVT_listBl";
            return "EVT_listBf";
        }

        public static string GetQuestListTable(int id)
        {
            if (id > 7000) return "FLD_QuestListAchievement";
            if (id > 6000) return "FLD_QuestListMercenaries";
            if (id > 5000) return "FLD_QuestListBlade";
            if (id > 2000) return "FLD_QuestListNormal";
            if (id > 1000) return "FLD_QuestListMini";
            return "FLD_QuestList";
        }

        public static string GetQuestListIraTable(int id)
        {
            if (id > 2000) return "FLD_QuestListNormalIra";
            return "FLD_QuestListIra";
        }

        public static string GetLayerTable(int id)
        {
            if (id >= 300) return "MNU_Layer_Dlc03";
            if (id >= 100) return "MNU_Layer_Dlc01";
            return "MNU_Layer";
        }

        public static string GetCharacterTable(int id)
        {
            if (id > 2000) return "RSC_NpcList";
            if (id > 1000) return "CHR_Bl";
            return "CHR_Dr";
        }

        public static string GetConditionTable(ConditionType conditionType)
        {
            switch (conditionType)
            {
                case ConditionType.Scenario:
                    return "FLD_ConditionScenario";
                case ConditionType.Quest:
                    return "FLD_ConditionQuest";
                case ConditionType.Environment:
                    return "FLD_ConditionEnv";
                case ConditionType.Flag:
                    return "FLD_ConditionFlag";
                case ConditionType.Item:
                    return "FLD_ConditionItem";
                case ConditionType.Party:
                    return "FLD_ConditionPT";
                case ConditionType.Idea:
                    return "FLD_ConditionIdea";
                case ConditionType.Level:
                    return "FLD_ConditionLevel";
                case ConditionType.Achievement:
                    return "FLD_ConditionAchievement";
                case ConditionType.FieldSkill:
                    return "FLD_ConditionFieldSkiiLevel";
            }

            return null;
        }

        public static string GetConditionTableXbx(ConditionTypeXbx conditionType)
        {
            switch (conditionType)
            {
                case ConditionTypeXbx.Scenario:
                    return "FLD_ConditionList_Scenario";
                case ConditionTypeXbx.Quest:
                    return "FLD_ConditionList_Quest";
                case ConditionTypeXbx.GameEnv:
                    return "FLD_ConditionList_GameEnv";
                case ConditionTypeXbx.Flag:
                    return "FLD_ConditionList_Flag";
                case ConditionTypeXbx.PartyIn:
                    return "FLD_ConditionList_PTin";
                case ConditionTypeXbx.PartyOut:
                    return "FLD_ConditionList_PTout";
                case ConditionTypeXbx.FriendPoint:
                    return "FLD_ConditionList_FriendPoint";
                case ConditionTypeXbx.GameStat:
                    return "FLD_ConditionList_GameStat";
                case ConditionTypeXbx.Segment:
                    return "FLD_ConditionList_Segment";
                case ConditionTypeXbx.FnStat:
                    return "FLD_ConditionList_FNStat";
                case ConditionTypeXbx.Probe:
                    return "FLD_ConditionList_Probe";
                case ConditionTypeXbx.Type12:
                    break;
                case ConditionTypeXbx.Item:
                    return "FLD_ConditionList_Item";
                case ConditionTypeXbx.Party:
                    return "FLD_ConditionList_Party";
                case ConditionTypeXbx.SquadFlag:
                    return "FLD_ConditionList_SquadFlag";
                case ConditionTypeXbx.Type16:
                    break;
                case ConditionTypeXbx.SquadQuest:
                    return "FLD_ConditionList_SquadQuest";
                case ConditionTypeXbx.SquadMission:
                    return "FLD_ConditionList_SquadMission";
                case ConditionTypeXbx.Type19:
                    break;
                case ConditionTypeXbx.Type20:
                    break;
                case ConditionTypeXbx.Type21:
                    break;
            }

            return null;
        }

        public static string GetTaskTable(TaskType taskType)
        {
            switch (taskType)
            {
                case TaskType.Battle:
                    return "FLD_QuestBattle";
                case TaskType.T2:
                    break;
                case TaskType.Collect:
                    return "FLD_QuestCollect";
                case TaskType.UseItem:
                    return "FLD_QuestUse";
                case TaskType.ReachPlace:
                    return "FLD_QuestReach";
                case TaskType.Talk:
                    return "FLD_QuestTalk";
                case TaskType.T7:
                    break;
                case TaskType.Gimmick:
                    return "FLD_QuestGimmick";
                case TaskType.Mercenary:
                    break;
                case TaskType.QuestCondition:
                    return "FLD_QuestCondition";
                case TaskType.UseFieldSkill:
                    return "FLD_QuestFieldSkillCount";
                case TaskType.StatCount:
                    return "FLD_Achievement";
            }

            return null;
        }

        public static string GetShopTable(ShopType shopType)
        {
            switch (shopType)
            {
                case ShopType.Normal:
                    return "MNU_ShopNormal";
                case ShopType.Exchange:
                    return "MNU_ShopChange";
                case ShopType.Inn:
                    return "MNU_ShopInn";
            }

            return null;
        }

        public static string GetTaskTableXb1(TaskTypeXb1 taskType, int itemId)
        {
            switch (taskType)
            {
                case TaskTypeXb1.KillEnemy:
                    return "BTL_enelist";
                case TaskTypeXb1.GetItem:
                    return "ITM_itemlist";
                case TaskTypeXb1.TalkNpc:
                    return "FLD_npclist";
                case TaskTypeXb1.CompleteQuest:
                    return GetQuestJournalTableXb1(itemId);
                case TaskTypeXb1.InteractObject:
                    break;
            }

            return null;
        }

        public static string GetArmorStyleTableXb1(int characterId)
        {
            return $"MNU_StyleArmorPc{characterId:D2}";
        }

        public static string GetWeaponStyleTableXb1(int characterId)
        {
            return $"MNU_StyleWeaponPc{characterId:D2}";
        }

        public static string PrintEnumFlags(Type enumType, object value)
        {
            var sb = new StringBuilder();
            bool first = true;

            foreach (Enum flag in EnumExtensions.GetIndividualFlags(enumType, value))
            {
                if (!first) sb.Append(", ");
                sb.Append(flag);
                first = false;
            }

            return sb.ToString();
        }

        public static string PrintWeatherIdMap(int weather, int mapId, BdatStringCollection tables)
        {
            if (weather == 0xFF) return "All";

            var sb = new StringBuilder();
            bool first = true;

            var weathers = new Weather[4];
            BdatStringItem map = tables["FLD_maplist"][mapId];
            weathers[0] = (Weather)Enum.Parse(typeof(Weather), map["wa_type"].ValueString);
            weathers[1] = (Weather)Enum.Parse(typeof(Weather), map["wb_type"].ValueString);
            weathers[2] = (Weather)Enum.Parse(typeof(Weather), map["wc_type"].ValueString);
            weathers[3] = (Weather)Enum.Parse(typeof(Weather), map["wd_type"].ValueString);

            for (int i = 0; i < 4; i++)
            {
                if ((weather & (1 << i)) != 0)
                {
                    if (!first) sb.Append(", ");
                    sb.Append(weathers[i]);
                    first = false;
                }
            }

            return sb.ToString();
        }

        public static List<BdatTag> ParseTags(string text)
        {
            int pos = 0;
            var tags = new List<BdatTag>();

            while (true)
            {
                int start = text.IndexOf('[', pos);
                if (start == -1) break;

                if (start > 0 && text[start - 1] == '\\')
                {
                    pos = start + 1;
                    continue;
                }

                int end = text.IndexOf(']', start);
                int length = end - start + 1;
                pos = end + 1;

                string tagText = text.Substring(start + 1, length - 2).Trim(); // Leave out brackets
                string[] values = tagText.Split(' ');

                if (values.Length == 0) throw new InvalidDataException();

                string[] tagHead = values[0].Split(':');
                if (tagHead.Length != 2) throw new InvalidDataException();

                var tag = new BdatTag
                {
                    Type = tagHead[0],
                    SubType = tagHead[1],
                    Start = start,
                    Length = length
                };

                for (int i = 1; i < values.Length; i++)
                {
                    string[] keyVal = values[i].Split('=');
                    if (keyVal.Length != 2) throw new InvalidDataException();
                    tag.Values.Add(keyVal[0], keyVal[1]);
                }

                tags.Add(tag);
            }

            return tags;
        }
    }

    public class BdatTag
    {
        public string Type { get; set; }
        public string SubType { get; set; }
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
        public int Start { get; set; }
        public int Length { get; set; }
    }
}
