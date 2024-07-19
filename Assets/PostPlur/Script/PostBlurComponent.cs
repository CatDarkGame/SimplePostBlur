using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatDarkGame.Rendering
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Custom Post-processing/PostBlur", typeof(UniversalRenderPipeline))]
    public class PostBlurComponent : VolumeComponent
    {
        [Tooltip("Blur 강력함을 제어합니다.\n0으로 세팅하면 렌더패스가 비활성화됩니다.")]
        public ClampedFloatParameter BlurAmount = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Blur 퍼지는 범위를 설정합니다.")]
        public ClampedFloatParameter BlurOffset = new ClampedFloatParameter(0.1f, 0.01f, 6.0f);
        
        [Space]
        [Tooltip("Blur Mask를 설정합니다. 마스크맵은 R채널만 사용합니다.")]
        public Texture2DParameter BlurMask = new Texture2DParameter(null);
        public Vector2Parameter BlurMask_Tilling = new Vector2Parameter(Vector2.one);
        public Vector2Parameter BlurMask_Offset = new Vector2Parameter(Vector2.zero);
     
        public bool IsActive()
        {
            if (!active ||
                BlurAmount.value <= 0.0f) return false;
            return true;
        }

        public Vector4 GetBlurMaskTilingOffset()
        {
            return new Vector4(BlurMask_Tilling.value.x, BlurMask_Tilling.value.y, 
                BlurMask_Offset.value.x, BlurMask_Offset.value.y);
        }

        public Texture GetBlurMaskTexture()
        {
            return BlurMask.value ? BlurMask.value : Texture2D.whiteTexture;
        }
    }
}