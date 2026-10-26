# Canvas and uGUI authoring

Read [foundation safety](foundation.md), [component identity](component.md), and [package lifecycle](package.md). These commands belong to the optional uGUI Editor assembly and require `com.unity.ugui` `[2.6.0,3.0.0)`. Discover each command in the selected Editor's catalog before dispatch. UI Toolkit uses its own `UIDocument`/UXML/USS owners; XR Canvas conversion uses [XR](xr.md).

Every typed `ui.*` mutation requires Edit mode, a loaded regular scene, and `confirm=true` or `dryRun=true`. Prefab Stage, preview scenes, assets, stale/wrong-type references, numeric enum strings, nonfinite numbers and invalid ranges are refused before authoring. A mutation creates one Undo step, including an automatically created Canvas and the complete widget subtree. Inspect nested `Ok`, `Error`, `Result.Applied`, exact `Target`/`Parent`/`Canvas`, `Backend`, actual `State` and `Issues`. Dry runs create nothing. Reacquire handles after Undo or scene reopening.

A supplied `parent` is an exact scene GameObject handle. Any regular GameObject can be a parent. Omitted parent uses the unique scene Canvas or creates an Overlay Canvas with CanvasScaler and GraphicRaycaster; multiple Canvases require an explicit parent. Names and hierarchy paths are display information, not exact identity.

Text controls accept `backend=Auto|Legacy|TMP`. Auto uses TMP only when project TMP Settings, default font, atlas, material and supported shader are ready; otherwise it uses builtin `LegacyRuntime.ttf`. Explicit TMP refuses missing resources before creating any object. Importing/configuring TMP Essential Resources is an explicit project content workflow. Factory success does not establish visibility, EventSystem/input readiness or application callbacks.

| Intent | Command and defaults |
| --- | --- |
| Create Canvas | `ui.canvas-create`: `name=Canvas`, `renderMode=ScreenSpaceOverlay`; named `ScreenSpaceCamera`/`WorldSpace` supported; optional exact Camera `camera` for Camera mode. Missing Camera is disclosed in Issues. |
| Create stretch panel | `ui.panel-create`: `name=Panel`, `r/g/b=1`, `a=.5`, zero edge offsets. |
| Create button | `ui.button-create`: `text=Button`, `width=160`, `height=30`, centered black 14-point label. |
| Create text | `ui.text-create`: `text=New Text`, `fontSize=14`, black `r/g/b=0`, 200×50. |
| Create image | `ui.image-create`: 100×100, optional `sprite` exact Sprite asset handle or single-Sprite asset path. Multi-Sprite paths require an exact subasset handle. |
| Create input | `ui.inputfield-create`: `placeholder=Enter text...`, 200×30, gray italic placeholder and linked black text. TMP retains its factory Text Area/RectMask2D and references. |
| Create slider | `ui.slider-create`: 160×20, `minValue=0`, `maxValue=1`, `value=.5`. Require finite strictly ordered range and in-range value. |
| Create toggle | `ui.toggle-create`: `text=Toggle`, `isOn=false`, 160×20, linked target/checkmark graphics. |
| Create dropdown | `ui.dropdown-create`: 160×30, trimmed comma-separated `options` or A/B/C; caption/item/template references, inactive 150-high template. |
| Create ScrollView | `ui.scrollview-create`: 300×200, `horizontal=false`, `vertical=true`, `movementType=Elastic`; RectMask2D viewport, top-anchored 400-high content, no scrollbars. |
| Create texture view | `ui.rawimage-create`: 100×100, optional `texture` exact Texture/RenderTexture asset or path; actual `hasTexture` returned. |
| Create scrollbar | `ui.scrollbar-create`: `direction=BottomToTop`, `value=0`, `size=.2`, `numberOfSteps=0`; vertical 20×160 or horizontal 160×20. |
| List UI | `ui.elements`: `limit=50` (0–10000), optional `uiType=Canvas|Button|Slider|Toggle|InputField|Text|Image|RawImage|RectTransform`. Includes inactive Canvas descendants, deduplicates nested Canvas trees, maps TMP to Text/InputField, returns stable exact identities/path/type/active. |
| Arrange children | `ui.layout-children`: exact GameObject `target`, `layoutType=Vertical|Horizontal|Grid`, `spacing=10`, integer paddings=0, `gridColumns=3`, force-expand flags=false. Grid uses first RectTransform child's sizeDelta. Existing ContentSizeFitter is preserved; a new fitter uses PreferredSize only on the linear layout axis. |
| Align rectangles | `ui.align`: comma-separated exact GameObject `targets`, otherwise Selection; at least two distinct RectTransforms; `alignment=Left|Center|Right|Top|Middle|Bottom`. |
| Distribute rectangles | `ui.distribute`: exact `targets` or Selection; at least three distinct RectTransforms; `direction=Horizontal|Vertical`. |

