namespace Aioros.Graphs.ArchitectureTests;

public sealed class AcceptanceTests
{
    [Fact]
    public async Task EveryCriterionIsDefinedOnceAsync()
    {
        var criteria = await Repository.CriteriaAsync();

        Assert.NotEmpty(criteria);
        Assert.Empty(criteria.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key));
    }

    [Fact]
    public async Task EveryCriterionHasExactlyOneTestAsync()
    {
        var criteria = await Repository.CriteriaAsync();
        var tests = await Repository.TestsAsync();

        var coverage = criteria.Distinct().Select(id => (id, tests: tests.Where(t => t.Criteria.Contains(id)).Select(t => t.Test).ToList()));

        Assert.Empty(coverage.Where(c => c.tests.Count != 1).Select(c => $"{c.id}: {c.tests.Count} tests ({string.Join(", ", c.tests)})"));
    }

    [Fact]
    public async Task EveryTestNamesOneKnownCriterionAsync()
    {
        var criteria = (await Repository.CriteriaAsync()).ToHashSet(StringComparer.Ordinal);
        var tests = await Repository.TestsAsync();

        Assert.Empty(tests.Where(t => t.Criteria.Count != 1 || !criteria.Contains(t.Criteria[0])).Select(t => $"{t.Test}: [{string.Join(", ", t.Criteria)}]"));
    }
}
