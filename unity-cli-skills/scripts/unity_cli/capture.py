from __future__ import annotations

import base64
import hashlib
import json
import pathlib
import re
import struct
import tempfile
import zlib

from . import authoring
from .compile import CompileWorkflow, WorkflowRefusal
from .lifecycle import SessionRefusal


class CaptureRefusal(RuntimeError):
    def __init__(self, code, message, details=None):
        super().__init__(message)
        self.code = code
        self.details = details or {}

    def as_dict(self):
        return {'ok': False, 'error': {'code': self.code, 'message': str(self), 'details': self.details}}


def _dimensions(width, height):
    if any(type(value) is not int or not 0 < value <= 2147483647 for value in (width, height)):
        raise CaptureRefusal('INVALID_ARGUMENT', 'Capture dimensions must be positive 32-bit integers.')


def png_dimensions(data):
    if not isinstance(data, bytes) or len(data) < 33 or data[:8] != b'\x89PNG\r\n\x1a\n' or data[8:16] != b'\0\0\0\rIHDR':
        raise CaptureRefusal('PNG_INVALID', 'Capture must return a PNG with a complete IHDR.')
    if zlib.crc32(data[12:29]) != struct.unpack('>I', data[29:33])[0]:
        raise CaptureRefusal('PNG_INVALID', 'PNG IHDR checksum is invalid.')
    width, height = struct.unpack('>II', data[16:24])
    _dimensions(width, height)
    return width, height


def _entity_id(value):
    if not isinstance(value, str) or not re.fullmatch(r'[0-9]+', value) or int(value) > 18446744073709551615:
        raise CaptureRefusal('INVALID_ARGUMENT', 'Object identity must be an unsigned decimal EntityId string.')
    return value


def _source():
    return (pathlib.Path(__file__).resolve().parents[1] / 'camera_capture.cs').read_text(encoding='utf-8')


def camera_code(camera, width=1920, height=1080):
    _dimensions(width, height)
    camera = _entity_id(camera)
    return f'string cameraEntityId = "{camera}";\nint width = {width}, height = {height};\n' + _source()


def _filename(value):
    if not isinstance(value, str) or not value or value in ('.', '..') or any(c in value for c in '/\\\0:'):
        raise CaptureRefusal('INVALID_ARGUMENT', 'Filename must be one PNG basename.')
    path = pathlib.PurePosixPath(value)
    if path.suffix and path.suffix.lower() != '.png':
        raise CaptureRefusal('INVALID_ARGUMENT', 'Capture filename must have a PNG extension.')
    return value if path.suffix else value + '.png'


def _asset_path(value):
    if not isinstance(value, str):
        raise CaptureRefusal('INVALID_ARGUMENT', 'Capture path must be beneath Assets.')
    value = value.replace('\\', '/')
    pieces = value.split('/')
    if len(pieces) < 2 or pieces[0] != 'Assets' or any(p in ('', '.', '..') for p in pieces) or ':' in value or '\0' in value:
        raise CaptureRefusal('INVALID_ARGUMENT', 'Capture path must be normalized beneath Assets.')
    pieces[-1] = _filename(pieces[-1])
    return '/'.join(pieces)


def panel_requests(items):
    if not isinstance(items, list) or not items:
        raise CaptureRefusal('INVALID_ARGUMENT', 'Panel capture needs a nonempty ordered panel array.')
    result = []
    for item in items:
        if not isinstance(item, dict) or not isinstance(item.get('panel'), str) or not item['panel']:
            raise CaptureRefusal('INVALID_ARGUMENT', 'Each item must name one exact panel.')
        prepared = dict(item)
        prepared['width'], prepared['height'] = item.get('width', 390), item.get('height', 844)
        _dimensions(prepared['width'], prepared['height'])
        prepared['path'] = _asset_path(item['path']) if 'path' in item else 'Assets/Screenshots/UI/' + _filename(item.get('filename', item['panel']))
        result.append(prepared)
    if len({x['panel'] for x in result}) != len(result) or len({x['path'] for x in result}) != len(result):
        raise CaptureRefusal('INVALID_ARGUMENT', 'Panel names and output paths must be unique within the batch.')
    return result


def _request_prelude(request):
    encoded = base64.b64encode(json.dumps(request, ensure_ascii=True).encode()).decode()
    return 'var request = Newtonsoft.Json.Linq.JObject.Parse(System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String("' + encoded + '")));\n'


