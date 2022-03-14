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

        public static void Main(string[] args)
        {
            LoadData();
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
                Console.WriteLine($"esp rolling alpha: Quick and easy packages from source\n\nCommands: \n\nesp install <package> [additional packages]: Installs the specified package(s). \n\nesp list-installed: Lists all installed packages. \n\nesp uninstall <package>: Uninstalls the specified package. \n\nesp update [package(s)]: Updates the specified package(s), or all packages.");
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
                        WriteData();
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
                        Console.WriteLine($"{pkg.Name} {pkg.Version}");
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
                    foreach(IPackage pkg in InstalledPackages.Values.ToArray())
                    {
                        Directory.CreateDirectory($@"{Utils.HomePath}/.cache/esp/pkgs");
                        Utils.ExecuteShellCommand($"curl {pkg.UpdateURL} -o {Utils.HomePath}/.cache/esp/pkgs/{pkg.Name}-temp.esp");
                        GitPackage package = GitPackage.LoadFromFile($@"{Utils.HomePath}/.cache/esp/pkgs/{pkg.Name}-temp.esp");
                        if(CompareVersions(pkg.Version, package.Version) == -1)
                        {
                            InstallPackage($@"{Utils.HomePath}/.cache/esp/pkgs/{pkg.Name}-temp.esp");
                        }
                    }
                    WriteData();
                }
            }
        }

        

        public static void InstallPackage(string package)
        {
            if(InstalledPackages.ContainsKey(package))
            {
                Console.Write($"Package {package} is already installed. Reinstall?");
                if(Utils.YesNoInput())
                {
                    UninstallPackage(package);
                }
                else
                {
                    return;
                }
            }
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
                        Utils.RunCommand(commandFormatted, $@"{Utils.HomePath}/.cache/esp/pkg/{pkg.Name}");
                    }
                    catch(Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[esp] Error installing {package}: {e.Message}");
                        Console.ResetColor();
                    }
                }
                if(InstalledPackages.ContainsKey(package))
                    InstalledPackages.Remove(package);
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
            InstalledPackages.Remove(package);
        }

        public static void LoadData()
        {
            if(File.Exists("/var/esp/InstalledPackages.esp"))
            {
                KONNode installedPkgsNode = KONParser.Default.Parse(File.ReadAllText("/var/esp/InstalledPackages.esp"));
                foreach(KONNode node in installedPkgsNode.Children)
                {
                    if(node.Name == "PACKAGE")
                    {
                        if((string)node.Values["type"] == "Git")
                        {
                            GitPackage pkg = GitPackage.ParseFromString(KONWriter.Default.Write(node));
                            if(!Packages.ContainsKey(pkg.Name))
                            {
                                Packages.Add(pkg.Name, pkg);
                                InstalledPackages.Add(pkg.Name, pkg);
                            }
                        }
                    }
                }
            }
        }

        public static void WriteData()
        {
            KONNode outputNode = new KONNode("ESP_INSTALLED_PACKAGES");
            foreach(IPackage package in InstalledPackages.Values)
            {
                KONNode pkgNode = new KONNode("PACKAGE");

                pkgNode.AddValue("name", package.Name);
                pkgNode.AddValue("description", package.Description);
                pkgNode.AddValue("version", package.Version.ToString());
                pkgNode.AddValue("type", package.Type.ToString());
                pkgNode.AddValue("updateURL", package.UpdateURL);

                if(package is GitPackage gitPackage)
                {
                    pkgNode.AddValue("cloneURL", gitPackage.CloneURL);
                }

                KONArray installArray = new KONArray("INSTALL_COMMANDS");
                foreach(string cmd in package.InstallCommands)
                {
                    installArray.AddItem(cmd);
                }
                pkgNode.AddArray(installArray);

                KONArray uninstallArray = new KONArray("UNINSTALL_COMMANDS");
                foreach(string cmd in package.UninstallCommands)
                {
                    uninstallArray.AddItem(cmd);
                }
                pkgNode.AddArray(uninstallArray);

                outputNode.AddChild(pkgNode);
            }
            File.WriteAllText($@"{Utils.HomePath}/.cache/esp/InstalledPackages.esp.temp", Utils.konWriter.Write(outputNode));

            Utils.ExecuteShellCommand($"sudo mv {Utils.HomePath}/.cache/esp/InstalledPackages.esp.temp /var/esp/InstalledPackages.esp");
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