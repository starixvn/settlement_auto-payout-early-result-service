using Fanex.AutoPayoutSettlement.Core.Models;

namespace Fanex.Library;

public interface Condition
{
    public Task HandleCondition(Match match);
}