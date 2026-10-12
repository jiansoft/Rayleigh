using Xunit;

namespace jIAnSoft.Rayleigh.Tests;

public class SequenceStorageTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(1024)]
    public void ReferenceValues_OutputIsFullyInitializedAndIndependent(int size)
    {
        var expected = Enumerable.Range(0, size).Select(i => $"value-{i}").ToArray();
        var options = expected.Select(Option<string>.Some).ToList();
        var results = expected.Select(Result<string, string>.Ok).ToList();
        var outputs = new[]
        {
            options.Sequence().Unwrap(), options.ToArray().Sequence().Unwrap(),
            results.Sequence().Unwrap(), results.ToArray().Sequence().Unwrap()
        };

        options.Clear();
        results.Clear();
        foreach (var output in outputs)
        {
            Assert.Equal(size, output.Count);
            Assert.Equal(expected, output);
            output.Add("appended");
            Assert.Equal("appended", output[size]);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(7)]
    public void UninitializedResult_ThrowsUnlessAnEarlierErrorShortCircuits(int index)
    {
        var values = Enumerable.Repeat(Result<string, string>.Ok("value"), 8).ToArray();
        values[index] = default;
        Assert.Throws<InvalidOperationException>(() => values.Sequence());
        Assert.Throws<InvalidOperationException>(() => values.ToList().Sequence());
        if (index > 0)
        {
            values[0] = Result<string, string>.Err("first");
            Assert.Equal("first", values.Sequence().UnwrapErr());
            Assert.Equal("first", values.ToList().Sequence().UnwrapErr());
        }
    }
}
