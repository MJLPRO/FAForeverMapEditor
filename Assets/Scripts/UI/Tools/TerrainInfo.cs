﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using EditMap;
using System.IO;

public class TerrainInfo : MonoBehaviour {

	public		CameraControler		KameraKontroler;
	public		Editing				Edit;
	public		ScmapEditor			Map;
	public		MarkersRenderer		Markers;
	public		Camera				GameplayCamera;
	public		Slider				BrushSizeSlider;
	public		InputField			BrushSize;
	public		Slider				BrushStrengthSlider;
	public		InputField			BrushStrength;
	public		Slider				BrushRotationSlider;
	public		InputField			BrushRotation;

	public		InputField			BrushMini;
	public		InputField			BrushMax;

	public		InputField			TerrainSet;
	public		InputField			TerrainAdd;
	public		InputField			TerrainScale;
	public		Toggle				TerrainScale_Height;
	public		InputField			TerrainScale_HeightValue;

	public		GameObject			BrushListObject;
	public		Transform			BrushListPivot;
	public		Material			TerrainMaterial;


	public		LayerMask				TerrainMask;
	public		List<Toggle>			BrushToggles;
	public		ToggleGroup				ToogleGroup;

	[Header("State")]
	public bool Invert;
	public bool Smooth;


	PaintWithBrush.BrushData TerrainBrush = new PaintWithBrush.BrushData ();

	void OnEnable(){
		BrushGenerator.LoadBrushesh ();

		if(!BrusheshLoaded) LoadBrushesh();
		UpdateMenu();
		TerrainMaterial.SetInt("_Brush", 1);
		BrushGenerator.Brushes[SelectedFalloff].wrapMode = TextureWrapMode.Clamp;
		BrushGenerator.Brushes[SelectedFalloff].mipMapBias = -1f;
		TerrainMaterial.SetTexture("_BrushTex", (Texture)BrushGenerator.Brushes[SelectedFalloff]);
	}

	void OnDisable(){
		TerrainMaterial.SetInt("_Brush", 0);
	}


	#region Load all brushesh
	bool BrusheshLoaded = false;
	string StructurePath;
	public void LoadBrushesh(){
		Clean();


		StructurePath = Application.dataPath + "/Structure/";;
		#if UNITY_EDITOR
		StructurePath = StructurePath.Replace("Assets", "");
		#endif
		StructurePath += "brush";

		if(!Directory.Exists(StructurePath)){
			Debug.LogError("Cant find brush folder");
			return;
		}
			
		BrushToggles = new List<Toggle>();

		for(int i = 0; i < BrushGenerator.Brushes.Count; i++){
			GameObject NewBrush = Instantiate(BrushListObject) as GameObject;
			NewBrush.transform.SetParent(BrushListPivot, false);
			NewBrush.transform.localScale = Vector3.one;
			string ThisName = BrushGenerator.BrushesNames[i];
			BrushToggles.Add( NewBrush.GetComponent<BrushListId>().SetBrushList(ThisName, BrushGenerator.Brushes[i], i ));
			NewBrush.GetComponent<BrushListId>().Controler = this;
		}

		foreach(Toggle tog in BrushToggles){
			tog.isOn = false;
			tog.group = ToogleGroup;
		}
		BrushToggles[0].isOn = true;
		SelectedFalloff = 0;

		BrusheshLoaded = true;
	}

	void Clean(){
		BrusheshLoaded = false;
		foreach(Transform child in BrushListPivot) Destroy(child.gameObject);
	}

	#endregion

	#region Update tool
	bool TerainChanged = false;
	float[,] beginHeights;

