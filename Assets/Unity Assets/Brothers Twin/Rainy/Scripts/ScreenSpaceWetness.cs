using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor.Rendering.HighDefinition;


namespace BrothersTwin
{
	[CustomPassDrawer(typeof(ScreenSpaceWetness))]
	class ScreenSpaceWetnessEditor : CustomPassDrawer
	{
		protected override PassUIFlag commonPassUIFlags => PassUIFlag.Name;
	}
}
#endif

namespace BrothersTwin
{
	class ScreenSpaceWetness : CustomPass
	{
		public Rainy rainy;

		RTHandle tmpNormalBuffer;
		MaterialPropertyBlock props;

		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			tmpNormalBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, useDynamicScale: true, name: "TMP Normal Buffer");
			props = new MaterialPropertyBlock();
		}

		protected override void Execute(CustomPassContext ctx)
		{
			if (injectionPoint != CustomPassInjectionPoint.AfterOpaqueDepthAndNormal)
			{
				Debug.LogError("Custom Pass ScreenSpaceWetness needs to be used at the injection point AfterOpaqueDepthAndNormal.");
				return;
			}

			if (rainy == null)
				return;

			if (rainy.wetnessMaterial == null)
				return;

			if (rainy.collisionCamera == null)
				return;

			if (rainy.collisionCamera.cam.targetTexture == null)
				return;

			Matrix4x4 V = rainy.collisionCamera.cam.worldToCameraMatrix;
			Matrix4x4 P = GL.GetGPUProjectionMatrix(rainy.collisionCamera.cam.projectionMatrix, true);
			
			props.SetMatrix("_CollisionCamMatrix", P * V);
			props.SetTexture("_CollisionTexture", rainy.collisionCamera.cam.targetTexture);
			props.SetFloat("_BlendOffset", rainy.SufaceMultiplier * rainy.GlobalMultiplier);

			CoreUtils.SetRenderTarget(ctx.cmd, tmpNormalBuffer, ctx.cameraDepthBuffer);
			CoreUtils.DrawFullScreen(ctx.cmd, rainy.wetnessMaterial, shaderPassId: 0, properties: props);
			CustomPassUtils.Copy(ctx, tmpNormalBuffer, ctx.cameraNormalBuffer);
		}

		protected override void Cleanup()
		{
			tmpNormalBuffer.Release();
		}
	}
}