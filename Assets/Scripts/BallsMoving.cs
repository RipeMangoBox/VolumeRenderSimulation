using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


// 通过委托实现信号与槽机制，来触发Module状态改变时的事件
// 单例类
public class BallSignalPublisher
{
    private static BallSignalPublisher instance;
    public static BallSignalPublisher Instance
    {
        get 
        {
            if (instance == null)
            {
                instance = new BallSignalPublisher();
            }
            return instance;
        }
    }

    private BallSignalPublisher()
    {
        // 私有构造函数
    }
    
    
    // 声明委托类型
    public delegate void SignalSender();
    
    // 
    public event SignalSender StateSignal;
    
    public void TriggerSignal()
    {
        StateSignal.Invoke();
    }
}


public class BallsMoving : MonoBehaviour
{
    public GameObject[] sphereArray; // 目标位置
    public List<Vector3> targetList; // 目标位置

    public float moveTime = 0.1f; // 移动时间
    public float waitingTime = 0.1f;
    public float fadeOutTime = 0.1f; // 消失时间
    private Coroutine moveCoroutine; // 移动协程的引用

    private GameObject _hitCell; // 碰撞cell
    private float[] _sphereColorWeightArray;
    private BallSignalPublisher _ballSignalPublisher;

    private void Start()
    {
        _ballSignalPublisher = BallSignalPublisher.Instance;
    }
    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator MoveToTarget()
    {
        for (int spIndex = 0; spIndex < sphereArray.Length; ++spIndex)
        {
            GameObject sphere = sphereArray[spIndex];
            Color sphereColor = sphere.GetComponent<Renderer>().material.color;
            /*
            // startWait
            {
                float elapsedTime = 0f;
                while (elapsedTime < startWaitingTime)
                {
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }
            */


            for (int i = 0; i < targetList.Count - 1; ++i)
            {
                Vector3 startPosition = sphere.transform.position;
                float elapsedTime = 0f;
                while (elapsedTime < moveTime)
                {
                    /*// 匀速版
                    {
                        // 根据插值计算当前位置
                        float t = elapsedTime / moveTime;
                        sphere.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }*/

                    // 变速版
                    {
                        // 根据插值计算当前位置
                        float t = elapsedTime / moveTime;
                        float distanceToTarget = Vector3.Distance(sphere.transform.position, targetList[i]);

                        // 使用缓动函数调整速度
                        //float easing = 1f - Mathf.Pow(1f - t, 2f);
                        float easing = Mathf.Pow(t, 0.9f);
                        // 根据缓动函数的调整速度计算当前位置
                        sphere.transform.position = Vector3.Lerp(startPosition, targetList[i], easing);
                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }
                }

                // 移动完成后，重置位置
                sphere.transform.position = targetList[i];
            }

            // 暂停等待
            {
                float elapsedTime = 0f;
                while (elapsedTime < waitingTime)
                {
                    // 匀速版
                    {
                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }
                }
            }

            // 进行最后一段移动
            // 边移动边降低sphere透明度和scale
            // 同时变化cell的颜色，仅rgb，不动a
            {
                float fadeElapsedTime = 0f;
                Vector3 startPosition = sphere.transform.position;
                Vector3 targetPosition = targetList[targetList.Count() - 1];
                Vector3 startScale = sphere.transform.localScale;
                Vector3 targetScale = Vector3.zero;
                Color startColor = _hitCell.GetComponent<Image>().material.color;
                //_hitCell.GetComponent<Image>().material.color =  Color.black;


                while (fadeElapsedTime < fadeOutTime)
                {
                    // 计算透明度的插值
                    float fadeT = fadeElapsedTime / fadeOutTime;

                    // 使用缓动函数调整速度
                    float fadeEasing = Mathf.Pow(fadeT, 0.2f);

                    // 线性插值部分
                    sphere.transform.position = Vector3.Lerp(startPosition, targetPosition, fadeT);
                    sphere.transform.localScale = Vector3.Lerp(startScale, targetScale, fadeT);
                    //_hitCell.GetComponent<Image>().color = Color.Lerp(startColor, sphereColor, fadeT);
                    _hitCell.GetComponent<Image>().color +=
                        sphereColor * _sphereColorWeightArray[spIndex] * Time.deltaTime / fadeOutTime;

                    yield return null;
                    
                    sphereColor.a = Mathf.Lerp(sphereColor.a, 0f, fadeEasing);
                    fadeElapsedTime += Time.deltaTime;
                    yield return null;
                }

                //_hitCell.GetComponent<Image>().color += sphereColor / 5 * sphereColor.a;
                yield return null;
                // 完全消失后销毁小球
                Destroy(sphere);

               
            }

        }
        // 触发信号，渲染下一个pixel
        _ballSignalPublisher.TriggerSignal();
    }

    public void SetTargetPosition(Vector3 targetP)
    {
        targetList = new List<Vector3>();
        targetList.Add(targetP);
    }
    
    public void SetTargetPosition(List<Vector3> targetP)
    {
        targetList = targetP;
    }
    
    public void SetBindingCell(GameObject cell, float[] sphereColorWeightArray)
    {
        _hitCell = cell;
        
        _sphereColorWeightArray = sphereColorWeightArray;
    }

    private void SetBallMovingParameters(BallsMovingParameters ballsMovingParameters)
    {
        moveTime = ballsMovingParameters.moveTime; // 移动时间
        waitingTime = ballsMovingParameters.waitingTime;
        fadeOutTime = ballsMovingParameters.fadeOutTime; // 消失时间
    }
    
    public void SetFadeOutTime(float t)
    {
        fadeOutTime = t;
    }
    
    
    
    public void StartMoving(GameObject[] SphereArray, List<Vector3> targetP, GameObject cell, float[] sphereColorWeightArray,
        BallsMovingParameters ballsMovingParameters)
    {
        sphereArray = SphereArray;
        targetList = targetP;
        SetBindingCell(cell, sphereColorWeightArray);
        SetBallMovingParameters(ballsMovingParameters);
        // 开始移动协程
        moveCoroutine = StartCoroutine(MoveToTarget());
    }
    
    public void StopMoving()
    {
        // 停止移动协程
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
    }
}