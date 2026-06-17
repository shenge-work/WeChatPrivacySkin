# Theme Assets

Theme packs are currently defined in code through `ThemeCatalog`.

The built-in `sponge-ocean` theme is intentionally code-generated with WPF
geometry and brushes. It keeps the repository free of copyrighted character
artwork while still demonstrating non-rectangular sponge, bubble, wave, coral,
and star-style decorations.

Future imported or custom theme files should live under:

```text
Assets/Themes/<theme-id>/
```

Custom transparent PNG overlay skins should live under:

```text
Assets/Skins/<skin-id>/
```

Suggested files:

- `theme.json` for colors, radii, frame, pattern, and behavior.
- `background.png` for optional overlay texture.
- `decorations.png` or small PNG/SVG assets for stickers and side ornaments.
