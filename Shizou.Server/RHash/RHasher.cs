using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Shizou.Server.RHash;

public class RHasher
{
    private readonly HashIds _hashIds;

    /* Pointer to the native structure. */
    private nint _ptr;

    public RHasher(HashIds hashtype)
    {
        _hashIds = hashtype;
        _ptr = Bindings.rhash_init(_hashIds);
    }

    ~RHasher()
    {
        if (_ptr == nint.Zero) return;
        Bindings.rhash_free(_ptr);
        _ptr = nint.Zero;
    }

    [Flags]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private enum PrintFlags
    {
        Default = 0x0, /* Print in a default format */
        Raw = 0x1, /* Output as binary message digest */
        Hex = 0x2, /* Print as a hexadecimal string */
        Base32 = 0x3, /* Print as a base32-encoded string */
        Base64 = 0x4, /* Print as a base64-encoded string */
        Uppercase = 0x8, /* Print as an uppercase string. Can be used for base32 or hexadecimal format only. */
        Reverse = 0x10, /* Reverse message digest bytes. Can be used for GOST hash functions. */
        NoMagnet = 0x20, /* Don't print 'magnet:?' prefix in rhash_print_magnet */
        Filesize = 0x40, /* Print file size in rhash_print_magnet */
        UrlEncode = 0x80 /* Print as URL-encoded string */
    }

    public override unsafe string ToString()
    {
        var sb = stackalloc byte[130];
        if (Bindings.rhash_print(sb, _ptr, 0, PrintFlags.Default) == 0)
            throw new ExternalException($"{nameof(Bindings.rhash_print)} failed");
        return Marshal.PtrToStringAnsi((nint)sb) ?? string.Empty;
    }

    public RHasher Update(byte[] buf, int len)
    {
        if (len < 0 || len > buf.Length) throw new IndexOutOfRangeException();
        if (Bindings.rhash_update(_ptr, buf, (nuint)len) < 0)
            throw new ExternalException($"{nameof(Bindings.rhash_update)} failed");
        return this;
    }

    public unsafe RHasher Finish()
    {
        if (Bindings.rhash_final(_ptr, (byte*)nint.Zero) < 0)
            throw new ExternalException($"{nameof(Bindings.rhash_final)} failed");
        return this;
    }

    public void Reset()
    {
        Bindings.rhash_reset(_ptr);
    }

    public unsafe string ToString(HashIds hashId)
    {
        // https://graphics.stanford.edu/~seander/bithacks.html#DetermineIfPowerOf2
        if (hashId == 0 || (hashId & (hashId - 1)) != 0) throw new ArgumentException("No hash id set or multiple hash ids set");
        if ((_hashIds & hashId) == 0) throw new ArgumentException("This hasher has not computed message digest for hash id: " + hashId, nameof(hashId));
        var sb = stackalloc byte[130];
        if (Bindings.rhash_print(sb, _ptr, hashId, PrintFlags.Default) == 0)
            throw new ExternalException($"{nameof(Bindings.rhash_print)} failed");
        return Marshal.PtrToStringAnsi((nint)sb) ?? string.Empty;
    }

    private static class Bindings
    {
        [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern void rhash_library_init();

        [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint rhash_init(HashIds hashIds);

        [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rhash_update(nint ctx, [In] byte[] message, nuint length);

        [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern unsafe int rhash_final(nint ctx, byte* firstResult);

        [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
        public static extern void rhash_reset(nint ctx);

        [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
        public static extern void rhash_free(nint ctx);

        [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern unsafe nuint rhash_print(byte* output, nint ctx, HashIds hashId, PrintFlags flags);

        private const string LibRHash = "librhash";

        static Bindings()
        {
            NativeLibrary.SetDllImportResolver(typeof(Bindings).Assembly, (name, assembly, path) =>
            {
                if (!NativeLibrary.TryLoad(name, assembly, path, out var libHandle))
                    NativeLibrary.TryLoad($"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/{name}", assembly, path, out libHandle);
                return libHandle;
            });
            rhash_library_init();
        }
    }
}
