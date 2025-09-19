<img src="docs/icon.png" alt="TLS.Shared" width="96" align="left" />

### TLS.Shared
Shared lib for MCM and TLS.ModKit

[![Version](https://img.shields.io/badge/version-1.0.0-blue)](https://github.com/stunl0ck/tls-mcm)

Used in
**[MCM](https://github.com/stunl0ck/tls-mcm)** and **[TLS.ModKit](https://github.com/stunl0ck/tls-modkit)**


## Summary

Small shared helper library for **The Last Spell** modding ecosystem.  
Currently ships a lightweight **Localization** utility that merges per-mod CSVs into the game’s `TPLib.Localization.Localizer` at runtime and provides simple lookup helpers.

- **CSV → Localizer merge**: read semicolon-separated CSVs (e.g., `Key;English;Français;…`) and inject/update entries in the game’s localization dictionary.
- **Per-mod scan**: walk every folder under `BepInEx/plugins/` and merge a CSV at a relative path you specify (e.g., `MCM/languages.csv` or `ModKit/languages.csv`).
- **Idempotent & thread-safe**: tracks merged files and won’t re-apply the same CSV; guarded by a lock.
- **Fallbacks**: lookup helpers fall back to current language → English → provided default.
- **Zero dependencies on BepInEx**: you can wire any logger via delegates.


## Features

- `Localization.MergeCsvsUnder(pluginsRoot, relativeCsvPath, logger?)`
  - Scans each subfolder of `pluginsRoot` for `relativeCsvPath` and merges any hits.
- `Localization.MergeCsv(csvPath, label?, logger?)`
  - Merges a single CSV file.
- `Localization.LocalizeOrDefault(key, fallback = "")`
  - Looks up a key in the current language, falls back to English, then to `fallback`.
- `Localization.LocalizeOrDefault(modId, optionKey, field, fallback)`
  - Convenience for MCM keys: `${modId}_${optionKey}_${field}`.
- `Localization.LocalizeOrDefaultForModDescription(modId, fallback)`
  - Convenience for `${modId}_description`.


## CSV format

- **Header row** must list the language names exactly as the game expects (e.g., `English`, `Français`, …) in any order.
- **Delimiter**: semicolon `;`
- **Comments**: lines starting with `#` or `//` are ignored.

Example:

```csv
Key;English;Français
com.example.myMod_description;My mod description;Description de mon mod
com.example.myMod_EnableThing_displayName;Enable Thing;Activer la chose
com.example.myMod_EnableThing_description;Turns the thing on.;Active la chose.
```

## Notes

The merger aligns CSV columns to `TPLib.Localization.Localizer.knownLanguages`; unknown language headers are skipped.

If a key doesn’t exist in the Localizer yet, it is created with the correct length for all known languages.

Duplicate merges of the same absolute CSV path are ignored.

## License

MIT