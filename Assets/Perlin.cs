using UnityEngine;

public class NoiseFunctions {
    public static float getPerlin(float scale, float offset, float x, float y) {
        var increasedOffset = scale * 10000;
        var value = Mathf.Clamp01(Mathf.PerlinNoise((increasedOffset + x) / scale, (increasedOffset + y) / scale));
        return value;
    }
}

internal class PerlinProvider {
    private float scale;
    private float offsetX;
    private float offsetY;
    private float weight1;
    private float weight2;
	private float totalWeight;
    private float exponent;

    internal PerlinProvider(float scale, float offset, float weight1, float weight2, float exponent) {
        this.scale = scale;
        this.offsetX = offset + scale * 10000;
        this.offsetY = offset + scale * 10000;
        this.weight1 = weight1;
        this.weight2 = weight2;
        this.exponent = exponent;
		totalWeight = weight1 + weight2 + 1;
    }

    internal void updateOffset(float x, float y) {
        offsetX += x;
        offsetY += y;
    }

	internal float get(float x, float y) {
        x += offsetX;
        y += offsetY;
		return Mathf.Pow((getPerlin(scale, x, y) 
							+ getPerlin(scale * 0.5f, x, y) * weight1
							+ getPerlin(scale * 0.2f, x, y) * weight2)
						/ totalWeight, exponent);
	}

    private float getPerlin(float scale, float x, float y) {
        var value = Mathf.Clamp01(Mathf.PerlinNoise(x / scale, y / scale));
        return value;
    }
}
