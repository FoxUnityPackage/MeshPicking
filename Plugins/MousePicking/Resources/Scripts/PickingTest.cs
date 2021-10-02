using UnityEngine;

// This script allow you to test the picking. Use you mobile and you mouse and click on element.
// This element will be deactivated
public class PickingTest : MonoBehaviour
{
    protected PickingSystem m_PickingSystem;
    
    void Start()
    {
        m_PickingSystem = GameObject.FindObjectOfType<PickingSystem>();
    }

    void Update()
    {
#if UNITY_STANDALONE
        bool isClic = Input.GetMouseButtonDown(0);
#else
        bool isClic = (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
#endif  
        
        if (isClic)
        {
#if UNITY_STANDALONE
        Vector2 pos = Input.mousePosition;
#else
            Vector2 pos = Input.GetTouch(0).position;
#endif  
            GameObject obj = m_PickingSystem.Picking(pos);
            if (obj)
            {
                obj.SetActive(false);
                
            }
        }
    }
}
