using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UILineRenderer : MaskableGraphic
{
    public List<Vector2> points = new List<Vector2>();
    public float lineWidth = 5f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (points.Count < 2)
            return;

        float halfWidth = lineWidth / 2f;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 p1 = points[i];
            Vector2 p2 = points[i + 1];

            Vector2 direction = (p2 - p1).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * halfWidth;

            Vector2 v1 = p1 - normal;
            Vector2 v2 = p1 + normal;
            Vector2 v3 = p2 - normal;
            Vector2 v4 = p2 + normal;

            int index = vh.currentVertCount;

            vh.AddVert(v1, color, Vector2.zero);
            vh.AddVert(v2, color, Vector2.zero);
            vh.AddVert(v3, color, Vector2.zero);
            vh.AddVert(v4, color, Vector2.zero);

            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 1, index + 3, index + 2);
        }
    }

    public void SetPoints(List<Vector2> newPoints)
    {
        points = newPoints;
        SetVerticesDirty();
    }
}