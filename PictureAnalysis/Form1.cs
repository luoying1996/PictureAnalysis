using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.IO;
using System.Web;

namespace PictureAnalysis
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            baiduAIHelper ba = new baiduAIHelper();
            string timeStar = dateTimePicker1.Value.ToString();
            int size =Convert.ToInt32(numericUpDown1.Text);
            DataSet ds = DBHelp.Query("select top "+ size + " * from SGL_PhotoGalleryList where SGL_PG_Coid in (select SGL_PG_Coid from [dbo].[SGL_PhotoGalleryConfig] where (sgl_pg_cmasterSort = 3 OR sgl_pg_cmasterSort = 4) and SGL_PG_CAddTimer > '"+timeStar + "') and SGL_Is_AuthCheck is null");
            int i = 1;
            foreach (DataRow item in ds.Tables[0].Rows)
            {
                string path = @"D:\SoonnetSiteSupport\Photo\Photo_Resources\" + item["SGL_Mem_Cid"].ToString() + @"\" + item["SGL_PG_LResourcesFolder"].ToString() + @"\" + item["SGL_PG_LPhotoName"].ToString() + "_P.JPG";
                //string path = @"D:\SoonnetSiteSupport\Photo\Photo_Resources\1\1\20190113_636829823082065594_PB.jpg";
                string res = ba.GeneralBasicDemoB(path);
                if (string.IsNullOrWhiteSpace(res))
                {
                    DBHelp.ExecuteSql("update SGL_PhotoGalleryList set SGL_Is_AuthCheck='N'  where SGL_PG_Liid = '" + item["SGL_PG_Liid"].ToString() + "'");
                }
                else
                {
                    DBHelp.ExecuteSql("update SGL_PhotoGalleryList set SGL_Is_AuthCheck='Y',SGL_PG_LPhotoKey='"+ res + "'  where SGL_PG_Liid = '" + item["SGL_PG_Liid"].ToString() + "'");
                }
                string text= "第" + i + "张已识别，ID：" + item["SGL_PG_Liid"].ToString() + ",识别文字：" + res+"\r\n";
                textBox1.Text += text;
                WriteLogA(text);
            }
        }
        public void WriteLogA(string strLog)
        {
            string sFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\" + DateTime.Now.ToString("yyyyMM");
            string sFileName = "textBox" + DateTime.Now.ToString("dd") + ".log";
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
        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_KeyPress(object sender, KeyPressEventArgs e)
        {
            
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = e.KeyChar < '0' || e.KeyChar > '9';  //允许输入数字
            if (e.KeyChar == (char)8)  //允许输入回退键
            {
                e.Handled = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Dispose();
            this.Close();
        }
    }
}
