using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script allow you to test the picking. Use you mobile and you mouse and click on element.
// This element will be deactivated
public class PickingTest : MonoBehaviour
{
    [SerializeField] protected PickingManager m_PickingManager;
        
    void Update()
    {
#if UNITY_STANDALONE
        bool isClic = Input.GetMouseButtonDown(0);     
#else
        bool isClic = (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
#endif
        if (isClic)
        {
            GameObject obj = m_PickingManager.Picking(Input.mousePosition);
            if (obj)
                obj.SetActive(false);
        }
    }
}
