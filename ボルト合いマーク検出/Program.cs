using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;


namespace ボルト合いマーク検出
{

    class Program
    {
        class TImage
        {
            public Mat input_image { get; private set; }
            public Mat output_image { get; private set; }
            public Scalar scalar_min { get; private set; }
            public Scalar scalar_max { get; private set; }
            private int erote_size { get; set; }
            private int dilate_size { get; set; }
            public Scalar judge_color { get; private set; }
            public int line_num { get; private set; }
            public int line_size { get; private set; }
            public double aspect_ratio_min { get; private set; }
            public double aspect_ratio_max { get; private set; }
            String result = "";
            public TImage(string image_path)
            {
                input_image = new Mat(image_path);
                SetDefault();
            }
            public TImage()
            {
                input_image = new Mat();
                SetDefault();
            }
            private void SetDefault()
            {
                output_image = new Mat();
                scalar_min = new Scalar(-180, 0, 0);
                scalar_max = new Scalar(179, 255, 255);
                judge_color = Scalar.Red;
                erote_size = 4;
                dilate_size = 9;
                line_num = 1;
                line_size = 1000;
                aspect_ratio_min = 0.1;
                aspect_ratio_max = 0.3;
                result = "";
            }
            public void SetImage(string image_path)
            {
                input_image = new Mat(image_path);
            }

