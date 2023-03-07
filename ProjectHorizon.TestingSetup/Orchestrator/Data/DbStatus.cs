using System;

namespace ProjectHorizon.TestingSetup.Orchestrator.Data
{
    public class DbStatus
    {
        public Guid Id { get; set; }

        public string State { get; set; }

        public DateTimeOffset DateTime { get; set; }

        public string DbName => $"z-testing-{Id}";
    }
}