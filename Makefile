.PHONY: help publish dry-run tracker next

help:
	@echo "make publish   — sync dev → main (installable paths only)"
	@echo "make dry-run   — show what publish would change; don't commit"
	@echo "make tracker   — tracker summary header"
	@echo "make next      — next 5 pending recipes for each gate"

publish:
	@tools/publish.sh

dry-run:
	@tools/publish.sh --dry-run

tracker:
	@sed -n '1,25p' docs/superpowers/notes/recipe-validation-tracker.md

next:
	@for g in ext pre comp run; do \
		echo "--- $$g ---"; \
		python3 tools/tracker_next.py --gate $$g --limit 5; \
	done
