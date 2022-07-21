using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using System.Xml.Linq;
using System.Xml.XPath;

namespace FocusStudy
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string uiautomatorErrorMessage = "ERROR: could not get idle state.";
        private DispatcherTimer dispatcherTimer;
        private bool run = false;

        public MainWindow()
        {
            InitializeComponent();
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = TimeSpan.FromSeconds(5);
            dispatcherTimer.Tick += DispatcherTimer_Tick;
        }

        private async void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            this.dispatcherTimer.Stop();
            if (run)
            {
                WriteLog("检测状态……");
                var response = await this.GetUiautomatorDump();
                String message;
                if (response == uiautomatorErrorMessage)
                {
                    TapScreen(540, 1315);
                    TapScreen(540, 1315);
                    message = "已完成一次签到";
                }
                else
                {
                    message = this.ParseStudyStatus(response);
                }
                WriteLog( message );
                this.dispatcherTimer.Start();
            }
        }

        private void btn_MonitorSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (run)
            {
                dispatcherTimer.Stop();
                run = false;
                WriteLog("关闭监控");
                this.btn_MonitorSwitch.Content = "开始学习";
            }
            else 
            {
                dispatcherTimer.Start();
                run = true;
                WriteLog("开启监控");
                this.btn_MonitorSwitch.Content = "结束学习";
            }
            
        }

        private void WriteLog( String line )
        {
            if( this.lab_Message.Text.Length > 200)
            {
                this.lab_Message.Text = string.Empty;
            }

            lab_Message.Text = String.Format("{0}:{1}\r\n{2}", DateTime.Now.ToString("HH:mm:ss"), line, lab_Message.Text);
        }

        private Task<string> GetUiautomatorDump()
        {
            var tcs = new TaskCompletionSource<string>();

            Process tracert = new Process();
            var output = string.Empty;

            ProcessStartInfo startInfo = tracert.StartInfo;
            startInfo.FileName = "adb.exe";
            startInfo.Arguments = "exec-out uiautomator dump /dev/tty";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            startInfo.StandardOutputEncoding = Encoding.UTF8;

            tracert.EnableRaisingEvents = true;

            tracert.OutputDataReceived += (s, e) =>
            {
                //Console.WriteLine(e.Data);
                output += e.Data;
            };

            tracert.Exited += (s, e) =>
            {
                tcs.TrySetResult(output);
            };

            tracert.Start();
            tracert.BeginOutputReadLine();

            return tcs.Task;
        }

        private void TapScreen( int x , int y)
        {
            Process tracert = new Process();
            var output = string.Empty;

            ProcessStartInfo startInfo = tracert.StartInfo;
            startInfo.FileName = "adb.exe";
            startInfo.Arguments = String.Format( "shell input tap {0} {1}",x,y);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;

            tracert.EnableRaisingEvents = true;
            tracert.Start();
            tracert.BeginOutputReadLine();
        }

        private String ParseStudyStatus( String content )
        {
            var retValue = "未在学习中，请进入到学习视频页面";
            content = content.Replace("UI hierchary dumped to: /dev/tty", string.Empty);
            XDocument document;
            try
            {
                document = XDocument.Parse(content);
            }
            catch( Exception ex)
            {
                return ex.Message + "解析xml错误";
            }
            var titlleBarElement = document.XPathSelectElement("//node[@resource-id='com.alibaba.android.rimet:id/title_bar_name']");
            if (titlleBarElement != null && titlleBarElement.Attribute("text") != null && titlleBarElement.Attribute("text").Value == "视频详情")
            {
                var warnElement = document.XPathSelectElement("//node[@text='!']");
                if (warnElement != null)
                {
                    retValue = warnElement.Parent.Elements().Last().Attribute("text").Value;
                }
            }

            return retValue;
        }
    }
}
