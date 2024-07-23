using AutoPayout.Constants;
using AutoPayout.Criteria;
using AutoPayout.Models;
using Fanex.Data.Repository;
using Fanex.Settlement.Converter.Services;
using Microsoft.Extensions.Options;

namespace AutoPayout.UnitTests.Services;

public class UpdateResultStatusServiceTests
{
    private readonly IDynamicRepository repository;

    private readonly XmlConvertService xmlConvertService;

    private readonly UpdateResultStatusService service;

    public UpdateResultStatusServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        xmlConvertService = Substitute.For<XmlConvertService>();
        var options = Options.Create(new AppSettingsKey());
        service = new UpdateResultStatusServiceImp(repository, xmlConvertService, options);
    }

    [Fact]
    public async Task UpdateResultStatus_Always_ExecuteCriteria()
    {
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail>()
        };

        var resources = "Resouces";
        xmlConvertService.Serialize(Arg.Any<Resources<ResultDetail>>()).Returns(resources);

        await service.UpdateResultStatus(match, MatchStatus.Failed);

        await repository.Received().ExecuteAsync(Arg.Is<UpdateAutoPayoutResultCriteria>(c =>
            c.MatchId == 1 && c.StatusId == MatchStatus.Failed && c.SelectedResources == resources));
    }
}