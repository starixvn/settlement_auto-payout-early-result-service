using AutoPayout.Conditions;
using AutoPayout.Conditions.Constants;
using AutoPayout.EarlyResult.Services;
using AutoPayout.Exceptions;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services;
public class CreateConditionChainServiceTests
{
    private readonly CreateConditionChainService service;

    public CreateConditionChainServiceTests()
    {
        var creator = Substitute.For<Creator>();

        service = new CreateConditionChainServiceImp(creator);
    }

    [Fact]
    public void CreateConditionChain_ConditionIdsIsEmpty_ThrowException()
    {
        var exception = Record.Exception(() => service.CreateConditionChain(Array.Empty<string>()));

        Assert.IsType<ServiceException>(exception);
        Assert.Equal(ErrorMessage.MatchNotValid, (exception as ServiceException)?.Description);
    }

    [Fact]
    public void CreateConditionChain_ConditionIdsNotEmpty_ReturnCorrectly()
    {
        var conditionIds = new[] { ConditionId.VerifyConfirmScore, ConditionId.VerifyFinalResult };
        var conditions = service.CreateConditionChain(conditionIds);

        Assert.Equal(2, conditions.Count);
    }
}
