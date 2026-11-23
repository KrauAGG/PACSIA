Shader "Custom/FairyGlow"
{
    Properties
    {
        [HDR] _Color ("Color", Color)=(0.4, 0.8, 1, 1)
        _Intensity ("Intensity", Range(0, 10))=2
        _Falloff ("Falloff (dureza del borde)", Range(0.1, 8))=2
        _CoreRadius ("Radio sólido", Range(0, 1))=0.4
        _CoreSoftness ("Suavizado borde núcleo", Range(0.001, 0.5))=0.05
        _CoreIntensity ("Brillo núcleo", Range(0, 10))=1.5
        _CoreWhiten ("Blanqueo núcleo", Range(0, 1))=0.3
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Pass
        {
            Name "FairyGlow"
            Tags { "LightMode"="UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _Falloff;
                half _CoreRadius;
                half _CoreSoftness;
                half _CoreIntensity;
                half _CoreWhiten;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS:POSITION;
                float3 normalOS:NORMAL;
            };

            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 normalWS:TEXCOORD0;
                float3 viewDirWS:TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 positionWS=TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS=TransformWorldToHClip(positionWS);
                OUT.normalWS=TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS=GetWorldSpaceViewDir(positionWS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 n=normalize(IN.normalWS);
                float3 v=normalize(IN.viewDirWS);
                half ndv=saturate(dot(n, v));
                half r=sqrt(saturate(1-ndv*ndv));
                half core=1-smoothstep(_CoreRadius-_CoreSoftness, _CoreRadius, r);
                half t=saturate((r-_CoreRadius)/max(1-_CoreRadius, 1e-4));
                half halo=pow(1-t, _Falloff);
                half3 col=_Color.rgb*_Intensity*halo;
                col+=lerp(_Color.rgb, 1, _CoreWhiten)*_CoreIntensity*core;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}