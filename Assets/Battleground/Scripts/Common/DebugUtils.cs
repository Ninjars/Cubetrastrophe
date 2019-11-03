using Unity.Mathematics;
using UnityEngine;

public class DebugUtils {
    public static string log(float3 arg) {
        return $"({math.degrees(arg.x)}, {math.degrees(arg.y)}, {math.degrees(arg.z)})";
    }

    public static string log(quaternion arg) {
        var angles = MathUtils.axisAngles(arg);
        return $"angles {log(angles)}";
    }
}