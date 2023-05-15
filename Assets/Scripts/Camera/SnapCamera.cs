using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapCamera : SingletonMonoBehaviour<SnapCamera>
{
    public Vector3 currentPos;
    public float speed;
    public float rotationSpeed;
    Camera cam;

    static Vector3 axisSize = Vector3.zero;
    static Vector3 sqrSize = Vector3.zero;
    static Vector3 forward, right, up;

    bool isRotating;
    public float rotationDuration;
    public AnimationCurve rotationCurve;

    public bool snapping;

    void Start()
    {
        cam = Camera.main;
        currentPos = transform.position;
        axisSize.x = (2f * cam.orthographicSize) / (float)Screen.height;
        axisSize.y = axisSize.x / Mathf.Cos(cam.transform.localEulerAngles.x * Mathf.Deg2Rad);
        axisSize.z = axisSize.x / Mathf.Sin(cam.transform.localEulerAngles.x * Mathf.Deg2Rad);
        //sqrSize = new Vector3(axisSize.x * axisSize.x, axisSize.y * axisSize.y, axisSize.z * axisSize.z);
        sqrSize = Vector3.Scale(axisSize, axisSize);
    }

    // Update is called once per frame
    void Update()
    {
        bool wasSnapping = snapping;

        right = transform.right * axisSize.x;
        up = transform.up * axisSize.y;
        forward = transform.forward * axisSize.z;

        currentPos += Time.deltaTime * speed *
                (Input.GetAxisRaw("Horizontal") * new Vector3(cam.transform.right.x, 0, cam.transform.right.z)
                + Input.GetAxisRaw("Vertical") * new Vector3(cam.transform.forward.x, 0, cam.transform.forward.z)).normalized;

        if (Input.GetKey(KeyCode.Y)) currentPos += Vector3.up * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.H)) currentPos -= Vector3.up * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.Q)) Rotate(true);
        if (Input.GetKey(KeyCode.E)) Rotate(false);

        if (Input.GetKey(KeyCode.Z))
        {
            transform.localEulerAngles += rotationSpeed * Vector3.up * Time.deltaTime;
            snapping = false;
        }
        if (Input.GetKey(KeyCode.C))
        {
            transform.localEulerAngles -= rotationSpeed * Vector3.up * Time.deltaTime;
            snapping = false;
        }

        if (snapping && !isRotating) transform.position = GetSnappedPosition(currentPos);
        else transform.position = currentPos;

        snapping = wasSnapping;

    }

    public void Rotate(bool clockwise)
    {
        if (isRotating) return;
        isRotating = true;
        StartCoroutine(RotateRoutine(clockwise));
    }

    IEnumerator RotateRoutine(bool clockwise)
    {
        bool wasSnapping = snapping;
        snapping = false;
        Vector3 localEulerAngles = transform.localEulerAngles;
        float yf = localEulerAngles.y + (clockwise ? 45 : -45);
        for (float t = 0; t < 1; t += Time.deltaTime / rotationDuration)
        {
            transform.localEulerAngles = new Vector3(localEulerAngles.x, Mathf.Lerp(localEulerAngles.y, yf, rotationCurve.Evaluate(t)), localEulerAngles.z);
            yield return null;
        }

        transform.localEulerAngles = new Vector3(localEulerAngles.x, yf, localEulerAngles.z);
        snapping = wasSnapping;
        isRotating = false;
    }

    public static Vector3 GetSnappedPosition(Vector3 position)
    {
        float x = Vector3.Dot(right, position) / sqrSize.x;
        float y = Vector3.Dot(up, position) / sqrSize.y;
        float z = Vector3.Dot(forward, position) / sqrSize.z;

        return Mathf.Round(x) * right + Mathf.Round(y) * up + Mathf.Round(z) * forward;

        /*return right * (x - xRemainder + (xRemainder > unitSize.x / 2f ? unitSize.x : 0)) +
               up * (y - yRemainder + (yRemainder > unitSize.y / 2f ? unitSize.y : 0)) +
               forward * (z - zRemainder + (zRemainder > unitSize.z / 2f ? unitSize.z : 0));*/

    }

    public float GetAngle()
    {
        float yLocalEulerAngle = transform.localEulerAngles.y;
        float result = yLocalEulerAngle - Mathf.CeilToInt(yLocalEulerAngle / 360f) * 360f;
        if (result < 0) result += 360f;
        return result;
    }
}