namespace Backend_Nghiencf.Dtos.Common;

public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public int Total { get; init; }
}
