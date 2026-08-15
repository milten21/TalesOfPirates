Shader "Top/Water"
{
    Properties
    {
        _Frames("Frames", 2DArray) = "" {}
        _FrameCount("Frame count", Float) = 30
        _FramesPerSecond("Frames per second", Float) = 30
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_ARRAY(_Frames);
            SAMPLER(sampler_Frames);
            float _FrameCount;
            float _FramesPerSecond;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float frame = fmod(floor(_Time.y * _FramesPerSecond), _FrameCount);
                half3 water = SAMPLE_TEXTURE2D_ARRAY(_Frames, sampler_Frames, input.uv, frame).rgb;
                return half4(water * input.color.rgb, input.color.a);
            }
            ENDHLSL
        }
    }
}
