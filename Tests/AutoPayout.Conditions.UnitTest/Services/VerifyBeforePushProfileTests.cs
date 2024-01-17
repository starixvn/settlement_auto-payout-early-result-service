using AutoPayout.Conditions.Constants;
using AutoPayout.Exceptions;
using AutoPayout.Models;
using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.ScaleOut.Models;
using Fanex.Settlement.ScaleOut.Request;
using Fanex.Settlement.ScaleOut.Services;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyBeforePushProfileTests
{
    private readonly ScaleOutSettleService scaleOutSettleService;

    private readonly VerifyBeforePushProfile condition;

    private readonly Match match = new()
    {
        MatchId = 1,
        ResultDetails = new List<ResultDetail>
        {
            new ()
            {
                ResourceId = "01"
            }
        }
    };

    public VerifyBeforePushProfileTests()
    {
        scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        var xmlConvertService = Substitute.For<XmlConvertService>();
        condition = new VerifyBeforePushProfile(scaleOutSettleService, xmlConvertService);
    }

    [Fact]
    public async Task HandleCondition_ValidatePassed_DoNotThrowAnything()
    {
        var result = new ResultMessage();
        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(result);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.Null(exception);
    }

    [Fact]
    public async Task HandleCondition_ValidateReturnDeploymentError_ThrowExceptionWithMarkFail()
    {
        var result = new ResultMessage
        {
            ErrorCode = ErrorCode.DeploymentError
        };
        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(result);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.IsType<ServiceException>(exception);
        Assert.False(((ServiceException)exception).MarkFail);
    }

    [Fact]
    public async Task HandleCondition_ValidateReturnMatchInQueue_ThrowExceptionWithRetry()
    {
        var result = new ResultMessage
        {
            ErrorCode = ErrorCode.MatchInQueue
        };
        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(result);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.IsType<ServiceException>(exception);
        Assert.True(((ServiceException)exception).NeedRetry);
        Assert.Equal(ErrorMessage.MatchInQueue, ((ServiceException)exception).Description);
    }

    [Fact]
    public async Task HandleCondition_ValidateReturnAnotherError_ThrowExceptionWithMessage()
    {
        var result = new ResultMessage
        {
            ErrorCode = 1,
            ErrorMessage = "message"
        };
        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(result);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.IsType<ServiceException>(exception);
        Assert.True(((ServiceException)exception).NeedRetry == false && ((ServiceException)exception).MarkFail);

        Assert.Equal("message", ((ServiceException)exception).Description);
    }
}