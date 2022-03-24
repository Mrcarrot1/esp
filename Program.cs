using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using KarrotObjectNotation;

namespace Esp
{
    public class Program
    {
        public static Dictionary<string, string> BuildVars = new Dictionary<string, string>();
        public static Dictionary<string, IPackage> Packages = new Dictionary<string, IPackage>();
        public static Dictionary<string, IPackage> InstalledPackages = new Dictionary<string, IPackage>();

        public static void Main(string[] args)
        {
            /// <summary>
            /// Whether or not the data has changed for this run and, subsequently, whether or not to update it on disk.
            /// </summary>
            bool dataChanged = false;
            LoadData();

            if (Environment.UserName == "root")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("esp must not be run as root!");
                Console.ResetColor();
                return;
            }

            BuildVars.Add("THREADS", Environment.ProcessorCount.ToString());

            if (args.Length == 0)
            {
                Console.WriteLine($"esp rolling alpha: Quick and easy packages from source\n\nCommands: \n\nesp install <package> [additional packages]: Installs the specified package(s). \n\nesp list-installed: Lists all installed packages. \n\nesp uninstall <package>: Uninstalls the specified package. \n\nesp update [package(s)]: Updates the specified package(s), or all packages.");
            }
            else
            {
                if (args[0].ToLower() == "install")
                {
                    if (args.Length == 1)
                    {
                        Console.WriteLine($"Usage: esp {args[0]} <package>");
                    }
                    else
                    {
                        for (int i = 1; i < args.Length; i++)
                        {
                            if (!args[i].StartsWith("-")) //Check for esp flags and don't try to install them as packages
                                if (InstallPackage(args[i]))
                                    dataChanged = true;
                        }
                    }
                }
                if (args[0].ToLower() == "list-installed")
                {
                    if (InstalledPackages.Count == 0)
                    {
                        Console.WriteLine("No packages installed.");
                    }
                    foreach (IPackage pkg in InstalledPackages.Values)
                    {
                        Console.WriteLine($"{pkg.Name} {pkg.Version}");
                    }
                }
                if (args[0].ToLower() == "uninstall")
                {
                    if (args.Length == 1)
                    {
                        Console.WriteLine($"Usage: esp {args[0]} <package>");
                    }
                    else
                    {
                        for (int i = 1; i < args.Length; i++)
                        {
                            if (args[i] == "*")
                            {
                                foreach (string pkg in InstalledPackages.Keys.ToArray())
                                {
                                    //Don't uninstall esp- this prevents an infinite loop, as 'esp uninstall esp' calls 'esp uninstall *'
                                    if (pkg != "esp")
                                        if (UninstallPackage(pkg))
                                            dataChanged = true;
                                }
                            }
                            if (!args[i].StartsWith("-")) //Check for esp flags and don't try to install them as packages
                                if (UninstallPackage(args[i]))
                                    dataChanged = true;
                        }
                    }
                }
                if (args[0].ToLower() == "update")
                {
                    if (args.Length == 1)
                    {
                        foreach (IPackage pkg in InstalledPackages.Values.ToArray())
                        {
                            Directory.CreateDirectory($@"{Utils.HomePath}/.cache/esp/pkgs");
                            Utils.ExecuteShellCommand($"curl {pkg.UpdateURL} -o {Utils.HomePath}/.cache/esp/pkgs/{pkg.Name}-temp.esp");
                            GitPackage package = GitPackage.LoadFromFile($@"{Utils.HomePath}/.cache/esp/pkgs/{pkg.Name}-temp.esp");
                            if (Utils.CompareVersions(pkg.Version, package.Version) == -1)
                            {
                                if (InstallPackage($@"{Utils.HomePath}/.cache/esp/pkgs/{pkg.Name}-temp.esp"))
                                    dataChanged = true;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 1; i < args.Length; i++)
                        {
                            if (InstalledPackages.ContainsKey(args[i]))
                            {
                                IPackage pkg = InstalledPackages[args[i]];
                                Directory.CreateDirectory($@"{Utils.HomePath}/.cache/esp/pkgs");
                                Utils.ExecuteShellCommand($"curl -SsL {pkg.UpdateURL} -o {Utils.HomePath}/.cache/esp/pkgs/{pkg.Name}-temp.esp");
                                GitPackage package = GitPackage.LoadFromFile($@"{Utils.HomePath}/.cache/esp/pkgs/{pkg.Name}-temp.esp");
                                if (Utils.CompareVersions(pkg.Version, package.Version) == -1)
                                {
                                    if (InstallPackage($@"{Utils.HomePath}/.cache/esp/pkgs/{pkg.Name}-temp.esp"))
                                        dataChanged = true;
                                }
                            }
                            else
                            {
                                Console.WriteLine($"esp: Package {args[i]} is not installed");
                            }
                        }
                    }
                }
            }
            if (dataChanged) WriteData();
        }


