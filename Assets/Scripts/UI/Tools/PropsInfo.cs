﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Ozone.UI;
using System.Runtime.InteropServices;
using SFB;
using System.IO;
using FAF.MapEditor;

namespace EditMap
{
	public class PropsInfo : MonoBehaviour
	{

		public static PropsInfo Current;

		[Header("Connections")]
		public Editing Edit;
		public static List<PropTypeGroup> AllPropsTypes;
		public Transform Pivot;
		public GameObject PropGroupObject;
		public Text TotalMass;
		public Text TotalEnergy;
		public Text TotalTime;
		public GameObject[] Tabs;
		public GameObject[] TabSelected;
		public GameObject PropObjectPrefab;
		public Transform PropsParent;

		[Header("Brush")]
		//public Slider BrushSizeSlider;
		public UiTextField BrushSize;
		//public Slider BrushStrengthSlider;
		public UiTextField BrushStrength;
		public AnimationCurve StrengthToField;
		public UiTextField BrushMini;
		public UiTextField BrushMax;
		public UiTextField Scatter;
		public Toggle AllowWaterLevel;
		public Toggle SnapToGround;
		public LayerMask TerrainMask;
		public Material TerrainMaterial;
		public Material PropMaterial;
		public Material UnitMaterial;

		float TotalMassCount = 0;
		float TotalEnergyCount = 0;
		float TotalReclaimTime = 0;

		const int SelectedFalloff = 0;


		[Header("State")]
		public bool Invert;

		#region Classes
		public class PropTypeGroup
		{
			public string Blueprint = "";
			public string LoadBlueprint
			{
				get
				{
					return GetGamedataFile.LocalBlueprintPath(Blueprint);
				}
			}

			//public string LoadBlueprint = "";
			public string HelpText = "";
			//public Prop[] Props = new Prop[0];
			public GetGamedataFile.PropObject PropObject;
			public HashSet<Prop> PropsInstances = new HashSet<Prop>();

			public void SetNewInstances(HashSet<Prop> NewProps)
			{
				HashSet<PropGameObject> ToRemove = new HashSet<PropGameObject>();

				foreach (Prop OldInstance in PropsInstances)
				{
					if (!NewProps.Contains(OldInstance) && OldInstance.Obj)
					{
						ToRemove.Add(OldInstance.Obj);
					}
				}

				foreach (Prop NewInstance in NewProps)
				{
					if (!NewInstance.Obj) // !PropsInstances.Contains(NewInstance)
					{
						NewInstance.Group = this;
						NewInstance.CreateObject();
					}
				}

				foreach (PropGameObject PropObj in ToRemove)
				{
					Destroy(PropObj.gameObject);
				}

				PropsInstances.Clear();
				PropsInstances = NewProps;
			}

			public PropTypeGroup()
			{
				PropsInstances = new HashSet<Prop>();
			}

			public PropTypeGroup(GetGamedataFile.PropObject FromPropObject)
			{
				PropObject = FromPropObject;
				Blueprint = PropObject.BP.Path;
				HelpText = PropObject.BP.HelpText;

				PropsInstances = new HashSet<Prop>();
			}

			public Prop[] GenerateSupComProps()
			{
				int count = PropsInstances.Count;
				Prop[] Props = new Prop[count];

				int i = 0;
				foreach (Prop PropInstance in PropsInstances)
				{
					PropInstance.Bake();

					Props[i] = PropInstance;
					i++;
				}
				return Props;
			}
		}
		#endregion

		void Awake()
		{
			Current = this;
			ShowTab(0);
		}


		void OnEnable()
		{

			BrushGenerator.Current.LoadBrushes();
			TerrainMaterial.SetInt("_Brush", 1);

			BrushGenerator.Current.Brushes[SelectedFalloff].wrapMode = TextureWrapMode.Clamp;
			BrushGenerator.Current.Brushes[SelectedFalloff].mipMapBias = -1f;
			TerrainMaterial.SetTexture("_BrushTex", (Texture)BrushGenerator.Current.Brushes[SelectedFalloff]);
			AllowBrushUpdate = true;
			UndoRegistered = false;

			ReloadPropStats();
		}

