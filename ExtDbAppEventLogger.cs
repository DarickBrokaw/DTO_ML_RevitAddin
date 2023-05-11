/// <summary>
/// This is a simple Revit add-in to log Revit events to a text file.
/// </summary>
/// <remarks>
/// This is a simple Revit add-in to log Revit events to a text file.
/// The log file is located in the same folder as the add-in.
/// The log file name is specified in the App.config file.
/// The log file is appended to each time the add-in is loaded.
/// The log file is comma delimited.
/// The log file contains the following fields: Date, User, Event, Version Build, Version Number, Product, Document Path Name (if applicable)
/// The following events are logged: Document Created, Document Opened, Document Closing (when the document is saved or closed)
/// </remarks>
/// <author>Darick Brokaw</author>
/// <date>2023</date>
/// <license>TBD</license>
/// <version>1.0</version>

// Required namespaces
using System; // For EventArgs
using System.IO; // For StreamWriter
using System.Windows.Forms; // For MessageBox
using System.Configuration; // For ConfigurationManager
using Autodesk.Revit.ApplicationServices;  // For ControlledApplication
using Autodesk.Revit.Attributes; // For Transaction, Regeneration
using Autodesk.Revit.DB; // For ExternalDBApplicationResult
using Autodesk.Revit.DB.Events; // For DocumentCreatedEventArgs, DocumentOpenedEventArgs, DocumentClosingEventArgs
using GitHubConnect; // For GitHubReleaseChecker
using System.Threading.Tasks; // For Task async await latestVersion
using static GitHubConnect.GitHubReleaseChecker;
using System.Xml.Linq;
using ComputeOptimization;
using FileManagement;
using System.Diagnostics;

/// <summary>
/// The EventLogger namespace is responsible for logging events related to Revit documents
/// such as document creation, opening, and closing.
/// </summary>
namespace EventLogger // Namespace must match the folder name
{
    /// <summary>
    /// ExtDbAppEventLogger class implements IExternalDBApplication and provides logging functionality
    /// for various Revit document events.
    /// </summary>
    [Transaction(TransactionMode.Manual)] // This add-in does not create or modify Revit elements
    [Regeneration(RegenerationOption.Manual)] // This add-in does not create or modify Revit elements
    public class ExtDbAppEventLogger : IExternalDBApplication
    {
        #region Cached Variables

        public static ControlledApplication _cachedCtrlApp; // Cached ControlledApplication
        String LogFilePath = ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location).AppSettings.Settings["LogFilePath"].Value; // Log file path

        #endregion

        #region IExternalApplication Members

        public ExternalDBApplicationResult OnStartup(ControlledApplication ctrlApp) // IExternalApplication.OnStartup
        {
            try // Catch any exceptions
            {
                ComputeOptimization.Program.Main(null); // Add this line to run ComputeOptimization

                _cachedCtrlApp = ctrlApp; // Cache the ControlledApplication

                _cachedCtrlApp.DocumentCreated += new EventHandler<DocumentCreatedEventArgs>(CachedCtrlApp_DocumentCreated); // Subscribe to the DocumentCreated event
                _cachedCtrlApp.DocumentOpened += new EventHandler<DocumentOpenedEventArgs>(CachedCtrlApp_DocumentOpened); // Subscribe to the DocumentOpened event
                _cachedCtrlApp.DocumentClosing += new EventHandler<DocumentClosingEventArgs>(CachedCtrlApp_DocumentClosing); // Subscribe to the DocumentClosing event

                return ExternalDBApplicationResult.Succeeded; // Return success
            }
            catch (Exception ex) // Catch any exceptions
            {
                MessageBox.Show(ex.ToString()); // Display the exception
                return ExternalDBApplicationResult.Failed; // Return failure
            }
        }

        void CachedCtrlApp_DocumentClosing(object sender, DocumentClosingEventArgs e) // DocumentClosing event handler
        {
            string vb = _cachedCtrlApp.VersionBuild.ToString(); // Get the version build
            string vn = _cachedCtrlApp.VersionNumber.ToString(); // Get the version number
            string pro = _cachedCtrlApp.Product.ToString(); // Get the product
            string docPath = e.Document.PathName; // Get the document path name

            if (string.IsNullOrEmpty(docPath)) // If the document path name is empty
            {
                docPath = "NotSaved"; // Set the document path name to "NotSaved"
            }

            using (StreamWriter sw = new StreamWriter(LogFilePath, true)) // Open the log file for appending
            {
                sw.WriteLine($"{DateTime.Now}, {Environment.UserName}, Closing, {vb}, {vn}, {pro}, {docPath}"); // Write the log entry
            }
        }

