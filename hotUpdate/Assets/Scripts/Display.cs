using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Display : MonoBehaviour
{
    public GameObject EquipmentBar;
    public GameObject Goods;

    // Start is called before the first frame update
    void Start()
    {
        EquipmentBar.SetActive(true);
        Goods.SetActive(false);
    }

   
    void Update()
    {
        
    }
    /// <summary>
    /// 显示装备栏
    /// </summary>
    public void clickingEquipmentBar()
    {
        EquipmentBar.SetActive(true);
        Goods.SetActive(false);

    }

    /// <summary>
    /// 显示物品栏
    /// </summary>
    public void clickingGoods()
    {
        EquipmentBar.SetActive(false);
        Goods.SetActive(true);

    }


}