		void OnDisable()
		{
			CleanSettingsList();
			TerrainMaterial.SetInt("_Brush", 0);
			TerrainMaterial.SetFloat("_BrushSize", 0);
			UndoRegistered = false;
		}

		void Update()
		{
			if (Tabs[0].activeSelf && AllowBrushUpdate)
			{
				BrushUpdate();
			}
		}

		#region Loading Assets
		public static void UnloadProps()
		{
			PropsRenderer.StopPropsUpdate();

			if (AllPropsTypes != null && AllPropsTypes.Count > 0)
				for (int i = 0; i < AllPropsTypes.Count; i++)
				{
					foreach (Prop PropInstance in AllPropsTypes[i].PropsInstances)
					{
						Destroy(PropInstance.Obj.gameObject);
					}
				}

			AllPropsTypes = new List<PropTypeGroup>();
			if (Current)
			{
				Current.TotalMassCount = 0;
				Current.TotalEnergyCount = 0;
				Current.TotalReclaimTime = 0;
			}
		}

		public bool LoadingProps;
		public int LoadedCount = 0;
		public IEnumerator LoadProps()
		{
			LoadingProps = true;
			UnloadProps();

			List<Prop> Props = ScmapEditor.Current.map.Props;

			//Debug.Log("Found props: " + Props.Count);

			const int YieldStep = 1000;
			int LoadCounter = YieldStep;
			int Count = Props.Count;
			LoadedCount = 0;

			bool AllowFarLod = Count < 10000;

			for (int i = 0; i < Count; i++)
			{
				bool NewProp = false;
				int GroupId = 0;
				if (AllPropsTypes.Count == 0) NewProp = true;
				else
				{
					NewProp = true;
					for (int g = 0; g < AllPropsTypes.Count; g++)
					{
						if (Props[i].BlueprintPath == AllPropsTypes[g].Blueprint)
						{
							NewProp = false;
							GroupId = g;
							break;
						}
					}
				}

				if (NewProp)
				{
					GroupId = AllPropsTypes.Count;
					AllPropsTypes.Add(new PropTypeGroup());
					AllPropsTypes[GroupId].Blueprint = Props[i].BlueprintPath;

					AllPropsTypes[GroupId].PropObject = GetGamedataFile.LoadProp(AllPropsTypes[GroupId].Blueprint);
					LoadCounter = YieldStep;
					yield return null;
				}

				Props[i].GroupId = GroupId;
				Props[i].CreateObject(AllowFarLod);
				/*
				Props[i].Obj = AllPropsTypes[GroupId].PropObject.CreatePropGameObject(
						ScmapEditor.ScmapPosToWorld(Props[i].Position),
						MassMath.QuaternionFromRotationMatrix(Props[i].RotationX, Props[i].RotationY, Props[i].RotationZ),
						Props[i].Scale, AllowFarLod
						);
						*/

				AllPropsTypes[GroupId].PropsInstances.Add(Props[i]);

				LoadedCount++;
				LoadCounter--;
				if (LoadCounter <= 0)
				{
					LoadCounter = YieldStep;
					yield return null;
				}


				TotalMassCount += AllPropsTypes[GroupId].PropObject.BP.ReclaimMassMax;
				TotalEnergyCount += AllPropsTypes[GroupId].PropObject.BP.ReclaimEnergyMax;
				TotalReclaimTime += AllPropsTypes[GroupId].PropObject.BP.ReclaimTime;
			}

			UpdatePropStats();

			yield return null;
			LoadingProps = false;

			//Debug.Log("Props types: " + AllPropsTypes.Count);
		}

		public void ReloadPropStats()
		{
			TotalMassCount = 0;
			TotalEnergyCount = 0;
			TotalReclaimTime = 0;

			int AllPropsTypesCount = AllPropsTypes.Count;
			for (int i = 0; i < AllPropsTypesCount; i++)
			{
				int InstancesCount = AllPropsTypes[i].PropsInstances.Count;
				TotalMassCount += AllPropsTypes[i].PropObject.BP.ReclaimMassMax * InstancesCount;
				TotalEnergyCount += AllPropsTypes[i].PropObject.BP.ReclaimEnergyMax * InstancesCount;
				TotalReclaimTime += AllPropsTypes[i].PropObject.BP.ReclaimTime * InstancesCount;
			}
			UpdatePropStats();
		}

