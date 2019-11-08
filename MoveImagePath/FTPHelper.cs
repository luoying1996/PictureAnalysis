using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MoveImagePath
{
    public class FTPHelper
    {
        public static FtpWebRequest reqFTP = null;
        public static string ftpServerIP = System.Configuration.ConfigurationManager.AppSettings["IPServer"];
        //连接ftp
        private static void Connect(String path)
        {
            try
            {
                // 根据uri创建FtpWebRequest对象
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(path));
                // 指定数据传输类型
                reqFTP.Method = System.Net.WebRequestMethods.Ftp.UploadFile;
                reqFTP.UseBinary = true;
                reqFTP.UsePassive = false;
                // ftp用户名和密码
                reqFTP.Credentials = new NetworkCredential("useid", "pwd");
            }
            catch (Exception ex)
            {
                LokWriteRunRecord(true, "FTPSERVER", ex.StackTrace + "行号--" + ex.ToString());

            }
        }

        public static void Upload(string filename) //上面的代码实现了从ftp服务器上载文件的功能
        {
            FileInfo fileInf = new FileInfo(filename);
            string uri = "ftp://" + ftpServerIP + "/" + fileInf.Name;
            Connect(uri);//连接          
            // 默认为true，连接不会被关闭
            // 在一个命令之后被执行
            reqFTP.KeepAlive = false;
            // 指定执行什么命令
            reqFTP.Method = WebRequestMethods.Ftp.UploadFile;
            // 上传文件时通知服务器文件的大小
            reqFTP.ContentLength = fileInf.Length;
            // 缓冲大小设置为kb 
            int buffLength = 2048;
            byte[] buff = new byte[buffLength];
            int contentLen;
            // 打开一个文件流(System.IO.FileStream) 去读上传的文件
            FileStream fs = fileInf.OpenRead();
            try
            {
                int allbye = (int)fileInf.Length;
                int startbye = 0;// 把上传的文件写入流
                Stream strm = reqFTP.GetRequestStream();//根据服务器的FTP配置不同，要使用不同的模式，否则会报错
                // 每次读文件流的kb 
                contentLen = fs.Read(buff, 0, buffLength);// 流内容没有结束
                while (contentLen != 0)
                {
                    // 把内容从file stream 写入upload stream 
                    strm.Write(buff, 0, contentLen);
                    contentLen = fs.Read(buff, 0, buffLength);
                    startbye += buffLength;
                }// 关闭两个流
                strm.Close();
                fs.Close();
            }
            catch (Exception ex)
            {
                LokWriteRunRecord(true, "FTPSERVER", ex.StackTrace + "行号--" + ex.ToString());

                fs.Close();
            }

        }
        public static void LokWriteRunRecord(bool OnlyWrite, string WriteName, string RecordString)
        {
            string PpPath = null;
            try
            {
                System.Diagnostics.Debug.Print("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "]::[" + RecordString + "]");
                PpPath = System.Environment.CurrentDirectory + "\\Lok\\";
                using (StreamWriter RunWriter = new StreamWriter(PpPath + WriteName + ".txt", OnlyWrite, System.Text.Encoding.UTF8))
                {
                    RunWriter.WriteLine(System.Environment.NewLine + DateTime.Now.ToString("yyyy/MM/dd  HH:mm:ss:fff"));
                    RunWriter.WriteLine(RecordString + System.Environment.NewLine);
                    RunWriter.Flush();
                    RunWriter.Close();
                }
            }
            catch (Exception FuckProgram)
            {
                System.Diagnostics.Debug.Print("[LokWriteRunRecord]" + PpPath + ":__:" + FuckProgram.ToString() + DateTime.Now.ToString("HH:mm:ss:fff"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename">不带文件名，带文件夹结构</param>
        /// <param name="LocalPath">上传的图片路径，带文件名</param>
        public static void UpFile(string filename, string LocalPath)
        {
            //string LocalPath = @"C:\Users\IT-016\Desktop\2.txt"; //待上传文件
            FtpCheckDirectoryExist(filename);
            FileInfo f = new FileInfo(LocalPath);
            string FileName = f.Name;
            //Path = Path.Replace("\\", "/");
            string ftpRemotePath = filename;
            string FTPPath = ftpServerIP + "//" + ftpRemotePath; //上传到ftp路径,如ftp://***.***.***.**:21/home/test/test.txt
            //实现文件传输协议 (FTP) 客户端
            FtpWebRequest reqFtp = (FtpWebRequest)FtpWebRequest.Create(new Uri(FTPPath));
            reqFtp.UseBinary = true;
            //      reqFtp.Credentials = new NetworkCredential("123", "123"); //设置通信凭据
            reqFtp.KeepAlive = false; //请求完成后关闭ftp连接
            reqFtp.Method = WebRequestMethods.Ftp.UploadFile;
            reqFtp.ContentLength = f.Length;
            int buffLength = 2048;
            byte[] buff = new byte[buffLength];
            int contentLen;
            //读本地文件数据并上传
            FileStream fs = f.OpenRead();
            try
            {
                Stream strm = reqFtp.GetRequestStream();
                contentLen = fs.Read(buff, 0, buffLength);
                while (contentLen != 0)
                {
                    strm.Write(buff, 0, contentLen);
                    contentLen = fs.Read(buff, 0, buffLength);
                }
                strm.Close();
                fs.Close();
            }
            catch (Exception ex)
            {
                LokWriteRunRecord(true, "FTPSERVER", ex.StackTrace + "行号--" + ex.ToString());
            }
        }


        /// <summary>
        /// 上传文件到共享文件夹
        /// </summary>
        /// <param name="sourceFile">本地文件</param>
        /// <param name="remoteFile">远程文件</param>
        public static bool UpLoadFile(string sourceFile, string remoteFile, int islog = 1)
        {
            try
            {
                //判断文件夹是否存在 ->不存在则创建
                var targetFolder = Path.GetDirectoryName(remoteFile);
                DirectoryInfo theFolder = new DirectoryInfo(targetFolder);
                if (theFolder.Exists == false)
                {
                    theFolder.Create();
                }

                var flag = true;


                WebClient myWebClient = new WebClient();
                NetworkCredential cread = new NetworkCredential();
                myWebClient.Credentials = cread;

                using (FileStream fs = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader r = new BinaryReader(fs))
                    {
                        byte[] postArray = r.ReadBytes((int)fs.Length);
                        using (Stream postStream = myWebClient.OpenWrite(remoteFile))
                        {
                            if (postStream.CanWrite == false)
                            {
                                //LogUtil.Error($"{remoteFile} 文件不允许写入~");
                                if (islog > 0)
                                    LokWriteRunRecord(true, "FTPSERVER", remoteFile + " 文件不允许写入~");
                                //com.log("UpLoadFile", remoteFile + " 文件不允许写入~");
                                flag = false;
                            }

                            postStream.Write(postArray, 0, postArray.Length);
                        }
                    }
                }

                return flag;
            }
            catch (Exception ex)
            {
                // string errMsg = $"{remoteFile}  ex:{ex.ToString()}";
                //LogUtil.Error(errMsg);
                //Console.WriteLine(ex.Message);
                if (islog > 0)
                    LokWriteRunRecord(true, "FTPSERVER", ex.StackTrace + "行号--" + ex.ToString());
                //com.log("UpLoadFile", "上传文件到共享文件夹：" + ex.Message);
                return false;
            }
        }



        //判断文件的目录是否存,不存则创建  
        public static void FtpCheckDirectoryExist(string destFilePath)
        {
            string fullDir = FtpParseDirectory(destFilePath);
            string[] dirs = fullDir.Split('\\');
            string curDir = "/";
            for (int i = 0; i < dirs.Length; i++)
            {
                string dir = dirs[i];
                //如果是以/开始的路径,第一个为空    
                if (dir != null && dir.Length > 0)
                {
                    try
                    {
                        curDir += dir + "/";
                        FtpMakeDir(curDir);
                    }
                    catch (Exception)
                    { }
                }
            }
        }

        public static string FtpParseDirectory(string destFilePath)
        {
            return destFilePath.Substring(0, destFilePath.Replace("/", "\\").LastIndexOf("\\"));
        }

        //创建目录  
        public static Boolean FtpMakeDir(string localFile)
        {
            FtpWebRequest req = (FtpWebRequest)WebRequest.Create(ftpServerIP + localFile);
            //req.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
            req.Method = WebRequestMethods.Ftp.MakeDirectory;
            try
            {
                FtpWebResponse response = (FtpWebResponse)req.GetResponse();
                response.Close();
            }
            catch (Exception ex)
            {
                req.Abort();
                return false;
            }
            req.Abort();
            return true;
        }

    }
}
