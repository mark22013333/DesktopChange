using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Diagnostics;

using ThreadingTimer = System.Threading.Timer;
using TimersTimer = System.Timers.Timer;


namespace DesktopChange
{
    public partial class Form1 : Form
    {
        #region 宣告
        List<string> myStringLists = new List<string>();
        Initial initt = new Initial();
        private NotifyIcon notifyIcon1;
        private ThreadingTimer _ThreadTimer = null;
        private int intBuffer = 0;
        private int tempnext = 0;
        private String imageFileName = "";
        private String filenamelist = @"C:\DesktopSetup\secret_plan.txt"; //寫入檔案路徑的文字檔
        private String TempPath = @"C:\DesktopSetup\TempPath.txt";//讀取的數字為第X個陣列
        public delegate void HotkeyEventHandler(int HotKeyID);
        Hotkey hotkey;
        #endregion

        #region 全域熱鍵類
        public class Hotkey : IMessageFilter
        {
            System.Collections.Hashtable keyIDs = new System.Collections.Hashtable();
            IntPtr hWnd;
            public event HotkeyEventHandler OnHotkey;
            public enum KeyFlags
            {
                MOD_ALT = 0x1,
                MOD_CONTROL = 0x2,
                MOD_SHIFT = 0x4,
                MOD_WIN = 0x8
            }
            [DllImport("user32.dll")]
            public static extern UInt32 RegisterHotKey(IntPtr hWnd, UInt32 id, UInt32 fsModifiers, UInt32 vk);
            [DllImport("user32.dll")]
            public static extern UInt32 UnregisterHotKey(IntPtr hWnd, UInt32 id);
            [DllImport("kernel32.dll")]
            public static extern UInt32 GlobalAddAtom(String lpString);
            [DllImport("kernel32.dll")]
            public static extern UInt32 GlobalDeleteAtom(UInt32 nAtom);

            public Hotkey(IntPtr hWnd)
            {
                this.hWnd = hWnd;
                Application.AddMessageFilter(this);
            }

