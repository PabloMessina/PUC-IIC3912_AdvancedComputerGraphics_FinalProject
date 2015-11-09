SamplerState BilinearTextureSampler
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};

struct vertexAttributes {
	float3 inPosition : POSITION;
};

struct fragmentAttributes {
	float4 position : SV_POSITION;
	float3 fragPosition : TEXCOORD0;
};

uniform float4x4 projectionMatrix;
uniform float4x4 viewMatrix;
uniform float4x4 modelMatrix;

uniform float3 cameraPosition;

uniform Texture2D topTex;
uniform Texture2D bottomTex;
uniform Texture2D leftTex;
uniform Texture2D rightTex;
uniform Texture2D frontTex;
uniform Texture2D backTex;

fragmentAttributes VShader(vertexAttributes input)
{
	fragmentAttributes output = (fragmentAttributes)0;
	float4 worldPosition = mul(float4(input.inPosition, 1), modelMatrix);
	output.fragPosition = worldPosition.xyz;
	output.position = mul(mul(worldPosition, viewMatrix), projectionMatrix);
	return output;
}

float4 FShader(fragmentAttributes input) : SV_Target
{
	float3 dir = normalize(input.fragPosition - cameraPosition);
	float rx = dir.x;
	float ry = dir.y;
	float rz = dir.z;
    float sc = 0;
    float tc = 0;
    float ma = 0;
    float u = 0;
    float v = 0;

	float _rx = abs(rx);
	float _ry = abs(ry);
	float _rz = abs(rz);


	if (_rx > _ry && _rx > _rz) {

		if (rx > 0) 
		{
			sc = -rz;
            tc = -ry;
            ma = _rx;
			u = (sc / ma + 1) / 2;
			v = (tc / ma + 1) / 2;
			float3 texColor = leftTex.Sample(BilinearTextureSampler, float2(u, v));
			return float4(texColor, 1.0);
		}
		else 
		{
			sc = rz;
            tc = -ry;
            ma = _rx;
			u = (sc / ma + 1) / 2;
			v = (tc / ma + 1) / 2;
			float3 texColor = rightTex.Sample(BilinearTextureSampler, float2(u, v));
			return float4(texColor, 1.0);
		}
	}
	else if (_ry > _rx && _ry > _rz) 
	{
		if (ry > 0)
        {
            sc = -rx;
            tc = rz;
            ma = _ry;
			u = (sc / ma + 1) / 2;
			v = (tc / ma + 1) / 2;
			float3 texColor = topTex.Sample(BilinearTextureSampler, float2(u, v));
			return float4(texColor, 1.0);
        }
        else
        {
            sc = -rx;
            tc = -rz;
            ma = _ry;
			u = (sc / ma + 1) / 2;
			v = (tc / ma + 1) / 2;
			float3 texColor = bottomTex.Sample(BilinearTextureSampler, float2(u, v));
			return float4(texColor, 1.0);
        }
	}
	else 
	{
		 if (rz > 0)
        {
            sc = rx;
            tc = -ry;
            ma = _rz;
			u = (sc / ma + 1) / 2;
			v = (tc / ma + 1) / 2;
			float3 texColor = frontTex.Sample(BilinearTextureSampler, float2(u, v));
			return float4(texColor, 1.0);
        }
        else
        {
            sc = -rx;
            tc = -ry;
            ma = _rz;
			u = (sc / ma + 1) / 2;
			v = (tc / ma + 1) / 2;
			float3 texColor = backTex.Sample(BilinearTextureSampler, float2(u, v));
			return float4(texColor, 1.0);
        }
	}

}


technique10 Render
{

	pass P0
	{
		SetGeometryShader(0);
		SetVertexShader(CompileShader(vs_4_0, VShader()));
		SetPixelShader(CompileShader(ps_4_0, FShader()));
	}
}
