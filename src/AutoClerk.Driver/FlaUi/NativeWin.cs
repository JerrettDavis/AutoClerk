using System.Runtime.InteropServices;

namespace AutoClerk.Driver.FlaUi;

/// <summary>
/// Minimal Win32 interop used to request graceful window teardown from UI processes.
/// </summary>
/// <remarks>
/// <para>
/// This helper is intentionally tiny and focused: it exposes <see cref="WM_CLOSE"/> and a P/Invoke for
/// <see cref="SendMessage(IntPtr,int,IntPtr,IntPtr)"/> to request that a window close itself. This is preferable to
/// terminating a process outright because it allows the application to run its normal close pipeline
/// (e.g., save prompts, cleanup handlers).
/// </para>
/// <para>
/// <b>Semantics</b>:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="WM_CLOSE"/> asks the target window to close. The window may ignore the request.</description></item>
///   <item><description><see cref="SendMessage(IntPtr,int,IntPtr,IntPtr)"/> is synchronous; the call blocks until the
///   target window procedure returns for that message.</description></item>
///   <item><description>For fire-and-forget shutdown requests, <c>PostMessage</c> could be used instead; this utility
///   uses <c>SendMessage</c> to provide a stronger close attempt and predictable sequencing.</description></item>
/// </list>
/// <para>
/// <b>Platform</b>: Windows only. Ensure you guard calls by platform checks if you share code across OSes.
/// </para>
/// <para>
/// <b>Usage</b>: Prefer sending <see cref="WM_CLOSE"/> after attempting a graceful close via automation patterns
/// (e.g., WindowPattern.Close), and only consider terminating the process as a last resort.
/// </para>
/// </remarks>
internal static class NativeWin
{
    /// <summary>
    /// Win32 message that requests a window to close (equivalent to a user clicking the window’s Close button).
    /// </summary>
    public const int WM_CLOSE = 0x0010;

    /// <summary>
    /// Sends the specified message to a window or windows; in this context, often used to send <see cref="WM_CLOSE"/>.
    /// </summary>
    /// <param name="hWnd">A handle to the target window.</param>
    /// <param name="msg">The message identifier (e.g., <see cref="WM_CLOSE"/>).</param>
    /// <param name="wParam">Additional message–specific information (unused for <see cref="WM_CLOSE"/>, pass <see cref="IntPtr.Zero"/>).</param>
    /// <param name="lParam">Additional message–specific information (unused for <see cref="WM_CLOSE"/>, pass <see cref="IntPtr.Zero"/>).</param>
    /// <returns>
    /// A message-dependent result. For <see cref="WM_CLOSE"/>, the return value is not typically used by callers.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Synchronous</b>: This call blocks until the target window procedure processes the message and returns.
    /// If the target thread is hung, the call can block for an extended period. Callers should enforce their own
    /// time budgets around this interop (e.g., by racing with a timeout and proceeding with alternative teardown).
    /// </para>
    /// <para>
    /// <b>Threading</b>: Can be called from any thread; the OS delivers the message to the target window’s UI thread.
    /// </para>
    /// <para>
    /// <b>Error handling</b>: When <paramref name="hWnd"/> is invalid, the behavior is undefined. Ensure the handle is
    /// non-zero and still valid before calling.
    /// </para>
    /// </remarks>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}