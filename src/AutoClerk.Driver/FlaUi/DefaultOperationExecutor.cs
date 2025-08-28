using AutoClerk.Abstractions;

namespace AutoClerk.Driver.FlaUi;

/// <summary>
/// Minimal, data-driven operation executor for the AutoClerk DSL.
/// </summary>
/// <param name="session">
/// The live <see cref="IAppSession"/> used to resolve and interact with UI elements
/// during step execution.
/// </param>
/// <remarks>
/// <para>
/// The executor interprets <see cref="OperationDefinition"/> instances produced from DSL
/// artifacts (e.g., YAML/JSON/XML) and runs their <see cref="OperationStep"/> list sequentially.
/// Built-in verbs:
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Action</term><description>Behavior</description>
///   </listheader>
///   <item>
///     <term><c>click</c></term>
///     <description>Clicks the element at <see cref="OperationStep.Target"/>.</description>
///   </item>
///   <item>
///     <term><c>type</c></term>
///     <description>Types text into <see cref="OperationStep.Target"/>. Requires <c>Args.text</c>.</description>
///   </item>
///   <item>
///     <term><c>expectVisible</c></term>
///     <description>Waits until <see cref="OperationStep.Target"/> is visible; throws on timeout.</description>
///   </item>
///   <item>
///     <term><c>wait</c></term>
///     <description>Pauses for a duration. Uses <c>Args.ms</c> (milliseconds, default 250).</description>
///   </item>
/// </list>
/// <para>
/// The executor is intentionally minimal; extend it with additional verbs (e.g., <c>press</c>,
/// <c>select</c>, <c>hover</c>, <c>close</c>) to suit your DSL. For complex flows, consider
/// composing higher-level steps that encapsulate multiple primitive actions.
/// </para>
/// <para><b>Failure &amp; cancellation:</b> An unrecognized action results in
/// <see cref="NotSupportedException"/>; missing/invalid arguments result in
/// <see cref="ArgumentException"/>. Pass a <see cref="CancellationToken"/> to abort between steps.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var op = new OperationDefinition(
///     "Smoke",
///     new[]
///     {
///         new OperationStep("type",  Selector.Id("UserName").With(kind: ControlKind.TextBox), new() { ["text"] = "jane" }),
///         new OperationStep("type",  Selector.Id("Password").With(kind: ControlKind.TextBox), new() { ["text"] = "secret" }),
///         new OperationStep("click", Selector.Id("Login").With(kind: ControlKind.Button)),
///         new OperationStep("expectVisible", new Selector(Name: "Welcome", Kind: ControlKind.Label)),
///         new OperationStep("wait", Selector.KindIs(ControlKind.Window), new() { ["ms"] = "150" })
///     });
///
/// var exec = new DefaultOperationExecutor(session);
/// await exec.ExecuteAsync(op, ct);
/// </code>
/// </example>
/// <seealso cref="OperationDefinition"/>
/// <seealso cref="OperationStep"/>
/// <seealso cref="IAppSession"/>
public sealed class DefaultOperationExecutor(IAppSession session) : IOperationExecutor
{
    /// <summary>
    /// Executes the specified operation step-by-step using the bound <see cref="IAppSession"/>.
    /// </summary>
    /// <param name="op">The parsed operation definition to execute.</param>
    /// <param name="ct">A token to observe for cancellation between steps.</param>
    /// <returns>A task that completes when the operation finishes or fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="op"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown for malformed steps (e.g., missing required arguments).</exception>
    /// <exception cref="NotSupportedException">Thrown if a step action is not recognized.</exception>
    /// <exception cref="TimeoutException">Propagated from underlying waits/interactions on timeout.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled.</exception>
    public async Task ExecuteAsync(
        OperationDefinition op,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(op);

        foreach (var step in op.Steps)
        {
            ct.ThrowIfCancellationRequested();

            var action = step.Action.ToLowerInvariant();
            switch (action)
            {
                case "click":
                    await session.ClickAsync(step.Target, ct).ConfigureAwait(false);
                    break;

                case "type":
                {
                    if (step.Args is null || !step.Args.TryGetValue("text", out var text))
                        throw new ArgumentException($"Step '{op.Name}': 'type' requires Args.text");
                    await session.TypeAsync(step.Target, text, ct).ConfigureAwait(false);
                }
                    break;

                case "expectvisible":
                {
                    var el = await session.QueryAsync(step.Target, ct: ct).ConfigureAwait(false);
                    await el.WaitForVisibleAsync(ct: ct).ConfigureAwait(false);
                }
                    break;

                case "wait":
                {
                    var ms = step.Args is not null && step.Args.TryGetValue("ms", out var s) && int.TryParse(s, out var v) ? v : 250;
                    await Task.Delay(ms, ct).ConfigureAwait(false);
                }
                    break;

                default:
                    throw new NotSupportedException($"Unknown action '{step.Action}'");
            }
        }
    }
}