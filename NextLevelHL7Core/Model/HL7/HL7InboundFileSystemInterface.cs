using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace NextLevelHL7
{
    public class HL7InboundFileSystemInterface : BaseHL7Interface
    {
        public TimeSpan FileSystemScanInterval { get; set; } = TimeSpan.FromSeconds(30);
        private string _FilePath;
        private string _Extension;
        private bool _Cancelled = false;

        public HL7InboundFileSystemInterface(string name, string filePath, string extension)
        {
            Name = name;
            _FilePath = filePath;
            _Extension = extension;
        }

        protected override bool OnStart()
        {
            _Cancelled = false;

            if (string.IsNullOrEmpty(_FilePath) || string.IsNullOrEmpty(_Extension) && Directory.Exists(_FilePath))
            {
                WriteStatus("Unable to scan file system, '" + _FilePath + "'");
                return false;
            }

            WriteStatus("File system scanning initiated at " + _FilePath);
            
            while (!_Cancelled)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(_FilePath);
                List<FileInfo> files = new List<FileInfo>(directoryInfo.GetFiles("*." + _Extension, SearchOption.TopDirectoryOnly));
                files = files.OrderBy(a => a.Name).ToList();

                foreach (FileInfo file in files)
                {
                    WriteStatus(files.Count + " ." + _Extension + " files found");
                    try
                    {
                        using (StreamReader reader = file.OpenText())
                        {
                            string text = reader.ReadToEnd();

                            byte startCharacter = 0x0B;
                            byte endCharacter = 0x1C;

                            string[] tokens = text.Split(new char[] { (char)startCharacter, (char)endCharacter },
                                StringSplitOptions.RemoveEmptyEntries);

                            foreach (string token in tokens)
                                if (!string.IsNullOrWhiteSpace(token))
                                {
                                    Message message = ParseMessage(token);
                                    WriteMessage(message);
                                }
                        }

                        try
                        {
                            file.MoveTo(file.FullName + ".processed");
                        }
                        catch (Exception e)
                        {
                            WriteError(e);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex);
                    }
                }

                Thread.Sleep(FileSystemScanInterval);
            }

            return true;
        }

        protected override bool OnStop()
        {
            _Cancelled = true;
            return true;
        }
    }
}
