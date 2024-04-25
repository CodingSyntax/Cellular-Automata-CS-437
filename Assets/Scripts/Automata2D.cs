using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Automata2D : MonoBehaviour
{
    public ComputeShader shader;
    public Material mat;

    public int x;
    public int y;

    Vector3 lastMousePos;

    private int kernelHandle;
    private RenderTexture inBuffer;
    private RenderTexture outBuffer;

    private int updateCount;
    private const int updatesPerFrame = 4;

    const int texSize = 256;

    private bool paused;
    private bool oneFrame;

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
        oneFrame = false;
        lastMousePos = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) {
            paused = !paused;
            Debug.Log("Paused: " + paused);
        }
        if (Input.GetKeyDown(KeyCode.KeypadEnter)) {
            oneFrame = true;
        }

        if (Input.GetMouseButton(0)) {
            if (Input.mousePosition != lastMousePos) {
                Ray ray = Camera.main.ScreenPointToRay(lastMousePos = Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)) {
                    //Debug.Log(hit.point);
                    float fx = 1 - (hit.point.x + 0.5f); //0-1
                    float fy = 1 - (hit.point.y + 0.5f); //0-1
                    x = (int)(fx * texSize);
                    y = (int)(fy * texSize);
                    setPixel = true;
                }
            }
        }


        if (!paused || oneFrame) {
            updateCount++;
            if (updateCount > updatesPerFrame || oneFrame) {
                updateCount = 0;
                shader.SetTexture(kernelHandle, "inBuffer", inBuffer);
                shader.SetTexture(kernelHandle, "outBuffer", outBuffer);

                shader.Dispatch(kernelHandle, texSize / 8, texSize / 8, 1);
                //swap buffers
                var temp = inBuffer;
                inBuffer = outBuffer;
                outBuffer = temp;

                mat.mainTexture = inBuffer;
                oneFrame = false;
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
