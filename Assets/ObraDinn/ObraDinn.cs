using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ObraDinn : MonoBehaviour
{
    [Header("Materials")]
    public Material ditherMat;
    public Material thresholdMat;
    
    [Header("Dither Settings")]
    [Range(0.1f, 4f)]
    public float contrast = 1.5f;
    [Range(0.1f, 2f)]
    public float ditherStrength = 1f;
    [Range(0f, 2f)]
    public float edgeIntensity = 1f;
    [Range(0f, 1f)]
    public float noiseAmount = 0.5f;
    [Range(0f, 1f)]
    public float threshold = 0.1f;
    
    [Header("Outline Settings")]
    [Range(0f, 4f)]
    public float outlineThickness = 1f;
    [Range(0f, 1f)]
    public float outlineThreshold = 0.1f;
    public Color outlineColor = Color.white;
    
    [Header("Color Settings")]
    public Color foregroundColor = Color.white;
    public Color backgroundColor = Color.black;
    [Range(0f, 1f)]
    public float colorBlend = 1f;
    [Range(-0.5f, 0.5f)]
    public float thresholdOffset = 0f;

    private Camera cam;
    private RenderTexture renderBuffer;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (ditherMat != null)
        {
            ditherMat.SetFloat("_Contrast", contrast);
            ditherMat.SetFloat("_DitherStrength", ditherStrength);
            ditherMat.SetFloat("_EdgeIntensity", edgeIntensity);
            ditherMat.SetFloat("_NoiseAmount", noiseAmount);
            ditherMat.SetFloat("_Threshold", threshold);
            
            ditherMat.SetFloat("_OutlineThickness", outlineThickness);
            ditherMat.SetFloat("_OutlineThreshold", outlineThreshold);
            ditherMat.SetColor("_OutlineColor", outlineColor);
        }

        if (thresholdMat != null)
        {
            thresholdMat.SetColor("_FG", foregroundColor);
            thresholdMat.SetColor("_BG", backgroundColor);
            thresholdMat.SetFloat("_ColorBlend", colorBlend);
            thresholdMat.SetFloat("_ThresholdOffset", thresholdOffset);
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (ditherMat == null || thresholdMat == null)
        {
            Graphics.Blit(src, dst);
            return;
        }

        int largeWidth = 1640;
        int largeHeight = 940;
        int mainWidth = 820;
        int mainHeight = 470;

        RenderTexture large = RenderTexture.GetTemporary(largeWidth, largeHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture main = RenderTexture.GetTemporary(mainWidth, mainHeight, 0, RenderTextureFormat.ARGB32);
        
        large.filterMode = FilterMode.Bilinear;
        main.filterMode = FilterMode.Bilinear;

        Vector3[] corners = new Vector3[4];
        cam.CalculateFrustumCorners(new Rect(0,0,1,1), cam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, corners);

        for (int i = 0; i < 4; i++)
        {
            corners[i] = transform.TransformVector(corners[i]);
            corners[i].Normalize();
        }

        ditherMat.SetVector("_BL", corners[0]);
        ditherMat.SetVector("_TL", corners[1]);
        ditherMat.SetVector("_TR", corners[2]);
        ditherMat.SetVector("_BR", corners[3]);

        Graphics.Blit(src, large, ditherMat);
        Graphics.Blit(large, main, thresholdMat);
        Graphics.Blit(main, dst);

        RenderTexture.ReleaseTemporary(large);
        RenderTexture.ReleaseTemporary(main);
    }

    void OnDisable()
    {
        if (renderBuffer != null)
        {
            renderBuffer.Release();
            renderBuffer = null;
        }
    }
}
