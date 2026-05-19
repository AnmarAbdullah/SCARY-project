using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering.HighDefinition;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
namespace BrothersTwin
{
	[CustomEditor(typeof(Rainy))]
	class RainyEditor : Editor
	{
		Rainy rainy;
		MaterialEditor wetnessMaterialEditor;
		MaterialEditor transWetnessMaterialEditor;

		public bool ShowEffectSettings = false;
		public bool ShowRenderingSettings = false;
		public bool ShowVFXSettings = false;
		public bool ShowMaterialSettings = false;

		SerializedProperty Size;
		SerializedProperty GlobalMultiplier;
		SerializedProperty SufaceMultiplier;
		SerializedProperty ParticlesMultiplier;
		
		SerializedProperty Resolution;
		SerializedProperty CollisionLayers;
		SerializedProperty updateMode;
		SerializedProperty SnapToCamera;
		SerializedProperty SnapToObject;
		SerializedProperty SnapEveryNPixel;

		SerializedProperty AffetedByRain;
		SerializedProperty renderQueue;
		
		SerializedProperty HeightBias;
		SerializedProperty rainVFX;

		SerializedProperty wetnessMaterial;
		SerializedProperty transWetnessMaterial;

		private void OnEnable()
		{
			Size = serializedObject.FindProperty("Size");
			GlobalMultiplier = serializedObject.FindProperty("GlobalMultiplier");
			SufaceMultiplier = serializedObject.FindProperty("SufaceMultiplier");
			ParticlesMultiplier = serializedObject.FindProperty("ParticlesMultiplier");

			Resolution = serializedObject.FindProperty("Resolution");
			CollisionLayers = serializedObject.FindProperty("CollisionLayers");
			updateMode = serializedObject.FindProperty("updateMode");
			SnapToCamera = serializedObject.FindProperty("SnapToCamera");
			SnapToObject = serializedObject.FindProperty("SnapToObject");
			SnapEveryNPixel = serializedObject.FindProperty("SnapEveryNPixel");

			AffetedByRain = serializedObject.FindProperty("AffetedByRain");
			renderQueue = serializedObject.FindProperty("renderQueue");

			HeightBias = serializedObject.FindProperty("HeightBias");
			rainVFX = serializedObject.FindProperty("rainVFX");

			wetnessMaterial = serializedObject.FindProperty("wetnessMaterial");
			transWetnessMaterial = serializedObject.FindProperty("transWetnessMaterial");
		}

