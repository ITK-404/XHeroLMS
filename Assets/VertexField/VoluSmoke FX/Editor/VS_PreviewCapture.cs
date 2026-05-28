using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace VertexField.VoluSmokeFX
{
    public static class VoluSmokePreviewCapture
    {
        public static Texture2D CapturePreview(VoluSmokeMeshGenerator generator, int resolution = 256)
        {
            if (generator == null)
                return CreatePlaceholderPreview(resolution);


            int originalLayer = generator.gameObject.layer;


            List<GameObject> hiddenObjects = new List<GameObject>();


            int previewLayer = 31;


            GameObject tempCameraObj = new GameObject("TempPreviewCamera");
            Camera previewCamera = tempCameraObj.AddComponent<Camera>();


            GameObject tempLightObj = new GameObject("TempPreviewLight");
            Light previewLight = tempLightObj.AddComponent<Light>();

            try
            {

                GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (GameObject obj in allObjects)
                {
                    if (obj != generator.gameObject &&
                        obj != tempCameraObj &&
                        obj != tempLightObj &&
                        obj.activeInHierarchy)
                    {
                        obj.SetActive(false);
                        hiddenObjects.Add(obj);
                    }
                }


                generator.gameObject.SetActive(true);
                generator.gameObject.layer = previewLayer;


                MeshRenderer meshRenderer = generator.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    return CreatePlaceholderPreview(resolution);
                }


                Bounds bounds = meshRenderer.bounds;
                float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                if (maxSize <= Mathf.Epsilon)
                {
                    return CreatePlaceholderPreview(resolution);
                }
                Vector3 boundsCenter = bounds.center;


                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = new Color(0.1f, 0.1f, 0.12f, 1f);
                previewCamera.orthographic = true;
                previewCamera.cullingMask = 1 << previewLayer;


                Vector3 viewDirection = new Vector3(-0.6f, -0.8f, -0.6f).normalized;
                Quaternion cameraRotation = Quaternion.LookRotation(viewDirection, Vector3.up);
                previewCamera.transform.rotation = cameraRotation;

                Vector3 camRight = previewCamera.transform.right;
                Vector3 camUp = previewCamera.transform.up;
                Vector3 camForward = previewCamera.transform.forward;

                float maxRight = 0f;
                float maxUp = 0f;
                float maxForward = 0f;

                MeshFilter meshFilterComponent = generator.GetComponent<MeshFilter>();
                Mesh sampledMesh = meshFilterComponent != null ? meshFilterComponent.sharedMesh : null;
                if (sampledMesh != null && sampledMesh.vertexCount > 0)
                {
                    Vector3[] verts = sampledMesh.vertices;
                    Matrix4x4 localToWorld = generator.transform.localToWorldMatrix;
                    for (int i = 0; i < verts.Length; i++)
                    {
                        Vector3 worldPos = localToWorld.MultiplyPoint3x4(verts[i]);
                        Vector3 offset = worldPos - boundsCenter;
                        maxRight = Mathf.Max(maxRight, Mathf.Abs(Vector3.Dot(offset, camRight)));
                        maxUp = Mathf.Max(maxUp, Mathf.Abs(Vector3.Dot(offset, camUp)));
                        maxForward = Mathf.Max(maxForward, Mathf.Abs(Vector3.Dot(offset, camForward)));
                    }
                }
                else
                {
                    Vector3 extents = bounds.extents;
                    Vector3[] corners = new Vector3[8];
                    int cornerIndex = 0;
                    for (int x = -1; x <= 1; x += 2)
                    {
                        for (int y = -1; y <= 1; y += 2)
                        {
                            for (int z = -1; z <= 1; z += 2)
                            {
                                Vector3 offset = new Vector3(extents.x * x, extents.y * y, extents.z * z);
                                corners[cornerIndex++] = boundsCenter + offset;
                            }
                        }
                    }

                    foreach (Vector3 corner in corners)
                    {
                        Vector3 toCorner = corner - boundsCenter;
                        maxRight = Mathf.Max(maxRight, Mathf.Abs(Vector3.Dot(toCorner, camRight)));
                        maxUp = Mathf.Max(maxUp, Mathf.Abs(Vector3.Dot(toCorner, camUp)));
                        maxForward = Mathf.Max(maxForward, Mathf.Abs(Vector3.Dot(toCorner, camForward)));
                    }
                }

                float dominantHalfExtent = Mathf.Max(maxUp, maxRight);
                float minimalPadding = Mathf.Max(0.001f * maxSize, 0.0005f);
                float boostedOrthoSize = Mathf.Max(0.1f, dominantHalfExtent + minimalPadding);

                float depthPadding = Mathf.Max(0.02f, 0.05f * maxSize);
                float cameraDistance = maxForward + depthPadding;
                Vector3 cameraPosition = boundsCenter - camForward * cameraDistance;

                previewCamera.transform.position = cameraPosition;
                previewCamera.nearClipPlane = Mathf.Max(0.001f, depthPadding * 0.25f);
                previewCamera.farClipPlane = Mathf.Max(previewCamera.nearClipPlane + 1f, 2f * maxForward + depthPadding * 3f);
                previewCamera.orthographicSize = boostedOrthoSize;


                previewLight.type = LightType.Directional;
                previewLight.color = new Color(1f, 0.98f, 0.95f);
                previewLight.intensity = 1.3f;
                previewLight.shadows = LightShadows.None;
                previewLight.cullingMask = 1 << previewLayer;


                tempLightObj.transform.rotation = Quaternion.Euler(45f, 45f, 0f);


                RenderTexture renderTexture = new RenderTexture(resolution, resolution, 24);
                renderTexture.antiAliasing = 4;
                previewCamera.targetTexture = renderTexture;


                previewCamera.Render();


                RenderTexture.active = renderTexture;
                Texture2D preview = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                preview.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
                preview.Apply();


                RenderTexture.active = null;
                previewCamera.targetTexture = null;
                Object.DestroyImmediate(renderTexture);

                return preview;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error capturing voluSmoke preview: {e.Message}");
                return CreatePlaceholderPreview(resolution);
            }
            finally
            {

                generator.gameObject.layer = originalLayer;


                foreach (GameObject obj in hiddenObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                    }
                }


                if (tempCameraObj != null)
                    Object.DestroyImmediate(tempCameraObj);
                if (tempLightObj != null)
                    Object.DestroyImmediate(tempLightObj);


                SceneView.RepaintAll();
            }
        }

        public static Texture2D CreatePlaceholderPreview(int resolution = 256)
        {
            Texture2D preview = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[resolution * resolution];


            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float t = (float)y / resolution;
                    Color skyColor = Color.Lerp(
                        new Color(0.3f, 0.4f, 0.6f),
                        new Color(0.6f, 0.75f, 0.95f),
                        t
                    );


                    float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                    skyColor = Color.Lerp(skyColor, new Color(0.9f, 0.9f, 0.95f), noise * 0.3f);

                    pixels[y * resolution + x] = skyColor;
                }
            }

            preview.SetPixels(pixels);
            preview.Apply();

            return preview;
        }
    }
}
