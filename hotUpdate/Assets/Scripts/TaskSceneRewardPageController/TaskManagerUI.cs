using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskManagerUI : MonoBehaviour
{


    public GameObject RewardPage;
    void Start()
    {
        
    }

    
    void Update()
    {
        
    }



    /// <summary>
    /// 点击领取时发生
    /// </summary>
    public void Onclicking()
    {
        Debug.Log("12432");
        
        RewardPage.SetActive (true) ;

    }
    


}
