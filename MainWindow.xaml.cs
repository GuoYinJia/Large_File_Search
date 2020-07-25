using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace 大文件搜索
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //用于刷新时间
        DispatcherTimer timer = new DispatcherTimer();
        //用于选择起始文件夹
        private FolderBrowserDialog folderBrowserDialog = null;
        //找到的文件的集合
        private ObservableCollection<FoundFile> FoundFiles = null;
        //通知线程是否被去取消
        private CancellationTokenSource cts = null;
        //移动文件窗口
        private ShowFileCopyOrMove win = null;
        //显示的提示--1.提示的信息  2.设置参数  搜索按钮是否可用  值  Y/N
        Action<String> showInfo = null;
        Action<bool> EnableSearchButton = null;
        //搜索 开始时间
        DateTime startTime;
       

        public MainWindow()
        {
            InitializeComponent();
            this.Title = "大文件搜索  " + Dns.GetHostName() + "  "+ DateTime.Now.ToString();
                
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();

            Init();
            
        }
       
        void timer_Tick(object sender, EventArgs e)
        {
            this.Title =
                "大文件搜索  "
                + Dns.GetHostName()   //电脑名称
                + "  "
                + DateTime.Now.ToString();
        }
       

        //开始搜索
        private void btnBeginSearch_Click(object sender, RoutedEventArgs e)
        {
            startTime = DateTime.Now;

            lblInfo.Text = "正在查找";
            FoundFiles.Clear();
            cts = new CancellationTokenSource();
            btnBeginSearch.IsEnabled = false;
            String location = txtLocation.Text;
            long length = Convert.ToInt32(txtFileSize.Text) * 1024 * 1024;//转换为字节
            Task tsk = new Task(() =>
            {
                //开始搜索
                searchFiles(location, length);

                if (!cts.IsCancellationRequested)

                    Dispatcher.Invoke(showInfo, "搜索完成" + "   耗时：" + (DateTime.Now - startTime).ToString() + FileNum());

                else
                    Dispatcher.Invoke(showInfo, "搜索被取消" + "   耗时：" + (DateTime.Now - startTime).ToString() + FileNum());
                Dispatcher.Invoke(EnableSearchButton, true);
            });

            try
            {
                tsk.Start();
            }
            catch (Exception ex)
            {
                lblInfo.Text = ex.Message;
                //如果收到取消请求
                if (cts.IsCancellationRequested)
                {
                    Dispatcher.Invoke(showInfo, "搜索已取消" + "   耗时：" + (DateTime.Now - startTime).ToString() + FileNum());
                    Dispatcher.Invoke(EnableSearchButton, true);
                    return;
                }
                else
                    Dispatcher.Invoke(EnableSearchButton, true);
            }
           
        }
       

        //取消搜索
        private void btnCancelSearch_Click(object sender, RoutedEventArgs e)
        {
            if (cts != null)
                cts.Cancel();
            
        }

        //删除
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedFile();
        }

        //移动
        private void btnMove_Click(object sender, RoutedEventArgs e)
        {
            MoveFile();
        }

        //打开
        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();
        }

        //打开文件所在位置
        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder();
        }

        //选择位置
        private void btnChooseLocation_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation();
        }


        #region 初始化操作/搜索路径/查找文件/删除文件/移动文件/打开文件/打开文件夹

        private void Init()
        {
            folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择要搜索的位置";

            FoundFiles = new ObservableCollection<FoundFile>();
            dgFiles.ItemsSource = FoundFiles;  //DataGrid 里面的信息

            showInfo = (info) =>
            {
                //提示栏所显示的信息
                lblInfo.Text = info ;
            };
            EnableSearchButton = (Enabled) =>
            {
                //搜索按钮是否可用
                btnBeginSearch.IsEnabled = Enabled;
            };
          

        }

        /// <summary>
        /// 选择搜索位置的路径
        /// </summary>
        private void ChooseLocation()
        {
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtLocation.Text = folderBrowserDialog.SelectedPath;
            }
        }

        /// <summary>
        /// 寻找符合条件的文件
        /// </summary>
        /// <param name="Location">路径</param>
        /// <param name="fileLength">要求大小</param>
        private void searchFiles(String Location, long fileLength)
        {
            DirectoryInfo dir = null;  //目录
            List<FileInfo> files = null; //文件集合
            FoundFile foundFile = null;
            try
            {
                dir = new DirectoryInfo(Location);//初始化目录
                Dispatcher.Invoke(showInfo, Location);//在工作的线程上去更新窗体的UI元素，显示正在扫描的文件信息---异步操作，无法让窗体显示某些信息，因为这是一个新线程。
                if (cts.IsCancellationRequested)
                {
                    return;
                }
                //启动并行查找--返回指定路径中与搜索模式匹配的文件集合  。
                //排除系统文件.sys

                var query = (from file in dir.GetFiles("*.*", SearchOption.TopDirectoryOnly).AsParallel()
                             where file.Length >= fileLength && (!file.Name.Contains(".sys"))
                             select file);

                files = query.ToList();


                //更新显示
                if (files != null)
                {
                    foreach (var item in files)
                    {
                        //如果收到取消请求
                        if (cts.IsCancellationRequested)
                        {
                            //Dispatcher.Invoke(showInfo, "搜索已取消");
                            //Dispatcher.Invoke(EnableSearchButton, true);
                            return;
                        }
                        foundFile = new FoundFile()
                        {
                            Name = item.Name,
                            Length = item.Length,
                            Location = item.DirectoryName
                        };
                        Action<FoundFile> addFileDelegate = (file) =>
                        {
                            FoundFiles.Add(file);
                        };
                        //线程异步委托 --- 展示在UI界面上
                        Dispatcher.BeginInvoke(addFileDelegate, foundFile);
                    }
                }
                //递归查找每个子文件夹
                foreach (var directory in dir.GetDirectories())
                {
                    searchFiles(directory.FullName, fileLength);
                }
            }
            catch
            {
                Dispatcher.Invoke(showInfo, dir.Name + "无权限访问");

            }
        }

        /// <summary>
        /// 删除网格中选中的文件
        /// </summary>
        private void DeleteSelectedFile()
        {
            int index = dgFiles.SelectedIndex;
            if (index != -1 && index < FoundFiles.Count)
            {
                try
                {
                    String FileName = FoundFiles[index].Name;
                    String FileToBeDelete = FoundFiles[index].Location + @"\\" + FileName;
                    File.Delete(FileToBeDelete);
                    FoundFiles.RemoveAt(index);
                    lblInfo.Text = "文件\"" + FileName + "\"已删除";
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
            }
        }

        /// <summary>
        /// 移动文件
        /// </summary>
        private void MoveFile()
        {
            int index = dgFiles.SelectedIndex;
            if (index == -1 || index >= FoundFiles.Count)
            {
                return;
            }
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    String FileName = FoundFiles[index].Name;
                    String ChoosedFile = FoundFiles[index].Location + @"\\" + FileName;
                    String toDir = folderBrowserDialog.SelectedPath;
                    String toDirFile = toDir.EndsWith("\\") ? toDir + FileName : toDir + @"\\" + FileName;

                    
                    //异步执行文件移动
                    startTime = DateTime.Now;
                    Task tsk = new Task(() =>
                    {
                        Action del = () =>
                        {
                            win = new ShowFileCopyOrMove();
                            win.Information = "\"" + FileName + "\"正在移动中...";
                            win.Topmost = true;//显示在最前端
                            win.Show();
                        };
                        Dispatcher.Invoke(del);
                        try
                        {
                            File.Move(ChoosedFile, toDirFile);
                        }
                        catch
                        {
                            System.Windows.Forms.MessageBox.Show("错误的移动！请检查重试");
                        }
                        FoundFiles[index].Location = toDir;//赋值新的位置
                        Dispatcher.Invoke(showInfo, "\"" + FileName + "\"文件移动完成" + "   耗时：" + (DateTime.Now - startTime).ToString());
                        del = () =>
                        {

                            win.Close();
                            win = null;
                        };
                       
                        Dispatcher.Invoke(del);
                    });
                    tsk.Start();
                }
                catch 
                {
                    System.Windows.Forms.MessageBox.Show("错误的移动！请检查重试");
                }
            }
        }

        /// <summary>
        /// 打开选中文件
        /// </summary>
        private void OpenFile()
        {
            int index = dgFiles.SelectedIndex;
            if (index != -1 && index < FoundFiles.Count)
            {
                try
                {
                    //选择的文件  路径+文件名
                    String ChoosedFile = FoundFiles[index].Location + @"\\" + FoundFiles[index].Name;
                    Process.Start(ChoosedFile);

                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show("异常错误！请检查相应文件 或复制路径打开");
                }
            }
        }

        /// <summary>
        /// 打开选中的文件所在的文件夹
        /// </summary>
        private void OpenFolder()
        {
            int index = dgFiles.SelectedIndex;
            if (index != -1 && index < FoundFiles.Count)
            {
                try
                {
                    String ChoosedFile = FoundFiles[index].Location;
                    Process.Start(ChoosedFile);
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show("异常错误！请检查相应文件夹 或复制路径打开");
                }
            }
        }

        /// <summary>
        /// 文件个数
        /// </summary>
        /// <returns></returns>
        public string FileNum()
        {
            return "  文件个数:" + dgFiles.Items.Count.ToString();
        }
        #endregion

      
    }
}
