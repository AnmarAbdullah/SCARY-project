using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.HighDefinition;

#if UNITY_EDITOR
using UnityEditor.Rendering.HighDefinition;


namespace BrothersTwin
{
	[CustomPassDrawer(typeof(ForwardWetness))]
	class ForwardWetnessEditor : CustomPassDrawer
	{
		protected override PassUIFlag commonPassUIFlags => PassUIFlag.Name;
	}
}
#endif

namespace BrothersTwin
{
	class ForwardWetness : CustomPass
	{
		public Rainy rainy;

		//Sort
		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
		}

		protected override void Execute(CustomPassContext ctx)
		{
			if (injectionPoint != CustomPassInjectionPoint.BeforePostProcess)
			{
				Debug.LogError("Custom Pass ForwardWetness needs to be used at the injection point BeforePostProcess.");
				return;
			}

			if (rainy == null)
				return;

			if (rainy.transWetnessMaterial == null)
				return;

			if (rainy.collisionCamera == null)
				return;

			if (rainy.collisionCamera.cam.targetTexture == null)
				return;


			//SetupMaterial
			Matrix4x4 V = rainy.collisionCamera.cam.worldToCameraMatrix;
			Matrix4x4 P = GL.GetGPUProjectionMatrix(rainy.collisionCamera.cam.projectionMatrix, true);

			rainy.transWetnessMaterial.SetMatrix("_CollisionCamMatrix", P * V);
			rainy.transWetnessMaterial.SetTexture("_CollisionTexture", rainy.collisionCamera.cam.targetTexture);
			rainy.transWetnessMaterial.SetFloat("_BlendOffset", rainy.SufaceMultiplier * rainy.GlobalMultiplier);

			RenderStateMask mask = RenderStateMask.Nothing;

			var stateBlock = new RenderStateBlock(mask)
			{
				depthState = new DepthState(false),
			};


			PerObjectData renderConfig = ctx.hdCamera.frameSettings.IsEnabled(FrameSettingsField.Shadowmask) ? HDUtils.GetBakedLightingWithShadowMaskRenderConfig() : HDUtils.GetBakedLightingRenderConfig();

			RendererListDesc result = new RendererListDesc(HDShaderPassNames.s_ForwardName, ctx.cullingResults, ctx.hdCamera.camera)
			{
				rendererConfiguration = renderConfig,
				renderQueueRange = GetRenderQueueRange(rainy.renderQueue),
				sortingCriteria = SortingCriteria.CommonTransparent,
				excludeObjectMotionVectors = false,
				overrideMaterial = rainy.transWetnessMaterial,
				overrideMaterialPassIndex = rainy.transWetnessMaterial.FindPass("Forward"),
				stateBlock = stateBlock,
				layerMask = rainy.AffetedByRain,
			};

			//Render
			CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, ctx.renderContext.CreateRendererList(result));
		}

		protected override void Cleanup()
		{
		}
	}
}