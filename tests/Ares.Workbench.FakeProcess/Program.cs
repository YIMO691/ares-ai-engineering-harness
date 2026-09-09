using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
if(args.Length==0)return 2;
if(args[0]=="-a")
{
    _ = await Console.In.ReadToEndAsync();
    var output=args[Array.IndexOf(args,"--output-last-message")+1];
    var status=Environment.GetEnvironmentVariable("ARES_FAKE_CODEX_STATUS")??"blocked";
    await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new {status,summary=status=="completed"?"Requested fixture completed.":"Blocked by read-only filesystem policy; no files changed."}));
    Console.WriteLine("{\"type\":\"turn.completed\"}");return 0;
}
switch(args[0])
{
    case "echo":Console.WriteLine(JsonSerializer.Serialize(args.Skip(1)));Console.Error.WriteLine("stderr-value");return 0;
    case "exit":return 7;
    case "flood":Console.Write(new string('x',120000));Console.Error.Write(new string('y',120000));return 0;
    case "hang":await Task.Delay(TimeSpan.FromSeconds(30));return 0;
    case "child":
        var start=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);start.ArgumentList.Add("hang");
        using(var child=Process.Start(start)!){Console.WriteLine(child.Id);Console.Out.Flush();await Task.Delay(TimeSpan.FromSeconds(30));}
        return 0;
    default:return 2;
}
