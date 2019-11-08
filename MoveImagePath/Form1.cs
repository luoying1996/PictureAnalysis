using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MoveImagePath
{
    public partial class Form1 : Form
    {
        delegate void AsynUpdateUI(string path);
        public Form1()
        {
            InitializeComponent();
        }
        public static bool IsDon = false;
        private void button1_Click(object sender, EventArgs e)
        {
            IsDon = false;
            if (string.IsNullOrWhiteSpace(this.textBox1.Text) || string.IsNullOrWhiteSpace(this.textBox3.Text))
            {
                MessageBox.Show("请填写两个路径信息");
                return;
            }

            string path = this.textBox1.Text;

            DataWrite dataWrite = new DataWrite();//实例化一个写入数据的类
            dataWrite.UpdateUIDelegate += UpdataUIStatus;//绑定更新任务状态的委托
            dataWrite.TaskCallBack += Accomplish;//绑定完成任务要调用的委托
            dataWrite.BackPath = this.textBox3.Text.TrimEnd('/');
            dataWrite.StartTime = dateTimePicker2.Value;
            dataWrite.EndTime = this.dateTimePicker1.Value;
            dataWrite.IsOnlyImage = false; //不是处理单张图片
            Thread thread = new Thread(new ParameterizedThreadStart(dataWrite.Write));
            thread.IsBackground = true;
            thread.Start(path);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            IsDon = false;

            if (string.IsNullOrWhiteSpace(this.textBox1.Text) || string.IsNullOrWhiteSpace(this.textBox3.Text))
            {
                MessageBox.Show("请填写两个路径信息");
                return;
            }
            string path = this.textBox1.Text;

            DataWrite dataWrite = new DataWrite();//实例化一个写入数据的类
            dataWrite.UpdateUIDelegate += UpdataUIStatus;//绑定更新任务状态的委托
            dataWrite.TaskCallBack += Accomplish;//绑定完成任务要调用的委托
            dataWrite.BackPath = this.textBox3.Text.TrimEnd('/');
            dataWrite.StartTime = dateTimePicker2.Value;
            dataWrite.EndTime = this.dateTimePicker1.Value;
            dataWrite.IsOnlyImage = true; //不是处理单张图片
            Thread thread = new Thread(new ParameterizedThreadStart(dataWrite.Write));
            thread.IsBackground = true;
            thread.Start(path);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            IsDon = false;

            if (string.IsNullOrWhiteSpace(this.textBox1.Text) || string.IsNullOrWhiteSpace(this.textBox3.Text))
            {
                MessageBox.Show("请填写两个路径信息");
                return;
            }
            if (MessageBox.Show("你确定要剪切数据到指定初始化目录么", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string path = this.textBox3.Text;

                DataWrite dataWrite = new DataWrite();//实例化一个写入数据的类
                dataWrite.UpdateUIDelegate += UpdataUIStatus;//绑定更新任务状态的委托
                dataWrite.TaskCallBack += Accomplish;//绑定完成任务要调用的委托
                dataWrite.BackPath = this.textBox1.Text.TrimEnd('/');
                dataWrite.StartTime = dateTimePicker2.Value;
                dataWrite.EndTime = this.dateTimePicker1.Value;
                Thread thread = new Thread(new ParameterizedThreadStart(dataWrite.RollBack));
                thread.IsBackground = true;
                thread.Start(path);
            }
        }

        /// <summary>
        /// 更新UI
        /// </summary>
        /// <param name="step"></param>
        private void UpdataUIStatus(string path, bool IsError = false)
        {
            string text = "";
            if (InvokeRequired)
            {
                this.Invoke(new AsynUpdateUI(delegate (string s)
                {

                    text += string.Format("正在执行目录： {0} 目录\n/n", s);
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

        //完成任务时需要调用
        private void Accomplish()
        {
            //还可以进行其他的一些完任务完成之后的逻辑处理
            MessageBox.Show("任务完成");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            IsDon = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            this.Hide();
            var dio =new MoveImagePath.Form2.Form2();
            dio.Show();
        }
    }

    public class DataWrite
    {
        public delegate void UpdateUI(string path, bool isError = false);//声明一个更新主线程的委托
        public UpdateUI UpdateUIDelegate;
        public string BackPath { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public bool IsOnlyImage { get; set; }

        public delegate void AccomplishTask();//声明一个在完成任务时通知主线程的委托
        public AccomplishTask TaskCallBack;

        public void Write(object path)
        {
            GetDirectorFiles(path.ToString(), BackPath);
            //任务完成时通知主线程作出相应的处理
            TaskCallBack();
        }


        public void RollBack(object path)
        {
            var lst = new List<string>();
            GetDirectorFiles(ref lst, path.ToString());
            Parallel.ForEach(lst, (item, loopState) =>
            {
                if (Form1.IsDon) return;

                UpdateUIDelegate(item + $"--还原来到路径：（{item.Replace(path.ToString(), BackPath)}）");
                MoveFile(item, item.Replace(path.ToString(), BackPath));
            });
            //foreach (var item in lst)
            //{
            //    if (Form1.IsDon) return;
            //    UpdateUIDelegate(item + $"--还原来到路径：（{item.Replace(path.ToString(), BackPath)}）");
            //    MoveFile(item, item.Replace(path.ToString(), BackPath));
            //}
            //任务完成时通知主线程作出相应的处理
            TaskCallBack();
        }

        /// <summary>
        /// 递归还原文件
        /// </summary>
        /// <param name="fileList"></param>
        /// <param name="dirs"></param>
        public void GetDirectorFiles(ref List<string> fileList, string dirs)
        {
            DirectoryInfo dir = new DirectoryInfo(dirs);
            //检索表示当前目录的文件和子目录
            FileSystemInfo[] fsinfos = dir.GetFileSystemInfos();

            foreach (FileSystemInfo fsinfo in fsinfos)
            {
                if (Form1.IsDon) return;

                if (fsinfo is DirectoryInfo)
                {
                    UpdateUIDelegate(fsinfo.FullName + "读取，查询所有");
                    GetDirectorFiles(ref fileList, fsinfo.FullName);
                }
                else
                {
                    fileList.Add(fsinfo.FullName);
                }
            }
        }

        /// <summary>
        /// 递归实现文件查找
        /// </summary>
        /// <param name="fileList"></param>
        /// <param name="dirs"></param>
        public void GetDirectorFiles(string path, string backPath)
        {

            DirectoryInfo dir = new DirectoryInfo(path);
            //检索表示当前目录的文件和子目录
            FileSystemInfo[] fsinfos = dir.GetFileSystemInfos(); //获取当前目录  206232
            fsinfos = fsinfos.Where(x => x.LastAccessTime >= StartTime && x.LastAccessTime <= EndTime).OrderBy(x => x.LastAccessTime).ToArray();
            foreach (FileSystemInfo fsinfo in fsinfos)
            {
                if (Form1.IsDon) return;

                //写入一条数据，调用更新主线程ui状态的委托        -  
                UpdateUIDelegate(fsinfo.Name);

                if (fsinfo is DirectoryInfo)        //判断是否是文件夹
                {
                    DirectoryInfo er = new DirectoryInfo(fsinfo.FullName); //4d0bfd36-b645-4ab5-8029-0366de1f336a
                    //检索表示当前目录的文件和子目录
                    FileSystemInfo[] erFsInfo = er.GetFileSystemInfos(); //获取当前目录  4d0bfd36-b645-4ab5-8029-0366de1f336a  //20190706_636980412288405970.jpg
                    //erFsInfo = erFsInfo.Where(x => x.LastAccessTime >= StartTime && x.LastAccessTime <= EndTime).ToArray();

                    foreach (var item in erFsInfo)
                    {
                        if (Form1.IsDon) return;

                        if (!IsOnlyImage)
                        {
                            //处理图册,图册存在情况跳过里面 单张图片不存在情况，采用另一个功能去做清理 btn2
                            var result = ExcuteQueue(item, fsinfo.Name);
                            if (!result)//不存在
                            {
                                MoveToFilePath(item.FullName, backPath);
                            }
                        }
                        else
                        {
                            //处理单张图片
                            var table = ExcuteQueue2(item, fsinfo.Name);
                            if (table != null && table.Rows?.Count > 0)//不存在
                            {
                                MoveToFilePath(item.FullName, backPath, table);
                            }
                            //else
                            //{
                            //    MoveToFilePath(item.FullName, backPath); //处理找不到图册的
                            //}
                        }

                    }
                    // GetDirectorFiles(ref fileList, fsinfo.FullName);//递归
                }
            }
        }

        /// <summary>
        /// 移动到备份目录
        /// </summary>
        /// <param name="FullName"></param>
        public void MoveToFilePath(string FullName, string backPath)
        {
            if (Form1.IsDon) return;

            var fuarry = FullName.Split('\\');

            var Uid = fuarry[fuarry.Length - 2];
            var guid = fuarry[fuarry.Length - 1];
            try
            {
                if (!File.Exists(string.Format("{0}\\{1}\\{2}", backPath, Uid, guid)))
                {
                    Directory.CreateDirectory(string.Format("{0}\\{1}\\{2}", backPath, Uid, guid));
                }
                DirectoryInfo er = new DirectoryInfo(FullName);
                //检索表示当前目录的文件和子目录
                FileSystemInfo[] erFsInfo = er.GetFileSystemInfos(); //获取当前目录
                foreach (var item in erFsInfo)
                {
                    if (Form1.IsDon) return;

                    UpdateUIDelegate(string.Format("{0} / {1}  迁移 Move 文件 {2}", Uid, guid, item.Name));
                    MoveFile(item.FullName, string.Format("{0}\\{1}\\{2}\\{3}", backPath, Uid, guid, item.Name));
                }
            }
            catch (Exception ex)
            {
                UpdateUIDelegate(string.Format("{0} / {1}  移动到备份目录文件出错了，错误内容：{2}", Uid, guid, ex.Message), true);
            }
        }

        public void MoveFile(string sourceFileName, string destFileName)
        {
            try
            {
                File.Move(sourceFileName, destFileName);
            }
            catch (Exception ex)
            {
                UpdateUIDelegate(string.Format("{0} 迁移 Move 文件出错了 {1}  ，错误内容：{2}", sourceFileName, destFileName, ex.Message), true);
            }
        }
        /// <summary>
        /// 处理备份
        /// </summary>
        /// <param name="FullName"></param>
        /// <param name="backPath"></param>
        /// <param name="dt"></param>
        public void MoveToFilePath(string FullName, string backPath, DataTable dt)
        {
            var fuarry = FullName.Split('\\');

            var Uid = fuarry[fuarry.Length - 2];
            var guid = fuarry[fuarry.Length - 1];
            try
            {
                if (!Directory.Exists(string.Format("{0}\\{1}\\{2}", backPath, Uid, guid)))
                {
                    Directory.CreateDirectory(string.Format("{0}\\{1}\\{2}", backPath, Uid, guid));
                }
                DirectoryInfo er = new DirectoryInfo(FullName);
                //检索表示当前目录的文件和子目录
                FileSystemInfo[] erFsInfo = er.GetFileSystemInfos(); //获取当前目录
                erFsInfo = erFsInfo.Where(x => x.Name.Contains("_S.jpg")).ToArray();
                foreach (var item in erFsInfo)
                {
                    if (Form1.IsDon) return;

                    var extentionName = item.Name.Split('.');
                    var removeS = item.Name.Split('.')[0].TrimEnd('S');
                    var fullName = item.FullName.Split('.')[0].TrimEnd('S');
                    if (!dt.AsEnumerable().Any<DataRow>(C => C["SGL_PG_LPhotoName"] + "_S".ToLower() == extentionName[0].ToLower()))
                    {
                        UpdateUIDelegate(string.Format("{0} / {1}  迁移 Move 文件 {2}", Uid, guid, item.Name));
                        MoveFile(fullName+"S.jpg", string.Format("{0}\\{1}\\{2}\\{3}", backPath, Uid, guid, removeS + "S.jpg"));
                        MoveFile(fullName+"P.jpg", string.Format("{0}\\{1}\\{2}\\{3}", backPath, Uid, guid, removeS + "P.jpg"));
                        MoveFile(fullName+"PB.jpg", string.Format("{0}\\{1}\\{2}\\{3}", backPath, Uid, guid, removeS + "PB.jpg"));
                        MoveFile(fullName+"B.jpg", string.Format("{0}\\{1}\\{2}\\{3}", backPath, Uid, guid, removeS + "B.jpg"));
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateUIDelegate(string.Format("{0} / {1}  迁移 Move 文件出错了，错误内容：{2}", Uid, guid, ex.Message), true);
            }
        }

        /// <summary>
        /// 查找整个目录
        /// </summary>
        /// <param name="path"></param>
        /// <param name="Uid"></param>
        /// <returns></returns>
        public bool ExcuteQueue(FileSystemInfo path, string Uid)
        {
            try
            {
                string sql = string.Format("select Count(1) from[dbo].[SGL_PhotoGalleryConfig] where SGL_Mem_Cid = {0}  and SGL_PG_CResourcesFolder = '{1}'", Uid, path.Name);
                return DBHelp.ExecuteScalar(sql) > 0;
            }
            catch (Exception ex)
            {
                UpdateUIDelegate(ex.Message, true);
                return true; //不确定异常是否跟文件有关所以异常则跳过
            }
        }

        /// <summary>
        /// 查找单张
        /// </summary>
        /// <param name="path"></param>
        /// <param name="Uid"></param>
        /// <returns></returns>
        public DataTable ExcuteQueue2(FileSystemInfo path, string Uid)
        {
            try
            {
                string sql = string.Format("select SGL_PG_LPhotoName from [dbo].[SGL_PhotoGalleryList] where SGL_Mem_Cid = {0} and SGL_PG_LResourcesFolder = '{1}'", Uid, path.Name);
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
