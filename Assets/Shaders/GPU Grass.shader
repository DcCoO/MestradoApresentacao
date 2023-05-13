// This shader fills the mesh shape with a color predefined in the code.
Shader "Pixel Art Engine/Grass"
{
    // The properties block of the Unity shader. In this example this block is empty
    // because the output color is predefined in the fragment shader code.
    Properties
    {
        _Color("Lit Color", Color) = (1, 1, 1, 1)
        _MainTex("Main Texture", 2D) = "white" {}
        _Ramp("Ramp Texture", 2D) = "white" {}
        _TimeScale("Time Scale", Float) = 1
        _GridSize("Billboard Grid", Vector) = (1, 1, 0, 0)
    }

    SubShader
    {
        //Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }

        Pass
        {
            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader. 
            #pragma vertex Vertex
            // This line defines the name of the fragment shader. 
            #pragma fragment Fragment


            //#define _SPECULAR_COLOR
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _LIGHT_COOKIES

            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "../Shaders/Helper.hlsl"

            float4 _Color;
            float _Smoothness;
            float _TimeScale;
            float2 _GridSize;
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); float4 _MainTex_ST;
            TEXTURE2D(_Ramp); SAMPLER(sampler_Ramp); float4 _Ramp_ST;


            StructuredBuffer<float3> positionBuffer;
            StructuredBuffer<float3> normalBuffer;
            StructuredBuffer<float> indexBuffer;
            //StructuredBuffer<uint> layerBuffer;

            // This attributes struct receives data about the mesh we're currently rendering
            // Data is automatically placed in fields according to their semantic
            struct Attributes {
                float3 positionOS : POSITION; // Position in object space
                float2 uv : TEXCOORD0; // Material texture UVs
                float3 normalOS : NORMAL;
                float4 texcoord : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // This struct is output by the vertex function and input to the fragment function.
            // Note that fields will be transformed by the intermediary rasterization stage
            struct Interpolators {
                // This value should contain the position in clip space (which is similar to a position on screen)
                // when output from the vertex function. It will be transformed into pixel position of the current
                // fragment on the screen when read from the fragment function
                float4 positionCS : SV_POSITION;

                // The following variables will retain their values from the vertex stage, except the
                // rasterizer will interpolate them between vertices
                float2 uv : TEXCOORD0; // Material texture UVs

                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            #define UnityObjectToViewPos(posOS) TransformWorldToView(TransformObjectToWorld(posOS))

            float Remap(in float value, in float from1, in float to1, in float from2, in float to2) {
                return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
            }
           
            float ToonAttenuation(int i, float3 positionWS, float pointBands, float spotBands) {
                int perObjectLightIndex = GetPerObjectLightIndex(i); // (i = index used in loop)
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
                float4 lightPositionWS = _AdditionalLightsBuffer[perObjectLightIndex].position;
                half4 spotDirection = _AdditionalLightsBuffer[perObjectLightIndex].spotDirection;
                half4 distanceAndSpotAttenuation = _AdditionalLightsBuffer[perObjectLightIndex].attenuation;
#else
                float4 lightPositionWS = _AdditionalLightsPosition[perObjectLightIndex];
                half4 spotDirection = _AdditionalLightsSpotDir[perObjectLightIndex];
                half4 distanceAndSpotAttenuation = _AdditionalLightsAttenuation[perObjectLightIndex];
#endif

                // Point
                float3 lightVector = lightPositionWS.xyz - positionWS * lightPositionWS.w;
                float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);
                float range = rsqrt(distanceAndSpotAttenuation.x);
                float dist = sqrt(distanceSqr) / range;

                // Spot
                half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));
                half SdotL = dot(spotDirection.xyz, lightDirection);
                half spotAtten = saturate(SdotL * distanceAndSpotAttenuation.z + distanceAndSpotAttenuation.w);
                spotAtten *= spotAtten;
                float maskSpotToRange = step(dist, 1);

                // Atten
                bool isSpot = (distanceAndSpotAttenuation.z > 0);
                return isSpot ?
                    //step(0.01, spotAtten) :		// cheaper if you just want "1" band for spot lights
                    (floor(spotAtten * spotBands) / spotBands) * maskSpotToRange :
                    saturate(1.0 - floor(dist * pointBands) / pointBands);
            }


            void AdditionalLights(float3 SpecColor, float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView, half4 Shadowmask,
                out float3 Diffuse, out float3 Specular) {

                float3 diffuseColor = 0;
                float3 specularColor = 0;

                Smoothness = exp2(10 * Smoothness + 1);
                WorldNormal = normalize(WorldNormal);
                WorldView = SafeNormalize(WorldView);
                int pixelLightCount = GetAdditionalLightsCount();
                for (int i = 0; i < pixelLightCount; ++i) {
                    Light light = GetAdditionalLight(i, WorldPosition, Shadowmask);
                    diffuseColor += light.color * light.shadowAttenuation * ToonAttenuation(i, WorldPosition, 3, 2);
                }

                Diffuse = diffuseColor;
                Specular = specularColor;
            }

            


            float4 CalculateVertex(float4 vertex, float3 worldPos)
            {
                float3 camUpVec = normalize(UNITY_MATRIX_V._m10_m11_m12);
                float3 camForwardVec = -normalize(UNITY_MATRIX_V._m20_m21_m22);
                float3 camRightVec = normalize(UNITY_MATRIX_V._m00_m01_m02);
                float4x4 camRotMat = float4x4(camRightVec, 0, camUpVec, 0, camForwardVec, 0, 0, 0, 0, 1);

                vertex = mul(vertex, camRotMat); // Billboard
                //vertex.xyz *= 1;   // Scale
                vertex.xyz += worldPos; // Instance Position

                // World => VP => Clip
                return mul(UNITY_MATRIX_VP, vertex);
            }



            // The vertex function. This runs for each vertex on the mesh.
            // It must output the position on the screen each vertex should appear at,
            // as well as any data the fragment function will need
            Interpolators Vertex(Attributes input, uint instanceID : SV_InstanceID) {
                Interpolators output = (Interpolators)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = positionBuffer[instanceID] + float3(0, 0.5, 0);
                VertexPositionInputs posnInputs = GetVertexPositionInputs(input.positionOS);
                
                output.positionCS = CalculateVertex(float4(input.positionOS, 1), positionWS);

                output.positionWS = positionWS;
                output.normalWS = normalBuffer[instanceID];


                float u = floor((_TimeScale * (_Time.y + output.positionWS.x)) % _GridSize.x);
                output.uv = float2(
                    Remap(input.texcoord.x, 0.0, 1.0, u / _GridSize.x, (u + 1.0) / _GridSize.x),
                    (input.texcoord.y + indexBuffer[instanceID]) / _GridSize.y
                );

                return output;
            }
            

            float4 Fragment(Interpolators input) : SV_TARGET {
                float2 uv = input.uv;
                float4 colorSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
                half shadowStrength = GetMainLightShadowStrength();
                float mlShadowAtten = SampleShadowmap(TransformWorldToShadowCoord(input.positionWS), TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), shadowSamplingData, shadowStrength, false);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord, input.positionWS, 1);

                float ndotl = saturate(dot(input.normalWS, mainLight.direction.xyz)) * mlShadowAtten;
                float ramp = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, ndotl).r;

                colorSample.xyz *= (ramp * mainLight.distanceAttenuation) * mainLight.color.xyz;
                colorSample.xyz += SampleSH(input.normalWS);
                colorSample *= _Color;

                OUTPUT_LIGHTMAP_UV(uv, unity_LightmapST, uv);
                float4 Shadowmask = SAMPLE_SHADOWMASK(uv);
                float3 diffuse, specular;
                AdditionalLights(float3(0, 0, 0), 2, input.positionWS, input.normalWS, normalize(GetWorldSpaceViewDir(input.positionWS)), Shadowmask, diffuse, specular);

                colorSample.xyz += diffuse;
                
                clip(colorSample.a - 0.5);
                return colorSample;
            }


            ENDHLSL
        }
    }
}