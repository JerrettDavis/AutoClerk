namespace AutoClerk.Abstractions;

/// <summary>
/// Logical control categories used by <see cref="Selector"/> to constrain UI queries.
/// </summary>
/// <remarks>
/// <para>
/// These values are driver-agnostic. A concrete driver (e.g., a Windows UIAutomation provider)
/// maps them to platform-specific types. For example, the FlaUI/UIA mapping typically is:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Window"/> → UIA <c>ControlType.Window</c></description></item>
///   <item><description><see cref="Button"/> → UIA <c>ControlType.Button</c></description></item>
///   <item><description><see cref="TextBox"/> → UIA <c>ControlType.Edit</c></description></item>
///   <item><description><see cref="ComboBox"/> → UIA <c>ControlType.ComboBox</c></description></item>
///   <item><description><see cref="List"/> → UIA <c>ControlType.List</c></description></item>
///   <item><description><see cref="ListItem"/> → UIA <c>ControlType.ListItem</c></description></item>
///   <item><description><see cref="Tab"/> → UIA <c>ControlType.Tab</c></description></item>
///   <item><description><see cref="CheckBox"/> → UIA <c>ControlType.CheckBox</c></description></item>
///   <item><description><see cref="RadioButton"/> → UIA <c>ControlType.RadioButton</c></description></item>
///   <item><description><see cref="MenuItem"/> → UIA <c>ControlType.MenuItem</c></description></item>
///   <item><description><see cref="DataGrid"/> → UIA <c>ControlType.DataGrid</c></description></item>
///   <item><description><see cref="Label"/> → UIA <c>ControlType.Text</c></description></item>
/// </list>
/// <para>
/// Use <see cref="Any"/> when you do not wish to constrain by control type (e.g., when searching by
/// <see cref="Selector.AutomationId"/> alone).
/// </para>
/// <para>
/// Example:
/// <code>
/// var login = app.Locator(factory, Selector.Id("LoginButton") with { Kind = ControlKind.Button });
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="Selector"/>
/// <seealso cref="ILocator"/>
/// <seealso cref="IAppSession"/>
public enum ControlKind
{
    /// <summary>
    /// No control-type filter. The driver will not restrict queries by platform control category.
    /// </summary>
    Any = 0,

    /// <summary>
    /// A top-level or child window surface (e.g., application window, dialog).
    /// </summary>
    Window,

    /// <summary>
    /// An actionable push button or command button.
    /// </summary>
    Button,

    /// <summary>
    /// A single-line or multi-line editable text control.
    /// </summary>
    /// <remarks>
    /// Typically mapped to UIA <c>Edit</c>. For read-only text, prefer <see cref="Label"/>.
    /// </remarks>
    TextBox,

    /// <summary>
    /// A combo box / drop-down selection control.
    /// </summary>
    ComboBox,

    /// <summary>
    /// A list container hosting a collection of items.
    /// </summary>
    List,

    /// <summary>
    /// An item within a list, tree, or similar container.
    /// </summary>
    ListItem,

    /// <summary>
    /// A tab strip control hosting pages or panels.
    /// </summary>
    Tab,

    /// <summary>
    /// A two-state (or tri-state) check box control.
    /// </summary>
    CheckBox,

    /// <summary>
    /// A mutually exclusive radio button control.
    /// </summary>
    RadioButton,

    /// <summary>
    /// A menu item (context menu, menu bar, or sub-menu entry).
    /// </summary>
    MenuItem,

    /// <summary>
    /// A data grid or table control with rows and columns.
    /// </summary>
    DataGrid,

    /// <summary>
    /// A static, read-only text label.
    /// </summary>
    /// <remarks>
    /// Typically mapped to UIA <c>Text</c>. For editable text, use <see cref="TextBox"/>.
    /// </remarks>
    Label
}
