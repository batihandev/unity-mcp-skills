if (width <= 0 || height <= 0) throw new System.ArgumentOutOfRangeException("width and height must be positive");
if (!ulong.TryParse(cameraEntityId, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var rawId))
    throw new System.ArgumentException("camera must be an unsigned decimal EntityId string");
var camera = UnityEditor.EditorUtility.EntityIdToObject(UnityEngine.EntityId.FromULong(rawId)) as UnityEngine.Camera;
if (camera == null) throw new System.ArgumentException("camera is stale or does not resolve to a Camera");
var previousTarget = camera.targetTexture;
var previousActive = UnityEngine.RenderTexture.active;
var target = UnityEngine.RenderTexture.GetTemporary(width, height, 24, UnityEngine.RenderTextureFormat.ARGB32);
try {
    camera.targetTexture = target;
    camera.Render();
    UnityEngine.RenderTexture.active = target;
    var pixels = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGBA32, false);
    try { pixels.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0); pixels.Apply(false, false); return new { pngBase64 = System.Convert.ToBase64String(UnityEngine.ImageConversion.EncodeToPNG(pixels)), width, height }; }
    finally { UnityEngine.Object.DestroyImmediate(pixels); }
} finally { camera.targetTexture = previousTarget; UnityEngine.RenderTexture.active = previousActive; UnityEngine.RenderTexture.ReleaseTemporary(target); }
