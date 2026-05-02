namespace ArcNET.Editor;

/// <summary>
/// Controls how one workspace-created <see cref="EditorArtResolver"/> should be seeded.
/// </summary>
public enum EditorArtResolverBindingStrategy
{
    /// <summary>
    /// Starts with no bindings. Hosts can add explicit bindings manually.
    /// </summary>
    None = 0,

    /// <summary>
    /// Binds only ART identifiers whose loaded asset path exposes one unique numeric token that
    /// exactly matches the referenced <see cref="ArcNET.Core.Primitives.ArtId"/>.
    /// This does not decode legacy AID bitfields and intentionally skips ambiguous matches.
    /// </summary>
    Conservative = 1,
}
