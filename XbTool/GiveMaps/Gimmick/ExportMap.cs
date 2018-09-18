using System;
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
        public static void Export(Options options, MapInfo[] gimmicks)
        {

            foreach (var map in gimmicks)
            {
                //if (map.Name != "ma40a") continue;
                foreach (var area in map.Areas)
                {
					//if (area.Name != "ma02a_f01") continue;
					//The files and info for the caves is super glitchy and weird, just changing em so no crashes or funny business
					if (area.Name == "ma40a_f01_cave1")
					{
						area.Name = "dlc3_ma40a_f01";
						area.SegmentInfo = map.Areas[0].SegmentInfo;
					}

					if (area.Name == "ma40a_f02_cave1")
					{
						area.Name = "dlc3_ma40a_f02";
						area.SegmentInfo = map.Areas[1].SegmentInfo;
					}
					if (area.Name == "ma40a_f03_cave1")
					{
						area.Name = "dlc3_ma40a_f03";
						area.SegmentInfo = map.Areas[2].SegmentInfo;
					}


					string texPath = $"{options.DataDir}/menu/image/{area.Name}_map.wilay";
					var texBytes = File.ReadAllBytes(texPath);
                    var wilay = new WilayRead(texBytes);
                    LahdTexture texture = wilay.Textures[0];
                    var bitmapBase = texture.ToBitmap();
                    float scale = 1;
                    bitmapBase = ResizeImage(bitmapBase, (int)(bitmapBase.Width * scale), (int)(bitmapBase.Height * scale));

                    var outerBrush = new SolidBrush(System.Drawing.Color.Black);
                    //var backing = new SolidBrush(System.Drawing.Color.White);
                    var innerBrush = new SolidBrush(System.Drawing.Color.LightBlue);
                    Pen pen = new Pen(outerBrush, 1 * scale);
					var circleSize = 15;
					Font font = new Font("Times New Roman", 10);
					var textBrush = new SolidBrush(System.Drawing.Color.Black);


					bitmapBase.RotateFlip(RotateFlipType.Rotate180FlipNone);

                    foreach (var gmkType in area.Gimmicks)
                    {
                        var type = gmkType.Key;
                        //if (type != "precious") continue;
                        var bitmap = (Bitmap)bitmapBase.Clone();
						int count = 0;
						var collTables = new List<FLD_CollectionTable>(); 
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            foreach (InfoEntry gmk in gmkType.Value)
                            {
								count++;
								var point = area.Get2DPosition(gmk.Xfrm.Position);

								var posx = point.X * scale;
								var posy = point.Y * scale;

                                graphics.FillCircle(innerBrush, posx, posy, circleSize * scale);
                                graphics.DrawCircle(pen, point.X * scale, point.Y * scale, circleSize * scale);
								if (type == "collection")
								{
									var gmkTable = getTable(map.Name, options, gmk);
									if (!collTables.Contains(gmkTable)) collTables.Add(gmkTable);
									var test1 = font.Size;
									var test2 = font.SizeInPoints;
									graphics.DrawString(gmkTable.Id.ToString("000"), font, textBrush, new RectangleF(posx - font.Size, posy - font.Size * 3 / 4, font.Size * 3, font.Size * 1.5f));
									//graphics.DrawRectangle(pen, posx - font.Size * 1.5f, posy - font.Size * 3 /4, font.Size * 3, font.Size * 1.5f);
								}
                            }
							//foreach (InfoEntry gmk in gmkType.Value)
							//{
							//    //if (gmk.String != "landmark_ma02a_101") continue;
							//    var point = area.Get2DPosition(gmk.Xfrm.Position);
							//    var pointS = area.Get2DPosition(gmk.Xfrm.Scale);
							//    //graphics.FillCircle(innerBrush, point.X * scale, point.Y * scale, 5 * scale);
							//    //DrawEllipse(graphics, pen, point.X * scale, point.Y * scale, pointS.X / 2 * scale, pointS.Y / 2 * scale);
							//    DrawRect(graphics, pen, point.X * scale, point.Y * scale, pointS.X / 2 * scale, pointS.Y / 2 * scale);
							//    //graphics.DrawCircle(pen, point.X * scale, point.Y * scale, 5 * scale);
							//}

							//foreach (InfoEntry gmk in gmkType.Value)
							//{
							//    //var text = gmk.String.Split('_').Last();
							//    var text = gmk.Type.ToString();
							//    var point = area.Get2DPosition(gmk.Xfrm.Position);

							//    //graphics.FillRectangle(backing, point.X * scale, point.Y * scale, 30 * scale, 12 * scale);
							//    graphics.FillRectangle(backing, point.X * scale, point.Y * scale, 12 * scale, 12 * scale);
							//    graphics.DrawString(text, new Font("Arial", 8 * scale), outerBrush, point.X * scale, point.Y * scale);
							//}

							int row = 0, x_off = 20, y_off = 20;
							Font tableFont = new Font("Times New Roman", 16);
							SolidBrush tableBrush = new SolidBrush(System.Drawing.Color.White);
							graphics.DrawString($"{options.Name}", tableFont, tableBrush, x_off, y_off + row++ * tableFont.Size * 1.2f);

							foreach (var collTable in collTables)
							{
								graphics.DrawString($"{collTable.Id.ToString("000")}\t{getItemRarity(collTable,options.Name)}%\t{collTable.randitmPopMin}-{collTable.randitmPopMax} items", 
									tableFont, tableBrush, x_off, y_off + row++ * tableFont.Size * 1.2f);
							}
						}

						if (count == 0) continue;
                        var png = bitmap.ToPng();
						Directory.CreateDirectory(Path.Combine(options.Output, $"{options.Name}/"));
						File.WriteAllBytes(Path.Combine(options.Output, 
							$"{options.Name}/{map.DisplayName} - {(area.DisplayName.Substring(0,5) == "ma40a" ? area.DisplayName : area.Name)}.png"), png);
                    }
                }
            }
        }

		private static string getItemRarity(FLD_CollectionTable collTable, string name)
		{
			if (collTable._itm1ID._Name.name == name) return collTable.itm1Per.ToString();
			if (collTable._itm2ID._Name.name == name) return collTable.itm2Per.ToString();
			if (collTable._itm3ID._Name.name == name) return collTable.itm3Per.ToString();
			if (collTable._itm4ID._Name.name == name) return collTable.itm4Per.ToString();
			return "N/A";
		}

		private static FLD_CollectionTable getTable(string name, Options options, InfoEntry gmk)
		{
			switch (name)
			{
				case "ma40a":
					return options.Tables.ma40a_FLD_CollectionPopList.Where(x => x.name == gmk.Name).First()._CollectionTable;
				case "ma41a":
					return options.Tables.ma41a_FLD_CollectionPopList.Where(x => x.name == gmk.Name).First()._CollectionTable;
			}
			return null;
		}

		public static void ExportCsv(MapInfo[] gimmicks, string outDir)
        {
            Directory.CreateDirectory(Path.Combine(outDir, "mi"));
            Directory.CreateDirectory(Path.Combine(outDir, "gmk"));
            foreach (MapInfo map in gimmicks)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Name,DisplayName,Priority,Width,Height,LowerX,LowerY,LowerZ,UpperX,UpperY,UpperZ");

                foreach (var area in map.Areas)
                {
                    sb.AppendLine(
                        $"{area.Name},\"{area.DisplayName}\",{area.Priority}," +
                        $"{area.SegmentInfo.FullWidth},{area.SegmentInfo.FullHeight}," +
                        $"{area.LowerBound.X},{area.LowerBound.Y},{area.LowerBound.Z}," +
                        $"{area.UpperBound.X},{area.UpperBound.Y},{area.UpperBound.Z}");
                }
                File.WriteAllText(Path.Combine(outDir, $"mi/{map.Name}.csv"), sb.ToString());
            }

            var sbAll = new StringBuilder();
            string header = "GmkType,Id,Id in file,Name,XformType,PosX,PosY,PosZ,fc,RotX,RotY,RotZ,f1c,ScaleX,ScaleY,ScaleZ,f2c,f30,f32,f34,f38,f3c";
            sbAll.Append("Map,Filename,");
            sbAll.AppendLine(header);

            foreach (var map in gimmicks)
            {
                foreach (var gmkTypeKv in map.Gimmicks)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(header);
                    var type = gmkTypeKv.Key;

                    foreach (var gmk in gmkTypeKv.Value.Info)
                    {
                        var pos = gmk.Xfrm.Position;
                        var xfrm = gmk.Xfrm;
                        string csvLine = $"{gmk.GmkType},{gmk.Id},{gmk.IdInFile},{gmk.Name},{gmk.Type},{pos.X},{pos.Y},{pos.Z},{xfrm.FieldC}," +
                                         $"{xfrm.Rotation.X},{xfrm.Rotation.Y},{xfrm.Rotation.Z},{xfrm.Field1C}," +
                                         $"{xfrm.Scale.X},{xfrm.Scale.Y},{xfrm.Scale.Z},{xfrm.Field2C}," +
                                         $"{xfrm.Field30},{xfrm.Field32},{xfrm.Field34},{xfrm.Field38},{xfrm.Field3C}";
                        sb.AppendLine(csvLine);
                        sbAll.Append($"{map.Name},{type},");
                        sbAll.AppendLine(csvLine);
                    }

                    File.WriteAllText(Path.Combine(outDir, $"gmk/{map.Name}-{type}.csv"), sb.ToString());
                }
            }

            File.WriteAllText(Path.Combine(outDir, "gmk/all.csv"), sbAll.ToString());
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
