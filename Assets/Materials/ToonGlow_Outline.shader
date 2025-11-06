Shader "Custom/ToonGlow_Outline"
{
    Properties
    {
        [HDR]_GlowColor ("Glow Color", Color) = (1, 0.85, 0.35, 1)
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0

        [HDR]_OutlineColor ("Outline Color", Color) = (1, 0.9, 0.5, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.0

        _RimPower ("Rim Sharpness", Range(0.5, 8)) = 2.5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        // --- OUTLINE SHELL PASS ---
        Pass
        {
            Name "Outline"
            Cull Front                // Render backside for outline
            ZWrite Off
            Blend One One             // Additive
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _OutlineColor;
            float _OutlineWidth;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                // Expand along normals
                worldPos += normalWS * _OutlineWidth;
                OUT.positionHCS = TransformWorldToHClip(worldPos);

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }

            ENDHLSL
        }

        // --- RIM GLOW PASS ---
        Pass
        {
            Name "RimGlow"
            Tags { "LightMode"="UniversalForward" }

            Blend One One
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _GlowColor;
            float _GlowStrength;
            float _RimPower;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.viewDirWS = normalize(_WorldSpaceCameraPos - worldPos);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float rim = 1.0 - saturate(dot(normalize(IN.normalWS), normalize(IN.viewDirWS)));
                rim = pow(rim, _RimPower);

                return _GlowColor * rim * _GlowStrength;
            }

            ENDHLSL
        }
    }
}
