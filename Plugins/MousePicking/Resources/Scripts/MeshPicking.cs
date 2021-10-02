using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Renderer)), DisallowMultipleComponent]
public class MeshPicking : MonoBehaviour
{
    protected Renderer m_MeshRenderer;
    protected static List<uint> m_AvalableID = new List<uint>();
    protected static uint m_StaticID = 0;
    protected uint m_ID = 0;
    protected int m_PreviousLayer = -1;
    protected Material[] m_previousMaterial;

    private void OnEnable()
    {
        PickingSystem.Instance.Register(this);
    }

    private void OnDisable()
    {
        PickingSystem.Instance.Unregister(this);
    }

    void Start()
    {
        // Register the component
        m_MeshRenderer = GetComponent<Renderer>();
        
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
        PickingSystem.Instance.Unregister(this);
        m_AvalableID.Add(m_ID);
    }

    float EncodeUI8ToFloat(byte value)
    {
        return value / (float)byte.MaxValue;
    }
    
    float EncodeUI16ToFloat(UInt16 value)
    {
        return value / (float)UInt16.MaxValue;
    }
    
    float EncodeUI32ToFloat(UInt32 value)
    {
        return value / (float)UInt32.MaxValue;
    }
    
    // Assign previous value and send ID to the shader. This function is called only in PickingSystem
    public void StartPicking(Material pickingMaterial, int pickingLayer, EPickingFormat pickingFormat)
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

        float normalizedID;
        switch (pickingFormat)
        {
            case EPickingFormat.R8bit:
                normalizedID = EncodeUI8ToFloat((byte)m_ID);
                break;
            case EPickingFormat.R16bit:
                normalizedID = EncodeUI16ToFloat((UInt16)m_ID);
                break;
            case EPickingFormat.R32bit:
                normalizedID = EncodeUI32ToFloat(m_ID);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(pickingFormat), pickingFormat, null);
        }
        
        // Send ID to the GPU (convert int ID to float ID ranged between 0 and 1)
        for (int i = 0; i < m_MeshRenderer.materials.Length; i++)
            m_MeshRenderer.material.SetFloat("_GameObjectID", normalizedID);
    }

    // Reassign previous value. This function is called only in PickingSystem
    public void EndPicking()
    {
        gameObject.layer = m_PreviousLayer;
        m_MeshRenderer.materials = m_previousMaterial;
    }

    public uint GetID()
    {
        return m_ID;
    }
}
