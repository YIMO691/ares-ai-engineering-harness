using Ares.Workbench.Domain;
namespace Ares.Workbench.Infrastructure;
public sealed class WorkspacePolicy(Workspace workspace)
{
    public string Root { get; }=ValidateRoot(workspace.RepoRoot);
    public static string ValidateRoot(string value)
    {
        if(!Path.IsPathFullyQualified(value))throw new UnauthorizedAccessException("Absolute root required.");
        var full=Path.GetFullPath(value);
        for(var p=new DirectoryInfo(full);p is not null;p=p.Parent)
            if(p.Exists && (p.Attributes&FileAttributes.ReparsePoint)!=0)throw new UnauthorizedAccessException("Linked root denied.");
        return full.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
    }
    public static string Under(string root,string value)
    {
        var full=Path.GetFullPath(value);
        var relative=Path.GetRelativePath(root,full);
        if(relative==".." || relative.StartsWith(".."+Path.DirectorySeparatorChar,StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new UnauthorizedAccessException("Path outside allowed root.");
        var current=root;
        foreach(var part in relative.Split(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar))
        {
            current=Path.Combine(current,part);
            if((File.Exists(current)||Directory.Exists(current)) && (File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)
                throw new UnauthorizedAccessException("Link traversal denied.");
        }
        return full;
    }
    public string Resolve(string relative)
    {
        if(Path.IsPathRooted(relative) || relative.Contains(':') || relative.Split('/','\\').Any(x=>x==".."))
            throw new UnauthorizedAccessException("Relative path required.");
        var full=Under(Root,Path.Combine(Root,relative));
        var normalized=Path.GetRelativePath(Root,full).Replace('\\','/');
        if(normalized.Split('/').Any(x=>x is ".git" or ".codex" or ".agents"))throw new UnauthorizedAccessException("Control path denied.");
        if(!workspace.AllowedPaths.Any(p=>p=="."||normalized==p.TrimEnd('/')||
            normalized.StartsWith(p.TrimEnd('/')+"/",StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Path not granted.");
        return full;
    }
    public string Read(string relative,int maxBytes=200000)
    {
        var path=Resolve(relative);
        if(new FileInfo(path).Length>maxBytes)throw new IOException("Read limit exceeded.");
        return File.ReadAllText(path);
    }
    public IReadOnlyList<string> Search(string query,IEnumerable<string> paths)=>paths.Where(p=>Read(p).Contains(query,StringComparison.Ordinal)).ToArray();
}