            public void SetScalar(Scalar s_min, Scalar s_max)
            {
                scalar_min = s_min;
                scalar_max = s_max;
            }
            public void SetNoise(int e_size, int d_size)
            {
                erote_size = e_size;
                dilate_size = d_size;
            }
            public void SetLine(int l_num,int l_size,double ar_min,double ar_max)
            {
                line_num = l_num;
                line_size = l_size;
                aspect_ratio_min = ar_min;
                aspect_ratio_max = ar_max;
            }
            public void SetProperty(Scalar s_min, Scalar s_max, int e_size, int d_size, int l_num, int l_size, double ar_min, double ar_max)
            {
                SetScalar(s_min, s_max);
                SetNoise(e_size, d_size);
                SetLine(l_num, l_size, ar_min, ar_max);
            }
            public void ImageJudgment(bool debug_mode)
            {
                /*==================================================
                内部変数の宣言
                ==================================================*/
                //画像
                Mat i_image = input_image;
                Mat hsv_image = new Mat();
                Mat mask_image = new Mat();
                Mat erodeMat = new Mat();
                Mat dilateMat = new Mat();
                Mat o_image = new Mat();

                //色検出
                Scalar s_min = scalar_min;
                Scalar s_max = scalar_max;

                //合いマーク検出色
                Scalar j_color = judge_color;

                //判定結果
                String result_value = "NG";

                //収縮
                Mat erode_noise = new Mat(new OpenCvSharp.Size(erote_size, erote_size), MatType.CV_8UC1);
                //膨張
                Mat dilate_noise = new Mat(new OpenCvSharp.Size(dilate_size, dilate_size), MatType.CV_8UC1);

                //面積・縦横比
                int l_size = line_size;
                double ar_min = aspect_ratio_min;
                double ar_max = aspect_ratio_max;

                //画像の読み込み
                Cv2.ImShow("image", i_image);
                if (debug_mode) Cv2.WaitKey(0); else Cv2.WaitKey(1);
                //色空間の変更
                Cv2.CvtColor(i_image, hsv_image, ColorConversionCodes.BGR2HSV);
                Cv2.ImShow("image", hsv_image);
                if (debug_mode) Cv2.WaitKey(0); else Cv2.WaitKey(1);
                //色の検出
                Cv2.InRange(hsv_image, s_min, s_max, mask_image);
                Cv2.ImShow("image", mask_image);
                if (debug_mode) Cv2.WaitKey(0); else Cv2.WaitKey(1);

                //収縮処理
                Cv2.Erode(mask_image, erodeMat, erode_noise);
                Cv2.ImShow("image", erodeMat);
                if (debug_mode) Cv2.WaitKey(0); else Cv2.WaitKey(1);

                //膨張処理
                Cv2.Dilate(erodeMat, dilateMat, dilate_noise);
                Cv2.ImShow("image", dilateMat);
                if (debug_mode) Cv2.WaitKey(0); else Cv2.WaitKey(1);

                //面積算出
                //ジャグ配列
                OpenCvSharp.Point[][] contours;
                OpenCvSharp.HierarchyIndex[] hierarchyIndexes;

                dilateMat.FindContours(out contours, out hierarchyIndexes, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                o_image = i_image;
                int cnt = 0;
                int area_cnt = 0;
                foreach (var contour in contours)
                {
                    //面積を算出
                    var area = Cv2.ContourArea(contour);
                    var rect = Cv2.MinAreaRect(contour);
                    float long_side = 0;
                    float short_side = 0;
                    if(rect.Size.Height > rect.Size.Width)
                    {
                        long_side = rect.Size.Height;
                        short_side = rect.Size.Width;
                    } else
                    {
                        long_side = rect.Size.Width;
                        short_side = rect.Size.Height;
                    }
                    double aspect_ratio = short_side / long_side;
                    Point2f[] vertices = rect.Points();
                    Cv2.Line(dilateMat, vertices[0].ToPoint(), vertices[1].ToPoint(), Scalar.Blue, 2);
                    Cv2.Line(dilateMat, vertices[1].ToPoint(), vertices[2].ToPoint(), Scalar.Blue, 2);
                    Cv2.Line(dilateMat, vertices[2].ToPoint(), vertices[3].ToPoint(), Scalar.Blue, 2);
                    Cv2.Line(dilateMat, vertices[3].ToPoint(), vertices[0].ToPoint(), Scalar.Blue, 2);
                    bool judge = false;
                    if (area > l_size && (aspect_ratio < ar_max && aspect_ratio > ar_min)) judge = true;
                    if (judge)
                    {
                        result_value = "OK";
                        Cv2.Line(o_image, vertices[0].ToPoint(), vertices[1].ToPoint(), judge_color, 2);
                        Cv2.Line(o_image, vertices[1].ToPoint(), vertices[2].ToPoint(), judge_color, 2);
                        Cv2.Line(o_image, vertices[2].ToPoint(), vertices[3].ToPoint(), judge_color, 2);
                        Cv2.Line(o_image, vertices[3].ToPoint(), vertices[0].ToPoint(), judge_color, 2);
                        cnt++;
                    }
                    area_cnt++;
                    if (debug_mode) Console.WriteLine(area_cnt.ToString() + "番目");
                    if (debug_mode) if(judge) Console.WriteLine("判定 : OK");else Console.WriteLine("判定 : NG");
                    if (debug_mode) Console.WriteLine("縦横比 : " + aspect_ratio.ToString());
                    if (debug_mode) Console.WriteLine("面積　 : " + area.ToString());
                }
                if (cnt > line_num) result_value = "OBS";
                Cv2.ImShow("image", dilateMat);
                if (debug_mode) Cv2.WaitKey(0); else Cv2.WaitKey(1);

                //結果書込み
                Bitmap img = BitmapConverter.ToBitmap(o_image);
                var g = System.Drawing.Graphics.FromImage(img);
                Pen haikeiPen = new Pen(Brushes.SpringGreen);
                Brush haikeiBrush = new SolidBrush(Color.SpringGreen);
                var result_color = System.Drawing.Brushes.Blue;
                if (result_value == "OK")
                {
                    result_color = System.Drawing.Brushes.Red;
                }
                haikeiPen.Width = 8.0F;
                haikeiPen.Brush = System.Drawing.Brushes.SpringGreen;
                haikeiPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Bevel;
                int label_width = (int)(result_value.Length * 35);
                System.Drawing.Point[] curvePoints = { new System.Drawing.Point(10, 10), new System.Drawing.Point(10, 45), new System.Drawing.Point(label_width, 45), new System.Drawing.Point(label_width, 10), };
                g.FillPolygon(haikeiBrush, curvePoints);
                g.DrawString(result_value, new Font("Arial", 24), result_color, new System.Drawing.PointF(10.0f, 10.0f));
                o_image = BitmapConverter.ToMat(img);
                //結果出力
                Cv2.ImShow("image", o_image);
                Cv2.WaitKey(0);
                output_image = o_image;
                result = result_value;
            }
        }

