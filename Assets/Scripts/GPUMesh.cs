using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class GPUMesh : MonoBehaviour
{
    //public static List<GPUMeshObject> objects = new List<GPUMeshObject>();
    public static GPUMesh instance;


    public static Dictionary<GPUMeshType, List<GPUMeshObject>> meshes = new Dictionary<GPUMeshType, List<GPUMeshObject>>();
    public List<GPUMeshLayer> layers = new List<GPUMeshLayer>();

    [System.Serializable]
    public class GPUMeshLayer
    {
        public GPUMeshType layer;
        public bool isInstancing = true;

        [HideInInspector] public ComputeBuffer positionBuffer;
        [HideInInspector] public ComputeBuffer normalBuffer;
        [HideInInspector] public ComputeBuffer indexBuffer;
        [HideInInspector] public ComputeBuffer argsBuffer;
        [HideInInspector] public uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        public Mesh mesh;
        public Material material;
        public Vector3 offset;
        public float normalOffset;
    }
    
    public Vector3 normalDisplacement;


 

    private void Awake()
    {
        //initialize newObjects with an empty list for each mesh type
        //foreach (GPUMeshType type in System.Enum.GetValues(typeof(GPUMeshType))) meshes.Add(type, new List<GPUMeshObject>());        
        instance = this;
    }

    private void OnDestroy() => instance = null;


    void Start()
    {
        foreach (GPUMeshLayer layer in layers)
        {
            layer.argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            UpdateBuffers(layer);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            foreach (GPUMeshLayer layer in layers)
            {
                if (layer.isInstancing) UpdateBuffers(layer);
            }
        }
        foreach (GPUMeshLayer layer in layers)
        {
            if (layer.isInstancing) Graphics.DrawMeshInstancedIndirect(layer.mesh, 0, layer.material, new Bounds(Vector3.zero, new Vector3(1000.0f, 1000.0f, 1000.0f)), layer.argsBuffer);
        }
    }


    void UpdateBuffers(GPUMeshLayer layer)
    {
        if (layer.positionBuffer != null) layer.positionBuffer.Release();
        if (layer.normalBuffer != null) layer.normalBuffer.Release();
        if (layer.indexBuffer != null) layer.indexBuffer.Release();

        List<Vector3> positions = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<float> indices = new List<float>();

        var objects = meshes[layer.layer];

        for (int i = 0; i < objects.Count; ++i)
        {
            Info info = objects[i].GetObjectInfo(ref layer);
            positions.AddRange(info.positions);
            normals.AddRange(info.normals);
            indices.AddRange(info.indices);
        }

        int n = positions.Count;

        layer.positionBuffer = new ComputeBuffer(n, 12);
        layer.normalBuffer = new ComputeBuffer(n, 12);
        layer.indexBuffer = new ComputeBuffer(n, 4);

        layer.positionBuffer.SetData(positions);
        layer.normalBuffer.SetData(normals);
        layer.indexBuffer.SetData(indices);

        layer.material.SetBuffer("positionBuffer", layer.positionBuffer);
        layer.material.SetBuffer("normalBuffer", layer.normalBuffer);
        layer.material.SetBuffer("indexBuffer", layer.indexBuffer);

        //print($"{n} {positions.Count} {normals.Count} {indices.Count}");

        // Indirect args
        if (layer.mesh != null)
        {
            layer.args = new uint[] { layer.mesh.GetIndexCount(0), (uint) n, layer.mesh.GetIndexStart(0), layer.mesh.GetBaseVertex(0), 0 };
            //layer.args[0] = (uint)layer.mesh.GetIndexCount(0);
            //layer.args[1] = (uint)n;
            //layer.args[2] = (uint)layer.mesh.GetIndexStart(0);
            //layer.args[3] = (uint)layer.mesh.GetBaseVertex(0);
        }
        else
        {
            layer.args[0] = layer.args[1] = layer.args[2] = layer.args[3] = 0;
        }
        layer.argsBuffer.SetData(layer.args);
    }

    void OnDisable()
    {
        foreach (GPUMeshLayer layer in layers)
        {
            if (layer.positionBuffer != null)
                layer.positionBuffer.Release();
            layer.positionBuffer = null;

            if (layer.normalBuffer != null)
                layer.normalBuffer.Release();
            layer.normalBuffer = null;

            if (layer.indexBuffer != null)
                layer.indexBuffer.Release();
            layer.indexBuffer = null;

            if (layer.argsBuffer != null)
                layer.argsBuffer.Release();
            layer.argsBuffer = null;
        }
    }

    public struct Info
    {
        public Vector3[] positions, normals;
        public float[] indices;

        public Info(Vector3[] positions, Vector3[] normals, float[] indices)
        {
            this.positions = positions;
            this.normals = normals;
            this.indices = indices;
        }
    }
}