# The same no-follow check guards import and inverse refresh in the selected Editor.
ASSET_PATH_POLICY = r'''
System.Action<string> guardAssetPath = assetPath => {
 if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/",System.StringComparison.Ordinal) || assetPath.Contains("\\") || assetPath.Contains(":") || System.Array.Exists(assetPath.Split('/'),p=>p==".." || p=="." || p=="")) throw new System.ArgumentException("invalid Assets path");
 var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath,".."));
 var absolute = System.IO.Path.GetFullPath(System.IO.Path.Combine(projectRoot,assetPath));
 var assetsRoot = System.IO.Path.GetFullPath(UnityEngine.Application.dataPath) + System.IO.Path.DirectorySeparatorChar;
 if(!absolute.StartsWith(assetsRoot,System.StringComparison.Ordinal)) throw new System.ArgumentException("path outside Assets");
 foreach(var target in new[]{absolute,absolute+".meta"}) {
  var cursor=target;
  while(cursor!=null) {
   try { if((System.IO.File.GetAttributes(cursor)&System.IO.FileAttributes.ReparsePoint)!=0) throw new System.InvalidOperationException("linked asset path"); }
   catch(System.IO.FileNotFoundException) {} catch(System.IO.DirectoryNotFoundException) {}
   cursor=System.IO.Path.GetDirectoryName(cursor);
  }
 }
};
'''

UI_RESOLVE = r'''
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling || UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage()!=null) throw new System.InvalidOperationException("UI capture requires ready Edit mode outside Prefab Stage");
System.Func<string,UnityEngine.GameObject> byId = value => {
 if(!ulong.TryParse(value,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var raw)) throw new System.ArgumentException("unsigned EntityId required");
 var go=UnityEditor.EditorUtility.EntityIdToObject(UnityEngine.EntityId.FromULong(raw)) as UnityEngine.GameObject;
 if(go==null || !go.scene.IsValid() || !go.scene.isLoaded || UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(go.scene)) throw new System.ArgumentException("stale or non-scene GameObject");
 return go;
};
var canvasName=(string)request["canvas"];
var rootName=(string)request["root"];
UnityEngine.GameObject canvasGo = null;
if(request["canvasId"] != null) canvasGo=byId((string)request["canvasId"]);
else foreach(var candidate in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Canvas>()) {
 if(candidate.gameObject.name!=canvasName || !candidate.gameObject.scene.IsValid() || !candidate.gameObject.scene.isLoaded || UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(candidate.gameObject.scene)) continue;
 if(canvasGo!=null) throw new System.ArgumentException("Canvas name is ambiguous; supply exact canvasId"); canvasGo=candidate.gameObject;
}
if(canvasGo==null) throw new System.ArgumentException("Canvas not found");
var canvasComp=canvasGo.GetComponent<UnityEngine.Canvas>();
if(canvasComp==null) throw new System.ArgumentException("exact Canvas GameObject required");
UnityEngine.Transform uiRoot=null;
if(request["rootId"]!=null) uiRoot=byId((string)request["rootId"]).transform;
else foreach(UnityEngine.Transform child in canvasGo.transform) if(child.name==rootName) { if(uiRoot!=null) throw new System.ArgumentException("UIRoot name is ambiguous"); uiRoot=child; }
if(uiRoot==null || uiRoot.parent!=canvasGo.transform) throw new System.ArgumentException("exact direct Canvas root required");
'''


