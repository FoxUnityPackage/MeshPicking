using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
    
// The CreateAssetMenu attribute lets you create instances of this class in the Unity Editor.
public class MousePickingRenderPipelineAsset : RenderPipelineAsset
{
    // Unity calls this method before rendering the first frame.
    // If a setting on the Render Pipeline Asset changes, Unity destroys the current Render Pipeline Instance and calls this method again before rendering the next frame.
    protected override RenderPipeline CreatePipeline()
    {
        return new MousePickingRenderPipelineInstance(this);
    }
}