# Search improvements roadmap

Concrete, scoped follow-ups to the work in `docs/proposal.md`.
Organized by impact, with rough cost estimates so maintainers can see
what each item costs before approving direction.

This file exists to make it easy to say "yes to A, no to B, defer C"
*before* anyone builds them.

## Tier 1 — demo-critical UX gaps

These would land before a real user-facing release. Each closes a gap
where the indexed provider returns surprising results today.

### Mid-string substring matching
**Cost:** ~3 hours. **Index size:** ~2× current.
Today `"phab"` doesn't match `ALPHABET.md` because Tantivy tokenizes
on word boundaries and we only do prefix queries. Fix: add a parallel
`filename_ngrams` field with a trigram tokenizer; route to it when the
query has no whitespace and the prefix field returns nothing.

### Underscore-friendly tokenization
**Cost:** ~2 hours. **Index size:** unchanged.
Default tokenizer splits on `_`, so `brand_new.txt` tokenizes to
`["brand", "new", "txt"]` and a query of `brand_new` matches nothing.
Fix: custom tokenizer that keeps `_` as a word character but still
splits on `.` / `-` / whitespace. Or: index the whole filename as a
second field and search both.

### Glob support (`*.txt`, `report-*-final.docx`)
**Cost:** ~4 hours router-only, ~1 day for native Tantivy regex.
Today the router falls back to legacy on `*` / `?`. Cleaner: detect
glob shape, route to a Tantivy `RegexQuery` over an `extension` field
plus a name predicate. The router-fallback is good enough for v0;
native handling buys consistency.

### Skip noise paths in the default walk
**Cost:** ~2 hours. **Index size:** -20–40% on typical home dirs.
`%USERPROFILE%` includes `AppData\Local\Temp`, browser caches,
`node_modules`, `.git/objects`, etc. They balloon the index and
pollute results. Add a configurable skip-list with sensible defaults;
honor `.gitignore`-style files at root.

### Recency boost in scoring
**Cost:** ~3 hours.
BM25 alone doesn't surface "the file you were editing yesterday"
above "a five-year-old file with the same name." Boost via Tantivy's
`BoostQuery` over `modified_unix_ms`, linear decay over the last
~30 days. Makes results feel intuitive without being magic.

## Tier 2 — robustness before public release

### Restart-time index reconcile
**Cost:** ~4 hours.
Watcher only catches changes while the service is running. If files
change while it's offline, the index goes stale until next manual
rebuild. Fix: at startup, walk root, diff against indexed paths +
mtimes, apply deltas. Closes the "I deleted this yesterday but it
still shows up" bug.

### Exact-match scoring tier
**Cost:** ~4 hours.
Currently `"report"` weights `report.txt` and
`quarterly_report_draft_v2.txt` similarly. Add explicit scoring tiers:
exact filename > exact name without extension > prefix > substring >
extension match. Single-field weighting won't get there.

### Faceted refinement
**Cost:** ~1 day.
Return facet counts (file type, date bucket, size bucket) alongside
results. UI can offer "5,234 results, 1,200 are PDFs" filtering.
Tantivy supports this natively via `FacetCollector`.

### Service crash + auto-restart
**Cost:** ~3 hours.
The C# `IndexedSearchProvider` already handles transport errors
gracefully. The service-launcher (separate roadmap item) should also
detect a crashed process and respawn it. Lock-file handling for
crash-recovery so Tantivy's `LockBusy` doesn't strand users.

## Tier 3 — capability expansion

### Content indexing
**Cost:** ~1 week per format tier.
Add a `content` TEXT field, populate from text-like formats first
(`.txt/.md/.log/.cs/.json/.html` etc.). Office formats need an
extractor (e.g., `dotnet-extract` or a Rust port of Apache Tika
shapes). PDF needs a parser. Each tier expands scope significantly;
start with text-only, gate the rest on user demand.

### Frequency-of-access boost
**Cost:** ~1 day, plus C# instrumentation.
Track how often the user opens each file (Files.App emits "user opened
path X" events to the service). Boost frequent files in scoring.
Real win, real privacy implication — needs an opt-out.

### Saved searches + search history
**Cost:** ~3 days, mostly UI.
Persistent saved searches ("project files modified this week"),
quick-recall of recent queries. Lives mostly in Files.App's settings
UI; the service surface stays the same.

### Fuzzy matching for typos
**Cost:** ~2 hours to enable.
`"repotr"` → `"report"`. Tantivy supports `FuzzyTermQuery` with edit
distance; only enable when no exact / prefix / substring match. Real
performance hit on large corpora; would gate on bench numbers.

## Tier 4 — long-term, opt-in

### Semantic / vector search
**Cost:** ~2 weeks. **RAM cost:** 200–500 MB on 100k files.
Sentence-transformer embeddings of filenames + HNSW index. The "find
me files about taxes" use case. Substantial cost; only worth doing
once content indexing is in place. Opt-in feature.

### "Turbo Mode" for power users
**Cost:** several weeks. **Requires admin.**
Per CLAUDE.md goal #3, default mode never asks for UAC. A future
opt-in mode could use MFT parsing for cold-start orders of magnitude
faster than `FindFirstFileEx`, plus filesystem filter drivers for
zero-latency change detection. Would ship behind a one-time UAC
prompt with a clear explanation. Architecture is friendly to bolting
this on as a third `ISearchProvider` impl.

### Compression of stored fields
**Cost:** ~1 day.
Paths share long prefixes (`C:\Users\Tommy\Documents\...`). Prefix
coding in Tantivy stored fields could cut index size 30–50%. Trades
read latency for disk; would gate on whichever the bench shows
matters more.

## What we'd want maintainer input on

Roughly in order of how much it changes our plan:

1. **Tier 3 content indexing** — yes/no, and which format tiers.
   Privacy-adjacent (we'd be reading file contents into an index);
   could be opt-in or opt-out.
2. **Tier 4 semantic search** — yes / no / opt-in. Adds a
   meaningful RAM and disk cost.
3. **Tier 4 Turbo Mode** — would you ever accept an admin-mode
   opt-in, or is no-admin a hard line?
4. **Tier 3 frequency boost** — privacy implications of tracking
   file access; needs a settings toggle minimum.
5. **Tier 1 / Tier 2** — assumed yes pending direction. These close
   bugs, not introduce features.
