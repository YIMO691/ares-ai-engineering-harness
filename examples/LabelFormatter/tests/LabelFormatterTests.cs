using LabelDemo;
public class LabelFormatterTests
{
    [Fact,Trait("Category","Baseline")]public void ValidLabelPreserved()=>Assert.Equal("Captain",LabelFormatter.Normalize("Captain"));
    [Fact,Trait("Category","Baseline")]public void NullRejected()=>Assert.Throws<ArgumentNullException>(()=>LabelFormatter.Normalize(null!));
    [Theory,Trait("Category","Core")][InlineData("  Captain  ","Captain")][InlineData("\tAlpha  Beta\n","Alpha  Beta")]
    public void TrimsOnlyOuterWhitespace(string input,string expected)=>Assert.Equal(expected,LabelFormatter.Normalize(input));
    [Theory,Trait("Category","Acceptance")][InlineData("   ")][InlineData("\t\r\n")]
    public void RejectsAllWhitespace(string input)=>Assert.Throws<ArgumentException>(()=>LabelFormatter.Normalize(input));
}