def ui_code(item, *, canvas='Canvas', root='UIRoot', canvas_id=None, root_id=None):
    _dimensions(item.get('width', 390), item.get('height', 844))
    request = dict(item, canvas=canvas, root=root)
    for key, value in (('canvasId', canvas_id), ('rootId', root_id)):
        if value is not None: request[key] = _entity_id(value)
    if item.get('panelId') is not None: request['panelId'] = _entity_id(item['panelId'])
    code = _request_prelude(request) + UI_RESOLVE + r'''
UnityEngine.Transform targetPanel=null;
if(request["panelId"]!=null) targetPanel=byId((string)request["panelId"]).transform;
else foreach(UnityEngine.Transform child in uiRoot) if(child.name==(string)request["panel"]) { if(targetPanel!=null) throw new System.ArgumentException("panel name is ambiguous"); targetPanel=child; }
if(targetPanel==null || targetPanel.parent!=uiRoot) throw new System.ArgumentException("exact direct UIRoot panel required");
var scene=canvasGo.scene; var originalDirty=scene.isDirty;
var origMode=canvasComp.renderMode; var origWorldCam=canvasComp.worldCamera; var origPlane=canvasComp.planeDistance;
var scaler=canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
var scalerJson=scaler==null ? null : UnityEditor.EditorJsonUtility.ToJson(scaler);
var rectStates=new System.Collections.Generic.Dictionary<UnityEngine.RectTransform,string>();
foreach(var rect in canvasGo.GetComponentsInChildren<UnityEngine.RectTransform>(true)) rectStates[rect]=UnityEditor.EditorJsonUtility.ToJson(rect);
var activeStates=new System.Collections.Generic.Dictionary<UnityEngine.GameObject,bool>();
for(var cursor=targetPanel;cursor!=null;cursor=cursor.parent) {
 activeStates[cursor.gameObject]=cursor.gameObject.activeSelf;
 if(cursor.parent!=null) foreach(UnityEngine.Transform sibling in cursor.parent) activeStates[sibling.gameObject]=sibling.gameObject.activeSelf;
}
UnityEngine.GameObject camGo=null; object captured=null;
try {
 for(var cursor=targetPanel;cursor!=null;cursor=cursor.parent) {
  if(cursor.parent!=null) foreach(UnityEngine.Transform sibling in cursor.parent) sibling.gameObject.SetActive(sibling==cursor);
  cursor.gameObject.SetActive(true);
 }
 camGo=new UnityEngine.GameObject("__UICaptureCam"); camGo.hideFlags=UnityEngine.HideFlags.HideAndDontSave;
 var cam=camGo.AddComponent<UnityEngine.Camera>();
 cam.clearFlags=UnityEngine.CameraClearFlags.SolidColor; cam.backgroundColor=UnityEngine.Color.black; cam.cullingMask=-1;
 cam.orthographic=true; cam.orthographicSize=(int)request["height"]*.5f; cam.nearClipPlane=.1f; cam.farClipPlane=1000f;
 cam.transform.position=new UnityEngine.Vector3(0,0,-500f);
 canvasComp.renderMode=UnityEngine.RenderMode.ScreenSpaceCamera; canvasComp.worldCamera=cam; canvasComp.planeDistance=100f;
 if(scaler!=null) { scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new UnityEngine.Vector2((int)request["width"],(int)request["height"]); }
 try { UnityEngine.Canvas.ForceUpdateCanvases(); } catch(System.Exception) { }
 System.Func<object> renderPixels = () => {
 string cameraEntityId=UnityEngine.EntityId.ToULong(cam.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture);
 int width=(int)request["width"],height=(int)request["height"];
'''
    code += _source()
    code += r'''
 };
 captured=renderPixels();
} finally {
 try {
  canvasComp.renderMode=origMode; canvasComp.worldCamera=origWorldCam; canvasComp.planeDistance=origPlane;
  if(scaler!=null) UnityEditor.EditorJsonUtility.FromJsonOverwrite(scalerJson,scaler);
  foreach(var state in activeStates) if(state.Key!=null) state.Key.SetActive(state.Value);
  foreach(var state in rectStates) if(state.Key!=null) UnityEditor.EditorJsonUtility.FromJsonOverwrite(state.Value,state.Key);
 } finally { if(camGo!=null) UnityEngine.Object.DestroyImmediate(camGo); }
}
if(scene.isDirty!=originalDirty) throw new System.InvalidOperationException("scene dirty state changed during capture");
return new {capture=captured,canvasId=UnityEngine.EntityId.ToULong(canvasGo.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture),rootId=UnityEngine.EntityId.ToULong(uiRoot.gameObject.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture),panelId=UnityEngine.EntityId.ToULong(targetPanel.gameObject.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture),sceneDirty=scene.isDirty,restored=true};
'''
    return code


