using System;
using Microsoft.Xrm.Sdk;

namespace CaseEscalation.Helpers
{
    public static class AuditLogger
    {
        public static void WriteEscalationRecord(IOrganizationService service, ITracingService tracingService, Entity caseRecord)
        {
            var log = new Entity("new_caseescalationlog")
            {
                ["new_name"] = $"Escalation - {caseRecord.GetAttributeValue<string>("title")}",
                ["new_caseid"] = caseRecord.ToEntityReference(),
                ["new_escalationdate"] = DateTime.UtcNow,
                ["new_reason"] = "SLA due date breached while case remained open."
            };

            try
            {
                service.Create(log);
                tracingService.Trace("Escalation audit record created for case {0}.", caseRecord.Id);
            }
            catch (Exception ex)
            {
                tracingService.Trace("Failed to write escalation audit record: {0}", ex.Message);
            }
        }
    }
}
