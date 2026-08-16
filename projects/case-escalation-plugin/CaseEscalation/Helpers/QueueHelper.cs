using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CaseEscalation.Helpers
{
    public static class QueueHelper
    {
        private const string EscalationQueueName = "Case Escalation Queue";

        public static EntityReference GetEscalationQueueRef(IOrganizationService service, ITracingService tracingService)
        {
            var query = new QueryExpression("queue")
            {
                ColumnSet = new ColumnSet(false),
                TopCount = 1,
                Criteria = new FilterExpression(LogicalOperator.And)
            };
            query.Criteria.AddCondition("name", ConditionOperator.Equal, EscalationQueueName);

            var result = service.RetrieveMultiple(query);
            var queue = result.Entities.FirstOrDefault();

            if (queue == null)
            {
                tracingService.Trace("Escalation queue '{0}' not found. Owner will not be reassigned.", EscalationQueueName);
                return null;
            }

            return queue.ToEntityReference();
        }
    }
}
