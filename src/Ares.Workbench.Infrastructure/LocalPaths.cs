namespace Ares.Workbench.Infrastructure;
public static class LocalPaths
{
    public static string Output(string path)
    {
        var full=WorkspacePolicy.ValidateRoot(path);
        if(!Path.GetPathRoot(full)!.Equals("D:\\",StringComparison.OrdinalIgnoreCase)
            || !(full.Equals("D:\\AgentWorkspace",StringComparison.OrdinalIgnoreCase)||full.StartsWith("D:\\AgentWorkspace\\",StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Workbench 数据、缓存和日志必须保存在 D:/AgentWorkspace。");
        return full;
    }
}
