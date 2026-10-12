using System.Collections;
using Xunit;

namespace jIAnSoft.Rayleigh.Tests;

public class ValuesIteratorTests
{
    [Theory]
    [InlineData("Array")]
    [InlineData("List")]
    [InlineData("Enumerable")]
    public void Values_EnumeratorsAreIndependentAndRepeatable(string sourceKind)
    {
        Option<string>[] items = [Option<string>.None, "first", Option<string>.None, "last"];
        IEnumerable<Option<string>> source = sourceKind switch
        {
            "Array" => items,
            "List" => new List<Option<string>>(items),
            _ => new TrackingSource<string>(items)
        };
        var values = source.Values();
        using var first = values.GetEnumerator();
        using var second = values.GetEnumerator();

        Assert.True(first.MoveNext());
        Assert.Equal("first", first.Current);
        Assert.True(first.MoveNext());
        Assert.Equal("last", first.Current);
        Assert.True(second.MoveNext());
        Assert.Equal("first", second.Current);
        Assert.False(first.MoveNext());
        Assert.True(second.MoveNext());
        Assert.Equal("last", second.Current);
        Assert.False(second.MoveNext());
        Assert.Equal(["first", "last"], values);
    }

    [Fact]
    public void Values_EnumerableIsNotOpenedUntilFirstMoveNext()
    {
        var source = new TrackingSource<int>([1, Option<int>.None, 3]);
        var values = source.Values();
        using var iterator = values.GetEnumerator();
        Assert.Equal(0, source.Opened);
        Assert.Equal(0, source.Visited);

        Assert.True(iterator.MoveNext());
        Assert.Equal(1, iterator.Current);
        Assert.Equal(1, source.Opened);
        Assert.Equal(1, source.Visited);
    }

    [Fact]
    public void Values_EarlyExitDisposesSourceWithoutVisitingRemainingItems()
    {
        var source = new TrackingSource<int>([Option<int>.None, 2, 3]);
        Assert.Equal([2], source.Values().Take(1));
        Assert.Equal(2, source.Visited);
        Assert.Equal(1, source.Disposed);
    }

    [Fact]
    public void Values_FullEnumerationDisposesSource()
    {
        var source = new TrackingSource<int>([1, Option<int>.None, 3]);
        Assert.Equal([1, 3], source.Values());
        Assert.Equal(3, source.Visited);
        Assert.Equal(1, source.Disposed);
    }

    [Fact]
    public void Values_SourceExceptionPropagatesAndDisposesSource()
    {
        var source = new TrackingSource<int>([1, 2]) { ThrowAfterItems = true };
        Assert.Throws<InvalidOperationException>(() => source.Values().ToArray());
        Assert.Equal(1, source.Disposed);
    }

    [Fact]
    public void Values_ListChangesBeforeFirstMoveNextAreObserved()
    {
        List<Option<int>> source = [1];
        var values = source.Values();
        using var iterator = values.GetEnumerator();
        source.Add(2);

        Assert.True(iterator.MoveNext());
        Assert.Equal(1, iterator.Current);
        Assert.True(iterator.MoveNext());
        Assert.Equal(2, iterator.Current);
        Assert.False(iterator.MoveNext());
    }

    [Fact]
    public void Values_ListChangesBetweenYieldsStillThrow()
    {
        List<Option<int>> source = [1, 2];
        using var iterator = source.Values().GetEnumerator();
        Assert.True(iterator.MoveNext());
        source.Add(3);
        Assert.Throws<InvalidOperationException>(() => iterator.MoveNext());
    }

    [Fact]
    public void Values_ArrayChangesBetweenYieldsAreObserved()
    {
        Option<int>[] source = [1, 2];
        using var iterator = source.Values().GetEnumerator();
        Assert.True(iterator.MoveNext());
        source[1] = 5;
        Assert.True(iterator.MoveNext());
        Assert.Equal(5, iterator.Current);
    }

    [Theory]
    [InlineData("Array")]
    [InlineData("List")]
    [InlineData("Enumerable")]
    public void Values_LargeStructPreservesEveryField(string sourceKind)
    {
        var expected = Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00");
        Option<Guid>[] items = [Option<Guid>.None, expected, Guid.Empty, Option<Guid>.None];
        IEnumerable<Option<Guid>> source = sourceKind switch
        {
            "Array" => items,
            "List" => new List<Option<Guid>>(items),
            _ => new TrackingSource<Guid>(items)
        };

        Assert.Equal([expected, Guid.Empty], source.Values());
    }

    private sealed class TrackingSource<T>(Option<T>[] items) : IEnumerable<Option<T>> where T : notnull
    {
        public int Opened { get; private set; }
        public int Visited { get; private set; }
        public int Disposed { get; private set; }
        public bool ThrowAfterItems { get; init; }

        public IEnumerator<Option<T>> GetEnumerator()
        {
            Opened++;
            return Iterate().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private IEnumerable<Option<T>> Iterate()
        {
            try
            {
                foreach (var item in items)
                {
                    Visited++;
                    yield return item;
                }

                if (ThrowAfterItems)
                {
                    throw new InvalidOperationException("Source failure");
                }
            }
            finally
            {
                Disposed++;
            }
        }
    }
}
