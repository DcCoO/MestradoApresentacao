using UnityEditor;
using UnityEngine;

public class Helper : MonoBehaviour
{
    [MenuItem("Assets/Create/Half-Sized Quad")]
    public static void CreateHalfQuad()
    {
        // Cria um novo GameObject vazio
        GameObject quad = new GameObject("Half-Quad");

        // Adiciona um MeshFilter e MeshRenderer ao objeto
        MeshFilter meshFilter = quad.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = quad.AddComponent<MeshRenderer>();

        // Cria o mesh do quad com a metade do tamanho
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(-0.25f, -0.25f, 0f);
        vertices[1] = new Vector3(-0.25f, 0.25f, 0f);
        vertices[2] = new Vector3(0.25f, 0.25f, 0f);
        vertices[3] = new Vector3(0.25f, -0.25f, 0f);
        mesh.vertices = vertices;
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        mesh.RecalculateNormals();

        // Define o mesh no MeshFilter
        meshFilter.sharedMesh = mesh;

        // Cria o objeto do quad como um recurso do Unity e salva como um arquivo .asset
        AssetDatabase.CreateAsset(mesh, "Assets/HalfQuad.asset");
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Assets/Create/Third-Sized Quad")]
    public static void CreateThirdQuad()
    {
        // Cria um novo GameObject vazio
        GameObject quad = new GameObject("Third-Quad");

        // Adiciona um MeshFilter e MeshRenderer ao objeto
        MeshFilter meshFilter = quad.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = quad.AddComponent<MeshRenderer>();

        // Cria o mesh do quad com a metade do tamanho
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(-0.1666f, -0.1666f, 0f);
        vertices[1] = new Vector3(-0.1666f, 0.1666f, 0f);
        vertices[2] = new Vector3(0.1666f, 0.1666f, 0f);
        vertices[3] = new Vector3(0.1666f, -0.1666f, 0f);
        mesh.vertices = vertices;
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        mesh.RecalculateNormals();

        // Define o mesh no MeshFilter
        meshFilter.sharedMesh = mesh;

        // Cria o objeto do quad como um recurso do Unity e salva como um arquivo .asset
        AssetDatabase.CreateAsset(mesh, "Assets/ThirdQuad.asset");
        AssetDatabase.SaveAssets();
    }
}
