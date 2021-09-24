using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
    
public class MousePickingRenderPipelineInstance : RenderPipeline
{
    
    protected ScriptableRenderContext m_Context;
    protected Camera m_Camera;
    
    // Use this variable to a reference to the Render Pipeline Asset that was passed to the constructor
    protected MousePickingRenderPipelineAsset RPData;
    
    static ShaderTagId m_ShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    const string m_BufferName = "MousePickingRenderPipeline";
    // Create and schedule the RP commands 
    CommandBuffer m_Buffer = new CommandBuffer {name = m_BufferName};
    CullingResults m_CullingResults;
    
    // The constructor has an instance of the ExampleRenderPipelineAsset class as its parameter.
    public MousePickingRenderPipelineInstance(MousePickingRenderPipelineAsset asset)
    {
        RPData = asset;
    }
    
    void Setup ()
    {
        m_Context.SetupCameraProperties(m_Camera);
        m_Buffer.ClearRenderTarget(true, true, Color.clear);
        m_Buffer.BeginSample(m_BufferName);
        ExecuteBuffer();
    }

    void Submit ()
    {
        m_Buffer.EndSample(m_BufferName);
        m_Context.Submit();
    }
    
    void ExecuteBuffer ()
    {
        m_Context.ExecuteCommandBuffer(m_Buffer);
        m_Buffer.Clear();
    }
    
    bool Cull ()
    {
        // Get the culling parameters from the current Camera
        if (m_Camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
        {
            // Get the culling parameters from the current Camera
            m_CullingResults = m_Context.Cull(ref cullingParameters);

            return true;
        }
        return false;
    }
    
    void DrawVisibleGeometry ()
    {
        // Tell Unity how to sort the geometry, based on the current Camera
        SortingSettings sortingSettings = new SortingSettings(m_Camera);
        
        // Create a DrawingSettings struct that describes which geometry to draw and how to draw it
        DrawingSettings drawingSettings = new DrawingSettings(m_ShaderTagId, sortingSettings);
        //drawingSettings.overrideMaterial = RPData.m_MousePickingMaterial;
        
        // Tell Unity how to filter the culling results, to further specify which geometry to draw
        // Use FilteringSettings.defaultValue to specify no filtering
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
        
        m_Context.DrawRenderers(m_CullingResults, ref drawingSettings, ref filteringSettings);
    }
    
    public void Render (ScriptableRenderContext context, Camera camera)
    {
        m_Context = context;
        m_Camera = camera;

        Setup();
        
        if (!Cull())
            return;

        DrawVisibleGeometry();
        
        // Instruct the graphics API to perform all scheduled commands
        Submit();
    }
    
    protected override void Render (ScriptableRenderContext context, Camera[] cameras)
    {
        // This ise where you can write custom rendering code. Customize this method to customiz your SRP.
        Render(context, Camera.current);
    }
}