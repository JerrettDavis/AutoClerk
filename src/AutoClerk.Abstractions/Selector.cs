// csharp

namespace AutoClerk.Abstractions;

/// <summary>
/// Playwright‑style selector used by drivers to compose UI Automation queries.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> This lightweight, immutable value describes how to locate an element.
/// Drivers translate it into platform‑specific queries (e.g., UIA conditions for FlaUI).
/// </para>
/// <para>
/// <b>Fields and combination:</b> Multiple fields may be specified and are typically combined
/// conjunctively (logical AND) by the driver. For example, specifying both <see cref="AutomationId"/>
/// and <see cref="Kind"/> narrows the search to that automation id and control category.
/// </para>
/// <para>
/// <b>Immutability:</b> As a record, this type is immutable and safe to reuse or log. Prefer
/// functional updates via C\# <c>with</c> expressions when refining a selector per step:
/// <code>
/// var s = Selector.Id("UserName") with { Kind = ControlKind.TextBox };
/// </code>
/// </para>
/// <para>
/// <b>Stability guidance:</b> Prefer <see cref="AutomationId"/> for resilient tests.
/// <see cref="Name"/> can vary with localization or dynamic content. Use <see cref="Path"/> as a last resort
/// for complex hierarchies where ids are unavailable.
/// </para>
/// <para>
/// <b>XPath semantics:</b> <see cref="Path"/> is interpreted by the underlying driver (e.g., FlaUI XPath).
/// It is evaluated relative to the driver’s current search scope and may be combined with other fields,
/// subject to the driver’s translator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // By AutomationId only
/// var ok = Selector.Id("OkButton");
///
/// // By name and kind
/// var save = new Selector(Name: "Save", Kind: ControlKind.Button);
///
/// // Refine using C# 'with' expression
/// var input = Selector.Id("UserName") with { Kind = ControlKind.TextBox };
///
/// // XPath (driver‑specific)
/// var result = Selector.XPath("//*[@AutomationId='CalculatorResults']");
/// </code>
/// </example>
/// <seealso cref="ControlKind"/>
/// <seealso cref="IAppSession"/>
/// <seealso cref="ILocator"/>
/// <seealso cref="Expect"/>
public sealed record Selector(
    string? AutomationId = null,
    string? Name = null,
    ControlKind Kind = ControlKind.Any,
    string? Path = null
)
{
    /// <summary>
    /// Creates a selector constrained by <paramref name="id"/> as <see cref="AutomationId"/>.
    /// </summary>
    /// <param name="id">The automation id to match.</param>
    /// <returns>A selector with <see cref="AutomationId"/> set.</returns>
    public static Selector Id(string id) => new(AutomationId: id);

    /// <summary>
    /// Creates a selector constrained by exact <paramref name="name"/> as <see cref="Name"/>.
    /// </summary>
    /// <param name="name">The exact control name to match.</param>
    /// <returns>A selector with <see cref="Name"/> set.</returns>
    public static Selector NameIs(string name) => new(Name: name);

    /// <summary>
    /// Creates a selector constrained by control category <paramref name="kind"/>.
    /// </summary>
    /// <param name="kind">The logical control kind (e.g., \`Button\`, \`TextBox\`).</param>
    /// <returns>A selector with <see cref="Kind"/> set.</returns>
    public static Selector KindIs(ControlKind kind) => new(Kind: kind);

    /// <summary>
    /// Creates a selector using a driver‑specific path expression (e.g., FlaUI XPath).
    /// </summary>
    /// <param name="path">An XPath‑like expression evaluated from the current scope.</param>
    /// <returns>A selector with <see cref="Path"/> set.</returns>
    public static Selector XPath(string path) => new(Path: path);
}