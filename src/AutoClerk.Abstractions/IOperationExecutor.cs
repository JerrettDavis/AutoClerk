namespace AutoClerk.Abstractions;

/// <summary>
/// Executes a high-level, data-driven operation composed of multiple UI steps (the project’s DSL).
/// </summary>
/// <remarks>
/// <para>
/// An operation (see <see cref="OperationDefinition"/>) typically originates from a feature/spec file
/// (YAML/JSON/XML) and contains an ordered list of steps such as <c>click</c>, <c>type</c>,
/// <c>expectVisible</c>, etc. The executor is responsible for interpreting those steps and driving the
/// underlying <see cref="IAppSession"/> and/or <see cref="ILocator"/> APIs accordingly.
/// </para>
///
/// <para><b>Semantics</b></para>
/// <list type="bullet">
///   <item>
///     <description><b>Sequential execution:</b> Steps are executed in order; a failing step aborts the operation
///     unless the executor supports a user-defined continue-on-failure policy.</description>
///   </item>
///   <item>
///     <description><b>Auto-waiting:</b> Step implementations should leverage locator/session auto-waiting
///     (timeouts, polling) when interacting with the UI.</description>
///   </item>
///   <item>
///     <description><b>Determinism:</b> Callers should not rely on idempotency; if an operation is retried,
///     steps may run again unless guarded within the DSL.</description>
///   </item>
///   <item>
///     <description><b>Cancellation:</b> Passing a <see cref="CancellationToken"/> allows callers to stop a
///     long-running operation promptly between steps.</description>
///   </item>
/// </list>
///
/// <para><b>Error handling</b></para>
/// <list type="bullet">
///   <item>
///     <description>Unknown/unsupported step actions should result in <see cref="NotSupportedException"/>.</description>
///   </item>
///   <item>
///     <description>Malformed or incomplete step arguments should result in <see cref="ArgumentException"/>.</description>
///   </item>
///   <item>
///     <description>Timeouts from underlying expectations/interactions typically surface as <see cref="TimeoutException"/>.</description>
///   </item>
/// </list>
///
/// <para><b>Example</b></para>
/// <code>
/// var op = new OperationDefinition(
///     "LoginSmoke",
///     new[]
///     {
///         new OperationStep("type",  new Selector(AutomationId: "UserName", Kind: ControlKind.TextBox), new() { ["text"] = "jane" }),
///         new OperationStep("type",  new Selector(AutomationId: "Password", Kind: ControlKind.TextBox), new() { ["text"] = "secret" }),
///         new OperationStep("click", new Selector(AutomationId: "Login",    Kind: ControlKind.Button)),
///         new OperationStep("expectVisible", new Selector(Name: "Welcome, Jane", Kind: ControlKind.Label))
///     });
///
/// await executor.ExecuteAsync(op, ct);
/// </code>
/// </remarks>
/// <seealso cref="OperationDefinition"/>
/// <seealso cref="OperationStep"/>
/// <seealso cref="IAppSession"/>
/// <seealso cref="ILocator"/>
public interface IOperationExecutor
{
    /// <summary>
    /// Executes the specified operation end-to-end.
    /// </summary>
    /// <param name="op">The parsed operation definition containing an ordered list of steps.</param>
    /// <param name="ct">A token used to cancel execution between steps.</param>
    /// <returns>A task that completes when the operation finishes (successfully or with an exception).</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="op"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="op"/> or any step is malformed.</exception>
    /// <exception cref="NotSupportedException">Thrown when a step action is not recognized by the executor.</exception>
    /// <exception cref="TimeoutException">Thrown when a step fails due to exceeding its effective timeout.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    Task ExecuteAsync(OperationDefinition op, CancellationToken ct = default);
}