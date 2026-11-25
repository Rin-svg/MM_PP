using System;
using System.Collections.Generic;
using System.Text;

namespace SlideStudio.Infrastructure.Persistence;

/
// Écriture atomique de fichiers + création de copie de secours.
// Garantit qu'on n'a jamais un fichier .slds partiellement écrit.

public sealed class SafeFileWriter
{
   // Écrit le contenu de façon sûre :
   // 1. Crée une copie de secours (.bak)
   // 2. Écrit dans un fichier temporaire (.tmp)
   // 3. Remplace atomiquement le fichier cible
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
        private async Task CreateBackupAsync(string targetPath, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}


   // Crée une copie de secours : fichier.slds.bak
    public async Task CreateBackupAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return;

        var backupPath = filePath + ".bak";

        await Task.Run(() =>
        {
            File.Copy(filePath, backupPath, overwrite: true);
        }, cancellationToken).ConfigureAwait(false);
    }
}