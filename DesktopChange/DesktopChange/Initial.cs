using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace DesktopChange
{
    class Initial
    {
        public string 變更桌布時間 { get; set; }
        public string 桌布路徑 { get; set; }
        public string 變更模式 { get; set; }

        public void Init()
        {
            try
            {
                string CheckStr;
                StreamReader sr = new StreamReader(@"C:\DesktopSetup\Setup.txt", Encoding.Default);
                while ((CheckStr = sr.ReadLine()) != null)
                {
                    string[] CheckString = CheckStr.Split('=');
                    switch (CheckString[0])
                    {
                        case "變更桌布時間":
                            變更桌布時間 = CheckString[1];
                            break;
                        case "桌布路徑":
                            桌布路徑 = CheckString[1];
                            break;
                        case "變更模式":
                            變更模式 = CheckString[1];
                            break;
                    }
                }
                sr.Close();
                //return true;
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
                MessageBox.Show("讀取不到設定檔");
                //return false;
            }
        }

    }
}
