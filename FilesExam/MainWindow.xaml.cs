﻿using FilesExam.Data;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
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
        string path;
        Mutex mutex = new Mutex();
        public MainWindow()
        {

            if (IsRunningProcess(Process.GetCurrentProcess()))
            {
                MessageBox.Show("This process is already running!","Error",MessageBoxButton.OK,MessageBoxImage.Error);
                this.Close();
            }
            else
            {
                InitializeComponent();
                btnStart.IsEnabled = false;
            }   
        }
        bool IsRunningProcess(Process procces)
        {
            Process[] procceses = Process.GetProcessesByName(procces.ProcessName);
            if (procceses.Count()>1)
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
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            ParameterizedThreadStart parameterized1 = new ParameterizedThreadStart(FilesData.ReadFiles);
            Thread thread3 = new Thread(parameterized1);

            statusAllFilesCount.Content = FilesData.GetFiles(path).Count().ToString();
            FilesData.ReadFiles(path);
            filesPathToCopy = GetMatchedFiles(path);
            statusMatchedFilesCount.Content = filesPathToCopy.Count().ToString();
            Thread thread1 = new Thread(CopyToFilesToDesktop);
            thread3.Start(path);
            thread1.Start();
            
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
            //SmtpServer.UseDefaultCredentials = true;
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


            //using (MailMessage mail = new MailMessage())
            //{
            //    mail.From = new MailAddress("justfortestmyexam@gmail.com");
            //    mail.To.Add("justfortestmyexam@gmail.com");
            //    mail.Subject = "Hello World";
            //    mail.Body = "<h1>Hello</h1>";
            //    mail.IsBodyHtml = true;
            //    //foreach (var file in filesPathToCopy)
            //    //{
            //    //    mail.Attachments.Add(new Attachment(file));
            //    //}
            //    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 465))
            //    {
            //        //smtp.UseDefaultCredentials = true;
            //        smtp.Credentials = new NetworkCredential("justfortestmyexam@gmail.com", "Exforfun12345");
            //        smtp.EnableSsl = true;
            //        try
            //        {
            //            smtp.Send(mail);
            //        }
            //        catch (Exception ex)
            //        {
            //            MessageBox.Show(ex.Message);
            //        }
            //    }
            //}
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
                        string[] words = reader.ReadToEnd().Split(' ');
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
                Update();
            }
        }
        private void cmbDrives_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbDrives!=null)
            {
                btnStart.IsEnabled = true;
                if (cmbDrives.SelectedIndex==0)
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
                        string line = sr.ReadToEnd().ToLower();
                        if (line != null)
                        {
                            for (int i = 0; i < listWords.Count; i++)
                            {
                                if (line.Contains(listWords[i].ToLower()))
                                {
                                    matchedFilesPath.Add(file);
                                    break;
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
        private void CopyToFilesToDesktop()
        { 
            mutex.WaitOne();
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string targetPath = desktopPath + @"\copied files";
            string reportPath = targetPath + @"\Reports";
            
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
                foreach (string s in filesPathToCopy)
                {
                    string fileName = System.IO.Path.GetFileName(s);
                    string destFile = System.IO.Path.Combine(targetPath, fileName);
                    File.Copy(s, destFile, true);
                }
                SendMail();
            }
            else
            {
                foreach (string s in filesPathToCopy)
                {
                    string fileName = System.IO.Path.GetFileName(s);
                    string destFile = System.IO.Path.Combine(targetPath, fileName);
                    File.Copy(s, destFile, true);
                }
                SendMail();
            }
            mutex.ReleaseMutex();
        }


    }
}
