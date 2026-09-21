# Profiler capture analysis

These workflows are verified against Unity `6000.4.2f1`, Unity CLI `1.0.0-beta.9`, Pipeline
`0.7.0-exp.1`, and Input System `1.20.0`. Read [foundation routing and safety](foundation.md) first. Discover
each command with `unity command --project-path "$PROJECT_PATH" --query <name> --detail full` because the
installed catalog owns the request syntax.

Profiler history is mutable global Editor state. Stop recording before selecting it, then call
`profiler.session.current` for stopped memory or `profiler.session.open` with one explicit project-relative
`.raw` or `.data` path. Pass the returned `SessionId` to every analysis command. A capture load, clear, new
frame, history rollover, or structural fingerprint change expires the session and every opaque ID from it.
Reloading a capture mutates profiler history, does not change project assets, and is not Undoable.

The result envelope at `data.result` is `CommandResult<T>` with PascalCase properties. Normal hierarchy,
raw occurrence, and bottom-up IDs are distinct. Keep each ID with its session, frame, and exact thread;
`SAMPLE_ID_KIND_MISMATCH`, `SAMPLE_ID_STALE`, and `SAMPLE_ID_INVALID` identify different failures. Use an
exact `threadIndex` or exact `threadName`, never both. An omitted selector requires one `Main Thread`, and a
duplicate exact name returns `THREAD_AMBIGUOUS`.

## Timing commands

- `profiler.time.range(sessionId, firstFrameIndex, lastFrameIndex, targetMs)` returns
  `unity.profiler.time-range@1`. The range is inclusive and must contain every integer frame. It echoes the
  session, bounds, and required positive finite target. Median averages the two middle values for even sets;
  p95 uses nearest rank; maximum ties choose the lowest frame index. Equality is within budget.
- `profiler.time.frame-total(sessionId, frameIndex, targetMs, threadIndex?, threadName?, limit?)` returns
  normal hierarchy IDs and rows ordered by total time, semantic path, then ID.
- `profiler.time.frame-self(sessionId, frameIndex, threadIndex?, threadName?, limit?)` returns bottom-up IDs
  ordered by self time, ordinal path set, then ID. `MarkerPaths` contains every proven normal path for the
  aggregate, sorted and deduplicated by the complete segment sequence. Pass one returned ID to
  `profiler.time.bottom-up`.
- `profiler.time.sample(sessionId, frameIndex, sampleId, threadIndex?, threadName?)` accepts a normal ID and
  returns ancestors, direct children, and raw occurrences. Use a raw occurrence ID as the source for
  `profiler.time.related` so its interval is an actual occurrence rather than an aggregate row.
- `profiler.time.bottom-up(sessionId, frameIndex, bottomUpId, threadIndex?, threadName?)` accepts only a
  bottom-up ID and returns the aggregate path set, callers with their own bottom-up IDs and path sets, and
  the raw occurrences used for total reconciliation.
- `profiler.time.marker-path(sessionId, frameIndex, markerPath, threadIndex?, threadName?)` requires the full
  ordered marker-name array and returns its normal ID. Lookup is exact; missing and duplicate paths fail.
- `profiler.time.related(sessionId, frameIndex, sourceSampleId, sourceThreadIndex?, sourceThreadName?,
  targetThreadIndex?, targetThreadName?)` accepts a raw source occurrence. It returns positive overlaps on
  the exact target thread using half-open intervals. Rows are ordered by overlap duration, start time, path,
  and ID. `Relation` is `flow` only when matching Unity flow events exist; `temporal-overlap` makes no
  causality claim.

All successful numeric fields are finite. A nonfinite or nonpositive target returns `TARGET_MS_INVALID`.
A valid target whose derived ratio or percentage cannot be represented returns `PROFILER_DATA_UNAVAILABLE`
with no partial result.

## Allocation commands

