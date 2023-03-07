namespace ProjectHorizon.TestingSetup.Orchestrator.Data
{
    public static class State
    {
        public const string Creating = nameof(Creating);
        public const string ReadyForTesting = nameof(ReadyForTesting);
        public const string TestingInProgress = nameof(TestingInProgress);
        public const string TestingDone = nameof(TestingDone);
    }
}