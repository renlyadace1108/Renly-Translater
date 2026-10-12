using ExamBilingualReader.Core.TextProcessing;
using Xunit;

namespace ExamBilingualReader.Tests;

public class SentenceSplitterTests
{
    [Fact]
    public void Split_ShouldNotBreakOnAcademicAbbreviations()
    {
        var text = "According to Smith et al., cognitive load increases during complex tasks (e.g., reading dense texts). This is confirmed by Fig. 3 in the paper.";
        
        var sentences = SentenceSplitter.Split(text);

        Assert.Equal(2, sentences.Count);
        Assert.Equal("According to Smith et al., cognitive load increases during complex tasks (e.g., reading dense texts).", sentences[0]);
        Assert.Equal("This is confirmed by Fig. 3 in the paper.", sentences[1]);
    }

    [Fact]
    public void Split_ShouldNotBreakOnHonorificTitles()
    {
        var text = "Dr. Watson met with Prof. Moriarty yesterday. Mr. Holmes was also present.";

        var sentences = SentenceSplitter.Split(text);

        Assert.Equal(2, sentences.Count);
        Assert.Equal("Dr. Watson met with Prof. Moriarty yesterday.", sentences[0]);
        Assert.Equal("Mr. Holmes was also present.", sentences[1]);
    }

    [Fact]
    public void Split_ShouldHandleQuotesAndParentheses()
    {
        var text = "\"Knowledge is power,\" he stated. Do you agree (or disagree)?";

        var sentences = SentenceSplitter.Split(text);

        Assert.Equal(2, sentences.Count);
        Assert.Equal("\"Knowledge is power,\" he stated.", sentences[0]);
        Assert.Equal("Do you agree (or disagree)?", sentences[1]);
    }

    [Fact]
    public void Split_ShouldHandleEllipsisAndExclamations()
    {
        var text = "Wait for it... It was amazing! Are you sure?";

        var sentences = SentenceSplitter.Split(text);

        Assert.Equal(3, sentences.Count);
        Assert.Equal("Wait for it...", sentences[0]);
        Assert.Equal("It was amazing!", sentences[1]);
        Assert.Equal("Are you sure?", sentences[2]);
    }
}
