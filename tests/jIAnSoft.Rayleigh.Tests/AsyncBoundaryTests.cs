using System.Threading.Tasks.Sources;
using Xunit;

namespace jIAnSoft.Rayleigh.Tests;

public class AsyncBoundaryTests
{
    [Theory]
    [InlineData("Bind")]
    [InlineData("Map")]
    [InlineData("MapErr")]
    [InlineData("OrElse")]
    [InlineData("Tap")]
    [InlineData("TapErr")]
    public async Task Result_PendingValueTaskSource_IsConsumedExactlyOnce(string operation)
    {
        var source = new SingleUseSource<Result<int, string>>();
        var pending = ApplyResult(source.Task, operation);
        Assert.False(pending.IsCompleted);
        source.SetResult(operation is "MapErr" or "OrElse" or "TapErr"
            ? Result<int, string>.Err("error") : Result<int, string>.Ok(41));
        var result = await pending;
        Assert.False(result.IsUninitialized);
        Assert.Equal(1, source.ConsumptionCount);
        if (operation is "Bind" or "Map" or "OrElse")
        {
            Assert.Equal(42, result.Unwrap());
        }
        else if (operation == "MapErr")
        {
            Assert.Equal("error!", result.UnwrapErr());
        }
    }

    [Theory]
    [InlineData("Bind")]
    [InlineData("Map")]
    [InlineData("OrElse")]
    public async Task Option_PendingValueTaskSource_IsConsumedExactlyOnce(string operation)
    {
        var source = new SingleUseSource<Option<int>>();
        var task = source.Task;
        var pending = operation switch
        {
            "Bind" => task.BindAsync(static x => ValueTask.FromResult(Option<int>.Some(x + 1))),
            "Map" => task.MapAsync(static x => ValueTask.FromResult(x + 1)),
            _ => task.OrElseAsync(static () => ValueTask.FromResult(Option<int>.Some(42)))
        };
        Assert.False(pending.IsCompleted);
        source.SetResult(operation == "OrElse" ? Option<int>.None : Option<int>.Some(41));
        Assert.Equal(42, (await pending).Unwrap());
        Assert.Equal(1, source.ConsumptionCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Result_PendingBinder_IsConsumedExactlyOnce(bool withToken)
    {
        var source = new SingleUseSource<Result<int, string>>();
        var start = ValueTask.FromResult(Result<int, string>.Ok(1));
        var pending = withToken
            ? start.BindAsync((_, _) => source.Task, CancellationToken.None)
            : start.BindAsync(_ => source.Task);
        Assert.False(pending.IsCompleted);
        source.SetResult(Result<int, string>.Ok(42));
        Assert.Equal(42, (await pending).Unwrap());
        Assert.Equal(1, source.ConsumptionCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingSource_FaultAndCancellation_Propagate(bool canceled)
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var source = new SingleUseSource<Result<int, string>>();
        var pending = source.Task.MapAsync(static x => ValueTask.FromResult(x + 1)).AsTask();
        Exception expected = canceled ? new OperationCanceledException(cts.Token) : new InvalidOperationException("source failed");
        source.SetException(expected);
        var observed = await Record.ExceptionAsync(async () => await pending);
        Assert.Same(expected, observed);
        Assert.Equal(canceled, pending.IsCanceled);
        Assert.Equal(1, source.ConsumptionCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletedSource_ThrowingBinder_StillReturnsFaultedOrCanceledTask(bool canceled)
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Exception expected = canceled ? new OperationCanceledException(cts.Token) : new InvalidOperationException("binder failed");
        var start = ValueTask.FromResult(Result<int, string>.Ok(1));
        ValueTask<Result<int, string>> pending = default;
        var synchronousException = Record.Exception(() =>
            pending = start.BindAsync<int, int, string>(_ => throw expected));
        Assert.Null(synchronousException);
        var task = pending.AsTask();
        Assert.Same(expected, await Record.ExceptionAsync(async () => await task));
        Assert.Equal(canceled, task.IsCanceled);
    }

    [Fact]
    public async Task TaskSource_SuspendsUntilCompletion()
    {
        var source = new TaskCompletionSource<Result<int, string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var invoked = false;
        var pending = source.Task.MapAsync(x =>
        {
            invoked = true;
            return Task.FromResult(x + 1);
        });
        Assert.False(pending.IsCompleted);
        Assert.False(invoked);
        source.SetResult(Result<int, string>.Ok(41));
        Assert.Equal(42, (await pending).Unwrap());
        Assert.True(invoked);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletedSource_FaultedBinder_NormalizesOperationCanceledException(bool canceled)
    {
        Exception expected = canceled ? new OperationCanceledException() : new InvalidOperationException("binder failed");
        var pending = ValueTask.FromResult(Result<int, string>.Ok(1))
            .BindAsync(_ => ValueTask.FromException<Result<int, string>>(expected)).AsTask();
        Assert.Same(expected, await Record.ExceptionAsync(async () => await pending));
        Assert.Equal(canceled, pending.IsCanceled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletedSource_IsConsumedEvenWhenShortCircuited(bool error)
    {
        var source = new SingleUseSource<Result<int, string>>();
        source.SetResult(error ? Result<int, string>.Err("error") : Result<int, string>.Ok(41));
        var result = await source.Task.BindAsync(static x => ValueTask.FromResult(Result<int, string>.Ok(x + 1)));
        Assert.Equal(error, result.IsErr);
        Assert.Equal(1, source.ConsumptionCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Tap_ConsumesActionValueTask_IncludingCompletedSource(bool completed)
    {
        var source = new SingleUseSource<Unit>();
        if (completed)
        {
            source.SetResult(Unit.Value);
        }

        var pending = Result<int, string>.Ok(42).TapAsync(_ => source.VoidTask);
        if (!completed)
        {
            Assert.False(pending.IsCompleted);
            source.SetResult(Unit.Value);
        }

        Assert.Equal(42, (await pending).Unwrap());
        Assert.Equal(1, source.ConsumptionCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Map_ConsumesMapperValueTask_IncludingCompletedSource(bool completed)
    {
        var source = new SingleUseSource<int>();
        if (completed)
        {
            source.SetResult(42);
        }

        var pending = Result<int, string>.Ok(1).MapAsync(_ => source.Task);
        if (!completed)
        {
            Assert.False(pending.IsCompleted);
            source.SetResult(42);
        }

        Assert.Equal(42, (await pending).Unwrap());
        Assert.Equal(1, source.ConsumptionCount);
    }

    private static ValueTask<Result<int, string>> ApplyResult(ValueTask<Result<int, string>> source, string operation)
        => operation switch
        {
            "Bind" => source.BindAsync(static x => ValueTask.FromResult(Result<int, string>.Ok(x + 1))),
            "Map" => source.MapAsync(static x => ValueTask.FromResult(x + 1)),
            "MapErr" => source.MapErrAsync(static e => ValueTask.FromResult(e + "!")),
            "OrElse" => source.OrElseAsync(static _ => ValueTask.FromResult(Result<int, string>.Ok(42))),
            "Tap" => source.TapAsync(static _ => ValueTask.CompletedTask),
            _ => source.TapErrAsync(static _ => ValueTask.CompletedTask)
        };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OptionMap_ConsumesMapperValueTask_IncludingCompletedSource(bool completed)
    {
        var source = new SingleUseSource<string>();
        if (completed)
        {
            source.SetResult("value");
        }

        var pending = Option<int>.Some(42).MapAsync((_, _) => source.Task, CancellationToken.None);
        if (!completed)
        {
            Assert.False(pending.IsCompleted);
            source.SetResult("value");
        }

        Assert.Equal("value", (await pending).Unwrap());
        Assert.Equal(1, source.ConsumptionCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OptionMap_NullMapperResult_FaultsReturnedValueTask(bool completed)
    {
        var source = new SingleUseSource<string>();
        if (completed)
        {
            source.SetResult(null!);
        }

        ValueTask<Option<string>> pending = default;
        Assert.Null(Record.Exception(() => pending = Option<int>.Some(42).MapAsync(_ => source.Task)));
        if (!completed)
        {
            source.SetResult(null!);
        }

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await pending);
        Assert.Equal(1, source.ConsumptionCount);
    }

    private sealed class SingleUseSource<T> : IValueTaskSource<T>, IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<T> _core = new() { RunContinuationsAsynchronously = true };
        public int ConsumptionCount { get; private set; }
        public ValueTask<T> Task => new(this, _core.Version);
        public ValueTask VoidTask => new(this, _core.Version);
        public void SetResult(T result) => _core.SetResult(result);
        public void SetException(Exception exception) => _core.SetException(exception);
        public T GetResult(short token)
        {
            Assert.Equal(0, ConsumptionCount++);
            return _core.GetResult(token);
        }
        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
        void IValueTaskSource.GetResult(short token) => GetResult(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _core.OnCompleted(continuation, state, token, flags);
    }
}
