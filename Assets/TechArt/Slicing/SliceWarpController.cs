using UnityEngine;

[ExecuteAlways]
public class SliceWarpController : MonoBehaviour
{
    [Header("Material used by SliceWarpFeature")]
    public Material material;

    [Header("Planes (world-space). Each plane uses Transform position + Transform up as normal.")]
    public Transform[] planes;

    [Header("Per-region params (0..7). Region id is bitmask of plane sides, folded to 0..7.")]
    public Vector3[] regionPivot = new Vector3[8];
    public float[] regionAngleDegrees = new float[8];
    public Vector3[] regionOffset = new Vector3[8];

    [Header("Global")]
    [Range(0, 1)] public float intensity = 1f;
    [Range(0f, 0.5f)] public float edgeWidth = 0.03f;
    [Range(0, 1)] public float edgeDarken = 0.35f;

    static readonly int PlanePointId = Shader.PropertyToID("_PlanePoint");
    static readonly int PlaneNormalId = Shader.PropertyToID("_PlaneNormal");
    static readonly int PlaneCountId = Shader.PropertyToID("_PlaneCount");
    static readonly int RegionPivotAngleId = Shader.PropertyToID("_RegionPivotAngle");
    static readonly int RegionOffsetId = Shader.PropertyToID("_RegionOffset");
    static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
    static readonly int EdgeDarkenId = Shader.PropertyToID("_EdgeDarken");

    Vector4[] _planePoints = new Vector4[8];
    Vector4[] _planeNormals = new Vector4[8];
    Vector4[] _regionPivotAngle = new Vector4[8];
    Vector4[] _regionOffset = new Vector4[8];

    void OnEnable()
    {
        Apply();
    }

    void Update()
    {
        Apply();
    }

    void Apply()
    {
        if (material == null)
            return;

        int count = planes != null ? Mathf.Min(planes.Length, 8) : 0;

        for (int i = 0; i < 8; i++)
        {
            if (i < count && planes[i] != null)
            {
                Vector3 p = planes[i].position;
                Vector3 n = planes[i].up.normalized; // use Up as plane normal

                _planePoints[i] = new Vector4(p.x, p.y, p.z, 0);
                _planeNormals[i] = new Vector4(n.x, n.y, n.z, 0);
            }
            else
            {
                _planePoints[i] = Vector4.zero;
                _planeNormals[i] = new Vector4(0, 1, 0, 0);
            }
        }

        for (int i = 0; i < 8; i++)
        {
            Vector3 pivot = (regionPivot != null && i < regionPivot.Length) ? regionPivot[i] : Vector3.zero;
            float angDeg = (regionAngleDegrees != null && i < regionAngleDegrees.Length) ? regionAngleDegrees[i] : 0f;
            float angRad = angDeg * Mathf.Deg2Rad;

            Vector3 off = (regionOffset != null && i < regionOffset.Length) ? regionOffset[i] : Vector3.zero;

            _regionPivotAngle[i] = new Vector4(pivot.x, pivot.y, pivot.z, angRad);
            _regionOffset[i] = new Vector4(off.x, off.y, off.z, 0);
        }

        material.SetInt(PlaneCountId, count);
        material.SetVectorArray(PlanePointId, _planePoints);
        material.SetVectorArray(PlaneNormalId, _planeNormals);
        material.SetVectorArray(RegionPivotAngleId, _regionPivotAngle);
        material.SetVectorArray(RegionOffsetId, _regionOffset);

        material.SetFloat(IntensityId, intensity);
        material.SetFloat(EdgeWidthId, edgeWidth);
        material.SetFloat(EdgeDarkenId, edgeDarken);
    }
}
