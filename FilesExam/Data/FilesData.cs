using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FilesExam.Data
{
    public class FilesData
    {
       static Mutex mutex = new Mutex();
       public  static List<string> textInFiles = new List<string>();

        public static void ReadFiles(object obj)
        {
           string path = obj.ToString();
           mutex.WaitOne();
            foreach (var item in GetFiles(path))
            {
                try
                {
                    FileStream fs = new FileStream(item, FileMode.Open, FileAccess.Read);
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        textInFiles.Add(reader.ReadToEnd().ToLower());
                    }
                }
                catch{}
            }
            mutex.ReleaseMutex();
        }

       public static List<string> GetFiles(object obj)
        {
            string path = obj.ToString();
            mutex.WaitOne();
            List<string> files = new List<string>();
            try
            {
                string[] entries = Directory.GetFiles(path, "*.txt");

                foreach (string entry in entries)
                    files.Add(System.IO.Path.Combine(path, entry));
            }
            catch
            {

            }

            // follow the subdirectories
            try
            {
                string[] entries = Directory.GetDirectories(path);

                foreach (string entry in entries)
                {
                    string current_path = System.IO.Path.Combine(path, entry);
                    List<string> files_in_subdir = GetFiles(current_path);

                    foreach (string current_file in files_in_subdir)
                        files.Add(current_file);
                }
            }
            catch
            {
                // an exception in directory.getdirectories is not recoverable: the directory is not accessible
            }
            mutex.ReleaseMutex();
            return files;
        }



    }
}
