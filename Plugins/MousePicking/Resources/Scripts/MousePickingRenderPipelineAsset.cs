using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
    
// The CreateAssetMenu attribute lets you create instances of this class in the Unity Editor.
[CreateAssetMenu(menuName = "Rendering/MousePickingRenderPipelineAsset")]
public class MousePickingRenderPipelineAsset : RenderPipelineAsset
{
    public Material m_MousePickingMaterial;
    
    // Unity calls this method before rendering the first frame.
    // If a setting on the Render Pipeline Asset changes, Unity destroys the current Render Pipeline Instance and calls this method again before rendering the next frame.
    protected override RenderPipeline CreatePipeline()
    {
        // Instantiate the Render Pipeline that this custom SRP uses for rendering.
        
        Shader pickingShader = Shader.Find("Unlit/PickingShader");
        
        // If assert append here, your shader is probably renamed or package is not complete.
        // You can re-download the package to obtain the shader
        Assert.IsNotNull(pickingShader, "PickingShader not find");
        m_MousePickingMaterial = new Material(pickingShader);

        return new MousePickingRenderPipelineInstance(this);
    }
}