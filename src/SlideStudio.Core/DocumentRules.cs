using SlideStudio.Core.Model;

namespace SlideStudio.Core;

/// <summary>
/// Règles de validation du document : une fonction par type de valeur, en liste blanche.
/// </summary>
/// <remarks>
/// <para>
/// POURQUOI des fonctions <c>Check…</c> qui renvoient un message plutôt qu'une exception : l'interface peut
/// les appeler à chaque frappe pour afficher une erreur en ligne (« le texte est trop long ») sans
/// déclencher d'exception. Les propriétés du modèle appellent les mêmes fonctions et lèvent
/// <see cref="DocumentValidationException"/> : la validation d'interface est un confort, celle du modèle est la barrière.
/// </para>
/// <para>
/// SÉCURITÉ : tout ce qui arrive ici peut venir d'un fichier piégé. On refuse sans « réparer » : corriger
/// silencieusement une valeur hostile masque l'attaque.
/// </para>
/// </remarks>
public static class DocumentRules
{
    private static readonly HashSet<string> _reservedDeviceNames = BuildReservedDeviceNames();

    /// <summary>Extensions d'images acceptées (pour la boîte de dialogue « Insérer une image » et la validation).</summary>
    public static IReadOnlySet<string> AllowedImageExtensions { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };

    /// <summary>Vérifie le contenu d'une zone de texte (retours à la ligne et tabulations autorisés).</summary>
    /// <param name="value">Texte à contrôler.</param>
    /// <returns><c>null</c> si valide, sinon le message d'erreur à afficher.</returns>
    public static string? CheckText(string? value) =>
        CheckString(value, "Le texte", Limits.MaxTextLength, allowLineBreaks: true);

    /// <summary>Vérifie le titre de la présentation (une seule ligne).</summary>
    /// <param name="value">Titre à contrôler.</param>
    /// <returns><c>null</c> si valide, sinon le message d'erreur à afficher.</returns>
    public static string? CheckTitle(string? value) =>
        CheckString(value, "Le titre", Limits.MaxTitleLength, allowLineBreaks: false);

    /// <summary>Vérifie le texte alternatif d'une image (une seule ligne).</summary>
    /// <param name="value">Texte alternatif à contrôler.</param>
    /// <returns><c>null</c> si valide, sinon le message d'erreur à afficher.</returns>
    public static string? CheckAltText(string? value) =>
        CheckString(value, "Le texte alternatif", Limits.MaxAltTextLength, allowLineBreaks: false);

    /// <summary>Vérifie un nom de police : lettres, chiffres, espaces et quelques signes seulement.</summary>
    /// <param name="value">Nom de police à contrôler.</param>
    /// <returns><c>null</c> si valide, sinon le message d'erreur à afficher.</returns>
    public static string? CheckFontFamily(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Le nom de la police est obligatoire.";
        }

        if (value.Length > Limits.MaxFontFamilyLength)
        {
            return FormattableString.Invariant(
                $"Le nom de la police est trop long : {Limits.MaxFontFamilyLength} caractères au maximum.");
        }

        // ROBUSTESSE : un espace en début ou en fin est presque toujours une faute de frappe qui ferait
        // échouer la recherche de la police ; on le signale au lieu de le supprimer en silence.
        if (value[0] == ' ' || value[^1] == ' ')
        {
            return "Le nom de la police ne doit pas commencer ni finir par un espace.";
        }

        foreach (char c in value)
        {
            bool allowed = char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' or '.' or '(' or ')';
            if (!allowed)
            {
                return "Le nom de la police contient un caractère interdit.";
            }
        }

