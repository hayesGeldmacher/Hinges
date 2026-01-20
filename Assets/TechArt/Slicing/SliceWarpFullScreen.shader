Shader "Hidden/SliceWarpFullscreen"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 1)) = 1
        _EdgeWidth ("Edge Width (world units)", Range(0.0, 0.5)) = 0.03
        _EdgeDarken ("Edge Darken", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "SliceWarpFullscreen"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            float _Intensity;
            float _EdgeWidth;
            float _EdgeDarken;

            float4 _PlanePoint[8];
            float4 _PlaneNormal[8];
            int _PlaneCount;

            float4 _RegionPivotAngle[8];
            float4 _RegionOffset[8];

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

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float3 ReconstructWorldPos(float2 uv)
            {
                float deviceDepth = SampleSceneDepth(uv);

                float4 clip = float4(uv * 2.0 - 1.0, deviceDepth, 1.0);
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

            float3 RotateAroundY(float3 p, float3 pivot, float angleRad)
            {
                float s = sin(angleRad);
                float c = cos(angleRad);
                float3 v = p - pivot;

                float3 r;
                r.x = v.x * c - v.z * s;
                r.y = v.y;
                r.z = v.x * s + v.z * c;
                return r + pivot;
            }

            int ComputeRegionId(float3 worldPos)
            {
                int id = 0;
                int count = clamp(_PlaneCount, 0, 8);

                [unroll(8)]
                for (int i = 0; i < 8; i++)
                {
                    if (i >= count) break;

                    float3 pp = _PlanePoint[i].xyz;
                    float3 nn = _PlaneNormal[i].xyz;

                    float d = dot(worldPos - pp, nn);
                    int bit = (d >= 0.0) ? 1 : 0;
                    id |= (bit << i);
                }

                return id & 7;
            }

            float ComputeEdgeMask(float3 worldPos)
            {
                float edge = 0.0;
                int count = clamp(_PlaneCount, 0, 8);

                [unroll(8)]
                for (int i = 0; i < 8; i++)
                {
                    if (i >= count) break;

                    float3 pp = _PlanePoint[i].xyz;
                    float3 nn = _PlaneNormal[i].xyz;

                    float d = abs(dot(worldPos - pp, nn));
                    float band = saturate(1.0 - smoothstep(0.0, _EdgeWidth, d));
                    edge = max(edge, band);
                }

                return edge;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                half4 col = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);

                if (_Intensity <= 0.0001)
                    return col;

                float3 worldPos = ReconstructWorldPos(uv);
                int regionId = ComputeRegionId(worldPos);

                float3 pivot = _RegionPivotAngle[regionId].xyz;
                float angle = _RegionPivotAngle[regionId].w * _Intensity;
                float3 offset = _RegionOffset[regionId].xyz * _Intensity;

                float3 warpedWorld = RotateAroundY(worldPos, pivot, angle) + offset;
                float2 warpedUV = saturate(WorldToScreenUV(warpedWorld));

                half4 warpedCol = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, warpedUV);

                float edge = ComputeEdgeMask(worldPos);
                float darken = lerp(1.0, 1.0 - _EdgeDarken, edge);
                warpedCol.rgb *= darken;

                return warpedCol;
            }

            ENDHLSL
        }
    }
}
