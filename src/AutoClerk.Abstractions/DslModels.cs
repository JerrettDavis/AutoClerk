namespace AutoClerk.Abstractions;

public sealed record OperationStep(
    string Action, 
    Selector Target,
    Dictionary<string, string>? Args = null
);

public sealed record OperationDefinition(
    string Name,
    IReadOnlyList<OperationStep> Steps
);