Shader "Top/Terrain"
{
    Properties
    {
        _Textures("Textures", 2DArray) = "" {}
        _Masks("Masks", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D_ARRAY(_Textures);
            SAMPLER(sampler_Textures);
            TEXTURE2D(_Masks);
            SAMPLER(sampler_Masks);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float4 baseMask1Uv : TEXCOORD0;
                float4 mask2Mask3Uv : TEXCOORD1;
                float4 slices : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 baseMask1Uv : TEXCOORD0;
                float4 mask2Mask3Uv : TEXCOORD1;
                nointerpolation float4 slices : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.baseMask1Uv = input.baseMask1Uv;
                output.mask2Mask3Uv = input.mask2Mask3Uv;
                output.slices = input.slices;
                output.color = input.color;
                return output;
            }

            half3 Detail(half3 color, float2 uv, float2 maskUv, float slice)
            {
                half mask = SAMPLE_TEXTURE2D(_Masks, sampler_Masks, maskUv).a * step(0.5, slice);
                half3 detail = SAMPLE_TEXTURE2D_ARRAY(_Textures, sampler_Textures, uv, slice).rgb;
                return lerp(color, detail, mask);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.baseMask1Uv.xy;
                half3 color = SAMPLE_TEXTURE2D_ARRAY(_Textures, sampler_Textures, uv, input.slices.x).rgb;
                color = Detail(color, uv, input.baseMask1Uv.zw, input.slices.y);
                color = Detail(color, uv, input.mask2Mask3Uv.xy, input.slices.z);
                color = Detail(color, uv, input.mask2Mask3Uv.zw, input.slices.w);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS), input.positionWS,
                    half4(1, 1, 1, 1));
                half3 shadow = lerp(SampleSH(half3(0, 1, 0)), half3(1, 1, 1), mainLight.shadowAttenuation);

                return half4(color * input.color.rgb * shadow, 1);
            }
            ENDHLSL
        }
    }
}
