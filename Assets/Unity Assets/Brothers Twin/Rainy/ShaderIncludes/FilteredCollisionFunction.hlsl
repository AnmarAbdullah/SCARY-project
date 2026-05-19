void FilteredCollision_float(UnityTexture2D CollisionMap, UnitySamplerState samplerState, float2 uv, float Filter, float WorldHeight, out float Result)
{
	float2 xuv = float2(1,0) * Filter;
	float2 yuv = float2(0,1) * Filter;

	Result = 0;

	for(int x = -2; x < 2; x++){
		for(int y = -2; y < 2; y++){
			float sample = SAMPLE_TEXTURE2D(CollisionMap, samplerState, uv + float2(x,y) * Filter).r;

			Result += WorldHeight > sample;
		}
	}

	Result /= 10;
}