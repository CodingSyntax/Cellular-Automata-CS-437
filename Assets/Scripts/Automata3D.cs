using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Automata3D : MonoBehaviour
{
    public ComputeShader shader;
    public GameObject cube;

    public bool randomCenterOnly;
    public bool regenShader;

    private int kernelHandle;
    private ComputeBuffer inBuffer;
    private ComputeBuffer outBuffer;

    private int updateCount;

    const int scale = 40;
    const int bits = scale * scale * scale;
    const int UINTSIZE = sizeof(uint) * 8;
    const int bufferSize = (bits + UINTSIZE - 1) / UINTSIZE;

    private bool paused;
    private bool oneFrame;

    // Start is called before the first frame update
    void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");
        shader.SetInt("size", scale);
        
        
        inBuffer = new ComputeBuffer(bufferSize, sizeof(uint));
        outBuffer = new ComputeBuffer(bufferSize, sizeof(uint));
        uint[] data = _Randomize();
        //create cubes
        for (int z = 0; z < scale; z++) {
            for (int y = 0; y < scale; y++) {
                for (int x = 0; x < scale; x++) {
                    GameObject newCube = Instantiate(cube, new Vector3(x, y, z), Quaternion.identity, transform);
                    newCube.SetActive(GetData(data, x + ((y + (z * scale)) * scale)));
                }
            }
        }
        StaticBatchingUtility.Combine(gameObject);
        updateCount = 0;
        paused = true;
        oneFrame = false;
    }

    private uint[] _Randomize(bool draw = false) {
        uint[] data;
        if (!randomCenterOnly) data = Randomize();
        else data = RandomizeCenter();

        if (draw) UpdateDisplay();
        return data;
    }

    private void RegenShader() {
        kernelHandle = shader.FindKernel("CSMain");
        shader.SetInt("size", scale);
    }

    private void OnApplicationQuit() {
        inBuffer.Dispose();
        outBuffer.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) {
            paused = !paused;
            Debug.Log("Paused: " + paused);
        }
        if (Input.GetKeyDown(KeyCode.Space)) {
            oneFrame = true;
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            _Randomize(true);
        }

        if (regenShader) {
            RegenShader();
            regenShader = false;
        }


        if (!paused || oneFrame) {
            updateCount++;
            if (updateCount > 1 || oneFrame) {
                updateCount = 0;
                shader.SetBuffer(kernelHandle, "inBuffer", inBuffer);
                shader.SetBuffer(kernelHandle, "outBuffer", outBuffer);
                const int s = scale + 3;
                shader.Dispatch(kernelHandle, s / 4, s / 4, s / 4);
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
        uint[] data = new uint[bufferSize];
        inBuffer.GetData(data);

        for (int z = 0; z < scale; z++) {
            for (int y = 0; y < scale; y++) {
                for (int x = 0; x < scale; x++) {
                    int index = x + ((y + (z * scale)) * scale);
                    GameObject newCube = transform.GetChild(index).gameObject;
                    
                    if (x == 0 || y == 0 || z == 0 || x == scale - 1 || y == scale - 1 || z == scale - 1) {
                        newCube.SetActive(false);
                    } else newCube.SetActive(GetData(data, index));
                    //Debug.Log($"{index} -> {index / UINTSIZE} / {bufferSize}");
                    //if (data[index / UINTSIZE] != 0) Debug.Log(data[index / UINTSIZE]);
                }
            }
        }
    }

    private bool GetData(uint[] data, int bitIndex) {
        int valueInd = bitIndex / UINTSIZE;
        int bit = bitIndex & (UINTSIZE - 1); // i % 32

        return (data[valueInd] & (1 << bit)) != 0;
    }

    private void SetData(uint[] data, int bitIndex, bool value) {
        int valueInd = bitIndex / UINTSIZE;
        int bit = bitIndex & (UINTSIZE - 1); // i % 32

        if (value) data[valueInd] |= (uint)(1 << bit);
        else data[valueInd] &= (uint)~(1 << bit);
    }

    public uint[] Randomize() {
        uint[] data = new uint[bufferSize];
        outBuffer.SetData(data);
        const int zScale = scale * scale;
        const int front = bits - zScale;
        const int top = zScale - scale;
        const int left = scale - 1;

        //shell is solid
        for (int i = 0; i < zScale; i++) {
            //Back face (z = 0)
            SetData(data, i, true);
            //Front face (z = scale - 1)
            SetData(data, i + front, true);
        }

        for (int i = zScale; i < front; i += scale) {
            //Right face (x = 0)
            SetData(data, i, true);
            //Left face (x = scale - 1)
            SetData(data, i + left, true);
        }

        for (int z = zScale; z < front; z += zScale) {
            for (int x = 1; x < left; x++) {
                //Bottom face (y = 0)
                SetData(data, x + z, true);
                //Top face (y = scale - 1)
                SetData(data, x + top + z, true);
            }
        }

        //Randomize interior
        for (int z = zScale; z < front; z += zScale) {
            for (int y = scale; y < top; y += scale) {
                for (int x = 1; x < left; x++) {
                    SetData(data, x + y + z, Random.Range(0, 2) == 1);
                }
            }
        }

        inBuffer.SetData(data);
        //outBuffer.SetData(data);
        return data;
    }

    public uint[] RandomizeCenter() {
        uint[] data = new uint[bufferSize];
        outBuffer.SetData(data);
        const int zScale = scale * scale;
        const int randomSize = (scale / 2) - 4;


        //Randomize interior
        for (int z = zScale * randomSize; z < bits - (randomSize * zScale); z += zScale) {
            for (int y = scale * randomSize; y < zScale - (randomSize * scale); y += scale) {
                for (int x = randomSize; x < scale - randomSize; x++) {
                    SetData(data, x + y + z, Random.Range(0, 2) == 1);
                }
            }
        }

        inBuffer.SetData(data);
        //outBuffer.SetData(data);
        return data;
    }
}
