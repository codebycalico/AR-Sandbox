﻿using UnityEngine;
using System.Collections;

public class DepthMesh : MonoBehaviour
{
    public DepthWrapper KinectDepth;
    int KinectWidth = 320;
    int KinectHeight = 240;
    [HideInInspector]
    public Vector3[] newVertices;
    [HideInInspector]
    public Vector3[] newNormals;
    [HideInInspector]
    public Color32[] newColors;
    [HideInInspector]
    public Vector2[] newUV;
    [HideInInspector]
    public int[] newTriangles;
    Mesh MyMesh;

    public int Width = 240;
    public int Height = 240;
    public int OffsetX;
    public int OffsetY;
    public int MinValue = 0;
    public int MaxValue = short.MaxValue;

    short MinValueBuffer;
    short MaxValueBuffer;
    short[] DepthImage;
    float[] FloatValues;

    int WidthBuffer;
    int HeightBuffer;

    // Use this for initialization
    void Start()
    {
        WidthBuffer = Width;
        HeightBuffer = Height;

        MyMesh = new Mesh();
        GetComponent<MeshFilter>().mesh = MyMesh;

        SetupArrays();
    }

    // Update is called once per frame
    void Update()
    {
        if (KinectDepth.pollDepth())
        {
            DepthImage = KinectDepth.depthImg;
            CheckArrays();
            CalculateFloatValues();
            UpdateMesh();
        }
    }

    void CheckArrays()
    {
        if((Width != WidthBuffer) || (Height != HeightBuffer))
        {
            SetupArrays();
            WidthBuffer = Width;
            HeightBuffer = Height;
        }
    }

    void SetupArrays()
    {
        FloatValues = new float[Width * Height];
        newVertices = new Vector3[Width * Height];
        newNormals = new Vector3[Width * Height];
        newColors = new Color32[Width * Height];
        newUV = new Vector2[Width * Height];
        newTriangles = new int[(Width - 1) * (Height - 1) * 6];

        for (int H = 0; H < Height; H++)
        {
            for (int W = 0; W < Width; W++)
            {
                int Index = W + H * Width;
                newVertices[Index] = new Vector3(W, H, 0f);
                newNormals[Index] = new Vector3(0, 0, 1);
                newColors[Index] = new Color32(0, 0, 0, 255);
                newUV[Index] = new Vector2(W / (float)Width, H / (float)Height);

                if ((W != (Width - 1)) && (H != (Height - 1)))
                {
                    int TopLeft = Index;
                    int TopRight = Index + 1;
                    int BotLeft = Index + Width;
                    int BotRight = Index + 1 + Width;

                    int TrinagleIndex = W + H * (Width - 1);
                    newTriangles[TrinagleIndex * 6 + 0] = TopLeft;
                    newTriangles[TrinagleIndex * 6 + 1] = BotLeft;
                    newTriangles[TrinagleIndex * 6 + 2] = TopRight;
                    newTriangles[TrinagleIndex * 6 + 3] = BotLeft;
                    newTriangles[TrinagleIndex * 6 + 4] = BotRight;
                    newTriangles[TrinagleIndex * 6 + 5] = TopRight;
                }
            }
        }

        MyMesh.vertices = newVertices;
        MyMesh.normals = newNormals;
        MyMesh.colors32 = newColors;
        MyMesh.uv = newUV;
        MyMesh.triangles = newTriangles;
    }

   void CalculateFloatValues()
    {
        for (int H = 0; H < Height; H++)
        {
            for (int W = 0; W < Width; W++)
            {
                int ImageValue = GetImageValue(W, H);
                int Index = GetArrayIndex(W, H);

                //Clamp Value

                if (ImageValue > MaxValueBuffer)
                {
                    MaxValueBuffer = (short)Mathf.Clamp(ImageValue, ImageValue, short.MaxValue);
                }

                if (ImageValue < MinValueBuffer)
                {
                    MinValueBuffer = (short)Mathf.Clamp(ImageValue, short.MinValue, ImageValue);
                }

                if (ImageValue > MaxValue)
                {
                    ImageValue = MaxValue;
                }

                if (ImageValue < MinValue)
                {
                    ImageValue = MinValue;
                }

                //Calculate
                float FloatValue = (ImageValue - MinValue) / (float)(MaxValue - MinValue);
                FloatValues[Index] = FloatValue;
            }
        }

   }

    void UpdateMesh()
    {
        MinValueBuffer = short.MaxValue;
        MaxValueBuffer = short.MinValue;

        for (int H = 0; H < Height; H++)
        {
            for (int W = 0; W < Width; W++)
            {
                ProcessPixel(W, H);
            }
        }

        MyMesh.vertices = newVertices;
        MyMesh.colors32 = newColors;

        Debug.Log(MinValueBuffer + " - " + MaxValueBuffer);
    }

    void ProcessPixel(int W, int H)
    {
        int Index = GetArrayIndex(W, H);
        float FloatValue = FloatValues[Index];
        //Calc Normal

        //Calc Position
        newVertices[Index].z = FloatValue * 100;

        //Calc Color
        float FloatValueClamped = Mathf.Clamp01(FloatValue);
        byte ByteValue = (byte)Mathf.RoundToInt(FloatValue * byte.MaxValue);

        //0-127 = 0 :: 127- 255 = 0 - 255
        byte R = (byte)(Mathf.Clamp((ByteValue - 127) * 2, 0, 255));
        //0 = 0; 127 = 255; 255 = 0
        byte G = (byte)(127 + (Mathf.Sign(127 - ByteValue) * ByteValue / 2));
        byte B = (byte)(255 - Mathf.Clamp(ByteValue * 2, 0, 255));
        newColors[Index] = new Color32(R, G, B, 255);
    }

    int GetImageValue(int W, int H)
    {
        int ImageW = OffsetX + W;
        int ImageH = OffsetY + H;

        if((ImageW < 0) || (ImageW > KinectWidth) || (ImageH < 0) || (ImageH > KinectHeight))
        {
            return (int)short.MaxValue;
        }

        int Index = ImageW + ImageH * KinectWidth;
        int Value = DepthImage[Index];

        if (Value == 0)
        {
            return (int)short.MaxValue;
        }
        else
        {
            return Value;
        }
    }
    int GetArrayIndex(int W, int H)
    {
        return W + H * Width;
    }

    int[] ShortToRGBA(short[] DepthImage)
    {
        int[] ImageData = new int[DepthImage.Length];

        for (int i = 0; i < DepthImage.Length; i++)
        {
            ImageData[i] = (int)((((int)DepthImage[i]) << 8) | 0x000000FF);
        }

        return ImageData;
    }

    int RGBAToShort(int Value)
    {
        return (Value >> 8);
    }
}