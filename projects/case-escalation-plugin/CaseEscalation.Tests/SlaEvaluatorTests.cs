using System;
using CaseEscalation.Helpers;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace CaseEscalation.Tests
{
    public class FakeTracingService : ITracingService
    {
        public void Trace(string format, params object[] args) { }
    }

    public class SlaEvaluatorTests
    {
        private readonly ITracingService _tracing = new FakeTracingService();

        [Fact]
        public void IsBreached_ReturnsFalse_WhenCaseIsClosed()
        {
            var caseRecord = new Entity("incident")
            {
                ["statuscode"] = new OptionSetValue(6),
                ["new_sladuedate"] = DateTime.UtcNow.AddDays(-5)
            };

            Assert.False(SlaEvaluator.IsBreached(caseRecord, _tracing));
        }

        [Fact]
        public void IsBreached_ReturnsFalse_WhenNoSlaDueDateSet()
        {
            var caseRecord = new Entity("incident")
            {
                ["statuscode"] = new OptionSetValue(1)
            };

            Assert.False(SlaEvaluator.IsBreached(caseRecord, _tracing));
        }

        [Fact]
        public void IsBreached_ReturnsFalse_WhenSlaDueDateIsInFuture()
        {
            var caseRecord = new Entity("incident")
            {
                ["statuscode"] = new OptionSetValue(1),
                ["new_sladuedate"] = DateTime.UtcNow.AddHours(2)
            };

            Assert.False(SlaEvaluator.IsBreached(caseRecord, _tracing));
        }

        [Fact]
        public void IsBreached_ReturnsTrue_WhenOpenAndSlaDueDateInPast()
        {
            var caseRecord = new Entity("incident")
            {
                ["statuscode"] = new OptionSetValue(1),
                ["new_sladuedate"] = DateTime.UtcNow.AddHours(-2)
            };

            Assert.True(SlaEvaluator.IsBreached(caseRecord, _tracing));
        }
    }
}