	Vector3 BeginMousePos;
	float StrengthBeginValue;
	bool ChangingStrength;
	float SizeBeginValue;
	bool ChangingSize;
	void Update () {
		Invert = Input.GetKey(KeyCode.LeftAlt);
		Smooth = Input.GetKey(KeyCode.LeftShift);

		if(Edit.MauseOnGameplay || ChangingStrength || ChangingSize){
			if(!ChangingSize && (Input.GetKey(KeyCode.M) || ChangingStrength)){
				// Change Strength
				if(Input.GetMouseButtonDown(0)){
					ChangingStrength = true;
					BeginMousePos = Input.mousePosition;
					StrengthBeginValue = BrushStrengthSlider.value;
				}
				else if(Input.GetMouseButtonUp(0)){
					ChangingStrength = false;
				}
				if(ChangingStrength){
					BrushStrengthSlider.value = Mathf.Clamp(StrengthBeginValue - (BeginMousePos.x - Input.mousePosition.x), 0, 100);
					UpdateMenu(true);
					//UpdateBrushPosition(true);

				}
			}
			else if(Input.GetKey(KeyCode.B) || ChangingSize){
				// Change Size
				if(Input.GetMouseButtonDown(0)){
					ChangingSize = true;
					BeginMousePos = Input.mousePosition;
					SizeBeginValue = BrushSizeSlider.value;
				}
				else if(Input.GetMouseButtonUp(0)){
					ChangingSize = false;
				}
				if(ChangingSize){
					BrushSizeSlider.value = Mathf.Clamp(SizeBeginValue - (BeginMousePos.x - Input.mousePosition.x), 1, 256);
					UpdateMenu(true);
					UpdateBrushPosition(true);

				}
			}
			else{
				if(Input.GetMouseButtonDown(0)){
					if(UpdateBrushPosition(true)){
						SymmetryPaint();
					}
				}
				else if(Input.GetMouseButton(0)){
					if(UpdateBrushPosition(false)){
						SymmetryPaint();
					}
				}
				else{
					UpdateBrushPosition(true);
				}
			}
		}

		if(TerainChanged && Input.GetMouseButtonUp(0)){
			MapLuaParser.Current.History.RegisterTerrainHeightmapChange(beginHeights);
			TerainChanged = false;
		}

		if(PlayerPrefs.GetInt("Symmetry", 0) != BrushGenerator.LastSym){
			BrushGenerator.GeneratePaintBrushesh();
		}
	}
	public float Min = 0;
	public float Max = 512;
	int LastRotation = 0;
	public void UpdateMenu(bool Slider = false){
		if(Slider){
			BrushSize.text = BrushSizeSlider.value.ToString();
			BrushStrength.text = BrushStrengthSlider.value.ToString();
			//BrushRotation.text = BrushRotationSlider.value.ToString();
		}
		else{
			BrushSizeSlider.value = float.Parse(BrushSize.text);
			BrushStrengthSlider.value = int.Parse(BrushStrength.text);
			//BrushRotationSlider.value = int.Parse(BrushRotation.text);
		}

		BrushSizeSlider.value = Mathf.Clamp(BrushSizeSlider.value, 1, 256);
		BrushStrengthSlider.value = (int)Mathf.Clamp(BrushStrengthSlider.value, 0, 100);
		//BrushRotationSlider.value = (int)Mathf.Clamp(BrushStrengthSlider.value, -360, 360);

		BrushSize.text = BrushSizeSlider.value.ToString();
		BrushStrength.text = BrushStrengthSlider.value.ToString();
		//BrushRotation.text = BrushRotationSlider.value.ToString();

		Min = int.Parse(BrushMini.text) / 128f;
		Max = int.Parse(BrushMax.text) / 128f;

		if(LastRotation != int.Parse(BrushRotation.text)){
			LastRotation = int.Parse( BrushRotation.text);
			if(LastRotation == 0){
				BrushGenerator.RotatedBrush = BrushGenerator.Brushes[SelectedFalloff];
			}
			else{
				BrushGenerator.RotatedBrush = BrushGenerator.rotateTexture(BrushGenerator.Brushes[SelectedFalloff], LastRotation);
			}

			TerrainMaterial.SetTexture("_BrushTex", (Texture)BrushGenerator.RotatedBrush);
			BrushGenerator.GeneratePaintBrushesh();
		}
		TerrainMaterial.SetFloat("_BrushSize", BrushSizeSlider.value );
	}
	#endregion

	#region Set Heightmap

	public void SetTerrainHeight(){
		int h = Map.Teren.terrainData.heightmapHeight;
		int w = Map.Teren.terrainData.heightmapWidth;
		beginHeights = Map.Teren.terrainData.GetHeights(0,0, w, h);
		MapLuaParser.Current.History.RegisterTerrainHeightmapChange(beginHeights);

		float[,] heights = Map.Teren.terrainData.GetHeights(0, 0, Map.Teren.terrainData.heightmapWidth, Map.Teren.terrainData.heightmapHeight);

		for(int i = 0; i < Map.Teren.terrainData.heightmapWidth; i++){
			for(int j = 0; j < Map.Teren.terrainData.heightmapWidth; j++){
				heights[i,j] = int.Parse(TerrainAdd.text) / 128f;
			}
		}
		Map.Teren.terrainData.SetHeights(0, 0, heights);
	}

