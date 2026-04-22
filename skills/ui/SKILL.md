---
name: unity-ui
description: "Use when users want to create Canvas, Button, Text, Image, or other UI elements."
---

# Unity UI Skills

## When to Use

Use this module for Unity UGUI / Canvas workflows. It is separate from UI Toolkit.

**Routing**:
- Check `../../mcp-tools.md` first for a dedicated official Unity MCP tool.
- If no native tool covers the operation, use `../../recipes/ui/<command>.md`.
- For UXML/USS/UIDocument → use `uitoolkit`.
- For XR-compatible world-space Canvas conversion → use `xr_setup_ui_canvas`.
- For text updates after creation → use `ui_set_text`.
- For layout and alignment → use `ui_layout_children`, `ui_align_selected`, `ui_distribute_selected`.

> **Creating 2+ elements**: call the individual `ui_create_<primitive>` recipes in a single `Unity_RunCommand` — one `CommandScript.Execute` block can instantiate any number of UI elements by chaining the per-primitive setup inline.

## Common Mistakes


**DO NOT** (common hallucinations):
- `ui_add_canvas` does not exist -> use `ui_create_canvas`
- `ui_create_label` does not exist -> use `ui_create_text`
- `ui_create_checkbox` does not exist -> use `ui_create_toggle`
- `ui_set_color` does not exist -> use `component_set_property` on `Image`/`Text`, or the dedicated UI property skills when available
- Do not confuse UGUI (`ui`) with UI Toolkit (`uitoolkit`)

## Quick Reference

### Create Skills

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `ui_create_canvas` | Create Canvas | `name?`, `renderMode?` |
| `ui_create_panel` | Create panel container | `name?`, `parent?`, `r/g/b/a?` |
| `ui_create_button` | Create button | `name?`, `parent?`, `text?`, `width/height?` |
| `ui_create_text` | Create text label | `name?`, `parent?`, `text?`, `fontSize?`, `r/g/b?` |
| `ui_create_image` | Create image | `name?`, `parent?`, `spritePath?`, `width/height?` |
| `ui_create_inputfield` | Create input field | `name?`, `parent?`, `placeholder?`, `width/height?` |
| `ui_create_slider` | Create slider | `name?`, `parent?`, `minValue?`, `maxValue?`, `value?` |
| `ui_create_toggle` | Create toggle | `name?`, `parent?`, `label?`, `isOn?` |
| `ui_create_dropdown` | Create dropdown | `name?`, `parent?`, `options?`, `width/height?` |
| `ui_create_scrollview` | Create ScrollRect hierarchy | `name?`, `parent?`, `width/height?`, `horizontal?`, `vertical?` |
| `ui_create_rawimage` | Create RawImage | `name?`, `parent?`, `texturePath?`, `width/height?` |
| `ui_create_scrollbar` | Create scrollbar | `name?`, `parent?`, `direction?`, `value?`, `size?` |

### Query and Layout Skills

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `ui_find_all` | Find scene UI elements | `uiType?`, `limit?` |
| `ui_set_text` | Update text content | `name`, `text` |
| `ui_set_rect` | Set RectTransform size/offsets | target, `width`, `height`, `posX`, `posY`, `left/right/top/bottom?` |
| `ui_set_anchor` | Apply anchor preset | target, `preset?`, `setPivot?` |
| `ui_layout_children` | Vertical/Horizontal/Grid layout | target, `layoutType?`, `spacing?` |
| `ui_align_selected` | Align current selection | `alignment?` |
| `ui_distribute_selected` | Distribute current selection | `direction?` |

