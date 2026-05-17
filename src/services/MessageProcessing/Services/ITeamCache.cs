namespace MessageProcessing.Services;

public interface ITeamCache
{
    IReadOnlyList<TeamEntry> Teams { get; }
}