- `profiler.gc.overall(sessionId, thresholdBytes=8192)` returns `unity.profiler.gc-overall@1` across every
  frame and thread. `TotalBytes` and `TotalCount` include every observed allocation event once. Rows and
  reported totals include frame/thread/site groups whose direct bytes meet the non-negative threshold,
  combined afterward by exact semantic path.
- `profiler.gc.frame(sessionId, frameIndex, threadIndex?, threadName?, thresholdBytes=8192)` returns
  `unity.profiler.gc-frame@1`. Rows carry normal sample IDs and direct allocation bytes/count, ordered by
  bytes, path, then ID. Top-level totals include below-threshold sites.
- `profiler.gc.range(sessionId, firstFrameIndex, lastFrameIndex, thresholdBytes=8192)` returns
  `unity.profiler.gc-range@1` for an inclusive contiguous range and every thread. Its frame summaries
  reconcile with frame results summed across threads. Statistics use total frame bytes, include zero frames,
  use an exact average for an even median, nearest-rank p95, and the lowest frame index for a maximum tie.
- `profiler.gc.sample(sessionId, frameIndex, sampleId, threadIndex?, threadName?)` accepts one normal sample
  ID. `profiler.gc.marker-path(sessionId, frameIndex, markerPath, threadIndex?, threadName?)` resolves one
  exact normal path. Both return inclusive allocation totals for the selected subtree and direct totals for
  allocations owned by the selected site.

Allocation metadata comes from recorded `GC.Alloc` occurrences. A valid raw view with no occurrences is an
explicit zero; an observed occurrence without byte metadata returns `ALLOCATION_DATA_UNAVAILABLE`. An event
whose normal site or native allocation node cannot be proven returns `PROFILER_DATA_UNAVAILABLE`. These
errors have no partial result, and no command derives allocation values from timing or call counts.

## Q01: range spike triage

Select a stopped session, then call `profiler.time.range` with its exact `SessionId`, inclusive bounds, and
the investigation budget. Return the `unity.profiler.time-range@1` domain result unchanged. Use
`MaxFrameIndex` for the next frame drill-down; do not recompute the statistics or budget fields in the skill.

## Q02: top hierarchy for one frame

Select a stopped session, then call `profiler.time.frame-total` with its exact `SessionId`, one frame,
the investigation budget, and at most one exact thread selector. Return the
`unity.profiler.frame-total@1` result unchanged. Preserve the row order, normal `SampleId` values, complete
`MarkerPath` arrays, `TargetMs`, and every frame/row budget annotation. An omitted selector is valid only
when the frame has one exact `Main Thread`.

## Q03: range allocation triage

Call `profiler.session.current` or `profiler.session.open`, then pass its exact `SessionId` and the requested
inclusive bounds to `profiler.gc.range`. Return the `unity.profiler.gc-range@1` result unchanged. Use
`MaxFrameIndex` for a frame drill-down and retain `ThresholdBytes`, total/reported counters, and
`PercentileMethod`; do not recompute them in the skill.

## Q04: frame allocation triage

Select a stopped session, frame, and exact thread, then call `profiler.gc.frame`. Return the
`unity.profiler.gc-frame@1` result unchanged. Use a row's normal `SampleId` with `profiler.gc.sample`, or its
complete `MarkerPath` with `profiler.gc.marker-path`, when inclusive subtree attribution is needed.

## Q05: leaf, caller, and path investigation

Q05 is a skill composition, not a package command. For one session, frame, exact thread, and full marker
path:

1. Call `profiler.time.frame-self` and select the exact row whose sole `MarkerPaths` entry equals the
   requested path. If the aggregate has multiple paths, return `MARKER_PATH_AMBIGUOUS` with the candidates
   and no partial result.
2. Call `profiler.time.bottom-up` with that row's `BottomUpId`.
3. Call `profiler.time.marker-path` with the same full path and retain its `MarkerSampleId`.
4. Compare leaf self time with bottom-up self time, and bottom-up occurrence total with marker total. A pair
   matches when its absolute delta is at most `0.01` ms or its relative delta is at most `1%`. For a nonzero
   pair, relative delta is `100 * abs(left-right) / max(abs(left),abs(right))`; the absolute test handles two
   zeros.

