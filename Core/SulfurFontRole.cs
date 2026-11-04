namespace Ryuka.Sulfur.NativeUI
{
    /// <summary>
    /// What a piece of text is for, in the game's own font vocabulary. The game
    /// keeps one font per role and swaps all of them when the language changes, so
    /// asking by role always yields a font that matches both the surrounding game UI
    /// and the current language.
    ///
    /// Where the game itself uses each role (1.0.4):
    ///  - <see cref="SerifRegular"/>: menus, the options screen and every option row.
    ///  - <see cref="SerifSemibold"/>: small HUD text (floor info), dialogue body.
    ///  - <see cref="SerifBold"/>: centred HUD alerts, character titles.
    ///  - <see cref="RoundRegular"/>: resource counters, "press to continue".
    ///  - <see cref="RoundSemibold"/>: button legends, rank text.
    ///
    /// Round fonts in English are Latin-only display faces; <see cref="SulfurFonts"/>
    /// attaches a CJK/symbol fallback to every font it hands out, so any role can
    /// show any language's text.
    /// </summary>
    public enum SulfurFontRole
    {
        RoundRegular = 0,
        RoundSemibold = 1,
        RoundBold = 2,
        SerifRegular = 3,
        SerifSemibold = 4,
        SerifBold = 5
    }
}
