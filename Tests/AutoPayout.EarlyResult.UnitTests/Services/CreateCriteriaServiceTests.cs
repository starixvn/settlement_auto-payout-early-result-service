using AutoPayout.EarlyResult.Services;
using AutoPayout.Models;
using Fanex.Settlement.Converter.Services;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services;
public class CreateCriteriaServiceTests
{
    private readonly CreateQueryServiceImp service = new(new XmlConvertServiceImp());

    [Fact]
    public void QuerySettle_Always_ReturnCorrectCriteria()
    {
        var match = new Match()
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail>()
            {
                new()
                {
                    ResourceId = "00",
                    HomeValue = "1",
                    AwayValue = "2",
                    ExternalSourceInfo = new ExternalSourceInfo[]
                    {
                        new ()
                        {
                            ResourceId = "00",
                            SourceId = 1,
                            SourceName = "testSource"
                        }
                    }
                }
            },
            MatchDetail = new MatchDetail()
            {
                BetStatus = "1-2,3-4",
                LeagueType = 1,
                ParentID = 0
            },
        };

        var criteria = service.QuerySettle(match);

        Assert.Equal(1, criteria.MatchId);
        Assert.Equal("<Resources><Resource Id=\"00\" Home=\"1\" Away=\"2\" /></Resources>", criteria.SelectedResources);
        Assert.Equal("Provider: testSource(00)", criteria.AutoSourceInfo);
    }
}