		void UpdatePropStats()
		{
			TotalMass.text = TotalMassCount.ToString();
			TotalEnergy.text = TotalEnergyCount.ToString();
			TotalTime.text = TotalReclaimTime.ToString();
		}

		#endregion


		#region Current Reclaims

		public void ShowReclaimGroups()
		{
			CleanSettingsList();

			if (AllPropsTypes.Count == 0)
			{
				Debug.LogWarning("Props count is 0");
				return;
			}

			for (int i = 0; i < AllPropsTypes.Count; i++)
			{

				GameObject NewListObject = Instantiate(PropGroupObject) as GameObject;
				NewListObject.transform.SetParent(Pivot, false);
				NewListObject.transform.localScale = Vector3.one;
				NewListObject.GetComponent<PropData>().SetPropList(i, AllPropsTypes[i].PropObject.BP.Name, AllPropsTypes[i].PropObject.BP.ReclaimMassMax, AllPropsTypes[i].PropObject.BP.ReclaimEnergyMax, AllPropsTypes[i].PropsInstances.Count, AllPropsTypes[i].Blueprint);

				/*
				TotalMassCount += AllPropsTypes[i].Props.Count * AllPropsTypes[i].PropObject.BP.ReclaimMassMax;
				TotalEnergyCount += AllPropsTypes[i].Props.Count * AllPropsTypes[i].PropObject.BP.ReclaimEnergyMax;
				TotalReclaimTime += AllPropsTypes[i].Props.Count * AllPropsTypes[i].PropObject.BP.ReclaimTime;

				TotalMass.text = TotalMassCount.ToString();
				TotalEnergy.text = TotalEnergyCount.ToString();
				TotalTime.text = TotalReclaimTime.ToString();*/
			}
		}

		public void Clean()
		{
			CleanSettingsList();
			CleanPaintList();

			TotalMassCount = 0;
			TotalEnergyCount = 0;
			TotalReclaimTime = 0;
			PaintPropObjects = new List<GetGamedataFile.PropObject>();
			PaintButtons = new List<PropData>();
		}

		public void CleanPaintList()
		{
			if (PaintPropPivot.childCount > 0)
			{
				foreach (Transform child in PaintPropPivot) Destroy(child.gameObject);
			}
		}

		public void CleanSettingsList()
		{
			if (Pivot.childCount > 0)
			{
				foreach (Transform child in Pivot) Destroy(child.gameObject);
			}
		}

		#endregion

		#region UI
		List<GetGamedataFile.PropObject> PaintPropObjects = new List<GetGamedataFile.PropObject>();
		List<PropData> PaintButtons = new List<PropData>();

		public GameObject PaintPropListObject;
		public Transform PaintPropPivot;
		public StratumLayerBtnPreview Preview;

		public void OpenResorceBrowser()
		{
			ResourceBrowser.Current.LoadPropBlueprint();
		}

		public void DropProp()
		{
			if (!ResourceBrowser.Current.gameObject.activeSelf && ResourceBrowser.DragedObject)
				return;
			if (ResourceBrowser.SelectedCategory == 3)
			{
				LoadProp(ResourceBrowser.Current.LoadedProps[ResourceBrowser.DragedObject.InstanceId]);
			}
		}

		bool LoadProp(GetGamedataFile.PropObject PropObj)
		{
			if (!PaintPropObjects.Contains(PropObj))
			{
				PaintPropObjects.Add(PropObj);

				GameObject NewPropListObject = Instantiate(PaintPropListObject, PaintPropPivot) as GameObject;
				PropData pb = NewPropListObject.GetComponent<PropData>();
				pb.SetPropPaint(PaintPropObjects.Count - 1, PropObj.BP.Name);
				PaintButtons.Add(pb);
				return true;
			}
			return false;
		}

