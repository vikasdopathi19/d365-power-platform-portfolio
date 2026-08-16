using System;
using Microsoft.Xrm.Sdk;

namespace CaseEscalation.Helpers
{
    public static class SlaEvaluator
    {
        private static readonly int[] ClosedStatusCodes = { 5, 6 };

        public static bool IsBreached(Entity caseRecord, ITracingService tracingService)
        {
            var statusCode = caseRecord.GetAttributeValue<OptionSetValue>("statuscode");
            if (statusCode != null && Array.IndexOf(ClosedStatusCodes, statusCode.Value) >= 0)
            {
                tracingService.Trace("Case is closed (statuscode={0}). Not eligible for escalation.", statusCode.Value);
                return false;
            }

            var slaDueDate = caseRecord.GetAttributeValue<DateTime?>("new_sladuedate");
            if (slaDueDate == null)
            {
                tracingService.Trace("No SLA due date set. Not eligible for escalation.");
                return false;
            }

            var isBreached = slaDueDate.Value.ToUniversalTime() < DateTime.UtcNow;
            tracingService.Trace("SLA due (UTC): {0}, Now (UTC): {1}, Breached: {2}",
                slaDueDate.Value.ToUniversalTime(), DateTime.UtcNow, isBreached);

            return isBreached;
        }
    }
}
