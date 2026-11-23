namespace SlideStudio.Core.Model;

/// <summary>
/// Position, taille et rotation d'un élément sur la diapositive (en unités logiques, pas en pixels).
/// </summary>
/// <remarks>
/// POURQUOI un seul objet pour ces cinq nombres : déplacer, redimensionner et pivoter sont une seule
/// action annulable (un glisser de souris = une entrée d'historique), et la validation les contrôle ensemble.
/// Les valeurs ne sont validées que lorsqu'elles sont affectées à un élément (voir <c>DocumentRules.CheckFrame</c>).
/// </remarks>
/// <param name="X">Abscisse du coin supérieur gauche.</param>
/// <param name="Y">Ordonnée du coin supérieur gauche.</param>
/// <param name="Width">Largeur (au moins <see cref="Limits.MinElementSize"/>).</param>
/// <param name="Height">Hauteur (au moins <see cref="Limits.MinElementSize"/>).</param>
/// <param name="RotationDegrees">Rotation en degrés, dans l'intervalle [0 ; 360[.</param>
public readonly record struct ElementFrame(double X, double Y, double Width, double Height, double RotationDegrees = 0)
{
    /// <summary>Renvoie un cadre décalé de <paramref name="deltaX"/> et <paramref name="deltaY"/>.</summary>
    /// <param name="deltaX">Décalage horizontal.</param>
    /// <param name="deltaY">Décalage vertical.</param>
    /// <returns>Nouveau cadre ; l'original n'est pas modifié.</returns>
    public ElementFrame Moved(double deltaX, double deltaY) => this with { X = X + deltaX, Y = Y + deltaY };

    /// <summary>Renvoie un cadre de même position avec une nouvelle taille.</summary>
    /// <param name="width">Nouvelle largeur.</param>
    /// <param name="height">Nouvelle hauteur.</param>
    /// <returns>Nouveau cadre ; l'original n'est pas modifié.</returns>
    public ElementFrame Resized(double width, double height) => this with { Width = width, Height = height };

    /// <summary>
    /// Ramène un angle quelconque (par exemple 370° ou −90° après plusieurs rotations à la souris) dans [0 ; 360[.
    /// </summary>
    /// <remarks>
    /// Une valeur non finie (NaN, infini) est renvoyée telle quelle : on ne « répare » pas silencieusement une valeur
    /// absurde, la validation la refusera.
    /// </remarks>
    /// <param name="degrees">Angle en degrés.</param>
    /// <returns>Angle équivalent dans [0 ; 360[.</returns>
    public static double NormalizeRotation(double degrees)
    {
        if (!double.IsFinite(degrees))
        {
            return degrees;
        }

        double remainder = degrees % 360;
        double normalized = remainder < 0 ? remainder + 360 : remainder;

        // ROBUSTESSE : un angle négatif infime (-1e-20) donne 360 exactement après l'addition à cause de
        // l'arrondi des nombres à virgule ; or 360 est hors de l'intervalle [0 ; 360[. On le ramène à 0.
        return normalized >= 360 ? 0 : normalized;
    }
}
