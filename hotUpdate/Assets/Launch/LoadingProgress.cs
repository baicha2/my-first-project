using UnityEngine;
using UnityEngine.UI;

public class LoadingProgress : MonoBehaviour
{
    [SerializeField] Slider _progressSlider;
    [SerializeField] Text _percentText;
    [SerializeField] Text _hintText;

    float _totalProgress;

    void OnEnable()
    {
        Reset();
    }

    public void Reset()
    {
        _hintText.text = "";
        _percentText.text = "";

        _progressSlider.value = 0;
    }

    /// <summary>
    /// 刷新提示语
    /// </summary>
    /// <param name="hint"></param>
    public void RefreshHint(string hint)
    {
        _hintText.text = hint;
    }

    /// <summary>
    /// 刷新进度
    /// </summary>
    /// <param name="progress">当前进度，取值范围：[0, 1]</param>
    public void RefreshProgress(float progress)
    {
        _totalProgress = progress;
        _progressSlider.value = _totalProgress;
        _percentText.text = $"{(int)(_progressSlider.value * 100)}%";
    }
}