using System;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;

namespace FileManagement
{
    class Program
    {
        static void Main(string[] args)
        {
            string downloadFolder = args[0];
            string zipFilePath = args[1];
            string destinationPath = args[2];

            FileUtils.ExtractAndMoveFiles(downloadFolder, zipFilePath, destinationPath);
        }
    }
    static class FileUtils
    {
        public static void ExtractAndMoveFiles(string downloadFolder, string zipFilePath, string destinationPath)
        {
            // Extract the zip file to a temporary folder
            ZipFile.ExtractToDirectory(zipFilePath, downloadFolder);

            // Move all files in subdirectories to the root folder
            foreach (string subDir in Directory.GetDirectories(downloadFolder))
            {
                foreach (string file in Directory.GetFiles(subDir))
                {
                    string extension = Path.GetExtension(file);
                    if (extension == ".zip")
                    {
                        // Skip files with .zip extension
                        continue;
                    }
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(downloadFolder, fileName);
                    File.Move(file, destFile);
                }
            }

            // Delete all subdirectories
            foreach (string subDir in Directory.GetDirectories(downloadFolder))
            {
                Directory.Delete(subDir, true);
            }

            foreach (string file in Directory.GetFiles(downloadFolder))
            {
                string extension = Path.GetExtension(file);
                if (extension == ".zip")
                {
                    // Delete files with .zip extension
                    File.Delete(file);
                }
            }


            // Copy all files in the root folder to the destination path using robocopy
            string robocopyArgs = $@"""{downloadFolder}"" ""{destinationPath}"" /E /MOVE /XD *";
            ProcessStartInfo startInfo = new ProcessStartInfo("robocopy", robocopyArgs);
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            Process process = Process.Start(startInfo);
            process.WaitForExit();

            // Delete the temporary folder
            //Directory.Delete(downloadFolder, true);
        }
    }
}

