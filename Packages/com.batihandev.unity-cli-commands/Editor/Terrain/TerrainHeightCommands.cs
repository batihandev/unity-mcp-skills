using System;
using System.Globalization;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEngine;
using UnityTerrain = UnityEngine.Terrain;

namespace BatihanDev.UnityCliCommands.TerrainAuthoring
{
    internal static class TerrainHeightMath
    {
        internal static float[,] ParseRectangle(string json)
        {
            JToken token;
            try { token = JToken.Parse(json); }
            catch (Exception e) when (e is Newtonsoft.Json.JsonException || e is ArgumentNullException)
            { throw new ArgumentException("heights must be rectangular JSON [z][x] with finite numbers."); }
            if (!(token is JArray rows) || rows.Count == 0 || !(rows[0] is JArray first) || first.Count == 0)
                throw new ArgumentException("heights must be a nonempty rectangular JSON [z][x] array.");
            var output = new float[rows.Count, first.Count];
            for (int z = 0; z < rows.Count; z++)
            {
                if (!(rows[z] is JArray row) || row.Count != first.Count)
                    throw new ArgumentException("heights rows must all have the same nonzero length.");
                for (int x = 0; x < row.Count; x++)
                {
                    if (!(row[x] is JValue value) || (value.Type != JTokenType.Float && value.Type != JTokenType.Integer) ||
                        !float.TryParse(Convert.ToString(value.Value, CultureInfo.InvariantCulture), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out var parsed) || !TerrainCore.Finite(parsed))
                        throw new ArgumentException("heights values must be finite JSON numbers.");
                    output[z, x] = Mathf.Clamp01(parsed);
                }
            }
            return output;
        }

        internal static (int x, int z, int width, int length) Bounds(int resolution, float centerX, float centerZ, int pixelRadius)
        {
            var cx = TerrainCore.Pixel(centerX, resolution);
            var cz = TerrainCore.Pixel(centerZ, resolution);
            var x = Mathf.Max(0, cx - pixelRadius);
            var z = Mathf.Max(0, cz - pixelRadius);
            return (x, z, Mathf.Min(resolution - 1, cx + pixelRadius) - x + 1,
                Mathf.Min(resolution - 1, cz + pixelRadius) - z + 1);
        }