Return `unity.profiler.investigation@1` with `SessionId`, `FrameIndex`, `Thread`, `MarkerPath`, `LinkedIds`,
`Leaf`, `BottomUp`, `Marker`, and `Reconciliation`. `LinkedIds` contains the bottom-up and normal IDs. Each
source object names its command schema. `Reconciliation` contains `AbsoluteToleranceMs`,
`RelativeTolerancePercent`, `LeafSelfMs`, `BottomUpSelfMs`, `CallerOccurrenceTotalMs`, `MarkerTotalMs`,
`LeafMatchesBottomUp`, and `CallersWithinMarker`.

If any source call fails, return no partial result. Use its error code and details
`{step, command, sourceCode}`. If either comparison fails, return `PROFILER_DATA_UNAVAILABLE`, no partial
result, step `reconciliation`, and the observed deltas. Do not use Pipeline `batch` unless live discovery and
evidence establish that its result preserves this all-or-nothing aggregate contract.

## Marker source search

P14–P17 are shell operations. They do not install or invoke the optional package and must not be exposed as
Unity commands, aliases, wrappers, or persistent helper scripts. Run them from a shell with `rg` available
and emit the named JSON schema so later steps can verify the result kind.

Apply this confinement before reading any candidate. Canonicalize the project root, accept only normalized
project-relative `/` paths beneath `Assets/` or `Packages/`, and inspect every path component without
following links. Reject a symbolic link, junction, mount-point reparse entry, non-regular file, or canonical
escape. Skip any component named `.git`, `Library`, `Temp`, `Logs`, `Obj`, `Builds`, `UserSettings`, or
`TestResults`. Report `PATH_REQUIRED`, `PATH_OUTSIDE_ROOT`, `PATH_TRAVERSAL`, `PATH_REPARSE_POINT`,
`PATH_INVALID`, `PATH_ACCESS_DENIED`, `PATH_IO_ERROR`, `FILE_NOT_FOUND`, or `NOT_REGULAR_FILE` as applicable;
never include an absolute host path in the result.

### P14: content and path regex search

Require a content pattern and accept an optional path pattern. Validate each by invoking `rg` with
`--regexp` against empty input before traversal: exit 2 is `INVALID_PATTERN`, while exit 1 is a valid regex
with no match. Use `rg`'s default Rust-regex dialect for both expressions. Enumerate non-link `.cs` files
only from the confined roots, normalize each relative path, filter the complete relative path with the path
pattern when supplied, and run `rg --json --line-number --column --color never --regexp <content> -- <file>`
without `--follow`. Treat each reported submatch as one match. Column is the one-based UTF-8 byte column
reported by `rg`; line is one-based. Decode the physical line as strict UTF-8, remove only its line
terminator, and return at most 240 Unicode scalar values in `Excerpt`, setting `ExcerptTruncated` when more
scalars exist.

Sort all matches by normalized path, line, column, then excerpt using ordinal comparison before taking the
first 200. Return `host.file-search@1` with `pattern`, optional `pathPattern`, `totalMatches`,
`returnedMatches`, `truncated`, and `matches`; each row contains `path`, `line`, `column`, `excerpt`, and
`excerptTruncated`. No match is a successful empty result. A 201st match increments `totalMatches` and sets
`truncated`; it is never silently discarded from the metadata.

### P15: exact file line count

Apply the confinement rules to one normalized project-relative file, then read at most 1,048,577 bytes so
an oversized file is detected before decoding. The 1,048,576-byte cap includes a UTF-8 BOM. Reject NUL as
`PATH_INVALID`, malformed UTF-8 as `PATH_INVALID`, and an oversized file as `FILE_TOO_LARGE`. Strip one
leading UTF-8 BOM from content. The empty remainder has zero lines. Otherwise count LF bytes and add one
only when the final byte is not LF; CRLF is one delimiter and a trailing newline adds no synthetic line.
Return `host.file-lines@1` with `path`, `contentUtf8Bytes` after BOM removal, `hasUtf8Bom`, and `lineCount`.

