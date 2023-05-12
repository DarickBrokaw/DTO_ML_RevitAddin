using System;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;
using System.Configuration; // For ConfigurationManager
using System.Threading.Tasks;

namespace FileManagement
{
    class Program
    {
        static void Main(string[] args)
        {
            string downloadFolder = args[0];
            string zipFilePath = args[1];
            string destinationPath = args[2];
            string operation = args[3]; // Add the operation argument
            string logFilePath = args[4]; // Add the LogFilePath argument

            if (operation == "ExtractAndMoveFiles")
            {
                FileUtils.ExtractAndMoveFiles(downloadFolder, zipFilePath, destinationPath);
            }
            else if (operation == "CopyAndDelete")
            {
                FileOperations.CopyAndDelete(downloadFolder, destinationPath, logFilePath);
            }
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
                string extension = Path.GetFullPath(file);
                if (extension.Contains("zip"))
                {
                    // Delete files with .zip extension
                    File.Delete(file);
                }
            }
        }
    }

    static class FileOperations
    {
        public static void CopyAndDelete(string downloadFolder, string destinationPath, string logFilePath)
        {
            // Get all files in the root folder
            var files = Directory.GetFiles(downloadFolder);

            // Copy all files in the root folder to the destination path in parallel
            Parallel.ForEach(files, file =>
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destinationPath, fileName);
                File.Copy(file, destFile, true);
            });

            // Delete the temporary folder
            Directory.Delete(downloadFolder, true);

            // Check for DTOPostAction (.bat, .py, .exe) and run it silently if found
            string[] extensions = { ".bat", ".py", ".exe" };
            foreach (string extension in extensions)
            {
                string dtopostactionFile = Path.Combine(logFilePath, $"DTOPostAction{extension}");
                if (File.Exists(dtopostactionFile))
                {
                    ProcessStartInfo psi = new ProcessStartInfo(dtopostactionFile)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    Process postActionProcess = Process.Start(psi);
                    break;
                }
            }
        }
    }
}
