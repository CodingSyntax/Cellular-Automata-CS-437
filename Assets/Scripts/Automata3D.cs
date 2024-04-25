using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Automata3D : MonoBehaviour
{
    public ComputeShader shader;
    public GameObject cube;

    Vector3 lastMousePos;

    private int kernelHandle;
    private ComputeBuffer inBuffer;
    private ComputeBuffer outBuffer;

    private int updateCount;

    const int scale = 20;
    const int bufferSize = scale * scale * scale;

    private bool paused;
    private bool oneFrame;

    bool setPixel;

    // Start is called before the first frame update
    void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");
        shader.SetInt("Size", scale - 1);
        inBuffer = new ComputeBuffer(bufferSize, 4);
        outBuffer = new ComputeBuffer(bufferSize, 4);
        bool[] data = Randomize();
        //create cubes
        for (int z = 0; z < scale; z++) {
            for (int y = 0; y < scale; y++) {
                for (int x = 0; x < scale; x++) {
                    GameObject newCube = Instantiate(cube, new Vector3(x, y, z), Quaternion.identity, transform);
                    newCube.SetActive(data[x + ((y + (z * scale)) * scale)]);
                }
            }
        }
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


        if (!paused || oneFrame) {
            updateCount++;
            if (updateCount > 4 || oneFrame) {
                updateCount = 0;
                shader.SetBuffer(kernelHandle, "inBuffer", inBuffer);
                shader.SetBuffer(kernelHandle, "outBuffer", outBuffer);

                shader.Dispatch(kernelHandle, scale / 4, scale / 4, scale / 4);
                //swap buffers
                var temp = inBuffer;
                inBuffer = outBuffer;
                outBuffer = temp;

                UpdateDisplay();
                oneFrame = false;
            }
        }
    }

    public void UpdateDisplay() {
        byte[] data = new byte[bufferSize];
        inBuffer.GetData(data);

        for (int z = 0; z < scale; z++) {
            for (int y = 0; y < scale; y++) {
                for (int x = 0; x < scale; x++) {
                    int index = x + ((y + (z * scale)) * scale);
                    GameObject newCube = transform.GetChild(index).gameObject;
                    
                    newCube.SetActive(data[index] == 1);

                    if (data[index] != 0) Debug.Log(data[index]);
                }
            }
        }
    }

    public bool[] Randomize() {
        bool[] data = new bool[bufferSize];
        const int zScale = scale * scale;
        const int front = bufferSize - zScale;
        const int top = zScale - scale;
        const int left = scale - 1;

        //shell is solid
        //Back face (z = 0)
        for (int i = 0; i < zScale; i++) {
            data[i] = true;
        }
        //Front face (z = scale - 1)
        for (int i = front; i < bufferSize; i++) {
            data[i] = true;
        }

        for (int i = zScale; i < front; i += scale) {
            //Right face (x = 0)
            data[i] = true;
            //Left face (x = scale - 1)
            data[i + left] = true;
        }

        for (int z = zScale; z < front; z += zScale) {
            for (int x = 1; x < left; x++) {
                //Bottom face (y = 0)
                data[x + z] = true;
                //Top face (y = scale - 1)
                data[x + top + z] = true;
            }
        }

        //Randomize interior
        for (int z = zScale; z < front; z += zScale) {
            for (int y = scale; y < top; y += scale) {
                for (int x = 1; x < left; x++) {
                    data[x + y + z] = Random.Range(0, 2) == 1;
                }
            }
        }

        inBuffer.SetData(System.Array.ConvertAll(data, b => b ? (byte)1 : (byte)0));
        return data;
    }
}
