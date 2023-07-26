using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = System.Random;

public class RandomOpt
{
    private static Random rand;
    private static RandomOpt instance; // 单例实例变量
    private RandomOpt() { } // 私有的构造函数，防止外部实例化
    public static RandomOpt Instance
    {
        get
        {
            if (instance == null)
            {
                RandomInit();
                instance = new RandomOpt();
            }
            return instance;
        }
    }
    
    private static void RandomInit()
    {
        rand = new Random();
    }

    public void RandomReset()
    {
        rand = new Random();
    }

    public Double RandDouble() { return rand.NextDouble(); }
    
    public float RandFloat(float offset = 0.1f) { return Mathf.Lerp(0.1f, 0.9f, (float)rand.NextDouble() + offset); }
    public int RandInt() { return rand.Next(); }
    public int RandInt(int min, int max) { return rand.Next(min, max + 1); }
}

public class SphereCcontroller : MonoBehaviour
{
    // 格子数量
    public int rows;
    public int cols;
    public int stepL;

    // 每个格子的长宽
    private float cellWidth;
    private float cellHeight;

    // 小格子，模拟像素
    // gameobject // parent
    public GameObject cell;
    
    // 空间点
    public GameObject ray;  //parent
    public GameObject msphere;
    public int sphereNum;
    private GameObject[] sphereArray;
    private Color[] SphereRGB;

    private Color[,] GridRGB;
    private GameObject[,] Grid; // 二维数组，存储网格元素
    
    public GameObject virtualCamera;
    // Start is called before the first frame update

    private int _stateX = 0;
    private int _stateY = 0;
    private int currentSphere = 0;
    
    private float movingInterval = 3f;
    private float alphaSum = 0f;

    private RandomOpt randOpt;
    
        
    private void Start()
    {
        randOpt = RandomOpt.Instance;

        ParameterInit();

        CreateGrid();

        CreateSphere();
        
        //VolumeRender();
        DrawPixel(0, 0);
        
        // 注册槽函数
        SubscribeToPublisher();

    }

    // 注册槽函数
    private void SubscribeToPublisher()
    {
        BallSignalPublisher.Instance.StateSignal += OnStateSignalReceived;
    }
    
    private void ParameterInit()
    {
        // cell长宽，用于二维坐标和三维空间坐标的互映射
        var rect = cell.GetComponent<RectTransform>().rect;
        cellWidth = rect.width;
        cellHeight = rect.height;

    }

    private void CreateSphere()
    {
        // sphere逻辑构建
        sphereArray = new GameObject[sphereNum];
        for (int i = 0; i < sphereNum; ++i)
            sphereArray[i] = Instantiate(msphere, ray.transform);
        
        // sphere颜色构建
        SphereRGB = new Color[sphereNum];
        Random rand = new Random();
        for (int i = 0; i < sphereNum; ++i)
        {
            SphereRGB[i] = randColor();
            alphaSum += SphereRGB[i].a;
        }
    }

    private Color randColor()
    {
        return new Color(randOpt.RandFloat(), randOpt.RandFloat(), randOpt.RandFloat(), randOpt.RandFloat());
    }

