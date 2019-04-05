using System;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;

namespace AutoLiveryImporter
{
    /// <summary>
    ///     The texture directory paired with it's corresponding config entry.
    /// </summary>
    public class DirectoryConfigPair
    {
        #region Backing fields
        
        /// <summary>
        ///     The directory.
        /// </summary>
        private string _directory;

        /// <summary>
        ///     The config entry.
        /// </summary>
        private string _config;
        
        #endregion
        
        /// <summary>
        ///     Gets or sets the Directory.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public string Directory
        {
            get => _directory;
            set
            {
                if (!System.IO.Directory.Exists(value))
                    throw new InvalidOperationException($"The directory \"{value}\" does not exist.");

                _directory = value;
            }
        }
        
        /// <summary>
        ///     Gets or sets the config entry.
        /// </summary>
        public string ConfigEntry
        {
            get => _config;
            set
            {
                if (!value.StartsWith("[FLTSIM."))
                    throw new InvalidOperationException($"The provided entry was not valid: \"{value}\"");

                _config = value;
            } 
        }

        /// <summary>
        ///     Gets the .air file name (excluding the ".air") that this config will be looking for.
        /// </summary>
        public string SimType
        {
            get
            {
                // Get our sim config line
                string simConfigLine =
                    ConfigEntry.Split("\r\n").FirstOrDefault(l => l.ToUpper().StartsWith("SIM"));
                
                // Return the sim.air it's pointing to
                return simConfigLine.Split("=")[1].Trim();
            }
        }

        /// <summary>
        ///     Gets the "Texture." folder name.
        /// </summary>
        public string LiveryFileName
        {
            get
            {
                // Get our texture config line
                string textureConfigLine =
                    ConfigEntry.Split("\r\n").FirstOrDefault(l => l.ToUpper().StartsWith("TEXTURE"));

                // Get the trimmed right hand side
                return textureConfigLine.Split("=")[1].Trim();
            }
        }

        /// <summary>
        ///     Gets a value indicating whether or not the <see cref="DirectoryConfigPair"/> is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                // Get our folder name
                string folderName = new DirectoryInfo(Directory).Name;
                
                // Check our livery file name
                if (string.IsNullOrEmpty(LiveryFileName)) return false;
                
                // Check if they match (Ignoring the "Texture." from the folder name)
                return folderName.Substring(8) == LiveryFileName;
            }
        }
        
        /// <summary>
        ///     Gets or sets a value indicating whether or not the texture file already exists.
        /// </summary>
        /// <remarks>
        ///     Used for conflict detection.
        /// </remarks>
        public bool FileExistsConflict { get; set; }
        
        /// <summary>
        ///     Gets or sets a value indicating whether or not the configuration entry already exists.
        /// </summary>
        /// <remarks>
        ///     Used for conflict detection.
        /// </remarks>
        public bool ConfigEntryConflict { get; set; }

        /// <summary>
        ///     Gets a value indicating whether or not the <see cref="FileExistsConflict"/> or <see cref="ConfigEntryConflict"/> properties are true.
        /// </summary>
        public bool HasConflicts => FileExistsConflict || ConfigEntryConflict;

        /// <summary>
        ///     Sets the FLTSIM number.
        /// </summary>
        /// <param name="number">
        ///     The number in sequence.
        /// </param>
        public void SetNumber(int number)
        {
            // Get all the lines in the config entry
            string[] lines = ConfigEntry.Split("\r\n");
            
            // Modify the first one
            lines[0] = $"[FLTSIM.{number}]";
            
            // Set the new string
            ConfigEntry = string.Join("\r\n", lines);
        }
    }
}