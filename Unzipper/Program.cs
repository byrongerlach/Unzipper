using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Unzipper
{
    class Program
    {
        public static void Main(params string[] args)
        {
            CommandLineApplication commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);

            CommandOption sourcePathOption = commandLineApplication.Option(
            "-$|-s |--source <source>",
            "The source path containing the zip files to unzip.",
            CommandOptionType.SingleValue);

            CommandOption destPathOption = commandLineApplication.Option(
            "-$|-d |--destination <destination>",
            "The destination where the zip files will be unzipped",
            CommandOptionType.SingleValue);

            CommandOption forceOverwriteOption = commandLineApplication.Option(
            "-$|-f |--forceOverwrite <forceOverwrite>",
            "Force overwrite of destination directory if it exists",
            CommandOptionType.NoValue);

            CommandOption timerCountOption = commandLineApplication.Option(
            "-$|-t |--timercount <timercount>",
            "Numer of simultaneous timers to run for the timer test",
            CommandOptionType.SingleValue);

            commandLineApplication.HelpOption("-? | -h | --help");
            commandLineApplication.OnExecute(() =>
            {
                int timerCount;

                if (timerCountOption.HasValue())
                {
                    if (int.TryParse(timerCountOption.Value(), out timerCount))
                    {
                        TimerMultiThreadingExample(timerCount);
                        return 1;
                    }
                    else
                    {
                        Console.WriteLine("\nPlease provide a valid int value for -t (timer count)");
                        commandLineApplication.ShowHelp();
                        return 0;
                    }
                }

                if (sourcePathOption.HasValue() && destPathOption.HasValue())
                {
                    var forceOverwrite = false;

                    if (forceOverwriteOption.HasValue())
                        forceOverwrite = true;

                    Unzip(sourcePathOption.Value(), destPathOption.Value(), forceOverwrite);
                }
                else
                {
                    Console.WriteLine("\nPlease provide values for both -s (source path) and -d (destination path)");
                    commandLineApplication.ShowHelp();
                }
                return 0;
            });

            commandLineApplication.Execute(args);
        }

        /// <summary>
        /// Provides an example of multithreading 
        /// </summary>
        /// <param name="numberTimers"></param>
        private static void TimerMultiThreadingExample(int numberTimers)
        {
            var timer = new Stopwatch();
            timer.Start();

            Console.WriteLine($"Number of timers:{numberTimers}");

            var tasks = new List<Task>();

            // For the below, the tasks complete but are not run in parallel
            foreach (var i in Enumerable.Range(0, numberTimers))
            {
                Console.WriteLine($"Starting timer {i}");
                var task = new Task( () => Thread.Sleep(5000) );

                task.ContinueWith(j => Console.WriteLine($"Finished timer {i}"));
                task.Start();
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
            var summaryTask = Task.WhenAll(tasks.ToArray());

            try
            {
                summaryTask.Wait();
            }
            catch { }

            if (summaryTask.Status == TaskStatus.RanToCompletion)
                Console.WriteLine("All tasks finished.");
            else if (summaryTask.Status == TaskStatus.Faulted)
                Console.WriteLine("Some tasks failed.");

            // Note: the below shows how to get a count of faulted tasks.
            // https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.whenall?view=netcore-3.1 

            timer.Stop();

            Console.WriteLine($"Ran {numberTimers} timers in {timer.Elapsed.Seconds} seconds");
        }

        private static void Unzip(string sourcePath, string destPath, bool forceOverWrite)
        {
            var timer = new Stopwatch();
            timer.Start();

            Console.WriteLine($"Source: {sourcePath} Destination: {destPath}");

            var zipFiles = Directory.GetFiles(sourcePath).Where(f => Path.GetExtension(f) == ".zip");

            var fileCount = 0;
            var skippedCount = 0;

            var tasks = new List<Task>();

            foreach (var fileName in zipFiles)
            {
                var destZipDir = Path.Combine(destPath, Path.GetFileNameWithoutExtension(fileName));

                if (Directory.Exists(destZipDir) && !forceOverWrite)
                {
                    Console.WriteLine($"Skipping: {fileName} Destination already exists: {destZipDir}");
                    skippedCount++;
                    continue;
                }

                Console.WriteLine($"Extracting: {fileName} to: {destZipDir}");

                var task = new Task(
                    () =>
                    {
                        if (Directory.Exists(destZipDir) && forceOverWrite)
                        {
                            Directory.Delete(destZipDir, true);
                        }

                        ZipFile.ExtractToDirectory(fileName, destZipDir);
                        Thread.Sleep(1000);
                    }
                    );

                task.ContinueWith(
                    i => Console.WriteLine($"Extracted: {fileName} to: {destZipDir}")
                    );

                task.Start();
                tasks.Add(task);

                fileCount++;
            }

            Task.WaitAll(tasks.ToArray());
            var summaryTask = Task.WhenAll(tasks.ToArray());
            try
            {
                summaryTask.Wait();
            }
            catch { }

            if (summaryTask.Status == TaskStatus.RanToCompletion)
                Console.WriteLine("All tasks finished.");
            else if (summaryTask.Status == TaskStatus.Faulted)
                Console.WriteLine("Some tasks failed.");

            timer.Stop();

            Console.WriteLine($"\nCompleted in {timer.Elapsed.Seconds} seconds.\n{zipFiles.Count()} zip files found.\n{fileCount} zip files unzipped.\n{skippedCount} zip files skipped.");
        }
    }
}