		public override void OnInspectorGUI()
		{
			rainy = (Rainy)target;

			EditorGUI.BeginChangeCheck();

			GUILayout.BeginVertical("Box");
			ShowEffectSettings = EditorGUILayout.BeginFoldoutHeaderGroup(ShowEffectSettings, "Effect");
			if (ShowEffectSettings)
			{
				serializedObject.Update();
				EditorGUILayout.PropertyField(Size);
				EditorGUILayout.PropertyField(GlobalMultiplier);
				EditorGUILayout.PropertyField(SufaceMultiplier);
				EditorGUILayout.PropertyField(ParticlesMultiplier);
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
			GUILayout.EndVertical();

			GUILayout.BeginVertical("Box");
			ShowRenderingSettings = EditorGUILayout.BeginFoldoutHeaderGroup(ShowRenderingSettings, "Rendering");
			if (ShowRenderingSettings)
			{
				EditorGUILayout.PropertyField(Resolution);
				EditorGUILayout.PropertyField(CollisionLayers);
				EditorGUILayout.PropertyField(updateMode);

				if (rainy.updateMode == Rainy.UpdateMode.OnMove)
				{
					EditorGUILayout.PropertyField(SnapToCamera);

					if (!rainy.SnapToCamera)
					{
						EditorGUILayout.PropertyField(SnapToObject);
						EditorGUILayout.HelpBox("If set, Effect snaps to Transform.\nIf not set, Effect could be positioned via script with (rainy.target)", MessageType.Info);
					}
					else
					{
						EditorGUILayout.HelpBox("Effect automaticaly snaps to Camera", MessageType.Info);
					}
					EditorGUILayout.PropertyField(SnapEveryNPixel);
				}
				if (rainy.updateMode == Rainy.UpdateMode.OnDemand)
				{
					EditorGUILayout.HelpBox("Effect could be freely moved and collision refresh must be manual triggered with ( rainy.RefreshCollision() )", MessageType.Info);
					if (GUILayout.Button("Refresh Collision"))
					{
						rainy.RefreshCollision();
					}
				}
				EditorGUILayout.PropertyField(AffetedByRain);
				EditorGUILayout.PropertyField(renderQueue);
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
			GUILayout.EndVertical();

			GUILayout.BeginVertical("Box");
			ShowVFXSettings = EditorGUILayout.BeginFoldoutHeaderGroup(ShowVFXSettings, "VFX");
			if (ShowVFXSettings)
			{
				EditorGUILayout.PropertyField(HeightBias);
				EditorGUILayout.PropertyField(rainVFX);
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
			GUILayout.EndVertical();

			GUILayout.BeginVertical("Box");
			ShowMaterialSettings = EditorGUILayout.BeginFoldoutHeaderGroup(ShowMaterialSettings, "Material");
			if (ShowMaterialSettings)
			{
				EditorGUILayout.PropertyField(wetnessMaterial);
				EditorGUILayout.PropertyField(transWetnessMaterial);


				if (rainy.wetnessMaterial != null)
				{
					if (wetnessMaterialEditor == null)
						wetnessMaterialEditor = (MaterialEditor)CreateEditor(rainy.wetnessMaterial);
					GUILayout.Space(10);
					wetnessMaterialEditor.DrawHeader();
					wetnessMaterialEditor.OnInspectorGUI();
				}

				if (rainy.transWetnessMaterial != null)
				{
					if (transWetnessMaterialEditor == null)
						transWetnessMaterialEditor = (MaterialEditor)CreateEditor(rainy.transWetnessMaterial);
					GUILayout.Space(10);
					transWetnessMaterialEditor.DrawHeader();
					transWetnessMaterialEditor.OnInspectorGUI();
				}
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
			GUILayout.EndVertical();

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(target, "Changed Area Of Effect");
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}
#endif
namespace BrothersTwin
{
	[ExecuteInEditMode]
	public class Rainy : MonoBehaviour
	{
		public int Size;
		[Range(0,1)]
		public float GlobalMultiplier = 1f;
		[Range(0, 1)]
		public float SufaceMultiplier = 1f;
		[Range(0, 1)]
		public float ParticlesMultiplier = 1f;

		[Header("Collision")]
		public int Resolution;
		public LayerMask CollisionLayers;
		public UpdateMode updateMode;
		public bool SnapToCamera = true;
		public Transform SnapToObject;
		public int SnapEveryNPixel = 4;

		public CollisionCamera collisionCamera;
		public VisualEffect rainVFX;
		public float HeightBias = 0.2f;

		[Header("ForwardMaterial Only Settings")]
		public LayerMask AffetedByRain = 0;
		public CustomPass.RenderQueueType renderQueue = CustomPass.RenderQueueType.AllTransparent;

		public Material wetnessMaterial;
		public Material transWetnessMaterial;

		public Vector3 target = Vector3.zero;
		bool refresh = false;

		private bool DrawHiddenCam = false;

		private void Update()
		{
			if (updateMode == UpdateMode.OnMove)
			{
				if (SnapToCamera)
				{
					if (Application.isPlaying)
					{
						target = Camera.main.transform.position;
					}
#if UNITY_EDITOR
					else if (SceneView.lastActiveSceneView != null)
					{
						target = SceneView.lastActiveSceneView.camera.transform.position;
					}
#endif
				}
				else if (SnapToObject)
				{
					target = SnapToObject.position;
				}
				else
				{
					target = transform.position;
				}

				float step = (Resolution * 1f) / (2f * Size) / SnapEveryNPixel;
				Vector3 pos = (Vector3)Vector3Int.RoundToInt(target * step) / step;

				if (transform.position != pos)
				{
					transform.position = pos;
					refresh = true;
				}
			}



			if (!collisionCamera)
			{
				collisionCamera = CollisionCamera.Initialize(this);
			}

			if (!collisionCamera || !collisionCamera.cam || !collisionCamera.cam.targetTexture)
				return;

			if (DrawHiddenCam)
				collisionCamera.hideFlags = HideFlags.DontSave;
			else
				collisionCamera.hideFlags = HideFlags.HideAndDontSave;


			if (rainVFX)
			{
				rainVFX.SetFloat("_HeightBias", HeightBias);
				rainVFX.SetFloat("_Size", Size);
				rainVFX.SetFloat("_Rate", ParticlesMultiplier * GlobalMultiplier);
				rainVFX.SetTexture("_CollisionTexture", collisionCamera.cam.targetTexture);
			}

			if (updateMode == UpdateMode.EveryFrame)
			{
				RefreshCollision();
			}

			if (refresh && updateMode == UpdateMode.OnMove)
			{
				RefreshCollision();
				refresh = false;
			}
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(transform.position, new Vector3(Size, 200, Size));
		}

		public void RefreshCollision()
		{
			collisionCamera.RefreshCollision();
		}

		public enum UpdateMode
		{
			EveryFrame,
			OnMove,
			OnDemand,
		}
	}
}