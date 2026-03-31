using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SessionManagement : MonoBehaviour
{
    public GameObject scene1;
    public GameObject scene2;
    


    void Start()
    {
        
    }

    void Update()
    {
        
    }



    public void clicking() 
    {
        scene1.SetActive(false);
        scene2.SetActive(true);

    }




}
