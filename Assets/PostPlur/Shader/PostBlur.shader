Shader "Hidden/PostProcessing/PostBlur"
{
    Properties
    {
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
		ZTest Always ZWrite Off Cull Off

		HLSLINCLUDE
            #pragma target 2.5
            #include "PostBlur_Pass.hlsl"
        ENDHLSL

		Pass    // 0
        {
            Name "PostBlur"
            Blend One Zero	

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment PostBlurFrag
            ENDHLSL
        }
        
        Pass    // 1
        {
            Name "PostBlur_DownSample"
            Blend One Zero	
            
            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment DownSampleFrag
            ENDHLSL
        }
    }
}