		public void RemoveProp(int ID)
		{
			//CleanPaintList();
			Preview.Hide(PaintPropPivot.GetChild(ID).gameObject);
			Destroy(PaintButtons[ID].gameObject);
			PaintPropObjects.RemoveAt(ID);
			PaintButtons.RemoveAt(ID);

			for (int i = 0; i < PaintPropObjects.Count; i++)
			{
				//GameObject NewPropListObject = Instantiate(PaintPropListObject, PaintPropPivot) as GameObject;
				//NewPropListObject.GetComponent<PropData>().SetPropPaint(i, PaintPropObjects[i].BP.Name);

				PaintButtons[i].SetPropPaint(i, PaintPropObjects[i].BP.Name);
			}
		}

		public void ShowPreview(int ID, GameObject Parent)
		{
			Preview.Show(PaintPropObjects[ID].BP.LODs[0].Albedo, Parent, 35f);
		}

		public void ShowTab(int id)
		{
			for (int i = 0; i < Tabs.Length; i++)
			{
				Tabs[i].SetActive(i == id);
				TabSelected[i].SetActive(i == id);
			}


			if (id == 1)
				ShowReclaimGroups();
		}


		bool InforeUpdate = false;
		public void UpdateBrushMenu(bool Slider)
		{
			if (InforeUpdate)
				return;

			UpdateBrushPosition(true);
		}


		#endregion

		#region Brush Update
		Vector3 BrushPos;
		Vector3 MouseBeginClick;
		Vector3 BeginMousePos;
		public bool AllowBrushUpdate = false;
		float StrengthBeginValue;
		bool ChangingStrength;
		float SizeBeginValue;
		bool ChangingSize;

		void BrushUpdate()
		{
			Invert = Input.GetKey(KeyCode.LeftAlt);

			if (Edit.MauseOnGameplay || ChangingStrength || ChangingSize)
			{
				if (!ChangingSize && (Input.GetKey(KeyCode.M) || ChangingStrength))
				{
					// Change Strength
					if (Input.GetMouseButtonDown(0))
					{
						ChangingStrength = true;
						BeginMousePos = Input.mousePosition;
						StrengthBeginValue = BrushStrength.value;
					}
					else if (Input.GetMouseButtonUp(0))
					{
						ChangingStrength = false;
						UndoRegistered = false;
					}
					if (ChangingStrength)
					{
						BrushStrength.SetValue(Mathf.Clamp(StrengthBeginValue - (int)((BeginMousePos.x - Input.mousePosition.x) * 0.1f), 0, 100));
						UpdateBrushMenu(true);

					}
				}
				else if (Input.GetKey(KeyCode.B) || ChangingSize)
				{
					// Change Size
					if (Input.GetMouseButtonDown(0))
					{
						ChangingSize = true;
						BeginMousePos = Input.mousePosition;
						SizeBeginValue = BrushSize.value;
					}
					else if (Input.GetMouseButtonUp(0))
					{
						ChangingSize = false;
						UndoRegistered = false;
					}
					if (ChangingSize)
					{
						BrushSize.SetValue(Mathf.Clamp(SizeBeginValue - (int)((BeginMousePos.x - Input.mousePosition.x) * 4f) * 0.075f, MinimumBrushSize, MaximumBrushSize));
						//BrushSize.SetValue(Mathf.Clamp(SizeBeginValue - (int)((BeginMousePos.x - Input.mousePosition.x) * 0.4f), 0, 256));
						UpdateBrushPosition(true, true, true);
					}
				}
				else
				{
					if (Edit.MauseOnGameplay && Input.GetMouseButtonDown(0))
					{
						BrushGenerator.Current.UpdateSymmetryType();

						if (CameraControler.Current.DragStartedGameplay && UpdateBrushPosition(true))
						{
							SymmetryPaint(true);
						}
					}
					else if (Input.GetMouseButton(0))
					{
						if (CameraControler.Current.DragStartedGameplay)
						{
							if (UpdateBrushPosition(false))
							{
								SymmetryPaint(false);
							}
						}
					}
					else if (Input.GetMouseButtonUp(0))
					{
						if (Painting)
						{
							UpdatePropStats();
							Painting = false;
						}

						UndoRegistered = false;
						UpdateBrushPosition(true);
					}
					else
					{
						UpdateBrushPosition(true);
					}
				}
			}
		}

		bool UndoRegistered = false;