        return null;
    }

    /// <summary>Vérifie une taille de police en points.</summary>
    /// <param name="points">Taille à contrôler.</param>
    /// <returns><c>null</c> si valide, sinon le message d'erreur à afficher.</returns>
    public static string? CheckFontSize(double points) =>
        IsInRange(points, Limits.MinFontSizePoints, Limits.MaxFontSizePoints)
            ? null
            : FormattableString.Invariant(
                $"La taille de la police doit être comprise entre {Limits.MinFontSizePoints} et {Limits.MaxFontSizePoints} points.");

    /// <summary>Vérifie l'épaisseur d'un contour.</summary>
    /// <param name="width">Épaisseur à contrôler (0 = pas de contour).</param>
    /// <returns><c>null</c> si valide, sinon le message d'erreur à afficher.</returns>
    public static string? CheckStrokeWidth(double width) =>
        IsInRange(width, 0, Limits.MaxStrokeWidth)
            ? null
            : FormattableString.Invariant($"L'épaisseur du contour doit être comprise entre 0 et {Limits.MaxStrokeWidth}.");

    /// <summary>Vérifie position, taille et rotation d'un élément.</summary>
    /// <param name="frame">Cadre à contrôler.</param>
    /// <returns><c>null</c> si valide, sinon le message d'erreur à afficher.</returns>
    public static string? CheckFrame(ElementFrame frame)
    {
        // ROBUSTESSE : NaN et ±infini ne doivent jamais entrer dans le modèle (ils casseraient le dessin).
        // Voir IsInRange : la plage les rejette, et IsFinite le garantit explicitement.
        if (!IsInRange(frame.X, -Limits.MaxCoordinate, Limits.MaxCoordinate)
            || !IsInRange(frame.Y, -Limits.MaxCoordinate, Limits.MaxCoordinate))
        {
            return FormattableString.Invariant(
                $"La position de l'élément doit rester entre -{Limits.MaxCoordinate} et {Limits.MaxCoordinate}.");
        }

        if (!IsInRange(frame.Width, Limits.MinElementSize, Limits.MaxElementSize)
            || !IsInRange(frame.Height, Limits.MinElementSize, Limits.MaxElementSize))
        {
            return FormattableString.Invariant(
                $"La taille de l'élément doit être comprise entre {Limits.MinElementSize} et {Limits.MaxElementSize}.");
        }

        if (!IsInRange(frame.RotationDegrees, 0, 360) || frame.RotationDegrees >= 360)
        {
            return "La rotation doit être comprise entre 0 et 360 degrés (360 exclu).";
        }

        return null;
    }

    /// <summary>
    /// Vérifie le nom d'une image embarquée dans le fichier : un simple nom (pas de dossier), en caractères ASCII sûrs,
    /// avec une extension d'image connue.
    /// </summary>
    /// <remarks>
    /// SÉCURITÉ : ce nom devient un nom de fichier lors de l'extraction du zip. La liste blanche rend impossibles
    /// « ../x.png » (remontée de dossier), « a/b.png », « C:x.png » ou « fichier.png:flux » (flux alternatif NTFS).
    /// </remarks>
    /// <param name="value">Nom à contrôler.</param>
    /// <returns><c>null</c> si valide, sinon le message d'erreur à afficher.</returns>
    public static string? CheckMediaName(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "Le nom de l'image est vide.";
        }

        if (value.Length > Limits.MaxMediaNameLength)
        {
            return FormattableString.Invariant(
                $"Le nom de l'image est trop long : {Limits.MaxMediaNameLength} caractères au maximum.");
        }

        if (!IsAsciiLetterOrDigit(value[0]))
        {
            return "Le nom de l'image doit commencer par une lettre ou un chiffre.";
        }

        foreach (char c in value)
        {
            if (!IsAsciiLetterOrDigit(c) && c is not ('.' or '-' or '_'))
            {
                return "Le nom de l'image ne peut contenir que des lettres, chiffres, points, tirets et soulignés.";
            }
        }

        // SÉCURITÉ : « .. » est refusé même s'il ne ferait rien ici ; on ne raisonne pas au cas par cas.
        if (value.Contains("..", StringComparison.Ordinal))
        {
            return "Le nom de l'image ne peut pas contenir deux points consécutifs.";
        }

        if (!AllowedImageExtensions.Contains(Path.GetExtension(value)))
        {
            return "Format d'image non pris en charge (PNG, JPEG, GIF ou BMP).";
        }

        // Windows traite « CON.png » ou « nul.png » comme un périphérique, pas comme un fichier.
        string baseName = value[..value.IndexOf('.', StringComparison.Ordinal)];
        if (_reservedDeviceNames.Contains(baseName))
        {
            return "Ce nom est réservé par le système.";
        }

        return null;
    }

    /// <summary>Lève <see cref="DocumentValidationException"/> si <paramref name="error"/> n'est pas <c>null</c>.</summary>
    /// <param name="error">Résultat d'une fonction <c>Check…</c>.</param>
    /// <param name="propertyName">Nom de la propriété concernée.</param>
    internal static void Enforce(string? error, string propertyName)
    {
        if (error is not null)
        {
            throw new DocumentValidationException(error, propertyName);
        }
    }

    private static string? CheckString(string? value, string label, int maxLength, bool allowLineBreaks)
    {
        if (value is null)
        {
            return $"{label} est absent.";
        }

        if (value.Length > maxLength)
        {
            return FormattableString.Invariant($"{label} est trop long : {maxLength} caractères au maximum.");
        }

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];

            // ROBUSTESSE : '\0' et les autres caractères de contrôle cassent l'enregistrement et certaines API natives.
            // Retours à la ligne et tabulations restent permis dans une zone de texte (UTILISATEUR : il tape Entrée).
            bool isAllowedControl = allowLineBreaks && (c is '\n' or '\r' or '\t');
            if (char.IsControl(c) && !isAllowedControl)
            {
                return $"{label} contient un caractère de contrôle interdit.";
            }

            // ROBUSTESSE : U+2028 et U+2029 (séparateurs de ligne et de paragraphe Unicode) ne sont pas des « caractères
            // de contrôle » au sens de .NET mais provoquent bien un saut de ligne à l'affichage : interdits dans un champ d'une ligne.
            if (!allowLineBreaks && (c is '\u2028' or '\u2029'))
            {
                return $"{label} ne peut pas contenir de saut de ligne.";
            }

            // ROBUSTESSE : un émoji est une PAIRE de char (haut + bas). Une moitié isolée n'est pas du texte valide :
            // l'encodage en UTF-8 la remplacerait par « ? » ou lèverait une exception à l'enregistrement.
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    i++;
                    continue;
                }

                return $"{label} contient un caractère Unicode invalide.";
            }

            if (char.IsLowSurrogate(c))
            {
                return $"{label} contient un caractère Unicode invalide.";
            }
        }

        return null;
    }

    // DÉFENSE EN PROFONDEUR : avec des bornes finies, « value >= min && value <= max » rejette déjà NaN (toute comparaison
    // avec NaN est fausse) et ±infini. IsFinite est donc techniquement redondant ici (le test de mutation le confirme : le
    // retirer ne fait échouer aucun test). On le garde explicite car si quelqu'un réécrit la condition à l'envers
    // (« rejeter si value < min || value > max »), NaN passerait silencieusement : toutes ces comparaisons sont fausses.
    private static bool IsInRange(double value, double min, double max) =>
        double.IsFinite(value) && value >= min && value <= max;

    private static bool IsAsciiLetterOrDigit(char c) => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9');

    private static HashSet<string> BuildReservedDeviceNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CON", "PRN", "AUX", "NUL" };
        for (int i = 1; i <= 9; i++)
        {
            names.Add(FormattableString.Invariant($"COM{i}"));
            names.Add(FormattableString.Invariant($"LPT{i}"));
        }

        return names;
    }
}
