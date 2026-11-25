using System;
using System.Collections.Generic;
using System.Text;

namespace SlideStudio.Infrastructure.Persistence;

/// <summary>
/// Écriture atomique de fichiers + création de copie de secours.
/// Garantit qu'on n'a jamais un fichier .slds partiellement écrit.
/// </summary>
public sealed class SafeFileWriter
{
    /// <summary>
    /// Écrit le contenu de façon sûre :
    /// 1. Crée une copie de secours (.bak)
    /// 2. Écrit dans un fichier temporaire (.tmp)
    /// 3. Remplace atomiquement le fichier cible
    /// </summary>
    public async Task WriteAtomicAsync(
        string targetPath,
        string content,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        ArgumentNullException.ThrowIfNull(content);

        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 1. Backup si le fichier existe déjà
        if (File.Exists(targetPath))
        {
            await CreateBackupAsync(targetPath, cancellationToken).ConfigureAwait(false);
        }