        /// <summary>
        /// Installs a package, either from the database with a given name, or from a provided package file.
        /// </summary>
        /// <param name="package">The package name or file location.</param>
        /// <param name="confirm">Whether to ask the user for confirmation.</param>
        public static bool InstallPackage(string package, bool confirm = true)
        {
            if (InstalledPackages.ContainsKey(package))
            {
                Console.Write($"esp: Package {package} is already installed. Reinstall?");
                if (Utils.YesNoInput())
                {
                    UninstallPackage(package, false);
                }
                else
                {
                    return false;
                }
            }
            Directory.CreateDirectory($@"{Utils.HomePath}/.cache/esp/pkg");
            IPackage? pkg = null;
            if (Packages.ContainsKey(package))
            {
                pkg = Packages[package];
            }
            else if (File.Exists(package))
            {
                pkg = GitPackage.LoadFromFile(package);
            }
            else
            {
                Console.WriteLine($"esp: {package}: package not found!");
            }
            if (pkg != null)
            {
                if (confirm)
                {
                    Console.Write($"esp: About to uninstall the current version of package {pkg.Name}. Continue?");
                    if (!Utils.YesNoInput(true)) return false;
                }
                if (pkg is GitPackage gitPkg)
                {
                    if (Directory.Exists($@"{Utils.HomePath}/.cache/esp/pkg/{pkg.Name}"))
                    {
                        Utils.ExecuteShellCommand($"git reset --hard; git fetch", $@"{Utils.HomePath}/.cache/esp/pkg/{pkg.Name}");
                        Process gitStatus = new Process();
                        gitStatus.StartInfo = new ProcessStartInfo("bash", "-c \"git status -sb\"")
                        {
                            WorkingDirectory = $@"{Utils.HomePath}/.cache/esp/pkg/{pkg.Name}",
                            RedirectStandardOutput = true
                        };

                        gitStatus.Start();
                        gitStatus.WaitForExit();
                        string gitStatusOutput = gitStatus.StandardOutput.ReadToEnd();

                        //Use a regex to check if the local repository is behind- in 'git status -sb', will be in the first line and say for example '[behind 1]', so we check for that.
                        if (!Regex.IsMatch(gitStatusOutput, @"\[behind .\]") && InstalledPackages.ContainsKey(pkg.Name))
                        {
                            Console.WriteLine($"esp: Skipping up-to-date package {pkg.Name}.");
                            return false;
                        }
                    }
                    else
                        Utils.ExecuteShellCommand($"git clone {gitPkg.CloneURL} {Utils.HomePath}/.cache/esp/pkg/{pkg.Name}");

                    Utils.ExecuteShellCommand("git pull", $@"{Utils.HomePath}/.cache/esp/pkg/{pkg.Name}");
                }

                try
                {
                    foreach (string command in pkg.BuildCommands)
                    {
                        //Create a copy of the command string to reformat.
                        string commandFormatted = Utils.FormatCommand(command);
                        //Run the command- note that RunCommand includes esp built-in commands, whereas ExecuteShellCommand does not.
                        int status = Utils.RunCommand(commandFormatted, $@"{Utils.HomePath}/.cache/esp/pkg/{pkg.Name}");
                        if (status != 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"esp: Command {commandFormatted} exited with code {status}");
                            Console.ResetColor();
                            break;
                        }
                    }

                    foreach (string command in pkg.InstallCommands)
                    {
                        string commandFormatted = Utils.FormatCommand(command);
                        int status = Utils.RunCommand(commandFormatted, $@"{Utils.HomePath}/.cache/esp/pkg/{pkg.Name}");
                        if (status != 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"esp: Command {commandFormatted} exited with code {status}");
                            Console.ResetColor();
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"esp: Error installing {package}: {e.Message}");
                    Console.ResetColor();
                }


                if (InstalledPackages.ContainsKey(package))
                    InstalledPackages.Remove(package);
                InstalledPackages.Add(package, pkg);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Uninstalls the specified package.
        /// </summary>
        /// <param name="package">The package's name.</param>
        /// <param name="confirm">Whether to ask the user for confirmation.</param>
        public static bool UninstallPackage(string package, bool confirm = true)
        {
            if (!InstalledPackages.ContainsKey(package))
            {
                Console.WriteLine($"Package {package} is not installed!");
                return false;
            }

            if (confirm)
            {
                Console.Write($"esp: About to uninstall package {package}. Continue?");
                if (!Utils.YesNoInput(true)) return false;
            }

            IPackage pkg = InstalledPackages[package];
            foreach (string command in pkg.UninstallCommands)
            {
                try
                {
                    string commandFormatted = Utils.FormatCommand(command);
                    Utils.RunCommand(commandFormatted, Environment.CurrentDirectory);
                }
                catch (FormatException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"esp: Error uninstalling {package}: {e.Message}");
                    Console.ResetColor();
                    return false;
                }
            }
            InstalledPackages.Remove(package);
            return true;
        }

