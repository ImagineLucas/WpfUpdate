using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MessageBox = System.Windows.Forms.MessageBox;

namespace WpfUpdate
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _strPath;
        private UpdateInfo _updateInfo = new UpdateInfo();
        private bool _blSuccess;
        string downUrl = System.Configuration.ConfigurationManager.AppSettings["downloadPath"];
        bool _isStartDown = false;

        private string _oldText = "";
        private string _strInfo = string.Empty;
        public MainWindow()
        {
            InitializeComponent();
            CenterWindowOnScreen();
            _strPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            if (UpdateInfo() == false)
            {
                btnUpdate.IsEnabled = false;
                ShowMsg(true, "Unable to update, please check network connection and make sure file is not corrupted.");
                return;
            }
            //lab_new.Content = "更新版本：" + _updateInfo.version;
            //lab_time.Content = "（" + _updateInfo.update_time + "）";
            //lab_title.Visibility = Visibility.Visible;
            //lab_new.Visibility = Visibility.Visible;
            //lab_time.Visibility = Visibility.Visible;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _updateInfo.infos.Length - 1; i++)
            {
                sb.AppendLine(_updateInfo.infos[i]);
            }
            sb.AppendLine(_updateInfo.infos[_updateInfo.infos.Length - 1]);
        }
        private void Image_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            if (_isStartDown)
                return;
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            KillProcess();
            var pathArr = _strPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var fileName = pathArr[pathArr.Length - 1];
            _isStartDown = true;
            closeImage.IsEnabled = false;
            Thread t = new Thread(() =>
            {
                try
                {
                    DownloadFileDetail(downUrl, fileName);
                    _blSuccess = true;
                }
                catch (Exception)
                {
                    Dispatcher.Invoke(new Action(() => { ShowMsg(true, "Unable to update, current version is too dated, please reinstall instead."); }));
                    _isStartDown = false;
                }
                finally
                {

                }
                if (_blSuccess)
                {
                    Unzip(fileName);
                }
            })
            { IsBackground = true };
            try
            {
                t.Start();
                btnUpdate.IsEnabled = false;
            }
            catch (Exception ex2)
            {
                System.Windows.Forms.MessageBox.Show(ex2.ToString());
            }

        }

        private void DownloadFileDetail(string url, string filename)
        {
            var myrq = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            var response = (System.Net.HttpWebResponse)myrq.GetResponse();
            try
            {
                var fileName = _strPath + filename + ".zip";
                File.Delete(filename);
                var httpStream = response.GetResponseStream();
                var totalBytes = response.ContentLength;
                var outputStream = new FileStream(fileName, FileMode.Create);
                var bufferSize = 4096;
                var buffer = new byte[bufferSize];
                if (httpStream != null)
                {
                    var readCount = httpStream.Read(buffer, 0, bufferSize);
                    var allbye = (int)response.ContentLength;
                    var startbye = 0;
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        pro4.Maximum = totalBytes;
                        pro4.Minimum = 0;
                        pro4.Visibility = Visibility.Visible;
                        lab_num.Visibility = Visibility.Visible;
                        lab_num.Text = "0%";
                        txt_Info.Visibility = Visibility.Visible;
                    }));
                    while (readCount > 0)
                    {
                        outputStream.Write(buffer, 0, readCount);
                        readCount = httpStream.Read(buffer, 0, bufferSize);
                        startbye += readCount;
                        var d = startbye * 100.00 / totalBytes;
                        var startbye1 = startbye;
                        Dispatcher.Invoke(new Action(() =>
                        {
                            this.pro4.Value = startbye1;
                            this.pro4.Visibility = Visibility.Visible;
                            this.lab_num.Visibility = Visibility.Visible;
                            this.lab_num.Text = $"{d:F}" + "%";
                        }));
                        Thread.Sleep(1);
                    }
                }

                this.Dispatcher.Invoke(new Action(() =>
                {
                    this.lab_num.Text = "Download complete";
                    this.pro4.Value = 0;
                }));
                httpStream?.Close();
                outputStream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// 结束进程
        /// </summary>
        private void KillProcess()
        {
            var processes = Process.GetCurrentProcess();
            var name = processes.ProcessName;
            var arr = Directory.GetFiles(_strPath, "*.exe");
            var list = new List<string>();
            foreach (var item in arr)
            {
                var p = System.IO.Path.GetFileNameWithoutExtension(item);
                if (!String.Equals(p, name, StringComparison.CurrentCultureIgnoreCase))
                {
                    list.Add(p.ToLower());
                }
            }
            var myproc = Process.GetProcesses();
            foreach (var item in myproc)
            {
                if (list.Contains(item.ProcessName.ToLower()))
                {
                    item.Kill();
                }
            }
        }

        private bool UpdateInfo()
        {
            var path1 = _strPath + "update.txt";
            var path2 = _strPath + "info.txt";
            if (File.Exists(path1) == false || File.Exists(path2) == false)
            {
                return false;
            }

            var strOldInfo = File.ReadAllText(path2, Encoding.UTF8);
            var strUpdateInfo = File.ReadAllText(path1, Encoding.UTF8);
            UpdateInfo oldInfo = new WpfUpdate.UpdateInfo();
            if (string.IsNullOrEmpty(strUpdateInfo) || string.IsNullOrEmpty(strOldInfo))
            {
                return false;
            }
            try
            {
                var js = new JavaScriptSerializer();
                _updateInfo = js.Deserialize<UpdateInfo>(strUpdateInfo);
                var js2 = new JavaScriptSerializer();
                oldInfo = js.Deserialize<UpdateInfo>(strOldInfo);
            }
            catch (Exception)
            {
                _updateInfo = new WpfUpdate.UpdateInfo();
                oldInfo = new WpfUpdate.UpdateInfo { version = "1.0.0" };
            }
            _oldText = oldInfo.version;
            if (string.IsNullOrEmpty(_updateInfo.version) || string.IsNullOrEmpty(oldInfo.version))
            {
                return false;
            }
            if (_updateInfo.version == oldInfo.version)
            {
                return false;
            }
            
            _strInfo = strUpdateInfo;
            return true;

        }

        private void ShowMsg(bool isError, string msg)
        {
            txtMsg.Text = msg;
            var strImagUrl = "\\img\\success.png";
            if (isError)
            {
                strImagUrl = "\\img\\fail.png";
            }
            var imagetemp = new BitmapImage(new Uri(strImagUrl, UriKind.Relative));
            msg_img.Source = imagetemp;
            gropMsg.Visibility = Visibility.Visible;
        }

        private void CenterWindowOnScreen()
        {
            var screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            var screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            var windowWidth = this.Width;
            var windowHeight = this.Height;
            Left = (screenWidth / 2) - (windowWidth / 2);
            Top = (screenHeight / 2) - (windowHeight / 2);
        }

        private void Unzip(string strFileName)
        {
            var zipFilePath = _strPath + strFileName + ".zip";
            var unZipDir = _strPath;
            var fs = File.OpenRead(zipFilePath);
            try
            {
                using (var s = new ZipInputStream(fs))
                {
                    ZipEntry theEntry;
                    var allBytes = (int)fs.Length;

                    long startByte = 0;
                    Dispatcher.Invoke(new Action(() =>
                    {
                        pro4.Maximum = allBytes;
                        pro4.Minimum = 0;
                        lab_num.Text = "0%";
                        txt_Info.Text = "Unzipping";
                    }));

                    while ((theEntry = s.GetNextEntry()) != null)
                    {
                        string directoryName = System.IO.Path.GetDirectoryName(theEntry.Name);
                        string fileName = System.IO.Path.GetFileName(theEntry.Name);
                        if (!string.IsNullOrEmpty(directoryName))
                        {
                            if (Directory.Exists(unZipDir + directoryName) == false)
                                Directory.CreateDirectory(unZipDir + directoryName);
                        }
                        Console.WriteLine(theEntry.Name);
                        if (!directoryName.EndsWith("\\"))
                            directoryName += "\\";

                        if (fileName != String.Empty)
                        {
                            startByte += theEntry.Size;
                            var b = startByte;
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                pro4.Value = b;
                                var d = b * 100.00 / allBytes;
                                if(d > 100)
                                    d = 100.00;
                                this.lab_num.Text = $"{d:F}" + "%";
                            }));
                            //TODO 自身的话会不会crash处理
                            if (File.Exists(unZipDir + theEntry.Name))
                            {
                                File.Delete(unZipDir + theEntry.Name);
                            }
                            using (var streamWriter = File.Create(unZipDir + theEntry.Name))
                            {
                                var data = new byte[4096];
                                while (true)
                                {
                                    var size = s.Read(data, 0, data.Length);

                                    if (size > 0)
                                    {
                                        streamWriter.Write(data, 0, size);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        Thread.Sleep(1);
                    }
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        File.WriteAllText(_strPath + "info.txt", _strInfo);
                        this.lab_num.Text = "100%";
                        ShowMsg(false, "Update completed, please relaunch Joyschool");
                    }));
                }
                fs.Dispose();
                fs.Close();
                _isStartDown = false;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                _isStartDown = false;
                File.Delete(zipFilePath);
            }
        }
    }


    public class UpdateInfo
    {
        private string _version;

        private string _update_time;

        private string[] _infos;

        public string version { get { return _version; } set { _version = value; } }
        public string update_time { get { return _update_time; } set { _update_time = value; } }
        public string[] infos { get { return _infos; } set { _infos = value; } }
    }
}
