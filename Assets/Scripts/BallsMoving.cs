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


public class BallMoving : MonoBehaviour
{
    public List<Vector3> targetList; // 目标位置
    public float startWaitingTime = 0f;
    public float moveTime = 5f; // 移动时间
    public float waitingTime = 3f;
    public float fadeOutTime = 3f; // 消失时间
    private Coroutine moveCoroutine; // 移动协程的引用
    private Renderer _renderer;
    private Color _startColor;
    
    private GameObject _hitCell; // 碰撞cell
    private float _cellColorWeight;
    private int sphereNum;
    private BallSignalPublisher _ballSignalPublisher;

    private void Start()
    {
        _renderer = GetComponent<Renderer>();
        _startColor = _renderer.material.color;
        _ballSignalPublisher = BallSignalPublisher.Instance;
    }
    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator MoveToTarget()
    {
        // startWait
        {
            float elapsedTime = 0f;
            while (elapsedTime < startWaitingTime)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }


        for (int i = 0; i < targetList.Count - 1; ++i)
        {
            Vector3 startPosition = transform.position;
            float elapsedTime = 0f;
            while (elapsedTime < moveTime)
            {
                /*// 匀速版
                {
                    // 根据插值计算当前位置
                    float t = elapsedTime / moveTime;
                    transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }*/

                // 变速版
                {
                    // 根据插值计算当前位置
                    float t = elapsedTime / moveTime;
                    float distanceToTarget = Vector3.Distance(transform.position, targetList[i]);

                    // 使用缓动函数调整速度
                    //float easing = 1f - Mathf.Pow(1f - t, 2f);
                    float easing = Mathf.Pow(t, 0.9f);
                    // 根据缓动函数的调整速度计算当前位置
                    transform.position = Vector3.Lerp(startPosition, targetList[i], easing);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }

            // 移动完成后，重置位置
            transform.position = targetList[i];
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
            Vector3 startPosition = transform.position;
            Vector3 targetPosition = targetList[targetList.Count() - 1];
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = Vector3.zero;
            Color startColor = _hitCell.GetComponent<Image>().material.color;
            Color targetColor = _renderer.material.color;
            //_hitCell.GetComponent<Image>().material.color =  Color.black;
            
            Debug.Log("_cellColorWeight: " + _cellColorWeight);
            
            while (fadeElapsedTime < fadeOutTime)
            {
                // 计算透明度的插值
                float fadeT = fadeElapsedTime / fadeOutTime;
            
                // 使用缓动函数调整速度
                float fadeEasing = Mathf.Pow(fadeT, 0.2f);
            
                // 线性插值部分
                transform.position = Vector3.Lerp(startPosition, targetPosition, fadeT);
                transform.localScale = Vector3.Lerp(startScale, targetScale, fadeT);
                //_hitCell.GetComponent<Image>().color = Color.Lerp(startColor, targetColor, fadeT);
                _hitCell.GetComponent<Image>().color += targetColor * _cellColorWeight * Time.deltaTime / fadeOutTime;
                
                yield return null;
            
                Color color = _renderer.material.color;
                color.a = Mathf.Lerp(_startColor.a, 0f, fadeEasing);
                _renderer.material.color = color;
                fadeElapsedTime += Time.deltaTime;
                yield return null;
            }
            //_hitCell.GetComponent<Image>().color += targetColor / 5 * targetColor.a;
            yield return null;
            // 完全消失后销毁小球
            Destroy(gameObject);
            
            // 触发信号，渲染下一个pixel
            _ballSignalPublisher.TriggerSignal();
        }
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
    
    public void SetBindingCell(GameObject cell, float cellColorWeight)
    {
        _hitCell = cell;
        _cellColorWeight = cellColorWeight;
    }
    
    public void SetFadeOutTime(float t)
    {
        fadeOutTime = t;
    }
    
    
    
    public void StartMoving(float startWait = 0f)
    {
        startWaitingTime = startWait;
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