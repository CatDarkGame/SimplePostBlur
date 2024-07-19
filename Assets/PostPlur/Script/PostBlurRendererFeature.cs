using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CatDarkGame.Rendering
{
    public class PostBlurRendererFeature : ScriptableRendererFeature
    {
        private const string k_ShaderName = "Hidden/PostProcessing/PostBlur";
        private const string k_RenderTag = "PostBlurRenderPass";

        [SerializeField] 
        private FilterMode filterMode = FilterMode.Point;
        [SerializeField][Range(1, 8)] 
        private int sampleCount = 2;
        [SerializeField] 
        private RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        [HideInInspector][SerializeField] 
        private Shader _shader;
        
        private PostBlurRenderPass _rendererPass;

        public override void Create()
        {
            #if UNITY_EDITOR
                if (!_shader) _shader = Shader.Find(k_ShaderName);
            #endif
            _rendererPass = new PostBlurRenderPass(_shader, k_RenderTag, renderPassEvent, filterMode, sampleCount);
        }
        
        protected override void Dispose(bool disposing)
        {
            _rendererPass?.Dispose();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game ||
                renderingData.cameraData.isPreviewCamera ||
                renderingData.cameraData.isSceneViewCamera) return;
            if (renderPassEvent == RenderPassEvent.AfterRendering) return;
            renderer.EnqueuePass(_rendererPass);
        }
    }
}