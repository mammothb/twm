using Twm.Domain.Tree;

namespace Twm.Adapters.Windows.Diagnostics;

/// <summary>
/// Per-window diagnostic projection: the owning process and the owner's
/// process/class. Carries no information the <c>WindowFilter</c> uses, it
/// exists solely to make <c>twm --dump</c> output actionable when twm and
/// komorebi disagree on a window. Populated lazily by
/// <see cref="WindowsWindowSystem.DescribeDiagnostics" /> so the hot path
/// (WinEvent-driven <c>Describe</c>, startup <c>EnumerateWindows</c>) does
/// not pay the <c>OpenProcess</c> + <c>QueryFullProcessImageName</c> cost.
/// </summary>
public sealed record WindowDiagnostic(
    WindowId Id,
    int ProcessId,
    string? ProcessName,
    string? OwnerClass,
    string? OwnerProcessName
);
