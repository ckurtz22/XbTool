using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XbTool.BdatString;
using XbTool.Common.Textures;
using XbTool.Types;
using XbTool.Xb2.Textures;

namespace XbTool.Xbde
{
    public static class Maps
    {
        public static void ReadMap(BdatStringCollection bdats, Options options)
        {
            var bdatMiniMapLists = bdats.Tables.Where(x => x.Key.Contains("minimaplist")).Select(x => x.Value).ToList();
            var bdatMaps = bdats.Tables["FLD_maplist"];

            foreach (var map in bdatMaps.Items)
            {
                string index = map.Values["resource"].Value.ToString().Substring(2);
                if (index != "2501" && index != "2601") continue;
                var minimap = bdatMiniMapLists.FirstOrDefault(x => x.Name.Contains(index));
                if (minimap == null) continue;

                int.TryParse(map["minimap_lt_x"].Value.ToString(), out int minX);
                int.TryParse(map["minimap_lt_z"].Value.ToString(), out int minZ);
                int.TryParse(map["minimap_rb_x"].Value.ToString(), out int maxX);
                int.TryParse(map["minimap_rb_z"].Value.ToString(), out int maxZ);

                Rectangle mapBounds = new Rectangle(minX, minZ, maxX - minX, maxZ - minZ);

                // var landmarks = bdatLandmarks.Items.Where(x => x["mapID"].Reference == map).ToList();
                if (!bdats.Tables.ContainsKey("Litemlist" + index)) continue;
                var collectables = bdats.Tables["Litemlist" + index]?.Items?.Where(x => CheckDrop(x, options.Filter));
                //var fb = bdats.Tables["poplist" + index]?.Items?.Where(x => x["NAMED_FLG"].DisplayString != "0" && x["ene1Per"].DisplayString == "100");
                //var um = bdats.Tables["poplist" + index]?.Items?.Where(x => x["NAMED_FLG"].DisplayString != "0" && x["ene1Per"].DisplayString == "30");
                var etherMine = bdats.Tables["gemMineList2501"].Items.Where(x => x.Id == 102 || x.Id == 120 || x.Id == 122 || x.Id == 126 || x.Id == 127 || x.Id == 128 || x.Id == 124 || x.Id == 103);
                var um = bdats.Tables["poplist" + index]?.Items?.Where(x => x["NAMED_FLG"].DisplayString != "0");
                var landmarks = bdats.Tables["landmarklist"].Items.Where(x => x["mapID"].Reference == map && (x["category"].DisplayString == "0" || x["category"].DisplayString == "1")).ToList();
                // if (!collectables.Any()) continue;

                int ground = int.MinValue;
                foreach (var floor in minimap.Items)
                {
                    int.TryParse(floor["height"].Value.ToString(), out int ceiling);

                    var map_file_name = floor["mapimg"].Reference["filename"].Value.ToString();
                    if (floor["mapimg1"].Reference != null)
                        map_file_name = floor["mapimg1"].Reference["filename"].Value.ToString();
                    if (floor["mapimg2"].Reference != null)
                        map_file_name = floor["mapimg2"].Reference["filename"].Value.ToString();

                    var map_file = Path.Combine(Path.Combine(options.Input, "image"), map_file_name + ".wilay");
                    byte[] file = File.ReadAllBytes(map_file);
                    var wilay = new WilayRead(file);
                    LahdTexture texture = wilay.Textures[0];
                    Bitmap bitmap = texture.ToBitmap();

                    var brush1 = new SolidBrush(System.Drawing.Color.Red);
                    var brush2 = new SolidBrush(System.Drawing.Color.Yellow);
                    var brush3 = new SolidBrush(System.Drawing.Color.Blue);
                    var outerBrush = new SolidBrush(System.Drawing.Color.Black);
                    Pen pen = new Pen(outerBrush, 2);

                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        // Landmarks
                        //graphics.MarkItems(landmarks, innerBrush, pen, mapBounds, bitmap.Size, ground, ceiling);

                        // Collectables
                        // graphics.MarkItems(collectables.ToList(), brush1, pen, mapBounds, bitmap.Size, ground, ceiling);

                        // Enemies
                        // graphics.MarkItems(fb.ToList(), brush2, pen, mapBounds, bitmap.Size, ground, ceiling);
                        // graphics.MarkItems(um.ToList(), brush1, pen, mapBounds, bitmap.Size, ground, ceiling);
                        // graphics.MarkItems(landmarks.ToList(), brush3, pen, mapBounds, bitmap.Size, ground, ceiling);


                        graphics.MarkItems(etherMine.ToList(), brush1, pen, mapBounds, bitmap.Size, ground, ceiling);



                    }
                    byte[] png = bitmap.ToPng();
                    File.WriteAllBytes(Path.Combine(options.Output, $"png/{map["name"].DisplayString} - f{floor["floorname"].Value.ToString()}.png"), png);

                    Console.WriteLine("");
                    ground = ceiling;
                }
            }
        }

        private static bool CheckDrop(BdatStringItem x, string filter)
        {
            for (int i = 1; i <= 8; i++)
            {
                int.TryParse(x[$"itm{i}Per"].DisplayString, out int percent);
                if (x[$"itm{i}ID"].DisplayString == filter && percent == 15)
                    return true;
            }
            return false;
        }

        public static void MarkItems(this Graphics g, List<BdatStringItem> items, Brush brush, Pen pen, Rectangle bounds, Size size, int ground, int ceiling)
        {
            foreach (var item in items)
            {
                int.TryParse(item["posX"].Value.ToString(), out int x_coord);
                int.TryParse(item["posY"].Value.ToString(), out int y);
                int.TryParse(item["posZ"].Value.ToString(), out int z_coord);
                // if (y < ground || y > ceiling) continue;

                float x = (float)(x_coord / 10000 - bounds.Left) / bounds.Width * size.Width;
                float z = (float)(z_coord / 10000 - bounds.Top) / bounds.Height * size.Height;

                g.FillCircle(brush, x, z, 16);
                g.DrawCircle(pen, x, z, 16);
            }

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
