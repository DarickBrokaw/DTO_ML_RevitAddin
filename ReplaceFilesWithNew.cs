// This program reads the latest version of a Revit add-in from a config file, downloads the
// corresponding zip file, extracts it, and replaces the existing add-in files using Robocopy.

// Import necessary namespaces
using System;                   // Core functionality
using System.Diagnostics;       // Process handling
using System.IO;                // File handling
using System.IO.Compression;    // Zip archive handling
using System.Xml;               // XML parsing

namespace ReplaceFilesWithNew
{
    /// <summary>
    /// The main program class to update a Revit add-in.
    /// </summary>
    class RevitAddinUpdater
    {
        /// <summary>
        /// The main entry point of the program.
        /// </summary>
        public static void UpdateRevitAddin()
        {
            // Initialize the latest version variable
            string latestVersion = "";

            // Get the path of the add-in DLL
            string addinPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            // Combine the add-in directory and config file name to create the config file path
            string configFilePath = Path.Combine(Path.GetDirectoryName(addinPath), "DTO.dll.config");

            // Create an XML document object and load the config file
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(configFilePath);

            // Find the appSettings node in the XML document
            XmlNode appSettingsNode = xmlDoc.SelectSingleNode("configuration/appSettings");

            // Check if the appSettings node exists
            if (appSettingsNode != null)
            {
                // Iterate through child nodes of appSettings node
                foreach (XmlNode childNode in appSettingsNode.ChildNodes)
                {
                    // Check if the child node has the "latestversion" key
                    if (childNode.Attributes["key"]?.Value == "latestversion")
                    {
                        // Store the latest version value from the child node
                        latestVersion = childNode.Attributes["value"]?.Value;
                        break;
                    }
                }
            }

            // Combine the user's downloads directory and zip file name to create the zip file path
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string zipFileName = $"DTO_ML_RevitAddin-{latestVersion}.zip";
            string zipPath = Path.Combine(downloadsPath, zipFileName);

            // Set the extraction path as a subdirectory "extracted_files" in the zip file directory
            string extractPath = Path.Combine(Path.GetDirectoryName(zipPath), "extracted_files");

            // Get all the files in the extraction path and its subdirectories
            string[] files = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories);

            // Move each file to the root folder and delete the subdirectory folders
            foreach (string file in files)
            {
                // Get the file name without the directory path
                string fileName = Path.GetFileName(file);

                // Construct the destination file path in the root folder
                string destinationFilePath = Path.Combine(extractPath, fileName);

                // Move the file to the destination file path, overwriting if necessary
                File.Move(file, destinationFilePath);
            }

            // Delete all the subdirectories in the extraction path
            Directory.Delete(extractPath, true);

            // Set the source and destination paths for Robocopy
            string sourcePath = extractPath;
            string destinationPath = Path.GetDirectoryName(configFilePath);

            // Create a new Robocopy process with the necessary settings
            Process robocopyProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "robocopy.exe",             // Robocopy executable
                    Arguments = $@"{sourcePath} {destinationPath} /MOVE",  // Robocopy arguments
                    RedirectStandardOutput = true,        // Redirect standard output
                    UseShellExecute = false,              // Do not use shell execute
                    CreateNoWindow = true,                // No window for process
                }
            };

            // Start the Robocopy process
            robocopyProcess.Start();

            // Read the process output
            string output = robocopyProcess.StandardOutput.ReadToEnd();

            // Wait for the process to exit
            robocopyProcess.WaitForExit();


            // Delete the zip file after the process has finished
            File.Delete(zipPath);
        }
    }
}