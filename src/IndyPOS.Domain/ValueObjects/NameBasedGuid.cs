using System.Security.Cryptography;
using System.Text;

namespace IndyPOS.Domain.ValueObjects;

/// <summary>
/// RFC 4122 version 5 (SHA-1) name-based UUIDs: the same namespace and name always give the same id. Used where
/// an id must survive being built again, e.g. a v3 debt migrated twice must keep its id in the cloud.
/// </summary>
public static class NameBasedGuid
{
    public static Guid Create(Guid namespaceId, string name)
    {
        Span<byte> namespaceBytes = stackalloc byte[16];
        namespaceId.TryWriteBytes(namespaceBytes, bigEndian: true, out _);

        var hash = SHA1.HashData([.. namespaceBytes, .. Encoding.UTF8.GetBytes(name)]);
        var uuid = hash.AsSpan(0, 16);
        uuid[6] = (byte)((uuid[6] & 0x0F) | 0x50);   // version 5
        uuid[8] = (byte)((uuid[8] & 0x3F) | 0x80);   // RFC 4122 variant

        return new Guid(uuid, bigEndian: true);
    }
}
