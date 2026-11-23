namespace SlideStudio.Core.Model;

/// <summary>Type de forme géométrique disponible dans la barre d'outils.</summary>
public enum ShapeKind
{
    /// <summary>Rectangle (ou carré).</summary>
    Rectangle = 0,

    /// <summary>Ellipse (ou cercle).</summary>
    Ellipse = 1,

    /// <summary>Ligne tracée du coin supérieur gauche au coin inférieur droit du cadre.</summary>
    Line = 2,
}
