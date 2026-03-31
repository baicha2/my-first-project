using System.Collections;
using UnityEngine;

public class RewardPage : MonoBehaviour
{
    void OnEnable()
    {
        StartCoroutine(AutoClose());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 鼠标点击
        {
            gameObject.SetActive(false);
        }
    }

    IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(3f);
        gameObject.SetActive(false);
    }
}