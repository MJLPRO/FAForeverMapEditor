﻿using UnityEngine;
using System.Collections;
using UnityStandardAssets.ImageEffects;

public class ScmapEditor : MonoBehaviour {

	const bool SaveStratumToPng = false;

	public		Terrain			Teren;
	public		TerrainData		Data;
	public		Transform		WaterLevel;
	public		MapLuaParser	Scenario;
	public		Camera			Kamera;
	private		float[,] 		heights = new float[1,1];
	public		Light			Sun;
	public		TerrainTexture[]	Textures;
	public		Material			TerrainMaterial;
	public		Material			WaterMaterial;
	public		float			MapHeightScale = 1;
	public		GetGamedataFile	Gamedata;
	public		bool			Grid;
	public		bool			Slope;

	[System.Serializable]
	public class TerrainTexture{
		public	 Texture2D	Albedo;
		public	 Texture2D	Normal;
		public	Vector2		Tilling = Vector2.one;
		//Scmap Data
		public	string		AlbedoPath;
		public	string		NormalPath;
		public	float		AlbedoScale;
		public	float		NormalScale;
	}
	
	public Map map;

	void Start(){
		ToogleGrid(false);
		heights = new float[10,10];
		RestartTerrain();
	}
	
	public IEnumerator LoadScmapFile(){

		map = new Map();

		string MapPath = PlayerPrefs.GetString("MapsPath", "maps/");
		string path = Scenario.ScenarioData.Scmap.Replace("/maps/", MapPath);

		Debug.Log("Load SCMAP file: " + path);


		if(map.Load(path)){
			//printMapDebug(map);
			Vector3 SunDIr = new Vector3(-map.SunDirection.x, -map.SunDirection.y, -map.SunDirection.z);
			Sun.transform.rotation = Quaternion.LookRotation( SunDIr);
			Sun.color = new Color(map.SunColor.x, map.SunColor.y , map.SunColor.z, 1) ;
			Sun.intensity = map.LightingMultiplier * 1.0f;
			//RenderSettings.ambientLight = new Color(map.SunAmbience.x, map.SunAmbience.y, map.SunAmbience.z, 1);
			RenderSettings.ambientLight = new Color(map.ShadowFillColor.x, map.ShadowFillColor.y, map.ShadowFillColor.z, 1);
			//Sun.shadowStrength = 1 - (map.ShadowFillColor.x + map.ShadowFillColor.y + map.ShadowFillColor.z) / 3;

			Kamera.GetComponent<Bloom>().bloomIntensity = map.Bloom * 4;

			RenderSettings.fogColor = new Color(map.FogColor.x, map.FogColor.y, map.FogColor.z, 1);
			RenderSettings.fogStartDistance = map.FogStart * 2;
			RenderSettings.fogEndDistance = map.FogEnd * 2;

			TerrainMaterial.SetFloat("_LightingMultiplier", map.LightingMultiplier);
			TerrainMaterial.SetColor("_SunColor",  new Color(map.SunColor.x * 0.5f, map.SunColor.y * 0.5f, map.SunColor.z * 0.5f, 1));
			TerrainMaterial.SetColor("_SunAmbience",  new Color(map.SunAmbience.x * 0.5f, map.SunAmbience.y * 0.5f, map.SunAmbience.z * 0.5f, 1));
			TerrainMaterial.SetColor("_ShadowColor",  new Color(map.ShadowFillColor.x * 0.5f, map.ShadowFillColor.y * 0.5f, map.ShadowFillColor.z * 0.5f, 1));
		}
		else{
			Debug.LogError("File not found");
			StopCoroutine( "LoadScmapFile" );
		}

		Scenario.ScenarioData.MaxHeight = map.Water.Elevation;
		MapLuaParser.Water = map.Water.HasWater;
		WaterLevel.gameObject.SetActive(map.Water.HasWater);

		// Set Variables
		int xRes = (int)Scenario.ScenarioData.Size.x;
		int zRes = (int)Scenario.ScenarioData.Size.y;
		float yRes = (float)map.HeightScale;;
		float HeightResize = 512 * 40;

		WaterMaterial.SetTexture("_UtilitySamplerC", map.WatermapTex);
		WaterMaterial.SetFloat("_WaterScale", xRes / -10f);
		//

		                     

//*****************************************
// ***** Set Terrain proportives
//*****************************************
		if(Teren) DestroyImmediate(Teren.gameObject);

		// Load Stratum Textures Paths
		for (int i = 0; i < Textures.Length; i++) {
			Textures[i].AlbedoPath = map.Layers[i].PathTexture;
			Textures[i].NormalPath = map.Layers[i].PathNormalmap;
			if(Textures[i].AlbedoPath.StartsWith("/")){
				Textures[i].AlbedoPath = Textures[i].AlbedoPath.Remove(0, 1);
			}
			if(Textures[i].NormalPath.StartsWith("/")){
				Textures[i].NormalPath = Textures[i].NormalPath.Remove(0, 1);
			}
			Textures[i].AlbedoScale = map.Layers[i].ScaleTexture;
			Textures[i].NormalScale = map.Layers[i].ScaleNormalmap;

			Gamedata.LoadTextureFromGamedata("env.scd", Textures[i].AlbedoPath, i, false);
			yield return null;
			Gamedata.LoadTextureFromGamedata("env.scd", Textures[i].NormalPath, i, true);
			yield return null;
		}




		// LoadTextures
		/*SplatPrototype[] tex = new SplatPrototype [Textures.Length - 2];

		for (int i = 0; i < tex.Length; i++) {
			tex[i] = new SplatPrototype ();
			tex[i].texture = Textures[i + 1].Albedo; 
			tex[i].normalMap = Textures[i + 1].Normal;
			tex[i].tileSize = Textures[i + 1].Tilling;
			tex[i].metallic = 0;
			tex[i].smoothness = 0.5f;
		}
		Data.splatPrototypes = tex;*/

		Teren = Terrain.CreateTerrainGameObject( Data ).GetComponent<Terrain>();
		Teren.gameObject.name = "TERRAIN";
		Teren.materialType = Terrain.MaterialType.Custom;
		Teren.materialTemplate = TerrainMaterial;
		Teren.heightmapPixelError = 1;
		Teren.basemapDistance = 10000;
		Teren.castShadows = false;
		Teren.drawTreesAndFoliage = false;
		Teren.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

		Data.heightmapResolution = (int)(xRes + 1);
		Data.size = new Vector3(
			xRes / 10.0f,
			yRes * MapHeightScale,
			zRes / 10.0f
			);
		Data.SetDetailResolution((int)(xRes / 2), 8);
		Data.baseMapResolution = (int)(xRes / 2);
		Data.alphamapResolution = (int)(xRes / 2);

		Teren.transform.localPosition = new Vector3(0, 0, -zRes / 10.0f);


		WaterLevel.transform.localScale = new Vector3(xRes / 10, 1, zRes / 10);
		WaterLevel.transform.position = Vector3.up * (map.Water.Elevation / 10.0f);
		TerrainMaterial.SetFloat("_WaterLevel", map.Water.Elevation / 10.0f);
		TerrainMaterial.SetFloat("_AbyssLevel", map.Water.ElevationAbyss / 10.0f);


		TerrainMaterial.SetInt("_Water", MapLuaParser.Water?1:0);
		//TerrainMaterial.SetFloat("_LowerScale", Textures[0].AlbedoScale / Textures[1].AlbedoScale);
		TerrainMaterial.SetTexture("_SplatLower", Textures[0].Albedo);
		TerrainMaterial.SetTexture("_NormalLower", Textures[0].Normal);
		TerrainMaterial.SetTexture("_UtilitySamplerC", map.WatermapTex);
		TerrainMaterial.SetFloat("_GridScale", xRes / 10f);


		heights = new float[map.Width + 1, map.Height + 1];
		// Modify heights array data
		for (int y = 0; y < map.Width + 1; y++) {
			for (int x = 0; x < map.Height + 1; x++) {
				heights[x,y] = map.GetHeight(y, map.Height - x) / HeightResize ;
			}
		}

		// Set terrain heights from heights array
		Data.SetHeights(0, 0, heights);


		// Mask textures
		/*float[,,] maps = new float[Data.alphamapWidth, Data.alphamapHeight, 8];
		Debug.Log("Load maps: " + Data.alphamapWidth);

		for(int i = 0; i < Data.alphamapWidth; i++){
			for(int e = 0; e < Data.alphamapHeight; e++){

				float stratum1 = map.TexturemapTex.GetPixel(e, Data.alphamapWidth - i - 1).b;
				float stratum2 = map.TexturemapTex.GetPixel(e, Data.alphamapWidth - i - 1).g;
				float stratum3 = map.TexturemapTex.GetPixel(e, Data.alphamapWidth - i - 1).r;
				float stratum4 = map.TexturemapTex.GetPixel(e, Data.alphamapWidth - i - 1).a;
				float stratum5 = map.TexturemapTex2.GetPixel(e, Data.alphamapWidth - i - 1).b;
				float stratum6 = map.TexturemapTex2.GetPixel(e, Data.alphamapWidth - i - 1).g;
				float stratum7 = map.TexturemapTex2.GetPixel(e, Data.alphamapWidth - i - 1).r;
				float stratum8 = map.TexturemapTex2.GetPixel(e, Data.alphamapWidth - i - 1).a;

				maps[i, e, 0] = stratum1; // stratum 1
				maps[i, e, 1] = stratum2; // stratum 2
				maps[i, e, 2] = stratum3; // stratum 3
				maps[i, e, 3] = stratum4; // stratum 4
				maps[i, e, 4] = stratum5; // stratum 5
				maps[i, e, 5] = stratum6; // stratum 6
				maps[i, e, 6] = stratum7; // stratum 7
				maps[i, e, 7] = stratum8; // stratum 8
			}
		}
		yield return null;*/

		// Save stratum mask to files
		if(SaveStratumToPng){
			byte[] bytes;
			string filename = "temfiles/tex1";
			bytes =  map.TexturemapTex.EncodeToPNG();
			filename += ".png";
			System.IO.File.WriteAllBytes(filename, bytes);


			bytes = null;
			filename = "temfiles/tex2";
			bytes =  map.TexturemapTex2.EncodeToPNG();
			filename += ".png";
			System.IO.File.WriteAllBytes(filename, bytes);
		}

		//Data.SetAlphamaps(0, 0, maps);
		Teren.gameObject.layer = 8;
		Teren.heightmapPixelError = 0;


		TerrainMaterial.SetFloat("_LowerScale", map.Width / Textures[0].AlbedoScale);
		TerrainMaterial.SetFloat("_LowerScaleNormal", map.Width / Textures[0].NormalScale);

		TerrainMaterial.SetTexture("_ControlXP", map.TexturemapTex);
		if(Textures[5].Albedo || Textures[6].Albedo || Textures[7].Albedo || Textures[8].Albedo) TerrainMaterial.SetTexture("_Control2XP", map.TexturemapTex2);

		TerrainMaterial.SetTexture("_Splat0XP", Textures[1].Albedo);
		TerrainMaterial.SetTexture("_Splat1XP", Textures[2].Albedo);
		TerrainMaterial.SetTexture("_Splat2XP", Textures[3].Albedo);
		TerrainMaterial.SetTexture("_Splat3XP", Textures[4].Albedo);
		TerrainMaterial.SetTexture("_Splat4XP", Textures[5].Albedo);
		TerrainMaterial.SetTexture("_Splat5XP", Textures[6].Albedo);
		TerrainMaterial.SetTexture("_Splat6XP", Textures[7].Albedo);
		TerrainMaterial.SetTexture("_Splat7XP", Textures[8].Albedo);

		TerrainMaterial.SetFloat("_Splat0Scale", map.Width /Textures[1].AlbedoScale);
		TerrainMaterial.SetFloat("_Splat1Scale", map.Width /Textures[2].AlbedoScale);
		TerrainMaterial.SetFloat("_Splat2Scale", map.Width /Textures[3].AlbedoScale);
		TerrainMaterial.SetFloat("_Splat3Scale", map.Width /Textures[4].AlbedoScale);
		TerrainMaterial.SetFloat("_Splat4Scale", map.Width /Textures[5].AlbedoScale);
		TerrainMaterial.SetFloat("_Splat5Scale", map.Width /Textures[6].AlbedoScale);
		TerrainMaterial.SetFloat("_Splat6Scale", map.Width /Textures[7].AlbedoScale);
		TerrainMaterial.SetFloat("_Splat7Scale", map.Width /Textures[8].AlbedoScale);

		TerrainMaterial.SetTexture("_Normal0", Textures[1].Normal);
		TerrainMaterial.SetTexture("_Normal1", Textures[2].Normal);
		TerrainMaterial.SetTexture("_Normal2", Textures[3].Normal);
		TerrainMaterial.SetTexture("_Normal3", Textures[4].Normal);
		TerrainMaterial.SetTexture("_Normal4", Textures[5].Normal);
		TerrainMaterial.SetTexture("_Normal5", Textures[6].Normal);
		TerrainMaterial.SetTexture("_Normal6", Textures[7].Normal);
		TerrainMaterial.SetTexture("_Normal7", Textures[8].Normal);


		TerrainMaterial.SetFloat("_Splat0ScaleNormal", map.Width / Textures[1].NormalScale);
		TerrainMaterial.SetFloat("_Splat1ScaleNormal", map.Width / Textures[2].NormalScale);
		TerrainMaterial.SetFloat("_Splat2ScaleNormal", map.Width / Textures[3].NormalScale);
		TerrainMaterial.SetFloat("_Splat3ScaleNormal", map.Width / Textures[4].NormalScale);
		TerrainMaterial.SetFloat("_Splat4ScaleNormal", map.Width / Textures[5].NormalScale);
		TerrainMaterial.SetFloat("_Splat5ScaleNormal", map.Width / Textures[6].NormalScale);
		TerrainMaterial.SetFloat("_Splat6ScaleNormal", map.Width / Textures[7].NormalScale);
		TerrainMaterial.SetFloat("_Splat7ScaleNormal", map.Width / Textures[8].NormalScale);

		TerrainMaterial.SetFloat("_UpperScale", map.Width / Textures[9].AlbedoScale);
		TerrainMaterial.SetFloat("_UpperScaleNormal", map.Width / Textures[9].NormalScale);
		TerrainMaterial.SetTexture("_SplatUpper", Textures[9].Albedo);
		TerrainMaterial.SetTexture("_NormalUpper", Textures[9].Normal);


		/*string AllProps = "";

		for(int i = 0; i < map.Props.Count; i++){
			if( !map.Props[i].BlueprintPath.Contains("pine")){
				AllProps += map.Props[i].BlueprintPath + "\n";
			}
		}
		Debug.Log("All Props\n" + AllProps);*/
		yield return null;
	}

