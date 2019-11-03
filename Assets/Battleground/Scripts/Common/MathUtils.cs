using Unity.Mathematics;

public class MathUtils {
    
    public static float3 axisAngles(quaternion quaternion) {
        float4 rotation = math.normalizesafe(quaternion).value;
        var angle = 2 * math.acos(rotation.w);
        var constant = math.sqrt(1 - rotation.w * rotation.w);
        float3 axisOfRotation;
        if (constant < 0.001) {
            // angle is effectively 0
            axisOfRotation = new float3(1, 0, 0);
        } else {
            axisOfRotation = new float3(rotation.x / constant, rotation.y / constant, rotation.z / constant);
        }
        return axisOfRotation * angle;
    }
}