using UnityEngine;
using System.Collections;

// This script allow you to test the picking. Use you mobile and you mouse and click on element.
// This element will be deactivated
public class PickingTest : MonoBehaviour
{
    protected PickingSystem m_PickingSystem;
    private bool isClic;

    IEnumerator Start()
    {
        m_PickingSystem = GameObject.FindObjectOfType<PickingSystem>();
        
        while (false)
        {
            // Wait until all rendering + UI is done.
            yield return new WaitForEndOfFrame();

            if (isClic)
            {
                GameObject obj = m_PickingSystem.Picking(Input.mousePosition);
                if (obj)
                {
                    obj.SetActive(false);
                }
            }
        }
    }

    void Update()
    {
#if UNITY_STANDALONE
        isClic = Input.GetMouseButtonDown(0);     
#else
        isClic = (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
#endif  
        
        if (isClic)
        {
            GameObject obj = m_PickingSystem.Picking(Input.mousePosition);
            if (obj)
            {
                obj.SetActive(false);
            }
        }
    }
}
