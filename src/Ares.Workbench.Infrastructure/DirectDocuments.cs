using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Ares.Workbench.Application;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Infrastructure;

public sealed class DirectDocuments(string root):IDirectDocuments {
    public const string SopVersion="Workflow-SOP @ a261bcaa1cb1c075f185e003d8ce73bbe7ee89ca / Ares task-snapshot v1";
    public static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static DocumentFile Capture(string role,string path,string text) {
        path=LocalPaths.Output(path);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path,text,new UTF8Encoding(false));return new(role,path,Hash(text),text);
    }
    public void Validate(DocumentSet set) {
        foreach(var f in set.Files) {
            var p=WorkspacePolicy.Under(LocalPaths.Output(root),f.Path);
            if(!File.Exists(p)||new FileInfo(p).Length>200000||Hash(File.ReadAllText(p))!=f.Sha256)
                throw new InvalidOperationException("Task document changed or is missing; reconcile the discussion and record a new document version: "+f.Role);
        }
    }
    public ImmutableArray<DocumentFile> CaptureEvidence(IEnumerable<string> paths)=>paths.Distinct().Select(p=>{
        p=LocalPaths.Output(p);
        if(!File.Exists(p))throw new InvalidOperationException("Verification evidence missing.");
        return new DocumentFile(Path.GetFileName(p),p,Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))),"");
    }).ToImmutableArray();
    public void ValidateEvidence(ImmutableArray<DocumentFile> files) {
        if(files.IsDefaultOrEmpty)throw new InvalidOperationException("Verification evidence is missing.");
        foreach(var f in files) {
            var p=LocalPaths.Output(f.Path);
            if(!File.Exists(p)||Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)))!=f.Sha256)
                throw new InvalidOperationException("Verification evidence changed or disappeared; repeat checks.");
        }
    }
    public DocumentSet Write(string taskId,string title,int version,EngineeringBrief b) {
        if(b.Level is not ("L1" or "L2" or "L3")||string.IsNullOrWhiteSpace(b.LevelReason)||string.IsNullOrWhiteSpace(b.Source)
            ||b.Context.IsDefaultOrEmpty||b.Context.Any(x=>string.IsNullOrWhiteSpace(x.Source)||string.IsNullOrWhiteSpace(x.Observation))
            ||string.IsNullOrWhiteSpace(b.Design)||b.Increments.IsDefaultOrEmpty||b.Increments.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Provide level/reason, original source/version, observed context with sources, design and verifiable increments.");
        // Drafts may contain unresolved issues; Agree remains the Ready gate.
        var dir=WorkspacePolicy.Under(LocalPaths.Output(root),Path.Combine(root,"TASK-"+taskId,"v"+version+"-"+Guid.NewGuid().ToString("N")));
        string bullets(IEnumerable<string> rows)=>string.Join("\n",rows.Select(x=>"- "+x));
        var a=b.Anchors;
        string header="# "+title+"\n\n"+b.Level+" · "+SopVersion+"\n\nAres 任务快照；项目功能文档按来源引用持续维护，本快照用于约定冻结与验证追溯。\n\n原始需求："+b.Source+"\n\n等级理由："+b.LevelReason+"\n\n";
        string context="## 工程上下文与来源\n"+bullets(b.Context.Select(x=>x.Observation+" — "+x.Source))+"\n\n";
        string intent="## 目标\n"+a.Goal+"\n\n## 范围与非目标\n"+a.NonGoals+"\n\n## 修改边界\n"+a.Boundary+
            "\n\n## 验收标准\n"+bullets(a.Acceptance.Select((x,n)=>$"AC-{n+1:00}: {x}"))+"\n\n";
        string design="## 实现设计与关键决定\n"+b.Design+"\n\n"+a.KeyDecisions+"\n\n## 可验证实施增量\n"+bullets(b.Increments)+"\n\n";
        string tests="## 测试与验证方案\n"+a.Verification+"\n\n"+bullets(b.Checks.Select(x=>$"AC-{x.Criterion:00}: {x.Kind} / {x.Method}"))+"\n\n";
        string pending="## 待确认\n"+(a.Unresolved.IsDefaultOrEmpty?"无阻断问题。":bullets(a.Unresolved))+"\n\n";
        var files=new List<DocumentFile>();
        void Add(string role,string text)=>files.Add(Capture(role,Path.Combine(dir,role+".md"),text));
        if(b.Level=="L3") {
            Add("PRD",header+context+intent+pending);
            Add("SDD",header+design);
            Add("TEST-PLAN",header+tests);
        } else Add(b.Level=="L1"?"TASK":"SPEC",header+context+intent+design+tests+pending);
        return new(version,SopVersion,b,[..files]);
    }
    public DocumentSet Deliver(DocumentSet set,string report) {
        Validate(set);
        var files=set.Files.ToList();
        var role=set.Brief.Level=="L3"?"DELIVERY":set.Brief.Level=="L1"?"TASK":"SPEC";
        var existing=files.FirstOrDefault(x=>x.Role==role);
        var original=existing?.Content??"# 交付与对齐\n";
        const string marker="\n<!-- ARES-DELIVERY -->\n";
        var offset=original.IndexOf(marker,StringComparison.Ordinal);if(offset>=0)original=original[..offset];
        var path=existing?.Path??Path.Combine(Path.GetDirectoryName(files[0].Path)!,"DELIVERY.md");
        var result=Capture(role,path,original+marker+report);
        files.RemoveAll(x=>x.Role==role);files.Add(result);
        return set with {Files=[..files]};
    }
}
