using UnityEngine;

public static class Perlin {
    public static float getPerlin(float offset, float x, float y) {
        var scale = 0.3f;
        return Mathf.Clamp01(Mathf.PerlinNoise(x * scale + 1000 + offset, y * scale + 2000));
    }
}
