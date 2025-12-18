using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static System.Net.Mime.MediaTypeNames;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
public class PlanarReflection : MonoBehaviour
{
    [Header("Reflection Settings")]
    [SerializeField] private LayerMask reflectionLayers = -1;
    [SerializeField] private int textureSize = 2048;
    [SerializeField] private float clipPlaneOffset = 0.07f;

    [Header("Rendering")]
    [SerializeField] private bool useOcclusionCulling = true;
    [SerializeField] private float farClipPlane = 1000f;
    [SerializeField] private Color clearColor = new Color(0.125f, 0.125f, 0.125f, 1f);

    private Camera reflectionCamera;
    private RenderTexture reflectionTexture;
    private Material reflectionMaterial;
    private static bool isRendering = false;
    private int reflectionTextureId = Shader.PropertyToID("_ReflectionTex");

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        InitializeReflection();
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        CleanupResources();
    }

    private void OnValidate()
    {
        textureSize = Mathf.ClosestPowerOfTwo(textureSize);
        textureSize = Mathf.Clamp(textureSize, 64, 2048);
    }

    private void InitializeReflection()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            // Use material instance, not sharedMaterial, so we can modify it at runtime
            reflectionMaterial = UnityEngine.Application.isPlaying ? meshRenderer.material : meshRenderer.sharedMaterial;
        }
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        // Avoid recursive rendering
        if (isRendering || camera == reflectionCamera)
            return;

        // Only render for game and scene cameras
        if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView)
            return;

        RenderReflection(context, camera);
    }

    private void RenderReflection(ScriptableRenderContext context, Camera currentCamera)
    {
        isRendering = true;

        try
        {
            // Create or update reflection camera
            if (reflectionCamera == null)
            {
                GameObject cameraObject = new GameObject("Reflection Camera",
                    typeof(Camera), typeof(UniversalAdditionalCameraData));
                reflectionCamera = cameraObject.GetComponent<Camera>();
                reflectionCamera.enabled = false;
                cameraObject.hideFlags = HideFlags.HideAndDontSave;

                // Configure URP camera data
                UniversalAdditionalCameraData cameraData =
                    reflectionCamera.GetComponent<UniversalAdditionalCameraData>();
                cameraData.renderShadows = false;
                cameraData.requiresColorOption = CameraOverrideOption.Off;
                cameraData.requiresDepthOption = CameraOverrideOption.Off;
            }

            // Create or update render texture
            UpdateRenderTexture();

            // Copy camera settings
            CopyCameraProperties(currentCamera, reflectionCamera);

            // Calculate reflection matrix
            Vector3 pos = transform.position;
            Vector3 normal = transform.up;
            float d = -Vector3.Dot(normal, pos) - clipPlaneOffset;
            Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

            Matrix4x4 reflection = CalculateReflectionMatrix(reflectionPlane);

            // Position reflection camera
            Vector3 oldPos = currentCamera.transform.position;
            Vector3 newPos = reflection.MultiplyPoint(oldPos);
            reflectionCamera.worldToCameraMatrix = currentCamera.worldToCameraMatrix * reflection;

            // Setup oblique projection matrix for clipping
            Vector4 clipPlane = CameraSpacePlane(reflectionCamera, pos, normal, 1.0f);
            reflectionCamera.projectionMatrix =
                currentCamera.CalculateObliqueMatrix(clipPlane);

            // Invert culling
            GL.invertCulling = true;

            // Render using new URP API
            reflectionCamera.targetTexture = reflectionTexture;

            // Use the new rendering API
            var renderRequest = new UniversalRenderPipeline.SingleCameraRequest
            {
                destination = reflectionTexture
            };
            RenderPipeline.SubmitRenderRequest(reflectionCamera, renderRequest);

            // Restore culling
            GL.invertCulling = false;

            // Assign texture to material
            if (reflectionMaterial != null)
            {
                reflectionMaterial.SetTexture(reflectionTextureId, reflectionTexture);
            }
            else
            {
                // Try to get material again if it's null
                MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    Material mat = UnityEngine.Application.isPlaying ? meshRenderer.material : meshRenderer.sharedMaterial;
                    if (mat != null)
                    {
                        mat.SetTexture(reflectionTextureId, reflectionTexture);
                        reflectionMaterial = mat;
                    }
                }
            }
        }
        finally
        {
            isRendering = false;
        }
    }

    private void UpdateRenderTexture()
    {
        if (reflectionTexture == null ||
            reflectionTexture.width != textureSize ||
            reflectionTexture.height != textureSize)
        {
            if (reflectionTexture != null)
            {
                reflectionTexture.Release();
                DestroyImmediate(reflectionTexture);
            }

            reflectionTexture = new RenderTexture(textureSize, textureSize, 16,
                RenderTextureFormat.DefaultHDR)
            {
                name = "Planar Reflection",
                hideFlags = HideFlags.DontSave,
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = true,
                autoGenerateMips = true
            };
        }
    }

    private void CopyCameraProperties(Camera src, Camera dest)
    {
        if (dest == null)
            return;

        dest.clearFlags = src.clearFlags;
        dest.backgroundColor = clearColor;
        dest.farClipPlane = Mathf.Min(src.farClipPlane, farClipPlane);
        dest.nearClipPlane = src.nearClipPlane;
        dest.orthographic = src.orthographic;
        dest.fieldOfView = src.fieldOfView;
        dest.aspect = src.aspect;
        dest.orthographicSize = src.orthographicSize;
        dest.cullingMask = reflectionLayers;
        dest.useOcclusionCulling = useOcclusionCulling;
    }

    private Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
    {
        Matrix4x4 reflectionMat = Matrix4x4.identity;

        reflectionMat.m00 = 1f - 2f * plane[0] * plane[0];
        reflectionMat.m01 = -2f * plane[0] * plane[1];
        reflectionMat.m02 = -2f * plane[0] * plane[2];
        reflectionMat.m03 = -2f * plane[3] * plane[0];

        reflectionMat.m10 = -2f * plane[1] * plane[0];
        reflectionMat.m11 = 1f - 2f * plane[1] * plane[1];
        reflectionMat.m12 = -2f * plane[1] * plane[2];
        reflectionMat.m13 = -2f * plane[3] * plane[1];

        reflectionMat.m20 = -2f * plane[2] * plane[0];
        reflectionMat.m21 = -2f * plane[2] * plane[1];
        reflectionMat.m22 = 1f - 2f * plane[2] * plane[2];
        reflectionMat.m23 = -2f * plane[3] * plane[2];

        reflectionMat.m30 = 0f;
        reflectionMat.m31 = 0f;
        reflectionMat.m32 = 0f;
        reflectionMat.m33 = 1f;

        return reflectionMat;
    }

    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * clipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    private void CleanupResources()
    {
        if (reflectionCamera != null)
        {
            if (reflectionCamera.targetTexture != null)
            {
                reflectionCamera.targetTexture.Release();
            }
            DestroyImmediate(reflectionCamera.gameObject);
        }

        if (reflectionTexture != null)
        {
            reflectionTexture.Release();
            DestroyImmediate(reflectionTexture);
        }
    }

    private void OnDestroy()
    {
        CleanupResources();
    }
}
