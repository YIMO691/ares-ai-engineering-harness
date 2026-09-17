using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ares.Workbench.Adapters.Codex;
using Ares.Workbench.Infrastructure;

namespace Ares.Workbench.Adapter.Tests;

public class ContextReferenceTests
{
    private static string Root()=>LocalPaths.Output(Path.Combine(Path.GetTempPath(),"context-reference-tests",Guid.NewGuid().ToString("N")));

    [Fact] public void CriticalMiddleIsRecoverableWithBoundedPreview()
    {
        var text=new string('a',17000)+"禁止重连后重复发奖"+new string('z',17000);
        var context=new ContextReferences(Root());var reference=context.Add("SPEC",text);
        Assert.DoesNotContain("禁止重连后重复发奖",reference.Preview);
        Assert.True(reference.PreviewTruncated);Assert.Equal(1000,reference.Preview.Length);
        Assert.Equal(text,File.ReadAllText(reference.Path));
        Assert.Equal(Encoding.UTF8.GetByteCount(text),reference.Bytes);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(reference.Path))),reference.Sha256);
        var index=context.Seal();
        Assert.Contains(reference.Path,JsonSerializer.Deserialize<ContextReference[]>(File.ReadAllText(index))!.Select(x=>x.Path));
        context.Validate();
    }

    [Fact] public void DifferentVersionsRemainReadableAndExistingContentIsNotOverwritten()
    {
        var context=new ContextReferences(Root());
        var old=context.Add("SPEC","before");var current=context.Add("SPEC","after");
        Assert.NotEqual(old.Path,current.Path);Assert.Equal("before",File.ReadAllText(old.Path));
        File.WriteAllText(current.Path,"wrong");
        Assert.Throws<InvalidDataException>(()=>context.Add("SPEC","after"));
        Assert.Equal("wrong",File.ReadAllText(current.Path));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void ChangedOrMissingSourceFailsValidation(bool missing)
    {
        var context=new ContextReferences(Root());var reference=context.Add("test","PASS");
        if(missing)File.Delete(reference.Path);else File.WriteAllText(reference.Path,"FAIL");
        Assert.Throws<InvalidDataException>(()=>context.Validate());
    }

    [Fact] public void RecordedHashMismatchCannotCreateASnapshot()
    {
        var root=Root();var context=new ContextReferences(root);
        Assert.Throws<InvalidDataException>(()=>context.Add("SPEC","changed","DEADBEEF"));
        Assert.False(Directory.Exists(root));
    }

    [Fact] public void IndexTamperingAlsoFailsValidation()
    {
        var context=new ContextReferences(Root());context.Add("test","PASS");
        var index=context.Seal();File.WriteAllText(index,"[]");
        Assert.Throws<InvalidDataException>(()=>context.Validate());
    }

    [Fact] public void ForbiddenOutputRootIsRejectedBeforeWriting()
    {
        Assert.Throws<UnauthorizedAccessException>(()=>new ContextReferences("C:/ares-context-forbidden"));
    }
}
