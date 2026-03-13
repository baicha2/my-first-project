using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SimpleDialog : MonoBehaviour
{
    [SerializeField]
    Text _titleText;

    [SerializeField]
    Text _contentText;

    [SerializeField]
    ScrollRect _scrollRect;

    [SerializeField]
    Button _leftBtn;

    [SerializeField]
    Button _rightBtn;

    Text _leftBtnText;
    Text _rightBtnText;

    Action _leftBtnAction;
    Action _rightBtnAction;

    const float LineHeight = 50f;

    bool _closeOnRightBtnClick = true;

    private void Awake()
    {
        _leftBtnText = _leftBtn.GetComponentInChildren<Text>();
        _rightBtnText = _rightBtn.GetComponentInChildren<Text>();

        _leftBtn.onClick.AddListener(OnLeftBtnClick);
        _rightBtn.onClick.AddListener(OnRightBtnClick);

        if (_scrollRect)
        {
            _scrollRect.onValueChanged.AddListener(RefreshScrollbar);
        }
    }

    void RefreshScrollbar(Vector2 vector2)
    {
        // 内容过多的时候，使滑块尺寸保持一个最小值
        if (_scrollRect.verticalScrollbar.size < 0.1)
        {
            _scrollRect.verticalScrollbar.size = 0.1f;
        }
    }

    void OnLeftBtnClick()
    {
        Hide();
        _leftBtnAction?.Invoke();
    }

    void OnRightBtnClick()
    {
        if (_closeOnRightBtnClick)
        {
            Hide();
        }
        _rightBtnAction?.Invoke();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show(string title, string content)
    {
        SetupShow(title: title, content: content);
    }

    public void Show(string content, Action rightAction)
    {
        SetupShow(content: content, rightAction: rightAction);
    }

    public void Show(string content, Action leftAction, Action rightAction)
    {
        SetupShow(content: content, leftAction: leftAction, rightAction: rightAction);
    }

    public void Show(string title, string content, string leftBtnText, string rightBtnText, Action leftAction, Action rightAction)
    {
        SetupShow(title: title, content: content, leftBtnText: leftBtnText, rightBtnText: rightBtnText, leftAction: leftAction, rightAction: rightAction);
    }

    public void Show(string content, string leftBtnText, string rightBtnText, Action leftAction, Action rightAction)
    {
        SetupShow(content: content, leftBtnText: leftBtnText, rightBtnText: rightBtnText, leftAction: leftAction, rightAction: rightAction);
    }

    public void SetupShow(string title = "提 示", string content = "", string leftBtnText = "取 消", string rightBtnText = "确 定", Action leftAction = null, Action rightAction = null)
    {
        gameObject.SetActive(true);
        _leftBtn.gameObject.SetActive(true);
        _rightBtn.gameObject.SetActive(true);
        _closeOnRightBtnClick = true;

        _titleText.text = title;
        _leftBtnText.text = leftBtnText;
        _rightBtnText.text = rightBtnText;

        _leftBtnAction = leftAction;
        _rightBtnAction = rightAction;
        _contentText.text = content;

        StartCoroutine(SetContentAlignment());
    }

    public void HideLeftBtn()
    {
        _leftBtn.gameObject.SetActive(false);
    }

    public void DontCloseOnRightBtnClick()
    {
        _closeOnRightBtnClick = false;
    }

    /// <summary>
    /// 检查内容文本的高度，并修改内容文本的对齐方式
    /// </summary>
    IEnumerator SetContentAlignment()
    {
        // 等待一帧，Text 组件的高度才会刷新
        yield return null;

        if (_contentText.preferredHeight > LineHeight)
        {
            _contentText.alignment = TextAnchor.MiddleLeft;
        }
        else
        {
            _contentText.alignment = TextAnchor.MiddleCenter;
        }
    }

    #region LinkText

    public void SetLinkClickCallback(Action<string> action = null)
    {
        ((LinkText)_contentText).onHyperlinkClick = action;
    }

    #endregion

}
