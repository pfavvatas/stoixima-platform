namespace MessageProcessing.Services;

public record TeamEntry(
    int      Id,
    string   Name,
    string[] Aliases,
    string?  Country,
    string?  League);
