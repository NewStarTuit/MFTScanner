
using MFTScanner;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MFTReader
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            foreach (var file in MftScanner.ScanFolder(@"C:\Users\Men\Downloads\Telegram Desktop", new[] { "pdf", "zip", "docx" }).OrderBy(t=>t.FullPath))
            {
                FileInfo fileInfo = new FileInfo(file.FullPath);
                if (fileInfo.Exists)
                {
                    Console.WriteLine(fileInfo.CreationTime.ToString() + " " + file.FullPath);
                }else
                    Console.WriteLine("--------------------- DELETED "  + file.FullPath);
            }

        }
    }
}
