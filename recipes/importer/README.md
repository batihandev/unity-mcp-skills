# Importer Recipes

Per-command `Unity_RunCommand` C# templates for the `unity-importer` skill.

Each file is named after the exact command ID.

## Reimport

| Command | File | Description |
|---------|------|-------------|
| `asset_reimport` | [asset_reimport.md](asset_reimport.md) | Force reimport a single asset |
| `asset_reimport_batch` | [asset_reimport_batch.md](asset_reimport_batch.md) | Reimport multiple assets by filter |

## Texture

### Settings

| Command | File | Description |
|---------|------|-------------|
| `texture_get_settings` | [texture_get_settings.md](texture_get_settings.md) | Read full importer settings |
| `texture_set_settings` | [texture_set_settings.md](texture_set_settings.md) | Set importer settings and reimport |
| `texture_set_settings_batch` | [texture_set_settings_batch.md](texture_set_settings_batch.md) | Batch set settings on multiple textures |
| `texture_get_import_settings` | [texture_get_import_settings.md](texture_get_import_settings.md) | Lightweight bridge getter |
| `texture_set_import_settings` | [texture_set_import_settings.md](texture_set_import_settings.md) | Lightweight bridge setter |

### Query and Info

| Command | File | Description |
|---------|------|-------------|
| `texture_find_assets` | [texture_find_assets.md](texture_find_assets.md) | Search Texture2D assets |
| `texture_get_info` | [texture_get_info.md](texture_get_info.md) | Runtime dimensions, format, memory size |
| `texture_find_by_size` | [texture_find_by_size.md](texture_find_by_size.md) | Filter textures by pixel dimensions |

### Typed and Platform

| Command | File | Description |
|---------|------|-------------|
| `texture_set_type` | [texture_set_type.md](texture_set_type.md) | Switch texture type |
| `texture_set_platform_settings` | [texture_set_platform_settings.md](texture_set_platform_settings.md) | Per-platform overrides |
| `texture_get_platform_settings` | [texture_get_platform_settings.md](texture_get_platform_settings.md) | Read per-platform override |
| `texture_set_sprite_settings` | [texture_set_sprite_settings.md](texture_set_sprite_settings.md) | Sprite PPU and mode |
| `sprite_set_import_settings` | [sprite_set_import_settings.md](sprite_set_import_settings.md) | Sprite bridge: PPU, packing tag, pivot |

## Audio

### Import Settings

| Command | File | Description |
|---------|------|-------------|
| `audio_get_settings` | [audio_get_settings.md](audio_get_settings.md) | Read full importer settings |
| `audio_set_settings` | [audio_set_settings.md](audio_set_settings.md) | Set importer settings and reimport |
| `audio_set_settings_batch` | [audio_set_settings_batch.md](audio_set_settings_batch.md) | Batch set settings on multiple clips |
| `audio_get_import_settings` | [audio_get_import_settings.md](audio_get_import_settings.md) | Lightweight bridge getter |
| `audio_set_import_settings` | [audio_set_import_settings.md](audio_set_import_settings.md) | Lightweight bridge setter |

### Clip Query and Info

| Command | File | Description |
|---------|------|-------------|
| `audio_find_clips` | [audio_find_clips.md](audio_find_clips.md) | Search AudioClip assets |
| `audio_get_clip_info` | [audio_get_clip_info.md](audio_get_clip_info.md) | Length, channels, frequency, samples |

### Scene Runtime

| Command | File | Description |
|---------|------|-------------|
| `audio_add_source` | [audio_add_source.md](audio_add_source.md) | Add AudioSource to a GameObject |
| `audio_get_source_info` | [audio_get_source_info.md](audio_get_source_info.md) | Read AudioSource configuration |
| `audio_set_source_properties` | [audio_set_source_properties.md](audio_set_source_properties.md) | Update AudioSource fields |
| `audio_find_sources_in_scene` | [audio_find_sources_in_scene.md](audio_find_sources_in_scene.md) | List all AudioSources in the scene |
| `audio_create_mixer` | [audio_create_mixer.md](audio_create_mixer.md) | Create a new AudioMixer asset |

## Model

### Import Settings

| Command | File | Description |
|---------|------|-------------|
| `model_get_settings` | [model_get_settings.md](model_get_settings.md) | Read full importer settings |
| `model_set_settings` | [model_set_settings.md](model_set_settings.md) | Set importer settings and reimport |
| `model_set_settings_batch` | [model_set_settings_batch.md](model_set_settings_batch.md) | Batch set settings on multiple models |
| `model_get_import_settings` | [model_get_import_settings.md](model_get_import_settings.md) | Lightweight bridge getter |
| `model_set_import_settings` | [model_set_import_settings.md](model_set_import_settings.md) | Lightweight bridge setter |

### Query and Info

| Command | File | Description |
|---------|------|-------------|
| `model_find_assets` | [model_find_assets.md](model_find_assets.md) | Search model assets |
| `model_get_mesh_info` | [model_get_mesh_info.md](model_get_mesh_info.md) | Vertex, triangle, submesh stats |
| `model_get_materials_info` | [model_get_materials_info.md](model_get_materials_info.md) | Embedded materials and meshes |
| `model_get_animations_info` | [model_get_animations_info.md](model_get_animations_info.md) | Animation clips and frame rates |
| `model_get_rig_info` | [model_get_rig_info.md](model_get_rig_info.md) | Rig, avatar, skeleton info |

### Animation and Rig

| Command | File | Description |
|---------|------|-------------|
| `model_set_animation_clips` | [model_set_animation_clips.md](model_set_animation_clips.md) | Configure animation clip splits |
| `model_set_rig` | [model_set_rig.md](model_set_rig.md) | Switch rig/skeleton mode |
