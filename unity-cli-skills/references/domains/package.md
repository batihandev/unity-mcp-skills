# Package inspection and lifecycle

Read [foundation routing and safety](foundation.md) first. Discover native package commands and keep one
package operation active at a time. Package mutations are non-Undo and may compile/reload the project.

- List with `package_list`; an installed check selects one exact name from that installed result.
- Query available versions and dependencies with exact-name `package_search` and its returned metadata.
  Never infer a version absent from that response.
- Add/remove use the guarded native operations. Retain request acknowledgement, poll `package_status` to a
  terminal state, verify the exact manifest and installed result, then require terminal recompilation.
- Resolve only after caller authorization and a manifest preview; use the native resolve operation and the
  same terminal status, manifest, package-list, and compile checks.

Capture the original manifest and package result before mutation. Dry-run wins over confirmation; an
unconfirmed mutation refuses. Apply only an owned reversible add/remove fixture, restore its exact prior
state, and verify no active package/reload operation remains.

Embedding and sample import remain staged for the Task 10 core asset/package-authoring slice. Their legacy
entrypoints stay intact until that slice proves exact selected package/sample, guarded mutation, path
confinement, terminal reload, import result, and inverse cleanup. Do not claim this reference supplies those
two operations yet.
