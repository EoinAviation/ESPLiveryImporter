using System;
using System.IO;
using System.Linq;

namespace AutoLiveryImporter
{
    /// <summary>
    ///     The helper methods for console interaction.
    /// </summary>
    public static class ConsoleHelper
    {
        /// <summary>
        ///     The positive answers.
        /// </summary>
        private static readonly string[] PositiveAnswers = new [] {"YES", "Y"};

        /// <summary>
        ///     The negative answers.
        /// </summary>
        private static readonly string[] NegativeAnswers = {"NO", "N"};
        
        /// <summary>
        ///     Gets the directory via user input.
        /// </summary>
        /// <param name="extraValidation">
        ///     User defined function for performing extra validation.
        /// </param>
        /// <param name="suppressBuiltInValidationMessage">
        ///     Whether or not to suppress the build in message for when the <paramref name="extraValidation"/> fails.
        ///     Use this if the <paramref name="extraValidation"/> has custom messages built in.
        /// </param>
        /// <returns>
        ///     The directory.
        /// </returns>
        /// <remarks>
        ///     Validates user input and accounts for environment variables.
        /// </remarks>
        public static string GetDirectory(Predicate<string> extraValidation = null, bool suppressBuiltInValidationMessage = false)
        {
            while (true)
            {
                // Get the user input
                string input = Console.ReadLine() ?? string.Empty;

                // Check for environment variables
                input = Environment.ExpandEnvironmentVariables(input);
                
                // Return if the directory exists
                if (!Directory.Exists(input))
                {
                    // Ask again if it doesn't exist
                    Console.WriteLine("The given directory does not exist, please try again:");
                    continue;
                }

                // Use extra validation if we have to
                if (extraValidation == null || extraValidation(input)) return input;
                
                // Ask again if the extra validation failed
                if (!suppressBuiltInValidationMessage) Console.WriteLine("Failed to validate input, please try again:");
            }
        }

        /// <summary>
        ///     Converts user input (Yes/No) to a boolean.
        /// </summary>
        /// <param name="strict">
        ///     If true, then a valid answer is enforced, otherwise, invalid answers will be presumed to return false.
        /// </param>
        /// <returns>
        ///     A <see cref="bool"/> given user input.
        /// </returns>
        public static bool GetBoolFromInput(bool strict = true)
        {
            while (true)
            {
                // Get user input
                string input = Console.ReadLine() ?? string.Empty;
            
                // Check if positive
                if (PositiveAnswers.Contains((input).Trim().ToUpper())) return true;
            
                // Check if negative
                if (NegativeAnswers.Contains((input).Trim().ToUpper())) return false;

                // Otherwise, we've got no idea what they've entered.
                if (strict)
                {
                    // Get a proper input if we're being strict about it
                    Console.WriteLine("Please answer \"Yes\" or \"No\". (\"Y\" and \"N\" are also accepted)");
                }
                else
                {
                    // Presume it's false if we don't really care
                    return false;
                }
            }
        }
    }
}