	public void SaveScmapFile(){
		heights = Teren.terrainData.GetHeights(0,0,Teren.terrainData.heightmapWidth, Teren.terrainData.heightmapHeight);

		float HeightResize = 512 * 40;
		for (int y = 0; y < map.Width + 1; y++) {
			for (int x = 0; x < map.Height + 1; x++) {
				map.SetHeight(y, map.Height - x,  (short)(heights[x,y] * HeightResize) );
			}
		}
		Debug.Log("Set Heightmap to map " + map.Width + ", " + map.Height);

		string MapPath = PlayerPrefs.GetString("MapsPath", "maps/");
		string path = Scenario.ScenarioData.Scmap.Replace("/maps/", MapPath);

		map.Save(path, map.VersionMinor);
	}

	public void RestartTerrain(){
		int xRes = (int)(256 + 1);
		int zRes = (int)(256 + 1);
		int yRes = (int)(128);
		heights = new float[xRes,zRes];
		
		// Set Terrain proportives
		Data.heightmapResolution = xRes;
		Data.size = new Vector3(
			256 / 10.0f,
			yRes / 10.0f,
			256 / 10.0f
			);
		//Data.SetDetailResolution((int)(256 / 2), 8);
		//Data.baseMapResolution = (int)(256 / 2);
		//Data.alphamapResolution = (int)(256 / 2);
		
		if(map != null) WaterLevel.transform.localScale = new Vector3(map.Width * 0.1f, Scenario.ScenarioData.WaterLevels.x, map.Height * 0.1f);
		if(Teren) Teren.transform.localPosition = new Vector3(-xRes / 20.0f, 1, -zRes / 20.0f);
		
		// Modify heights array data
		for (int y = 0; y < zRes; y++) {
			for (int x = 0; x < xRes; x++) {
				heights[x,y] = 0;
			}
		}
		
		// Set terrain heights from heights array
		Data.SetHeights(0, 0, heights);
	}

