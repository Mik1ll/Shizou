using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Shizou.Hashers
{
    public class RHasher
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
            Ed2K = 1 << 7, /* EDonkey 2000 hash. */
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
        private IntPtr _ptr;

        public RHasher(HashIds hashtype)
        {
            _hashIds = hashtype;
            _ptr = Bindings.rhash_init(_hashIds);
        }

        public static Dictionary<HashIds, string> GetFileHashs(string filepath, HashIds ids, bool useTask = false)
        {
            var hasher = new RHasher(ids);
            if (useTask)
                hasher.UpdateFileWithTask(filepath);
            else
                hasher.UpdateFile(filepath);
            hasher.Finish();
            return Enum.GetValues(typeof(HashIds)).Cast<HashIds>().Where(id => ids.HasFlag(id))
                .ToDictionary(id => id, id => hasher.ToString(id));
        }

        ~RHasher()
        {
            if (_ptr == IntPtr.Zero) return;
            Bindings.rhash_free(_ptr);
            _ptr = IntPtr.Zero;
        }

        public RHasher Update(byte[] buf)
        {
            if (Bindings.rhash_update(_ptr, buf, buf.Length) < 0)
                throw new ExternalException($"{nameof(Bindings.rhash_update)} failed");
            return this;
        }

        public RHasher Update(byte[] buf, int len)
        {
            if (len < 0 || len >= buf.Length) throw new IndexOutOfRangeException();
            if (Bindings.rhash_update(_ptr, buf, len) < 0)
                throw new ExternalException($"{nameof(Bindings.rhash_update)} failed");
            return this;
        }

        public RHasher UpdateFileWithTask(string filePath)
        {
            const int bufSize = 1 << 25;
            using FileStream file = new(filePath, FileMode.Open, FileAccess.Read, FileShare.None, bufSize, FileOptions.SequentialScan);
            byte[] buf1 = new byte[bufSize], buf2 = new byte[bufSize];
            int len;
            Task<int>? hashTask = null;
            while ((len = file.Read(buf1, 0, buf1.Length)) > 0)
            {
                if ((hashTask?.Result ?? 0) < 0)
                    throw new ExternalException($"{nameof(Bindings.rhash_update)} failed");
                // ReSharper disable AccessToModifiedClosure
                hashTask = Task.Run(() => Bindings.rhash_update(_ptr, buf1, len));
                // ReSharper restore AccessToModifiedClosure
                byte[] temp = buf1;
                buf1 = buf2;
                buf2 = temp;
            }
            if ((hashTask?.Result ?? 0) < 0)
                throw new ExternalException($"{nameof(Bindings.rhash_update)} failed");
            return this;
        }

        public RHasher UpdateFile(string filePath)
        {
            const int bufSize = 1 << 25;
            using FileStream file = new(filePath, FileMode.Open, FileAccess.Read, FileShare.None, bufSize, FileOptions.SequentialScan);
            byte[] buf = new byte[bufSize];
            int len;
            while ((len = file.Read(buf, 0, buf.Length)) > 0)
                if (Bindings.rhash_update(_ptr, buf, len) < 0)
                    throw new ExternalException($"{nameof(Bindings.rhash_update)} failed");
            return this;
        }

        public RHasher Finish()
        {
            if (Bindings.rhash_final(_ptr, null) < 0)
                throw new ExternalException($"{nameof(Bindings.rhash_final)} failed");
            return this;
        }

        public void Reset()
        {
            Bindings.rhash_reset(_ptr);
        }

        public override string ToString()
        {
            StringBuilder sb = new(130);
            if (Bindings.rhash_print(sb, _ptr, 0, PrintFlags.Default) == 0)
                throw new ExternalException($"{nameof(Bindings.rhash_print)} failed");
            return sb.ToString();
        }

        public string ToString(HashIds id)
        {
            if ((_hashIds & id) == 0) throw new ArgumentException("This hasher has not computed message digest for id: " + id, nameof(id));
            StringBuilder sb = new(130);
            if (Bindings.rhash_print(sb, _ptr, id, PrintFlags.Default) == 0)
                throw new ExternalException($"{nameof(Bindings.rhash_print)} failed");
            return sb.ToString();
        }

        public static string GetHashForMsg(byte[] buf, HashIds id)
        {
            return new RHasher(id).Update(buf).Finish().ToString();
        }

        private static class Bindings
        {
            private const string LibRHash = "Hashers/librhash.dll";

            static Bindings()
            {
                rhash_library_init();
            }

            [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
            private static extern void rhash_library_init();

            [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr rhash_init(HashIds hashIds);

            [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
            public static extern int rhash_update(IntPtr ctx, byte[] message, int length);

            [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
            public static extern int rhash_final(IntPtr ctx, StringBuilder? firstResult);

            [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
            public static extern void rhash_reset(IntPtr ctx);

            [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
            public static extern void rhash_free(IntPtr ctx);

            [DllImport(LibRHash, CallingConvention = CallingConvention.Cdecl)]
            public static extern nuint rhash_print(StringBuilder output, IntPtr ctx, HashIds hashId, PrintFlags flags);
        }

        [Flags]
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
}
