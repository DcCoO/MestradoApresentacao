using UnityEngine;

public class GPUMeshObject : MonoBehaviour
{
    public GPUMeshType meshLayer;
    public MeshFilter meshFilter;
    public int randomSeed;
    public int instancesPerTriangle;
    public float normalOffset;
    public int gridRowCount = 1;
    public bool isFloor;

    Mesh mesh;

    void Awake()
    {
        if(!isFloor) GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        if (GPUMesh.meshes.ContainsKey(meshLayer)) GPUMesh.meshes[meshLayer].Add(this);
        else GPUMesh.meshes[meshLayer] = new System.Collections.Generic.List<GPUMeshObject> { this };
    }

    /*private void Update()
    {
        DrawRays();
    }*/

    public GPUMesh.Info GetObjectInfo()
    {
        var vertices = mesh.vertices;
        var vertexNormals = mesh.normals;
        var triangles = mesh.triangles;

        int n = (triangles.Length / 3) * instancesPerTriangle;

        Vector3[] positions = new Vector3[n];
        Vector3[] normals = new Vector3[n];
        float[] indices = new float[n];

        System.Random random = new System.Random(randomSeed);

        Vector3 normalDisplacement = GPUMesh.instance.normalDisplacement;
        int index = 0;
        Vector3 pos;
        var rot = Quaternion.Euler(meshFilter.transform.eulerAngles);
        for (int i = 0, len = triangles.Length / 3; i < len; ++i)
        {
            for (int j = 0; j < instancesPerTriangle; ++j)
            {
                int triangleIndex = i * 3;

                pos = GetRandomPointInTriangle(ref vertices[triangles[triangleIndex]], ref vertices[triangles[triangleIndex + 1]], ref vertices[triangles[triangleIndex + 2]], ref random);
                //pos = (vertices[triangles[triangleIndex]] + vertices[triangles[triangleIndex + 1]] + vertices[triangles[triangleIndex + 2]]) / 3f;
                normals[index] = rot * (vertexNormals[triangles[triangleIndex]] + vertexNormals[triangles[triangleIndex + 1]] + vertexNormals[triangles[triangleIndex + 2]]) / 3;
                normals[index] += new Vector3(Random.Range(-normalDisplacement.x, normalDisplacement.x), Random.Range(-normalDisplacement.y, normalDisplacement.y), Random.Range(-normalDisplacement.z, normalDisplacement.z));
                positions[index] = rot * Vector3.Scale(pos, meshFilter.transform.lossyScale) + meshFilter.transform.position + normals[index] * normalOffset;
                indices[index++] = random.Next(gridRowCount);
            }
        }

        return new GPUMesh.Info(positions, normals, indices);
    }

    void DrawRays()
    {
        var info = GetObjectInfo();
        for (int i = 0, len = info.positions.Length; i < len; ++i)
            Debug.DrawRay(info.positions[i], info.normals[i] * 0.03f, Color.red);
    }

    Vector3 GetRandomPointInTriangle(ref Vector3 v1, ref Vector3 v2, ref Vector3 v3, ref System.Random random)
    {
        //float a = 0.5f;
        //float b = 0.5f;
        float a = (float)random.NextDouble();
        float b = (float)random.NextDouble();
        float sqrta = Mathf.Sqrt(a);
        return ((1f - sqrta) * v1) + (sqrta * (1f - b) * v2) + (b * sqrta * v3);
    }
}
