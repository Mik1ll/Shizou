using System;
using System.Diagnostics.CodeAnalysis;

namespace Shizou.Server.RHash;

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
