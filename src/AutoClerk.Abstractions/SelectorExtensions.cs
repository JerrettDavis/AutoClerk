namespace AutoClerk.Abstractions;

/// <summary>
/// Convenience helpers for composing <see cref="Selector"/> instances fluently.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Selector"/> is an immutable record. These helpers return a new instance with the requested
/// modifications, leaving the original unchanged. Prefer this over manual construction at the call site
/// to keep selectors readable and intention-revealing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Add/override fields fluently:
/// var login = Selector.Id("LoginButton").With(kind: ControlKind.Button);
///
/// // Combine with record "with" when useful:
/// var editor = Selector.NameIs("Editor") with { Kind = ControlKind.TextBox };
/// </code>
/// </example>
/// <seealso cref="Selector"/>
/// <seealso cref="ControlKind"/>
public static class SelectorExtensions
{
    /// <summary>
    /// Returns a copy of the selector with any provided fields overridden.
    /// </summary>
    /// <param name="s">The source selector.</param>
    /// <param name="automationId">Optional override for <see cref="Selector.AutomationId"/>.</param>
    /// <param name="name">Optional override for <see cref="Selector.Name"/>.</param>
    /// <param name="kind">Optional override for <see cref="Selector.Kind"/>.</param>
    /// <param name="path">Optional override for <see cref="Selector.Path"/> (e.g., XPath/driver-specific path).</param>
    /// <returns>A new <see cref="Selector"/> with the specified overrides applied.</returns>
    /// <remarks>
    /// Fields that are passed as <c>null</c> retain their original values from <paramref name="s"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="s"/> is <c>null</c>.</exception>
    public static Selector With(
        this Selector s,
        string? automationId = null,
        string? name = null,
        ControlKind? kind = null,
        string? path = null)
    {
        ArgumentNullException.ThrowIfNull(s);

        return new Selector(
            AutomationId: automationId ?? s.AutomationId,
            Name: name ?? s.Name,
            Kind: kind ?? s.Kind,
            Path: path ?? s.Path
        );
    }
}