// Additive, unlit, depth-tested but never depth-writing: the material for every energy effect
// in HORDE - bolt trails, the vortex funnel, ice shards, engine plumes, boss charge beams.
Shader "Horde/Glow"
{
    Properties
    {
        _Tint     ("Tint", Color) = (1,1,1,1)
        _Emissive ("Emissive", Range(0,4)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
            UNITY_INSTANCING_BUFFER_END(Props)

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
                // hotter where the surface turns away: energy reads as a glowing shell
                float fres = pow(1.0 - saturate(dot(normalize(i.normalWS), normalize(i.viewWS))), 1.6);
                float3 c = i.color.rgb * tint.rgb * (0.55 + 0.9 * fres) * max(emissive, 0.35);
                return half4(c, i.color.a * tint.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