Alignment uses anchored positions, pivots and rect extents in each object's parent-relative context. Center/Middle use mean anchored coordinates. Left/Bottom use minimum pivot-adjusted edges; Right/Top use maximum edges. Different parents are allowed: equal numeric coordinates may occupy different world or screen positions. Distribution sorts anchored positions, preserves endpoint positions and the other axis, and evenly spaces anchored coordinates without subtracting extents.

## Existing component composition

Inspect the exact GameObject and its components before choosing an add/reuse route. Normalize/validate every requested value before dispatch and preserve omitted fields. Use one native `set_component_properties` for an existing component; use native transactional `batch` for supported add-and-set operations. An add result's `instanceId` can be referenced as `$add.instanceId` by a later operation. ObjectRef JSON uses `{"instanceId": ENTITY_ID}`; obtain the actual 64-bit handle without narrowing it. Always read actual resulting state and inspect every nested result.

For computed/public-member writes inside batch, follow [typed batch result checks and restoration](foundation.md#typed-results-inside-a-batch). An outer successful batch does not prove nested typed success. Retain `undoGroup`, check all nested `Ok`, and immediately restore through the canonical owner if any typed write fails, before another mutation. Do not claim automatic typed-failure rollback. Direct native serialized patches validate all fields before their single apply.

| Intent | Owner and verified fields |
| --- | --- |
| Set text | `component.member-set` on exact Text/TextMeshProUGUI Component, public `text` member, `value` string. Inspect the selected backend first. |
| Set rect dimensions/position | One native patch with only requested `m_SizeDelta.x/.y` and `m_AnchoredPosition.x/.y`. These are sizeDelta values; `rect-transform.size` owns physical rect size. |
| Set rect edge offsets | Read current public `offsetMin`/`offsetMax`; merge requested left/bottom into min x/y and negative right/top into max x/y, preserving omitted axes. Write each supplied merged Vector2 through `component.member-set` in one native batch, then apply the canonical nested-result restoration contract above. Read back computed offsets, sizeDelta and anchoredPosition. |
| Configure Image | One atomic native patch: `m_Type`, `m_FillMethod`, `m_FillAmount`, `m_FillClockwise`, `m_FillOrigin`, `m_PreserveAspect`, `m_Sprite`, `m_PixelsPerUnitMultiplier`. Validate named enums, finite fill [0,1], valid origin for the effective fill method, positive density multiplier. Resolve actual Sprite subasset/handle before any write. PNG main Texture is not necessarily Sprite. |
| Ensure LayoutElement | Reuse exact existing component, or transactional add+set; optional `m_MinWidth`, `m_MinHeight`, `m_PreferredWidth`, `m_PreferredHeight`, `m_FlexibleWidth`, `m_FlexibleHeight`, `m_IgnoreLayout`, `m_LayoutPriority`. Preserve omitted fields including priority. |
| Ensure CanvasGroup | Reuse, or transactional add+set: `m_Alpha` [0,1], `m_Interactable`, `m_BlocksRaycasts`, `m_IgnoreParentGroups`. |
| Ensure mask | Validate strict `Mask` or `RectMask2D`; reuse matching component. For Mask, transactionally ensure Image if absent, add Mask if absent, and set `m_ShowMaskGraphic`. RectMask2D has no showMaskGraphic effect. |
| Add effect | Validate strict `Shadow` or `Outline`; always add a new exact component, then transactional set `m_EffectColor` RGBA (default 0,0,0,.5), `m_EffectDistance` Vector2 (default 1,-1), `m_UseGraphicAlpha=true`. Repeated calls stack components. |
| Configure Selectable | One atomic native patch: `m_Transition`, `m_Interactable`, `m_Navigation.m_Mode`; individual `m_Colors.m_NormalColor.r/.g/.b`, corresponding Highlighted/Pressed/DisabledColor channels, `m_Colors.m_ColorMultiplier`, `m_Colors.m_FadeDuration`. Omitted channels and alpha stay unchanged; G-only/B-only writes are valid. Validate named transition/navigation values, finite channels [0,1], multiplier [1,5], nonnegative duration. |

Image Type names: Simple/Sliced/Tiled/Filled. FillMethod names: Horizontal/Vertical/Radial90/Radial180/Radial360. FillOrigin is 0–1 for linear, 0–3 for radial methods. Selectable Transition names: None/ColorTint/SpriteSwap/Animation. Navigation names: None/Horizontal/Vertical/Automatic/Explicit. Resolve requested enums against the live component/catalog before writing.

## Anchor presets

Normalize case and remove spaces, then resolve one row before dispatch; unknown presets refuse. Apply `m_AnchorMin` and `m_AnchorMax` in one native properties call. Add `m_Pivot` only when requested; omitted pivot stays unchanged.

| Preset | anchorMin | anchorMax | optional pivot |
| --- | --- | --- | --- |
| TopLeft | 0,1 | 0,1 | 0,1 |
| TopCenter | .5,1 | .5,1 | .5,1 |
| TopRight | 1,1 | 1,1 | 1,1 |
| MiddleLeft | 0,.5 | 0,.5 | 0,.5 |
| MiddleCenter | .5,.5 | .5,.5 | .5,.5 |
| MiddleRight | 1,.5 | 1,.5 | 1,.5 |
| BottomLeft | 0,0 | 0,0 | 0,0 |
| BottomCenter | .5,0 | .5,0 | .5,0 |
| BottomRight | 1,0 | 1,0 | 1,0 |
| StretchHorizontal | 0,.5 | 1,.5 | .5,.5 |
| StretchVertical | .5,0 | .5,1 | .5,.5 |
| StretchAll | 0,0 | 1,1 | .5,.5 |

## Mobile UI design baselines

Choose the CanvasScaler reference resolution and scaling mode for the intended device and orientation through its actual component owner. The following values are project design baselines in Canvas reference units. They require readability and touch verification on actual devices; web CSS pixels and hardware pixels do not establish the resulting physical size. Adapt portrait proportions for landscape or tablet layouts against the intended design spec.

| Touch target | Baseline size |
| --- | --- |
| Primary action | Minimum height 60 |
| Secondary action | Minimum height 48 |
| Icon button | 44×44 |
| Toggle/checkbox background | 28×28; verify the complete interactive target separately |

| Portrait HUD bar | Width relative to Canvas | Height |
| --- | --- | --- |
| XP/level | 80–85% | 48–56 |
| HP | 65–70% | 24–32 |
| Thin progress strip | 100% | 8–12 |

Use a 44-unit minimum bar height when text is inside, allowing room for the label and padding. Check the intended spec against this floor before authoring; resolve undersized text-bearing bars in the design, then verify readability.

| Typography role | Font size baseline |
| --- | --- |
| Screen title | 34–44 |
| Card/panel title | 18–26 |
| Body/stat label | 13–16 |
| Accent stat value | 18–24 |
| Badge/caption | 10–13 |

Set TMP properties through `component.member-set` on the exact TMP text component: set public `enableAutoSizing=true` before `fontSizeMin` and `fontSizeMax`. Read back those bounds and the actual resulting `fontSize`, then inspect representative labels at target dimensions. Text shrinking to the minimum calls for a layout/readability check. Use the current `Screen.safeArea` to derive edge insets in the chosen Canvas coordinate system and verify orientation/device changes.

Compare screenshot and layout measurements against the intended design spec and these numeric baselines. Build the acceptance checklist from those requirements before inspecting the image; a checklist derived solely from the observed screenshot cannot detect divergence from the design.

## Author, inspect and exercise

Create Canvas/panel/widgets, arrange children, then apply native anchors/partial rect patches. Use Pipeline batch composition for repeated creation and the foundation typed-result contract; no separate menu-batch constructor is needed. Configure CanvasScaler through the component owner for the intended resolution and mobile scale. Inspect font size, wrapping and TMP autosizing through actual component properties; test labels against the target screen dimensions.

Wait for compile through [editor](editor.md), inspect exact hierarchy/state, frame the intended UI and capture through [lighting and cameras](lighting-camera.md) or [scene](scene.md). Require a readable image of legacy and TMP labels, input placeholder/text, dropdown caption/list and other controls. Confirm EventSystem and the project's chosen input module/actions before normal Play interaction. Exercise click, text input, dropdown selection and scroll through public input/event paths; do not invoke private callbacks. Save/reopen through the [scene owner](scene.md), then verify references and rendering again.

## Isolated panel captures

Use the existing project's UGUI choice for this workflow; capture does not install packages.
The skill's [bounded host capture owner](../../scripts/unity_cli/capture.py) runs one native eval
per panel. Default Canvas/root names are `Canvas`/`UIRoot`; names must resolve uniquely among loaded
regular scene objects. Supply `--canvas-id`, `--root-id`, or `--panel-id` when exact identities are
needed. A root is a direct Canvas child and a panel is a direct root child. Inactive panels and
ancestors are temporarily enabled; sibling visibility is isolated and restored.

```bash
python3 <skill-root>/scripts/unity_workflow.py ui-capture --project <exact-project> \
  --panel MainMenu --filename ui_main_menu
python3 <skill-root>/scripts/unity_workflow.py ui-capture-batch --project <exact-project> \
  --items '[{"panel":"PanelA","filename":"ui_panel_a"},{"panel":"PanelB","filename":"ui_panel_b"}]'
```

Each panel defaults to 390×844 and `Assets/Screenshots/UI/<filename>.png`; an omitted filename
uses the panel name. Override `--width`, `--height`, or `--save-path` for a single panel, or
`width`, `height`, `path` per batch item. Names and filenames must be unique in a batch. Existing
outputs require explicit current-file hash authorization: `--replace-sha256`, or
`replace_sha256` per item. One exact Editor lease and timeout cover a batch. Readiness
and restoration are checked independently for each panel. Batches run sequentially
and retain one ordered outcome per requested
panel, including native capture and publication/import failures; any failure produces nonzero exit.

The eval snapshots Canvas mode/camera/plane, complete scaler state, descendant RectTransforms,
and every altered ancestor/sibling activeSelf before mutation. It renders through the same
[portable camera body](../../scripts/camera_capture.cs), restores all temporary UI state in
`finally`, destroys its camera, and returns inline PNG only after restoration. It checks the
scene's original dirty state without marking a scene clean. Edit mode readiness and compiler
errors refuse before capture. Canvas layout callbacks or external component side effects can
require project-specific verification of the full before/after scene state.

Publication uses the existing authoring transaction and [asset metadata lifecycle](asset.md#external-import).
A successful capture proves actual PNG bytes, synchronous import, exact file hash, metadata,
GUID/path/main type; `width`/`height` describe generated pixels and `importedWidth`/`importedHeight`
describe the loaded Texture2D. Importer scaling is reported separately. Inspect the real image
for project-specific readability, clipping and rendering expectations. A recovered failed item
remains a failure; incomplete recovery preserves snapshots and intervening external changes.
