namespace Etg.Yams.Storage
{
    public enum ConflictResolutionMode
    {
        DoNothingIfBinariesExist,
        FailIfBinariesExist,
        OverwriteExistingBinaries
    }
}