	public Vector3 MapPosInWorld(Vector3 MapPos){
		Vector3 ToReturn = MapPos;
		
		// Position
		//ToReturn.x =  1 * (MapPos.x / Scenario.ScenarioData.Size.x) * (Scenario.ScenarioData.Size.x / 10);
		//ToReturn.z = - 1 * (MapPos.z / Scenario.ScenarioData.Size.y) * (Scenario.ScenarioData.Size.y / 10);
		ToReturn.x = MapPos.x / 10f;
		ToReturn.z = -MapPos.z / 10f;
		
		// Height
		ToReturn.y =  1 * (MapPos.y / 10);
		
		return ToReturn;
	}

	public Vector3 MapWorldPosInSave(Vector3 MapPos){
		Vector3 ToReturn = MapPos;
		
		// Position
		//ToReturn.x = (MapPos.x / (Scenario.ScenarioData.Size.x / 10)) * (Scenario.ScenarioData.Size.x) - 0.5f;
		//ToReturn.z = (MapPos.z / -(Scenario.ScenarioData.Size.y / 10)) * (Scenario.ScenarioData.Size.y) - 0.5f;

		ToReturn.x = MapPos.x * 10;
		ToReturn.z = MapPos.z * -10f;
		
		// Height
		ToReturn.y = MapPos.y * 10;
		
		return ToReturn;
	}

	public void ToogleGrid(bool To){
		Grid = To;
		TerrainMaterial.SetInt("_Grid", Grid?1:0);
	}

	public void ToogleSlope(bool To){
		Slope = To;
		TerrainMaterial.SetInt("_Slope", Slope?1:0);
	}
}
