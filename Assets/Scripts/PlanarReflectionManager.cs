using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class PlanarReflectionManager : MonoBehaviour
{
    Camera m_ReflectionCamera;
    Camera m_MainCamera;

    public GameObject m_ReflectionPlane;

    RenderTexture m_RenderTarget;
    public float distance;

    public Vector4 plane;
    public Vector2 plane2;

    public float offsetY;
    public float nearOffset;
     
    void Start()
    {
        GameObject rgo = new GameObject("ReflectionCamera");
        m_ReflectionCamera = rgo.AddComponent<Camera>();
        //m_ReflectionCamera.depthTextureMode = DepthTextureMode.None;
        //StartCoroutine(DisableDepth(rgo));
        m_ReflectionCamera.GetUniversalAdditionalCameraData().requiresDepthTexture = false;
        m_ReflectionCamera.enabled = false;
        m_ReflectionCamera.depth = 0;

        m_MainCamera = Camera.main;
        m_RenderTarget = new RenderTexture(Screen.width, Screen.height, 24);
        //m_RenderTarget.antiAliasing = 0;
        m_RenderTarget.filterMode = FilterMode.Point;
        m_RenderTarget.Create();

        m_ReflectionPlane.GetComponent<MeshRenderer>().material.SetTexture("_Reflection", m_RenderTarget);
        //GameObject.Find("RawImage").GetComponent<RawImage>().texture = m_RenderTarget;
    }

    void Update() => RenderReflection();

    void CalculateObliqueMatrixOrtho(ref Matrix4x4 projection, Vector4 clipPlane)
    {
        Vector4 q;
        q = projection.inverse * new Vector4(
            Mathf.Sign(clipPlane.x),
            Mathf.Sign(clipPlane.y),
            1.0f,
            1.0f
        );
        Vector4 c = clipPlane * (2.0F / (Vector4.Dot(clipPlane, q)));
        projection[2] = c.x;
        projection[6] = c.y;
        projection[10] = c.z;
        projection[14] = c.w - 1.0F;
    }

    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * offsetY;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    void RenderReflection()
    {
        var bgColor = m_ReflectionCamera.backgroundColor;
        m_ReflectionCamera.CopyFrom(m_MainCamera);
        m_ReflectionCamera.backgroundColor = bgColor;
        

        //m_ReflectionCamera.clearFlags = CameraClearFlags.Color;
        m_ReflectionCamera.nearClipPlane -= 10f;

        Vector3 cameraDirectionWorldSpace = m_MainCamera.transform.forward;
        Vector3 cameraUpWorldSpace = m_MainCamera.transform.up;
        Vector3 cameraPositionWorldSpace = m_MainCamera.transform.position;

        //convert to floor space
        Vector3 cameraDirectionPlaneSpace = m_ReflectionPlane.transform.InverseTransformDirection(cameraDirectionWorldSpace);
        Vector3 cameraUpPlaneSpace = m_ReflectionPlane.transform.InverseTransformDirection(cameraUpWorldSpace);
        Vector3 cameraPositionPlaneSpace = m_ReflectionPlane.transform.InverseTransformPoint(cameraPositionWorldSpace);

        //mirror
        cameraDirectionPlaneSpace.y *= -1f;
        cameraUpPlaneSpace.y *= -1f;
        cameraPositionPlaneSpace.y *= -1f;

        //back to world space
        cameraDirectionWorldSpace = m_ReflectionPlane.transform.TransformDirection(cameraDirectionPlaneSpace);
        cameraUpWorldSpace = m_ReflectionPlane.transform.TransformDirection(cameraUpPlaneSpace);
        cameraPositionWorldSpace = m_ReflectionPlane.transform.TransformPoint(cameraPositionPlaneSpace);

        //set position and rotation
        m_ReflectionCamera.transform.position = cameraPositionWorldSpace;
        
        m_ReflectionCamera.transform.LookAt(cameraPositionWorldSpace + cameraDirectionWorldSpace, cameraUpWorldSpace);
        float n = (m_ReflectionPlane.transform.position.y - m_ReflectionCamera.transform.position.y) / m_ReflectionCamera.transform.forward.y;
        m_ReflectionCamera.transform.position += n * m_ReflectionCamera.transform.forward;
        //p1 = cameraPositionPlaneSpace;
        //p2 = m_ReflectionCamera.transform.position;
        //m_ReflectionCamera.transform.position += offset;

        m_ReflectionCamera.targetTexture = m_RenderTarget;

        var projection = m_ReflectionCamera.projectionMatrix;
        var clipPlane = CameraSpacePlane(m_ReflectionCamera, m_ReflectionCamera.transform.position, Vector3.up, 1.0f);
        CalculateObliqueMatrixOrtho(ref projection, clipPlane);
        m_ReflectionCamera.projectionMatrix = projection;

        //Debug.DrawRay(m_ReflectionCamera.transform.position, m_ReflectionCamera.transform.forward * 100f);

        //render
        m_ReflectionCamera.Render();
    }
}
