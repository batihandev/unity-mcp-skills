# Shared Recipe Helpers

These files contain utility recipes and C# helper classes used across multiple domain skills. They live here rather than in a topic folder because they are cross-cutting concerns.

## Files

- **validate.md** — Validation helper recipes extracted from `ValidationSkills.cs`. Covers scene validation, missing script detection, unused asset finding, texture size checks, project structure inspection, and shader error reporting.

- **skills_common.md** — Common skill utility recipes providing the `SkillsCommon` static class. Includes UTF-8 encoding helpers, assembly type enumeration, and allocation-free triangle counting for meshes.

- **gameobject_finder.md** — Helper recipes for finding GameObjects in the scene hierarchy. Provides `FindHelper` (Unity 6+ compatible `FindObjectsByType` wrapper) and `GameObjectFinder` (cached scene traversal supporting lookup by name, instance ID, hierarchy path, and tag).