    private void CreateGrid()
    {
        // Grid颜色构建
        GridRGB = new Color[rows, cols];
        Random rand = new Random();
        for (int i = 0; i < rows; ++i)
        {
            for (int j = 0; j < cols; ++j)
            {
                GridRGB[i, j] = randColor();
            }
            
        }
        
        // Grid逻辑&物理构建
        Grid = new GameObject[rows, cols]; // 初始化二维数组
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                // 创建一个网格元素的游戏对象
                GameObject gridElement = Instantiate(cell,gameObject.transform);
                // 设置网格元素的位置
                float x = i * cellWidth + cellWidth / 2;
                float y = j * cellHeight + cellHeight / 2;
                float z = 0f;
                gridElement.transform.position = new Vector3(x, y, z);
                //gridElement.transform.rotation = Quaternion.Euler(0, 0, 0);
                //gridElement.GetComponent<Image>().color = GridRGB[i, j];
                gridElement.GetComponent<Image>().color = Color.black;
                // 将网格元素添加到二维数组中
                Grid[i, j] = gridElement;
            }
        }
    }

    private Vector3 GetDir(Vector3 pixel)
    {
        string info = String.Format("pixel: ({0}, {1}, {2})", pixel[0], pixel[1], pixel[2]);
        Debug.Log(info);

        Vector3 virP = virtualCamera.transform.position;
        string info1 = String.Format("virP: ({0}, {1}, {2})", virP[0], virP[1], virP[2]);
        Debug.Log(info1);
        
        Vector3 normalized = (pixel - virP).normalized;
        string info2 = String.Format("normalized: ({0}, {1}, {2})", normalized[0], normalized[1], normalized[2]);
        Debug.Log(info2);
        
        return (pixel - virtualCamera.transform.position).normalized;
    }
    
    private Vector3 Index2Position(int x, int y)
    {
        return new Vector3(x * cellWidth + cellWidth / 2, y * cellHeight + cellHeight / 2, 0);
    }
    
    public GameObject GetGridElement(int row, int column)
    {
        if (row >= 0 && row < rows && column >= 0 && column < cols)
        {
            return Grid[row, column];
        }
        else
        {
            Debug.LogError("Invalid grid element coordinates");
            return null;
        }
    }

    private List<Vector3> CreateTargetL(Vector3 gridIntersactionP, Vector3 dir)
    {
        List<Vector3> tarList = new List<Vector3>();
        
        // 计算dir方向的offset
        float offset = msphere.transform.localScale.z / (2 * dir.z);
        
        tarList.Add(gridIntersactionP + offset * dir);
        tarList.Add(gridIntersactionP);
        
        return tarList;
    }

    private void DrawPixel(int x, int y)
    {
        CreateSphere();
        
        Vector3 gridIntersactionP = Index2Position(x, y);
        Vector3 dir = GetDir(gridIntersactionP);

        string info = String.Format("dir: ({0}, {1}, {2})", dir[0], dir[1], dir[2]);
        Debug.Log(info);

        var tarList = CreateTargetL(gridIntersactionP, dir);
        
        for (int i = 0;i < sphereNum;++i)
        {
            var _sphere = sphereArray[i];
            _sphere.transform.position = gridIntersactionP + dir * stepL * (i + 1);
            _sphere.GetComponent<Renderer>().material.color = SphereRGB[i];
            _sphere.AddComponent<BallMoving>();
            var ballMoving = _sphere.GetComponent<BallMoving>();
            
            ballMoving.SetTargetPosition(tarList);
            ballMoving.SetBindingCell(Grid[x, y], SphereRGB[i].a/alphaSum);

            ballMoving.SetFadeOutTime(movingInterval);
            ballMoving.StartMoving(2 * i * movingInterval);
            
            //Grid[3, 2].GetComponent<Image>().color += SphereRGB[i]/ sphereNum * SphereRGB[i].a;
        }
    }

    private void VolumeRender()
    {
        for(int i = 0;i < rows;++i)
            for(int j = 0;j < cols;++j)
                DrawPixel(i, j);
    }

    private void OnStateSignalReceived()
    {
        currentSphere++;
        String info = String.Format("currentSphere: {0}, _stateX: {1}, _stateY: {2}", currentSphere, _stateX, _stateY);
        Debug.Log(info);
        if (currentSphere == sphereNum)
        {
            if (_stateY == rows) return;
            if (_stateX == cols)
            {
                _stateX = 0;
                _stateY++;
            }
            currentSphere = 0;
            _stateX++;
            DrawPixel(_stateX, _stateY);
        }
        else
        {
            DrawPixel(_stateX, _stateY);
        }
    }
    
}
