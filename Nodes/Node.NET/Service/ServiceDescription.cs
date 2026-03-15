namespace Eva.Node.Service;

public record ServiceDescription(
        string Name,
        string[]? Authorization = null,
        string Class = "",
        string DisplayName = "Default display name",
        string Description = "Default description",
        string Version = "1.0.0",
        string Author = "No author",
        string License = "No license"
);