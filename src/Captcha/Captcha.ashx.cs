using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace Captcha
{
    /// <summary>
    /// Summary description for Captcha
    /// </summary>
    public class Captcha : IHttpHandler
    {
        private int width = 100;
        private int height = 40;
        private string captcha = string.Empty;
        private Brush[] BrushList = new Brush[] { Brushes.White };
        private Brush[] BorderBrushList = new Brush[] { Brushes.Gainsboro };

        private Graphics GetGraphics(Bitmap bmp)
        {
            Graphics g = Graphics.FromImage(bmp);

            g.Clear(Color.Gainsboro);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            return g;
        }


        public void ProcessRequest(HttpContext context)
        {
            context.Response.Clear();

            // image width
            this.width = !string.IsNullOrEmpty(context.Request.QueryString["w"]) ? Convert.ToInt32(context.Request.QueryString["w"].Trim()) : 300;

            // image height
            this.height = !string.IsNullOrEmpty(context.Request.QueryString["h"]) ? Convert.ToInt32(context.Request.QueryString["h"].Trim()) : 100;

            // captcha text
            this.captcha = context.Session != null && context.Session["captcha"] != null ? 
                context.Session["captcha"].ToString().Trim() : "012345"; // up to five charactors.

            float emSize = Math.Max(height / 4, 14);
            Font f = new Font("Helvetica", emSize, FontStyle.Bold | FontStyle.Strikeout); // font style
            Random rand = new Random(); // rand variable for global

            using (Bitmap bmp = new Bitmap(width, height))
            {
                using (Graphics g = GetGraphics(bmp))
                {
                    //Brush b = BrushList[rand.Next() % BrushList.Length];
                    SizeF fontSize = g.MeasureString(captcha, f);
                    float midX = (width - fontSize.Width) / 2;
                    float midY = (height - fontSize.Height) / 2;

                    RectangleF rectf = new RectangleF(
                        0,//(width - fontSize.Width) / 2, // position x
                        0, // position y
                        0,
                        0);

                    for (int i = 0; i < captcha.Length; ++i)
                    {
                        Brush b = BrushList[rand.Next() % BrushList.Length];
                        g.DrawString(captcha[i].ToString(), f, b, rectf);

                        float w = g.MeasureString(captcha[i].ToString(), f).Width;
                        rectf.X += w + rand.Next(5, 20);
                    }

                    // make image distortion
                    // from: http://stackoverflow.com/questions/225548/resources-for-image-distortion-algorithms
                    using (Bitmap db = new Bitmap(width, height))
                    {
                        using (Graphics dg = GetGraphics(db))
                        {
                            for (double x = 0; x < db.Width; ++x)
                            {
                                for (double y = 0; y < db.Height; ++y)
                                {
                                    // image distoration effect
                                    double rotation = 1;
                                    double effect = Math.Max(width, height) / 3;

                                    double angle = rotation * Math.Exp(-(x * x + y * y) / Math.Pow(effect, 2));
                                    double u = Math.Cos(angle) * x + Math.Sin(angle) * y + midX / 2;
                                    double v = -Math.Sin(angle) * x + Math.Cos(angle) * y + midY * (effect * 0.015);

                                    if (u >= 0 && u < db.Width && v >= 0 && v < db.Height)
                                    {
                                        db.SetPixel((int)u, (int)v, bmp.GetPixel((int)x, (int)y));
                                    }
                                }
                            }

                            Pen pen = new Pen(BorderBrushList[rand.Next() % BorderBrushList.Length], 1);

                            dg.DrawRectangle(pen, 1, 1, width - 2, height - 2);
                            dg.Flush();

                            context.Response.ContentType = "image/jpeg";
                            db.Save(context.Response.OutputStream, ImageFormat.Jpeg);
                        }
                    }
                }
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}