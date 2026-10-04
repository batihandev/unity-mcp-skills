from __future__ import annotations

import json
import math
import pathlib
from typing import Any

from .compile import CompileWorkflow, WorkflowRefusal
from .lifecycle import SessionRefusal


class ImportWorkflowRefusal(WorkflowRefusal):
    """A refusal raised before or during a guarded importer workflow."""


class ImportWorkflow:
    KINDS = {"audio": "AudioImporter", "texture": "TextureImporter", "model": "ModelImporter"}
    PLATFORM_ALIASES = {"iPhone": "iOS"}
    ENUM_FIELDS = {
        "audio": {"loadType", "compressionFormat", "sampleRateSetting"},
        "texture": {"textureType", "textureShape", "alphaSource", "filterMode", "wrapMode", "textureCompression", "spriteImportMode", "npotScale", "format", "resizeAlgorithm", "androidETC2FallbackOverride"},
        "model": {"meshCompression", "importNormals", "importTangents", "materialImportMode", "materialLocation", "animationType", "avatarSetup", "indexFormat", "normalCalculationMode", "meshOptimizationFlags"},
    }
    INTEGER_FIELDS = {
        "audio": {"sampleRateOverride"},
        "texture": {"maxTextureSize", "anisoLevel", "compressionQuality"},
        "model": set(),
    }
    NUMBER_FIELDS = {
        "audio": {"quality", "sampleRateOverride"},
        "texture": {"spritePixelsPerUnit", "maxTextureSize", "anisoLevel", "compressionQuality"},
        "model": {"globalScale", "normalSmoothingAngle"},
    }
    BOOL_FIELDS = {
        "audio": {"forceToMono", "loadInBackground", "ambisonic", "overridden"},
        "texture": {"sRGBTexture", "alphaIsTransparency", "isReadable", "mipmapEnabled", "streamingMipmaps", "crunchedCompression", "overridden"},
        "model": {"useFileScale", "importBlendShapes", "importVisibility", "importCameras", "importLights", "isReadable", "optimizeMeshPolygons", "optimizeMeshVertices", "generateSecondaryUV", "keepQuads", "weldVertices", "importAnimation", "importConstraints", "swapUVChannels", "overridden"},
    }
    POSITIVE_FIELDS = {"audio": set(), "texture": {"maxTextureSize", "spritePixelsPerUnit", "anisoLevel"}, "model": {"globalScale"}}
    ENUM_TYPES = {
        "loadType": "UnityEngine.AudioClipLoadType", "compressionFormat": "UnityEngine.AudioCompressionFormat", "sampleRateSetting": "UnityEditor.AudioSampleRateSetting",
        "textureType": "UnityEditor.TextureImporterType", "textureShape": "UnityEditor.TextureImporterShape", "alphaSource": "UnityEditor.TextureImporterAlphaSource",
        "filterMode": "UnityEngine.FilterMode", "wrapMode": "UnityEngine.TextureWrapMode", "textureCompression": "UnityEditor.TextureImporterCompression",
        "spriteImportMode": "UnityEditor.SpriteImportMode", "npotScale": "UnityEditor.TextureImporterNPOTScale", "format": "UnityEditor.TextureImporterFormat",
        "resizeAlgorithm": "UnityEditor.TextureResizeAlgorithm", "androidETC2FallbackOverride": "UnityEditor.AndroidETC2FallbackOverride",
        "meshCompression": "UnityEditor.ModelImporterMeshCompression", "importNormals": "UnityEditor.ModelImporterNormals",
        "importTangents": "UnityEditor.ModelImporterTangents", "materialImportMode": "UnityEditor.ModelImporterMaterialImportMode",
        "materialLocation": "UnityEditor.ModelImporterMaterialLocation", "animationType": "UnityEditor.ModelImporterAnimationType",
        "avatarSetup": "UnityEditor.ModelImporterAvatarSetup", "indexFormat": "UnityEditor.ModelImporterIndexFormat", "normalCalculationMode": "UnityEditor.ModelImporterNormalCalculationMode",
        "meshOptimizationFlags": "UnityEditor.MeshOptimizationFlags",
    }
    TEXTURE_PUBLIC_COMPRESSION_FIELDS = {"textureCompression", "crunchedCompression", "compressionQuality"}
    SPRITE_FIELDS = {"spriteImportMode", "spritePixelsPerUnit", "spritePivot"}
    CLIP_FIELDS = {"clipAnimations"}
    AUDIO_SAMPLE_FIELDS = {"loadType", "compressionFormat", "quality", "sampleRateSetting", "sampleRateOverride"}
    MINIMAL_FIELDS = {
        "audio": {"forceToMono", "loadInBackground", "loadType", "compressionFormat", "quality"},
        "texture": {"textureType", "maxTextureSize", "filterMode", "wrapMode", "mipmapEnabled", "isReadable", "sRGBTexture", "alphaSource", "alphaIsTransparency", "textureCompression", "npotScale", "spriteImportMode", "spritePixelsPerUnit"},
        "model": {"globalScale", "meshCompression", "animationType", "materialImportMode", "importAnimation"},
    }

    def __init__(self, project: str | pathlib.Path, session):
        self.project = pathlib.Path(project).resolve()
        self.session = session

    def run(self, *, asset: str, kind: str, settings: dict[str, Any], platform: str = "Default",
            dry_run: bool = False, quality_unit: str = "normalized", profile: str = "rich") -> dict[str, Any]:
        request = self._request(asset=asset, kind=kind, settings=settings, platform=platform,
                                dry_run=dry_run, quality_unit=quality_unit, profile=profile)
        try:
            with self.session.workflow_session() as lease:
                return self.run_with_lease(lease, **request)
        except ImportWorkflowRefusal:
            raise
        except SessionRefusal as exc:
            raise ImportWorkflowRefusal(exc.code, str(exc), exc.details) from exc

    def _request(self, *, asset, kind, settings, platform, dry_run, quality_unit, profile):
        if not isinstance(dry_run, bool):
            raise ImportWorkflowRefusal("INVALID_ARGUMENT", "dry_run must be Boolean")
        if not isinstance(kind, str) or kind not in self.KINDS:
            raise ImportWorkflowRefusal("INVALID_ARGUMENT", "kind must be audio, texture, or model")
        if profile not in {"minimal", "rich"}:
            raise ImportWorkflowRefusal("INVALID_ARGUMENT", "profile must be minimal or rich")
        asset = self._asset(asset)
        platform = self._platform(platform)
        settings = self._settings(settings)
        return dict(asset=asset, kind=kind, settings=settings, platform=platform,
                    dry_run=dry_run, quality_unit=quality_unit, profile=profile)

    def restore(self, *, capture: dict[str, Any], dry_run: bool = False) -> dict[str, Any]:
        if not isinstance(capture, dict) or not isinstance(capture.get("assetPath"), str) or not isinstance(capture.get("kind"), str) or not isinstance(capture.get("settings"), dict):
            raise ImportWorkflowRefusal("INVALID_ARGUMENT", "capture must contain assetPath, kind, and settings")
        kind = capture["kind"]
        if kind not in self.KINDS:
            raise ImportWorkflowRefusal("INVALID_ARGUMENT", "capture kind must be audio, texture, or model")
        asset = self._asset(capture["assetPath"])
        platform = self._platform(capture.get("platform", "Default"))
        settings = {key: value for key, value in capture["settings"].items() if key in self._allowed(kind)}
        supplements = capture.get("public", {})
        if kind == "texture" and platform == "Default" and isinstance(supplements, dict):
            for field in self.TEXTURE_PUBLIC_COMPRESSION_FIELDS:
                if field in supplements:
                    settings[field] = supplements[field]
        if kind == "texture" and isinstance(supplements, dict) and supplements.get("textureType") == "Sprite":
            for field in ("spritePivot",):
                if field in supplements:
                    settings[field] = supplements[field]
        if kind == "model" and isinstance(supplements, dict):
            for field in ("avatarSetup", "clipAnimations"):
                if field in supplements:
                    settings[field] = supplements[field]
        audio_extras = None
        if kind == "audio":
            audio_extras = self._audio_extras(supplements)
            if platform != "Default" and settings.get("overridden") is False:
                settings = {"overridden": False}
        if not isinstance(dry_run, bool):
            raise ImportWorkflowRefusal("INVALID_ARGUMENT", "dry_run must be Boolean")
        try:
            with self.session.workflow_session() as lease:
                current = self._read(lease, asset, platform, kind)
                metadata = self._metadata(lease, current["assetPath"], kind, platform)
                settings = self._preflight(kind, settings, current, metadata, quality_unit="normalized", profile="rich", restoring=True)
                return self._apply(lease, asset, platform, kind, settings, current, metadata,
                                   dry_run=dry_run, action="restore", audio_extras=audio_extras)
        except ImportWorkflowRefusal:
            raise
        except SessionRefusal as exc:
            raise ImportWorkflowRefusal(exc.code, str(exc), exc.details) from exc

    def run_batch(self, *, items: list[dict[str, Any]], platform: str = "Default", quality_unit: str = "normalized", profile: str = "rich") -> dict[str, Any]:
        if not isinstance(items, list) or not items:
            raise ImportWorkflowRefusal("INVALID_ARGUMENT", "items must be a nonempty ordered array")
        outcomes, prepared = [], []
        for index, item in enumerate(items):
            try:
                if not isinstance(item, dict):
                    raise ImportWorkflowRefusal("INVALID_ARGUMENT", "item must be an object")
                request = self._request(asset=item.get("asset"), kind=item.get("kind"), settings=item.get("settings"),
                    platform=item.get("platform", platform), dry_run=False,
                    quality_unit=item.get("quality_unit", quality_unit), profile=item.get("profile", profile))
                prepared.append((index, request))
            except ImportWorkflowRefusal as exc:
                outcomes.append({"index": index, "ok": False, "error": exc.as_dict()["error"]})
        completed = set()
        if prepared:
            try:
                with self.session.workflow_session() as lease:
                    for index, request in prepared:
                        try:
                            result = self.run_with_lease(lease, **request)
                            outcomes.append({"index": index, "ok": True, "result": result})
                        except (ImportWorkflowRefusal, SessionRefusal) as exc:
                            outcomes.append({"index": index, "ok": False, "error": exc.as_dict()["error"]})
                        completed.add(index)
            except SessionRefusal as exc:
                for index, _ in prepared:
                    if index not in completed:
                        outcomes.append({"index": index, "ok": False, "error": exc.as_dict()["error"]})
        outcomes.sort(key=lambda row: row["index"])
        return {"ok": all(row["ok"] for row in outcomes), "action": "importer-batch", "transactional": False, "outcomes": outcomes}

    def run_with_lease(self, lease, *, asset: str, kind: str, settings: dict[str, Any], platform: str,
                       dry_run: bool, quality_unit: str, profile: str) -> dict[str, Any]:
        before = self._read(lease, asset, platform, kind)
        metadata = self._metadata(lease, before["assetPath"], kind, platform)
        prepared = self._preflight(kind, settings, before, metadata, quality_unit=quality_unit, profile=profile)
        return self._apply(lease, asset, platform, kind, prepared, before, metadata, dry_run=dry_run, action="apply")

    def _apply(self, lease, asset, platform, kind, settings, before, metadata, *, dry_run, action, audio_extras=None):
        if kind == "audio":
            if audio_extras is None:
                audio_extras = self._audio_extras(metadata["properties"])
            if platform != "Default" and settings.get("overridden") is False and len(settings) != 1:
                self._invalid("overridden", False, "clearing an audio override must be requested alone")
        native_settings = self._native_settings(kind, settings, before["effectiveSettings"], platform, audio_extras)
        dry = self._invoke(lease, "set_import_settings", {"asset": asset, "platform": platform,
                             "settings": json.dumps(native_settings, separators=(",", ":")), "dry_run": True})
        dry_result = self._outer_result(dry, "set_import_settings")
        self._verify_applied(dry_result, native_settings)
        if dry_run:
            return {"ok": True, "action": action, "dryRun": True, "assetPath": before["assetPath"],
                    "kind": kind, "platform": platform, "capture": self._capture(asset, kind, platform, before, metadata),
                    "preflight": {"host": "passed", "nativeDryRun": dry_result}, "write": None,
                    "readback": before, "restoration": "explicit"}

        capture = self._capture(asset, kind, platform, before, metadata)
        try:
            # A write response is never retried: a transport failure can arrive after Unity applied it.
            write = self._invoke(lease, "set_import_settings", {"asset": asset, "platform": platform,
                                   "settings": json.dumps(native_settings, separators=(",", ":")), "dry_run": False})
            write_result = self._outer_result(write, "set_import_settings")
            self._verify_applied(write_result, native_settings)
            after = self._read(lease, asset, platform, kind)
            after_meta = self._metadata(lease, after["assetPath"], kind, platform)
            self._verify_readback(kind, settings, after, after_meta, platform=platform)
            if kind == "audio" and (platform == "Default" or settings.get("overridden") is not False):
                if self._audio_extras(after_meta["properties"]) != audio_extras:
                    raise ImportWorkflowRefusal("IMPORT_READBACK_MISMATCH", "Audio sample settings changed outside the requested fields", {"requestedExtras": audio_extras, "actualExtras": self._audio_extras(after_meta["properties"])})
            return {"ok": True, "action": action, "dryRun": False, "assetPath": after["assetPath"], "before": before,
                    "kind": kind, "platform": platform, "capture": capture,
                    "preflight": {"host": "passed", "nativeDryRun": dry_result},
                    "write": write_result, "readback": {**after, "public": after_meta}, "restoration": "explicit"}
        except (ImportWorkflowRefusal, SessionRefusal) as exc:
            details = {**(exc.details or {}), "capture": capture, "writeMayHaveApplied": True}
            raise ImportWorkflowRefusal(exc.code, str(exc), details) from exc

    def _read(self, lease, asset, platform, kind):
        result = self._invoke(lease, "get_import_settings", {"asset": asset, "platform": platform})
        data = self._outer_result(result, "get_import_settings")
        if not isinstance(data, dict) or data.get("importerType") != self.KINDS[kind] or not isinstance(data.get("settings"), dict):
            raise ImportWorkflowRefusal("IMPORTER_KIND_MISMATCH", "The exact asset does not have the requested importer kind", {"expected": self.KINDS[kind], "actual": data.get("importerType") if isinstance(data, dict) else None})
        path = data.get("assetPath")
        if not isinstance(path, str) or not path:
            raise ImportWorkflowRefusal("IMPORTER_READ_INVALID", "Native settings omitted the resolved assetPath")
        platform_block = data.get("platformOverride")
        if platform == "Default":
            data["effectiveSettings"] = dict(data["settings"])
        elif isinstance(platform_block, dict):
            data["effectiveSettings"] = dict(platform_block)
        else:
            raise ImportWorkflowRefusal("IMPORTER_READ_INVALID", "Native settings omitted the selected platform block", {"platform": platform})
        return data

    def _metadata(self, lease, asset_path, kind, platform="Default"):
        p = json.dumps(asset_path)
        type_names = self.ENUM_TYPES
        enum_items = ",".join(f"[\"{key}\"]=names(\"{value}\")" for key, value in type_names.items())
        lines = ["var p=" + p + ";", "var importer=UnityEditor.AssetImporter.GetAtPath(p);", "if(importer==null)throw new System.ArgumentException(\"Importer missing at exact path\");",
                 "System.Func<string,System.Reflection.FieldInfo[]> fields=n=>{var t=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n,false)).FirstOrDefault(x=>x!=null);return t==null?new System.Reflection.FieldInfo[0]:t.GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);};",
                 "System.Func<System.Reflection.FieldInfo,bool> accepted=f=>!f.GetCustomAttributes(typeof(System.ObsoleteAttribute),false).Cast<System.ObsoleteAttribute>().Any(a=>a.IsError);",
                 "System.Func<string,string[]> names=n=>fields(n).Where(accepted).Select(f=>f.Name).ToArray();",
                 "System.Func<string,System.Collections.Generic.Dictionary<string,string>> canonical=n=>{var all=fields(n);var valid=all.Where(accepted).OrderBy(f=>f.Name,System.StringComparer.Ordinal).ToArray();var map=new System.Collections.Generic.Dictionary<string,string>();foreach(var field in all){var match=valid.FirstOrDefault(f=>System.Object.Equals(f.GetRawConstantValue(),field.GetRawConstantValue()));if(match!=null)map[field.Name]=match.Name;}return map;};",
                 "var enumValues=new System.Collections.Generic.Dictionary<string,string[]>{" + enum_items + "};",
                 "var enumAliases=new System.Collections.Generic.Dictionary<string,System.Collections.Generic.Dictionary<string,string>>{" + ",".join(f"[\"{key}\"]=canonical(\"{value}\")" for key, value in type_names.items()) + "};"]
        if kind == "texture":
            lines.append("var texture=importer as UnityEditor.TextureImporter;if(texture==null)throw new System.ArgumentException(\"TextureImporter required\");return new{assetPath=p,enumValues=enumValues,enumAliases=enumAliases,properties=new{textureType=texture.textureType.ToString(),textureCompression=texture.textureCompression.ToString(),crunchedCompression=texture.crunchedCompression,compressionQuality=texture.compressionQuality,spriteImportMode=texture.spriteImportMode.ToString(),spritePixelsPerUnit=texture.spritePixelsPerUnit,spritePackingTag=texture.spritePackingTag,spritePivot=new{x=texture.spritePivot.x,y=texture.spritePivot.y}}};")
        elif kind == "model":
            lines.append("var model=importer as UnityEditor.ModelImporter;if(model==null)throw new System.ArgumentException(\"ModelImporter required\");var defaults=model.defaultClipAnimations;var clips=model.clipAnimations;var source=model.sourceAvatar;string sourceGuid=null;long sourceLocalId=0;if(source!=null)UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out sourceGuid,out sourceLocalId);var generated=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(p).OfType<UnityEngine.AnimationClip>().Where(c=>!c.name.StartsWith(\"__preview__\",System.StringComparison.Ordinal)).Select(c=>new{name=c.name,length=c.length,frameRate=c.frameRate,looping=c.isLooping}).ToArray();return new{assetPath=p,enumValues=enumValues,enumAliases=enumAliases,properties=new{animationType=model.animationType.ToString(),avatarSetup=model.avatarSetup.ToString(),sourceAvatar=source==null?null:new{assetPath=UnityEditor.AssetDatabase.GetAssetPath(source),guid=sourceGuid,localId=sourceLocalId,entityId=UnityEngine.EntityId.ToULong(source.GetEntityId()).ToString()},hasSourceAvatar=source!=null,defaultClipAnimations=defaults.Select(c=>new{name=c.name,takeName=c.takeName,firstFrame=c.firstFrame,lastFrame=c.lastFrame}).ToArray(),clipAnimations=clips.Select(c=>new{name=c.name,takeName=c.takeName,firstFrame=c.firstFrame,lastFrame=c.lastFrame,loopTime=c.loopTime}).ToArray(),generatedClips=generated}};")
        else:
            lines.append("var audio=importer as UnityEditor.AudioImporter;if(audio==null)throw new System.ArgumentException(\"AudioImporter required\");var sample=" + ("audio.defaultSampleSettings" if platform == "Default" else "audio.GetOverrideSampleSettings(" + json.dumps("iPhone" if platform == "iOS" else platform) + ")") + ";return new{assetPath=p,enumValues=enumValues,enumAliases=enumAliases,properties=new{audioSampleSettingsExtras=new{conversionMode=sample.conversionMode,preloadAudioData=sample.preloadAudioData}}};")
        code = " ".join(lines)
        result = self._eval(lease, code)
        if not isinstance(result, dict) or result.get("assetPath") != asset_path or not isinstance(result.get("enumValues"), dict) or not isinstance(result.get("properties"), dict):
            raise ImportWorkflowRefusal("IMPORT_METADATA_INVALID", "Public importer metadata evaluation returned an unexpected result")
        if kind == "audio":
            self._audio_extras(result["properties"])
        return result

    def _eval(self, lease, code):
        # Pipeline eval takes its C# block as a positional value, unlike named structured commands.
        lease.verify_current()
        transport = self.session.transport
        target, budget = transport._project_budget(str(self.project), lease.remaining(), deadline=lease.transport_deadline)
        result = transport._invoke(["command", "eval", code, "--project-path", target, "--format", "json"], timeout=budget, deadline=lease.transport_deadline)
        if not result.success:
            raise ImportWorkflowRefusal("IMPORT_METADATA_FAILED", "Public importer metadata evaluation failed", {"code": result.code, "message": result.message})
        envelope = result.data
        if not isinstance(envelope, dict) or envelope.get("success") is not True or not isinstance(envelope.get("data"), dict):
            raise ImportWorkflowRefusal("IMPORT_METADATA_INVALID", "Public importer metadata returned a malformed CLI envelope")
        data = envelope["data"]
        value = data.get("result")
        if data.get("command") == "eval":
            value = data.get("result")
        if isinstance(value, dict) and value.get("success") is True and "result" in value:
            value = value["result"]
        elif isinstance(value, dict) and value.get("Ok") is True and "Result" in value:
            value = value["Result"]
        return value

    def _preflight(self, kind, settings, before, metadata, *, quality_unit, profile, restoring=False):
        if profile not in {"minimal", "rich"}:
            raise ImportWorkflowRefusal("IMPORT_PREFLIGHT_INVALID", "profile must be minimal or rich", {"profile": profile})
        values = dict(settings)
        if kind == "texture" and "spritePackingTag" in values:
            self._invalid("spritePackingTag", values["spritePackingTag"], "Unity 6 ignores this property; assign the Sprite to a Sprite Atlas and verify its packable membership")
        if quality_unit not in {"normalized", "percent"}:
            raise ImportWorkflowRefusal("INVALID_ARGUMENT", "quality_unit must be normalized or percent")
        if kind == "model" and values.get("animationType") == "Humanoid":
            values["animationType"] = "Human"
        if kind == "audio" and profile == "minimal" and "quality" in values and quality_unit != "percent":
            self._invalid("quality", values["quality"], "minimal audio profile quality requires integer percent input")
        if kind == "audio" and profile == "rich" and "quality" in values and quality_unit != "normalized":
            self._invalid("quality", values["quality"], "rich audio profile quality uses normalized input")
        if kind == "audio" and "quality" in values and quality_unit == "percent":
            value = values["quality"]
            if not self._integer(value) or not 0 <= value <= 100:
                self._invalid("quality", value, "percentage quality must be an integer from 0 through 100")
            values["quality"] = value / 100.0
        allowed = self._allowed(kind)
        if profile == "minimal":
            allowed = allowed & self.MINIMAL_FIELDS[kind]
        unknown = sorted(set(values) - allowed)
        if unknown:
            self._invalid(unknown[0], values[unknown[0]], "field is outside the documented importer profile")
        for field, value in values.items():
            if field in self.BOOL_FIELDS[kind] and not isinstance(value, bool):
                self._invalid(field, value, "must be a Boolean")
            if field in self.NUMBER_FIELDS[kind] and (isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value)):
                self._invalid(field, value, "must be a finite number")
            if field in self.INTEGER_FIELDS[kind] and not self._integer(value):
                self._invalid(field, value, "must be an integer")
            if field in self.POSITIVE_FIELDS[kind] and value <= 0:
                self._invalid(field, value, "must be greater than zero")
            if field in self.ENUM_FIELDS[kind]:
                enum_values = metadata["enumValues"].get(field)
                if restoring:
                    value = self._canonical_enum(field, value, metadata)
                    values[field] = value
                if not isinstance(value, str) or not isinstance(enum_values, list) or value not in enum_values:
                    self._invalid(field, value, "must match an enum name returned by the current Unity API")
            if field in {"quality"} and not 0 <= value <= 1:
                self._invalid(field, value, "must be from 0 through 1")
            if field == "compressionQuality" and (not self._integer(value) or not 0 <= value <= 100):
                self._invalid(field, value, "must be an integer from 0 through 100")
            if field == "spritePivot":
                if not isinstance(value, dict) or set(value) != {"x", "y"} or any(not self._finite_number(value[k]) or not 0 <= value[k] <= 1 for k in ("x", "y")):
                    self._invalid(field, value, "must contain finite x and y values from 0 through 1")
            if field == "clipAnimations":
                self._clips(value, metadata)
        if kind == "texture" and not restoring and set(values) & self.SPRITE_FIELDS:
            current_type = before.get("settings", {}).get("textureType")
            requested_type = values.get("textureType", current_type)
            if current_type != "Sprite" and requested_type != "Sprite":
                self._invalid("spriteImportMode", values, "sprite fields require a Sprite importer or a same-request Sprite transition")
        if kind == "model" and values.get("avatarSetup") == "CopyFromOther" and not metadata["properties"].get("hasSourceAvatar"):
            self._invalid("avatarSetup", "CopyFromOther", "the exact ModelImporter must already have a sourceAvatar")
        if kind == "model":
            animation = values.get("animationType", metadata["properties"].get("animationType"))
            avatar_setup = values.get("avatarSetup", metadata["properties"].get("avatarSetup"))
            if animation in {"None", "Legacy"} and avatar_setup not in {None, "NoAvatar"}:
                self._invalid("avatarSetup", avatar_setup, "None and Legacy animation types require NoAvatar")
        return values

    def _clips(self, value, metadata):
        if not isinstance(value, list):
            self._invalid("clipAnimations", value, "must be an ordered array")
        source = metadata["properties"].get("defaultClipAnimations", [])
        names = {row.get("takeName") for row in source if isinstance(row, dict) and isinstance(row.get("takeName"), str)}
        ranges = {row.get("takeName"): row for row in source if isinstance(row, dict)}
        clip_names = set()
        for i, row in enumerate(value):
            if not isinstance(row, dict) or set(row) != {"name", "takeName", "firstFrame", "lastFrame", "loopTime"}:
                self._invalid("clipAnimations", row, f"clip {i} must contain exactly name, takeName, firstFrame, lastFrame, and loopTime")
            name, take = row["name"], row["takeName"]
            if not isinstance(name, str) or not name or name in clip_names:
                self._invalid("clipAnimations", name, "clip names must be nonempty and unique")
            clip_names.add(name)
            if not isinstance(take, str) or take not in names:
                self._invalid("clipAnimations", take, "takeName must match an actual source take")
            for frame in (row["firstFrame"], row["lastFrame"]):
                if not self._finite_number(frame):
                    self._invalid("clipAnimations", frame, "frame bounds must be finite numbers")
            source_range = ranges[take]
            if row["firstFrame"] > row["lastFrame"] or row["firstFrame"] < source_range["firstFrame"] or row["lastFrame"] > source_range["lastFrame"]:
                self._invalid("clipAnimations", row, "frame range must be ordered and inside the selected source take")
            if not isinstance(row["loopTime"], bool):
                self._invalid("clipAnimations", row["loopTime"], "loopTime must be Boolean")

    @staticmethod
    def _audio_extras(properties):
        extras = properties.get("audioSampleSettingsExtras") if isinstance(properties, dict) else None
        if (not isinstance(extras, dict) or set(extras) != {"conversionMode", "preloadAudioData"}
                or not ImportWorkflow._integer(extras.get("conversionMode"))
                or not -(2 ** 31) <= extras["conversionMode"] < 2 ** 31
                or not isinstance(extras.get("preloadAudioData"), bool)):
            raise ImportWorkflowRefusal("IMPORT_METADATA_INVALID", "Audio capture requires integer conversionMode and Boolean preloadAudioData for exact sample-settings preservation")
        return dict(extras)

    def _native_settings(self, kind, settings, captured, platform="Default", audio_extras=None):
        result = dict(settings)
        if kind == "audio":
            sample_fields = set(result) & self.AUDIO_SAMPLE_FIELDS
            if platform == "Default" and sample_fields:
                complete = {key: value for key, value in captured.items() if key in self.AUDIO_SAMPLE_FIELDS}
                complete.update(audio_extras)
                complete.update({key: result.pop(key) for key in sample_fields})
                result["defaultSampleSettings"] = complete
            elif platform != "Default" and result.get("overridden") is not False:
                result.update(audio_extras)
        return result

    def _verify_readback(self, kind, requested, after, metadata, platform="Default"):
        state = dict(after.get("effectiveSettings", after["settings"]))
        state.update({field: value for field, value in metadata.get("properties", {}).items()
                      if not (kind == "texture" and platform != "Default"
                              and field in self.TEXTURE_PUBLIC_COMPRESSION_FIELDS)})
        aliases = {"spritePivot": "spritePivot", "avatarSetup": "avatarSetup", "clipAnimations": "clipAnimations"}
        for field, value in requested.items():
            got = state.get(aliases.get(field, field))
            if field == "clipAnimations":
                got = metadata.get("properties", {}).get("clipAnimations")
            equivalent = (self._canonical_enum(field, got, metadata) == self._canonical_enum(field, value, metadata)
                          if field in self.ENUM_FIELDS[kind] else got == value)
            if not equivalent:
                raise ImportWorkflowRefusal("IMPORT_READBACK_MISMATCH", "Importer readback did not match a requested field", {"field": field, "requested": value, "actual": got, "writeMayHaveApplied": True})

    @staticmethod
    def _canonical_enum(field, value, metadata):
        aliases = metadata.get("enumAliases", {}).get(field, {})
        if not isinstance(value, str) or not isinstance(aliases, dict):
            return value
        canonical = aliases.get(value, value)
        return canonical if canonical in metadata["enumValues"].get(field, []) else value

    @staticmethod
    def _capture(asset, kind, platform, data, metadata):
        return {"assetPath": data["assetPath"], "kind": kind, "platform": platform,
                "settings": data.get("effectiveSettings", data["settings"]), "defaultSettings": data["settings"],
                "platformOverride": data.get("platformOverride"), "public": metadata.get("properties", {})}

    @staticmethod
    def _verify_applied(result, settings):
        if not isinstance(result, dict):
            raise ImportWorkflowRefusal("IMPORT_NATIVE_RESULT_INVALID", "Native importer command returned an unexpected result")
        applied = result.get("applied")
        unknown = result.get("unknown", [])
        expected = set(settings)
        if not isinstance(applied, list) or unknown or not expected <= set(applied):
            raise ImportWorkflowRefusal("IMPORT_NATIVE_PREFLIGHT_FAILED", "Native dry run did not accept every requested importer field", {"expected": sorted(expected), "applied": applied, "unknown": unknown})

    def _invoke(self, lease, command, args):
        lease.verify_current()
        result = self.session.transport.invoke_command(str(self.project), command, args, timeout=lease.remaining(), deadline=lease.transport_deadline)
        if not result.success:
            raise ImportWorkflowRefusal("NATIVE_COMMAND_FAILED", "Unity Pipeline importer command failed; inspect capture before deciding whether to restore", {"command": command, "code": result.code, "message": result.message, "invocation": result.diagnostics.get("invocation"), "writeMayHaveApplied": command == "set_import_settings" and args.get("dry_run") is False})
        return result

    @staticmethod
    def _outer_result(result, command):
        try:
            return CompileWorkflow._outer_result(result, command)["result"]
        except WorkflowRefusal as exc:
            raise ImportWorkflowRefusal(exc.code, str(exc), exc.details) from exc

    @staticmethod
    def _allowed(kind):
        return ImportWorkflow.NUMBER_FIELDS[kind] | ImportWorkflow.BOOL_FIELDS[kind] | ImportWorkflow.ENUM_FIELDS[kind] | {
            "texture": {"spritePivot", "alphaSource", "alphaIsTransparency", "textureType", "textureShape", "isReadable", "mipmapEnabled", "sRGBTexture", "streamingMipmaps", "maxTextureSize", "textureCompression", "spriteImportMode", "spritePixelsPerUnit", "npotScale", "filterMode", "wrapMode", "anisoLevel", "format", "compressionQuality", "resizeAlgorithm", "crunchedCompression", "androidETC2FallbackOverride", "overridden"},
            "audio": {"quality", "sampleRateOverride", "forceToMono", "loadInBackground", "ambisonic", "loadType", "compressionFormat", "sampleRateSetting", "overridden"},
            "model": {"globalScale", "useFileScale", "importBlendShapes", "importVisibility", "importCameras", "importLights", "meshCompression", "isReadable", "optimizeMeshPolygons", "optimizeMeshVertices", "meshOptimizationFlags", "generateSecondaryUV", "keepQuads", "weldVertices", "importNormals", "importTangents", "animationType", "importAnimation", "materialImportMode", "materialLocation", "normalCalculationMode", "normalSmoothingAngle", "indexFormat", "swapUVChannels", "importConstraints", "avatarSetup", "clipAnimations", "overridden"},
        }[kind]

    @staticmethod
    def _asset(value):
        if not isinstance(value, str) or not value.strip() or "\x00" in value:
            raise ImportWorkflowRefusal("INVALID_ARGUMENT", "asset must be a nonempty exact asset identity")
        return value

    @classmethod
    def _platform(cls, value):
        if not isinstance(value, str) or not value.strip():
            raise ImportWorkflowRefusal("INVALID_ARGUMENT", "platform must be a nonempty canonical platform name")
        canonical = cls.PLATFORM_ALIASES.get(value, value)
        if canonical not in {"Default", "Standalone", "iOS", "Android", "WebGL"}:
            raise ImportWorkflowRefusal("IMPORT_PREFLIGHT_INVALID", "platform must be Default, Standalone, iOS, Android, or WebGL", {"platform": value})
        return canonical

    @staticmethod
    def _settings(value):
        if not isinstance(value, dict) or not value:
            raise ImportWorkflowRefusal("INVALID_ARGUMENT", "settings must be a nonempty object")
        return dict(value)

    @staticmethod
    def _finite_number(value):
        return not isinstance(value, bool) and isinstance(value, (int, float)) and math.isfinite(value)

    @staticmethod
    def _integer(value):
        return isinstance(value, int) and not isinstance(value, bool)

    @staticmethod
    def _invalid(field, value, reason):
        raise ImportWorkflowRefusal("IMPORT_PREFLIGHT_INVALID", f"Invalid importer field {field}: {reason}", {"field": field, "value": value})
