using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Automata2D : MonoBehaviour
{
    public ComputeShader shader;
    public Material mat;

    public int x;
    public int y;

    private int kernelHandle;
    private RenderTexture inBuffer;
    private RenderTexture outBuffer;

    private int updateCount;

    const int texSize = 256;

    private bool paused;

    bool setPixel;

    // Start is called before the first frame update
    void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");
        inBuffer = new RenderTexture(texSize, texSize, 1);
        outBuffer = new RenderTexture(texSize, texSize, 1);
        inBuffer.enableRandomWrite = true;
        outBuffer.enableRandomWrite = true;
        inBuffer.filterMode = FilterMode.Point;
        outBuffer.filterMode = FilterMode.Point;
        inBuffer.Create();
        outBuffer.Create();
        updateCount = 0;
        paused = true;
        setPixel = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) {
            paused = !paused;
            Debug.Log("Paused: " + paused);
        }
        if (Input.GetKeyDown(KeyCode.KeypadEnter)) {
            setPixel = true;
        }


        if (!paused) {
            updateCount++;
            if (updateCount > 4) {
                updateCount = 0;
                shader.SetTexture(kernelHandle, "inBuffer", inBuffer);
                shader.SetTexture(kernelHandle, "outBuffer", outBuffer);

                shader.Dispatch(kernelHandle, texSize / 8, texSize / 8, 1);
                //swap buffers
                var temp = inBuffer;
                inBuffer = outBuffer;
                outBuffer = temp;

                mat.mainTexture = inBuffer;
            }
        }
    }

    public void PostRenderCall() {
        if (setPixel) {
            Texture2D tex = new(inBuffer.width, inBuffer.height, TextureFormat.RGBA32, false, false, true);
            var oldRt = RenderTexture.active;
            RenderTexture.active = inBuffer;
            tex.ReadPixels(new Rect(0, 0, inBuffer.width, inBuffer.height), 0, 0);
            RenderTexture.active = oldRt;
            tex.Apply();
            if (tex.GetPixel(x, y) == Color.white) {
                tex.SetPixel(x, y, Color.black);
                Debug.Log($"Draw black at: ({x}, {y})");
            } else {
                tex.SetPixel(x, y, Color.white);
                Debug.Log($"Draw white at: ({x}, {y})");
            }
            tex.Apply();
            Graphics.CopyTexture(tex, inBuffer);
            setPixel = false;
        }
    }
}
