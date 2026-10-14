using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public static class Helpers
{
    /// <summary>
    /// <param name="seconds"> The duration in seconds to wait</param>
    /// </summary>
    static readonly Dictionary<float, WaitForSeconds> WaitForSeconds = new();

    public static WaitForSeconds GetWaitForSeconds(float seconds)
    {
        if (WaitForSeconds.TryGetValue(seconds, out var forSeconds)) return forSeconds;

        var waitForSeconds = new WaitForSeconds(seconds);
        WaitForSeconds.Add(seconds, waitForSeconds);
        return WaitForSeconds[seconds];
    }
}
public static class Vector3Extensions
{
    public static Vector3 Add(this Vector3 vector, float x = 0, float y = 0, float z = 0)
    {
        return new Vector3(vector.x + x, vector.y + y, vector.z + z);
    }
}
