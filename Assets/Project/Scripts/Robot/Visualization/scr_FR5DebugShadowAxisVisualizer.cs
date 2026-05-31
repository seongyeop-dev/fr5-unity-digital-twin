using System.Collections.Generic;
using UnityEngine;

public class scr_FR5DebugShadowAxisVisualizer : MonoBehaviour
{
    [System.Serializable]
    public class AxisTarget
    {
        public string name;
        public Transform target;
    }

    [Header("Targets")]
    public List<AxisTarget> axisTargets = new List<AxisTarget>();

    [Header("Axis Settings")]
    public float axisLength = 0.08f;
    public float axisWidth = 0.004f;
    public bool useWorldSpace = true;
    public bool showAxes = true;
    public bool createOnAwake = true;

    private List<LineRenderer[]> axisLines = new List<LineRenderer[]>();

    void Awake()
    {
        if (createOnAwake)
        {
            CreateAxes();
        }
    }

    void CreateAxes()
    {
        axisLines.Clear();

        foreach (var axisTarget in axisTargets)
        {
            if (axisTarget == null || axisTarget.target == null)
                continue;

            LineRenderer[] lines = new LineRenderer[3];

            lines[0] = CreateAxisLine("X", Color.red);
            lines[1] = CreateAxisLine("Y", Color.green);
            lines[2] = CreateAxisLine("Z", Color.blue);

            axisLines.Add(lines);
        }
    }

    LineRenderer CreateAxisLine(string name, Color color)
    {
        GameObject go = new GameObject("Axis_" + name);
        go.transform.SetParent(transform);

        LineRenderer lr = go.AddComponent<LineRenderer>();

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.material.color = color;

        lr.startColor = color;
        lr.endColor = color;

        lr.startWidth = axisWidth;
        lr.endWidth = axisWidth;

        lr.useWorldSpace = useWorldSpace;

        lr.positionCount = 2;

        return lr;
    }

    void Update()
    {
        if (!showAxes) return;

        for (int i = 0; i < axisTargets.Count; i++)
        {
            if (axisTargets[i].target == null) continue;

            Transform t = axisTargets[i].target;

            LineRenderer[] lines = axisLines[i];

            Vector3 pos = t.position;

            Vector3 x = t.right * axisLength;
            Vector3 y = t.up * axisLength;
            Vector3 z = t.forward * axisLength;

            lines[0].SetPosition(0, pos);
            lines[0].SetPosition(1, pos + x);

            lines[1].SetPosition(0, pos);
            lines[1].SetPosition(1, pos + y);

            lines[2].SetPosition(0, pos);
            lines[2].SetPosition(1, pos + z);
        }
    }
}