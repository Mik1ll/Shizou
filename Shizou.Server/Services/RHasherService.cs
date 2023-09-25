using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Shizou.Server.Services;

public class RHasherService
{
    [Flags]
    public enum HashIds : uint
    {
        Crc32 = 1, /* CRC32 checksum. */
        Md4 = 1 << 1, /* MD4 hash. */
        Md5 = 1 << 2, /* MD5 hash. */
        Sha1 = 1 << 3, /* SHA-1 hash. */
        Tiger = 1 << 4, /* Tiger hash. */
        Tth = 1 << 5, /* Tiger tree hash */
        Btih = 1 << 6, /* BitTorrent info hash. */

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        Ed2k = 1 << 7, /* EDonkey 2000 hash. */
        Aich = 1 << 8, /* eMule AICH. */
        Whirlpool = 1 << 9, /* Whirlpool hash. */
        RipeMd160 = 1 << 10, /* RIPEMD-160 hash. */
        Gost94 = 1 << 11, /* GOST R 34.11-94. */
        Gost94Cryptopro = 1 << 12,
        Has160 = 1 << 13, /* HAS-160 hash. */
        Gost12256 = 1 << 14, /* GOST R 34.11-2012. */
        Gost12512 = 1 << 15,
        Sha224 = 1 << 16, /* SHA-224 hash. */
        Sha256 = 1 << 17, /* SHA-256 hash. */
        Sha384 = 1 << 18, /* SHA-384 hash. */
        Sha512 = 1 << 19, /* SHA-512 hash. */
        Edonr256 = 1 << 20, /* EDON-R 256. */
        Edonr512 = 1 << 21, /* EDON-R 512. */
        Sha3224 = 1 << 22, /* SHA3-224 hash. */
        Sha3256 = 1 << 23, /* SHA3-256 hash. */
        Sha3384 = 1 << 24, /* SHA3-384 hash. */
        Sha3512 = 1 << 25, /* SHA3-512 hash. */
        Crc32C = 1 << 26, /* CRC32C checksum. */
        Snefru128 = 1 << 27, /* Snefru-128 hash. */
        Snefru256 = 1 << 28, /* Snefru-256 hash. */
        Blake2S = 1 << 29, /* BLAKE2s hash. */
        Blake2B = 1 << 30 /* BLAKE2b hash. */
    }

    private readonly HashIds _hashIds;

    /* Pointer to the native structure. */
    private nint _ptr;

    public RHasherService(HashIds hashtype)
    {
        _hashIds = hashtype;
        _ptr = Bindings.rhash_init(_hashIds);
    }

    public static async Task<Dictionary<HashIds, string>> GetFileHashesAsync(FileInfo file, HashIds ids)
    {
        var hasher = new RHasherService(ids);
        await hasher.UpdateFileAsync(file);
        hasher.Finish();
        return Enum.GetValues(typeof(HashIds)).Cast<HashIds>().Where(id => ids.HasFlag(id))
            .ToDictionary(id => id, id => hasher.ToString(id));
    }

    public static async Task<string> GetFileSignatureAsync(string filePath)
    {
        var file = new FileInfo(filePath);
        if (!file.Exists)
            return string.Empty;
        var bufSize = 1 << 20;
        var seekLen = Math.Max(file.Length / 30 - bufSize, 0);
        var hasher = new RHasherService(HashIds.Sha1);
        await using var stream = file.OpenRead();
        var buf = new byte[bufSize];
        int len;
        while ((len = await stream.ReadAsync(buf, 0, buf.Length)) > 0)
        {
            hasher.Update(buf, len);
            stream.Seek(seekLen, SeekOrigin.Current);
        }

        return hasher.Finish().ToString();
    }

    ~RHasherService()
    {
        if (_ptr == nint.Zero) return;
        Bindings.rhash_free(_ptr);
        _ptr = nint.Zero;
    }

    public RHasherService Update(byte[] buf, int len)
    {
        if (len < 0 || len > buf.Length) throw new IndexOutOfRangeException();
        if (Bindings.rhash_update(_ptr, buf, len) < 0)
            throw new ExternalException($"{nameof(Bindings.rhash_update)} failed");
        return this;
    }

    public async Task<RHasherService> UpdateFileAsync(FileInfo file)
    {
        var bufSize = 1 << 20;
        await using var stream = file.OpenRead();
        var buf = new byte[bufSize];
        int len;
        while ((len = await stream.ReadAsync(buf, 0, buf.Length)) > 0)
            Update(buf, len);
        return this;
    }

    public unsafe RHasherService Finish()
    {
        if (Bindings.rhash_final(_ptr, (byte*)nint.Zero) < 0)
            throw new ExternalException($"{nameof(Bindings.rhash_final)} failed");
        return this;
    }

    public void Reset()
    {
        Bindings.rhash_reset(_ptr);
    }

    public override unsafe string ToString()
    {
        var sb = stackalloc byte[130];
        if (Bindings.rhash_print(sb, _ptr, 0, PrintFlags.Default) == 0)
            throw new ExternalException($"{nameof(Bindings.rhash_print)} failed");
        return Marshal.PtrToStringAnsi((nint)sb) ?? string.Empty;
    }

    public unsafe string ToString(HashIds id)
    {
        if ((_hashIds & id) == 0) throw new ArgumentException("This hasher has not computed message digest for id: " + id, nameof(id));
        var sb = stackalloc byte[130];
        if (Bindings.rhash_print(sb, _ptr, id, PrintFlags.Default) == 0)
            throw new ExternalException($"{nameof(Bindings.rhash_print)} failed");
        return Marshal.PtrToStringAnsi((nint)sb) ?? string.Empty;
    }

    private static class Bindings
    {
        private const string LibRHash = "librhash.dll";

        static Bindings()
        {
            rhash_library_init();
        }

        [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern void rhash_library_init();

        [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint rhash_init(HashIds hashIds);

        [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rhash_update(nint ctx, [In] byte[] message, int length);

        [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern unsafe int rhash_final(nint ctx, byte* firstResult);

        [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
        public static extern void rhash_reset(nint ctx);

        [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
        public static extern void rhash_free(nint ctx);

        [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern unsafe nuint rhash_print(byte* output, nint ctx, HashIds hashId, PrintFlags flags);
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
}