        void CachedCtrlApp_DocumentOpened(object sender, DocumentOpenedEventArgs e)
        {
            string vb = _cachedCtrlApp.VersionBuild.ToString();
            string vn = _cachedCtrlApp.VersionNumber.ToString();
            string pro = _cachedCtrlApp.Product.ToString();
            string docPath = e.Document.PathName;

            using (StreamWriter sw = new StreamWriter(LogFilePath, true))
            {
                sw.WriteLine($"{DateTime.Now}, {Environment.UserName}, Opened, {vb}, {vn}, {pro}, {docPath}");
            }
        }

        void CachedCtrlApp_DocumentCreated(object sender, DocumentCreatedEventArgs e)
        {
            string vb = _cachedCtrlApp.VersionBuild.ToString();
            string vn = _cachedCtrlApp.VersionNumber.ToString();
            string pro = _cachedCtrlApp.Product.ToString();
            // No PathName for new documents

            using (StreamWriter sw = new StreamWriter(LogFilePath, true))
            {
                sw.WriteLine($"{DateTime.Now}, {Environment.UserName}, Created, {vb}, {vn}, {pro}, NewNoPath");
            }
        }

        //public async Task<ExternalDBApplicationResult> OnShutdown(ControlledApplication ctlApp)
        public ExternalDBApplicationResult OnShutdown(ControlledApplication ctlApp)
        {
            try
            {
                //Start new code for GitHubConnect
                var checker = new GitHubConnect.GitHubReleaseChecker();
                var owner = "DarickBrokaw";
                var repoName = "DTO_ML_RevitAddin";
                //var latestVersion = await checker.GetLatestVersionAsync(owner, repoName);
                var task = Task.Run(() => checker.GetLatestVersionAsync(owner, repoName));
                GitHubRelease latestVersion = task.Result;
                // Log the latest version information to a file
                using (StreamWriter sw = new StreamWriter(LogFilePath, true))
                {
                    sw.WriteLine($"{DateTime.Now}, {Environment.UserName}, OnShutdown, GitHubReleaseLatestVersion, {latestVersion.TagName}");
                }

                // Read RvtAddinInstalledVersion key from DTO.dll.config
                var config = ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string installedVersion = config.AppSettings.Settings["RvtAddinInstalledVersion"].Value;

                // Compare installed version with latest version and download release assets if they don't match
                if (installedVersion != latestVersion.TagName)
                {
                    string downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"{repoName}-{latestVersion.TagName}");
                    string zipFilePath = Path.Combine(downloadFolder, $"{latestVersion.TagName}.zip");
                    Task.Run(() => checker.DownloadReleaseAssetsAsync(latestVersion,  downloadFolder)).Wait();
                    string destinationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "Revit", "Addins", "2023");
                    

                    //// Update RvtAddinInstalledVersion value in DTO.dll.config to latestVersion.TagName
                    //config.AppSettings.Settings["RvtAddinInstalledVersion"].Value = latestVersion.TagName;
                    //config.AppSettings.Settings["DownloadFolderPath"].Value = downloadFolder;
                    //config.AppSettings.Settings["ZipFilePath"].Value = zipFilePath;
                    //config.AppSettings.Settings["DestinationPath"].Value = destinationPath;
                    //config.Save(ConfigurationSaveMode.Modified);
                    //ConfigurationManager.RefreshSection("appSettings");

                    //FileUtils.Main(downloadFolder, zipFilePath, destinationPath);

                    // The path to the console application
                    string fileManagmentConsoleAppPath = Path.Combine(destinationPath, "DTOFileManager.exe");

                    // Create a new process start info object
                    ProcessStartInfo startInfo = new ProcessStartInfo(fileManagmentConsoleAppPath);

                    // Set any arguments that you want to pass to the console application
                    string latestVersionTagName = latestVersion.TagName;
                    startInfo.Arguments = $"{downloadFolder} {zipFilePath} {destinationPath} {latestVersionTagName}";

                    // Set any options for how the console application should be started
                    startInfo.CreateNoWindow = true;
                    startInfo.UseShellExecute = false;

                    // Start the process and wait for it to exit
                    using (Process process = Process.Start(startInfo))
                    {
                    }

                }

                //End new code for GitHubConnect

                return ExternalDBApplicationResult.Succeeded;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return ExternalDBApplicationResult.Failed;
            }
        }

        #endregion       
    }
}