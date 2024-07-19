#ifndef CATDARKGAME_POSTPROCESSING_POSTBLUR_PASS
#define CATDARKGAME_POSTPROCESSING_POSTBLUR_PASS

#include "PostBlur_Input.hlsl"

struct Attributes
{
    uint vertexID     : SV_VertexID;
    float4 positionOS : POSITION;
    float2 uv         : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 uv         : TEXCOORD0;
};

Varyings FullscreenVert(Attributes input)
{
    Varyings output;
    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);

    float2 uv = (output.positionCS * 0.5f + 0.5f).xy;
    #if UNITY_UV_STARTS_AT_TOP
        uv.y = 1.0f - uv.y;
    #endif

    output.uv.xy = uv;
    output.uv.zw = uv * DEF_MASK_ST.xy + DEF_MASK_ST.zw;
    return output;
}

half4 PostBlurFrag (Varyings i) : SV_Target
{
    float2 uv = i.uv.xy;
    float2 uv_Mask = i.uv.zw;
    half blurMask = BlurMask_Texture(uv_Mask, _MaskTex, sampler_MaskTex);
    half3 sourceCol = SAMPLE_TEXTURE2D(_SourceTex, sampler_linear_clamp, uv).rgb;
    half3 blurCol = SAMPLE_TEXTURE2D(_BlurTex, sampler_linear_clamp, uv).rgb;

    half3 result = lerp(sourceCol, blurCol, blurMask * _Amount);
    return half4(result, 1.0h);
}

half4 DownSampleFrag (Varyings i) : SV_Target
{
    float2 uv = i.uv;
    float offset = DEF_BLUROFFSET;
    half3 tex_1 = SAMPLE_TEXTURE2D(_DownSampleTex, sampler_DownSampleTex, uv + float2(_DownSampleTex_TexelSize.x * -offset, 0.0f)).rgb;
    half3 tex_2 = SAMPLE_TEXTURE2D(_DownSampleTex, sampler_DownSampleTex, uv + float2(_DownSampleTex_TexelSize.x * offset, 0.0f)).rgb;
    half3 tex_3 = SAMPLE_TEXTURE2D(_DownSampleTex, sampler_DownSampleTex, uv + float2(0.0f, _DownSampleTex_TexelSize.y * -offset)).rgb;
    half3 tex_4 = SAMPLE_TEXTURE2D(_DownSampleTex, sampler_DownSampleTex, uv + float2(0.0f, _DownSampleTex_TexelSize.y * offset)).rgb;

    half3 result = (tex_1 + tex_2 + tex_3 + tex_4) * 0.25h;
    return half4(result, 1.0h);
}

#endif