using UnityEngine;

public class HoldNoteVisual : MonoBehaviour
{
    public Transform startCircle, holdBody, endCircle;
    public float bodyHeight = 0.15f;
    Vector3 localStart, localEnd;
    bool hasEndpoints;

    public void SetHoldPoints(Vector3 start, Vector3 end)
    {
        transform.position = start;
        localStart = transform.InverseTransformPoint(start);
        localEnd = transform.InverseTransformPoint(end);
        hasEndpoints = true;
        if (startCircle != null) startCircle.localPosition = localStart;
        if (endCircle != null) endCircle.localPosition = localEnd;
        if (holdBody == null) return;

        // Hold is one straight bar, even across slopes, jumps and direction changes.
        Vector3 direction = localEnd - localStart;
        holdBody.localPosition = (localStart + localEnd) * 0.5f;
        holdBody.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        holdBody.localScale = Vector3.one;
        var sprite = holdBody.GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.drawMode = SpriteDrawMode.Tiled;
            sprite.size = new Vector2(direction.magnitude, bodyHeight);
        }
    }

    // Retain the API for other callers, but never bend the body along intermediate samples.
    public void SetHoldPath(Vector3[] points)
    {
        if (points != null && points.Length >= 2)
            SetHoldPoints(points[0], points[points.Length - 1]);
    }

    public Vector3 GetPoint(float progress)
    {
        if (hasEndpoints)
            return transform.TransformPoint(Vector3.Lerp(localStart, localEnd, progress));
        return startCircle != null && endCircle != null
            ? Vector3.Lerp(startCircle.position, endCircle.position, progress) : transform.position;
    }
}
