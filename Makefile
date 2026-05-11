.PHONY: help publish dry-run lint tracker next

help:
	@echo "make publish   — sync dev → main (installable paths only)"
	@echo "make dry-run   — show what publish would change; don't commit"
	@echo "make lint      — sweep ship paths for known stale/forbidden patterns"
	@echo "make tracker   — tracker summary header"
	@echo "make next      — next 5 pending recipes for each gate"

publish: lint
	@tools/publish.sh

dry-run: lint
	@tools/publish.sh --dry-run

lint:
	@echo "→ Newtonsoft sweep (recipes/, skills/, references/, root MDs)"
	@! grep -rn "Newtonsoft" recipes/ skills/ references/ README.md SKILL.md mcp-tools.md 2>/dev/null \
		|| { echo "FAIL: Newtonsoft refs in ship paths — use MiniJson (_shared/execution_result.md)"; exit 1; }
	@echo "→ AssetDatabase.DeleteAsset sweep (analyzer kills the module on this token)"
	@! grep -rn "AssetDatabase\.DeleteAsset(" recipes/ 2>/dev/null \
		|| { echo "FAIL: use AssetDatabase.MoveAssetToTrash instead"; exit 1; }
	@echo "→ using System.Reflection; sweep (triggers Unity_RunCommand reformatter NRE)"
	@! grep -rn "^using System\.Reflection;" recipes/ 2>/dev/null \
		|| { echo "FAIL: fully-qualify reflection types (System.Reflection.MethodInfo etc.)"; exit 1; }
	@echo "✓ lint passed"

tracker:
	@sed -n '1,25p' docs/superpowers/notes/recipe-validation-tracker.md

next:
	@for g in ext pre comp run; do \
		echo "--- $$g ---"; \
		python3 tools/tracker_next.py --gate $$g --limit 5; \
	done
