using System.Runtime.InteropServices;
namespace PhotoSortingApp.Data.Services;

internal static class WindowsFileMetadataWriter
{
    private const int RpcChangedMode = unchecked((int)0x80010106);
    private static readonly Guid PropertyStoreGuid = new("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
    private static readonly PropertyKey TitleKey = new(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), 2);
    private static readonly PropertyKey SubjectKey = new(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), 3);
    private static readonly PropertyKey CommentKey = new(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), 6);
    private static readonly PropertyKey KeywordsKey = new(new Guid("D5CDD505-2E9C-101B-9397-08002B2CF9AE"), 5);

    public static void TryWriteIdentityAndTags(
        string fullPath,
        IReadOnlyList<string> peopleIds,
        IReadOnlyList<string> animalIds,
        IReadOnlyList<string> tags)
    {
        if (!OperatingSystem.IsWindows() ||
            string.IsNullOrWhiteSpace(fullPath) ||
            !File.Exists(fullPath))
        {
            return;
        }

        var title = Path.GetFileNameWithoutExtension(fullPath);
        var peopleText = peopleIds.Count == 0 ? "(none)" : string.Join(", ", peopleIds);
        var animalsText = animalIds.Count == 0 ? "(none)" : string.Join(", ", animalIds);
        var tagText = tags.Count == 0 ? "(none)" : string.Join(", ", tags.Take(20));
        var keywords = tags
            .Concat(peopleIds.Select(x => $"person:{x}"))
            .Concat(animalIds.Select(x => $"animal:{x}"))
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(40)
            .ToArray();
        var subject = $"People: {peopleText}; Animals: {animalsText}";
        var comment =
            $"PhotoSortingApp Metadata{Environment.NewLine}" +
            $"People IDs: {peopleText}{Environment.NewLine}" +
            $"Animal IDs: {animalsText}{Environment.NewLine}" +
            $"Tags: {tagText}";

        var initHr = CoInitializeEx(IntPtr.Zero, CoInit.COINIT_MULTITHREADED);
        var shouldUninitialize = initHr >= 0;
        if (initHr < 0 && initHr != RpcChangedMode)
        {
            return;
        }

        IPropertyStore? store = null;
        try
        {
            var propertyStoreGuid = PropertyStoreGuid;
            var hr = SHGetPropertyStoreFromParsingName(
                fullPath,
                IntPtr.Zero,
                GetPropertyStoreFlags.GPS_READWRITE | GetPropertyStoreFlags.GPS_BESTEFFORT,
                ref propertyStoreGuid,
                out store);
            if (hr < 0 || store is null)
            {
                return;
            }

            SetStringProperty(store, TitleKey, title);
            SetStringProperty(store, SubjectKey, subject);
            SetStringProperty(store, CommentKey, comment);
            if (keywords.Length > 0)
            {
                SetStringVectorProperty(store, KeywordsKey, keywords);
            }

            var commitHr = store.Commit();
            if (commitHr < 0)
            {
                Marshal.ThrowExceptionForHR(commitHr);
            }
        }
        catch
        {
            // Best effort only: DB updates must still succeed even if file metadata write fails.
        }
        finally
        {
            if (store is not null)
            {
                Marshal.FinalReleaseComObject(store);
            }

            if (shouldUninitialize)
            {
                CoUninitialize();
            }
        }
    }

    private static void SetStringProperty(IPropertyStore store, PropertyKey key, string value)
    {
        var pv = PropVariant.FromString(value);
        try
        {
            var hr = store.SetValue(ref key, ref pv);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
        finally
        {
            PropVariantClear(ref pv);
        }
    }

    private static void SetStringVectorProperty(IPropertyStore store, PropertyKey key, IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        var array = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (array.Length == 0)
        {
            return;
        }

        var hrInit = InitPropVariantFromStringVector(array, (uint)array.Length, out var pv);
        if (hrInit < 0)
        {
            Marshal.ThrowExceptionForHR(hrInit);
        }

        try
        {
            var hr = store.SetValue(ref key, ref pv);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
        finally
        {
            PropVariantClear(ref pv);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int SHGetPropertyStoreFromParsingName(
        string pszPath,
        IntPtr pbc,
        GetPropertyStoreFlags flags,
        ref Guid riid,
        [Out, MarshalAs(UnmanagedType.Interface)] out IPropertyStore? ppv);

    [DllImport("ole32.dll", PreserveSig = true)]
    private static extern int PropVariantClear(ref PropVariant pvar);

    [DllImport("propsys.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int InitPropVariantFromStringVector(
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] prgsz,
        uint cElems,
        out PropVariant ppropvar);

    [DllImport("ole32.dll", PreserveSig = true)]
    private static extern int CoInitializeEx(IntPtr pvReserved, CoInit dwCoInit);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    [Flags]
    private enum GetPropertyStoreFlags : uint
    {
        GPS_READWRITE = 0x00000002,
        GPS_BESTEFFORT = 0x00000040
    }

    [Flags]
    private enum CoInit : uint
    {
        COINIT_MULTITHREADED = 0x0
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        public Guid fmtid;
        public uint pid;

        public PropertyKey(Guid formatId, uint propertyId)
        {
            fmtid = formatId;
            pid = propertyId;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        [FieldOffset(0)]
        public ushort vt;
        [FieldOffset(8)]
        public IntPtr pointerValue;

        public static PropVariant FromString(string value)
        {
            return new PropVariant
            {
                vt = (ushort)VarEnum.VT_LPWSTR,
                pointerValue = Marshal.StringToCoTaskMemUni(value)
            };
        }
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        uint GetCount(out uint cProps);
        uint GetAt(uint iProp, out PropertyKey pkey);
        uint GetValue(ref PropertyKey key, out PropVariant pv);
        int SetValue(ref PropertyKey key, ref PropVariant pv);
        int Commit();
    }
}
