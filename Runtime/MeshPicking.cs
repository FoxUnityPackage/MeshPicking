using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MeshPicking : MonoBehaviour
{
    protected MeshRenderer m_MeshRenderer;
    protected static List<int> m_AvalableID = new List<int>();
    protected static int m_StaticID = 0;
    protected int m_ID = -1;
    protected int m_PreviousLayer = -1;
    protected Material[] m_previousMaterial;

    // Register the component
    void Start()
    {
        m_MeshRenderer = GetComponent<MeshRenderer>();
        PickingManager.GetInstance().Register(this);
        
        // Try to recycle available ID else create another
        if (m_AvalableID.Count != 0)
        {
            m_ID = m_AvalableID.First();
            m_AvalableID.RemoveAt(0);
        }
        else
        {
            m_ID = ++m_StaticID;
        }
    }

    // Unregister the component
    void OnDestroy()
    {
        PickingManager.GetInstance().UnRegister(this);
        m_AvalableID.Add(m_ID);
    }

    // Assign previous value and send ID to the shader. This function is called only in PickingManager
    public void StartPicking(Material pickingMaterial, int pickingLayer)
    {
        m_PreviousLayer = gameObject.layer;
        m_previousMaterial = m_MeshRenderer.materials;

        Material[] matCp = m_MeshRenderer.materials;
        for (int i = 0; i < m_MeshRenderer.materials.Length; i++)
        {
            matCp[i] = pickingMaterial;
        }

        m_MeshRenderer.materials = matCp;

        gameObject.layer = pickingLayer;
        
        // Send ID to the GPU (convert int ID to float ID ranged between 0 and 1)
        float fltID = 1f / m_ID;
        
        for (int i = 0; i < m_MeshRenderer.materials.Length; i++)
            m_MeshRenderer.material.SetFloat("_GameObjectID", fltID);
    }

    // Reassign previous value. This function is called only in PickingManager
    public void EndPicking()
    {
        gameObject.layer = m_PreviousLayer;
        m_MeshRenderer.materials = m_previousMaterial;
    }

    public int GetID()
    {
        return m_ID;
    }
}
