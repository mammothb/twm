namespace Twm.Core.Tree;

/// <summary>
/// Opaque identifier for an OS window. The platform layer maps this to a native
/// handle (HWND); the core never interprets the value.
/// </summary>
public readonly record struct WindowId(nint Value);
