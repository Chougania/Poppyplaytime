using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileEnabled : MonoBehaviour
{
    public MobileIcons mobileicons;


    void Start()
    {
        if (!mobileicons.isMobile)
        {
            gameObject.SetActive(false);
        }
    }


}