### P16: exact file slice

Apply P15's confinement, size, NUL, BOM, and strict UTF-8 rules. Require a zero-based non-negative
`startLine`. Accept `count=-1` for the remainder or `count=0..400`; reject other values as
`SLICE_LIMIT_EXCEEDED`. Split logical lines after each LF while retaining every original terminator, so LF,
CRLF, mixed endings, and a final unterminated line round-trip unchanged. Select consecutive complete lines
from `startLine`, stopping before line 401 or before the first line that would make the returned content
exceed 65,536 UTF-8 bytes. If any selected logical line alone exceeds that byte cap, return
`LINE_TOO_LONG` with no content. EOF clipping succeeds.

Return `host.file-content@1` with `path`, `startLine`, `requestedCount`, `returnedLineCount`, `totalLines`,
`contentUtf8Bytes`, `nextLine`, `truncated`, and `content`. `nextLine` is `startLine + returnedLineCount`.
`truncated` is true only when requested content remains after the 400-line cap or
the byte cap; it is false for ordinary EOF clipping and for `count=0`.

### P17: exact marker declaration resolution

Require one exact marker string and search the same confined `.cs` set, ordering, and 200-candidate cap as
P14. Perform a lexical pass over each complete file before matching declarations. Track code, line comment,
block comment, character literal, ordinary string, verbatim string, interpolated string, and raw-string
states across physical lines. A declaration may begin only in code state; text inside any comment or string
is never a declaration. The declaration's argument itself must be one non-interpolated ordinary C# string
literal on that physical line. Decode its legal simple escapes, `\x` (one to four hex digits), `\u` (four),
and `\U` (eight) to Unicode scalar values before ordinal comparison, including escaped quotes and
backslashes. Reject malformed escapes and invalid scalar values rather than treating their source spelling
as a marker. Verbatim, interpolated, raw, concatenated, non-literal, and multiline arguments are not
candidates.

Tokenize identifiers and punctuation for the declaration match. Each qualified name below must match its
complete token sequence with an identifier boundary on both sides. In particular, `myProfiler.BeginSample`,
`OtherProfilerMarker`, and an unrelated member whose name merely ends in one of these sequences do not
match. Permit whitespace between the displayed tokens, but do not infer aliases, extension methods, or
other qualification. Recognize only these forms:

```text
new ProfilerMarker("...")
new Unity.Profiling.ProfilerMarker("...")
ProfilerMarker <identifier> = new("...")
Profiler.BeginSample("...")
UnityEngine.Profiling.Profiler.BeginSample("...")
CustomSampler.Create("...")
UnityEngine.Profiling.CustomSampler.Create("...")
```

Return `host.marker-source@1` with `marker`, `resolution`, `totalCandidates`, `returnedCandidates`, `truncated`, and
ordinally sorted `candidates` containing normalized path, line, column, declaration form, and excerpt. One
candidate is `resolved-managed`, two or more are `ambiguous`, and zero is `not-found`; never auto-select an
ambiguous candidate. Return `unresolved-native` only when the caller also supplies a selected profiler
session/frame marker record whose origin field explicitly classifies this exact marker as native. A
native-looking name or namespace is not origin evidence.

## Q06: marker to source

Start with P17 and preserve its `host.marker-source@1` result. For `resolved-managed`, pass the sole
candidate's exact normalized path to P16 and choose a bounded slice around its zero-based source line
(`candidate.line - 1`); preserve the P17 resolution and the complete `host.file-content@1` result. Keep
`ambiguous`, `not-found`, and `unresolved-native` as terminal explicit outcomes, with no guessed file and no
source slice. Package installation is a separate project-owner decision and is unnecessary for Q06.