### Property and Effect Skills

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `ui_set_image` | Image type/fill/sprite | target, `type?` (`Simple`/`Sliced`/`Tiled`/`Filled`), `fillMethod?`, `fillAmount?`, `spritePath?`, `preserveAspect?` |
| `ui_add_layout_element` | Add LayoutElement constraints | target, `minWidth/Height?`, `preferredWidth/Height?`, `flexibleWidth/Height?`, `ignoreLayout?`, `layoutPriority?` |
| `ui_add_canvas_group` | Add CanvasGroup | target, `alpha?`, `interactable?`, `blocksRaycasts?`, `ignoreParentGroups?` |
| `ui_add_mask` | Add `Mask` or `RectMask2D` | target, `maskType?`, `showMaskGraphic?` |
| `ui_add_outline` | Add Shadow/Outline effect | target, `effectType?` (`Shadow`/`Outline`), `r/g/b/a?`, `distanceX/Y?`, `useGraphicAlpha?` |
| `ui_configure_selectable` | Configure transitions/navigation/colors | target, `transition?`, `navigationMode?`, `interactable?`, `normalR/G/B?`, `highlightedR/G/B?`, `pressedR/G/B?`, `disabledR/G/B?`, `colorMultiplier?`, `fadeDuration?` |

## High-Frequency Defaults

### Canvas and Parenting

- `ui_create_canvas` defaults to `ScreenSpaceOverlay`.
- Most create skills accept `parent`; if omitted, Unity will create under the active Canvas or scene root depending on the implementation context.
- For reusable menu groups, create the Canvas once, then create a Panel and put all child controls under that panel.

### Common Create Parameters

| Skill | High-frequency fields |
|-------|-----------------------|
| `ui_create_button` | `text`, `width`, `height` |
| `ui_create_text` | `text`, `fontSize`, `r/g/b` |
| `ui_create_image` | `spritePath`, `width`, `height` |
| `ui_create_slider` | `minValue`, `maxValue`, `value` |
| `ui_create_toggle` | `label`, `isOn` |
| `ui_create_dropdown` | `options` |
| `ui_create_scrollview` | `horizontal`, `vertical`, `movementType` |

Important:
- Most create skills do **not** take explicit `x/y` placement.
- Create first, then place/anchor with `ui_set_rect`, `ui_set_anchor`, or `ui_layout_children`.

### Layout and Anchoring Rules

- `ui_set_anchor` is the fastest way to move a control into a standard layout position.
- `ui_set_rect` is better for precise size/offset edits after anchoring.
- `ui_layout_children` is preferred over hand-positioning every child when building vertical, horizontal, or grid menus.

Anchor presets commonly used in production:
- `MiddleCenter` for modal/menu panels
- `TopLeft` or `TopRight` for HUD corners
- `StretchAll` for full-screen backgrounds

### TextMeshPro Note

Text creation auto-detects TMP:
- TMP available -> `TextMeshProUGUI`
- TMP unavailable -> legacy `Text`

Read the response payload if you need to know which one was created before later component-specific edits.

## Workflow Notes

1. Create a Canvas first.
2. Use panels to group related controls.
3. Prefer `ui_create_batch` for menus, HUD groups, and repeated widgets.
4. Use anchors and layout groups before hand-placing every child.
5. Text creation auto-detects TextMeshPro. Responses indicate whether TMP was used.
6. For world-space gameplay UI, build the Canvas here first, then convert for XR only if needed.
7. `ui_create_batch` is mainly for bulk creation, not precise positioning. Follow it with layout or rect/anchor adjustments.
8. `ui_create_batch.items` is a JSON string parameter, not a raw array object.

## Typical Menu Workflow

```
ui_create_canvas  name="MainMenu" renderMode="ScreenSpaceOverlay"
ui_create_panel   name="MenuPanel" parent="MainMenu" a=0.65
ui_set_rect       name="MenuPanel" width=300 height=200
ui_create_batch   items=[
  {type:"Button", name:"StartBtn",   parent:"MenuPanel", text:"Start",   width:220, height:44},
  {type:"Button", name:"OptionsBtn", parent:"MenuPanel", text:"Options", width:220, height:44},
  {type:"Button", name:"QuitBtn",    parent:"MenuPanel", text:"Quit",    width:220, height:44}
]
ui_layout_children name="MenuPanel" layoutType="Vertical" spacing=12
```

## RunCommand Examples

Recipe path rule: `../../recipes/ui/<command>.md`

*See `../../recipes/ui/<command>.md` for C# templates.*

