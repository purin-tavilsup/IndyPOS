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

    // GeneralHardware has sold 325,780 lines in all (PLAN.md, defect 6): 1,629 pages of 200. A wide date range
    // must read them all, not hit the runaway cap and throw away what it fetched.
    [Fact]
    public async Task ReadAllAsync_WithEveryLineGeneralHardwareHasSold_ReadsThemAll()
    {
        const int pagesOfEveryLineSold = 1_700;

        var items = await PageReader.ReadAllAsync<int>(page =>
            Task.FromResult<(IReadOnlyList<int>, bool)>(([page], page < pagesOfEveryLineSold)));

        items.Count.Should()
                   .Be(pagesOfEveryLineSold);
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
