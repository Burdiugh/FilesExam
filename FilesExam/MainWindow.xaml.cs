using FilesExam.Data;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

namespace FilesExam
{

    public partial class MainWindow : Window
    {
        List<string> listWords = new List<string>();
        List<string> filesPathToCopy = new List<string>();
        List<string> foundSwearwords = new List<string>();

        string reportFileName;
        string path;
        Mutex mutex = new Mutex();
        int wordsCount = 0;
        public MainWindow()
        {

            if (IsRunningProcess(Process.GetCurrentProcess()))
            {
                MessageBox.Show("This process is already running!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
            else
            {
                InitializeComponent();
                btnStart.IsEnabled = false;
            }
            // cmd mode...
            this.Hide();
            InitListWordsForCmd();
            Processing();
            this.Close();
        }
        void InitListWordsForCmd()
        {
            listWords.Add("Fuck");
            listWords.Add("Sucker");
            listWords.Add("Bastard");
            listWords.Add("Shit");
            listWords.Add("Wanker");
            for (int i = 0; i < listWords.Count; i++)
            {
                listWords[i] = listWords[i].ToLower();
            }
        }
        bool IsRunningProcess(Process procces)
        {
            Process[] procceses = Process.GetProcessesByName(procces.ProcessName);
            if (procceses.Count() > 1)
            {
                return true;
            }
            return false;
        }
        void Update()
        {
            viewWordsList.ItemsSource = null;
            viewWordsList.ItemsSource = listWords;

        }
        void Processing()
        {
            path=@"D:\";
            ParameterizedThreadStart parameterized1 = new ParameterizedThreadStart(FilesData.ReadFiles);
            Thread thread3 = new Thread(parameterized1);
            FilesData.ReadFiles(path);
            filesPathToCopy = GetMatchedFiles(path);
            Thread thread1 = new Thread(CopyFilesToDesktop);
            thread3.Start(path);
            thread1.Start();
            SetStat();
        }
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            Processing();
        }
        void SetStat()
        {
            statusAllFilesCount.Content = FilesData.GetFiles(path).Count().ToString();
            statusMatchedFilesCount.Content = filesPathToCopy.Count().ToString();
            statusChangedWordsCount.Content = wordsCount;
            SetInfoToReport(reportFileName, FilesData.GetFiles(path).Count(), filesPathToCopy.Count(),wordsCount,InfoSwearword()); 
        }
        private void SendMail()
        {
            mutex.WaitOne();
            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
            mail.From = new MailAddress("justfortestmyexam@gmail.com");
            mail.To.Add("justfortestmyexam@gmail.com");
            mail.Subject = "Exam Files";
            mail.Body = "...";

            Attachment attachment;
            foreach (var file in filesPathToCopy)
            {
                attachment = new Attachment(file);
                mail.Attachments.Add(attachment);
            }
           
            SmtpServer.Port = 587;
            SmtpServer.Credentials = new System.Net.NetworkCredential("justfortestmyexam@gmail.com", "Exforfun12345");
            SmtpServer.EnableSsl = true;

            try
            {
                SmtpServer.Send(mail);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            
            mutex.ReleaseMutex();
        }
        private void btnUploadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.ShowDialog();
            string filePath = openFileDialog.FileName;
            if (filePath != null)
            {
                try
                {
                    var fileStream = openFileDialog.OpenFile();
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        string[] words = reader.ReadToEnd().Split(new char[] { ' ', ',', '.', '!', '?','\r','\n' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < words.Length; i++)
                        {
                            listWords.Add(words[i].ToLower());
                            Update();
                        }
                    }
                }
                catch
                {

                }
            }
        }
        private void btnAddWord_Click(object sender, RoutedEventArgs e)
        {
            string word = tbWords.Text;
            if (string.IsNullOrEmpty(word))
            {
                MessageBox.Show("Field can not be empty!");
            }
            else
            {
                listWords.Add(word.ToLower());
                tbWords.Text = "";
                Update();
            }
        }
        private void cmbDrives_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbDrives != null)
            {
                btnStart.IsEnabled = true;
                if (cmbDrives.SelectedIndex == 0)
                {
                    path = @"C:\";
                }
                else if (cmbDrives.SelectedIndex == 1)
                {
                    path = @"D:\";
                }
            }
        }
        private List<string> GetMatchedFiles(string path)
        {
            List<string> matchedFilesPath = new List<string>();
            foreach (string file in FilesData.GetFiles(path))
            {
                try
                {
                    FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string[] words = sr.ReadToEnd().ToLower().Split(new char[] { ' ', '.', ',', '!', '?', ':', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        if (words != null)
                        {
                            for (int i = 0; i < listWords.Count; i++)
                            {
                                foreach (var word in words)
                                {
                                    if (word == listWords[i].ToLower())
                                    {
                                        matchedFilesPath.Add(file);
                                        break;
                                    }
                                }
                            }

                        }
                    }
                }
                catch
                {
                    // ignore denied files
                }
            }
            return matchedFilesPath;
        }
        private void CopyToFolder(string targetPath)
        {
            mutex.WaitOne();
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
                foreach (string s in filesPathToCopy)
                {
                    string fileName = System.IO.Path.GetFileName(s);
                    string destFile = System.IO.Path.Combine(targetPath, fileName);
                    File.Copy(s, destFile, true);
                }
            }
            else
            {
                foreach (string s in filesPathToCopy)
                {
                    string fileName = System.IO.Path.GetFileName(s);
                    string destFile = System.IO.Path.Combine(targetPath, fileName);
                    File.Copy(s, destFile, true);
                }
            }
            mutex.ReleaseMutex();
        }
        void SetInfoToReport(string path, int allFiles, int machedFiles,int wordsCount,string wordsInfo)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var word in listWords)
            {
                stringBuilder.Append(word + "\n");
            }
            try
            {
                FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(
                        $"All files count: {allFiles}\n" +
                        $"Mached files count: {machedFiles}\n" +
                        $"Words which we tried to find:\n"+ stringBuilder+"\n"+
                        $"Words were changed: {wordsCount}\n" +
                        $"More info about words were found:\n{new string('-', 50)}{wordsInfo}" +
                        $"{new string('-', 50)}"
                        );
                    foreach (var file in filesPathToCopy)
                    {
                        sw.Write($"\n{file}\n");
                    }
                    sw.Write($"{new string('-', 50)}\nDate: {DateTime.Now}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        void ReplaceSwearwords(string path)
        {
            string line;
            using (StreamReader sr = new StreamReader(path))
            {
                line = sr.ReadToEnd().ToLower();
            }
            for (int i = 0; i < listWords.Count; i++)
            {
                if (line.Contains(listWords[i]))
                {
                    foundSwearwords.Add(listWords[i]);
                    line = Regex.Replace(line, listWords[i], "******");
                    wordsCount++;
                }
            }
            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.Write(line);
            }
        }
        private void CopyFilesToDesktop()
        {
            mutex.WaitOne();
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string targetPath = desktopPath + @"\copied files";
            string reportPath = System.IO.Path.Combine(targetPath, "Reports");
            reportFileName = System.IO.Path.Combine(reportPath, "report.txt");
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
                foreach (string s in filesPathToCopy)
                {
                    string fileName = System.IO.Path.GetFileName(s);
                    string destFile = System.IO.Path.Combine(targetPath, fileName);
                    try
                    {
                        File.Copy(s, destFile, true);
                        ReplaceSwearwords(destFile);
                    }
                    catch
                    {
                    }

                }
                Directory.CreateDirectory(reportPath);
                try
                {
                    SendMail();
                }
                catch
                {
                }
            }
            else
            {
                foreach (string s in filesPathToCopy)
                {
                    string fileName = System.IO.Path.GetFileName(s);
                    string destFile = System.IO.Path.Combine(targetPath, fileName);
                    try
                    {
                        File.Copy(s, destFile, true);
                        ReplaceSwearwords(destFile);
                    }
                    catch
                    {
                    }
                }
                try
                {
                    SendMail();
                }
                catch
                {
                }
            }
            mutex.ReleaseMutex();
        }
        private void viewWordsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (viewWordsList.SelectedIndex!=-1)
            {
                listWords.Remove(viewWordsList.SelectedItem.ToString());
                Update();
            }
        }
        string InfoSwearword()
        {
            StringBuilder stringBuilder = new StringBuilder();
            var q = foundSwearwords.GroupBy(x => x)
       .Select(g => new { Value = g.Key, Count = g.Count() })
       .OrderByDescending(x => x.Count);

            foreach (var x in q)
            {
                stringBuilder.Append("\n Word: " + x.Value + " Count: " + x.Count+"\n");
            }
            return stringBuilder.ToString();
        }
    }
}
