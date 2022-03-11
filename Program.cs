using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Linq;
using KarrotObjectNotation;

public class Program
{
    public static Dictionary<string, string> BuildVars = new Dictionary<string, string>();
    public static Dictionary<string, Package> Packages = new Dictionary<string, Package>();
    public static Dictionary<string, Package> InstalledPackages = new Dictionary<string, Package>();
    private static bool ignoreProtected = false;
    private static KONParser manifestParser = KONParser.Default;
    private static KONWriter pkgWriter = new KONWriter(new KONWriterOptions(arrayInline: false));

    public static void Main(string[] args)
    {
        string[] porthInstallCommands = 
        {
            "fasm -m 524288 ./bootstrap/porth-linux-x86_64.fasm",
            "chmod +x ./bootstrap/porth-linux-x86_64",
            "./bootstrap/porth-linux-x86_64 com ./porth.porth",
            "./porth com ./porth.porth",
            "sudo cp ./porth /usr/bin"
        };
        Package[] porthDependencies = {};
        Package porthPackage = new Package("porth", "Compiler for the Porth programming language created by Alexey Kutepov.", "https://gitlab.com/tsoding/porth", porthInstallCommands, porthDependencies);
        Packages.Add("porth", porthPackage);

        string[] espInstallCommands = 
        {
            "make -j $THREADS",
            "sudo make install-esp",
            "echo -e 'An updated version of esp has been installed to a temporary location.\nPlease run esp-update as root to install it.'"
        };
        Package[] espDependencies = {};
        Package espPackage = new Package("esp", "esp package manager.", "git@github.com:Mrcarrot1/esp", espInstallCommands, espDependencies);
        Packages.Add("esp", espPackage);

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
            Console.WriteLine("esp v1.0.0: Quick and easy packages from Git\n\nCommands: \n\nesp install <package> [additional packages]: Installs the specified package(s). \n\nesp list-installed: Lists all installed packages. \n\nesp uninstall <package>: Uninstalls the specified package. \n\nesp update [package(s)]: Updates the specified package(s), or all packages.");
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
                foreach(Package pkg in InstalledPackages.Values)
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
                    //TODO: Implement uninstall
                }
            }
            if(args[0].ToLower() == "update")
            {
                foreach(Package pkg in InstalledPackages.Values)
                {
                    InstallPackage(pkg.Name);
                }
            }
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

    public static void InstallPackage(string package)
    {
        Directory.CreateDirectory($@"/home/{Environment.UserName}/.cache/esp/pkg");
        if(Packages.ContainsKey(package))
        {
            Package pkg = Packages[package];
            if(Directory.Exists($@"/home/{Environment.UserName}/.cache/esp/pkg/{pkg.Name}"))
                ExecuteShellCommand($"git reset --hard; git pull", $@"/home/{Environment.UserName}/.cache/esp/pkg/{pkg.Name}");
            else
                ExecuteShellCommand($"git clone {pkg.CloneURL} /home/{Environment.UserName}/.cache/esp/pkg/{pkg.Name}");
            bool readingVar = false;
            string currentVar = "";
            foreach(string command in pkg.InstallCommands)
            {
                //Create a copy of the command string to modify
                string commandFormatted = command;
                for(int i = 0; i < command.Length; i++)
                {
                    if(readingVar)
                    {
                        if(!char.IsWhiteSpace(command[i])) //Check for whitespace character to delineate variables
                            currentVar += command[i];
                        if(char.IsWhiteSpace(command[i]) || i == command.Length - 1) //Also check for end of string
                        {
                            if(BuildVars.ContainsKey(currentVar))
                                commandFormatted = commandFormatted.Replace($"${currentVar}", BuildVars[currentVar]);
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"[esp] Error installing {package}: build variable ${currentVar} not found");
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
                ExecuteShellCommand(commandFormatted, $@"/home/{Environment.UserName}/.cache/esp/pkg/{pkg.Name}");
            }
        }
        else
        {
            Console.WriteLine($"{package}: package not found!");
        }
    }

    public static void LoadData()
    {
        
    }
}

public class Package
{
    public string Name { get; }
    public string Description { get; }
    public string CloneURL { get; }
    public List<string> InstallCommands { get; }
    public List<Package> Dependencies { get; }
    public List<Package> Dependents { get; }

    public Package(string name, string description, string cloneURL)
    {
        Name = name;
        Description = description;
        CloneURL = cloneURL;
        InstallCommands = new List<string>();
        Dependencies = new List<Package>();
        Dependents = new List<Package>();
    }
    public Package(string name, string description, string cloneURL, IEnumerable<string> installCommands, IEnumerable<Package> dependencies) : this(name, description, cloneURL)
    {
        InstallCommands = installCommands.ToList();
        Dependencies = dependencies.ToList();
    }
}