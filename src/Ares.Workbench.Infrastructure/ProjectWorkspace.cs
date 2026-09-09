using Ares.Workbench.Application;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Infrastructure;
public sealed class ProjectWorkspace(string dataRoot) : IProjectWorkspace
{
    public ProjectProfile Validate(ProjectProfile p)
    {
        var root=WorkspacePolicy.ValidateRoot(p.RepoRoot);
        if(!Directory.Exists(root))throw new ArgumentException("项目目录不存在。");
        if(Path.GetPathRoot(root)!.StartsWith("C:",StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("当前工作区规则禁止向 C 盘项目写入，请选择 D 盘项目。");
        if(string.IsNullOrWhiteSpace(p.BuildCommand)||string.IsNullOrWhiteSpace(p.TestCommand))throw new ArgumentException("构建和测试命令不能为空。");
        _=CommandLineSpec.Parse(p.BuildCommand);_=CommandLineSpec.Parse(p.TestCommand);
        if(p.AllowedPaths.IsDefaultOrEmpty)throw new ArgumentException("至少填写一个允许写入的相对路径。");
        var workspace=new Workspace(p.ProjectId,root,"HEAD",p.AllowedPaths,"unused","local");
        var policy=new WorkspacePolicy(workspace);
        foreach(var allowed in p.AllowedPaths)policy.Resolve(allowed);
        foreach(var instruction in p.InstructionFiles) {
            if(Path.IsPathRooted(instruction)||instruction.Contains(':')||instruction.Split('/','\\').Contains(".."))throw new ArgumentException("指令文件必须是项目内相对路径。");
            var file=WorkspacePolicy.Under(root,Path.Combine(root,instruction));
            if(!File.Exists(file))throw new ArgumentException("指令文件不存在："+instruction);
        }
        return p with {RepoRoot=root};
    }
    public Workspace Create(ProjectProfile p,string runId)
    {
        p=Validate(p);
        if(!Guid.TryParseExact(runId,"N",out _))throw new ArgumentException("Invalid run id");
        var artifacts=LocalPaths.Output(Path.Combine(dataRoot,"artifacts",runId));Directory.CreateDirectory(artifacts);
        return new(p.ProjectId,p.RepoRoot,"HEAD",p.AllowedPaths,artifacts,"local");
    }
}
public sealed record CommandLineSpec(string Executable,string[] Arguments)
{
    // Commands are direct argument arrays, not shell scripts. Quotes support paths with spaces.
    public static CommandLineSpec Parse(string text)
    {
        var tokens=new List<string>();var value=new System.Text.StringBuilder();char quote='\0';bool started=false;
        foreach(char c in text) {
            if(quote!='\0') {if(c==quote)quote='\0';else value.Append(c);started=true;continue;}
            if(c is '\'' or '"'){quote=c;started=true;continue;}
            if(c is ';' or '|' or '&' or '>' or '<' or '\r' or '\n')throw new ArgumentException("请使用单条可执行命令，不使用 shell 管道、重定向或命令连接符。");
            if(char.IsWhiteSpace(c)){if(started){tokens.Add(value.ToString());value.Clear();started=false;}continue;}
            value.Append(c);started=true;
        }
        if(quote!='\0')throw new ArgumentException("命令引号未闭合。");
        if(started)tokens.Add(value.ToString());
        if(tokens.Count==0)throw new ArgumentException("命令为空。");
        return new(tokens[0],tokens.Skip(1).ToArray());
    }
}
