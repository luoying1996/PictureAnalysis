using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Net;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Text.RegularExpressions;

namespace PictureAnalysis
{
  
    public class baiduAIHelper
    {
        public string APP_ID = "16550139";
        public string API_KEY = "uVciedbCwBE2KvMoGUxMiGCO";
        public string SECRET_KEY = "e34b8ENUQ53el2G829FCHZRpq2G96jqC";
        public Baidu.Aip.Ocr.Ocr client;
        
        public baiduAIHelper()
        {
            //ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            
        }
        public string GeneralBasicDemo(string path)
        {
            byte[] arr;
            client = new Baidu.Aip.Ocr.Ocr(API_KEY, SECRET_KEY);
            client.Timeout = 60000;
            using (Bitmap bmp = new Bitmap(path))
            {
                FileInfo fileInfo = new FileInfo(path);
                double length = Convert.ToDouble(fileInfo.Length);
                double Size = length / 1024 / 1024;
                if (Size > 1.8)
                {
                    int width = 1500;
                    int height = 0;
                    if (width == 0)
                    {
                        width = height * bmp.Width / bmp.Height;
                    }
                    if (height == 0)
                    {
                        height = width * bmp.Height / bmp.Width;
                    }

                    Image imgSource = bmp;
                    Bitmap outBmp = new Bitmap(width, height);
                    Graphics g = Graphics.FromImage(outBmp);
                    g.Clear(Color.Transparent);
                    // 设置画布的描绘质量         
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(imgSource, new Rectangle(0, 0, width, height + 1), 0, 0, imgSource.Width, imgSource.Height, GraphicsUnit.Pixel);
                    g.Dispose();
                    imgSource.Dispose();
                    bmp.Dispose();
                    MemoryStream ms = new MemoryStream();
                    outBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    arr = new byte[ms.Length];
                    ms.Position = 0;
                    ms.Read(arr, 0, (int)ms.Length);
                    ms.Close();
                }
                else
                {
                    MemoryStream ms = new MemoryStream();
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    arr = new byte[ms.Length];
                    ms.Position = 0;
                    ms.Read(arr, 0, (int)ms.Length);
                    ms.Close();
                }
                
                //return outBmp;
            }

            //var image = File.ReadAllBytes(path);
            // 调用通用文字识别, 图片参数为本地图片，可能会抛出网络等异常，请使用try/catch捕获
            // = client.GeneralBasic(arr);
            //Console.WriteLine(result);
            // 如果有可选参数
            var options = new Dictionary<string, object>{
        {"language_type", "CHG_ENG"},
        {"detect_direction", "true"},
        {"detect_language", "true"},
        {"probability", "true"}
    };
            // 带参数调用通用文字识别, 图片参数为本地图片
            var result = client.GeneralBasic(arr, options);
            string res = JsonConvert.SerializeObject(result);
            return res;
        }