            public int RegisterHotkey(Keys Key, KeyFlags keyflags)
            {
                UInt32 hotkeyid = GlobalAddAtom(Guid.NewGuid().ToString());
                RegisterHotKey((IntPtr)hWnd, hotkeyid, (UInt32)keyflags, (UInt32)Key);
                keyIDs.Add(hotkeyid, hotkeyid);
                return (int)hotkeyid;
            }
            public void UnregisterHotkeys()
            {
                Application.RemoveMessageFilter(this);
                foreach (UInt32 key in keyIDs.Values)
                {
                    UnregisterHotKey(hWnd, key);
                    GlobalDeleteAtom(key);
                }
            }
            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == 0x312) //WM_HOTKEY
                {
                    if (OnHotkey != null)
                    {
                        foreach (UInt32 key in keyIDs.Values)
                        {
                            if ((UInt32)m.WParam == key)
                            {
                                OnHotkey((int)m.WParam);
                                return true;
                            }
                        }
                    }
                }
                return false;
            }     
        }
        #endregion

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            //指定使用的容器
            notifyIcon1 = new NotifyIcon(components)
            {
                //建立NotifyIcon
                Icon = new Icon(@"C:\DesktopSetup\bread.ico"),
                Text = "桌布程式"
            };
            notifyIcon1.MouseDoubleClick += new MouseEventHandler(Form1_MouseClick);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                hotkey = new Hotkey(this.Handle); //指定this.Handle給hotkey類別
                int Hotkey1 = hotkey.RegisterHotkey(Keys.A, Hotkey.KeyFlags.MOD_ALT);
                hotkey.OnHotkey += new HotkeyEventHandler(下一張方法);

                initt.Init();
                button4.PerformClick();
                Text = "DesktopChange 開啟時間  " + DateTime.Now.ToString("HH:mm:ss");
                timer1.Start();
                button1.Enabled = false;
                button3.Enabled = false;
                if (initt.變更模式 == "正常")
                {
                    button1_Click(sender, e);
                    button1.BackColor = Color.LightGreen;
                    button3.BackColor = Color.DimGray;
                    label11.Text = "正常模式";
                }
                else if (initt.變更模式 == "隨機")
                {
                    button3_Click(sender, e);
                    button1.BackColor = Color.DimGray;
                    button3.BackColor = Color.LightGreen;
                    label11.Text = "隨機模式";
                }
            }
            catch (Exception ex1)
            {
                #region 發生例外寫LOG
                String nowtime = DateTime.Now.ToString("yyyyMMdd-HH：mm");

                if (!File.Exists("C:\\LOG\\"))
                {
                    Directory.CreateDirectory("C:\\LOG\\");
                }
                if (!File.Exists("C:\\LOG\\" + nowtime + "程式例外.txt"))
                {
                    FileStream fileStream = new FileStream("C:\\LOG\\" + nowtime + "程式例外.txt", FileMode.Create);
                    fileStream.Close();
                }
                using (StreamWriter file = new StreamWriter("C:\\LOG\\" + nowtime + "程式例外.txt", true))
                {
                    file.WriteLine("[ " + nowtime + " ]" + "程式紀錄出現例外：" + ex1.Message + "\r\n\r\n" +
                        "StackTrace：" + "[===== " + ex1.StackTrace + " =====]\r\n\r\n" +
                        "Source：" + ex1.Source + "\r\n\r\n" +
                        "TargetSite：" + ex1.TargetSite + "\r\n\r\n" +
                        "DATA：" + ex1.Data + "\r\n\r\n" +
                        "HResult：" + ex1.HResult + "\r\n\r\n" +
                        "HelpLink：" + ex1.HelpLink + "\r\n\r\n" +
                        "InnerException：" + ex1.InnerException);
                }
                #endregion
            }
        }

        #region 變更桌布
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SystemParametersInfo(UInt32 uiAction, UInt32 uiParam, String pvParam, UInt32 fWinIni);
        private static UInt32 SPI_SETDESKWALLPAPER = 20;
        private static UInt32 SPIF_UPDATEINIFILE = 0x1;
        public void SetImage(string filename)
        {
            if (filename != "")
            {
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, filename, SPIF_UPDATEINIFILE);
            }
        }
        #endregion

        #region 正常模式
        private void 正常模式(object State)
        {
            try
            {
                檔案列表();
                String Buffer = "";
                FileStream f1 = new FileStream(TempPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                StreamReader sr = new StreamReader(f1);
                Buffer = sr.ReadLine();
                intBuffer = Convert.ToInt16(Buffer);
                sr.Dispose();
                sr.Close();
                f1.Dispose();
                f1.Close();
 
                for (int i = intBuffer; i < myStringLists.Count; i++)
                {
                    if (myStringLists[i] != "")
                    {
                        SetImage(myStringLists[i]);
                        intBuffer = i;
                        label6.Text = (intBuffer + 1).ToString();
                        label9.Text = Path.GetFileNameWithoutExtension(myStringLists[intBuffer]);
                        Thread.Sleep(Convert.ToInt32(initt.變更桌布時間) * 1000);
                        if (tempnext > 0)
                        {
                            i = tempnext;
                            tempnext++;
                        }
                    }
                }
                if (intBuffer == myStringLists.Count - 1)
                {
                    重新一輪();
                    正常模式(null);
                }
            }
            catch (Exception ex1)
            {
                #region 發生例外寫LOG
                String nowtime = DateTime.Now.ToString("yyyyMMdd-HH：mm");

                if (!File.Exists("C:\\LOG\\"))
                {
                    Directory.CreateDirectory("C:\\LOG\\");
                }
                if (!File.Exists("C:\\LOG\\" + nowtime + "程式例外.txt"))
                {
                    FileStream fileStream = new FileStream("C:\\LOG\\" + nowtime + "程式例外.txt", FileMode.Create);
                    fileStream.Close();
                }
                using (StreamWriter file = new StreamWriter("C:\\LOG\\" + nowtime + "程式例外.txt", true))
                {
                    file.WriteLine("[ " + nowtime + " ]" + "程式紀錄出現例外：" + ex1.Message + "\r\n\r\n" +
                        "StackTrace：" + "[===== " + ex1.StackTrace + " =====]\r\n\r\n" +
                        "Source：" + ex1.Source + "\r\n\r\n" +
                        "TargetSite：" + ex1.TargetSite + "\r\n\r\n" +
                        "DATA：" + ex1.Data + "\r\n\r\n" +
                        "HResult：" + ex1.HResult + "\r\n\r\n" +
                        "HelpLink：" + ex1.HelpLink + "\r\n\r\n" +
                        "InnerException：" + ex1.InnerException);
                }
                #endregion
            }
        }
        #endregion

        #region 隨機模式
        private void 隨機模式(object State)
        {
            try
            {
                檔案列表();
                Random Counter = new Random(Guid.NewGuid().GetHashCode());
                for (int i = 0; i < myStringLists.Count; i++)
                {
                    int d = Counter.Next(0, myStringLists.Count);
                    SetImage(myStringLists[d]);
                    label6.Text = (d + 1).ToString();
                    label9.Text = Path.GetFileNameWithoutExtension(myStringLists[d]);
                    Thread.Sleep(Convert.ToInt32(initt.變更桌布時間) * 1000);
                }
                隨機模式(null);
            }
            catch (Exception ex1)
            {
                #region 發生例外寫LOG
                String nowtime = DateTime.Now.ToString("yyyyMMdd-HH：mm");

                if (!File.Exists("C:\\LOG\\"))
                {
                    Directory.CreateDirectory("C:\\LOG\\");
                }
                if (!File.Exists("C:\\LOG\\" + nowtime + "程式例外.txt"))
                {
                    FileStream fileStream = new FileStream("C:\\LOG\\" + nowtime + "程式例外.txt", FileMode.Create);
                    fileStream.Close();
                }
                using (StreamWriter file = new StreamWriter("C:\\LOG\\" + nowtime + "程式例外.txt", true))
                {
                    file.WriteLine("[ " + nowtime + " ]" + "程式紀錄出現例外：" + ex1.Message + "\r\n\r\n" +
                        "StackTrace：" + "[===== " + ex1.StackTrace + " =====]\r\n\r\n" +
                        "Source：" + ex1.Source + "\r\n\r\n" +
                        "TargetSite：" + ex1.TargetSite + "\r\n\r\n" +
                        "DATA：" + ex1.Data + "\r\n\r\n" +
                        "HResult：" + ex1.HResult + "\r\n\r\n" +
                        "HelpLink：" + ex1.HelpLink + "\r\n\r\n" +
                        "InnerException：" + ex1.InnerException);
                }
                #endregion
            }
        }
        #endregion

        #region 取得檔案列表並寫入陣列
        private void 檔案列表()
        {
            imageFileName = initt.桌布路徑;
            myStringLists.Clear();
            richTextBox1.Clear();
            if (!File.Exists(TempPath))
            {
                StreamWriter sw2 = new StreamWriter(TempPath);
                sw2.WriteLine(0);
                sw2.Close();
            }
            StreamWriter sw = new StreamWriter(filenamelist);
            String[] filename = Directory.GetFiles(imageFileName, "*.*", SearchOption.AllDirectories);
            foreach (string s in filename)
            {
                string Format = s.Substring(s.Length - 3, 3).ToUpper();
                if (Format == "JPG" || Format == "PNG")
                {
                    sw.WriteLine(s + "\n");
                    myStringLists.Add(s);
                    richTextBox1.AppendText(s + "\r\n");
                }
            }
            label4.Text = myStringLists.Count.ToString();
            sw.Close();
        }
        #endregion

        #region 正常模式按鈕
        public void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //initt.Init();
                //int s = myStringLists.Count * Int32.Parse(initt.變更桌布時間);
                string currentName = new StackTrace(true).GetFrame(0).GetMethod().Name;
                _ThreadTimer = new ThreadingTimer(new TimerCallback(正常模式), currentName, 1, -1);
                //Normal = new Thread(new ParameterizedThreadStart(正常模式));
                //Normal.Start();
            }
            catch (Exception ex1)
            {
                #region 發生例外寫LOG
                String nowtime = DateTime.Now.ToString("yyyyMMdd-HH：mm");

                if (!File.Exists("C:\\LOG\\"))
                {
                    Directory.CreateDirectory("C:\\LOG\\");
                }
                if (!File.Exists("C:\\LOG\\" + nowtime + "程式例外.txt"))
                {
                    FileStream fileStream = new FileStream("C:\\LOG\\" + nowtime + "程式例外.txt", FileMode.Create);
                    fileStream.Close();
                }
                using (StreamWriter file = new StreamWriter("C:\\LOG\\" + nowtime + "程式例外.txt", true))
                {
                    file.WriteLine("[ " + nowtime + " ]" + "程式紀錄出現例外：" + ex1.Message + "\r\n\r\n" +
                        "StackTrace：" + "[===== " + ex1.StackTrace + " =====]\r\n\r\n" +
                        "Source：" + ex1.Source + "\r\n\r\n" +
                        "TargetSite：" + ex1.TargetSite + "\r\n\r\n" +
                        "DATA：" + ex1.Data + "\r\n\r\n" +
                        "HResult：" + ex1.HResult + "\r\n\r\n" +
                        "HelpLink：" + ex1.HelpLink + "\r\n\r\n" +
                        "InnerException：" + ex1.InnerException);
                }
                #endregion
            }
        }
        #endregion

        #region 重啟程式按鈕
        public void button2_Click(object sender, EventArgs e)
        {
            _ThreadTimer.Dispose();
            Application.ExitThread();
            Restart();
        }
        #endregion

        #region 隨機模式按鈕
        public void button3_Click(object sender, EventArgs e)
        {
            try
            {
                string currentName = new StackTrace(true).GetFrame(0).GetMethod().Name;
                _ThreadTimer = new ThreadingTimer(new TimerCallback(隨機模式), currentName, 1, -1);
            }
            catch (Exception ex1)
            {
                #region 發生例外寫LOG
                String nowtime = DateTime.Now.ToString("yyyyMMdd-HH：mm");

                if (!File.Exists("C:\\LOG\\"))
                {
                    Directory.CreateDirectory("C:\\LOG\\");
                }
                if (!File.Exists("C:\\LOG\\" + nowtime + "程式例外.txt"))
                {
                    FileStream fileStream = new FileStream("C:\\LOG\\" + nowtime + "程式例外.txt", FileMode.Create);
                    fileStream.Close();
                }
                using (StreamWriter file = new StreamWriter("C:\\LOG\\" + nowtime + "程式例外.txt", true))
                {
                    file.WriteLine("[ " + nowtime + " ]" + "程式紀錄出現例外：" + ex1.Message + "\r\n\r\n" +
                        "StackTrace：" + "[===== " + ex1.StackTrace + " =====]\r\n\r\n" +
                        "Source：" + ex1.Source + "\r\n\r\n" +
                        "TargetSite：" + ex1.TargetSite + "\r\n\r\n" +
                        "DATA：" + ex1.Data + "\r\n\r\n" +
                        "HResult：" + ex1.HResult + "\r\n\r\n" +
                        "HelpLink：" + ex1.HelpLink + "\r\n\r\n" +
                        "InnerException：" + ex1.InnerException);
                }
                #endregion
            }
        }
        #endregion

        #region 最後一張重新再一次
        public void 重新一輪()
        {
            SetImage(myStringLists[myStringLists.Count - 1]);
            Thread.Sleep(Convert.ToInt32(initt.變更桌布時間) * 1000);
            File.Delete(TempPath);
            StreamWriter sw22 = new StreamWriter(TempPath);
            sw22.WriteLine(0);
            sw22.Dispose();
            sw22.Close();
        }
        #endregion

        public void 下一張方法(int i)
        {
            MessageBox.Show("test");
          
        }

        #region 播放下一張桌布按鈕
        private void button5_Click(object sender, EventArgs e)
        {
            switch (initt.變更模式)
            {
                case "正常":
                    intBuffer++;
                    if (intBuffer == myStringLists.Count)
                    {
                        intBuffer = 0;
                        button2_Click(sender, e);
                        return;
                    }
                    else if (intBuffer < myStringLists.Count)
                    {
                        SetImage(myStringLists[intBuffer]);
                        label6.Text = (intBuffer + 1).ToString();
                        label9.Text = Path.GetFileNameWithoutExtension(myStringLists[intBuffer]);
                        tempnext = intBuffer;
                    }
                    return;

                case "隨機":
                    Random Counter = new Random(Guid.NewGuid().GetHashCode());
                    int d = Counter.Next(0, myStringLists.Count);
                    SetImage(myStringLists[d]);
                    label6.Text = d.ToString();
                    label9.Text = Path.GetFileNameWithoutExtension(myStringLists[d]);
                    return;
            }
        }
        #endregion

        #region 最小化至縮圖按鈕
        private void button4_Click(object sender, EventArgs e)
        {
            exeFormMin();
        }
        #endregion

        #region 重新啟動程式
        private void Restart()
        {
            Thread thtmp = new Thread(new ParameterizedThreadStart(Run));
            object appName = Application.ExecutablePath;
            Thread.Sleep(1000); 
            if (initt.變更模式 == "正常")
            {
                StreamWriter sw2 = new StreamWriter(TempPath);
                sw2.Write(intBuffer);
                sw2.Close();
            }
            thtmp.Start(appName);
        }

        private void Run(Object obj)
        {
            Process ps = new Process();
            ps.StartInfo.FileName = obj.ToString();
            ps.Start();
        }
        #endregion

        #region 視窗最小化
        private void exeFormMin()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
        }
        #endregion

        #region 視窗事件
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                this.notifyIcon1.Visible = true;
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {    
            if (initt.變更模式 == "正常")
            {
                StreamWriter sw2 = new StreamWriter(TempPath);
                sw2.Write(intBuffer);
                sw2.Close();
            }
        }
        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }
        #endregion


        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Interval = 1000;
            label1.Text = DateTime.Now.ToString();
        }

    }
}


///// <summary>
///// 產生亂數。
///// </summary>
///// <param name="start">起始數字。</param>
///// <param name="end">結束數字。</param>        
//static int[] GenerateRandom(int start, int end)
//{
//    int iLength = end - start + 1;
//    int[] arrList = new int[iLength];
//    for (int N1 = 0; N1 < iLength; N1++)
//    {
//        arrList[N1] = N1 + start;
//    }

//    arrList = Shuffle<int>(arrList);
//    return arrList;
//}
////洗牌。
//static T[] Shuffle<T>(IEnumerable<T> values)
//{
//    List<T> list = new List<T>(values);
//    T tmp;
//    int iS;
//    Random r = new Random();
//    for (int N1 = 0; N1 < list.Count; N1++)
//    {
//        iS = r.Next(N1, list.Count);
//        tmp = list[N1];
//        list[N1] = list[iS];
//        list[iS] = tmp;
//    }
//    return list.ToArray();
//}