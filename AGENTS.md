# Fork Invariants

## Behavior

- UI supports English and Simplified Chinese only.
- Item names and stats follow the parsed Grim Dawn language, independent of UI language.
- Respect the parsed `tagItemNameOrder`. Chinese names use prefix, suffix, then base item.
- Duplicate identity uses the canonical base item. Green affixes do not split groups.
- Duplicate filtering, comparison refresh, and remaining-count checks must use the same identity rule.
- The comparer shows all properties, but ranks only item affixes and base properties. Granted skills remain visible and unranked.
- Unique properties count as advantages. Shared identical properties rank last. Preserve source order within sections.
- After transfer, refresh and recalculate the comparer. Keep it open while at least two matching items remain.
- Transfer notifications must name the item and report the exact transferred quantity.
- Dismissing the Mod filter warning is persistent across restarts.
- Max-roll values must use the deterministic Grim Dawn seed calculation.

## Safety and build

- Do not change the PlayerItem schema, serialization, or write semantics for feature work.
- Read queries may be optimized without changing stored data meaning.
- Read and map all game data before replacing parsed tables.
- Only update parse settings, owned-item stats, and icons after a successful parse.
- Keep `UserData` beside the application. Never use `$HOME` or machine-specific paths.
- Keep log rotation bounded by `IAGrim/Log4net.config`.
- Build without `iagd-source` or any external source tree. Do not hardcode SDK, tool, or user paths.
- Use PATH tools by default. Repo-local `.tools` dependencies are optional and ignored by Git.
- `build-package.ps1` may clear only the validated repo `artifacts` directory. Output runnable files there without ZIP.

## Upstream merges

- Recheck language initialization, parsing, duplicate SQL, comparer alignment, transfer refresh, paths, logging, and packaging after every upstream merge.
- Prefer these fork invariants when upstream behavior conflicts with them.
