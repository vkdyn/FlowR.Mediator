using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowR.Mediator.Tests.Infrastructure;

public static class AssertEx
{
    public static async Task<TException> ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is TException expectedException)
            {
                return expectedException;
            }

            throw new AssertFailedException(
                $"Expected {typeof(TException).FullName}, but got {exception.GetType().FullName}. Message: {exception.Message}");
        }

        throw new AssertFailedException($"Expected {typeof(TException).FullName}, but no exception was thrown.");
    }
}
