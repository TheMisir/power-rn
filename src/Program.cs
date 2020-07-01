using CommandLine;
using System;
using System.Diagnostics;
using System.IO;

namespace powertoys
{
    class Program
    {
        public class Options
        {
            [Option('e', "editor", Required = false, HelpText = "Set editor.", Default = "editor.bat")]
            public string Editor { get; set; }

            [Option('f', "files", Required = false, HelpText = "File mask.", Default = "*")]
            public string Files { get; set; }

            [Option('d', "directory", Required = false, HelpText = "Search path.", Default = "$(pwd)")]
            public string Directory { get; set; }

            [Option('r', "recursive", Required = false, HelpText = "Recursive subdirectories.", Default = false)]
            public bool Recursive { get; set; }

            [Option('f', "Force", Required = false, HelpText = "Do not ask to confirm deletion.", Default = false)]
            public bool Force { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(Run);
        }

        static void Run(Options options)
        {
            if (options.Directory == "$(pwd)")
            {
                options.Directory = Directory.GetCurrentDirectory();
            }

            if (options.Editor == "editor.bat" && Environment.OSVersion.Platform == PlatformID.Unix)
            {
                options.Editor = "editor.sh";
            }

            // Search for files
            string path = Path.GetTempFileName();
            string[] oldNames = Directory.GetFiles(options.Directory, options.Files, new EnumerationOptions
            {
                ReturnSpecialDirectories = false,
                AttributesToSkip = FileAttributes.Directory | FileAttributes.System | FileAttributes.Hidden,
                IgnoreInaccessible = true,
                RecurseSubdirectories = options.Recursive,
            });

            // Write file names to the created temporary file
            File.WriteAllLines(path, oldNames);
            DateTime lastWrite = File.GetLastWriteTimeUtc(path);

            // Launch editor and wait for exit
            using Process editor = Process.Start(options.Editor, path);
            editor.WaitForExit();

            if (editor.ExitCode != 0)
            {
                Console.Error.WriteLine("Editor exit with non zero exit code.");
                Environment.Exit(2);
            }

            if (File.GetLastWriteTimeUtc(path) <= lastWrite)
            {
                Console.WriteLine("File names are not modified.");
                Environment.Exit(1);
            }

            string[] newNames = File.ReadAllLines(path);

            // Clean up
            File.Delete(path);

            if (newNames.Length != oldNames.Length)
            {
                Console.Error.WriteLine("You should not remove any line! To delete file you can remove text but keep te line empty");
                Environment.Exit(2);
            }

            /// Batch delete choose
            /// a - Not yet answered
            /// y - Delete
            /// n - Do not delete
            char batchDelete = options.Force ? 'y' : 'a';

            for (int i = 0; i<oldNames.Length; i++)
            {
                string oldName = oldNames[i];
                string newName = newNames[i];

                if (oldName != newName)
                {
                    if (newName == "")
                    {
                        // Skip if user choose to not delete any files
                        if (batchDelete == 'n') continue;

                        // Confirm deletion
                        if (batchDelete == 'a')
                        {
                            Console.WriteLine("Are you sure you want to delete {0}?\n" +
                                              "Y - Yes   N - No   A - Yes (all)   C - No (all)", Path.GetFileName(oldName));

                            readAnswer:
                            switch (Console.ReadLine().ToLower())
                            {
                                case "y":
                                    break;

                                case "n":
                                    continue;

                                case "a":
                                    batchDelete = 'y';
                                    break;

                                case "c":
                                    batchDelete = 'n';
                                    continue;

                                default:
                                    goto readAnswer;
                            }
                        }

                        // Remove file
                        Console.WriteLine("Removing {0}", Path.GetFileName(oldName));
                        File.Delete(oldName);
                    }
                    else
                    {
                        // Move (aka: rename) file
                        Console.WriteLine("Renaming {0} to {1}", Path.GetFileName(oldName), Path.GetFileName(newName));
                        File.Move(oldName, newName);
                    }
                }
            }
        }
    }
}