		const float MinimumRenderBrushSize = 0.1f;
		const float MinimumBrushSize = 0.0f;
		const float MaximumBrushSize = 256;
		bool UpdateBrushPosition(bool Forced = false, bool Size = true, bool Position = true)
		{
			//Debug.Log(Vector3.Distance(MouseBeginClick, Input.mousePosition));
			if (Forced || Vector3.Distance(MouseBeginClick, Input.mousePosition) > 1) { }
			else
			{
				return false;
			}

			float SizeXprop = MapLuaParser.GetMapSizeX() / 512f;
			float SizeZprop = MapLuaParser.GetMapSizeY() / 512f;
			float BrushSizeValue = BrushSize.value;
			if (BrushSizeValue < 0.2f)
				BrushSizeValue = 0.2f;

			if (Size)
				TerrainMaterial.SetFloat("_BrushSize", BrushSizeValue / ((SizeXprop + SizeZprop) / 2f));


			MouseBeginClick = Input.mousePosition;
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (Position && Physics.Raycast(ray, out hit, 2000, TerrainMask))
			{
				BrushPos = hit.point;
				if (SnapToGround.isOn && BrushSize.value < 1.5f)
				{
					//BrushPos = Vector3.Lerp(ScmapEditor.SnapToSmallGridCenter(BrushPos), BrushPos, (BrushSize.value - 0.2f) / 1.5f);
					BrushPos = ScmapEditor.SnapToSmallGrid(BrushPos + new Vector3(0.025f, 0, -0.025f));
				}

				BrushPos.y = ScmapEditor.Current.Teren.SampleHeight(BrushPos);

				Vector3 tempCoord = ScmapEditor.Current.Teren.gameObject.transform.InverseTransformPoint(BrushPos);
				Vector3 coord = Vector3.zero;
				float SizeX = (int)((BrushSizeValue / SizeXprop) * 100) * 0.01f;
				float SizeZ = (int)((BrushSizeValue / SizeZprop) * 100) * 0.01f;
				coord.x = (tempCoord.x - SizeX * MapLuaParser.GetMapSizeX() * 0.0001f) / ScmapEditor.Current.Teren.terrainData.size.x;
				coord.z = (tempCoord.z - SizeZ * MapLuaParser.GetMapSizeY() * 0.0001f) / ScmapEditor.Current.Teren.terrainData.size.z;

				TerrainMaterial.SetFloat("_BrushUvX", coord.x);
				TerrainMaterial.SetFloat("_BrushUvY", coord.z);

				return true;
			}
			return false;
		}

		#endregion

		#region Painting
		bool _Painting = false;
		bool Painting
		{
			set
			{
				TerrainMaterial.SetInt("_BrushPainting", _Painting ? (1) : (0));
				_Painting = value;
			}
			get
			{
				return _Painting;
			}
		}

		float size = 0;
		int RandomProp = 0;
		int RandomPropGroup = 0;
		float RandomScale = 1f;
		float StepCount = 100;
		public void SymmetryPaint(bool forced = false)
		{
			Painting = true;
			int Count = PaintPropObjects.Count;
			if (Count <= 0 && !Invert)
			{
#if UNITY_EDITOR
				Debug.Log("No props selected");

#endif
				return;
			}

			//size = BrushSize.value * MapLuaParser.GetMapSizeX() * 0.0001f;
			//float SizeXprop = MapLuaParser.GetMapSizeX() / 512f;
			//float SizeZprop = MapLuaParser.GetMapSizeY() / 512f;
			//size = BrushSize.value / ((SizeXprop + SizeZprop) / 2f);
			size = BrushSize.value * 0.05f;

			//float BrushField = Mathf.PI * (size * size);
			//BrushField /= 16f;

			//Debug.Log(size + ", " + BrushField);

			float BrushField = Mathf.PI * Mathf.Pow(size, 2);

			StepCount += BrushField * StrengthToField.Evaluate(BrushStrength.value);

			// Check if paint
			//StepCount--;
			//if (StepCount >= Mathf.Lerp(BrushStrength.value, 100, Mathf.Sqrt(size / 7f)) && !forced)
			//	return;
			if (forced)
				StepCount = 101;

			while (StepCount > 100)
			{
				StepCount -= 100;
				DoPaintSymmetryPaint();
			}
		}

