using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using CaseEscalation.Helpers;

namespace CaseEscalation.Plugins
{
    public class CaseEscalationPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("CaseEscalationPlugin: Execute start.");

            if (context.MessageName.ToLowerInvariant() != "update")
            {
                tracingService.Trace("Skipping: message is not Update.");
                return;
            }

            if (!(context.InputParameters.Contains("Target") &&
                  context.InputParameters["Target"] is Entity))
            {
                tracingService.Trace("Skipping: Target is not an Entity.");
                return;
            }

            var targetCase = (Entity)context.InputParameters["Target"];

            if (targetCase.LogicalName != "incident")
            {
                tracingService.Trace("Skipping: target entity is not incident.");
                return;
            }

            if (context.Depth > 1)
            {
                tracingService.Trace("Skipping: recursive execution guard (Depth > 1).");
                return;
            }

            try
            {
                ProcessEscalation(service, tracingService, targetCase);
            }
            catch (Exception ex)
            {
                tracingService.Trace("CaseEscalationPlugin error: {0}", ex.ToString());
                throw new InvalidPluginExecutionException(
                    "CaseEscalationPlugin failed while evaluating SLA escalation. See trace log for details.", ex);
            }

            tracingService.Trace("CaseEscalationPlugin: Execute end.");
        }

        private void ProcessEscalation(IOrganizationService service, ITracingService tracingService, Entity targetCase)
        {
            var caseId = targetCase.Id;
            var fullCase = service.Retrieve("incident", caseId, new ColumnSet(
                "title", "statuscode", "prioritycode", "ownerid", "new_sladuedate"));

            if (!SlaEvaluator.IsBreached(fullCase, tracingService))
            {
                tracingService.Trace("SLA not breached or case is closed. No escalation needed.");
                return;
            }

            tracingService.Trace("SLA breach detected for case {0}. Escalating.", caseId);

            var escalationUpdate = new Entity("incident", caseId);

            var currentPriority = fullCase.GetAttributeValue<OptionSetValue>("prioritycode");
            if (currentPriority == null || currentPriority.Value != 1)
            {
                escalationUpdate["prioritycode"] = new OptionSetValue(1);
            }

            var escalationQueue = QueueHelper.GetEscalationQueueRef(service, tracingService);
            if (escalationQueue != null)
            {
                escalationUpdate["ownerid"] = escalationQueue;
            }

            if (escalationUpdate.Attributes.Count > 0)
            {
                service.Update(escalationUpdate);
                tracingService.Trace("Case {0} updated: priority escalated, owner reassigned.", caseId);
            }

            AuditLogger.WriteEscalationRecord(service, tracingService, fullCase);
        }
    }
}
