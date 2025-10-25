using UnityEngine;

public static class AnimationUltis
{
    public static AnimationCurve CreateInOutBackCurve()
    {
        var smoothCurve = new AnimationCurve();

        // Constants for back easing
        float c1 = 1.70158f;
        float c2 = c1 * 1.525f;

        // Tạo nhiều keyframes để curve mượt mà hơn
        int keyCount = 50;

        for (int i = 0; i <= keyCount; i++)
        {
            float t = i / (float)keyCount;
            float value;

            // InOutBack easing formula
            if (t < 0.5f)
            {
                value = (Mathf.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2;
            }
            else
            {
                value = (Mathf.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
            }

            // Add keyframe
            Keyframe key = new Keyframe(t, value);

            // Set tangents to smooth (optional, có thể điều chỉnh)
            key.inTangent = 0;
            key.outTangent = 0;
            key.weightedMode = WeightedMode.None;

            smoothCurve.AddKey(key);
        }

        // Smooth all tangents
        for (int i = 0; i < smoothCurve.keys.Length; i++)
        {
            smoothCurve.SmoothTangents(i, 0);
        }

        return smoothCurve;
    }
}