		void DoPaintSymmetryPaint()
		{
			if (Invert)
			{
				float Tolerance = SymmetryWindow.GetTolerance();

				BrushGenerator.Current.GenerateSymmetry(BrushPos, 0, Scatter.value, size);

				float SearchSize = Mathf.Clamp(size, MinimumRenderBrushSize, MaximumBrushSize);

				// Search props by grid
				//int ClosestG = -1;
				//int ClosestP = -1;
				PropGameObject ClosestInstance = SearchClosestProp(BrushGenerator.Current.PaintPositions[0], SearchSize);

				if (ClosestInstance == null)
					return; // No props found

				BrushPos = ClosestInstance.transform.position;
				BrushGenerator.Current.GenerateSymmetry(BrushPos, 0, 0, 0);

				for (int i = 0; i < BrushGenerator.Current.PaintPositions.Length; i++)
				{
					if (i == 0)
					{
						RegisterUndo();
						//AllPropsTypes[ClosestG].PropsInstances.RemoveAt(ClosestP);

						TotalMassCount -= ClosestInstance.Connected.Group.PropObject.BP.ReclaimMassMax;
						TotalEnergyCount -= ClosestInstance.Connected.Group.PropObject.BP.ReclaimEnergyMax;
						TotalReclaimTime -= ClosestInstance.Connected.Group.PropObject.BP.ReclaimTime;

						ClosestInstance.Connected.Group.PropsInstances.Remove(ClosestInstance.Connected);
						//AllPropsTypes[ClosestG].PropsInstances.Remove(ClosestInstance.Connected);
						Destroy(ClosestInstance.gameObject);

					}
					else
					{
						PropGameObject TestObj = SearchClosestProp(BrushGenerator.Current.PaintPositions[i], Tolerance);
						if (TestObj != null)
						{

							TotalMassCount -= TestObj.Connected.Group.PropObject.BP.ReclaimMassMax;
							TotalEnergyCount -= TestObj.Connected.Group.PropObject.BP.ReclaimEnergyMax;
							TotalReclaimTime -= TestObj.Connected.Group.PropObject.BP.ReclaimTime;

							TestObj.Connected.Group.PropsInstances.Remove(TestObj.Connected);
							//AllPropsTypes[ClosestG].PropsInstances.Remove(TestObj.Connected);
							Destroy(TestObj.gameObject);

						}
					}
				}

			}
			else
			{

				RandomProp = GetRandomProp();
				RandomScale = Random.Range(PaintButtons[RandomProp].ScaleMin.value, PaintButtons[RandomProp].ScaleMax.value);

				BrushGenerator.Current.GenerateSymmetry(BrushPos, size, Scatter.value, size);

				float RotMin = PaintButtons[RandomProp].RotationMin.intValue;
				float RotMax = PaintButtons[RandomProp].RotationMax.intValue;

				BrushGenerator.Current.GenerateRotationSymmetry(Quaternion.Euler(Vector3.up * Random.Range(RotMin, RotMax)));



				// Search group id
				RandomPropGroup = -1;
				for (int i = 0; i < AllPropsTypes.Count; i++)
				{
					if (AllPropsTypes[i].LoadBlueprint == PaintPropObjects[RandomProp].BP.Path)
					{
						RandomPropGroup = i;
						break;
					}
				}
				if (RandomPropGroup < 0) // Create new group
				{
					PropTypeGroup NewGroup = new PropTypeGroup(PaintPropObjects[RandomProp]);
					RandomPropGroup = AllPropsTypes.Count;
					AllPropsTypes.Add(NewGroup);
				}

				//float BrushSlope = ScmapEditor.Current.Teren.
				int Min = BrushMini.intValue;
				int Max = BrushMax.intValue;

				if (Min > 0 || Max < 90)
				{

					Vector3 LocalPos = ScmapEditor.Current.Teren.transform.InverseTransformPoint(BrushGenerator.Current.PaintPositions[0]);
					LocalPos.x /= ScmapEditor.Current.Teren.terrainData.size.x;
					LocalPos.z /= ScmapEditor.Current.Teren.terrainData.size.z;

					float angle = Vector3.Angle(Vector3.up, ScmapEditor.Current.Teren.terrainData.GetInterpolatedNormal(LocalPos.x, LocalPos.z));
					if ((angle < Min && Min > 0) || (angle > Max && Max < 90))
						return;
				}

				if (!AllowWaterLevel.isOn && ScmapEditor.Current.map.Water.HasWater)
					if (ScmapEditor.Current.Teren.SampleHeight(BrushGenerator.Current.PaintPositions[0]) <= ScmapEditor.Current.WaterLevel.position.y)
						return;

				for (int i = 0; i < BrushGenerator.Current.PaintPositions.Length; i++)
				{
					Paint(BrushGenerator.Current.PaintPositions[i], BrushGenerator.Current.PaintRotations[i]);
				}
			}
		}

