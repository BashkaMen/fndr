using Dunet;

namespace Fndr;

[Union]
public partial record Result<T, E>
{
    public partial record Ok(T Value);
    public partial record Error(E Value);
}

public readonly record struct Unit;

public static class ResultExtensions
{
    extension<T, E>(Result<T, E> result)
    {
        public Result<U, E> Map<U>(Func<T, U> map) =>
            result.Match<Result<U, E>>(ok => new Result<U, E>.Ok(map(ok.Value)), error => new Result<U, E>.Error(error.Value));

        public Result<U, E> Bind<U>(Func<T, Result<U, E>> bind) =>
            result.Match(ok => bind(ok.Value), error => new Result<U, E>.Error(error.Value));

        public void OnError(Action<E> onError) => result.MatchError(error => onError(error.Value), () => { });
    }
}
