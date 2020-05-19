// Compute shader implementation of Gaussian blur by Olli S.

using UnityEngine;

public class GaussianBlurGroupSharedMemory : MonoBehaviour
{
    public ComputeShader compute;

    [Header("Textures")]
    [SerializeField] Texture2D sourceTexture;
    RenderTexture horizResultTexture = null;
    RenderTexture resultTexture = null;

    int kernel_horizontal;
    int kernel_vertical;

    int propSourceTexture = Shader.PropertyToID("SourceTexture");
    int propHorizResultTexture = Shader.PropertyToID("HorizResultTexture");
    int propResultTexture = Shader.PropertyToID("ResultTexture");
    int propSpread = Shader.PropertyToID("_Spread");
    int propSamples = Shader.PropertyToID("_Samples");

    int textureSize;

    [Header("Gaussian parameters")]
    [SerializeField] [Range(0,128)] int kernelSize;

    [Header("Toggle passes")]
    [SerializeField] bool enable_h;
    [SerializeField] bool enable_v;

    [Header("Thread groups (debug)")]
    [SerializeField] int count;

    const int THREADSGROUPS_BLUR = 256;

    int samples;
    float spread;

    void Start()
    {
        sourceTexture.wrapMode = TextureWrapMode.Clamp;

        textureSize = sourceTexture.width;

        kernel_horizontal = compute.FindKernel("K_Horizontal");
        kernel_vertical   = compute.FindKernel("K_Vertical");

        horizResultTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        horizResultTexture.enableRandomWrite = true;
        horizResultTexture.wrapMode = TextureWrapMode.Clamp;
        horizResultTexture.autoGenerateMips = false;
        horizResultTexture.Create();

        resultTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        resultTexture.enableRandomWrite = true;
        resultTexture.wrapMode = TextureWrapMode.Clamp;
        resultTexture.autoGenerateMips = false;
        resultTexture.Create();

        count = Mathf.Max(1, textureSize / THREADSGROUPS_BLUR);
    }


    void Update()
    {
        samples = Mathf.Max(1, kernelSize / 2);

        spread = Mathf.Max(1.0f, samples / 2.0f);

        compute.SetTexture(kernel_horizontal, propSourceTexture, sourceTexture);
        compute.SetTexture(kernel_horizontal, propHorizResultTexture, horizResultTexture);

        compute.SetTexture(kernel_vertical, propHorizResultTexture, horizResultTexture);
        compute.SetTexture(kernel_vertical, propResultTexture, resultTexture);

        compute.SetInt(propSamples, samples);
        compute.SetFloat(propSpread, spread);


        if (enable_h) {
            compute.Dispatch(kernel_horizontal, count, textureSize, 1);
        }
        else if (!enable_h) {
            Graphics.Blit(sourceTexture, horizResultTexture);
        }
        if (enable_v) {
            compute.Dispatch(kernel_vertical, textureSize, count, 1);
        }
        else if (!enable_v) {
            Graphics.Blit(horizResultTexture, resultTexture);
        }
        if (!enable_h && !enable_v) {
            Graphics.Blit(sourceTexture, resultTexture);
        }
    }


    GUIStyle guiStyle = new GUIStyle();

    void  OnGUI ()
    {
        int  w = Screen.width  / 2;
        int  h = Screen.height / 2;
        int  s = 1024;

        // This test is just intended for a 1920x1080 viewport, but this scales image down if you use smaller screen
        if (Screen.height < s) {
            s = Screen.height;
        }

        GUI.DrawTexture (new Rect (w - s/2 , h - s/2 , s , s), resultTexture, ScaleMode.ScaleToFit, false, w/h);

        guiStyle.fontSize = 18;
        GUILayout.BeginArea(new Rect(20, 20, 300, 200));
        GUILayout.Label("Blur horizontal: " + enable_h, guiStyle);
        GUILayout.Label("Blur vertical: " + enable_v, guiStyle);
        GUILayout.Label("Kernel size: " + kernelSize, guiStyle);
        GUILayout.Label("Samples: " + samples, guiStyle);
        GUILayout.Label("Spread: " + spread, guiStyle);
        GUILayout.EndArea();
    }


    void OnDisable()
    {
        if (horizResultTexture != null) {
            horizResultTexture.Release();
            DestroyImmediate(horizResultTexture);
        }

        if (resultTexture != null) {
            resultTexture.Release();
            DestroyImmediate(resultTexture);
        }
    }
}