        public string GeneralBasicDemoB(string path)
        {
            try
            {
                client = new Baidu.Aip.Ocr.Ocr(API_KEY, SECRET_KEY);
                client.Timeout = 30000;
                var image = File.ReadAllBytes(path);
                //var image = by;
                // 调用通用文字识别, 图片参数为本地图片，可能会抛出网络等异常，请使用try/catch捕获
                //var result = client.GeneralBasic(image);
                // 如果有可选参数
                var options = new Dictionary<string, object>{
                        {"language_type", "CHG_ENG"},
                        {"detect_direction", "true"},
                        {"detect_language", "true"},
                        {"probability", "true"}
                 };
                // 带参数调用通用文字识别, 图片参数为本地图片
                var result = client.GeneralBasic(image, options);
                if (result != null)
                    WriteLog("path:" + path + "，Result" + JsonConvert.SerializeObject(result));

                string str = string.Empty;
                for (int i = 0; i < result["words_result"].Count(); i++)
                {
                    var words = result["words_result"][i]["words"]?.ToString();
                    var en = Regex.Replace(words, "[^a-zA-Z]+", "", RegexOptions.IgnoreCase);
                    var shu = Regex.Replace(words, "[^0-9]+", "", RegexOptions.IgnoreCase);
                    var en_shu = "";
                    if (string.IsNullOrEmpty(shu))
                    {
                        continue;
                    }
                    if (!string.IsNullOrWhiteSpace(en))
                    {
                        en_shu += en;
                    }
                    if (!string.IsNullOrWhiteSpace(shu))
                    {
                        en_shu += shu;
                    }

                    if (!string.IsNullOrWhiteSpace(en_shu))
                        en_shu += "，";

                    if (!string.IsNullOrWhiteSpace(en) && !string.IsNullOrWhiteSpace(shu) && !string.IsNullOrWhiteSpace(en_shu))
                    {
                        en_shu += shu + en + "，";
                    }

                    str += en_shu;
                    //str += result["words_result"][i]["words"] + "，";
                }
                var resultEnd = str.TrimEnd('，');
                if (resultEnd.Length > 200)
                {
                    return resultEnd.Substring(0, 200);
                }

                return resultEnd;
            }
            catch (Exception ex)
            {
                WriteLog("path:" + path + "，Message:" + ex.Message);
                return "";
            }
        }
        public void WriteLog(string strLog)
        {
            string sFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\" + DateTime.Now.ToString("yyyyMM");
            string sFileName = "CheckSpecialStrsLog" + DateTime.Now.ToString("dd") + ".log";
            sFileName = sFilePath + "\\" + sFileName; //文件的绝对路径
            if (!Directory.Exists(sFilePath))//验证路径是否存在
            {
                Directory.CreateDirectory(sFilePath);
                //不存在则创建
            }
            FileStream fs;
            StreamWriter sw;
            if (File.Exists(sFileName))
            //验证文件是否存在，有则追加，无则创建
            {
                fs = new FileStream(sFileName, FileMode.Append, FileAccess.Write);
            }
            else
            {
                fs = new FileStream(sFileName, FileMode.Create, FileAccess.Write);
            }
            sw = new StreamWriter(fs);
            sw.WriteLine(strLog);
            sw.Close();
            fs.Close();
        }

       

        public string GeneralBasicDemoA(string path)
        {
            

            var image = File.ReadAllBytes(path);
            // 调用通用文字识别, 图片参数为本地图片，可能会抛出网络等异常，请使用try/catch捕获
            var result = client.GeneralBasic(image);
            Console.WriteLine(result);
            // 如果有可选参数
            var options = new Dictionary<string, object>{
        {"language_type", "CHN_ENG"},
        {"detect_direction", "true"},
        {"detect_language", "true"},
        {"probability", "true"}
    };
            // 带参数调用通用文字识别, 图片参数为本地图片
            result = client.GeneralBasic(image, options);
            string res = JsonConvert.SerializeObject(result);
            return res;
        }

        public string GeneralBasic(FileStream FileStream)
        {
            BinaryReader r = new BinaryReader(FileStream);

            r.BaseStream.Seek(0, SeekOrigin.Begin);    //将文件指针设置到文件开

            var image = r.ReadBytes((int)r.BaseStream.Length);
            //var image = FileStream.;
            // 调用通用文字识别, 图片参数为本地图片，可能会抛出网络等异常，请使用try/catch捕获
            var result = client.GeneralBasic(image);
            Console.WriteLine(result);
            // 如果有可选参数
            var options = new Dictionary<string, object>{
        {"language_type", "CHN_ENG"},
        {"detect_direction", "true"},
        {"detect_language", "true"},
        {"probability", "true"}
    };
            // 带参数调用通用文字识别, 图片参数为本地图片
            result = client.GeneralBasic(image, options);
            string res = JsonConvert.SerializeObject(result);
            return res;
        }
        public void GeneralBasicUrlDemo()
        {
            var url = "http//www.x.com/sample.jpg";

            // 调用通用文字识别, 图片参数为远程url图片，可能会抛出网络等异常，请使用try/catch捕获
            var result = client.GeneralBasicUrl(url);
            Console.WriteLine(result);
            // 如果有可选参数
            var options = new Dictionary<string, object>{
        {"language_type", "ENG"},
        {"detect_direction", "true"},
        {"detect_language", "true"},
        {"probability", "true"}
    };
            // 带参数调用通用文字识别, 图片参数为远程url图片
            result = client.GeneralBasicUrl(url, options);
            Console.WriteLine(result);
        }
    }
    public class baiduPictureEntity
    {
        public string log_id { get; set; }
        public int words_result_num { get; set; }
        public List<words_result> words_result { get; set; }

    }
    public class words_result
    {
        public string words { get; set; }
    }
}