		int GetRandomProp()
		{
			int Count = PaintPropObjects.Count;
			int TotalValue = 0;
			for (int i = 0; i < Count; i++)
			{
				TotalValue += PaintButtons[i].Chance.intValue;
			}

			int RandomInt = Random.Range(0, TotalValue);


			TotalValue = 0;
			for (int i = 0; i < Count; i++)
			{
				TotalValue += PaintButtons[i].Chance.intValue;

				if (RandomInt < TotalValue)
					return i;
			}
			return Count - 1;


			//return Random.Range(0, PaintPropObjects.Count);
		}

		void RegisterUndo()
		{
			if (UndoRegistered)
				return;
			UndoRegistered = true;
			Undo.Current.RegisterPropsChange();
		}

		void Paint(Vector3 AtPosition, Quaternion Rotation)
		{
			RegisterUndo();

			AtPosition.y = ScmapEditor.Current.Teren.SampleHeight(AtPosition);

			Prop NewProp = new Prop();
			NewProp.GroupId = RandomPropGroup;
			NewProp.CreateObject(AtPosition, Rotation, Vector3.one * RandomScale);

			AllPropsTypes[RandomPropGroup].PropsInstances.Add(NewProp);

			TotalMassCount += AllPropsTypes[RandomPropGroup].PropObject.BP.ReclaimMassMax;
			TotalEnergyCount += AllPropsTypes[RandomPropGroup].PropObject.BP.ReclaimEnergyMax;
			TotalReclaimTime += AllPropsTypes[RandomPropGroup].PropObject.BP.ReclaimTime;

		}



		PropGameObject SearchClosestProp(Vector3 Pos, float tolerance) //, out int ClosestP
		{
			int GroupsCount = AllPropsTypes.Count;
			int g = 0;
			//int p = 0;
			float dist = 0;

			//int ClosestG = -1;
			//ClosestP = -1;
			float ClosestDist = 1000000f;
			PropGameObject ToReturn = null;

			for (g = 0; g < AllPropsTypes.Count; g++)
			{
				foreach (Prop PropInstance in AllPropsTypes[g].PropsInstances)
				{
					dist = Vector3.Distance(Pos, PropInstance.Obj.Tr.localPosition);
					if (dist < ClosestDist && dist < tolerance)
					{
						//ClosestG = g;
						ToReturn = PropInstance.Obj;
						//ClosestP = p;
						ClosestDist = dist;
					}
				}
			}
			return ToReturn;
		}

		#endregion

		#region Import/Export
		const string ExportPathKey = "PropsSetExport";
		static string DefaultPath
		{
			get
			{
				return EnvPaths.GetLastPath(ExportPathKey, EnvPaths.GetMapsPath() + MapLuaParser.Current.FolderName);
			}
		}

		[System.Serializable]
		public class PaintButtonsSet{

			public PaintProp[] PaintProps;

			[System.Serializable]
			public class PaintProp
			{
				public string Blueprint;
				public float ScaleMin;
				public float ScaleMax;
				public int RotationMin;
				public int RotationMax;
				public int Chance;
			}
		}