        /// <summary>
        /// Loads in data about currently installed packages.
        /// </summary>
        public static void LoadData()
        {
            if (File.Exists("/var/esp/InstalledPackages.esp"))
            {
                KONNode installedPkgsNode = KONParser.Default.Parse(File.ReadAllText("/var/esp/InstalledPackages.esp"));
                foreach (KONNode node in installedPkgsNode.Children)
                {
                    if (node.Name == "PACKAGE")
                    {
                        if ((string)node.Values["type"] == "Git")
                        {
                            GitPackage pkg = GitPackage.ParseFromString(KONWriter.Default.Write(node));
                            if (!Packages.ContainsKey(pkg.Name))
                            {
                                Packages.Add(pkg.Name, pkg);
                                InstalledPackages.Add(pkg.Name, pkg);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the installed package data on disk.
        /// </summary>
        public static void WriteData()
        {
            KONNode outputNode = new KONNode("ESP_INSTALLED_PACKAGES");
            foreach (IPackage package in InstalledPackages.Values)
            {
                KONNode pkgNode = new KONNode("PACKAGE");

                pkgNode.AddValue("name", package.Name);
                pkgNode.AddValue("description", package.Description);
                pkgNode.AddValue("version", package.Version.ToString());
                pkgNode.AddValue("type", package.Type.ToString());
                pkgNode.AddValue("updateURL", package.UpdateURL);

                if (package is GitPackage gitPackage)
                {
                    pkgNode.AddValue("cloneURL", gitPackage.CloneURL);
                }

                KONArray installArray = new KONArray("INSTALL_COMMANDS");
                foreach (string cmd in package.InstallCommands)
                {
                    installArray.AddItem(cmd);
                }
                pkgNode.AddArray(installArray);

                KONArray uninstallArray = new KONArray("UNINSTALL_COMMANDS");
                foreach (string cmd in package.UninstallCommands)
                {
                    uninstallArray.AddItem(cmd);
                }
                pkgNode.AddArray(uninstallArray);

                outputNode.AddChild(pkgNode);
            }
            File.WriteAllText($@"{Utils.HomePath}/.cache/esp/InstalledPackages.esp.temp", Utils.konWriter.Write(outputNode));

            Console.WriteLine("esp: Moving stored data from temporary location(will require root access)");

            Utils.ExecuteShellCommand($"sudo mv {Utils.HomePath}/.cache/esp/InstalledPackages.esp.temp /var/esp/InstalledPackages.esp");
        }
    }
}