        class CsvReader
        {
            public class CsvProperty
            {
                public string path { get; set; }
                public int hue_min { get; set; }
                public int saturation_min { get; set; }
                public int value_min { get; set; }
                public int hue_max { get; set; }
                public int saturation_max { get; set; }
                public int value_max { get; set; }
                public int erote_size { get; set; }
                public int dilate_size { get; set; }
                public int line_num { get; set; }
                public int line_size { get; set; }
                public double aspect_ratio_min { get; set; }
                public double aspect_ratio_max { get; set; }
            }

            public List<CsvProperty> pt { get; private set; }
            public CsvReader(string csv_path)
            {
                StreamReader sr = new StreamReader(@csv_path);
                int line_count = 0;
                bool check_result = false;
                pt = new List<CsvProperty>();
                while (!sr.EndOfStream)
                {
                    line_count++;
                    // CSVファイルの一行を読み込む
                    string line = sr.ReadLine();
                    // 読み込んだ一行をカンマ毎に分けて配列に格納する
                    string[] values = line.Split(',');
                    if(line_count == 1)
                    {
                        //ヘッダー項目数チェック
                        if (values.Length == 13)
                        {
                            //ヘッダー行チェック
                            if (values[0] == "Path" && 
                                values[1] == "Hue_min" && 
                                values[2] == "Saturation_min" &&
                                values[3] == "Value_min" &&
                                values[4] == "Hue_max" &&
                                values[5] == "Saturation_max" &&
                                values[6] == "Value_max" &&
                                values[7] == "Erote_size" &&
                                values[8] == "Dilate_size" &&
                                values[9] == "Line_num" &&
                                values[10] == "Line_size" &&
                                values[11] == "Aspect_ratio_min" &&
                                values[12] == "Aspect_ratio_max")
                            {
                                check_result = true;
                            }
                        }
                    }
                    else if(check_result)
                    {
                        CsvProperty pt_row = new CsvProperty();
                        pt_row.path = @values[0];
                        pt_row.hue_min = Int32.Parse(values[1]);
                        pt_row.saturation_min = Int32.Parse(values[2]);
                        pt_row.value_min = Int32.Parse(values[3]);
                        pt_row.hue_max = Int32.Parse(values[4]);
                        pt_row.saturation_max = Int32.Parse(values[5]);
                        pt_row.value_max = Int32.Parse(values[6]);
                        pt_row.erote_size = Int32.Parse(values[7]);
                        pt_row.dilate_size = Int32.Parse(values[8]);
                        pt_row.line_num = Int32.Parse(values[9]);
                        pt_row.line_size = Int32.Parse(values[10]);
                        pt_row.aspect_ratio_min = Double.Parse(values[11]);
                        pt_row.aspect_ratio_max = Double.Parse(values[12]);
                        pt.Add(pt_row);
                    }
                }
            }

        }
        static void Main(string[] args)
        {
            string csv_path = @"input.csv";
            bool debug_mode = false;
            string[] cmd_args = System.Environment.GetCommandLineArgs();
            if(cmd_args.Length >= 2)
            {
                csv_path = cmd_args[1];
            }
            if (cmd_args.Length >= 3)
            {
                if(cmd_args[2] == "1") debug_mode = true;
            }
            CsvReader cr = new CsvReader(csv_path);
            foreach (CsvReader.CsvProperty pt_row in cr.pt)
            {
                TImage ti = new TImage(pt_row.path);
                ti.SetProperty(new Scalar(pt_row.hue_min, pt_row.saturation_min, pt_row.value_min),
                               new Scalar(pt_row.hue_max, pt_row.saturation_max, pt_row.value_max),
                               pt_row.erote_size, pt_row.dilate_size, pt_row.line_num,pt_row.line_size,pt_row.aspect_ratio_min,pt_row.aspect_ratio_max);
                ti.ImageJudgment(debug_mode);
            }
        }
    }
}
