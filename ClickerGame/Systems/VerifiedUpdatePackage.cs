// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System.IO.Compression;
using System.Security.Cryptography;
namespace ClickerGame.Systems;
/// <summary>Verifies and stages a portable release without changing the running installation.</summary>
public static class VerifiedUpdatePackage
{
    public static string Stage(string archive,string destination,string expectedSha256)
    {
        if(expectedSha256.Length!=64||!expectedSha256.All(Uri.IsHexDigit))throw new InvalidDataException("Release requires a SHA-256 digest.");
        using(var file=File.OpenRead(archive))
            if(!CryptographicOperations.FixedTimeEquals(SHA256.HashData(file),Convert.FromHexString(expectedSha256)))throw new InvalidDataException("Release checksum does not match.");
        string root=Path.GetFullPath(destination);
        if(Directory.Exists(root)&&Directory.EnumerateFileSystemEntries(root).Any())throw new InvalidDataException("Staging directory must be empty.");
        for(var dir=new DirectoryInfo(root);dir!=null;dir=dir.Parent)
            if(dir.Exists&&dir.Attributes.HasFlag(FileAttributes.ReparsePoint))throw new InvalidDataException("Staging links are not supported.");
        using var zip=ZipFile.OpenRead(archive);
        if(zip.Entries.Count is 0 or >10000)throw new InvalidDataException("Invalid release file count.");
        long total=0;var targets=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entries=new List<(ZipArchiveEntry Entry,string Target)>();
        foreach(var entry in zip.Entries)
        {
            string name=entry.FullName.Replace('\\','/');
            if(name.StartsWith('/')||name.Contains(':')||name.Split('/').Any(p=>p is "." or "..")||((entry.ExternalAttributes>>16)&0xF000)==0xA000)throw new InvalidDataException("Unsafe release path.");
            total=checked(total+entry.Length);
            if(total>2L*1024*1024*1024||entry.Length>512L*1024*1024)throw new InvalidDataException("Release exceeds expansion limits.");
            string target=Path.GetFullPath(Path.Combine(root,name));
            if(!target.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!targets.Add(target))throw new InvalidDataException("Duplicate or escaping release path.");
            entries.Add((entry,target));
        }
        if(!entries.Any(e=>Path.GetRelativePath(root,e.Target).Equals("ClickerGame.exe",StringComparison.OrdinalIgnoreCase)))throw new InvalidDataException("Portable release must include ClickerGame.exe at its root.");
        Directory.CreateDirectory(root);
        foreach(var item in entries)
        {
            if(item.Entry.FullName.EndsWith('/')){Directory.CreateDirectory(item.Target);continue;}
            Directory.CreateDirectory(Path.GetDirectoryName(item.Target)!);
            using var input=item.Entry.Open();using var output=new FileStream(item.Target,FileMode.CreateNew,FileAccess.Write,FileShare.None);input.CopyTo(output);output.Flush(true);
            if(output.Length!=item.Entry.Length)throw new InvalidDataException("Incomplete release file.");
        }
        return Path.Combine(root,"ClickerGame.exe");
    }
}
