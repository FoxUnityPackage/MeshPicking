 using System;
 using System.Collections.Generic;
 using JetBrains.Annotations;
 using UnityEngine;
 using UnityEngine.Assertions;

 public enum EPickingFormat
 {
     R8bit,
     R16bit
 }

 public class PickingManager : MonoBehaviour 
 {

     // Singleton
     protected static PickingManager m_Instance = null;
     protected List<MeshPicking> m_MeshPickingBuffer = new List<MeshPicking>();
     protected RenderTexture m_RenderTexture = null;
     
     protected Shader m_PickingShader = null;
     protected int m_LayerPicking = -1;
     protected Camera m_Cam;
     
     [SerializeField, Range(0, 1)]
     [Tooltip("This value correspond to the ratio of the render texture used compared to the screen size. Less this value is, better performance will be but with less precision")]
     protected float m_RenderTextureScreenRatio = 0.5f;
     
     [SerializeField]
     [Tooltip("The format used for the texture. 8bit = max 255 object. 16bit = max 65535 objects. More the size is, slower the process will be")]
     protected EPickingFormat m_Format = EPickingFormat.R16bit;
     
     void Awake()
     {
         if(m_Instance != null)
             GameObject.Destroy(m_Instance);
         else
             m_Instance = this;
         
         DontDestroyOnLoad(this);

         m_LayerPicking = LayerMask.NameToLayer("Picking");
         // If asset append here, you need to add "Picking" in the layer.
         // See https://docs.unity3d.com/Manual/Layers.html to understand how to add layer
         Assert.IsFalse(m_LayerPicking == -1, "Layer Picking must be assigned in Layer Manager");
         
         m_Cam = GetComponent<Camera>();
     }

     void Start()
     {
         m_PickingShader = Shader.Find("Unlit/PickingShader");
         // If assert append here, your shader is probably renamed or package is not complete.
         // You can re-download the package to obtain the shader
         Assert.IsNotNull(m_PickingShader, "PickingShader not find");
     }

     public static PickingManager GetInstance()
     {
         return m_Instance;
     }

     // This function must be called only by MeshPicking them onto the system
     // Safe register check if MeshPicking don't already exist
     public void SafeRegister(MeshPicking objectToRegister)
     {
         // Make sur that object doesn't already exist
         m_MeshPickingBuffer.Remove(objectToRegister);
         
         m_MeshPickingBuffer.Add(objectToRegister);
     }
     
     // This function must be called only by MeshPicking to register them onto the system
     public void Register(MeshPicking objectToRegister)
     {
         m_MeshPickingBuffer.Add(objectToRegister);
     }
     
     // This function must be called only by MeshPicking to unregister them onto the system
     public void UnRegister(MeshPicking objectToRegister)
     {
         m_MeshPickingBuffer.Remove(objectToRegister);
     }

     // Copy the camera property and assign value for the picking
     void CreateCamera()
     {
         m_Cam.CopyFrom(Camera.main);
         m_Cam.clearFlags = CameraClearFlags.SolidColor;
         m_Cam.cullingMask = 1 << m_LayerPicking;
         m_Cam.backgroundColor = Color.black;
         m_Cam.allowMSAA = false;
     }

     // Create the render texture based on the setting
     void CreateRenderTexture()
     { 
         int width = (int)(m_Cam.pixelWidth * m_RenderTextureScreenRatio);
         int height = (int)(m_Cam.pixelHeight * m_RenderTextureScreenRatio);
         m_RenderTexture = new RenderTexture(width, height, 0, m_Format == EPickingFormat.R8bit ? RenderTextureFormat.R8 : RenderTextureFormat.R16);
         m_RenderTexture.filterMode = FilterMode.Point;
     }

     // Call this function to now if the an object is clicked by the user. Example : 
     // if (Input.GetMouseButtonDown(0))
     //{
     //     GameObject obj = Picking(Input.mousePosition);
     //     if (obj)
     //         obj.SetActive(false);
     //}
     // Position represent the position of the cursor on the screen.
     // Return the gameObject selected or null
     public GameObject Picking(Vector2 position)
     {
         // Get previous renderTexture
         RenderTexture previousRT = RenderTexture.active;
         
         // Init picking process in each meshPicking component
         for (int i = 0; i < m_MeshPickingBuffer.Count; i++)
             m_MeshPickingBuffer[i].StartPicking(new Material(m_PickingShader), m_LayerPicking);

         // Create camera and render target and bind it
         CreateCamera();
         CreateRenderTexture();
         m_Cam.targetTexture = m_RenderTexture;

         // Render the picking scene
         m_Cam.Render();
         
         // Make a new texture and read the active Render Texture into it.
         Texture2D image = new Texture2D(1, 1, m_Format == EPickingFormat.R8bit ? TextureFormat.R8 : TextureFormat.R16, false);
         
         // Compute the position in render texture referential
         float finalPosX = m_RenderTexture.width * position.x / Screen.width;
         float finalPosY = m_RenderTexture.height * (Screen.height - position.y) / Screen.height;
         
         // Read the desired pixel
         RenderTexture.active = m_RenderTexture;
         image.ReadPixels(new Rect(finalPosX, finalPosY, 1, 1), 0, 0, false);

         // Reassign previous renderTexture
         RenderTexture.active = previousRT;
         
         // Convert float ID ranged between 0 and 1 to int ID
         int pixelID = image.GetPixel(0, 0).r == 0f ? 0 : (int)Math.Round(1f / image.GetPixel(0, 0).r);
         GameObject target = null;
         
         for (int i = 0; i < m_MeshPickingBuffer.Count; i++)
         {
             // Reassign previous layers and materials
             m_MeshPickingBuffer[i].EndPicking();
             
             // Check if ID correspond to the pixelID
             if (m_MeshPickingBuffer[i].GetID() == pixelID)
                 target = m_MeshPickingBuffer[i].gameObject;
         }

         return target;
     }
 }