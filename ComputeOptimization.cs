using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading.Tasks;

namespace ComputeOptimization
{
    class Program
    {
        public static void Main(string[] args)
        {
            FindRevitProcessesAndSubprocesses();
            ////See class/ function below for more details
            ////string revitIniPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Autodesk\Revit\Autodesk Revit 2023\Revit.ini");
            ////RevitIniModifier.DisableSplashScreen(revitIniPath);
        }

        static void FindRevitProcessesAndSubprocesses()
        {
            HashSet<string> revitProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Revit", "RevitWorker", "RevitAccelerator" };
            ConcurrentDictionary<int, Process> revitProcessDict = new ConcurrentDictionary<int, Process>();

            try
            {
                Process[] processes = Process.GetProcesses();
                Parallel.ForEach(processes, process =>
                {
                    Process parentProcess = process.Parent();
                    if (revitProcesses.Contains(process.ProcessName))
                    {
                        revitProcessDict.TryAdd(process.Id, process);
                        try
                        {
                            process.PriorityClass = ProcessPriorityClass.RealTime;
                        }
                        catch (Exception)
                        {
                            // Handle exceptions silently or log them if required
                        }
                    }
                    else if (parentProcess != null && revitProcessDict.ContainsKey(parentProcess.Id))
                    {
                        try
                        {
                            process.PriorityClass = ProcessPriorityClass.RealTime;
                        }
                        catch (Exception)
                        {
                            // Handle exceptions silently or log them if required
                        }
                    }
                });
            }
            catch (Exception)
            {
                // Handle exceptions silently or log them if required
            }
        }
    }

    public static class ProcessExtensions
    {
        public static Process Parent(this Process process)
        {
            try
            {
                using (var query = new ManagementObjectSearcher(
                    "SELECT * " +
                    "FROM Win32_Process " +
                    "WHERE ProcessId=" + process.Id))
                {
                    using (var results = query.Get().GetEnumerator())
                    {
                        if (results.MoveNext())
                        {
                            var parentId = (uint)results.Current["ParentProcessId"];
                            return Process.GetProcessById((int)parentId);
                        }
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }
    }

    // Revit Desktop Icon shortcut command line switch /nosplash works, but the revit.ini file code below does not work.
    //public static class RevitIniModifier
    //{
    //    public static void DisableSplashScreen(string revitIniPath)
    //    {
    //        if (File.Exists(revitIniPath))
    //        {
    //            var lines = File.ReadAllLines(revitIniPath);
    //            bool splashScreenFound = false;

    //            for (int i = 0; i < lines.Length; i++)
    //            {
    //                if (lines[i].Trim().StartsWith("ShowSplash", StringComparison.OrdinalIgnoreCase))
    //                {
    //                    lines[i] = "ShowSplash=0";
    //                    splashScreenFound = true;
    //                    break;
    //                }
    //            }

    //            if (!splashScreenFound)
    //            {
    //                Array.Resize(ref lines, lines.Length + 1);
    //                lines[lines.Length - 1] = "ShowSplash=0";
    //            }

    //            File.WriteAllLines(revitIniPath, lines);
    //        }
    //        else
    //        {
    //            Console.WriteLine("Revit.ini file not found.");
    //        }
    //    }
    //}
}
