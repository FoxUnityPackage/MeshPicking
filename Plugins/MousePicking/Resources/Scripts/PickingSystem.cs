 using System;
 using System.Collections.Generic;
 using UnityEditor;
 using UnityEngine;
 using UnityEngine.Assertions;
 using UnityEngine.Experimental.Rendering;
 using UnityEngine.Rendering;
 using UnityEngine.UI;

 public enum EPickingFormat
 {
     AUTO,
     R8bit,
     R16bit,
     R32bit
 }
 
 public enum EDepthType
 {
     NO_Z_BUFFER = 0,
     Z_BUFFER_16 = 16,
     Z_BUFFER_24 = 24,
     Z_BUFFER_32 = 32
 }

 public class PickingSystem : MonoBehaviour 
 {
#region Singleton
     protected static PickingSystem m_Instance = null;
     public static PickingSystem Instance
     {
         get
         {
             Debug.Log("instance");
             if (m_Instance == null)
             {
                 m_Instance = FindObjectOfType<PickingSystem>();
                 if (m_Instance == null)
                 {
                     // Create the Picking system
                     GameObject newObj = new GameObject("PickingSystem");
                     m_Instance = Instantiate(newObj).AddComponent<PickingSystem>();
                 }
             }

             return m_Instance;
         }
     }
#endregion

     // Component collection
     protected List<MeshPicking> m_MeshPickingBuffer = new List<MeshPicking>();
     
     // Buffer used to collect the gameObject ID
     protected RenderTexture m_RenderTexture = null;
     
     // Mouse picking shader
     protected Shader m_PickingShader = null;
     
     // Layer used for the picking pass
     protected int m_LayerPicking = -1;
     
     // The render pipeline to use to process the picking pass
     protected MousePickingRenderPipelineAsset m_mousePickingRenderPipeline;
     
     [SerializeField, Range(0, 1)]
     [Tooltip("This value correspond to the ratio of the render texture used compared to the screen size. Less this value is, better performance will be but with less precision")]
     protected float m_RenderTextureScreenRatio = 0.25f;
     
     [Tooltip("The format used for the texture. 8bit = max 255 object. 16bit = max 65535 objects. More the size is, slower the process will be. Use auto to let the system manage it")]
     public EPickingFormat m_Format = EPickingFormat.AUTO;
     
     [Tooltip("The format used for the depth buffer. Highter this value will be, highter prescision you will have but slowest the process will be")]
     public EDepthType m_DepthType = EDepthType.Z_BUFFER_32;
     
     [SerializeField]
     [Tooltip("Use this flag to vertically flip the mouse position ")]
     public bool m_InvertYScreen = false;
     
     [SerializeField]
     [Tooltip("Use this flag to horizontally flip the mouse position ")]
     public bool m_InvertXScreen = false;

     void Awake()
     {
         Debug.Log("Awake");
         if (m_Instance != null && m_Instance != this)
         {
             GameObject.Destroy(m_Instance);
             return;
         }
         else
             m_Instance = this;
         
         DontDestroyOnLoad(this);
         
         m_LayerPicking = LayerMask.NameToLayer("Picking");
         // If asset append here, you need to add "Picking" in the layer.
         // See https://docs.unity3d.com/Manual/Layers.html to understand how to add layer
         // You can also right click on PickingSystem/Install package settings
         Assert.IsFalse(m_LayerPicking == -1, "Layer Picking must be assigned in Layer Manager. You can also right click on PickingSystem/Install package settings");

         // Create the render pipeline to precess picking
         m_mousePickingRenderPipeline = (MousePickingRenderPipelineAsset)ScriptableObject.CreateInstance(typeof(MousePickingRenderPipelineAsset));
         
#if UNITY_EDITOR
         // If asset append here, you need to include shader in your projectSettings/Graphics/AlwaysIncludedShader properties 
         // You can also right click on PickingSystem/Install package settings
         Assert.IsTrue(IsInAlwaysIncludedShaderPropertie("Unlit/PickingShader"), "You need to include shader in your projectSettings/Graphics/AlwaysIncludedShader properties. Else, the shader will not be compiled in your build. You can also right click on PickingSystem/Install package settings");
#endif
         
         m_PickingShader = Shader.Find("Unlit/PickingShader");
         
         // If assert append here, your shader is probably renamed or package is not complete.
         // You can re-download the package to obtain the shader
         Assert.IsNotNull(m_PickingShader, "PickingShader not find");

         // If assert append here, your system don't support current PickingFormat and you need to use another format
         Assert.IsTrue(SystemInfo.SupportsRenderTextureFormat(GetRTFormat()));
         Assert.IsTrue(SystemInfo.SupportsTextureFormat(GetTextureFormat()));
         
     }

     public EPickingFormat ComputeNecessaryFormat()
     {
         if (m_MeshPickingBuffer.Count < byte.MaxValue)
             return EPickingFormat.R8bit;
         else if (m_MeshPickingBuffer.Count < UInt16.MaxValue)
             return EPickingFormat.R16bit;
         else return EPickingFormat.R32bit;
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
     public void Unregister(MeshPicking objectToUnregister)
     {
         m_MeshPickingBuffer.Remove(objectToUnregister);
     }
     
     // Render the scene with the main camera
     void RenderScene()
     {
         // Unbind Camera to use it
         Camera cam = Camera.main;
         Camera.SetupCurrent(null);

         // Save previous settings
         CameraClearFlags previousClearFlag = cam.clearFlags;
         int previousCamMask = cam.cullingMask;
         Color previousBGColor = cam.backgroundColor;
         bool previousAllowMSAA = cam.allowMSAA;
         RenderTexture previousRenderTexture = cam.targetTexture;
         
         // Set camera picking setting
         cam.clearFlags = CameraClearFlags.Color;
         cam.cullingMask = 1 << m_LayerPicking;
         cam.backgroundColor = Color.black;
         cam.allowMSAA = false;

         CreateRenderTexture(cam.pixelWidth, cam.pixelHeight);
         cam.targetTexture = m_RenderTexture;
         
         // change current render pipeline for simplest RP
         RenderPipelineAsset previousRP = GraphicsSettings.renderPipelineAsset;
         GraphicsSettings.renderPipelineAsset = m_mousePickingRenderPipeline;
         
         // Render the picking scene
         cam.Render();
         
         // Reset previous setting
         GraphicsSettings.renderPipelineAsset = previousRP;
         cam.clearFlags = previousClearFlag;
         cam.cullingMask = previousCamMask;
         cam.backgroundColor = previousBGColor;
         cam.allowMSAA = previousAllowMSAA;
         cam.targetTexture = previousRenderTexture;
         
         // Rebind the camera
         Camera.SetupCurrent(cam);
     }

     byte DecodeFloatToUI8(float f)
     {
         return (byte)(Mathf.Round(f * byte.MaxValue));
     }
     
     UInt16 DecodeFloatToUI16(float f)
     {
         return (UInt16)(Mathf.Round(f * UInt16.MaxValue));
     }
     
     UInt32 DecodeFloatToUI32(float f)
     {
         return (UInt32)(Mathf.Round(f * UInt32.MaxValue));
     }

     RenderTextureFormat GetRTFormat()
     {
         EPickingFormat format = m_Format == EPickingFormat.AUTO ? ComputeNecessaryFormat() : m_Format; 
         switch (format)
         {
             case EPickingFormat.R8bit:
                 return RenderTextureFormat.R8;
             case EPickingFormat.R16bit:
                 return RenderTextureFormat.RHalf;
             case EPickingFormat.R32bit:
                 return RenderTextureFormat.RFloat;
             default:
                 throw new ArgumentOutOfRangeException();
         }
     }
     
     TextureFormat GetTextureFormat()
     {
         EPickingFormat format = m_Format == EPickingFormat.AUTO ? ComputeNecessaryFormat() : m_Format; 
         switch (format)
         {
             case EPickingFormat.R8bit:
                 return TextureFormat.R8;
             case EPickingFormat.R16bit:
                 return TextureFormat.RHalf;
             case EPickingFormat.R32bit:
                 return TextureFormat.RFloat;
             default:
                 throw new ArgumentOutOfRangeException();
         }
     }

     // Create the render texture based on the setting
     void CreateRenderTexture(float camWidth, float camHeight)
     {
         int width = (int)(camWidth * m_RenderTextureScreenRatio);
         int height = (int)(camHeight * m_RenderTextureScreenRatio);
         m_RenderTexture = RenderTexture.GetTemporary(width, height, 0, GetRTFormat());
         m_RenderTexture.filterMode = FilterMode.Point;
         m_RenderTexture.depth = (int)m_DepthType;
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
         {
             Material newMat = new Material(m_PickingShader);
             m_MeshPickingBuffer[i].StartPicking(newMat, m_LayerPicking, m_Format == EPickingFormat.AUTO ? ComputeNecessaryFormat() : m_Format);
         }

         // Render the picking scene
         RenderScene();

         // Make a new texture and read the active Render Texture into it.
        Texture2D image = new Texture2D(1, 1, GetTextureFormat(), false);

         // Compute the position in render texture referential
         float finalPosX = m_RenderTexture.width * position.x / Screen.width;
         if (m_InvertXScreen)
             finalPosX = Screen.width - finalPosX;
         float finalPosY = m_RenderTexture.height * position.y / Screen.height;
         if (m_InvertYScreen)
             finalPosY = Screen.height - finalPosY;
         
         // Read the desired pixel
         RenderTexture.active = m_RenderTexture;
         image.ReadPixels(new Rect(finalPosX, finalPosY, 1, 1), 0, 0, false);

         // Reassign previous renderTexture
         RenderTexture.ReleaseTemporary(m_RenderTexture);
         RenderTexture.active = previousRT;
         
         // Convert float ID ranged between 0 and 1 to int ID
         uint pixelID;
         switch (m_Format == EPickingFormat.AUTO ? ComputeNecessaryFormat() : m_Format)
         {
             case EPickingFormat.R8bit:
                 pixelID = DecodeFloatToUI8(image.GetPixel(0, 0).r);
                 break;
             case EPickingFormat.R16bit:
                 pixelID = DecodeFloatToUI16(image.GetPixel(0, 0).r);
                 break;
             case EPickingFormat.R32bit:
                 pixelID = DecodeFloatToUI32(image.GetPixel(0, 0).r);
                 break;
             default:
                 throw new ArgumentOutOfRangeException();
         }

         GameObject target = null;

         Debug.Log(m_MeshPickingBuffer.Count + "    " + pixelID);
         
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

#if UNITY_EDITOR
     static bool IsInAlwaysIncludedShaderPropertie(string shaderName)
     {
         var shader = Shader.Find(shaderName);
         if (shader == null)
             return false;
     
         var graphicsSettingsObj = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
         var serializedObject = new SerializedObject(graphicsSettingsObj);
         var arrayProp = serializedObject.FindProperty("m_AlwaysIncludedShaders");
         bool hasShader = false;
         
         for (int i = 0; i < arrayProp.arraySize; ++i)
         {
             var arrayElem = arrayProp.GetArrayElementAtIndex(i);
             if (shader == arrayElem.objectReferenceValue)
             {
                 hasShader = true;
                 break;
             }
         }

         return hasShader;
     }

     [ContextMenu("Install package settings")]
     protected void Install()
     {
         AddAlwaysIncludedShader("Unlit/PickingShader");
         AddLayer("Picking");
         AssetDatabase.SaveAssets();
         Debug.Log("Installation complete");
     }
     
     [ContextMenu("Uninstall package Settings")]
     protected void Uninstall()
     {
         RemoveAlwaysIncludedShader("Unlit/PickingShader");
         RemoveLayer("Picking");
         AssetDatabase.SaveAssets();
         Debug.Log("Uninstall complete");
     }

     void AddAlwaysIncludedShader(string shaderName)
     {
         var shader = Shader.Find(shaderName);
         if (shader == null)
             return;
     
         var graphicsSettingsObj = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
         var serializedObject = new SerializedObject(graphicsSettingsObj);
         var arrayProp = serializedObject.FindProperty("m_AlwaysIncludedShaders");
         bool hasShader = false;
         for (int i = 0; i < arrayProp.arraySize; ++i)
         {
             var arrayElem = arrayProp.GetArrayElementAtIndex(i);
             if (shader == arrayElem.objectReferenceValue)
             {
                 hasShader = true;
                 break;
             }
         }
     
         if (!hasShader)
         {
             int arrayIndex = arrayProp.arraySize;
             arrayProp.InsertArrayElementAtIndex(arrayIndex);
             var arrayElem = arrayProp.GetArrayElementAtIndex(arrayIndex);
             arrayElem.objectReferenceValue = shader;
     
             serializedObject.ApplyModifiedProperties();
             Debug.Log("Shader " + shaderName + " add to AlwaysIncludedShaders");
         }
     }
     
     void AddLayer(string layerName)
     {
         // Open tag manager
         SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
         SerializedProperty layersProp = tagManager.FindProperty("layers");

         // First check if it is not already present
         bool hasLayer = false;
         int indexFree = -1;
         for (int i = 0; i < layersProp.arraySize; i++)
         {
             SerializedProperty t = layersProp.GetArrayElementAtIndex(i);
             if (t.stringValue.Equals(layerName))
             {
                 hasLayer = true;
                 break;
             }
             else if (indexFree == -1 && t.stringValue.Equals(""))
             {
                 indexFree = i;
             }
         }

         if (!hasLayer)
         {
             layersProp.GetArrayElementAtIndex(indexFree).stringValue = layerName;
             tagManager.ApplyModifiedProperties();
             
             Debug.Log("Layer " + layerName + " add to layers");
         }
     }
     
     void RemoveAlwaysIncludedShader(string shaderName)
     {
         var shader = Shader.Find(shaderName);
         if (shader == null)
             return;
     
         var graphicsSettingsObj = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
         var serializedObject = new SerializedObject(graphicsSettingsObj);
         var arrayProp = serializedObject.FindProperty("m_AlwaysIncludedShaders");

         for (int i = 0; i < arrayProp.arraySize; ++i)
         {
             var arrayElem = arrayProp.GetArrayElementAtIndex(i);
             if (shader == arrayElem.objectReferenceValue)
             {
                 arrayProp.DeleteArrayElementAtIndex(i);
                 serializedObject.ApplyModifiedProperties();
                 Debug.Log("Shader " + shaderName + " remove to AlwaysIncludedShaders");
                 break;
             }
         }
     }
     
     void RemoveLayer(string layerName)
     {
         // Open tag manager
         SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
         SerializedProperty layersProp = tagManager.FindProperty("layers");

         // First check if it is not already present
         for (int i = 0; i < layersProp.arraySize; i++)
         {
             SerializedProperty t = layersProp.GetArrayElementAtIndex(i);
             if (t.stringValue.Equals(layerName))
             {
                 layersProp.GetArrayElementAtIndex(i).stringValue = "";
                 tagManager.ApplyModifiedProperties();
                 Debug.Log("Layer " + layerName + " remove to layers");
                 break;
             }
         }
     }
#endif
 }