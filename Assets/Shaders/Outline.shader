Shader "Unlit/Test"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _PixelDensity("Pixel Density", Int) = 1
        _Color("0 for original, 1 for outline", Range(0.0, 1)) = 0

        _MaxDepthDiff("Max Depth Diff", Float) = 0.05
        _MaxNormalDiff("Max Normal Angle", Float) = 20
        _MaxColorDiff("Max Color Diff", Float) = 0.05

        _SnowCover("SnowCover", Range(0.0, 1)) = 0

        _OutlineMultiplier("Outline Multiplier", Float) = 0.5
    }
        SubShader
        {
            Pass
            {

                Tags { "LightMode" = "ForwardBase" }

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                sampler2D _CameraDepthNormalsTexture;
                sampler2D _CameraDepthTexture;
                sampler2D _CameraColorTexture;
                sampler2D _NormalTex;
                sampler2D _ColorTex;
                int _PixelDensity;

                float _MaxDepthDiff;
                float _MaxNormalDiff;
                float _MaxColorDiff;

                float _OutlineMultiplier;
                float4 _LightDirection;
                float _Color;
                float _SnowCover;

                StructuredBuffer<float3> buffer;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    return o;
                }

                inline float Angle(in float3 v1, in float3 v2) {
                    //return acos(dot(v1, v2));
                    return distance(v1, v2);
                }

                float Remap(in float value, in float from1, in float to1, in float from2, in float to2) {
                    return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
                }

                

                float4 GetColor(in float2 uv) {
                    float dummy;
                    uint i;

                    //evaluating current information
                    float4 filteredImage = tex2D(_NormalTex, uv);
                    float3 color = tex2D(_CameraColorTexture, uv);
                    float depth = tex2D(_CameraDepthTexture, uv).x;
                    float3 filteredNormal = filteredImage.xyz;
                    float filteredDepth = filteredImage.a;
                    float3 normal;
                    DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv), dummy, normal);

                    if(depth - filteredDepth > 0.005) return float4(color, 1);
                    if(Angle(filteredNormal, float3(0,0,0)) < 0.01) return float4(color, 1);
                    
                    float depthDiff = abs(depth - filteredDepth);

                    uint2 pixel = floor(_ScreenParams.xy * uv);
                    float2 directions[] = { float2(-1, 0), float2(1, 0), float2(0, -1), float2(0, 1) };
                    float depths[4], filteredDepths[4];
                    float3 colors[4], normals[4], filteredNormals[4];

                    //float minimum = min(min(color.r, color.g), color.b);
                    //float clarity = (color.r + color.g + color.b) / 3;
                    float clarity = 0.299 * color.r + 0.587 * color.g + 0.114 * color.b;
                    //if ((pixel.x + pixel.y) % 2 == 0) return float4(1, 0, 0, 1);
                    //else return float4(0, 0, 1, 1);
                    
                    
                    float2 size = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);
                    
                    [unroll]
                    for (i = 0; i < 4; ++i) {
                        float2 offset = directions[i] * size;
                        float2 _uv = uv + offset;

                        //evaluating color, depth and normal
                        colors[i] = tex2D(_CameraColorTexture, _uv);
                        depths[i] = tex2D(_CameraDepthTexture, _uv);
                        float4 filteredDepthNormal = tex2D(_NormalTex, _uv);
                        filteredNormals[i] = filteredDepthNormal.xyz;
                        filteredDepths[i] = filteredDepthNormal.a;
                        DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, _uv), dummy, normals[i]);
                    }

                    half nl = dot(normal, _LightDirection.xyz);
                    //half nl = saturate(dot(normal, _WorldSpaceLightPos0.xyz));

                    half dnl = lerp(1.4, 1.1, clarity);
                    //half nnl = nl < 0 ? 1.4 : 0.7;
                    //half nnl = lerp(0.6, 0.9, clarity);
                    half nnl = nl < 0.5 ? lerp(1.5, 1.1, clarity) : lerp(0.7, 0.9, clarity);
                    
                    //testing silhouette
                    /*if (filteredDepth < 0.96) {
                        [unroll]
                        for (i = 0; i < 4; ++i)
                            if (filteredDepths[i] > 0.96) {
                                //return float4(1, 0, 0, 1);
                                return float4(0, 0, 0, 1);
                            }
                    }*/

                    //if (pixel.x == 67 && pixel.y == 120) {

                        //for (i = 0; i < 4; ++i)
                          //  if (Angle(filteredNormals[i], float3(0,0,0)) < 0.001)
                            //    return float4(1, 0, 0, 1);

                        //return float4(0, 0, 1, 1);
                    //}
                            //return float4(dnl * color, 1);

                    
                    //return float4(1, 0, 0, 1);


                    //testing depth
                    [unroll]
                    for (i = 0; i < 4; ++i)
                        if (depth - depths[i] > _MaxDepthDiff) {
                            //return float4(1, 0, 0, 1);
                            return float4(dnl * color, 1);
                        }
                    
                    //testing normal

                    /*[unroll]
                    for (i = 0; i < 4; ++i)
                        if (Angle(filteredNormal, filteredNormals[i]) > _MaxNormalDiff) {
                            half nil = saturate(dot(normals[i], _LightDirection.xyz));
                            if (nl > nil || (nl == nil && depth > depths[i])) {
                                //return float4(0, 1, 0, 1);
                                //return float4(color, 1);
                                //return float4(0.8 * color, 1);
                                return float4(nnl * color, 1);
                            }
                        }*/


                    for (i = 0; i < 4; ++i)
                        if (Angle(filteredNormal, filteredNormals[i]) > _MaxNormalDiff) {
                            half nil = saturate(dot(normals[i], _LightDirection.xyz));
                            if (depth > depths[i]) {
                                //return float4(0, 1, 0, 1);
                                //return float4(color, 1);
                                //return float4(0.8 * color, 1);
                                return float4(nnl * color, 1);
                            }
                        }

                    return float4(color, 1);
                }

                float4 ApplySnow(in float3 normal, in float4 color) {
                    if (normal.y <= 0.2) return color;
                    return lerp(color, float4(1, 1, 1, 1), _SnowCover * normal.y * 0.8);
                }

                fixed4 frag(v2f i) : SV_Target
                {

                    //uint2 fixedPixel = round(_ScreenParams.xy * i.uv);

                    //uint2 pixel = _PixelDensity * floor(_ScreenParams.xy * i.uv / _PixelDensity) + (_PixelDensity >> 1);
                    //float2 uv = pixel / _ScreenParams.xy;

                    float4 depthNormal;
                    DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv), depthNormal.w, depthNormal.xyz);

                    //bool isOutline = IsOutline(pixel, depthNormal);

                    float4 normal = tex2D(_NormalTex, i.uv);

                    //if (depthNormal.w < _MaxDepthDiff) return tex2D(_MainTex, uv);
                    if (_Color < 0.001) return tex2D(_CameraColorTexture, i.uv);
                    if (_Color < 0.1) return ApplySnow(depthNormal.xyz, tex2D(_CameraColorTexture, i.uv));
                    if (_Color < 0.25) return float4(depthNormal.xyz, 1);;
                    if (_Color < 0.5) return float4(normal.xyz, 1);
                    if (_Color < 0.75) return tex2D(_CameraDepthTexture, i.uv).r;
                    if (_Color < 0.85) return normal.a;
                    if (_Color < 0.99) return tex2D(_CameraColorTexture, i.uv);
                    //return tex2D(_CameraDepthTexture, uv).r;
                    //float mult = isOutline ? _OutlineMultiplier : 1;
                    //return mult;Linear01Depth
                    return GetColor(i.uv);
                }
                ENDCG
            }
        }
}
