﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using GiveMaps.Common.Textures;
using GiveMaps.Types;
using GiveMaps.Xb2.Textures;
using static GiveMaps.Program;

namespace GiveMaps.Gimmick
{
    public static class ExportMap
    {

		public static void MakeMap(Options options, MapInfo[] gimmicks)
		{
			float scale = 1;
			foreach (var map in gimmicks)
			{
				var areaList = map.Areas.OrderBy(x => x.Priority).ToList();
				Point3 min = GetMinPoint(areaList), max = GetMaxPoint(areaList);

				Bitmap baseMap = GetBaseMap(map.Name);

				var collectionTables = new List<FLD_CollectionTable>();

				foreach (var area in areaList)
				{
					var gmkType = area.Gimmicks.FirstOrDefault(x => x.Key == options.Type);
					if (gmkType.Value == null) continue;
					float posx = 2 * (area.LowerBound.X - min.X);
					float posy = 2 * (area.LowerBound.Z - min.Z);

					var color = ColorTranslator.FromWin32(area.Priority * 150000);
					color = System.Drawing.Color.Red;
					var brush = new SolidBrush(color);
					Pen pen = new Pen(new SolidBrush(System.Drawing.Color.Black), 1 * scale);
					var circleSize = 15;
					Font font = new Font("Times New Roman", 10);
					var textBrush = new SolidBrush(System.Drawing.Color.Black);


					foreach (InfoEntry gmk in gmkType.Value)
					{
						var point = area.Get2DPosition(gmk.Xfrm.Position);
						using (Graphics graphics = Graphics.FromImage(baseMap))
						{
							graphics.FillCircle(brush, posx + point.X * scale, posy + point.Y * scale, circleSize * scale);
							graphics.DrawCircle(pen, posx + point.X * scale, posy + point.Y * scale, circleSize * scale);
							if (options.Type == "collection")
							{
								var gmkTable = GetTable(map.Name, options, gmk);
								if (!collectionTables.Contains(gmkTable)) collectionTables.Add(gmkTable);
								graphics.DrawString(gmkTable?.Id.ToString("000") ?? "N/A", font, textBrush,
									new RectangleF((posx + point.X * scale) - font.Size, (posy + point.Y * scale) - font.Size * 3 / 4, font.Size * 3, font.Size * 1.5f));
								//brush = new SolidBrush(CategoryColor(gmkTable));
							}
						}
					}
				}
				DrawTables(baseMap, collectionTables);
				Directory.CreateDirectory($"Maps/");
				File.WriteAllBytes($"Maps/{map.DisplayName} - {options.Type}.png", baseMap.ToPng());
			}
		}

		private static System.Drawing.Color CategoryColor(FLD_CollectionTable gmkTable)
		{
			if (gmkTable == null) return System.Drawing.Color.White;
			switch (gmkTable.categoryName)
			{
				case 132: case 148: return System.Drawing.Color.ForestGreen;
				case 152: case 136: return System.Drawing.Color.Blue;
				case 137: case 153: return System.Drawing.Color.Brown;
				case 135: case 151: return System.Drawing.Color.GreenYellow;
				case 133: case 150: case 149: case 134: return System.Drawing.Color.Red;
				case 138: case 144: return System.Drawing.Color.SlateGray;
				case 139: case 145: return System.Drawing.Color.Purple;
				default: return System.Drawing.Color.White;

			}
		}

		private static void DrawTables(Bitmap baseMap, List<FLD_CollectionTable> collectionTables)
		{
			collectionTables = collectionTables.OrderBy(x => x.Id).ToList();
			using (Graphics graphics = Graphics.FromImage(baseMap))
			{
				int row = 0, x_off = 20, y_off = 20;
				Font tableFont = new Font("Times New Roman", 16);
				SolidBrush tableBrush = new SolidBrush(System.Drawing.Color.White);
				/*
				graphics.DrawString("Forestry", tableFont, new SolidBrush(System.Drawing.Color.ForestGreen), x_off + tableFont.Size * 10, y_off + row++ * tableFont.Size * 1.2f);
				graphics.DrawString("Ichthyology", tableFont, new SolidBrush(System.Drawing.Color.Blue), x_off + tableFont.Size * 10, y_off + row++ * tableFont.Size * 1.2f);
				graphics.DrawString("Mineralogy", tableFont, new SolidBrush(System.Drawing.Color.Brown), x_off + tableFont.Size * 10, y_off + row++ * tableFont.Size * 1.2f);
				graphics.DrawString("Entomology", tableFont, new SolidBrush(System.Drawing.Color.GreenYellow), x_off + tableFont.Size * 10, y_off + row++ * tableFont.Size * 1.2f);
				graphics.DrawString("Botany", tableFont, new SolidBrush(System.Drawing.Color.Red), x_off + tableFont.Size * 10, y_off + row++ * tableFont.Size * 1.2f);
				graphics.DrawString("Mechanical", tableFont, new SolidBrush(System.Drawing.Color.SlateGray), x_off + tableFont.Size * 10, y_off + row++ * tableFont.Size * 1.2f);
				graphics.DrawString("Ether Miasma", tableFont, new SolidBrush(System.Drawing.Color.Purple), x_off + tableFont.Size * 10, y_off + row++ * tableFont.Size * 1.2f);
				graphics.DrawString("N/A", tableFont, new SolidBrush(System.Drawing.Color.White), x_off + tableFont.Size * 10, y_off + row++ * tableFont.Size * 1.2f);*/


				foreach (var table in collectionTables.Where(x => x != null))
				{
					var text1 = $"Table {table.Id.ToString("000")} drops {table.randitmPopMin} to {table.randitmPopMax} items";
					string[] text = {	table.itm1Per != 0 ? $"{table._itm1ID?._Name.name ?? ""} - {table.itm1Per}%" : "",
										table.itm2Per != 0 ? $"{table._itm2ID?._Name.name ?? ""} - {table.itm2Per}%" : "",
										table.itm3Per != 0 ? $"{table._itm3ID?._Name.name ?? ""} - {table.itm3Per}%" : "",
										table.itm4Per != 0 ? $"{table._itm4ID?._Name.name ?? ""} - {table.itm4Per}%" : "" };
					graphics.DrawString(text1,   tableFont, tableBrush, x_off + tableFont.Size * 10, y_off + row++ * tableFont.Size * 1.2f);
					graphics.DrawString(text[0], tableFont, tableBrush, x_off, y_off + row * tableFont.Size * 1.2f);
					graphics.DrawString(text[1], tableFont, tableBrush, x_off + tableFont.Size * 20, y_off + row++ * tableFont.Size * 1.2f);
					graphics.DrawString(text[2], tableFont, tableBrush, x_off, y_off + row * tableFont.Size * 1.2f);
					graphics.DrawString(text[3], tableFont, tableBrush, x_off + tableFont.Size * 20, y_off + row++ * tableFont.Size * 1.2f);
					row++;
				}
			}
		}

