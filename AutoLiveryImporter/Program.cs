using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutoLiveryImporter
{
    /// <summary>
    ///     The automated livery importer.
    /// </summary>
    public class Program
    {
        /// <summary>
        ///     The main entry point of the application.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // Intro message
            Console.WriteLine("Please enter a directory to import the liveries from:");
            
            // Get the directory from the user input
            string importDirectory = ConsoleHelper.GetDirectory();
                
            // Find the matching liveries
            string[] possibleMatches = FindLiveries(importDirectory).ToArray();
            
            // Pair our directories up with our config entries
            DirectoryConfigPair[] pairs = GetPairs(possibleMatches).ToArray();

            // Alert the user that multiple sim types are not allowed
            if (pairs.Select(p => p.SimType).Distinct().Count() > 1)
            {
                Console.WriteLine("WARNING: Multiple aircraft sim types have been detected in this directory. The program currently does not support processing multiple sim types.");
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
                return;
            }
            
            // Notify user of invalid pairs
            if (pairs.Any(p => !p.IsValid))
            {
                DirectoryConfigPair[] invalidPairs = pairs.Where(p => !p.IsValid).ToArray();
                Console.WriteLine($"{invalidPairs.Length} liveries were invalid. Would you like to view these invalid pairs? (Y/N)");
                if (ConsoleHelper.GetBoolFromInput())
                {
                    foreach(DirectoryConfigPair pair in invalidPairs) Console.WriteLine($"Directory: {pair.Directory}\r\nConfig:\r\n{pair.ConfigEntry}\r\n\r\n");
                }
            }
            
            // Filter out invalid pairs
            DirectoryConfigPair[] validPairs = pairs.Where(p => p.IsValid).ToArray();
            
            // Check if the user wants to check the liveries before processing them
            Console.WriteLine($"Found {validPairs.Length} valid liveries in {importDirectory}. Would you like to list these liveries? (Y/N)");
            if (ConsoleHelper.GetBoolFromInput())
            {
                foreach(DirectoryConfigPair pair in validPairs) Console.WriteLine(pair.Directory);
            }
            
            // Get our output directory
            Console.WriteLine("Please enter the directory to import the liveries to:");
            string outputDirectory = ConsoleHelper.GetDirectory(s =>
            {
                // Check if this directory has a valid .air file
                string[] files = Directory.GetFiles(s);
                if (files.All(f => new FileInfo(f).Name.ToUpper() != validPairs[0].SimType.ToUpper() + ".AIR"))
                {
                    Console.WriteLine(
                        $"The given directory does not contain a matching .air file (Looking for {validPairs[0].SimType.ToUpper()}.air)\r\nPlease try again.");
                    return false;
                }

                // Check if this directory has an aircraft.cfg file
                if (files.All(f => new FileInfo(f).Name.ToUpper() != "AIRCRAFT.CFG"))
                {
                    Console.WriteLine(
                        "The given directory does not contain any aircraft.cfg file.\r\nPlease try again.");
                    return false;
                }

                return true;
            }, true);
            
            // Check for conflicts in the output directory
            GetConflicts(outputDirectory, validPairs);
            DirectoryConfigPair[] conflictingLiveries =
                validPairs.Where(p => p.HasConflicts).ToArray();
            if (conflictingLiveries.Any())
            {
                Console.WriteLine($"{conflictingLiveries.Length} conflicts were detected. Do you wish to review these conflicts? (Y/N)");
                if (ConsoleHelper.GetBoolFromInput())
                {
                    foreach(DirectoryConfigPair pair in conflictingLiveries) Console.WriteLine($"{pair.LiveryFileName}: {BuildConflictMessage(pair)}");
                }
            }
            
            // Prepare to install
            Console.WriteLine("To begin the installation process, press any key...");
            Console.ReadKey();
            
            // Copy the existing CFG file just in case something goes wrong
            string backupFileName = outputDirectory + $"\\aircraft.backup-{DateTime.Now:yyMMddHHmmss}.cfg";
            File.Copy(outputDirectory + "\\aircraft.cfg",  backupFileName);

            try
            {
                int currentLiverySequence = GetLastLiveryEntryNumber(outputDirectory + "\\aircraft.cfg") + 1;
                foreach (DirectoryConfigPair pair in validPairs)
                {
                    // Determine what we can do given the conflicts
                    if (pair.FileExistsConflict && pair.ConfigEntryConflict)
                    {
                        Console.WriteLine($"Skipping {pair.LiveryFileName}. Livery already exists.");
                        continue;
                    }
                    
                    Console.WriteLine($"Installing {pair.LiveryFileName}.");
                    
                    // Determine what we can do given the conflicts
                    if (pair.FileExistsConflict)
                    {
                        Console.WriteLine("Skipping file transfer. Already exists.");
                    }
                    else
                    {
                        Console.WriteLine($"Copying {pair.Directory} to {outputDirectory}");
                        
                        // Create the directory
                        DirectoryInfo info = Directory.CreateDirectory(outputDirectory + "\\Texture." + pair.LiveryFileName);
                        
                        // Copy the files
                        foreach (string file in Directory.GetFiles(pair.Directory))
                        {
                            File.Copy(file, info.FullName + "\\" + new FileInfo(file).Name, pair.FileExistsConflict);
                        }
                        
                        Console.WriteLine($"{pair.LiveryFileName} copied.");
                    }

                    if (pair.ConfigEntryConflict)
                    {
                        Console.WriteLine($"Skipping config modification. Already configured.");
                        continue;
                    }
                    else
                    {
                        Console.WriteLine($"Creating new config entry.");
                        InsertNewConfigEntry(outputDirectory + "\\aircraft.cfg", pair, currentLiverySequence);
                        Console.WriteLine($"{pair.LiveryFileName} added as [FLTSIM.{currentLiverySequence}]");
                    }

                    currentLiverySequence++;
                }
            }
            catch (Exception ex)
            {
                // Restore backup file
                File.Copy(backupFileName, outputDirectory + "\\aircraft.cfg", true);
                
                // Alert the user
                Console.WriteLine("An error has occured...\r\nConfig file recovered from backup.\r\nError details below:\r\n" + ex.ToString() + "Press any key to close the application.");
                Console.ReadKey();
                return;
            }
            
            Console.WriteLine("Installation complete. Press any key to exit.");
            Console.ReadKey();
        }
        
        /// <summary>
        ///     Compiles a list of directories where liveries exist given a base search directory.
        /// </summary>
        /// <param name="baseDirectory">
        ///     The base search directory.
        /// </param>
        /// <returns>
        ///     A <see cref="IEnumerable{T}"/> of <see cref="string"/> representing the livery directories.
        /// </returns>
        private static IEnumerable<string> FindLiveries(string baseDirectory)
        {
            List<string> results = new List<string>();
            
            Console.WriteLine($"Scanning {baseDirectory} for liveries...");
            
            // Get the sub-directories
            string[] targetDirectory = Directory.GetDirectories(baseDirectory);

            // Find a directory starting with "Texture." and containing a valid config entry
            foreach (string directory in targetDirectory)
            {
                // Get just the folder name
                string folderName = new DirectoryInfo(directory).Name;
                
                // If it's a valid texture folder, and it has a valid config entry, add it
                if (folderName.ToUpper().StartsWith("TEXTURE.") && HasValidConfigEntry(directory))
                {
                    Console.WriteLine($"Found livery in {folderName}.");
                    results.Add(directory);
                }
                else
                {
                    // Otherwise, ignore it
                    Console.WriteLine($"Ignoring \"{folderName}\".");
                }
            }

            // Show me what you got!
            return results;
        }

        /// <summary>
        ///     Determines whether or not the given directory has a livery configuration entry within.
        /// </summary>
        /// <param name="directory">
        ///     The livery directory.
        /// </param>
        /// <returns>
        ///     Whether or not the given directory has a livery configuration entry within.
        /// </returns>
        private static bool HasValidConfigEntry(string directory)
        {
            // Look for a readme file
            string[] files = Directory.GetFiles(directory);
            foreach (string file in files)
            {
                // Get the file name
                string fileName = new FileInfo(file).Name;
                
                // Ignore files that aren't Readmes
                if (!fileName.ToUpper().Contains("README") &&
                    !fileName.ToUpper().Contains("READ ME")) continue;
            
                // If the readme file contains a [FLTSIM.XX] entry, then this is our template
                string[] lines = File.ReadAllLines(file);
                return lines.Any(l => l.ToUpper().StartsWith("[FLTSIM."));
            }
            
            // No matches if we've gotten this far
            return false;
        }

        /// <summary>
        ///     Gets the <see cref="DirectoryConfigPair"/>s given an <see cref="IEnumerable{T}"/> of directories.
        /// </summary>
        /// <param name="directories">
        ///     The directories.
        /// </param>
        /// <returns>
        ///     The <see cref="DirectoryConfigPair"/>s
        /// </returns>
        private static IEnumerable<DirectoryConfigPair> GetPairs(IEnumerable<string> directories)
        {
            List<DirectoryConfigPair> pairs = new List<DirectoryConfigPair>();
            
            // Loop over each directory
            foreach (string directory in directories)
            {
                // Get the config entry
                string config = string.Empty;
                string[] files = Directory.GetFiles(directory);
                foreach (string file in files)
                {
                    // Get the file name
                    string fileName = new FileInfo(file).Name;
                
                    // Ignore files that aren't Readmes
                    if (!fileName.ToUpper().Contains("README") &&
                        !fileName.ToUpper().Contains("READ ME")) continue;
            
                    // Loop through each line of the supposed readme file
                    string[] lines = File.ReadAllLines(file);
                    bool readingData = false;
                    foreach(string line in lines)
                    {
                        // Ignore this line if it doesn't start with [FLTSIM. and we aren't reading data already
                        if (!line.ToUpper().StartsWith("[FLTSIM.") && !readingData) continue;
                        
                        // If we're currently reading data and have come across a blank line, we can stop reading the data
                        if (string.IsNullOrEmpty(line.Trim()) && readingData) break;
                        
                        // Start reading the data and adding it to our config
                        readingData = true;
                        config += line + "\r\n";
                    }
                }
                
                // Add the pair
                pairs.Add(new DirectoryConfigPair()
                {
                    Directory = directory,
                    ConfigEntry = config
                });
            }

            return pairs;
        }

        /// <summary>
        ///     Determines the conflicts between the <paramref name="outputDirectory"/> and the given <paramref name="pairs"/>.
        /// </summary>
        /// <param name="outputDirectory">
        ///     The livery output path.
        /// </param>
        /// <param name="pairs">
        ///     The <see cref="DirectoryConfigPair"/>s.
        /// </param>
        private static void GetConflicts(string outputDirectory, DirectoryConfigPair[] pairs)
        {
            // Get the existing liveries
            string[] existingLiveries = Directory.GetDirectories(outputDirectory);
            
            // Get the existing config
            string[] existingConfigLines = File.ReadAllLines(outputDirectory + "\\aircraft.cfg");
            
            // Find the names of our existing liveries
            string[] existingConfigLiveries = existingConfigLines.Where(l => l.ToUpper().StartsWith("TEXTURE"))
                .Select(s => s.Split("=")[1].Trim()).ToArray();
            
            // Scan for conflicts
            foreach (DirectoryConfigPair pair in pairs)
            {
                // Check for existing files
                pair.FileExistsConflict = existingLiveries.Any(s =>
                    new DirectoryInfo(s).Name.ToUpper() ==
                    "TEXTURE." + pair.LiveryFileName.ToUpper());
                
                // Check for existing config lines
                pair.ConfigEntryConflict = existingConfigLiveries.Contains(pair.LiveryFileName);
            }
        }

        /// <summary>
        ///     Builds a config message given the <see cref="DirectoryConfigPair"/>.
        /// </summary>
        /// <param name="pair">
        ///     The <see cref="DirectoryConfigPair"/>.
        /// </param>
        /// <returns>
        ///     A message describing the conflicts in the given <see cref="DirectoryConfigPair"/>.
        /// </returns>
        private static string BuildConflictMessage(DirectoryConfigPair pair)
        {
            const string defaultMessage = "No conflicts detected.";
            if (!pair.FileExistsConflict && 
                !pair.ConfigEntryConflict) return defaultMessage;
            if (pair.FileExistsConflict &&
                pair.ConfigEntryConflict) return "The livery is already installed. (Will ignore this livery upon installation)";
            if (pair.FileExistsConflict) return "The texture directory already exists but is not configured. (Resolvable)";
            if (pair.ConfigEntryConflict) return "The texture is configured but corresponding directory is missing. (Resolvable)";
            return defaultMessage;
        }

        /// <summary>
        ///     Gets the FLTSIM number of the last installed livery in the given config file.
        /// </summary>
        /// <param name="configFile">
        ///     The config file.
        /// </param>
        /// <returns>
        ///     The FLTSIM number of the last installed livery.
        /// </returns>
        private static int GetLastLiveryEntryNumber(string configFile)
        {
            string[] lines = File.ReadAllLines(configFile);
            string[] headers = lines.Where(l => l.ToUpper().StartsWith("[FLTSIM.")).ToArray();
            return int.Parse(headers.Select(l => l.ToUpper().Replace("[FLTSIM.", string.Empty).Replace("]", string.Empty)).OrderBy(l => l).Last());
        }
        
        /// <summary>
        ///     Inserts a new config entry into the given <paramref name="configFile"/> using the given <paramref name="pair"/> and FLTSIM <paramref name="number"/>.
        /// </summary>
        /// <param name="configFile">
        ///     The config file.
        /// </param>
        /// <param name="pair">
        ///     The <see cref="DirectoryConfigPair"/>.
        /// </param>
        /// <param name="number">
        ///     The FLTSIM number.
        /// </param>
        private static void InsertNewConfigEntry(string configFile, DirectoryConfigPair pair, int number)
        {
            // Get the config file data
            string configData = File.ReadAllText(configFile);
            
            // Check if the FLTSIM entry already exists
            string liveryHeader = $"[FLTSIM.{number}]";
            if (configData.ToUpper().Contains(liveryHeader)) throw new Exception($"The header \"{liveryHeader}\" already exists.");
            
            // Get each line for the config file
            List<string> configLines = configData.Split("\r\n").ToList();
            
            // Get the index of the blank line under the last FLTSIM entry
            int insertIndex = 0;
            bool nextBlankLine = false;
            foreach (string line in configLines)
            {
                // Increment
                insertIndex++;

                // If the current line is [FLTSIM.whatever the number is but - 1],
                // then our next blank line is where we want to insert into
                if(!nextBlankLine && line.ToUpper() == $"[FLTSIM.{number - 1}]")
                {
                    nextBlankLine = true;
                }

                // Found the line we want, no need to keep going
                if (nextBlankLine && string.IsNullOrEmpty(line.Trim())) break;
            }
            
            // Get out new config lines
            string[] liveryConfigLines = pair.ConfigEntry.Split("\r\n");
            
            // Set the header
            for (int i = 0; i < liveryConfigLines.Length; i++)
            {
                // Ignore lines that don't match
                if (!liveryConfigLines[i].ToUpper().StartsWith("[FLTSIM.")) continue;
                // Matching line found
                liveryConfigLines[i] = liveryHeader;
            }
            
            // Insert our lines
            configLines.InsertRange(insertIndex, (string.Join("\r\n", liveryConfigLines) + "\r\n").Split("\r\n"));
            
            // Convert back to a string
            string newConfigString = string.Join("\r\n", configLines);
            
            // Save the file
            File.WriteAllText(configFile, newConfigString);
        }
    }
}