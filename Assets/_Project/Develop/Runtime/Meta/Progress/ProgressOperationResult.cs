namespace Assets._Project.Develop.Runtime.Meta.Progress
{
    public enum ProgressOperationStatus
    {
        Completed,
        SavedInMemoryOnly,
        InsufficientGold,
        Unavailable,
        Busy,
        Failed
    }

    public readonly struct ProgressOperationResult
    {
        public ProgressOperationStatus Status { get; }
        public int GoldDelta { get; }

        public ProgressOperationResult(ProgressOperationStatus status, int goldDelta = 0)
        {
            Status = status;
            GoldDelta = goldDelta;
        }
    }
}