		public void ImportPropsSet()
		{
			var extensions = new[]
			{
				new ExtensionFilter("Props paint set", "proppaintset")
			};

			var paths = StandaloneFileBrowser.OpenFilePanel("Import props paint set", DefaultPath, extensions, false);


			if (paths.Length <= 0 || string.IsNullOrEmpty(paths[0]))
				return;

			string data = File.ReadAllText(paths[0]);

			PaintButtonsSet PaintSet = JsonUtility.FromJson<PaintButtonsSet>(data);

			bool[] Exist = new bool[PaintPropObjects.Count];

			for(int i = 0; i < PaintSet.PaintProps.Length; i++)
			{
				bool Found = false;
				int o = 0;
				for(o = 0; i < PaintPropObjects.Count; o++)
				{
					if(PaintPropObjects[i].BP.Path == PaintSet.PaintProps[i].Blueprint)
					{
						if (o < Exist.Length)
							Exist[o] = true;
						Found = true;
						break;
					}
				}

				if (!Found)
				{
					// Load
					if (!LoadProp(GetGamedataFile.LoadProp(PaintSet.PaintProps[i].Blueprint)))
					{
						Debug.LogWarning("Can't load prop at path: " + PaintSet.PaintProps[i].Blueprint);
						continue;
					}

					o = PaintButtons.Count - 1;

				}



				PaintButtons[o].ScaleMin.SetValue(PaintSet.PaintProps[i].ScaleMin);
				PaintButtons[o].ScaleMax.SetValue(PaintSet.PaintProps[i].ScaleMax);

				PaintButtons[o].RotationMin.SetValue(PaintSet.PaintProps[i].RotationMin);
				PaintButtons[o].RotationMax.SetValue(PaintSet.PaintProps[i].RotationMax);

				PaintButtons[o].Chance.SetValue(PaintSet.PaintProps[i].Chance);
			}

			for(int i = Exist.Length - 1; i >= 0; i--)
			{
				if (!Exist[i])
				{
					RemoveProp(i);
				}
			}

			EnvPaths.SetLastPath(ExportPathKey, System.IO.Path.GetDirectoryName(paths[0]));
		}

		public void ExportPropsSet()
		{
			var extensions = new[]
			{
				new ExtensionFilter("Props paint set", "proppaintset")
			};

			var path = StandaloneFileBrowser.SaveFilePanel("Export props paint set", DefaultPath, "", extensions);

			if (string.IsNullOrEmpty(path))
				return;

			PaintButtonsSet PaintSet = new PaintButtonsSet();
			PaintSet.PaintProps = new PaintButtonsSet.PaintProp[PaintButtons.Count];
			
			for(int i = 0; i < PaintSet.PaintProps.Length; i++)
			{
				if (PaintPropObjects[i].BP == null)
					Debug.Log("Prop object is empty!");

				PaintSet.PaintProps[i] = new PaintButtonsSet.PaintProp();

				PaintSet.PaintProps[i].Blueprint = PaintPropObjects[i].BP.Path;
				PaintSet.PaintProps[i].ScaleMin = PaintButtons[RandomProp].ScaleMin.value;
				PaintSet.PaintProps[i].ScaleMax = PaintButtons[RandomProp].ScaleMin.value;
				PaintSet.PaintProps[i].RotationMin = PaintButtons[RandomProp].RotationMin.intValue;
				PaintSet.PaintProps[i].RotationMax = PaintButtons[RandomProp].RotationMax.intValue;
				PaintSet.PaintProps[i].Chance = PaintButtons[RandomProp].Chance.intValue;
			}



			string data = JsonUtility.ToJson(PaintSet);

			File.WriteAllText(path, data);
			EnvPaths.SetLastPath(ExportPathKey, System.IO.Path.GetDirectoryName(path));
		}
		#endregion

		public void RemoveAllProps()
		{
			UndoRegistered = false;
			RegisterUndo();



			int GroupsCount = AllPropsTypes.Count;
			int g = 0;
			
			for (g = 0; g < AllPropsTypes.Count; g++)
			{
				foreach (Prop PropInstance in AllPropsTypes[g].PropsInstances)
				{
					Destroy(PropInstance.Obj.gameObject);
				}
				AllPropsTypes[g].PropsInstances.Clear();
			}


			TotalMassCount = 0;
			TotalEnergyCount = 0;
			TotalReclaimTime = 0;

			UpdatePropStats();
			Painting = false;

			UndoRegistered = false;
			UpdateBrushPosition(true);
		}

	}
}