/*
 * This file is a part of Mono Bindings for Librhash
 *
 * Copyright (c) 2011, Sergey Basalaev <sbasalaev@gmail.com>
 *
 * Permission to use, copy, modify, and/or distribute this software for any
 * purpose with or without fee is hereby granted.
 *
 * THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
 * REGARD TO THIS SOFTWARE  INCLUDING ALL IMPLIED WARRANTIES OF  MERCHANTABILITY
 * AND FITNESS.  IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
 * INDIRECT,  OR CONSEQUENTIAL DAMAGES  OR ANY DAMAGES WHATSOEVER RESULTING FROM
 * LOSS OF USE,  DATA OR PROFITS,  WHETHER IN AN ACTION OF CONTRACT,  NEGLIGENCE
 * OR OTHER TORTIOUS ACTION,  ARISING OUT OF  OR IN CONNECTION  WITH THE USE  OR
 * PERFORMANCE OF THIS SOFTWARE.
 */

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Shizou.Hasher
{
    public static class Hasher
    {
        public static (string ed2k, string crc) GetHash(string filepath)
        {
            var hasher = new RHasher(RHasher.HashIds.Ed2K | RHasher.HashIds.Crc32);
            hasher.UpdateFile(filepath);
            return (hasher.ToString(RHasher.HashIds.Ed2K), hasher.ToString(RHasher.HashIds.Crc32));
        }
        
        
        private sealed class RHasher
        {
            private readonly HashIds _hashIds;

            /* Pointer to the native structure. */
            private IntPtr _ptr;

            public RHasher(HashIds hashtype)
            {
                _hashIds = hashtype;
                _ptr = Bindings.rhash_init(_hashIds);
            }

            ~RHasher()
            {
                if (_ptr == IntPtr.Zero) return;
                Bindings.rhash_free(_ptr);
                _ptr = IntPtr.Zero;
            }

            public RHasher Update(byte[] buf)
            {
                Bindings.rhash_update(_ptr, buf, buf.Length);
                return this;
            }

            public RHasher Update(byte[] buf, int len)
            {
                if (len < 0 || len >= buf.Length) throw new IndexOutOfRangeException();
                Bindings.rhash_update(_ptr, buf, len);
                return this;
            }

            public RHasher UpdateFile(string filename)
            {
                using Stream file = new FileStream(filename, FileMode.Open);
                byte[] buf = new byte[8192];
                int len;
                while ((len = file.Read(buf, 0, buf.Length)) > 0)
                    Bindings.rhash_update(_ptr, buf, len);
                return this;
            }

            public void Finish()
            {
                Bindings.rhash_final(_ptr, IntPtr.Zero);
            }

            public void Reset()
            {
                Bindings.rhash_reset(_ptr);
            }

            public override string ToString()
            {
                StringBuilder sb = new(130);
                Bindings.rhash_print(sb, _ptr, 0, PrintFlags.Default);
                return sb.ToString();
            }

            public string ToString(HashIds id)
            {
                if ((_hashIds & id) == 0) throw new ArgumentException("This hasher has not computed message digest for id: " + id, nameof(id));
                StringBuilder sb = new(130);
                Bindings.rhash_print(sb, _ptr, id, PrintFlags.Default);
                return sb.ToString();
            }

            public string ToHex(HashIds id)
            {
                if ((_hashIds & id) == 0) throw new ArgumentException("This hasher has not computed message digest for id: " + id, nameof(id));
                StringBuilder sb = new(130);
                Bindings.rhash_print(sb, _ptr, id, PrintFlags.Hex);
                return sb.ToString();
            }

            public string ToBase32(HashIds id)
            {
                if ((_hashIds & id) == 0) throw new ArgumentException("This hasher has not computed message digest for id: " + id, nameof(id));
                StringBuilder sb = new(130);
                Bindings.rhash_print(sb, _ptr, id, PrintFlags.Base32);
                return sb.ToString();
            }

            public string ToBase64(HashIds id)
            {
                if ((_hashIds & id) == 0) throw new ArgumentException("This hasher has not computed message digest for id: " + id, nameof(id));
                StringBuilder sb = new(130);
                Bindings.rhash_print(sb, _ptr, id, PrintFlags.Base64);
                return sb.ToString();
            }

            public string ToRaw(HashIds id)
            {
                if ((_hashIds & id) == 0) throw new ArgumentException("This hasher has not computed message digest for id: " + id, nameof(id));
                StringBuilder sb = new(130);
                Bindings.rhash_print(sb, _ptr, id, PrintFlags.Raw);
                return sb.ToString();
            }

            public string GetMagnet(string filepath)
            {
                return GetMagnet(filepath, _hashIds);
            }

            public string GetMagnet(string filepath, HashIds hashmask)
            {
                var len = Bindings.rhash_print_magnet(null, filepath, _ptr, hashmask, PrintFlags.Filesize);
                StringBuilder sb = new(len);
                Bindings.rhash_print_magnet(sb, filepath, _ptr, hashmask, PrintFlags.Filesize);
                return sb.ToString();
            }

            public static string GetHashForMsg(byte[] buf, HashIds id)
            {
                return new RHasher(id).Update(buf).ToString(id);
            }

            public static string GetHashForFile(string filename, HashIds id)
            {
                return new RHasher(id).UpdateFile(filename).ToString(id);
            }

            public static string GetMagnetFor(string filepath, HashIds hashmask)
            {
                return new RHasher(hashmask).UpdateFile(filepath).GetMagnet(filepath);
            }

            private static class Bindings
            {
                private const string LibRHash = "Hasher/librhash.dll";

                static Bindings()
                {
                    rhash_library_init();
                }

                [DllImport(LibRHash)]
                public static extern void rhash_library_init();

                [DllImport(LibRHash)]
                public static extern IntPtr rhash_init(HashIds hashIds);

                [DllImport(LibRHash)]
                public static extern void rhash_update(IntPtr ctx, byte[] message, int length);

                //may crash, rhash_final actually have 2 arguments
                [DllImport(LibRHash)]
                public static extern void rhash_final(IntPtr ctx, IntPtr unused);

                [DllImport(LibRHash)]
                public static extern void rhash_reset(IntPtr ctx);

                [DllImport(LibRHash)]
                public static extern void rhash_free(IntPtr ctx);

                [DllImport(LibRHash, CharSet = CharSet.Ansi)]
                public static extern void rhash_print(StringBuilder output, IntPtr ctx, HashIds hashId, PrintFlags flags);

                [DllImport(LibRHash)]
                public static extern int rhash_print_magnet(StringBuilder? output, string? filepath, IntPtr ctx, HashIds hashMask, PrintFlags flags);
            }


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
}
