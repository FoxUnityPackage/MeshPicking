using UnityEngine;

public class PrimitiveSpawner : MonoBehaviour
{
    public PrimitiveType m_MeshType;
    public int m_Count;
    public int m_Radius;
    
    // Start is called before the first frame update
    void Start()
    {
        GameObject cube = GameObject.CreatePrimitive(m_MeshType);
        cube.AddComponent<MeshPicking>();
        
        for (int i = 0; i < m_Count; i++)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * m_Radius;
            Instantiate(cube, pos, Random.rotation, transform);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
