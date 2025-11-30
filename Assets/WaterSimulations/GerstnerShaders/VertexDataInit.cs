using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexData
{
    public Vector3 position;
    public Vector3 normal;
}

[System.Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct WaveType
{
    [Tooltip("Amplituda fali (wysokoœæ).")]
    public float A;
    [Tooltip("Stromoœæ (Steepness), kontroluje ostroœæ grzbietów. Zakres 0..1.")]
    public float Q;
    [Tooltip("D³ugoœæ fali (Wavelength).")]
    public float lambda;
    [Tooltip("Przesuniêcie w fazie.")]
    public float fi;
    [Tooltip("Kierunek propagacji fali (znormalizowany wektor 2D).")]
    public Vector2 D;
}

public class VertexDataInit : MonoBehaviour
{
    private MeshFilter meshFiltered;
    private Mesh mesh;
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private Material oceanMaterial;
    // Struct of wave containing parameters

    [Header("Parametry Fal")]
    [SerializeField]public WaveType[] waves = new WaveType[4];
    // Compute shader data init
    private int karnelHandle;
    private ComputeBuffer vertexInputBuffer;
    private ComputeBuffer vertexOutputBuffer;
    private ComputeBuffer waveBuffer;

    void SetupMesh()
    {

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.sharedMesh;

        VertexData[] initialVertices = new VertexData[mesh.vertexCount];

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            initialVertices[i].position = mesh.vertices[i];
            initialVertices[i].normal = mesh.normals[i];
        }

        for (int i = 0; i < waves.Length; i++)
        {
            waves[i].D = waves[i].D.normalized;
        }

        int vertexStride = Marshal.SizeOf(typeof(VertexData));
        vertexInputBuffer = new ComputeBuffer(mesh.vertexCount, vertexStride);
        vertexInputBuffer.SetData(initialVertices);

        vertexOutputBuffer = new ComputeBuffer(mesh.vertexCount, vertexStride);


        int waveStride = Marshal.SizeOf(typeof(WaveType));
        waveBuffer = new ComputeBuffer(waves.Length, waveStride);
        waveBuffer.SetData(waves);
    }

    private void Start()
    {
        SetupMesh();

        karnelHandle = computeShader.FindKernel("GerstnerWaveKernel");

        computeShader.SetBuffer(karnelHandle,"VertexInputBuffer" , vertexInputBuffer);
        computeShader.SetBuffer(karnelHandle, "VertexOutputBuffer", vertexOutputBuffer);
        computeShader.SetBuffer(karnelHandle, "WaveBuffer", waveBuffer); 
        computeShader.SetInt("VertexCount", mesh.vertexCount);

        oceanMaterial.SetBuffer("VertexBuffer", vertexOutputBuffer);
    }

    private void Update()
    {
        computeShader.SetFloat("Time", Time.time);

        waveBuffer.SetData(waves);

        int threadGroupSize = 64;
        int groups = Mathf.CeilToInt((float)mesh.vertexCount / threadGroupSize);
        computeShader.Dispatch(karnelHandle, groups, 1, 1);
    }

    private void OnDisable()
    {
        // Zwolnienie zasobów GPU
        vertexInputBuffer?.Release();
        vertexOutputBuffer?.Release();
        waveBuffer?.Release();
        vertexInputBuffer = null;
        vertexOutputBuffer = null;
        waveBuffer = null;
    }
}
