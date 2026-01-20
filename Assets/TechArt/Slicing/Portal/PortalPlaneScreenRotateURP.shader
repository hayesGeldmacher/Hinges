Shader "Unlit/PortalPlaneScreenRotateURP"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float _AngleRad;        // 旋转角度（弧度）
            float2 _Pivot;          // 屏幕空间中心 (0-1)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float2 RotateUV(float2 uv, float2 pivot, float angle)
            {
                // aspect 修正（关键）
                float aspect = _ScreenParams.x / _ScreenParams.y;

                float2 p = uv - pivot;
                p.x *= aspect;

                float s = sin(angle);
                float c = cos(angle);
                p = float2(
                    p.x * c - p.y * s,
                    p.x * s + p.y * c
                );

                p.x /= aspect;
                return p + pivot;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 uv = RotateUV(i.uv, _Pivot, _AngleRad);
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);
            }
            ENDHLSL
        }
    }
}
