using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace BrothersTwin
{
	[ExecuteInEditMode]
	public class CollisionCamera : MonoBehaviour
	{
		public Rainy rainy;
		public Camera cam;

		HDAdditionalCameraData additionalCameraData;
		CommandBuffer buffer;
		Shader rainyCollisionShader;
		Material collisionMaterial;

		bool initialized = false;

		private void Start()
		{
			RefreshCollision();
		}

		public static CollisionCamera Initialize(Rainy _rainy)
		{
			GameObject cameraGO = new GameObject("CollisionCam");

			cameraGO.hideFlags = HideFlags.HideAndDontSave;
			cameraGO.transform.SetParent(_rainy.transform, false);
			cameraGO.transform.localPosition = new Vector3(0, 32, 0);
			cameraGO.transform.localEulerAngles = new Vector3(90, 0, 0);

			CollisionCamera cc = cameraGO.AddComponent<CollisionCamera>();

			cc.rainy = _rainy;

			cc.cam = cc.gameObject.AddComponent<Camera>();
			cc.cam.enabled = false;
			cc.cam.orthographic = true;
			cc.cam.farClipPlane = 200f;
			cc.cam.nearClipPlane = 0.1f;
			cc.cam.targetTexture = new RenderTexture(cc.rainy.Resolution, cc.rainy.Resolution, 16, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);

			cc.additionalCameraData = cc.gameObject.AddComponent<HDAdditionalCameraData>();
			cc.additionalCameraData.customRender += cc.DepthCam_RenderDepth;

			cc.rainyCollisionShader = Shader.Find("Hidden/Rainy/Collision");
			cc.collisionMaterial = new Material(cc.rainyCollisionShader);

			cc.RefreshCollision();
			cc.initialized = true;

			return cc;
		}

		private void OnEnable()
		{
			if (initialized)
			{
				additionalCameraData.customRender += DepthCam_RenderDepth;
				RefreshCollision();
			}
		}

		private void OnDisable()
		{
			additionalCameraData.customRender -= DepthCam_RenderDepth;
		}

		private void OnDestroy()
		{
			additionalCameraData.customRender -= DepthCam_RenderDepth;
			cam.targetTexture.Release();
		}

		private void Update()
		{
			cam.orthographicSize = rainy.Size;

			if (!rainyCollisionShader)
				rainyCollisionShader = Shader.Find("Hidden/Rainy/Collision");
			if (!collisionMaterial)
				collisionMaterial = new Material(rainyCollisionShader);
			if (collisionMaterial.shader != rainyCollisionShader)
				collisionMaterial.shader = rainyCollisionShader;

			if (cam.targetTexture == null || cam.targetTexture.width != rainy.Resolution)
			{
				if (cam.targetTexture != null)
				{
					cam.targetTexture.Release();
				}
				cam.targetTexture = new RenderTexture(rainy.Resolution, rainy.Resolution, 16, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
			}
		}

		public void RefreshCollision()
		{
			cam.Render();
		}

		private void DepthCam_RenderDepth(ScriptableRenderContext renderContext, HDCamera hdCamera)
		{
			//Create Buffer if null
			if (buffer == null)
				buffer = new CommandBuffer()
				{
					name = "Rain Camera"
				};

			//Setup Camera Properties for Projection Matrices
			renderContext.SetupCameraProperties(hdCamera.camera);

			//Clear RenderTarget befor Render
			buffer.ClearRenderTarget(true, true, new Color(-200, -200, -200, -200));

			//Sort
			SortingSettings sortingSettings = new SortingSettings(hdCamera.camera)
			{
				criteria = SortingCriteria.None
			};

			// DrawSettings
			DrawingSettings drawingSettings = new DrawingSettings(HDShaderPassNames.s_ForwardName, sortingSettings);

			// Override Material to render just Depth
			if (collisionMaterial)
			{
				drawingSettings.overrideMaterial = collisionMaterial;
				drawingSettings.overrideMaterialPassIndex = collisionMaterial.FindPass("Collision");
			}

			// Filter
			FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all, rainy.CollisionLayers);

			// Culling
			hdCamera.camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters);

			CullingResults cullingResults = renderContext.Cull(ref cullingParameters);

			RendererListParams rendererListParams = new RendererListParams(cullingResults, drawingSettings, filteringSettings);
			RendererList rendererList = renderContext.CreateRendererList(ref rendererListParams);

			buffer.DrawRendererList(rendererList);

			//Render
			renderContext.ExecuteCommandBuffer(buffer);

			//Clear all Buffer Commands
			buffer.Clear();
		}
	}
}