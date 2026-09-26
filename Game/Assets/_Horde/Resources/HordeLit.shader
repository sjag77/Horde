// One shader for every 3D object in HORDE: baked vertex colours multiplied by a per-renderer
// tint, lit by a single hard directional key plus a cool ambient fill. Cheap enough for a
// phone rendering a few hundred creatures, and it keeps the game's flat, punchy look.
Shader "Horde/Lit"
{
    Properties
    {
        _Tint      ("Tint", Color) = (1,1,1,1)
        _Emissive  ("Emissive", Range(0,4)) = 0
        _Alpha     ("Alpha", Range(0,1)) = 1
        _RimPower  ("Rim Power", Range(0.5,8)) = 3
        _RimStrength ("Rim Strength", Range(0,2)) = 0.55
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewWS     : TEXCOORD1;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Tint)
                UNITY_DEFINE_INSTANCED_PROP(float, _Emissive)
                UNITY_DEFINE_INSTANCED_PROP(float, _Alpha)
            UNITY_INSTANCING_BUFFER_END(Props)

            float _RimPower;
            float _RimStrength;

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = normalize(GetCameraPositionWS() - posWS);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _Tint);
                float emissive = UNITY_ACCESS_INSTANCED_PROP(Props, _Emissive);
                float alpha = UNITY_ACCESS_INSTANCED_PROP(Props, _Alpha);

                float3 albedo = i.color.rgb * tint.rgb;
                float3 n = normalize(i.normalWS);

                Light key = GetMainLight();
                float ndl = saturate(dot(n, key.direction));
                // two-step ramp keeps the art graphic instead of muddy
                float ramp = lerp(0.45, 1.0, smoothstep(0.02, 0.55, ndl)) + smoothstep(0.82, 1.0, ndl) * 0.25;
                float3 lit = albedo * (key.color * ramp);

                // cool sky fill so the shadow sides read as night-time, not black
                float sky = saturate(n.y * 0.5 + 0.5);
                lit += albedo * lerp(float3(0.10, 0.12, 0.22), float3(0.22, 0.26, 0.38), sky);

                // rim light picks every creature out of the horde
                float rim = pow(1.0 - saturate(dot(n, normalize(i.viewWS))), _RimPower);
                lit += albedo * rim * _RimStrength;

                lit += albedo * emissive;
                return half4(lit, i.color.a * tint.a * alpha);
            }
            ENDHLSL
        }

        // Shadow casting: the horde throws real shadows on the arena floor.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };

            V shadowVert(A v)
            {
                V o = (V)0;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                float4 cs = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = cs;
                return o;
            }

            half4 shadowFrag(V i) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    Fallback Off
}
