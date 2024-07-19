using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatDarkGame.Rendering
{
    public class PostBlurRenderPass : ScriptableRenderPass
    {
        private static class ShaderPropertyId
        {
            public static readonly int BlitTexture = Shader.PropertyToID("_SourceTex");
            public static readonly int BlitTexture_Blur = Shader.PropertyToID("_BlurTex");
            public static readonly int BlitTexture_Down = Shader.PropertyToID("_DownSampleTex");
            
            public static readonly int Amount = Shader.PropertyToID("_Amount");
            public static readonly int BlurOffset = Shader.PropertyToID("_BlurOffset");
            public static readonly int BlurMask = Shader.PropertyToID("_MaskTex");
            public static readonly int BlurMask_ST = Shader.PropertyToID("_MaskTex_TilingOffset");
        }
        
        private static MaterialPropertyBlock k_PropertyBlock = new MaterialPropertyBlock();
        private string RenderTag { get; }
        private ProfilingSampler _profilingSampler;
        
        private RTHandle[] _downSampleTextures;
        private RTHandle _sourceTexture;
        private RTHandle _destTexture;
        private PostBlurComponent _component;
        
        private int _blurIteration = 2;
        private int _stepCount;
        private FilterMode _filterMode;
        private Material _material;

        public PostBlurRenderPass(Shader shader, string renderTag, RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing, FilterMode filterMode = FilterMode.Bilinear, int blurIteration = 2)
        {
            if (!shader) return;
            if(!_material) _material = CoreUtils.CreateEngineMaterial(shader);
            
            RenderTag = renderTag;
            renderPassEvent = passEvent;
            _profilingSampler = new ProfilingSampler(RenderTag);
            _filterMode = filterMode;
            _blurIteration = blurIteration;
            _stepCount = Mathf.Max(_blurIteration * 2 - 1, 1);
            _downSampleTextures = new RTHandle[_stepCount + 1];
        } 
        
        public void Dispose()
        {
            DisposeRT();
            if(_material) CoreUtils.Destroy(_material);
            _material = null;
        }

        private void DisposeRT()
        {
            if (_downSampleTextures != null)
            {
                for (int i = 0; i < _downSampleTextures.Length; i++)
                {
                    _downSampleTextures[i]?.Release();
                }
            }
            _destTexture?.Release();
            _sourceTexture?.Release();
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            SetupVolumeComponent();
            if (!CheckRenderingState(ref renderingData))
            {
                DisposeRT();
                return;
            }
            ResetTarget();
       
            int screenWidth = renderingData.cameraData.cameraTargetDescriptor.width;
            int screenHeight = renderingData.cameraData.cameraTargetDescriptor.height;
            int screenWidth_Down = screenWidth;
            int screenWidth_Height = screenHeight;
            int iteration = _blurIteration;
            int stepCount = _stepCount;
            
            for (int i = 0; i < stepCount; i++)
            {
                int downsampleIndex = SimplePingPong(i, iteration - 1);
                screenWidth_Down = screenWidth >> downsampleIndex + 1;
                screenWidth_Height = screenHeight >> downsampleIndex + 1;
                    
                RenderTextureDescriptor descriptor = GetCompatibleDescriptor(screenWidth_Down, screenWidth_Height, true);
                descriptor.graphicsFormat = renderingData.cameraData.cameraTargetDescriptor.graphicsFormat;
                RenderingUtils.ReAllocateIfNeeded(ref _downSampleTextures[i], descriptor, _filterMode, TextureWrapMode.Clamp);
            }
            
            RenderTextureDescriptor descriptor_Source = GetCompatibleDescriptor(screenWidth, screenHeight, true);
            descriptor_Source.graphicsFormat = renderingData.cameraData.cameraTargetDescriptor.graphicsFormat;
            RenderingUtils.ReAllocateIfNeeded(ref _downSampleTextures[GetLastDownSampleRT()], descriptor_Source, _filterMode, TextureWrapMode.Clamp);
            RenderingUtils.ReAllocateIfNeeded(ref _sourceTexture, descriptor_Source, FilterMode.Bilinear, TextureWrapMode.Clamp);
            RenderingUtils.ReAllocateIfNeeded(ref _destTexture, descriptor_Source, FilterMode.Bilinear, TextureWrapMode.Clamp);
           
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!CheckRenderingState(ref renderingData)) return;
            ref CameraData cameraData = ref renderingData.cameraData;
            
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                Render(cmd, ref cameraData);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref CameraData cameraData)
        {
            RTHandle sourceHandle = cameraData.renderer.cameraColorTargetHandle;
            RTHandle targetHandle = sourceHandle;
            
            int stepCount = _stepCount;
            for (int i = 0; i < stepCount + 1; i++)
            {
                k_PropertyBlock.SetTexture(ShaderPropertyId.BlitTexture_Down, targetHandle);
                CoreUtils.SetRenderTarget(cmd, _downSampleTextures[i], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.DrawProcedural(Matrix4x4.identity, _material, 1, MeshTopology.Triangles, 3, 1, k_PropertyBlock);
                targetHandle = _downSampleTextures[i];
            }

            {
                k_PropertyBlock.SetTexture(ShaderPropertyId.BlurMask, _component.GetBlurMaskTexture());
                k_PropertyBlock.SetVector(ShaderPropertyId.BlurMask_ST, _component.GetBlurMaskTilingOffset());
                k_PropertyBlock.SetFloat(ShaderPropertyId.Amount, _component.BlurAmount.value);
                k_PropertyBlock.SetFloat(ShaderPropertyId.BlurOffset, _component.BlurOffset.value);
                k_PropertyBlock.SetTexture(ShaderPropertyId.BlitTexture, _sourceTexture);
                k_PropertyBlock.SetTexture(ShaderPropertyId.BlitTexture_Blur, targetHandle);
                
                CoreUtils.SetRenderTarget(cmd, _sourceTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                Blitter.BlitTexture(cmd, sourceHandle, new Vector4(1, 1, 0, 0), 0.0f, true);
                CoreUtils.SetRenderTarget(cmd, sourceHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3, 1, k_PropertyBlock);
            }
        }

        private int GetLastDownSampleRT()
        {
            if (_downSampleTextures == null) return -1;
            return _downSampleTextures.Length - 1;
        }
        
        private bool CheckRenderingState(ref RenderingData renderingData)
        {
            if (!renderingData.cameraData.postProcessEnabled ||
                !_material) return false;
            if (!_component || !_component.IsActive()) return false;
            return true;
        }
        
        private void SetupVolumeComponent()
        {
            VolumeStack volumeStack = VolumeManager.instance.stack;
            _component = volumeStack.GetComponent<PostBlurComponent>();
        }
        
        private static int SimplePingPong(int t, int max)
        {
            if (t > max) return 2 * max - t;
            return t;
        }

        private static RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, bool isHdrEnabled, int depthBufferBits = 0)
        {
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(width, height);
            descriptor.msaaSamples = 1;
            descriptor.graphicsFormat = MakeRenderTextureGraphicsFormat(isHdrEnabled);
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = depthBufferBits;
            return descriptor;
        }

        private static GraphicsFormat MakeRenderTextureGraphicsFormat(bool isHdrEnabled)
        {
            return SystemInfo.GetGraphicsFormat(isHdrEnabled ? DefaultFormat.HDR : DefaultFormat.LDR);
        }
    }
}