class CaptureWorkflow:
    def __init__(self, project, session):
        self.project = pathlib.Path(project).resolve()
        self.session = session

    def _eval(self, lease, code):
        lease.verify_current()
        result = self.session.transport.invoke_command(str(self.project), 'eval', {'code': code}, timeout=lease.remaining(), deadline=lease.transport_deadline)
        lease.verify_current()
        value = CompileWorkflow._outer_result(result, 'eval')['result']
        if not isinstance(value, dict) or value.get('success') is not True or 'result' not in value:
            raise CaptureRefusal('CAPTURE_NATIVE_FAILED', 'Native eval failed or omitted its semantic result.', {'native': value})
        return value['result']

    def _ready(self, lease):
        compile_owner = CompileWorkflow(self.project, self.session)
        compile_owner._editor_state(lease)
        compile_owner._fresh_ground_truth(lease)

    def _asset_eval(self, lease, path, action):
        code = _request_prelude({'path': path}) + ASSET_PATH_POLICY + 'string assetPath=(string)request["path"]; guardAssetPath(assetPath);\n'
        if action == 'validate':
            return self._eval(lease, code + 'return new {path=assetPath};')
        if action == 'import':
            code += 'guardAssetPath(assetPath); UnityEditor.AssetDatabase.ImportAsset(assetPath,UnityEditor.ImportAssetOptions.ForceSynchronousImport|UnityEditor.ImportAssetOptions.ForceUpdate);\n'
        elif action == 'refresh':
            code += 'guardAssetPath(assetPath); UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport|UnityEditor.ImportAssetOptions.ForceUpdate);\n'
        code += r'''
var asset=UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath);
var texture=asset as UnityEngine.Texture2D;
return new {path=assetPath,guid=UnityEditor.AssetDatabase.AssetPathToGUID(assetPath),loadable=asset!=null,type=asset==null?null:asset.GetType().FullName,width=texture==null?(int?)null:texture.width,height=texture==null?(int?)null:texture.height};
'''
        return self._eval(lease, code)

    def _publish(self, lease, capture, path, width, height, replace_sha256):
        try:
            data = base64.b64decode(capture['pngBase64'], validate=True)
        except (KeyError, ValueError, TypeError) as exc:
            raise CaptureRefusal('PNG_INVALID', 'Native capture omitted valid inline PNG bytes.') from exc
        actual = png_dimensions(data)
        if actual != (width, height):
            raise CaptureRefusal('PNG_DIMENSIONS_MISMATCH', 'Actual PNG dimensions differ from requested dimensions.', {'requested':[width,height], 'actual':list(actual)})
        path = _asset_path(path)
        request = {'projectRoot':str(self.project), 'operation':'write', 'writes':[{'path':path, 'content':base64.b64encode(data).decode()}]}
        if replace_sha256 is not None:
            if not authoring.valid_sha256(replace_sha256):
                raise CaptureRefusal('INVALID_ARGUMENT', 'Replacement requires an authorized SHA-256.')
            request.update(replaceAuthorized=True, expectedSha256={path:replace_sha256})
        snapshot_directory = pathlib.Path(tempfile.mkdtemp(prefix='unity-capture-recovery-'))
        try:
            result = authoring.import_asset_lifecycle(request,
                       validate_path=lambda value: self._asset_eval(lease,value,'validate'),
                       observe=lambda value: self._asset_eval(lease,value,'observe'),
                       import_asset=lambda value: self._asset_eval(lease,value,'import'),
                       refresh=lambda: self._asset_eval(lease,path,'refresh'),
                       expected_type='UnityEngine.Texture2D', snapshot_directory=snapshot_directory)
            result.update(path=path,width=actual[0],height=actual[1],pngSha256=hashlib.sha256(data).hexdigest(),editorIdentity=lease.identity.as_dict())
            return result
        finally:
            if not any(snapshot_directory.iterdir()): snapshot_directory.rmdir()

    def camera(self, *, camera, width=1920, height=1080, path='Assets/screenshot.png', replace_sha256=None):
        code = camera_code(camera,width,height)
        path = _asset_path(path)
        try:
            with self.session.workflow_session() as lease:
                self._ready(lease)
                capture = self._eval(lease,code)
                result = self._publish(lease,capture,path,width,height,replace_sha256)
                result['action'] = 'camera-capture'
                return result
        except (SessionRefusal, WorkflowRefusal, authoring.Refusal) as exc:
            raise CaptureRefusal(exc.code,str(exc),exc.details) from exc

    def panel(self, *, panel, width=390, height=844, filename=None, path=None, canvas='Canvas', root='UIRoot', canvas_id=None, root_id=None, panelId=None, replace_sha256=None):
        item = {'panel':panel,'width':width,'height':height}
        if filename is not None: item['filename'] = filename
        if path is not None: item['path'] = path
        if panelId is not None: item['panelId'] = panelId
        item = panel_requests([item])[0]
        code = ui_code(item,canvas=canvas,root=root,canvas_id=canvas_id,root_id=root_id)
        try:
            with self.session.workflow_session() as lease:
                self._ready(lease)
                value = self._eval(lease,code)
                if not isinstance(value,dict) or value.get('restored') is not True:
                    raise CaptureRefusal('UI_RESTORATION_UNVERIFIED','UI capture did not verify scene restoration.')
                result = self._publish(lease,value['capture'],item['path'],width,height,replace_sha256)
                result.update(action='ui-capture',panel=panel,canvasId=value['canvasId'],rootId=value['rootId'],panelId=value['panelId'],sceneDirty=value['sceneDirty'],restored=True)
                return result
        except (SessionRefusal, WorkflowRefusal, authoring.Refusal) as exc:
            raise CaptureRefusal(exc.code,str(exc),exc.details) from exc

    def panels(self, *, items, canvas='Canvas', root='UIRoot', canvas_id=None, root_id=None):
        prepared = panel_requests(items)
        outcomes = []
        for index, item in enumerate(prepared):
            try:
                result = self.panel(**item,canvas=canvas,root=root,canvas_id=canvas_id,root_id=root_id)
                outcomes.append({'index':index,'ok':result.get('ok') is True,'result':result})
            except CaptureRefusal as exc:
                outcomes.append({'index':index,'ok':False,'error':exc.as_dict()['error']})
        return {'ok':all(row['ok'] for row in outcomes),'action':'ui-capture-batch','transactional':False,'outcomes':outcomes}
