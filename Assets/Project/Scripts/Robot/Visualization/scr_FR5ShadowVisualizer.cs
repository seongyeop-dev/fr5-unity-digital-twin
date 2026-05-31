using UnityEngine;

/// <summary>
/// FR5 Shadow Visualizer
///
/// 역할:
/// - 최신 Shadow 구조 노드 전체 연결선 표시
/// - 디지털트윈 수학 구조 확인용
/// </summary>
public class scr_FR5ShadowVisualizer : MonoBehaviour
{
    [Header("Shadow Structure Nodes")]
    [SerializeField] private Transform baseShadow;
    [SerializeField] private Transform joint1Shadow;
    [SerializeField] private Transform d1Shadow;
    [SerializeField] private Transform alpha1Shadow;
    [SerializeField] private Transform joint2Shadow;
    [SerializeField] private Transform a2Shadow;
    [SerializeField] private Transform joint3Shadow;
    [SerializeField] private Transform a3Shadow;
    [SerializeField] private Transform joint4Shadow;
    [SerializeField] private Transform d4Shadow;
    [SerializeField] private Transform alpha4Shadow;
    [SerializeField] private Transform joint5Shadow;
    [SerializeField] private Transform d5Shadow;
    [SerializeField] private Transform alpha5Shadow;
    [SerializeField] private Transform joint6Shadow;
    [SerializeField] private Transform d6Shadow;
    [SerializeField] private Transform tcpShadow;

    [Header("Line Settings")]
    [SerializeField] private Color lineColor = Color.green;
    [SerializeField] private float lineWidth = 0.01f;

    private Transform[] chain;
    private LineRenderer[] lines;

    private void Start()
    {
        chain = new Transform[]
        {
            baseShadow,
            joint1Shadow,
            d1Shadow,
            alpha1Shadow,
            joint2Shadow,
            a2Shadow,
            joint3Shadow,
            a3Shadow,
            joint4Shadow,
            d4Shadow,
            alpha4Shadow,
            joint5Shadow,
            d5Shadow,
            alpha5Shadow,
            joint6Shadow,
            d6Shadow,
            tcpShadow
        };

        CreateLines();
    }

    private void Update()
    {
        UpdateLines();
    }

    private void CreateLines()
    {
        if (chain == null || chain.Length < 2)
        {
            return;
        }

        lines = new LineRenderer[chain.Length - 1];

        for (int i = 0; i < lines.Length; i++)
        {
            GameObject go = new GameObject("ShadowLine_" + i);
            go.transform.SetParent(transform);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            lr.positionCount = 2;

            lines[i] = lr;
        }
    }

    private void UpdateLines()
    {
        if (!Validate())
        {
            return;
        }

        for (int i = 0; i < chain.Length - 1; i++)
        {
            SetLine(lines[i], chain[i], chain[i + 1]);
        }
    }

    private void SetLine(LineRenderer lr, Transform a, Transform b)
    {
        if (lr == null || a == null || b == null)
        {
            return;
        }

        lr.SetPosition(0, a.position);
        lr.SetPosition(1, b.position);
    }

    private bool Validate()
    {
        if (chain == null || lines == null)
        {
            return false;
        }

        if (lines.Length != chain.Length - 1)
        {
            return false;
        }

        for (int i = 0; i < chain.Length; i++)
        {
            if (chain[i] == null)
            {
                return false;
            }
        }

        return true;
    }
}