		private static Bitmap GetBaseMap(string areaName)
		{
			switch (areaName)
			{
				case "ma40a":
					return new Bitmap("Data/tornabase.png");
				case "ma41a":
					return new Bitmap("Data/gormottbase.png");
			}

			return null;
		}

		private static Bitmap GenerateBaseMap(List<MapAreaInfo> areaList, Point3 min, Point3 max, Options options, float scale)
		{
			var baseMap = new Bitmap((int)(scale * (max.X - min.X) * 2), (int)(scale * (max.Z - min.Z) * 2));
			foreach (var area in areaList)
			{
				var wilay = new WilayRead(File.ReadAllBytes($"{options.DataDir}/menu/image/{area.Name}_map.wilay"));
				var areaMap = wilay.Textures[0].ToBitmap();
				areaMap.RotateFlip(RotateFlipType.Rotate180FlipNone);
				areaMap = ResizeImage(areaMap, (int)(areaMap.Width * scale), (int)(areaMap.Height * scale));
				using (Graphics graphics = Graphics.FromImage(baseMap))
					graphics.DrawImage(areaMap, 2 * (area.LowerBound.X - min.X), 2 * (area.LowerBound.Z - min.Z));
			}

			return baseMap;
		}

		private static Point3 GetMinPoint(List<MapAreaInfo> areaList)
		{
			float x = areaList.Select(a => a.LowerBound.X).Min();
			float y = areaList.Select(a => a.LowerBound.Y).Min();
			float z = areaList.Select(a => a.LowerBound.Z).Min();
			return new Point3(x, y, z);
		}

		private static Point3 GetMaxPoint(List<MapAreaInfo> areaList)
		{
			float x = areaList.Select(a => a.UpperBound.X).Max();
			float y = areaList.Select(a => a.UpperBound.Y).Max();
			float z = areaList.Select(a => a.UpperBound.Z).Max();
			return new Point3(x, y, z);
		}

		private static FLD_CollectionTable GetTable(string name, Options options, InfoEntry gmk)
		{
			switch (name)
			{
				case "ma40a":
					return options.Tables.ma40a_FLD_CollectionPopList.FirstOrDefault(x => x.name == gmk.Name)?._CollectionTable;
				case "ma41a":
					return options.Tables.ma41a_FLD_CollectionPopList.FirstOrDefault(x => x.name == gmk.Name)?._CollectionTable;
			}
			return null;
		}

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static void FillCircle(this Graphics g, Brush brush,
            float centerX, float centerY, float radius)
        {
			g.FillEllipse(brush, centerX - radius, centerY - radius,
				radius + radius, radius + radius);
        }

        public static void DrawRect(this Graphics g, Pen pen,
            float centerX, float centerY, float scaleX, float scaleY)
        {
            g.DrawRectangle(pen, centerX - scaleX / 2, centerY - scaleY / 2, scaleX, scaleY);
        }

        public static void DrawEllipse(this Graphics g, Pen pen,
            float centerX, float centerY, float scaleX, float scaleY)
        {
            g.DrawEllipse(pen, centerX - scaleX / 2, centerY - scaleY / 2, scaleX, scaleY);
        }

        public static void FillEllipse(this Graphics g, Brush brush,
            float centerX, float centerY, float scaleX, float scaleY)
        {
            g.FillEllipse(brush, centerX - scaleX / 2, centerY - scaleY / 2, scaleX, scaleY);
        }

        public static void FillRect(this Graphics g, Brush brush,
            float centerX, float centerY, float scaleX, float scaleY)
        {
            g.FillRectangle(brush, centerX - scaleX / 2, centerY - scaleY / 2, scaleX, scaleY);
        }

        public static void DrawCircle(this Graphics g, Pen pen,
            float centerX, float centerY, float radius)
        {
			g.DrawEllipse(pen, centerX - radius, centerY - radius,
				radius + radius, radius + radius);
        }
    }
}
