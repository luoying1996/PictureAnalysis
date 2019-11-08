using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MoveImagePath.Form2
{
    public partial class Form2 : Form
    {
        public static bool IsDon = false;

        delegate void AsynUpdateUI(string path);
        public Form2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox2.Text) || string.IsNullOrWhiteSpace(dateTimePicker2.Text) || string.IsNullOrWhiteSpace(dateTimePicker1.Text))
            {
                MessageBox.Show("请输入所有必填项");
                return;
            }
            string path = string.Format($"{dateTimePicker2.Text}∮{dateTimePicker1.Text}∮{textBox2.Text}∮{textBox1.Text}");
            DataWrite dataWrite = new DataWrite();//实例化一个写入数据的类
            dataWrite.UpdateUIDelegate += UpdataUIStatus;//绑定更新任务状态的委托
            dataWrite.TaskCallBack += Accomplish;//绑定完成任务要调用的委托
            dataWrite.BackPath = this.textBox1.Text.TrimEnd('/');
            dataWrite.IsOnlyImage = true; //不是处理单张图片
            Thread thread = new Thread(new ParameterizedThreadStart(dataWrite.Write));
            thread.IsBackground = true;
            thread.Start(path);

        }
        private void Accomplish()
        {
            //还可以进行其他的一些完任务完成之后的逻辑处理
            MessageBox.Show("任务完成");
        }
        private void UpdataUIStatus(string path, bool IsError = false)
        {
            string text = "";
            if (InvokeRequired)
            {
                this.Invoke(new AsynUpdateUI(delegate (string s)
                {

                    text += string.Format("{0}", s);
                    ListViewItem Item = new ListViewItem();
                    Item.SubItems[0].Text = text;
                    if (IsError)
                        Item.SubItems[0].BackColor = Color.Red;
                    this.listView1.Items.Add(Item);//显示  
                }), path);
            }
            else
            {
                text += path;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //List<string> s = new List<string>();
            //s.Add("1");
            //s.Add("2");
            //s.Add("3");
            //Expression<Func<string, bool>> lambda = null;
            //lambda.
            //lambda.an
            //lambda x => x != "1";
            //lambda = x => x != "3";
            //var aaa = s.Where(lambda);

            //IsDon = true;
            this.listView1.Clear();
        }

        /// <summary>
        /// 提交到远程FTP站点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            List<string> str = new List<string>();
            GetDirectorFiles(ref str, this.textBox2.Text);
            foreach (var item in str)
            {
                try
                {
                    //FTPHelper.UpFile(item.Replace(textBox1.Text, ""), item);
                    FTPHelper.UpLoadFile(item, textBox1.Text + item.Replace(textBox2.Text, ""));
                }
                catch (Exception ex)
                {
                    FTPHelper.LokWriteRunRecord(true, "FTPSERVER", "Form2" + ex.StackTrace + "行号--" + ex.ToString());
                }

                break;
            }
        }

        public static void GetDirectorFiles(ref List<string> fileList, string dirs)
        {
            DirectoryInfo dir = new DirectoryInfo(dirs);
            //检索表示当前目录的文件和子目录
            FileSystemInfo[] fsinfos = dir.GetFileSystemInfos();

            foreach (FileSystemInfo fsinfo in fsinfos)
            {
                if (fsinfo is DirectoryInfo)
                {
                    GetDirectorFiles(ref fileList, fsinfo.FullName);
                }
                else
                {
                    fileList.Add(fsinfo.FullName);
                }
            }
        }

    }

    public class DataWrite
    {
        public delegate void UpdateUI(string path, bool isError = false);//声明一个更新主线程的委托
        public UpdateUI UpdateUIDelegate;
        public string BackPath { get; set; }
        public bool IsOnlyImage { get; set; }

        public delegate void AccomplishTask();//声明一个在完成任务时通知主线程的委托
        public AccomplishTask TaskCallBack;

        public void Write(object path)
        {
            GetDirectorFiles(path.ToString());
            //任务完成时通知主线程作出相应的处理
            TaskCallBack();
        }

        public void GetDirectorFiles(string path)
        {
            var str = path.Split('∮');
            var filePath = str[2]; //原图路径
            var bakPath = str[3];//备份路径

            try
            {
                //时间查询                开始   结束时间
                var datable = ExcuteQueue(str[0], str[1]);
                var count = datable.Rows.Count;
                UpdateUIDelegate($"开始处理数据：总条数：{count}" + path, true);
                if (datable != null && count > 0)
                {
                    Func<DataRow, bool> lambda = null;
                    foreach (var item in lambda == null ? datable.AsEnumerable() : datable.AsEnumerable().Where(lambda))
                    {
                        if (Form1.IsDon) return;
                        var pathPhotoName = string.Format($"{filePath}\\{item["SGL_Mem_Cid"]}\\{item["SGL_PG_LResourcesFolder"]}\\{item["SGL_PG_LPhotoName"]}_B.jpg");

                        //UpdateUIDelegate($"开始处理原图片路径：" + pathPhotoName, false);
                        if (File.Exists(pathPhotoName))
                        {
                            UpdateUIDelegate("原图--存在" + pathPhotoName, true);
                            var bak = string.Format($"{bakPath}\\{item["SGL_Mem_Cid"]}\\{item["SGL_PG_LResourcesFolder"]}\\");
                             
                            var jpg = "_B.jpg";
                            //FTPHelper.UpFile(bak + jpg, pathPhotoName);//FTP上传
                            var a = FTPHelper.UpLoadFile(pathPhotoName, bak+ item["SGL_PG_LPhotoName"] + jpg); //局域网共享磁盘上传
                            UpdateUIDelegate($"处理局域网共享磁盘上传==>{pathPhotoName}==>{bak + item["SGL_PG_LPhotoName"] + jpg}。结果==>{a}", false);
                            Thread.Sleep(500);
                            if (a)
                            {
                                var b = string.Format($"E:\\bakImage\\{item["SGL_Mem_Cid"]}\\{item["SGL_PG_LResourcesFolder"]}\\");
                                if (!File.Exists(b))
                                {
                                    Directory.CreateDirectory(b);
                                }
                                MoveFile(pathPhotoName,b+ item["SGL_PG_LPhotoName"]);
                                Thread.Sleep(500);
                            }
                        }
                    }
                }
                else
                {
                    UpdateUIDelegate("处理完毕，没有数据了", true);
                    return;
                }
            }
            catch (Exception ex)
            {
                UpdateUIDelegate($"异常数据。错误内容：${ex.Message}", true);
            }
        }

        public void MoveFile(string sourceFileName, string destFileName)
        {
            try
            {
                var jpg = "_B.jpg";
                destFileName += jpg;
                UpdateUIDelegate($"开始处理数据剪切：源文件：{sourceFileName} 备份文件路径：{destFileName}", true);
                File.Move(sourceFileName, destFileName);
            }
            catch (Exception ex)
            {
                UpdateUIDelegate(string.Format("{0} 迁移 Move 文件出错了 {1}  ，错误内容：{2}", sourceFileName, destFileName, ex.Message), true);
            }
        }
        //
        /// <summary>
        /// 查找整个目录
        /// </summary>
        /// <param name="path"></param>
        /// <param name="Uid"></param>
        /// <returns></returns>
        public DataTable ExcuteQueue(string strTime, string endTime)
        {
            //dateTimePicker2
            try
            {
                string sql = string.Format("select  SGL_PG_Liid,SGL_Mem_Cid,SGL_PG_LResourcesFolder,SGL_PG_LPhotoOldName,SGL_PG_LPhotoName from SGL_PhotoGalleryList where SGL_PG_LRemoteStorage=1 and SGL_PG_LaddTimer between '{0}' and '{1}' order by SGL_PG_Liid desc"
                    , strTime, endTime);
                return DBHelp.Query(sql).Tables[0];
            }
            catch (Exception ex)
            {
                UpdateUIDelegate(ex.Message, true);
                return null; //不确定异常是否跟文件有关所以异常则跳过
            }
        }
    }
}