	public void AddTerrainHeight(){
		int h = Map.Teren.terrainData.heightmapHeight;
		int w = Map.Teren.terrainData.heightmapWidth;
		beginHeights = Map.Teren.terrainData.GetHeights(0,0, w, h);
		MapLuaParser.Current.History.RegisterTerrainHeightmapChange(beginHeights);

		float[,] heights = Map.Teren.terrainData.GetHeights(0, 0, Map.Teren.terrainData.heightmapWidth, Map.Teren.terrainData.heightmapHeight);

		for(int i = 0; i < Map.Teren.terrainData.heightmapWidth; i++){
			for(int j = 0; j < Map.Teren.terrainData.heightmapWidth; j++){
				heights[i,j] += int.Parse(TerrainAdd.text) / 128f;
			}
		}
		Map.Teren.terrainData.SetHeights(0, 0, heights);
	}

	public void ExportHeightmap(){
		string Filename = PlayerPrefs.GetString("MapsPath", "maps/") + MapLuaParser.Current.FolderName + "/heightmap.raw";

		int h = Map.Teren.terrainData.heightmapHeight;
		int w = Map.Teren.terrainData.heightmapWidth;

		float[,] data = Map.Teren.terrainData.GetHeights(0, 0, w, h);

		using (BinaryWriter writer = new BinaryWriter(new System.IO.FileStream(Filename, System.IO.FileMode.Create)))
		{
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					uint ThisPixel = (uint)(data[y,x] * 0xFFFF);
					writer.Write(System.BitConverter.GetBytes(System.BitConverter.ToUInt16(System.BitConverter.GetBytes(ThisPixel),0)));
				}
			}
			writer.Close();
		}
	}

	public void ExportWithSizeHeightmap(){

		int scale =  int.Parse(TerrainScale.text);
		scale = Mathf.Clamp(scale, 129, 2049);

		string Filename = PlayerPrefs.GetString("MapsPath", "maps/") + MapLuaParser.Current.FolderName + "/heightmap.raw";

		int h = Map.Teren.terrainData.heightmapWidth;
		int w = Map.Teren.terrainData.heightmapWidth;

		float[,] data = Map.Teren.terrainData.GetHeights(0, 0, w, h);

		Texture2D ExportAs = new Texture2D(Map.Teren.terrainData.heightmapWidth, Map.Teren.terrainData.heightmapWidth, TextureFormat.RGB24, false);
		Debug.Log(data[128,128]);
		//Debug.Log(data[256,256]);

		float Prop = (float)scale / (float)Map.Teren.terrainData.heightmapWidth;
		float HeightValue = 1;
		HeightValue = float.Parse(TerrainScale_HeightValue.text);
		if(HeightValue < 0) HeightValue = 1;

		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				//Debug.Log(data[y,x]);
				float Value = data[y,x] / (1f / 255f);

				if( TerrainScale_Height.isOn){
							Value *= HeightValue;
				}
				float ColorR = (Mathf.Floor(Value) * (1f / 255f));
				float ColorG = (Value - Mathf.Floor(Value));

				if(x == 128 && y == 128){
					Debug.Log(Value);
					Debug.Log(ColorR +", "+ ColorG);
				}

				ExportAs.SetPixel(h - y - 1, x, new Color(ColorR, ColorG, 0));
			}
		}
		ExportAs.Apply();

		Debug.Log(ExportAs.GetPixel(128, 128).r +", "+ ExportAs.GetPixel(128, 128).g);
		Debug.Log(ExportAs.GetPixel(128, 128).r + ExportAs.GetPixel(128, 128).g * (1f / 255f));

		TextureScale.Bilinear(ExportAs, scale, scale);

		h = scale;
		w = scale;
		Debug.Log(Prop);
		//ExportAs.Resize(scale, scale);
		//ExportAs.Apply();

		using (BinaryWriter writer = new BinaryWriter(new System.IO.FileStream(Filename, System.IO.FileMode.Create)))
		{
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					Color pixel =  ExportAs.GetPixel(y,x);
					float value = (pixel.r + pixel.g * (1f / 255f));
					uint ThisPixel = (uint)( value * 0xFFFF);
					writer.Write(System.BitConverter.GetBytes(System.BitConverter.ToUInt16(System.BitConverter.GetBytes(ThisPixel),0)));
				}
			}
			writer.Close();
		}
		ExportAs = null;

	}

	public void ImportHeightmap(){

		int h = Map.Teren.terrainData.heightmapHeight;
		int w = Map.Teren.terrainData.heightmapWidth;
		beginHeights = Map.Teren.terrainData.GetHeights(0,0, w, h);
		MapLuaParser.Current.History.RegisterTerrainHeightmapChange(beginHeights);

		string Filename = PlayerPrefs.GetString("MapsPath", "maps/") + MapLuaParser.Current.FolderName + "/heightmap.raw";
		if(!File.Exists(Filename)){
			Debug.Log("File not exist: " + Filename);
			return;
		}
			
		float[,] data = new float[h, w];
		using (var file = System.IO.File.OpenRead(  Filename))
		using (var reader = new System.IO.BinaryReader(file))
		{
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					float v = (float)reader.ReadUInt16() / 0xFFFF;
					data[y, x] = v;
				}
			}
		}
		Map.Teren.terrainData.SetHeights(0, 0, data);
	}
	#endregion

	#region Brush Update
	int SelectedBrush = 0;
	public void ChangeBrush(int id){
		SelectedBrush = id;
	}

	int SelectedFalloff = 0;
	public void ChangeFalloff(int id){
		SelectedFalloff = id;
		BrushGenerator.Brushes[SelectedFalloff].wrapMode = TextureWrapMode.Clamp;
		BrushGenerator.Brushes[SelectedFalloff].mipMapBias = -1f;
		LastRotation = int.Parse( BrushRotation.text);
		if(LastRotation == 0){
			BrushGenerator.RotatedBrush = BrushGenerator.Brushes[SelectedFalloff];
		}
		else{
			BrushGenerator.RotatedBrush = BrushGenerator.rotateTexture(BrushGenerator.Brushes[SelectedFalloff], LastRotation);
		}
		TerrainMaterial.SetTexture("_BrushTex", (Texture)BrushGenerator.RotatedBrush);
		BrushGenerator.GeneratePaintBrushesh();
	}


	Vector3 BrushPos;
	Vector3 MouseBeginClick;
	bool UpdateBrushPosition(bool Forced = false){
		//Debug.Log(Vector3.Distance(MouseBeginClick, Input.mousePosition));
		if(Forced || Vector3.Distance(MouseBeginClick, Input.mousePosition) > 1){}
		else{
			return false;
		}


		MouseBeginClick = Input.mousePosition;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, 2000, TerrainMask)){
			BrushPos = hit.point;
			BrushPos.y = Map.Teren.SampleHeight(BrushPos);

			Vector3 tempCoord = Map.Teren.gameObject.transform.InverseTransformPoint(BrushPos);
			Vector3 coord  = Vector3.zero;
			coord.x = (tempCoord.x -  (int)BrushSizeSlider.value * MapLuaParser.Current.ScenarioData.Size.x * 0.0001f) / Map.Teren.terrainData.size.x; // TODO 0.05 ?? this should be terrain proportion?
			//coord.y = tempCoord.y / Map.Teren.terrainData.size.y;
			coord.z = (tempCoord.z -  (int)BrushSizeSlider.value * MapLuaParser.Current.ScenarioData.Size.y * 0.0001f) / Map.Teren.terrainData.size.z;

			TerrainMaterial.SetFloat("_BrushSize", BrushSizeSlider.value );
			TerrainMaterial.SetFloat("_BrushUvX", coord.x );
			TerrainMaterial.SetFloat("_BrushUvY", coord.z );

			return true;
		}
		return false;
	}
	#endregion

	void SymmetryPaint(){
		BrushGenerator.GenerateSymmetry(BrushPos);
		/*

		float[,] AllHeights = Map.Teren.terrainData.GetHeights (0, 0, Map.Teren.terrainData.heightmapWidth, Map.Teren.terrainData.heightmapHeight);

		TerrainBrush.size = (int)BrushSizeSlider.value;
		TerrainBrush.strength = BrushStrengthSlider.value;
		TerrainBrush.Invert = Invert;
		TerrainBrush.MinMax = new Vector2 (Min, Max);

		if (Smooth || SelectedBrush == 2) {
			TerrainBrush.BrushType = PaintWithBrush.BrushTypes.Smooth;
		} else if (SelectedBrush == 3) {
			TerrainBrush.BrushType = PaintWithBrush.BrushTypes.Sharpen;
		}
		else{
			TerrainBrush.BrushType = PaintWithBrush.BrushTypes.Standard;
			}

		PaintWithBrush.PaintWithSymmetry (ref AllHeights, TerrainBrush);

		Map.Teren.terrainData.SetHeights (0, 0, AllHeights);
*/
		for(int i = 0; i < BrushGenerator.PaintPositions.Length; i++){
			Paint(BrushGenerator.PaintPositions[i], i);

		}
		Map.Teren.ApplyDelayedHeightmapModification ();
	}


	#region Old Painting
	void Paint(Vector3 AtPosition, int id = 0){
		int hmWidth = Map.Teren.terrainData.heightmapWidth;
		int hmHeight = Map.Teren.terrainData.heightmapHeight;

		Vector3 tempCoord = Map.Teren.gameObject.transform.InverseTransformPoint(AtPosition);
		Vector3 coord  = Vector3.zero;
		coord.x = tempCoord.x / Map.Teren.terrainData.size.x;
		//coord.y = tempCoord.y / Map.Teren.terrainData.size.y;
		coord.z = tempCoord.z / Map.Teren.terrainData.size.z;

		if(coord.x > 1) return;
		if(coord.x < 0) return;
		if(coord.z > 1) return;
		if(coord.z < 0) return;

		// get the position of the terrain heightmap where this game object is
		int posXInTerrain = (int) (coord.x * hmWidth); 
		int posYInTerrain = (int) (coord.z * hmHeight);
		// we set an offset so that all the raising terrain is under this game object
		int size = (int)BrushSizeSlider.value;
		int offset = size / 2;
		// get the heights of the terrain under this game object

		// Horizontal Brush Offsets
		int OffsetLeft = 0;
		if(posXInTerrain-offset < 0) OffsetLeft = Mathf.Abs(posXInTerrain-offset);
		int OffsetRight = 0;
		if(posXInTerrain-offset + size > Map.Teren.terrainData.heightmapWidth) OffsetRight = posXInTerrain-offset + size - Map.Teren.terrainData.heightmapWidth;

		// Vertical Brush Offsets
		int OffsetDown = 0;
		if(posYInTerrain-offset < 0) OffsetDown = Mathf.Abs(posYInTerrain-offset);
		int OffsetTop = 0;
		if(posYInTerrain-offset + size > Map.Teren.terrainData.heightmapWidth) OffsetTop = posYInTerrain-offset + size - Map.Teren.terrainData.heightmapWidth;

		float[,] heights = Map.Teren.terrainData.GetHeights(posXInTerrain-offset + OffsetLeft, posYInTerrain-offset + OffsetDown ,(size - OffsetLeft) - OffsetRight, (size - OffsetDown) - OffsetTop);
		float CenterHeight = 0;

		if(Smooth || SelectedBrush == 2 || SelectedBrush == 3){
			for (int i=0; i<(size - OffsetDown) - OffsetTop; i++){
				for (int j=0; j<(size - OffsetLeft) - OffsetRight; j++){
					CenterHeight += heights[i,j];
				}
			}
			CenterHeight /= size * size;
		}

		for (int i = 0; i<(size - OffsetDown) - OffsetTop; i++){
			for (int j = 0; j<(size - OffsetLeft) - OffsetRight; j++){
				// Brush strength
				int x = (int)(((i + OffsetDown) / (float)size) * BrushGenerator.PaintImage[id].width);
				int y = (int)(((j + OffsetLeft) / (float)size) * BrushGenerator.PaintImage[id].height);
				Color BrushValue =  BrushGenerator.PaintImage[id].GetPixel(y, x);
				float SambleBrush = BrushValue.r;
				if(SambleBrush >= 0.02f) {
					if(Smooth || SelectedBrush == 2){
						float PixelPower = Mathf.Abs( heights[i,j] - CenterHeight);
						heights[i,j] = Mathf.Lerp(heights[i,j], CenterHeight, BrushStrengthSlider.value * 0.4f * Mathf.Pow(SambleBrush, 2) * PixelPower);
					}
					else if(SelectedBrush == 3){
						float PixelPower = heights[i,j] - CenterHeight;
						heights[i,j] += Mathf.Lerp(PixelPower, 0, PixelPower * 10) * BrushStrengthSlider.value * 0.01f * Mathf.Pow(SambleBrush, 2);
					}
					else{
						heights[i,j] += SambleBrush * BrushStrengthSlider.value * 0.0002f * (Invert?(-1):1);
					}

					heights[i,j] = Mathf.Clamp(heights[i,j], Min, Max);
				}
			}
		}
		// set the new height
		if(!TerainChanged){
			beginHeights = Map.Teren.terrainData.GetHeights(0,0, hmWidth, hmHeight);
			TerainChanged = true;
		}

		Map.Teren.terrainData.SetHeightsDelayLOD(posXInTerrain-offset + OffsetLeft, posYInTerrain-offset + OffsetDown,heights);
		Markers.UpdateMarkersHeights();
	}
	#endregion
}