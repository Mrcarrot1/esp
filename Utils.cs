using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Esp
{
    public class Utils
    {
        public static string FormatCommand(string command, string currentPackage)
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
                        if(Program.BuildVars.ContainsKey(currentVar))
                            commandFormatted = commandFormatted.Replace($"${currentVar}", Program.BuildVars[currentVar]);
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[esp] Error: {currentPackage}: build variable ${currentVar} not found");
                            Console.ResetColor();
                        }
                        readingVar = false;
                        currentVar = "";
                    }
                }
                if(command[i] == '$')
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
        public static void RunCommand(string command, string directory)
        {
            //Check for esp built-in commands. Otherwise, run them in a shell.
            string[] commandSplit = command.Split(' ');
            if(commandSplit[0] != "esp")
                ExecuteShellCommand(command, directory);
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
                        return;
                    }
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
                        return;
                    }
                }
                //If not an esp package management command, run it as an esp shell command
                else
                    ExecuteShellCommand(command, directory);
            }
        }

        /// <summary>
        /// Executes the specified bash command.
        /// </summary>
        /// <param name="command"></param>
        public static void ExecuteShellCommand(string command, string? cwd = null)
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
            }
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
    }
}