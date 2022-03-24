using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using KarrotObjectNotation;

namespace Esp
{
    public class Utils
    {
        private static string? homeVarPath = Environment.GetEnvironmentVariable("HOME");
        public static string HomePath = homeVarPath != null ? homeVarPath : $@"/home/{Environment.UserName}";
        public static string LogPath = $@"{HomePath}/.config/esp/logs";
        public static KONWriter konWriter = new KONWriter(new KONWriterOptions(arrayInline: false));

        /// <summary>
        /// Formats the given command by replacing variables with their values.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static string FormatCommand(string command)
        {
            bool readingVar = false;
            string currentVar = "";
            string commandFormatted = command;
            for(int i = 0; i < command.Length; i++)
            {
                if(readingVar)
                {
                    if(!char.IsWhiteSpace(command[i])) //Check for whitespace character to delineate variables
                        currentVar += command[i];
                    if(char.IsWhiteSpace(command[i]) || i == command.Length - 1) //Also check for end of string
                    {
                        //Check for esp built-in variables.
                        if(Program.BuildVars.ContainsKey(currentVar))
                            commandFormatted = commandFormatted.Replace($"${currentVar}", Program.BuildVars[currentVar]);
                        //If not an esp variable, check for an environment variable.
                        else if(Environment.GetEnvironmentVariable(currentVar) != null)
                            commandFormatted = commandFormatted.Replace($"${currentVar}", Environment.GetEnvironmentVariable(currentVar));
                        //Otherwise, the variable wasn't found.
                        else
                        {
                            throw new FormatException($"Build variable {currentVar} not found!");
                        }
                        readingVar = false;
                        currentVar = "";
                    }
                }
                if(command[i] == '$' && (i != 0 && command[i - 1] != '\\'))
                {
                    readingVar = true;
                }
            }
            return commandFormatted;
        }

        /// <summary>
        /// Runs a bash or esp built-in command.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="directory"></param>
        /// <returns>The exit code of the process, or -1 if esp experienced an internal error.</returns>
        public static int RunCommand(string command, string? cwd = null)
        {
            //Check for esp built-in commands. Otherwise, run them in a shell.
            string[] commandSplit = command.Split(' ');
            if(commandSplit[0] != "esp")
                return ExecuteShellCommand(command, cwd);
            else
            {
                if(commandSplit[1] == "yes-no")
                {
                    string message = "";
                    for(int i = 2; i < commandSplit.Length; i++)
                    {
                        message += commandSplit[i] + ' ';
                    }
                    message = message.Trim();
                    Console.Write(message);
                    if(!YesNoInput(true))
                    {
                        return -1;
                    }
                    return 0;
                }
                else if(commandSplit[1] == "no-yes")
                {
                    string message = "";
                    for(int i = 2; i < commandSplit.Length; i++)
                    {
                        message += commandSplit[i] + ' ';
                    }
                    message = message.Trim();
                    Console.Write(message);
                    if(!YesNoInput())
                    {
                        return -1;
                    }
                    return 0;
                }
                else if(commandSplit[1] == "alert")
                {
                    string message = "";
                    for(int i = 2; i < commandSplit.Length; i++)
                    {
                        message += commandSplit[i] + ' ';
                    }
                    Console.Write(message);
                    return 0;
                }
                //If not an esp package management command, run it as an esp shell command
                else
                    return ExecuteShellCommand(command, cwd);
            }
        }

        /// <summary>
        /// Executes the specified bash command.
        /// </summary>
        /// <param name="command"></param>
        /// <returns>The exit code of the process, or -1 if esp experienced an internal error</returns>
        public static int ExecuteShellCommand(string command, string? cwd = null)
        {
            if(cwd == null)
            {
                cwd = Environment.CurrentDirectory;
            }
            ProcessStartInfo startInfo = new ProcessStartInfo("bash", $"-c \"{command}\"");
            startInfo.WorkingDirectory = cwd;
            if(command.Split(' ')[0] != "echo" || command.Contains(">") || command.Contains("<") || command.Contains("|")) //If the command is just echo and hasn't been piped anywhere, don't bother printing it out first.
                Console.WriteLine(command);
            var process = Process.Start(startInfo);
            if(process != null)
                process.WaitForExit();
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[esp] Command \"{command}\" failed to run!");
                Console.ResetColor();
                return -1;
            }
            return process.ExitCode;
        }

        /// <summary>
        /// Outputs [Y/n] or [y/N] and gets the user's input.
        /// </summary>
        /// <param name="yesDefault"></param>
        /// <returns></returns>
        public static bool YesNoInput(bool yesDefault = false)
        {
            if(yesDefault)
                Console.Write(" [Y/n] ");
            else
                Console.Write(" [y/N] ");

            string? input = Console.ReadLine();
            if(input == null) return false; //If there's a problem reading input always assume no
            input = input.Trim().ToLower();
            if(input == string.Empty) return yesDefault;
            if(input == "y") return true;
            if(input == "n") return false;
            else
            {
                Console.Write("Please enter y or n!");
                return YesNoInput(yesDefault);
            }
        }

        /// <summary>
        /// Compares two version strings. Returns -1 if the current version is older, 0 if they are the same, and 1 if the current version is newer.
        /// </summary>
        /// <param name="currentVersion"></param>
        /// <param name="otherVersion"></param>
        /// <returns></returns>
        public static int CompareVersions(PackageVersion currentVersion, PackageVersion otherVersion)
        {
            //If it's a rolling release, assume the other version is newer
            if(otherVersion.Rolling)
            {
                return -1;
            }

            //Check major version numbers
            if(otherVersion.Major > currentVersion.Major) return -1;
            if(otherVersion.Major < currentVersion.Major) return 1;

            //Check minor version numbers
            if(otherVersion.Minor > currentVersion.Minor) return -1;
            if(otherVersion.Minor < currentVersion.Minor) return 1;

            //Check patch numbers
            if(otherVersion.Patch > currentVersion.Patch) return -1;
            if(otherVersion.Patch < currentVersion.Patch) return 1;

            //If all the previous checks were false, the version numbers are equal.
            //The prerelease of a given version is assumed to be older.
            if(otherVersion.Prerelease && !currentVersion.Prerelease) return 1;
            if(currentVersion.Prerelease && !otherVersion.Prerelease) return -1;

            //If both are prereleases of the same version, check their labels against each other
            if(currentVersion.Prerelease && otherVersion.Prerelease)
            {
                //Sort the strings alphabetically, then return that in reverse.
                //This is because of the way that semantic versioning handles prerelease labels.
                return -StringComparer.InvariantCulture.Compare(currentVersion.PrereleaseType, otherVersion.PrereleaseType);
            }

            //If we get to this point, the versions are equal.
            return 0;
        }
    }
}