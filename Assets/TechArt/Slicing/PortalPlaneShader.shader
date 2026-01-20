Shader "Unlit/PortalPlaneWarpURP"
{
    Properties
    {
        _AngleDeg ("Angle (Degrees)", Range(-180, 180)) = 25
        _Intensity ("Intensity", Range(0, 1)) = 1
        _PivotWS ("Pivot (World)", Vector) = (0, 1, 0, 0)
        _AxisWS ("Axis (World)", Vector) = (0, 1, 0, 0)
        _EdgeFade ("Edge Fade", Range(0.0, 0.5)) = 0.08
        _Opacity ("Opacity", Range(0, 1)) = 1
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            float _AngleDeg;
            float _Intensity;
            float4 _PivotWS;
            float4 _AxisWS;
            float _EdgeFade;
            float _Opacity;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0; // plane mesh uv (0..1)
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0; // plane uv
                float4 screenPos   : TEXCOORD1; // for screen uv
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            float3 ReconstructWorldPos(float2 screenUV)
            {
                float deviceDepth = SampleSceneDepth(screenUV);

                float4 clip = float4(screenUV * 2.0 - 1.0, deviceDepth, 1.0);
                float4 view = mul(unity_CameraInvProjection, clip);
                view.xyz /= max(view.w, 1e-6);

                float4 world = mul(unity_CameraToWorld, float4(view.xyz, 1.0));
                return world.xyz;
            }

            float2 WorldToScreenUV(float3 worldPos)
            {
                float4 clip = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                clip.xyz /= max(clip.w, 1e-6);
                return clip.xy * 0.5 + 0.5;
            }

            // Rodrigues rotation around arbitrary axis
            float3 RotateAroundAxis(float3 p, float3 pivot, float3 axis, float angleRad)
            {
                float3 v = p - pivot;
                float s = sin(angleRad);
                float c = cos(angleRad);

                float3 k = normalize(axis);
                float3 v_rot = v * c + cross(k, v) * s + k * dot(k, v) * (1.0 - c);
                return v_rot + pivot;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / max(IN.screenPos.w, 1e-6);

                // Reconstruct the world position of what¡¯s currently on screen at this pixel
                float3 worldPos = ReconstructWorldPos(screenUV);

                float angleRad = radians(_AngleDeg) * _Intensity;
                float3 pivot = _PivotWS.xyz;
                float3 axis  = _AxisWS.xyz;

                float3 warpedWorld = RotateAroundAxis(worldPos, pivot, axis, angleRad);
                float2 warpedUV = saturate(WorldToScreenUV(warpedWorld));

                half4 warpedCol = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, warpedUV);

                // Edge fade so the plane border feels softer / less like a sticker
                float2 d = abs(IN.uv - 0.5) * 2.0;          // center to edge
                float edge = max(d.x, d.y);                 // square-ish edge factor
                float fade = saturate((1.0 - edge) / max(_EdgeFade, 1e-4));
                float alpha = fade * _Opacity;

                return half4(warpedCol.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
