/*
half4 _CloudsColor;
half _SpeedX;
half _SpeedY;
half _Offset;
float4 _ShadowTexTO;
float4 _NoiseTexTO;
TEXTURE2D(_NoiseTex);       SAMPLER(sampler_NoiseTex);
TEXTURE2D(_ShadowTex);       SAMPLER(sampler_ShadowTex);
*/

half _SpeedX;
half _SpeedY;
half _Offset;
half _Step;
float4 _ShadowTexTO;
float4 _NoiseTexTO;
float4 _ShadowTex_ST;
float4 _NoiseTex_ST;
TEXTURE2D(_ShadowTex);       SAMPLER(sampler_ShadowTex);
TEXTURE2D(_NoiseTex);       SAMPLER(sampler_NoiseTex);

half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
	return SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv);
}

float GetCloudShadow(in float3 positionWS) {
	half4 noise = SampleAlbedoAlpha(float2(positionWS.x * _NoiseTexTO.x + frac(_Time.x * _SpeedX * _Offset), positionWS.z * _NoiseTexTO.y + frac(_Time.x * _SpeedY * _Offset)), TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex));
	float intensity = SampleAlbedoAlpha(positionWS.xz * _ShadowTexTO.xy + frac(_Time.xx * float2(_SpeedX, _SpeedY)) + noise, TEXTURE2D_ARGS(_ShadowTex, sampler_ShadowTex)).a;
	return intensity;
}

void ApplyCloudShadow(in float3 positionWS, in float shadowIntensity, inout float shadowAtten) {
	half4 noise = SampleAlbedoAlpha(float2(positionWS.x * _NoiseTexTO.x + frac(_Time.x * _SpeedX * _Offset), positionWS.z * _NoiseTexTO.y + frac(_Time.x * _SpeedY * _Offset)), TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex));
	//half4 shadowtexture = _CloudsColor * SampleAlbedoAlpha(positionWS.xz * _ShadowTexTO.xy + frac(_Time.xx * float2(_SpeedX, _SpeedY)) + noise, TEXTURE2D_ARGS(_ShadowTex, sampler_ShadowTex));
	//color = lerp(color, shadowtexture.rgb, shadowtexture.a * _CloudsColor.a);
	//if (shadowAtten < 1 && shadowtexture.a > 0.5) shadowAtten = min(shadowAtten, shadowIntensity);
	//shadowAtten *= shadowtexture.a;

	//if (shadowAtten != 1 - shadowIntensity && GetCloudShadow(positionWS) > 0) shadowAtten = 1 - shadowIntensity;
}

void ApplyCloudShadow_float(float3 positionWS, float shadowIntensity, float currentShadow, out float cloudAtten) {
	half4 noise = SampleAlbedoAlpha(float2(positionWS.x * _NoiseTexTO.x + frac(_Time.x * _SpeedX * _Offset), positionWS.z * _NoiseTexTO.y + frac(_Time.x * _SpeedY * _Offset)), TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex));
	//half4 shadowtexture = _CloudsColor * SampleAlbedoAlpha(positionWS.xz * _ShadowTexTO.xy + frac(_Time.xx * float2(_SpeedX, _SpeedY)) + noise, TEXTURE2D_ARGS(_ShadowTex, sampler_ShadowTex));
	//cloudAtten = shadowtexture.a > 0 ? shadowIntensity : 1;
	if (currentShadow < 0.9) cloudAtten = shadowIntensity;
	else cloudAtten = currentShadow;
	//if (GetCloudShadow(positionWS) > 0) cloudAtten = shadowIntensity;
	//cloudAtten = GetCloudShadow(positionWS) + currentShadow;;
	//	currentShadow = shadowIntensity;
	//cloudAtten = currentShadow;
}