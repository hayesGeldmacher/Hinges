Shader "Unlit/PortalPlaneScreenRotateURP"
{
    Properties
    {
        _AngleDeg ("Angle (Degrees)", Range(-180, 180)) = 25
        _Opacity ("Opacity", Range(0, 1)) = 1
        _EdgeFade ("Edge Fade", Range(0.0, 0.5)) = 0.08
        _PivotSS ("Pivot Screen (0-1)", Vector) = (0.5, 0.5, 0, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        ZTest LEqual
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            float _AngleDeg;
            float _Opacity;
            float _EdgeFade;
            float4 _PivotSS;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            float2 Rotate2D(float2 p, float a)
            {
                float s = sin(a);
                float c = cos(a);
                return float2(p.x * c - p.y * s, p.x * s + p.y * c);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / max(IN.screenPos.w, 1e-6);

                float angleRad = radians(_AngleDeg);

                float2 pivot = _PivotSS.xy;
                float2 d = screenUV - pivot;
                float2 rotated = Rotate2D(d, angleRad) + pivot;

                rotated = saturate(rotated);

                half4 col = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, rotated);

                // Edge fade using plane UV
                float2 t = abs(IN.uv - 0.5) * 2.0;
                float edge = max(t.x, t.y);
                float fade = saturate((1.0 - edge) / max(_EdgeFade, 1e-4));

                return half4(col.rgb, fade * _Opacity);
            }
            ENDHLSL
        }
    }
}
