using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using KarrotObjectNotation;

namespace Esp
{
    public class Program
    {
        public static Dictionary<string, string> BuildVars = new Dictionary<string, string>();
        public static Dictionary<string, IPackage> Packages = new Dictionary<string, IPackage>();
        public static Dictionary<string, IPackage> InstalledPackages = new Dictionary<string, IPackage>();
        private static bool ignoreProtected = false;

        public static void Main(string[] args)
        {
            /* string[] porthInstallCommands = 
            {
                "fasm -m 524288 ./bootstrap/porth-linux-x86_64.fasm",
                "chmod +x ./bootstrap/porth-linux-x86_64",
                "./bootstrap/porth-linux-x86_64 com ./porth.porth",
                "./porth com ./porth.porth",
                "sudo cp ./porth /usr/bin"
            };
            IPackage[] porthDependencies = {};
            GitPackage porthPackage = new GitPackage("porth", "Compiler for the Porth programming language created by Alexey Kutepov.", "https://gitlab.com/tsoding/porth", porthInstallCommands, porthDependencies);
            Packages.Add("porth", porthPackage);

            string[] espInstallCommands = 
            {
                "make -j $THREADS",
                "sudo make install-esp",
                "echo -e 'An updated version of esp has been installed to a temporary location.\nPlease run esp-update as root to install it.'"
            };
            IPackage[] espDependencies = {};
            GitPackage espPackage = new GitPackage("esp", "esp package manager.", "git@github.com:Mrcarrot1/esp", espInstallCommands, espDependencies);
            Packages.Add("esp", espPackage); */

            /* foreach(string file in Directory.GetFiles(@"/home/mrcarrot/esp"))
            {
                if(file.EndsWith(".esp"))
                {
                    GitPackage package = GitPackage.LoadFromFile(file);
                    Packages.Add(package.Name, package);
                }
            }*/

            if(Environment.UserName == "root")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("esp must not be run as root!");
                Console.ResetColor();
                return;
            }

            BuildVars.Add("THREADS", Environment.ProcessorCount.ToString());

            if(args.Length == 0)
            {
                Console.WriteLine($"esp v0.1.0: Quick and easy packages from source\n\nCommands: \n\nesp install <package> [additional packages]: Installs the specified package(s). \n\nesp list-installed: Lists all installed packages. \n\nesp uninstall <package>: Uninstalls the specified package. \n\nesp update [package(s)]: Updates the specified package(s), or all packages.");
            }
            else
            {
                if(args[0].ToLower() == "install")
                {
                    if(args.Length == 1)
                    {
                        Console.WriteLine($"Usage: esp {args[0]} <package>");
                    }
                    else
                    {
                        for(int i = 1; i < args.Length; i++)
                        {
                            if(!args[i].StartsWith("-")) //Check for esp flags and don't try to install them as packages
                                InstallPackage(args[i]);
                        }
                    }
                }
                if(args[0].ToLower() == "list-installed")
                {
                    if(InstalledPackages.Count == 0)
                    {
                        Console.WriteLine("No packages installed.");
                    }
                    foreach(IPackage pkg in InstalledPackages.Values)
                    {
                        Console.WriteLine($"{pkg.Name}: {pkg.Description}");
                    }
                }
                if(args[0].ToLower() == "uninstall")
                {
                    if(args.Length == 1)
                    {
                        Console.WriteLine($"Usage: esp {args[0]} <package>");
                    }
                    else
                    {
                        for(int i = 1; i < args.Length; i++)
                        {
                            if(!args[i].StartsWith("-")) //Check for esp flags and don't try to install them as packages
                                UninstallPackage(args[i]);
                        }
                    }
                }
                if(args[0].ToLower() == "update")
                {
                    foreach(IPackage pkg in InstalledPackages.Values)
                    {
                        InstallPackage(pkg.Name);
                    }
                }
            }
        }

        

        public static void InstallPackage(string package)
        {
            Directory.CreateDirectory($@"{Utils.HomePath}/.cache/esp/pkg");
            IPackage? pkg = null;
            if(Packages.ContainsKey(package))
            {
                pkg = Packages[package];
            }
            else if(File.Exists(package))
            {
                pkg = GitPackage.LoadFromFile(package);
            }
            else
            {
                Console.WriteLine($"{package}: package not found!");
            }
            if(pkg != null)
            {
                if(pkg is GitPackage gitPkg)
                {
                    if(Directory.Exists($@"{Utils.HomePath}/.cache/esp/pkg/{pkg.Name}"))
                        Utils.ExecuteShellCommand($"git reset --hard; git pull", $@"{Utils.HomePath}/.cache/esp/pkg/{pkg.Name}");
                    else
                        Utils.ExecuteShellCommand($"git clone {gitPkg.CloneURL} {Utils.HomePath}/.cache/esp/pkg/{pkg.Name}");
                }
                //Implement other package types:
                //Tarball
                //Other- install commands include download instructions
                
                foreach(string command in pkg.InstallCommands)
                {
                    try
                    {
                        //Create a copy of the command string to reformat.
                        string commandFormatted = Utils.FormatCommand(command);
                        //Run the command- note that RunCommand includes esp built-in commands, whereas ExecuteShellCommand does not.
                        Utils.RunCommand(commandFormatted, Environment.CurrentDirectory);
                    }
                    catch(FormatException e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[esp] Error installing {package}: {e.Message}");
                        Console.ResetColor();
                    }
                }
                InstalledPackages.Add(package, pkg);
            }
        }

        public static void UninstallPackage(string package)
        {
            if(!InstalledPackages.ContainsKey(package))
            {
                Console.WriteLine($"Package {package} is not installed!");
                return;
            }

            IPackage pkg = InstalledPackages[package];
            foreach(string command in pkg.UninstallCommands)
            {
                try
                {
                    string commandFormatted = Utils.FormatCommand(command);
                    Utils.RunCommand(commandFormatted, Environment.CurrentDirectory);
                }
                catch(FormatException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[esp] Error uninstalling {package}: {e.Message}");
                    Console.ResetColor();
                }
            }
        }

        public static void LoadData()
        {
            
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