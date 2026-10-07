using FluentAssertions;
using IndyPOS.Application.Common.Models;
using Xunit;

namespace IndyPOS.Application.Tests.Models;

public class PageReaderTests
{
    [Fact]
    public async Task ReadAllAsync_WhenPagesNeverEnd_Throws()
    {
        var act = () => PageReader.ReadAllAsync<int>(_ => Task.FromResult<(IReadOnlyList<int>, bool)>(([1], true)));

        await act.Should()
                 .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ReadAllAsync_WithAnEmptyFirstPage_ReturnsNothing()
    {
        var items = await PageReader.ReadAllAsync<int>(_ => Task.FromResult<(IReadOnlyList<int>, bool)>(([], false)));

        items.Should()
             .BeEmpty();
    }

    [Fact]
    public async Task ReadAllAsync_WithThreePages_ReturnsEveryItemInOrder()
    {
        var items = await PageReader.ReadAllAsync<int>(page =>
            Task.FromResult<(IReadOnlyList<int>, bool)>(([page * 10, page * 10 + 1], page < 3)));

        items.Should()
             .Equal(10, 11, 20, 21, 30, 31);
    }
}
