using FlowR.Mediator.Tests.Requests;
using System.Runtime.CompilerServices;

namespace FlowR.Mediator.Tests.Handlers;

public sealed class CountToStreamRequestHandler : IStreamRequestHandler<CountToStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(
        CountToStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int index = 1; index <= request.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Yield();

            yield return index;
        }
    }
}