        internal static float Falloff(float x, float z, float cx, float cz, float radius)
        {
            if (radius == 0f) return x == cx && z == cz ? 1f : 0f;
            var distance = Mathf.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz));
            return distance > radius ? 0f : Mathf.Max(0f, Mathf.Cos(distance / radius * Mathf.PI * 0.5f));
        }


    }

    public static class TerrainHeightCommands
    {
        private static CommandResult<object> Prepare(string schema, string target, bool confirm, bool dryRun,
            out UnityTerrain terrain) => TerrainCore.PrepareWrite(schema, target, confirm, dryRun, out terrain);
        private static CommandResult<object> Invalid(string schema, string message) => TerrainCore.Fail(schema, "INPUT_INVALID", message);
        private static bool CenterRadius(float x, float z, float radius) =>
            TerrainCore.Unit(x) && TerrainCore.Unit(z) && TerrainCore.Unit(radius);

        [CliCommand("terrain.height-set", "Set one normalized height sample on exact TerrainData.", Tags = new[] { "unity-cli-commands", "terrain" })]
        public static CommandResult<object> SetHeight(
            [CliArg("target", "Exact Terrain component handle.", Required = true)] string target,
            [CliArg("normalizedX", "Normalized X, 0..1.")] float normalizedX = 0.5f,
            [CliArg("normalizedZ", "Normalized Z, 0..1.")] float normalizedZ = 0.5f,
            [CliArg("height", "Normalized height, clamped 0..1.")] float height = 0.5f,
            [CliArg("confirm", "Apply.")] bool confirm = false,
            [CliArg("dryRun", "Preview.")] bool dryRun = false)
        {
            const string schema = "unity.terrain.height-set@1";
            var check = Prepare(schema, target, confirm, dryRun, out var terrain);
            if (check != null) return check;
            if (!TerrainCore.Finite(normalizedX) || !TerrainCore.Finite(normalizedZ) || !TerrainCore.Finite(height))
                return Invalid(schema, "Coordinates and height must be finite.");
            var data = terrain.terrainData;
            int x = TerrainCore.Pixel(Mathf.Clamp01(normalizedX), data.heightmapResolution);
            int z = TerrainCore.Pixel(Mathf.Clamp01(normalizedZ), data.heightmapResolution);
            return TerrainCore.WriteHeights(schema, terrain, new[,] { { Mathf.Clamp01(height) } }, x, z, dryRun,
                new { normalizedX, normalizedZ, requested = height, actual = Mathf.Clamp01(height) });
        }

        [CliCommand("terrain.heights-set", "Set one clipped rectangular [z][x] height region.", Tags = new[] { "unity-cli-commands", "terrain" })]
        public static CommandResult<object> SetHeights(
            [CliArg("target", "Exact Terrain component handle.", Required = true)] string target,
            [CliArg("heights", "Strict rectangular JSON [z][x] finite numbers.", Required = true)] string heights,
            [CliArg("startX", "Start pixel X, clamped to heightmap.")] int startX = 0,
            [CliArg("startZ", "Start pixel Z, clamped to heightmap.")] int startZ = 0,
            [CliArg("confirm", "Apply.")] bool confirm = false,
            [CliArg("dryRun", "Preview.")] bool dryRun = false)
        {
            const string schema = "unity.terrain.heights-set@1";
            var check = Prepare(schema, target, confirm, dryRun, out var terrain);
            if (check != null) return check;
            float[,] input;
            try { input = TerrainHeightMath.ParseRectangle(heights); }
            catch (ArgumentException e) { return Invalid(schema, e.Message); }
            var resolution = terrain.terrainData.heightmapResolution;
            int x = Mathf.Clamp(startX, 0, resolution - 1), z = Mathf.Clamp(startZ, 0, resolution - 1);
            int width = Math.Min(input.GetLength(1), resolution - x), length = Math.Min(input.GetLength(0), resolution - z);
            var clipped = new float[length, width];
            float min = 1f, max = 0f;
            for (int row = 0; row < length; row++) for (int col = 0; col < width; col++)
            {
                var value = input[row, col]; clipped[row, col] = value;
                min = Mathf.Min(min, value); max = Mathf.Max(max, value);
            }
            return TerrainCore.WriteHeights(schema, terrain, clipped, x, z, dryRun,
                new { startX, startZ, suppliedWidth = input.GetLength(1), suppliedLength = input.GetLength(0), min, max });
        }

        [CliCommand("terrain.hill", "Add a circular cosine hill or depression.", Tags = new[] { "unity-cli-commands", "terrain" })]
        public static CommandResult<object> Hill(
            [CliArg("target", "Exact Terrain component handle.", Required = true)] string target,
            [CliArg("centerX", "Normalized center X.")] float centerX = 0.5f,
            [CliArg("centerZ", "Normalized center Z.")] float centerZ = 0.5f,
            [CliArg("radius", "Normalized radius.")] float radius = 0.2f,
            [CliArg("height", "Signed normalized height change.")] float height = 0.5f,
            [CliArg("smoothness", "Nonnegative falloff exponent.")] float smoothness = 1f,
            [CliArg("confirm", "Apply.")] bool confirm = false,
            [CliArg("dryRun", "Preview.")] bool dryRun = false)
        {
            const string schema = "unity.terrain.hill@1";
            var check = Prepare(schema, target, confirm, dryRun, out var terrain);
            if (check != null) return check;
            if (!CenterRadius(centerX, centerZ, radius) || !TerrainCore.Finite(height) ||
                !TerrainCore.Finite(smoothness) || smoothness < 0f)
                return Invalid(schema, "Center/radius must be 0..1; height finite; smoothness finite and nonnegative.");
            var data = terrain.terrainData;
            var pixelRadius = Mathf.Max(1, Mathf.RoundToInt(radius * data.heightmapResolution));
            var rect = TerrainHeightMath.Bounds(data.heightmapResolution, centerX, centerZ, pixelRadius);
            var candidate = data.GetHeights(rect.x, rect.z, rect.width, rect.length);
            var cx = TerrainCore.Pixel(centerX, data.heightmapResolution);
            var cz = TerrainCore.Pixel(centerZ, data.heightmapResolution);
            for (int row = 0; row < rect.length; row++) for (int col = 0; col < rect.width; col++)
            {
                var dx = rect.x + col - cx;
                var dz = rect.z + row - cz;
                if ((double)dx * dx + (double)dz * dz <= (double)pixelRadius * pixelRadius)
                {
                    var falloff = TerrainHeightMath.Falloff(rect.x + col, rect.z + row, cx, cz, pixelRadius);
                    candidate[row, col] = Mathf.Clamp01(candidate[row, col] + height * Mathf.Pow(falloff, smoothness));
                }
            }
            return TerrainCore.WriteHeights(schema, terrain, candidate, rect.x, rect.z, dryRun,
                new { centerX, centerZ, radius, height, smoothness });
        }

        [CliCommand("terrain.flatten", "Blend a local circular height region toward a target.", Tags = new[] { "unity-cli-commands", "terrain" })]
        public static CommandResult<object> Flatten(
            [CliArg("target", "Exact Terrain component handle.", Required = true)] string target,
            [CliArg("centerX", "Normalized center X.")] float centerX = 0.5f,
            [CliArg("centerZ", "Normalized center Z.")] float centerZ = 0.5f,
            [CliArg("radius", "Normalized radius; zero selects one pixel.")] float radius = 0.1f,
            [CliArg("targetHeight", "Normalized target, clamped 0..1.")] float targetHeight = 0.5f,
            [CliArg("strength", "Blend strength, 0..1.")] float strength = 1f,
            [CliArg("confirm", "Apply.")] bool confirm = false,
            [CliArg("dryRun", "Preview.")] bool dryRun = false)
        {
            const string schema = "unity.terrain.flatten@1";
            var check = Prepare(schema, target, confirm, dryRun, out var terrain);
            if (check != null) return check;
            if (!CenterRadius(centerX, centerZ, radius) || !TerrainCore.Finite(targetHeight) || !TerrainCore.Unit(strength))
                return Invalid(schema, "Center/radius/strength must be 0..1; target height must be finite.");
            var data = terrain.terrainData;
            var pixelRadius = Mathf.RoundToInt(radius * data.heightmapResolution);
            var rect = TerrainHeightMath.Bounds(data.heightmapResolution, centerX, centerZ, pixelRadius);
            var candidate = data.GetHeights(rect.x, rect.z, rect.width, rect.length);
            var cx = TerrainCore.Pixel(centerX, data.heightmapResolution);
            var cz = TerrainCore.Pixel(centerZ, data.heightmapResolution);
            for (int row = 0; row < rect.length; row++) for (int col = 0; col < rect.width; col++)
            {
                var weight = strength * TerrainHeightMath.Falloff(rect.x + col, rect.z + row, cx, cz, pixelRadius);
                candidate[row, col] = Mathf.Lerp(candidate[row, col], Mathf.Clamp01(targetHeight), weight);
            }
            return TerrainCore.WriteHeights(schema, terrain, candidate, rect.x, rect.z, dryRun,
                new { centerX, centerZ, radius, targetHeight = Mathf.Clamp01(targetHeight), strength });
        }

        [CliCommand("terrain.smooth", "Apply bounded 3x3 height smoothing with one-sample halo.", Tags = new[] { "unity-cli-commands", "terrain" })]
        public static CommandResult<object> Smooth(
            [CliArg("target", "Exact Terrain component handle.", Required = true)] string target,
            [CliArg("centerX", "Normalized center X.")] float centerX = 0.5f,
            [CliArg("centerZ", "Normalized center Z.")] float centerZ = 0.5f,
            [CliArg("radius", "Normalized target radius.")] float radius = 0.1f,
            [CliArg("iterations", "Smoothing passes, 0..64.")] int iterations = 1,
            [CliArg("confirm", "Apply.")] bool confirm = false,
            [CliArg("dryRun", "Preview.")] bool dryRun = false)
        {
            const string schema = "unity.terrain.smooth@1";
            var check = Prepare(schema, target, confirm, dryRun, out var terrain);
            if (check != null) return check;
            if (!CenterRadius(centerX, centerZ, radius) || iterations < 0 || iterations > 64)
                return Invalid(schema, "Center/radius must be 0..1 and iterations must be 0..64.");
            var data = terrain.terrainData;
            var rect = TerrainHeightMath.Bounds(data.heightmapResolution, centerX, centerZ,
                Mathf.RoundToInt(radius * data.heightmapResolution));
            if (iterations == 0) return CommandResult<object>.Success(schema, new { identity = TerrainCore.Identity(terrain),
                rect, iterations, applied = false, dryRun, saved = false, undoable = false });
            int x0 = Math.Max(0, rect.x - 1), z0 = Math.Max(0, rect.z - 1);
            int x1 = Math.Min(data.heightmapResolution - 1, rect.x + rect.width), z1 = Math.Min(data.heightmapResolution - 1, rect.z + rect.length);
            var current = data.GetHeights(x0, z0, x1 - x0 + 1, z1 - z0 + 1);
            for (int pass = 0; pass < iterations; pass++)
            {
                var next = (float[,])current.Clone();
                for (int row = rect.z; row < rect.z + rect.length; row++) for (int col = rect.x; col < rect.x + rect.width; col++)
                {
                    if (row == 0 || col == 0 || row == data.heightmapResolution - 1 || col == data.heightmapResolution - 1) continue;
                    float sum = 0f;
                    for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++) sum += current[row + dz - z0, col + dx - x0];
                    next[row - z0, col - x0] = sum / 9f;
                }
                current = next;
            }
            var candidate = new float[rect.length, rect.width];
            for (int row = 0; row < rect.length; row++) for (int col = 0; col < rect.width; col++)
                candidate[row, col] = current[rect.z + row - z0, rect.x + col - x0];
            return TerrainCore.WriteHeights(schema, terrain, candidate, rect.x, rect.z, dryRun,
                new { centerX, centerZ, radius, iterations });
        }

        [CliCommand("terrain.perlin", "Replace full heightmap with reproducible multi-octave Perlin noise.", Tags = new[] { "unity-cli-commands", "terrain" })]
        public static CommandResult<object> Perlin(
            [CliArg("target", "Exact Terrain component handle.", Required = true)] string target,
            [CliArg("scale", "Nonnegative base sampling scale.")] float scale = 20f,
            [CliArg("multiplier", "Signed height multiplier.")] float multiplier = 0.3f,
            [CliArg("octaves", "Octaves, 1..32.")] int octaves = 4,
            [CliArg("persistence", "Nonnegative amplitude multiplier.")] float persistence = 0.5f,
            [CliArg("lacunarity", "Positive frequency multiplier.")] float lacunarity = 2f,
            [CliArg("seed", "Nonzero repeatable seed; zero selects a recorded seed.")] int seed = 0,
            [CliArg("confirm", "Apply.")] bool confirm = false,
            [CliArg("dryRun", "Preview.")] bool dryRun = false)
        {
            const string schema = "unity.terrain.perlin@1";
            var check = Prepare(schema, target, confirm, dryRun, out var terrain);
            if (check != null) return check;
            if (!TerrainCore.Finite(scale) || scale < 0f || !TerrainCore.Finite(multiplier) ||
                octaves < 1 || octaves > 32 || !TerrainCore.Finite(persistence) || persistence < 0f ||
                !TerrainCore.Finite(lacunarity) || lacunarity <= 0f)
                return Invalid(schema, "Scale/persistence must be nonnegative, lacunarity positive, multiplier finite, octaves 1..32.");
            int actualSeed = seed == 0 ? Guid.NewGuid().GetHashCode() : seed;
            if (actualSeed == 0) actualSeed = 1;
            var random = new System.Random(actualSeed);
            float offsetX = random.Next(-10000, 10000);
            float offsetZ = random.Next(-10000, 10000);
            var resolution = terrain.terrainData.heightmapResolution;
            float amplitude = 1f, frequency = 1f;
            for (int i = 0; i < octaves; i++)
            {
                double endpoint = ((double)scale + 10000d) * frequency;
                if (double.IsNaN(endpoint) || double.IsInfinity(endpoint) || Math.Abs(endpoint) > 100000000d ||
                    !TerrainCore.Finite(amplitude) || !TerrainCore.Finite(frequency) ||
                    Math.Abs((double)amplitude * multiplier) > 100000000d)
                    return Invalid(schema, "Derived Perlin sampling or amplitude is outside finite supported range.");
                amplitude *= persistence; frequency *= lacunarity;
                if (!TerrainCore.Finite(amplitude) || !TerrainCore.Finite(frequency))
                    return Invalid(schema, "Derived Perlin amplitude or frequency is nonfinite.");
            }
            var heights = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++) for (int x = 0; x < resolution; x++)
            {
                float value = 0f; amplitude = 1f; frequency = 1f;
                for (int i = 0; i < octaves; i++)
                {
                    float sx = (x / (float)resolution * scale + offsetX) * frequency;
                    float sz = (z / (float)resolution * scale + offsetZ) * frequency;
                    float noise = Mathf.PerlinNoise(sx, sz) * 2f - 1f;
                    value += noise * amplitude;
                    amplitude *= persistence; frequency *= lacunarity;
                }
                float final = value * multiplier + 0.5f;
                if (!TerrainCore.Finite(final)) return Invalid(schema, "Derived Perlin height is nonfinite.");
                heights[z, x] = Mathf.Clamp01(final);
            }
            return TerrainCore.WriteHeights(schema, terrain, heights, 0, 0, dryRun,
                new { scale, multiplier, octaves, persistence, lacunarity, seed = actualSeed, offsetX, offsetZ });
        }
    }
}
