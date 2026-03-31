using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToHome : MonoBehaviour
{
    void Start()
    {
        
    }

    
    void Update()
    {
        
    }

    // 指定场景名称
    public void ReturnHome()
    {
        SceneManager.LoadScene("HomePage");
    }


}
