using System.Buffers.Binary;
using SlideStudio.Core.Model;

namespace SlideStudio.Core;

/// <summary>Stocke les images embarquées d'une présentation après validation de leur nom, format et dimensions.</summary>
public sealed class MediaStore
{
    private readonly Dictionary<string, byte[]> _items = new(StringComparer.Ordinal);
    private long _totalBytes;

    /// <summary>Nombre d'images stockées.</summary>
    public int Count => _items.Count;

    /// <summary>Taille cumulée des images stockées, en octets.</summary>
    public long TotalBytes => _totalBytes;

    /// <summary>Noms des images embarquées.</summary>
    public IReadOnlyCollection<string> Names => _items.Keys.ToArray();

    /// <summary>Indique si une image portant ce nom est stockée.</summary>
    public bool Contains(string mediaName)
    {
        ArgumentNullException.ThrowIfNull(mediaName);
        return _items.ContainsKey(mediaName);
    }

    /// <summary>Ajoute une image après validation complète de son en-tête, sans décoder ses pixels.</summary>
    /// <param name="mediaName">Nom simple et sûr du média.</param>
    /// <param name="bytes">Contenu du fichier image.</param>
    /// <exception cref="DocumentValidationException">Le nom, le format, la taille ou les dimensions sont invalides.</exception>
    /// <exception cref="InvalidOperationException">Un média de même nom existe déjà.</exception>
    public void Add(string mediaName, ReadOnlySpan<byte> bytes)
    {
        DocumentRules.Enforce(DocumentRules.CheckMediaName(mediaName), nameof(mediaName));
        if (bytes.Length is 0 or > Limits.MaxMediaBytes)
        {
            throw new DocumentValidationException("La taille de l'image est invalide.", nameof(bytes));
        }

        if (_items.ContainsKey(mediaName))
        {
            throw new InvalidOperationException("Un média portant ce nom existe déjà.");
        }

        if (_items.Count >= Limits.MaxMediaCount)
        {
            throw new DocumentValidationException("Le document contient trop d'images.", nameof(mediaName));
        }

        if (_totalBytes + bytes.Length > Limits.MaxTotalMediaBytes)
        {
            throw new DocumentValidationException("La taille cumulée des images est trop importante.", nameof(bytes));
        }

        (int width, int height) = ReadDimensions(mediaName, bytes);
        if (width <= 0
            || height <= 0
            || width > Limits.MaxImageDimension
            || height > Limits.MaxImageDimension
            || (long)width * height > Limits.MaxImagePixels)
        {
            throw new DocumentValidationException("Les dimensions de l'image dépassent les limites autorisées.", nameof(bytes));
        }

        _items.Add(mediaName, bytes.ToArray());
        _totalBytes += bytes.Length;
    }

    /// <summary>Renvoie une copie des octets du média.</summary>
    /// <exception cref="KeyNotFoundException">Le média n'existe pas.</exception>
    public byte[] GetBytes(string mediaName)
    {
        ArgumentNullException.ThrowIfNull(mediaName);
        return _items.TryGetValue(mediaName, out byte[]? bytes)
            ? bytes.ToArray()
            : throw new KeyNotFoundException($"Le média « {mediaName} » n'existe pas.");
    }

    /// <summary>Crée une copie indépendante du magasin.</summary>
    public MediaStore Clone()
    {
        MediaStore clone = new();
        foreach ((string name, byte[] bytes) in _items)
        {
            clone._items.Add(name, bytes.ToArray());
            clone._totalBytes += bytes.Length;
        }

        return clone;
    }

    private static (int Width, int Height) ReadDimensions(string mediaName, ReadOnlySpan<byte> bytes)
    {
        string extension = Path.GetExtension(mediaName);
        return extension.ToLowerInvariant() switch
        {
            ".png" => ReadPngDimensions(bytes),
            ".gif" => ReadGifDimensions(bytes),
            ".bmp" => ReadBmpDimensions(bytes),
            ".jpg" or ".jpeg" => ReadJpegDimensions(bytes),
            _ => throw new DocumentValidationException("Format d'image non pris en charge.", nameof(mediaName))
        };
    }

    private static (int Width, int Height) ReadPngDimensions(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (bytes.Length < 24
            || !bytes[..8].SequenceEqual(signature)
            || !bytes.Slice(12, 4).SequenceEqual("IHDR"u8))
        {
            throw InvalidImage();
        }

        uint width = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(16, 4));
        uint height = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(20, 4));
        return (ToDimension(width), ToDimension(height));
    }

    private static (int Width, int Height) ReadGifDimensions(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 10
            || !(bytes[..6].SequenceEqual("GIF87a"u8) || bytes[..6].SequenceEqual("GIF89a"u8)))
        {
            throw InvalidImage();
        }

        return (BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(6, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(8, 2)));
    }

    private static (int Width, int Height) ReadBmpDimensions(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 26 || !bytes[..2].SequenceEqual("BM"u8))
        {
            throw InvalidImage();
        }

        uint headerSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(14, 4));
        if (headerSize == 12 && bytes.Length >= 26)
        {
            return (BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(18, 2)),
                BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(20, 2)));
        }

        if (headerSize < 40 || bytes.Length < 30)
        {
            throw InvalidImage();
        }

        int width = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(18, 4));
        int signedHeight = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(22, 4));
        if (width <= 0 || signedHeight == 0 || signedHeight == int.MinValue)
        {
            throw InvalidImage();
        }

        return (width, Math.Abs(signedHeight));
    }

    private static (int Width, int Height) ReadJpegDimensions(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 0xff || bytes[1] != 0xd8)
        {
            throw InvalidImage();
        }

        int offset = 2;
        while (offset + 4 <= bytes.Length)
        {
            if (bytes[offset] != 0xff)
            {
                throw InvalidImage();
            }

            while (offset < bytes.Length && bytes[offset] == 0xff)
            {
                offset++;
            }

            if (offset >= bytes.Length)
            {
                break;
            }

            byte marker = bytes[offset++];
            if (marker is 0xd9 or 0xda)
            {
                break;
            }

            if (marker is >= 0xd0 and <= 0xd7 or 0x01)
            {
                continue;
            }

            if (offset + 2 > bytes.Length)
            {
                break;
            }

            int segmentLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset, 2));
            if (segmentLength < 2 || offset + segmentLength > bytes.Length)
            {
                throw InvalidImage();
            }

            if (IsStartOfFrame(marker))
            {
                if (segmentLength < 7)
                {
                    throw InvalidImage();
                }

                int height = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset + 3, 2));
                int width = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset + 5, 2));
                return (width, height);
            }

            offset += segmentLength;
        }

        throw InvalidImage();
    }

    private static int ToDimension(uint dimension) =>
        dimension <= int.MaxValue ? (int)dimension : int.MaxValue;

    private static bool IsStartOfFrame(byte marker) =>
        marker is >= 0xc0 and <= 0xcf
            && marker is not (0xc4 or 0xc8 or 0xcc);

    private static DocumentValidationException InvalidImage() =>
        new("Le fichier ne contient pas une image valide du format annoncé.", "media");
}
