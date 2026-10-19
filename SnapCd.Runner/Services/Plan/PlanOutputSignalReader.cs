// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Runner.Services.Plan;

/// <summary>
/// Splits a plan file's output value into the two parts a downstream root needs: the type and the
/// value, each kept as the bytes the plan held.
///
/// A plan file stores an output's value as a msgpack 2-element array of [type-as-ctyjson, value].
/// Those two elements are byte-identical to the type and value a signals document carries, so the
/// work here is unwrapping the array rather than converting anything: the value is never decoded,
/// which is what keeps an unknown an unknown. The unknown itself is msgpack fixext1 (d4 00 00),
/// which has no JSON form and would become a guess if it were decoded.
/// </summary>
public static class PlanOutputSignalReader
{
    /// <summary>
    /// The type and value halves of one encoded output, or null when the bytes are not the
    /// 2-element array a plan file writes.
    /// </summary>
    public static (byte[] Type, byte[] Value)? Split(ReadOnlySpan<byte> msgpack)
    {
        // fixarray of 2. A plan file writes exactly this; anything else is a shape this does not
        // claim to understand, and guessing at it would put the wrong bytes on the wire.
        if (msgpack.Length < 1 || msgpack[0] != 0x92) return null;

        var offset = 1;

        var type = ReadElement(msgpack, ref offset);
        if (type == null) return null;

        var value = ReadElement(msgpack, ref offset);
        if (value == null) return null;

        return (type, value);
    }

    /// <summary>
    /// One msgpack element, returned whole and still encoded. Only the families a plan file uses
    /// for these two positions are understood: the type is always a binary string, and the value
    /// is whatever the output's own type encodes to.
    /// </summary>
    private static byte[]? ReadElement(ReadOnlySpan<byte> buffer, ref int offset)
    {
        if (offset >= buffer.Length) return null;

        var start = offset;
        var marker = buffer[offset];

        int length;

        switch (marker)
        {
            // Lengths that live in the marker itself.
            case >= 0x00 and <= 0x7f: // positive fixint
            case >= 0xe0: // negative fixint
            case 0xc0: // nil
            case 0xc2: // false
            case 0xc3: // true
                offset += 1;
                return buffer[start..offset].ToArray();

            case >= 0xa0 and <= 0xbf: // fixstr
                length = marker & 0x1f;
                offset += 1 + length;
                break;

            case 0xc4: // bin 8
                if (offset + 1 >= buffer.Length) return null;
                length = buffer[offset + 1];
                offset += 2 + length;
                break;

            case 0xc5: // bin 16
                if (offset + 2 >= buffer.Length) return null;
                length = (buffer[offset + 1] << 8) | buffer[offset + 2];
                offset += 3 + length;
                break;

            case 0xd9: // str 8
                if (offset + 1 >= buffer.Length) return null;
                length = buffer[offset + 1];
                offset += 2 + length;
                break;

            case 0xda: // str 16
                if (offset + 2 >= buffer.Length) return null;
                length = (buffer[offset + 1] << 8) | buffer[offset + 2];
                offset += 3 + length;
                break;

            // An unknown value, and the reason none of this decodes: fixext1 with type 0 is cty's
            // marker for a value the plan cannot predict.
            case 0xd4:
                offset += 3;
                break;

            case 0xcc: offset += 2; break; // uint 8
            case 0xcd: offset += 3; break; // uint 16
            case 0xce: offset += 5; break; // uint 32
            case 0xcf: offset += 9; break; // uint 64
            case 0xd0: offset += 2; break; // int 8
            case 0xd1: offset += 3; break; // int 16
            case 0xd2: offset += 5; break; // int 32
            case 0xd3: offset += 9; break; // int 64
            case 0xca: offset += 5; break; // float 32
            case 0xcb: offset += 9; break; // float 64

            default:
                // A map or an array here means a structured output, whose encoding this does not
                // walk. Reporting nothing is better than reporting a prefix of it.
                return null;
        }

        return offset <= buffer.Length ? buffer[start..offset].ToArray() : null;
    }
}
