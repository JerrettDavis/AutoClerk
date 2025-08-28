using AutoClerk.Abstractions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;

namespace AutoClerk.Driver.FlaUi;

/// <summary>
/// Translates driver-agnostic <see cref="Selector"/>s into FlaUI/UIA conditions and performs scoped queries.
/// </summary>
/// <remarks>
/// <para>
/// If <see cref="Selector.Path"/> is provided, XPath takes precedence and is used directly. Otherwise,
/// a compound condition is built from <see cref="Selector.AutomationId"/>, <see cref="Selector.Name"/>,
/// and <see cref="Selector.Kind"/> (mapped to UIA <see cref="ControlType"/>).
/// </para>
/// <para>
/// The translator is stateless and thread-safe.
/// </para>
/// </remarks>
internal static class SelectorTranslator
{
    /// <summary>
    /// Finds the first descendant of <paramref name="scope"/> that matches <paramref name="s"/>.
    /// </summary>
    /// <param name="scope">The search root (typically a window or desktop).</param>
    /// <param name="cf">A FlaUI <see cref="ConditionFactory"/> used to compose UIA conditions.</param>
    /// <param name="s">The selector to translate.</param>
    /// <returns>
    /// The first matching <see cref="AutomationElement"/>, or <c>null</c> if none is found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="scope"/>, <paramref name="cf"/>, or <paramref name="s"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// When <see cref="Selector.Path"/> is non-empty, the search uses <see cref="AutomationElement.FindFirstByXPath(string)"/>.
    /// Otherwise it composes a condition with <see cref="BuildCondition(ConditionFactory, Selector)"/> and calls
    /// <see cref="AutomationElement.FindFirstDescendant(ConditionBase)"/>.
    /// </remarks>
    public static AutomationElement? FindFirst(AutomationElement scope, ConditionFactory cf, Selector s)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(cf);
        ArgumentNullException.ThrowIfNull(s);

        if (!string.IsNullOrWhiteSpace(s.Path))
            return scope.FindFirstByXPath(s.Path!);

        var cond = BuildCondition(cf, s);
        return scope.FindFirstDescendant(cond);
    }

    /// <summary>
    /// Finds all descendants of <paramref name="scope"/> that match <paramref name="s"/>.
    /// </summary>
    /// <param name="scope">The search root (typically a window or desktop).</param>
    /// <param name="cf">A FlaUI <see cref="ConditionFactory"/> used to compose UIA conditions.</param>
    /// <param name="s">The selector to translate.</param>
    /// <returns>
    /// A read-only list of matching <see cref="AutomationElement"/> instances (empty if none).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="scope"/>, <paramref name="cf"/>, or <paramref name="s"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// When <see cref="Selector.Path"/> is non-empty, the search uses <see cref="AutomationElement.FindAllByXPath(string)"/>.
    /// Otherwise it composes a condition with <see cref="BuildCondition(ConditionFactory, Selector)"/> and calls
    /// <see cref="AutomationElement.FindAllDescendants(ConditionBase)"/>.
    /// </remarks>
    public static IReadOnlyList<AutomationElement> FindAll(AutomationElement scope, ConditionFactory cf, Selector s)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(cf);
        ArgumentNullException.ThrowIfNull(s);

        if (!string.IsNullOrWhiteSpace(s.Path))
            return scope.FindAllByXPath(s.Path!).ToList();

        var cond = BuildCondition(cf, s);
        return scope.FindAllDescendants(cond).ToList();
    }

    /// <summary>
    /// Builds a FlaUI/UIA condition from a driver-agnostic <see cref="Selector"/>.
    /// </summary>
    /// <param name="cf">The condition factory used to create property conditions.</param>
    /// <param name="s">The selector to translate.</param>
    /// <returns>
    /// A single <see cref="ConditionBase"/> that represents the selector. If the selector contains no fields,
    /// returns <see cref="TrueCondition.Default"/> (matches anything).
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cf"/> or <paramref name="s"/> is <c>null</c>.</exception>
    /// <remarks>
    /// The composed condition is a logical AND of any present fields:
    /// <list type="bullet">
    ///   <item><description><see cref="Selector.AutomationId"/> → <see cref="ConditionFactory.ByAutomationId(string, PropertyConditionFlags)"/></description></item>
    ///   <item><description><see cref="Selector.Name"/> → <see cref="ConditionFactory.ByName(string, PropertyConditionFlags)"/></description></item>
    ///   <item><description><see cref="Selector.Kind"/> → <see cref="ConditionFactory.ByControlType(ControlType)"/></description></item>
    /// </list>
    /// </remarks>
    private static ConditionBase BuildCondition(ConditionFactory cf, Selector s)
    {
        ArgumentNullException.ThrowIfNull(cf);
        ArgumentNullException.ThrowIfNull(s);

        var conds = new List<ConditionBase>();

        if (!string.IsNullOrWhiteSpace(s.AutomationId))
            conds.Add(cf.ByAutomationId(s.AutomationId!));

        if (!string.IsNullOrWhiteSpace(s.Name))
            conds.Add(cf.ByName(s.Name!));

        if (s.Kind != ControlKind.Any)
            conds.Add(cf.ByControlType(ToControlType(s.Kind)));

        // If you want to support class names later:
        // if (!string.IsNullOrWhiteSpace(s.ClassName))
        //     conds.Add(cf.ByClassName(s.ClassName!));

        return conds.Count switch
        {
            0 => TrueCondition.Default,
            1 => conds[0],
            _ => new AndCondition(conds.ToArray()),
        };
    }

    /// <summary>
    /// Maps a driver-agnostic <see cref="ControlKind"/> to a UIA <see cref="ControlType"/>.
    /// </summary>
    /// <param name="kind">The logical control kind.</param>
    /// <returns>The corresponding UIA control type (or <see cref="ControlType.Custom"/> as a fallback).</returns>
    private static ControlType ToControlType(ControlKind kind) => kind switch
    {
        ControlKind.Window => ControlType.Window,
        ControlKind.Button => ControlType.Button,
        ControlKind.TextBox => ControlType.Edit,
        ControlKind.ComboBox => ControlType.ComboBox,
        ControlKind.List => ControlType.List,
        ControlKind.ListItem => ControlType.ListItem,
        ControlKind.Tab => ControlType.Tab,
        ControlKind.CheckBox => ControlType.CheckBox,
        ControlKind.RadioButton => ControlType.RadioButton,
        ControlKind.MenuItem => ControlType.MenuItem,
        ControlKind.DataGrid => ControlType.DataGrid,
        ControlKind.Label => ControlType.Text,
        _ => ControlType.Custom
    };
}