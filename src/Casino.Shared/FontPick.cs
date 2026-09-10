using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Casino.Shared
{
    /// <summary>
    /// Picks the font every casino label draws with.
    ///
    /// The tables used to borrow "whatever TMP_FontAsset is around", which in an
    /// English install is a font with no CJK glyphs in it. Chinese then rendered
    /// through the global fallback chain, and a glyph drawn by a fallback font whose
    /// material does not match the base font comes out wrong (dark strokes full of
    /// light noise). The fix is to make the base font itself carry the glyphs:
    /// find a loaded TMP_FontAsset that already contains both CJK and Latin, and use
    /// that for everything. Chinese-localising font mods (e.g. FontReplace) load such
    /// assets, so in a Chinese install this finds them; in an English-only install it
    /// falls back to the old "first asset" behaviour.
    /// </summary>
    internal static class FontPick
    {
        internal static TMP_FontAsset Pick()
        {
            try
            {
                var all = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                if (all == null)
                {
                    return null;
                }

                // A static atlas that already holds both a CJK glyph and an ASCII one
                // is a complete font: every casino string draws from a single material.
                foreach (var f in all)
                {
                    if (f == null)
                    {
                        continue;
                    }
                    try
                    {
                        if (f.HasCharacter('赌') && f.HasCharacter('A'))
                        {
                            return f;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                // Same test but tolerate fonts without the ASCII glyph (Chinese text
                // still reads; Latin would fall back per-glyph).
                foreach (var f in all)
                {
                    if (f == null)
                    {
                        continue;
                    }
                    try
                    {
                        if (f.HasCharacter('赌'))
                        {
                            return f;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                return all.FirstOrDefault();
            }
            catch (Exception)
            {
                try
                {
                    return Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}
