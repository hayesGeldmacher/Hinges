Shader "Unlit/IntersectionGlowURP_Linear"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _Thickness ("Thickness (meters-ish)", Range(0.0001, 1.0)) = 0.03
        _Softness ("Softness", Range(0.0001, 2.0)) = 0.08
        _Intensity ("Intensity", Range(0, 10)) = 2
        _MaxDistance ("Max Distance", Range(0.01, 50)) = 5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        ZTest LEqual
        Cull Off
        Blend One One

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _GlowColor;
            float _Thickness;
            float _Softness;
            float _Intensity;
            float _MaxDistance;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos   : TEXCOORD0;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.screenPos.xy / max(IN.screenPos.w, 1e-6);

                // Scene device depth (0..1)
                float sceneDevice = SampleSceneDepth(uv);

                // Plane device depth (0..1)
                float planeDevice = IN.screenPos.z / max(IN.screenPos.w, 1e-6);

                // Convert both to linear eye depth (positive forward distance)
                float sceneEye = LinearEyeDepth(sceneDevice, _ZBufferParams);
                float planeEye = LinearEyeDepth(planeDevice, _ZBufferParams);

                // If scene surface is behind the plane, sceneEye > planeEye
                float d = sceneEye - planeEye;

                // Ignore if the scene surface is in front of the plane or too far away
                if (d < 0.0 || d > _MaxDistance)
                    return half4(0, 0, 0, 0);

                // Glow only when close to intersection
                float glow = 1.0 - smoothstep(_Thickness, _Thickness + _Softness, d);

                float3 rgb = _GlowColor.rgb * glow * _Intensity;
                return half4(rgb, 1);
            }
            ENDHLSL
        }
    }
}
