using UnityEngine;

public class scr_FR5Kinematics : scr_BaseKinematics
{
    [Header("FR5 Parameters (mm)")]
    [SerializeField] private float d1 = 152f;
    [SerializeField] private float a2 = -425f;
    [SerializeField] private float a3 = -395f;
    [SerializeField] private float d4 = 102f;
    [SerializeField] private float d5 = 102f;
    [SerializeField] private float d6 = 100f;

    [Header("Alpha (deg)")]
    [SerializeField] private float alpha1 = 90f;
    [SerializeField] private float alpha4 = 90f;
    [SerializeField] private float alpha5 = -90f;

    [Header("Unit")]
    [SerializeField] private bool convertMmToMeter = true;

    private static readonly string[] StructureNames =
    {
        "BASE",
        "JOINT1",
        "D1",
        "ALPHA1",
        "JOINT2",
        "A2",
        "JOINT3",
        "A3",
        "JOINT4",
        "D4",
        "ALPHA4",
        "JOINT5",
        "D5",
        "ALPHA5",
        "JOINT6",
        "D6",
        "TCP"
    };

    public override RobotPoseResult Calculate(
        float j1,
        float j2,
        float j3,
        float j4,
        float j5,
        float j6)
    {
        float scale = convertMmToMeter ? 0.001f : 1f;

        Matrix4x4 T = Matrix4x4.identity;

        Matrix4x4[] structureTransforms = new Matrix4x4[StructureNames.Length];
        Vector3[] structurePositions = new Vector3[StructureNames.Length];
        Vector3[] structureRotations = new Vector3[StructureNames.Length];

        StoreNode(0, T, structureTransforms, structurePositions, structureRotations);

        T = T * BuildRotZ(j1);
        StoreNode(1, T, structureTransforms, structurePositions, structureRotations);

        T = T * BuildTranslation(0f, 0f, d1 * scale);
        StoreNode(2, T, structureTransforms, structurePositions, structureRotations);

        T = T * BuildRotX(alpha1);
        StoreNode(3, T, structureTransforms, structurePositions, structureRotations);

        T = T * BuildRotZ(j2);
        StoreNode(4, T, structureTransforms, structurePositions, structureRotations);

        T = T * BuildTranslation(a2 * scale, 0f, 0f);
        StoreNode(5, T, structureTransforms, structurePositions, structureRotations);

        T = T * BuildRotZ(j3);
        StoreNode(6, T, structureTransforms, structurePositions, structureRotations);

        T = T * BuildTranslation(a3 * scale, 0f, 0f);
        StoreNode(7, T, structureTransforms, structurePositions, structureRotations);

        T = T * BuildRotZ(j4);
        StoreNode(8, T, structureTransforms, structurePositions, structureRotations);

        T = T * BuildTranslation(0f, 0f, d4 * scale);
        StoreNode(9, T, structureTransforms, structurePositions, structureRotations);

        T = T * BuildRotX(alpha4);
        StoreNode(10, T, structureTransforms, structurePositions, structureRotations);

        T = T * BuildRotZ(j5);
        StoreNode(11, T, structureTransforms, structurePositions, structureRotations);

        T = T * BuildTranslation(0f, 0f, d5 * scale);
        StoreNode(12, T, structureTransforms, structurePositions, structureRotations);

        T = T * BuildRotX(alpha5);
        StoreNode(13, T, structureTransforms, structurePositions, structureRotations);

        T = T * BuildRotZ(j6);
        StoreNode(14, T, structureTransforms, structurePositions, structureRotations);

        T = T * BuildTranslation(0f, 0f, d6 * scale);
        StoreNode(15, T, structureTransforms, structurePositions, structureRotations);

        StoreNode(16, T, structureTransforms, structurePositions, structureRotations);

        // Raw frame -> Unity frame º¯È¯
        for (int i = 0; i < structurePositions.Length; i++)
        {
            structurePositions[i] = scr_FR5CoordinateMapper.ConvertFrPositionMetersToUnity(
                structurePositions[i]
            );

            Quaternion rawRotation = ExtractRotation(structureTransforms[i]);

            structureRotations[i] = scr_FR5CoordinateMapper.ConvertFrBasisQuaternionToUnityEuler(
                rawRotation
            );
        }

        Vector3[] jointPositions = new Vector3[6]
        {
            structurePositions[1],
            structurePositions[4],
            structurePositions[6],
            structurePositions[8],
            structurePositions[11],
            structurePositions[14]
        };

        Vector3[] jointRotations = new Vector3[6]
        {
            structureRotations[1],
            structureRotations[4],
            structureRotations[6],
            structureRotations[8],
            structureRotations[11],
            structureRotations[14]
        };

        Matrix4x4 unityTcpTransform = Matrix4x4.TRS(
            structurePositions[16],
            Quaternion.Euler(structureRotations[16]),
            Vector3.one
        );

        return new RobotPoseResult(
            structurePositions[16],
            structureRotations[16],
            unityTcpTransform,
            jointPositions,
            jointRotations,
            (string[])StructureNames.Clone(),
            structurePositions,
            structureRotations
        );
    }

    private void StoreNode(
        int index,
        Matrix4x4 T,
        Matrix4x4[] structureTransforms,
        Vector3[] structurePositions,
        Vector3[] structureRotations)
    {
        structureTransforms[index] = T;
        structurePositions[index] = ExtractPosition(T);
        structureRotations[index] = ExtractRotation(T).eulerAngles;
    }

    private Matrix4x4 BuildTranslation(float x, float y, float z)
    {
        Matrix4x4 m = Matrix4x4.identity;
        m.m03 = x;
        m.m13 = y;
        m.m23 = z;
        return m;
    }

    private Matrix4x4 BuildRotX(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);

        Matrix4x4 m = Matrix4x4.identity;
        m.m11 = c;
        m.m12 = -s;
        m.m21 = s;
        m.m22 = c;
        return m;
    }

    private Matrix4x4 BuildRotZ(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);

        Matrix4x4 m = Matrix4x4.identity;
        m.m00 = c;
        m.m01 = -s;
        m.m10 = s;
        m.m11 = c;
        return m;
    }

    private Vector3 ExtractPosition(Matrix4x4 m)
    {
        return new Vector3(m.m03, m.m13, m.m23);
    }

    private Quaternion ExtractRotation(Matrix4x4 m)
    {
        Vector3 forward = new Vector3(m.m02, m.m12, m.m22);
        Vector3 up = new Vector3(m.m01, m.m11, m.m21);

        if (forward.sqrMagnitude < 1e-8f || up.sqrMagnitude < 1e-8f)
        {
            return Quaternion.identity;
        }

        return Quaternion.LookRotation(forward.normalized, up.normalized);
    }
}