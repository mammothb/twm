using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Twm.Platform.Windows;

// Undocumented Windows shell COM used to cloak/uncloak foreign windows.
//
// Only the vtable slots up to the called method matter, so methods before it
// are placeholders (never invoked); their exact signatures are irrelevant to
// dispatch, only their count/order.

/// <summary>
/// Standard OLE <c>IServiceProvider</c> (renamed to avoid the BCL type of the
/// same name).
/// </summary>
[GeneratedComInterface]
[Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
internal partial interface IShellServiceProvider
{
    [PreserveSig]
    int QueryService(in Guid service, in Guid riid, out IApplicationViewCollection collection);
}

[GeneratedComInterface]
[Guid("1841C6D7-4F9D-42C0-AF41-8747538F10E5")]
internal partial interface IApplicationViewCollection
{
    // slot 3
    [PreserveSig]
    int GetViews(out nint views);

    // slot 4
    [PreserveSig]
    int GetViewsByZOrder(out nint views);

    // slot 5
    [PreserveSig]
    int GetViewsByAppUserModelId(nint id, out nint views);

    // slot 6, called
    [PreserveSig]
    int GetViewForHwnd(nint window, out IApplicationView? view);
}

[GeneratedComInterface]
[Guid("372E1D3B-38D3-42E4-A15B-8AB2B178F513")]
internal partial interface IApplicationView
{
    // IInspectable (slots 3-5) + IApplicationView methods preceding SetCloak
    // (slots 6-11). Present only to place SetCloak at vtable slot 12
    [PreserveSig]
    int GetIids();

    [PreserveSig]
    int GetRuntimeClassName();

    [PreserveSig]
    int GetTrustLevel();

    [PreserveSig]
    int SetFocus();

    [PreserveSig]
    int SwitchTo();

    [PreserveSig]
    int TryInvokeBack();

    [PreserveSig]
    int GetThumbnailWindow();

    [PreserveSig]
    int GetMonitor();

    [PreserveSig]
    int GetVisibility();

    // slot 12, called
    [PreserveSig]
    int SetCloak(int cloakType, int flags);
}

/// <summary>
/// Cloaks and uncloaks foreign windows. All calls must run on the WM's
/// message-loop thread (the COM aparment is initialized and the collection
/// cached for that thread). Failures are swallowed (logged by callers if
/// needed) rather than crashing the WM.
/// </summary>
internal static partial class ImmersiveShell
{
    private static readonly Guid s_clsidImmersiveShell = new(
        "C2F03A33-21F5-47FA-B4BB-156362A2F239"
    );
    private static readonly Guid s_iidServiceProvider = new("6D5140C1-7436-11CE-8034-00AA006009FA");
    private static readonly Guid s_iidApplicationViewCollection = new(
        "1841C6D7-4F9D-42C0-AF41-8747538F10E5"
    );
    private static readonly StrategyBasedComWrappers s_comWrappers = new();

    // CLSCTX_LOCAL_SERVER
    private const uint ClsctxLocalServer = 0x4;

    // COINIT_APARTMENTTHREADED
    private const uint CoinitApartmentThreaded = 0x2;

    private const int CloakTypeApplication = 1;
    private const int CloakFlagCloak = 2;
    private const int CloakFlagUncloak = 0;

    private static IApplicationViewCollection? s_collection;

    [LibraryImport("ole32.dll")]
    private static partial int CoInitializeEx(nint pvReserved, uint dwCoInit);

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(
        in Guid rclsid,
        nint pUnkOuter,
        uint dwClsContext,
        in Guid riid,
        out nint ppv
    );

    internal static void Cloak(nint window) => SetCloak(window, CloakFlagCloak);

    internal static void Uncloak(nint window) => SetCloak(window, CloakFlagUncloak);

    private static void SetCloak(nint window, int flag)
    {
        if (Collection() is not IApplicationViewCollection collection)
        {
            return;
        }

        if (collection.GetViewForHwnd(window, out IApplicationView? view) == 0 && view is not null)
        {
            view.SetCloak(CloakTypeApplication, flag);
        }
    }

    private static IApplicationViewCollection? Collection()
    {
        if (s_collection is IApplicationViewCollection cached)
        {
            return cached;
        }

        _ = CoInitializeEx(0, CoinitApartmentThreaded);

        Guid serviceProviderIid = s_iidServiceProvider;
        if (
            CoCreateInstance(
                s_clsidImmersiveShell,
                0,
                ClsctxLocalServer,
                serviceProviderIid,
                out nint providerPtr
            ) != 0
            || providerPtr == 0
        )
        {
            return null;
        }
        var provider = (IShellServiceProvider)
            s_comWrappers.GetOrCreateObjectForComInstance(providerPtr, CreateObjectFlags.None);
        Marshal.Release(providerPtr);

        Guid collectionIid = s_iidApplicationViewCollection;
        if (
            provider.QueryService(
                collectionIid,
                collectionIid,
                out IApplicationViewCollection collection
            ) != 0
        )
        {
            return null;
        }

        s_collection = collection;
        return collection;
    }
}
