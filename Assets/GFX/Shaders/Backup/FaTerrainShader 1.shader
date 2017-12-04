﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "MapEditor/FaTerrainShader" {
Properties {
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
	_Shininess ("Shininess", Range (0.03, 1)) = 0.078125
	[MaterialToggle] _Water("Has Water", Int) = 0
	[MaterialToggle] _Grid("Grid", Int) = 0
	[MaterialToggle] _Slope("Slope", Int) = 0
	[MaterialToggle] _UseSlopeTex("Use Slope Data", Int) = 0
	_SlopeTex ("Slope data", 2D) = "black" {}
	[MaterialToggle] _TTerrainXP("_TTerrainXP", Int) = 0
	
	_WaterLevel ("Water Level", float) = 0.078125
	_DepthLevel ("Depth Level", float) = 0.078125
	_AbyssLevel ("Abyss Level", float) = 0.078125
	
	[MaterialToggle] _Area("Area", Int) = 0
	_AreaX ("Area X", Range (0, 2048)) = 0
	_AreaY ("Area Y", Range (0, 2048)) = 0
	_AreaWidht ("Area Widht", Range (0, 2048)) = 0
	_AreaHeight ("Area Height", Range (0, 2048)) = 0

	// set by terrain engine
	_Control ("Control (RGBA)", 2D) = "black" {}
	_ControlXP ("ControlXP (RGBA)", 2D) = "black" {}
	_Control2XP ("Control2XP (RGBA)", 2D) = "black" {}

	
	[MaterialToggle] _HideSplat0("Hide splat 2", Int) = 0
	[MaterialToggle] _HideSplat1("Hide splat 2", Int) = 0
	[MaterialToggle] _HideSplat2("Hide splat 3", Int) = 0
	[MaterialToggle] _HideSplat3("Hide splat 4", Int) = 0
	[MaterialToggle] _HideSplat4("Hide splat 5", Int) = 0
	[MaterialToggle] _HideSplat5("Hide splat 6", Int) = 0
	[MaterialToggle] _HideSplat6("Hide splat 7", Int) = 0
	[MaterialToggle] _HideSplat7("Hide splat 8", Int) = 0
	[MaterialToggle] _HideSplat8("Hide splat Upper", Int) = 0

	_SplatAlbedoArray ("Albedo array", 2DArray) = "" {}
	_SplatNormalArray ("Normal array", 2DArray) = "" {}


	_Splat0Scale ("Splat1 Level", Range (1, 1024)) = 10
	_Splat1Scale ("Splat2 Level", Range (1, 1024)) = 10
	_Splat2Scale ("Splat3 Level", Range (1, 1024)) = 10
	_Splat3Scale ("Splat4 Level", Range (1, 1024)) = 10
	_Splat4Scale ("Splat5 Level", Range (1, 1024)) = 10
	_Splat5Scale ("Splat6 Level", Range (1, 1024)) = 10
	_Splat6Scale ("Splat7 Level", Range (1, 1024)) = 10
	_Splat7Scale ("Splat8 Level", Range (1, 1024)) = 10

	// set by terrain engine
	[MaterialToggle] _GeneratingNormal("Generating Normal", Int) = 0
	_TerrainNormal ("Terrain Normal", 2D) = "bump" {}
	

	_Splat0ScaleNormal ("Splat1 Normal Level", Range (1, 1024)) = 10
	_Splat1ScaleNormal ("Splat2 Normal Level", Range (1, 1024)) = 10
	_Splat2ScaleNormal ("Splat3 Normal Level", Range (1, 1024)) = 10
	_Splat3ScaleNormal ("Splat4 Normal Level", Range (1, 1024)) = 10
	_Splat4ScaleNormal ("Splat5 Normal Level", Range (1, 1024)) = 10
	_Splat5ScaleNormal ("Splat6 Normal Level", Range (1, 1024)) = 10
	_Splat6ScaleNormal ("Splat7 Normal Level", Range (1, 1024)) = 10
	_Splat7ScaleNormal ("Splat8 Normal Level", Range (1, 1024)) = 10

	// used in fallback on old cards & base map
	[HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "white" {}
	[HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)

	[MaterialToggle] _Brush ("Brush", Int) = 0
	_BrushTex ("Brush (RGB)", 2D) = "white" {}
	_BrushSize ("Brush Size", Range (0, 128)) = 0
	_BrushUvX ("Brush X", Range (0, 1)) = 0
	_BrushUvY ("Brush Y", Range (0, 1)) = 0

	//Lower Stratum
	_SplatLower ("Layer Lower (R)", 2D) = "white" {}
	_NormalLower ("Normal Lower (R)", 2D) = "bump" {}
	_LowerScale ("Lower Level", Range (1, 128)) = 1
	_LowerScaleNormal ("Lower Normal Level", Range (1, 128)) = 1

	//Upper Stratum
	_SplatUpper ("Layer Lower (R)", 2D) = "white" {}
	_NormalUpper ("Normal Lower (R)", 2D) = "bump" {}
	_UpperScale ("Upper Level", Range (1, 128)) = 1
	_UpperScaleNormal ("Upper Normal Level", Range (1, 128)) = 1

	_GridScale ("Grid Scale", Range (0, 2048)) = 512
	_GridTexture ("Grid Texture", 2D) = "white" {}
	//_GridCamDist ("Grid Scale", Range (0, 10)) = 5
	_WaterScaleX ("Water Scale X", float) = 1024
		_WaterScaleZ ("Water Scale Z", float) = 1024
}
	
	SubShader {

			CGPROGRAM
			#define UNITY_BRDF_PBS BRDF3_Unity_PBS

			#pragma surface surf SimpleLambert vertex:vert  fullforwardshadows addshadow nometa
			//#pragma debug
			#pragma target 3.5
			#pragma exclude_renderers gles
			#include "UnityLightingCommon.cginc"
			#include "UnityGBuffer.cginc"
			#include "UnityGlobalIllumination.cginc"

			struct Input {
				float2 uv_Control : TEXCOORD0;
				float3 worldPos;
				float SlopeLerp;
				float4 grabUV;
			};

			half _GeneratingNormal;

			void vert (inout appdata_full v, out Input o){
				UNITY_INITIALIZE_OUTPUT(Input,o);

				o.SlopeLerp = dot(v.normal, half3(0,1,0));

				if(_GeneratingNormal == 0)
					v.normal = float3(0,1,0);
				v.tangent.xyz = cross(v.normal, float3(0,0,1));
				v.tangent.w = -1;
				//o.normal = v.normal;
				 float4 hpos = UnityObjectToClipPos (v.vertex);
		         o.grabUV = ComputeGrabScreenPos(hpos);
				//v.color = _Abyss;
			}

			sampler2D _MyGrabTexture3;
			sampler2D _WaterRam;
			half _Shininess;
			half _WaterLevel;
			half _DepthLevel, _AbyssLevel;
			fixed4 _Abyss;
			fixed4 _Deep;
			int _Water;

			int _Slope, _UseSlopeTex;
			sampler2D _SlopeTex;

			int _Grid;
			half _GridScale;
			half _GridCamDist;
			sampler2D _GridTexture;

			uniform half _LightingMultiplier;
			uniform fixed4 _SunColor;
			uniform fixed4 _SunAmbience;
			uniform fixed4 _ShadowColor;
			//uniform
			sampler2D _ControlXP;
			sampler2D _Control2XP;
			uniform sampler2D _UtilitySamplerC;
			sampler2D _TerrainNormal;
			sampler2D _SplatLower, _SplatUpper;
			sampler2D  _NormalLower;
			//sampler2D _SplatNormal0, _SplatNormal1, _SplatNormal2, _SplatNormal3, _SplatNormal4, _SplatNormal5, _SplatNormal6, _SplatNormal7;
			half _Splat0Scale, _Splat1Scale, _Splat2Scale, _Splat3Scale, _Splat4Scale, _Splat5Scale, _Splat6Scale, _Splat7Scale;
			half _Splat0ScaleNormal, _Splat1ScaleNormal, _Splat2ScaleNormal, _Splat3ScaleNormal, _Splat4ScaleNormal, _Splat5ScaleNormal, _Splat6ScaleNormal, _Splat7ScaleNormal;

			UNITY_DECLARE_TEX2DARRAY(_SplatAlbedoArray);
			 UNITY_DECLARE_TEX2DARRAY(_SplatNormalArray);

			int _HideSplat0, _HideSplat1, _HideSplat2, _HideSplat3, _HideSplat4, _HideSplat5, _HideSplat6, _HideSplat7, _HideSplat8;

			half _LowerScale, _UpperScale;
			fixed _TTerrainXP;
			float _WaterScaleX, _WaterScaleZ;

			int _Brush;
			sampler2D _BrushTex;
			half _BrushSize;
			half _BrushUvX;
			half _BrushUvY;

			int _Area;
			half _AreaX;
			half _AreaY;
			half _AreaWidht;
			half _AreaHeight;

			/*
			float4 LightingSimpleLambertLight  (SurfaceOutput s, float3 lightDir, half atten) {
			    float NdotL = dot (lightDir, s.Normal);
			              
			    float4 c;
			    float3 spec = float3(0,0,0);

			    float3 light =  _SunColor.rgb * 2 * saturate(NdotL) * atten + _SunAmbience.rgb * 2;
			    light = _LightingMultiplier * light + _ShadowColor.rgb * 2 * (1 - light);


			    c.rgb = (s.Albedo + spec) * light;
			    c.a = s.Alpha;
			    return c;
			}*/


			
			inline float4 LightingSimpleLambertLight  (SurfaceOutput s, UnityLight light)
			{
			    float NdotL = dot (light.dir, s.Normal);
			    fixed diff = max (0, dot (s.Normal, light.dir));
						 
			    float4 c;
			    float3 spec = float3(0,0,0);

			    float3 lighting = light.color * saturate(NdotL);
			    lighting = _LightingMultiplier * lighting + _ShadowColor.rgb * 2 * (1 - lighting);


			    c.rgb = (s.Albedo + spec) * lighting;
			    c.a = s.Alpha;
			    return c;
			}

			inline fixed4 LightingSimpleLambert_PrePass (SurfaceOutput s, half4 light)
			{
				fixed4 c;
				c.rgb = s.Albedo * light.rgb;
				c.rgb = s.Albedo;
				c.a = s.Alpha;
				return c;
			}

			inline fixed4 LightingSimpleLambert (SurfaceOutput s, UnityGI gi)
			{
				fixed4 c;
				c = LightingSimpleLambertLight (s, gi.light);

				//#ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
				//	c.rgb += s.Albedo * gi.indirect.diffuse;
				//#endif

				return c;
			}

			inline half4 LightingSimpleLambert_Deferred (SurfaceOutput s, UnityGI gi, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
			{
				UnityStandardData data;
				data.diffuseColor   = s.Albedo;
				data.occlusion      = 1;
				data.specularColor  = 0;
				data.smoothness     = 0;
				data.normalWorld    = s.Normal;

				UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

				half4 emission = half4(s.Emission, 1);

				//#ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
				//	emission.rgb += s.Albedo * gi.indirect.diffuse;
				//#endif

				return emission;
			}

			inline void LightingSimpleLambert_GI (
					SurfaceOutput s,
					UnityGIInput data,
					inout UnityGI gi)
				{
					gi = UnityGlobalIllumination (data, 1.0, s.Normal);
				}


			float3 ApplyWaterColor( float depth, float3  inColor){
				float4 wcolor = tex2D(_WaterRam, float2(depth,0));
				return lerp( inColor.rgb, wcolor.rgb, wcolor.a );
				//return inColor.rgb;
				//return inColor;
			}

			inline fixed3 UnpackNormalDXT5nmScaled (fixed4 packednormal, fixed scale)
{
			   fixed3 normal = 0;
			   normal.xz = packednormal.wx * 2 - 1;



			   normal.y = sqrt(1 - saturate(dot(normal.xz, normal.xz)));

			normal.xz *= scale;

			
			   return normal.xzy;
			}

			void surf (Input IN, inout SurfaceOutput o) {

				float4 waterTexture = tex2D( _UtilitySamplerC, IN.uv_Control * float2(1, -1) + float2(1 / (_WaterScaleX * 1), 1 / (_WaterScaleZ * 1) + 1));

				//float WaterDepth = (_WaterLevel - IN.worldPos.y) / (_WaterLevel - _AbyssLevel);
				float WaterDepth = waterTexture.g;

				float2 UVCtrl = IN.uv_Control * fixed2(1, -1) + half2(0, 1);
				float2 UV = IN.uv_Control * fixed2(1, -1) + half2(0, 1);
				//float2 UV = IN.uv_Control * half2(1, -1)  + half2(0, 1);
				float4 splat_control = saturate(tex2D (_ControlXP, UVCtrl) * 2 - 1);
				float4 splat_control2 = saturate(tex2D (_Control2XP, UVCtrl) * 2 - 1);

				//float4 col = tex2Dproj( _MyGrabTexture3, UNITY_PROJ_COORD(IN.grabUV));

				

				float4 col = tex2D(_SplatLower, UV * _LowerScale);

				if(_HideSplat0 == 0)
					//col = lerp(col, tex2D(_Splat0XP, UV * _Splat0Scale), splat_control.r);
					col = lerp(col, UNITY_SAMPLE_TEX2DARRAY(_SplatAlbedoArray, float3(UV * _Splat0Scale, 0)), splat_control.r);
				if(_HideSplat1 == 0)
					//col = lerp(col, tex2D(_Splat1XP, UV * _Splat1Scale), splat_control.g);
					col = lerp(col, UNITY_SAMPLE_TEX2DARRAY(_SplatAlbedoArray, float3(UV * _Splat1Scale, 1)), splat_control.g);
				if(_HideSplat2 == 0)
					//col = lerp(col, tex2D(_Splat2XP, UV * _Splat2Scale), splat_control.b);
					col = lerp(col, UNITY_SAMPLE_TEX2DARRAY(_SplatAlbedoArray, float3(UV * _Splat2Scale, 2)), splat_control.b);
				if(_HideSplat3 == 0)
					//col = lerp(col, tex2D(_Splat3XP, UV * _Splat3Scale), splat_control.a);
					col = lerp(col, UNITY_SAMPLE_TEX2DARRAY(_SplatAlbedoArray, float3(UV * _Splat3Scale, 3)), splat_control.a);

				if (_TTerrainXP > 0) {
					if(_HideSplat4 == 0)
					//col = lerp(col, tex2D(_Splat4XP, UV * _Splat4Scale), splat_control2.r);
					col = lerp(col, UNITY_SAMPLE_TEX2DARRAY(_SplatAlbedoArray, float3(UV * _Splat4Scale, 4)), splat_control2.r);
					if(_HideSplat5 == 0)
					//col = lerp(col, tex2D(_Splat5XP, UV * _Splat5Scale), splat_control2.g);
					col = lerp(col, UNITY_SAMPLE_TEX2DARRAY(_SplatAlbedoArray, float3(UV * _Splat5Scale, 5)), splat_control2.g);
					if(_HideSplat6 == 0)
					//col = lerp(col, tex2D(_Splat6XP, UV * _Splat6Scale), splat_control2.b);
					col = lerp(col, UNITY_SAMPLE_TEX2DARRAY(_SplatAlbedoArray, float3(UV * _Splat6Scale, 6)), splat_control2.b);
					if(_HideSplat7 == 0)
					//col = lerp(col, tex2D(_Splat7XP, UV * _Splat7Scale), splat_control2.a);
					col = lerp(col, UNITY_SAMPLE_TEX2DARRAY(_SplatAlbedoArray, float3(UV * _Splat7Scale, 7)), splat_control2.a);
				}

				if(_HideSplat8 == 0){
					float4 UpperAlbedo = tex2D (_SplatUpper, UV * _UpperScale);
					col = lerp(col, UpperAlbedo, UpperAlbedo.a);
				}






				//col = 1;
				half4 nrm;
				//UV *= 0.01;
				nrm = tex2D (_NormalLower, UV * _LowerScale);
				if(_HideSplat0 == 0)
				//nrm = lerp(nrm, tex2D (_SplatNormal0, UV * _Splat0ScaleNormal), splat_control.r);
				nrm = lerp(nrm, UNITY_SAMPLE_TEX2DARRAY(_SplatNormalArray, float3(UV * _Splat0ScaleNormal, 0)), splat_control.r);
				if(_HideSplat1 == 0)
				//nrm =  lerp(nrm, tex2D (_SplatNormal1, UV * _Splat1ScaleNormal), splat_control.g);
				nrm = lerp(nrm, UNITY_SAMPLE_TEX2DARRAY(_SplatNormalArray, float3(UV * _Splat1ScaleNormal, 1)), splat_control.g);
				if(_HideSplat2 == 0)
				//nrm =  lerp(nrm, tex2D (_SplatNormal2, UV * _Splat2ScaleNormal), splat_control.b);
				nrm = lerp(nrm, UNITY_SAMPLE_TEX2DARRAY(_SplatNormalArray, float3(UV * _Splat2ScaleNormal, 2)), splat_control.b);
				if(_HideSplat3 == 0)
				//nrm =  lerp(nrm, tex2D (_SplatNormal3, UV * _Splat3ScaleNormal), splat_control.a);
				nrm = lerp(nrm, UNITY_SAMPLE_TEX2DARRAY(_SplatNormalArray, float3(UV * _Splat3ScaleNormal, 3)), splat_control.a);

				if (_TTerrainXP > 0) {
					if(_HideSplat4 == 0)
					//nrm = lerp(nrm, tex2D(_SplatNormal4, UV * _Splat4ScaleNormal), splat_control2.r);
					nrm = lerp(nrm, UNITY_SAMPLE_TEX2DARRAY(_SplatNormalArray, float3(UV * _Splat4ScaleNormal, 4)), splat_control2.r);
					if(_HideSplat5 == 0)
					//nrm = lerp(nrm, tex2D(_SplatNormal5, UV * _Splat5ScaleNormal), splat_control2.g);
					nrm = lerp(nrm, UNITY_SAMPLE_TEX2DARRAY(_SplatNormalArray, float3(UV * _Splat5ScaleNormal, 5)), splat_control2.g);
					if(_HideSplat6 == 0)
					//nrm = lerp(nrm, tex2D(_SplatNormal6, UV * _Splat6ScaleNormal), splat_control2.b);
					nrm = lerp(nrm, UNITY_SAMPLE_TEX2DARRAY(_SplatNormalArray, float3(UV * _Splat6ScaleNormal, 6)), splat_control2.b);
					if(_HideSplat7 == 0)
					//nrm = lerp(nrm, tex2D(_SplatNormal7, UV * _Splat7ScaleNormal), splat_control2.a);
					nrm = lerp(nrm, UNITY_SAMPLE_TEX2DARRAY(_SplatNormalArray, float3(UV * _Splat7ScaleNormal, 7)), splat_control2.a);
				}



				//nrm = tex2D (_NormalLower, UV * 1000);
				//nrm.rg *= 5;
				//nrm.b = 1;
				//nrm.rgb = UnpackNormal(nrm);
				//nrm.rgb = nrm.rgb * 2 - half3(1, 1, 1);
				//nrm.rg *= 3;
				//nrm.rgb = normalize(nrm.rgb);
				//o.Normal = UnpackNormalDXT5nm(tex2D(_TerrainNormal, UV ));
				//o.Normal = (UnpackNormalDXT5nm(tex2D(_TerrainNormal, UV )) + UnpackNormalDXT5nmScaled(nrm.rgbg, 2));
				if(_GeneratingNormal == 0){
					half4 TerrainNormal = tex2D(_TerrainNormal, UVCtrl );
					half3 TerrainNormalVector = UnpackNormalDXT5nm( half4(TerrainNormal.r, 1 - TerrainNormal.g, TerrainNormal.b, TerrainNormal.a));
					IN.SlopeLerp = dot(TerrainNormalVector, half3(0,0,1));
					//o.Albedo = IN.SlopeLerp;
					o.Normal = BlendNormals(TerrainNormalVector , UnpackNormalDXT5nmScaled(nrm.rgbg, 1));

				}
				else
					o.Normal = UnpackNormalDXT5nmScaled(nrm.rgbg, 1);


				//				o.Normal.y *= 0.5f;
				//o.Normal = normalize(o.Normal);

				//half3 Emit = _SunAmbience.rgb * 2;
				half3 Emit = 0;

				if(_Slope > 0){
					o.Normal = half3(0,0,1);
					half3 SlopeColor = 0;
					if(_UseSlopeTex > 0){
						//col = 0;
						float2 UV2 = IN.uv_Control * fixed2(1, 1) + half2(0, 0) - float2(-0.00, -0.00) / _GridScale;
						float4 splat_control = tex2D (_SlopeTex, UV2);
						SlopeColor = splat_control.rgb;
					
					}
					else{

						if(IN.worldPos.y < _WaterLevel){
							if(IN.SlopeLerp > 0.75) SlopeColor = half3(0,0.4,1);
							else SlopeColor = half3(0.6,0,1);
						}
						else if(IN.SlopeLerp > 0.999) SlopeColor = half3(0,0.8,0);
						else if(IN.SlopeLerp > 0.95) SlopeColor = half3(0.3,0.89,0);
						else if(IN.SlopeLerp > 0.80) SlopeColor = half3(0.5,0.8,0);
						else SlopeColor = half3(1,0,0);

						}
					Emit = SlopeColor * 0.5;
					col.rgb = lerp(col.rgb, SlopeColor, 0.5);
				}
				else if(_Water > 0) col.rgb = ApplyWaterColor(WaterDepth, col.rgb);	
				

				if(_Grid > 0){
					fixed4 GridColor = tex2D (_GridTexture, IN.uv_Control * _GridScale - float2(-0.00, -0.00));
					fixed4 GridFinal = fixed4(0,0,0,GridColor.a);
					if(_GridCamDist < 1){
						GridFinal.rgb = lerp(GridFinal.rgb, fixed3(1,1,1), GridColor.r * lerp(1, 0, _GridCamDist));
						GridFinal.rgb = lerp(GridFinal.rgb, fixed3(0,1,0), GridColor.g * lerp(1, 0, _GridCamDist));
						GridFinal.rgb = lerp(GridFinal.rgb, fixed3(0,1,0), GridColor.b * lerp(0, 1, _GridCamDist));
					}
					else{
					GridFinal.rgb = lerp(GridFinal.rgb, fixed3(0,1,0), GridColor.b);
					}
						  
					//col.rgb = lerp(col.rgb, GridFinal.rgb, GridColor.a);
					Emit += GridFinal * GridColor.a;
				}

				if(_Brush > 0){	
					fixed4 BrushColor = tex2D (_BrushTex, ((IN.uv_Control - float2(_BrushUvX, _BrushUvY)) * _GridScale) / (_BrushSize * _GridScale * 0.002)  );

					half LerpValue = clamp(_BrushSize / 20, 0, 1);

					half From = 0.1f;
					half To = lerp(0.2f, 0.13f, LerpValue);
					half Range = lerp(0.015f, 0.008f, LerpValue);

					if(BrushColor.r >= From && BrushColor.r <= To){
						half AA = 1;

						if (BrushColor.r < From + Range)
							AA = (BrushColor.r - From) / Range;
						else if(BrushColor.r > To - Range)
							AA = 1 - (BrushColor.r - (To - Range)) / Range;

						AA = clamp(AA, 0, 1);

						Emit += half3(0, 0.3, 1) * (AA * 0.8);
					}

					Emit += half3(0, BrushColor.r * 0.1, BrushColor.r * 0.2);
				}


				
				o.Albedo = col;
				o.Emission = Emit;

				if(_Area > 0){
					fixed3 BlackEmit = -1;
					fixed3 Albedo = 0;
					if(IN.worldPos.x < _AreaX){
						o.Emission = BlackEmit;
						o.Albedo = Albedo;
					}
					else if(IN.worldPos.x > _AreaWidht){
						o.Emission = BlackEmit;
						o.Albedo = Albedo;
					}
					else if(IN.worldPos.z < _AreaY - _GridScale){
						o.Emission = BlackEmit;
						o.Albedo = Albedo;
					}
					else if(IN.worldPos.z > _AreaHeight - _GridScale){
						o.Emission = BlackEmit;
						o.Albedo = Albedo;
					}
				}

				//o.Albedo = 0.5;
				//o.Emission = tex2D (_SplatNormal3, UV * 10 );
				//o.Emission = nrm;

				o.Gloss = 0;
				o.Specular = 0;
				o.Alpha = 1;
			}
			ENDCG  


}


FallBack "Diffuse"
}
