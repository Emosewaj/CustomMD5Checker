#pragma warning disable IDE0044

using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace CustomMD5Checker
{
    class Program
    {
        static string exeName = AppDomain.CurrentDomain.FriendlyName;
        static string[] fileList;
        static List<HashEntry> hashList;

        static ResultStore Results = new ResultStore();

        /**
         * ##############################
         *              MAIN             
         * ##############################
         * */

        static void Main(string[] args)
        {
            // Abort if no file dropped onto exe
            if (args.Length == 0)
                Exit($"Drop an .md5 file onto {exeName} to start checking!");

            // Set current working directory to the path of the exe instead of the md5 file
            // https://stackoverflow.com/questions/837488/how-can-i-get-the-applications-path-in-a-net-console-application
            Directory.SetCurrentDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));

            Results.AddNote($"Game directory: {Directory.GetCurrentDirectory()}");
            Results.AddNote($"Hashfile: {args[0]}");

            // Fetch all files in the current dir
            Console.Write("Fetching files...");
            fileList = Directory.GetFiles(".", "*.*", SearchOption.AllDirectories);
            Console.WriteLine("Done");
            Console.Write("Collecting information...");
            {
                var folderSize = DirSize(new DirectoryInfo(Directory.GetCurrentDirectory()));
                Results.AddNote("");
                Results.AddNote($"Dir size: {folderSize.ToString("N2")} B");
                Results.AddNote($"Dir size: {(folderSize / 1024.0).ToString("N2")} KB");
                Results.AddNote($"Dir size: {(folderSize / 1024.0 / 1024.0).ToString("N2")} MB");
                Results.AddNote($"Dir size: {(folderSize / 1024.0 / 1024.0 / 1024.0).ToString("N2")} GB");
                Results.AddNote("");

                var freeDiskSpace = GetTotalFreeSpace(Directory.GetCurrentDirectory().Substring(0, 3));
                Results.AddNote($"Free disk space: {freeDiskSpace.ToString("N2")} B");
                Results.AddNote($"Free disk space: {(freeDiskSpace / 1024.0).ToString("N2")} KB");
                Results.AddNote($"Free disk space: {(freeDiskSpace / 1024.0 / 1024.0).ToString("N2")} MB");
                Results.AddNote($"Free disk space: {(freeDiskSpace / 1024.0 / 1024.0 / 1024.0).ToString("N2")} GB");
                Results.AddNote($"Free disk space: {(freeDiskSpace / 1024.0 / 1024.0 / 1024.0 / 1024.0).ToString("N2")} TB");
                Results.AddNote("");

                Results.AddNote($"OS: {FriendlyName()}");
            }

            Results.AddNote($"File count: {fileList.Length}");

            try
            {
                // Read and parse the supplied file
                Console.Write("Reading hash list...");
                string[] lines = File.ReadAllLines(args[0]);
                Console.WriteLine("Done");
                hashList = new List<HashEntry>();
                var lineNum = 0;
                Console.Write("Parsing hashes");
                foreach(string line in lines)
                {
                    lineNum++;
                    if (Math.Floor((float)lineNum / (float)lines.Length * 10.0f) != Math.Floor((float)(lineNum - 1) / (float)lines.Length * 10.0f))
                        Console.Write(".");
                    if (line == "")
                        continue;

                    var contents = Regex.Match(line, @"^([0-9a-f]{32}) \*(.+)$");
                    if (!contents.Success)
                        Exit($"Something seems to be wrong with the md5 file!\nLine {lineNum}: {line}");
                    var hash = contents.Groups[1].ToString();
                    var path = @".\" + contents.Groups[2];

                    hashList.Add(new HashEntry(path, hash));
                }
                Console.WriteLine("Done");
            }
            catch (Exception e)
            {
                Exit(e.Message);
            }

            Console.WriteLine($"Read {hashList.Count} MD5 entries...");

            Console.WriteLine("Scanning for missing files...");
            // Find possible missing files
            foreach(HashEntry entry in hashList)
                if (!File.Exists(entry.Path))
                    Results.AddNA(entry.Path);

            Console.WriteLine("Checking files...");
            // Loop through files
            int i = 0;
            foreach (string file in fileList)
            {
                i++;
                var entry = FindEntryByPath(file);
                Console.Write($"{i}/{fileList.Length} {file} - ");
                if (entry == null)
                {
                    Results.AddUnknown(file);
                    Console.WriteLine("?");
                    continue;
                }

                var md5 = CalculateMD5(file);
                Console.Write($" - {md5} ");

                if (md5 == entry.Hash)
                {
                    Results.AddPassed(file);
                    Console.Write("OK");
                }
                else
                {
                    Results.AddFailed(file);
                    Console.Write("FAILED");
                }

                Console.WriteLine();
            }

            List<string>[] final = Results.GetAll();
            Console.WriteLine("\n##############################\n");
            Console.WriteLine("Results:\n");
            Console.WriteLine($"Passed:  {final[3].Count}");
            Console.WriteLine($"Failed:  {final[2].Count}");
            Console.WriteLine($"Unknown: {final[1].Count}");
            Console.WriteLine($"N/A:     {final[0].Count}");

            Results.AddNote($"Passed: {final[3].Count}");
            Results.AddNote($"Failed: {final[2].Count}");
            Results.AddNote($"Unknown: {final[1].Count}");
            Results.AddNote($"N/A: {final[0].Count}");

            Results.AddNote($"DX9 installed: {File.Exists(Environment.SystemDirectory + "\\d3d9.dll")}");
            Results.AddNote($"D3DX9_40 available: {File.Exists(Environment.SystemDirectory + "\\d3dx9_40.dll")}");

            Console.WriteLine("\nDone!");
            Console.Write("Writing results to files");

            Directory.CreateDirectory("./MD5RESULTS");
            File.WriteAllText("./MD5RESULTS/NA.log", "N/A:\n\n" + Results.NAToString() + "\nEOF");
            Console.Write(".");
            File.WriteAllText("./MD5RESULTS/Unknown.log", "Unknown:\n\n" + Results.UnknownToString() + "\nEOF");
            Console.Write(".");
            File.WriteAllText("./MD5RESULTS/Failed.log", "Failed:\n\n" + Results.FailedToString() + "\nEOF");
            Console.Write(".");
            File.WriteAllText("./MD5RESULTS/Passed.log", "Passed:\n\n" + Results.PassedToString() + "\nEOF");
            Console.Write(".");
            File.WriteAllText("./MD5RESULTS/Notes.log", "Notes:\n\n" + Results.NotesToString() + "\nEOF");
            Console.WriteLine(".Done!");

            Console.Write("Zipping up...");
            if (File.Exists("./RESULTS.zip"))
                File.Delete("./RESULTS.zip");
            ZipFile.CreateFromDirectory("./MD5RESULTS", "./RESULTS.zip");
            Directory.Delete("./MD5RESULTS", true);
            Console.WriteLine(" Done!");
            if (final[2].Count > 0 || final[0].Count > 0)
                Console.WriteLine("\nYou are missing or have corrupted files, send RESULT.zip to tech support!\n");
            Console.WriteLine("You can now close this window.");
            Console.ReadKey();
        }

        /**
         * ##############################
         *         HELPER METHODS        
         * ##############################
         * */
        
        // https://stackoverflow.com/questions/2124468/possible-to-calculate-md5-or-other-hash-with-buffered-reads/2124500#2124500
        static string CalculateMD5(string path)
        {
            MD5 md5 = MD5.Create();
            FileStream file = File.OpenRead(path);

            // Quickfix for empty files, wtf even tho
            if (file.Length == 0)
                return "d41d8cd98f00b204e9800998ecf8427e";

            int size = file.Length >= 10 ? (int)(file.Length / 10) : (int)file.Length;
            byte[] block = new byte[size];
            long bytesRead = 0;

            while (file.Length - bytesRead >= size)
            {
                bytesRead += file.Read(block, 0, size);
                md5.TransformBlock(block, 0, size, block, 0);
                Console.Write(".");
            }

            file.Read(block, 0, (int)(file.Length - bytesRead));
            md5.TransformFinalBlock(block, 0, (int)(file.Length - bytesRead));

            file.Close();
            file.Dispose();
            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLowerInvariant();
        }

        static long DirSize(DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += DirSize(di);
            }
            return size;
        }

        static void Exit(string text)
        {
            Console.WriteLine(text + "\n");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(0);
        }

        static HashEntry FindEntryByHash(string hash)
        {
            if (hash == null)
                throw new ArgumentNullException("hash");

            for(int i = 0; i < hashList.Count; i++)
            {
                if (hash == hashList[i].Hash)
                    return hashList[i];
            }

            return null;
        }

        static HashEntry FindEntryByPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            for(int i = 0; i < hashList.Count; i++)
            {
                if (path == hashList[i].Path)
                    return hashList[i];
            }

            return null;
        }

        // https://stackoverflow.com/questions/577634/how-to-get-the-friendly-os-version-name
        static string FriendlyName()
        {
            string ProductName = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
            string CSDVersion = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CSDVersion");
            if (ProductName != "")
            {
                return (ProductName.StartsWith("Microsoft") ? "" : "Microsoft ") + ProductName +
                            (CSDVersion != "" ? " " + CSDVersion : "");
            }
            return "";
        }

        static long GetTotalFreeSpace(string driveName)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name == driveName)
                {
                    return drive.TotalFreeSpace;
                }
            }
            return -1;
        }

        static string HKLM_GetString(string path, string key)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(path);
                if (rk == null) return "";
                return (string)rk.GetValue(key);
            }
            catch { return ""; }
        }

        /**
         * ##############################
         *              EOF              
         * ##############################
         * */
    }
}
