#ifndef CATDARKGAME_POSTPROCESSING_POSTBLUR_INPUT
#define CATDARKGAME_POSTPROCESSING_POSTBLUR_INPUT

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#define DEF_BLURAMOUNT _Amount
#define DEF_BLUROFFSET _BlurOffset
#define DEF_MASK_ST _MaskTex_TilingOffset

TEXTURE2D(_MaskTex);        SAMPLER(sampler_MaskTex);
TEXTURE2D(_SourceTex);      SAMPLER(sampler_linear_clamp); 
TEXTURE2D(_BlurTex); 
TEXTURE2D(_DownSampleTex);  SAMPLER(sampler_DownSampleTex);
float4 _DownSampleTex_TexelSize;

half _Amount;
half _BlurOffset;
float4 _MaskTex_TilingOffset;


half BlurMask_Texture(float2 uv, TEXTURE2D(blurMaskMap), SAMPLER(sampler_blurMaskMap))
{
    return SAMPLE_TEXTURE2D(blurMaskMap, sampler_blurMaskMap, uv).r;
}

half ProceduralBlurMask(float2 uv, half maskPow, half maskIntensity)
{
    half blurMask = abs(uv.y * 2.0 - 1.0);
    return saturate(pow(blurMask, maskPow) * maskIntensity);
}

#endif