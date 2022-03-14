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
    string UpdateURL { get; }
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
    /// <summary>
    /// List of commands to run to uninstall the package.
    /// </summary>
    /// <value></value>
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
            if(versionSplit.Length > 1)
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
            Version ver = new Version(versionSplit[0]);
            Major = ver.Major;
            Minor = ver.Minor;
            Patch = ver.Build;
            Rolling = false;
            Prerelease = versionSplit.Length > 1;
            if(Prerelease)
                PrereleaseType = versionSplit[1];
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
    public string UpdateURL { get; }
    /// <summary>
    /// The URL of the Git repository that contains the package source.
    /// </summary>
    /// <value></value>
    public string CloneURL { get; }
    public List<string> InstallCommands { get; }
    public List<string> UninstallCommands { get; }
    public List<IPackage> Dependencies { get; }
    public List<IPackage> Dependents { get; }

    public GitPackage(string name, string description, string cloneURL, string updateURL, PackageVersion version)
    {
        Name = name;
        Description = description;
        CloneURL = cloneURL;
        UpdateURL = updateURL;
        InstallCommands = new List<string>();
        UninstallCommands = new List<string>();
        Dependencies = new List<IPackage>();
        Dependents = new List<IPackage>();
    }
    public GitPackage(string name, string description, string cloneURL, string updateURL, PackageVersion version, IEnumerable<string> installCommands, IEnumerable<string> uninstallCommands, IEnumerable<IPackage> dependencies) : this(name, description, cloneURL, updateURL, version)
    {
        InstallCommands = installCommands.ToList();
        UninstallCommands = uninstallCommands.ToList();
        Dependencies = dependencies.ToList();
    }

    public static GitPackage LoadFromFile(string filePath)
    {
        return ParseFromString(File.ReadAllText(filePath));
    }
    public static GitPackage ParseFromString(string packageString)
    {
        if(KONParser.Default.TryParse(packageString, out KONNode pkgNode))
        {
            if((string)pkgNode.Values["type"] == "Git")
            {
                GitPackage output = new GitPackage((string)pkgNode.Values["name"], (string)pkgNode.Values["description"], (string)pkgNode.Values["cloneURL"], (string)pkgNode.Values["updateURL"], new PackageVersion((string)pkgNode.Values["version"]));

                output.Version = new PackageVersion((string)pkgNode.Values["version"]);
                
                foreach(KONArray array in pkgNode.Arrays)
                {
                    if(array.Name == "INSTALL_COMMANDS")
                    {
                        foreach(string str in array)
                        {
                            output.InstallCommands.Add(str);
                        }
                    }
                    if(array.Name == "UNINSTALL_COMMANDS")
                    {
                        foreach(string str in array)
                        {
                            output.UninstallCommands.Add(str);
                        }
                    }
                }

                return output;
            }
        }
        throw new FormatException("The provided string did not conform to the expected format!");
    }
}