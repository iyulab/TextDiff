namespace TextDiff.Core;

public interface IChangeTracker
{
    void TrackChanges(DiffBlock block, ChangeStats stats);
}
