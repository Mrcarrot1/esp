using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.IO;
using KarrotObjectNotation;

/// <summary>
/// Interface for package type classes to implement.
/// </summary>
public interface IPackage 
{
    /// <summary>
    /// The package's name.
    /// </summary>
    /// <value></value>
    string Name { get; }
    /// <summary>
    /// The package's description.
    /// </summary>
    /// <value></value>
    string Description { get; }
    /// <summary>
    /// The package's version.
    /// </summary>
    /// <value></value>
    PackageVersion Version { get; set; }
    /// <summary>
    /// What type of package the package is.
    /// </summary>
    /// <value></value>
    PackageType Type { get; }
    /// <summary>
    /// List of bash commands to run to build and install the package.
    /// 
    /// For packages of type PackageType.Other, must also include download commands.
    /// </summary>
    /// <value></value>
    List<string> InstallCommands { get; }
    List<string> UninstallCommands { get; }
}

/// <summary>
/// The package types supported by esp.
/// </summary>
public enum PackageType
{
    Git,
    Tarball,
    Other
}

/// <summary>
/// Simple struct to represent a package version. Used when checking against another version to install.
/// </summary>
public struct PackageVersion
{
    public int Major;
    public int Minor;
    public int Patch;
    public bool Prerelease;
    public string? PrereleaseType;
    public bool Rolling;

    public override string ToString()
    {
        if(Rolling) return "rolling";
        string output = $"{Major}.{Minor}.{Patch}";
        if(Prerelease) output += $"-{PrereleaseType}";
        return output;
    }

    /// <summary>
    /// Parses a version string. See #Package Format in README for more details.
    /// </summary>
    /// <param name="versionString"></param>
    public PackageVersion(string versionString)
    {
        versionString = versionString.ToLower();
        string[] versionSplit = versionString.Split('-');
        if(versionSplit[0] == "rolling")
        {
            Major = 0;
            Minor = 0;
            Patch = 0;
            Rolling = true;
            if(versionSplit.Length > 0)
            {
                Prerelease = true;
                PrereleaseType = versionSplit[1];
            }
            else
            {
                Prerelease = false;
                PrereleaseType = null;
            }
        }
        else
        {
            SemanticVersion semVer = new SemanticVersion(versionString);
            Major = semVer.Major;
            Minor = semVer.Minor;
            Patch = semVer.Patch;
            Rolling = false;
            Prerelease = semVer.PreReleaseLabel != null && semVer.PreReleaseLabel != "";
            if(Prerelease)
                PrereleaseType = semVer.PreReleaseLabel;
            else
                PrereleaseType = null;
        }
    }
}

/// <summary>
/// A package to be installed from Git.
/// </summary>
public class GitPackage : IPackage
{
    public string Name { get; }
    public string Description { get; }
    public PackageVersion Version { get; set; }
    public PackageType Type 
    { 
        get
        { 
            return PackageType.Git;
        } 
    }
    /// <summary>
    /// The URL of the Git repository that contains the package source.
    /// </summary>
    /// <value></value>
    public string CloneURL { get; }
    public List<string> InstallCommands { get; }
    public List<string> UninstallCommands { get; }
    public List<IPackage> Dependencies { get; }
    public List<IPackage> Dependents { get; }

    public GitPackage(string name, string description, string cloneURL, PackageVersion version)
    {
        Name = name;
        Description = description;
        CloneURL = cloneURL;
        InstallCommands = new List<string>();
        UninstallCommands = new List<string>();
        Dependencies = new List<IPackage>();
        Dependents = new List<IPackage>();
    }
    public GitPackage(string name, string description, string cloneURL, PackageVersion version, IEnumerable<string> installCommands, IEnumerable<string> uninstallCommands, IEnumerable<IPackage> dependencies) : this(name, description, cloneURL, version)
    {
        InstallCommands = installCommands.ToList();
        UninstallCommands = uninstallCommands.ToList();
        Dependencies = dependencies.ToList();
    }

    public static GitPackage LoadFromFile(string filePath)
    {
        if(KONParser.Default.TryParse(File.ReadAllText(filePath), out KONNode pkgNode))
        {
            if((string)pkgNode.Values["type"] == "Git")
            {
                GitPackage output = new GitPackage((string)pkgNode.Values["name"], (string)pkgNode.Values["description"], (string)pkgNode.Values["cloneURL"], new PackageVersion((string)pkgNode.Values["version"]));

                foreach(KONArray array in pkgNode.Arrays)
                {
                    if(array.Name == "INSTALL_COMMANDS")
                    {
                        foreach(string str in array)
                        {
                            output.InstallCommands.Add(str);
                        }
                    }
                }

                return output;
            }
        }
        throw new FormatException("The provided file did not conform to the expected format!");
    }
}