using System;
using UnityEngine;

/// <summary>
/// Launch 场景的UI基类，由不同项目自行实现
/// </summary>
public abstract class LaunchUIBase : MonoBehaviour
{
    /// <summary>
    /// 异常提示，退出或者重试
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="retry"></param>
    public abstract void ErrorTips(string msg, Action retry);

    /// <summary>
    /// 设置版本号信息
    /// </summary>
    public abstract void SetVersionInfo(string version);

    /// <summary>
    /// 首次运行
    /// </summary>
    public abstract void OnFirstOpen();

    /// <summary>
    /// 用户按下了Esc键
    /// </summary>
    public abstract void OnEscBtnDown();

    #region 热更新过程

    /// <summary>
    /// 检测更新
    /// </summary>
    public abstract void OnCheckUpdate();

    /// <summary>
    /// 刷新热更新进度
    /// </summary>
    /// <param name="curBytes">已下载多少 MB</param>
    /// <param name="totalBytes">需要下载多少 MB</param>
    /// <param name="percent">下载进度（范围：0.0 ~ 1.0）</param>
    public abstract void RefreshPrecentProgress(float percent, float curBytes = 0f, float totalBytes = 0f);

    /// <summary>
    /// 热更新已完成
    /// </summary>
    public abstract void OnHotUpdateFinish();

    #endregion

    #region 加载资源

    /// <summary>
    /// 加载资源
    /// </summary>
    public abstract void OnLoadingResource();

    #endregion


    #region 加载场景

    /// <summary>
    /// 开始加载主场景的AB包
    /// </summary>
    public abstract void OnBeginLoadSceneBundle();

    /// <summary>
    /// 开始加载场景
    /// </summary>
    public abstract void OnBeginLoadScene();

    #endregion
}