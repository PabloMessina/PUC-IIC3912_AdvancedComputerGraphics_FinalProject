struct vertexAttributes {
	float3 inPosition : POSITION;
	float3 inNormal : NORMAL;
	float3 inTextureCoords: TEXCOORD0;
};

struct fragmentAttributes {
	float4 position : SV_POSITION;
	float3 fragPosition : TEXCOORD0;
	float3 fragNormal : NORMAL;
	float3 fragTextureCoords : TEXCOORD1;
};

uniform float4x4 projectionMatrix;
uniform float4x4 viewMatrix;
uniform float4x4 modelMatrix;

uniform float3 color;

fragmentAttributes VShader(vertexAttributes input)
{
	fragmentAttributes output = (fragmentAttributes)0;
	float4 worldPosition = mul(float4(input.inPosition, 1), modelMatrix);
		float4 worldNormal = mul(float4(input.inNormal, 0), modelMatrix);
		output.fragPosition = worldPosition.xyz;
	output.fragNormal = worldNormal.xyz;
	output.fragTextureCoords = input.inTextureCoords;
	output.position = mul(mul(worldPosition, viewMatrix), projectionMatrix);
	return output;
}

float4 FShader(fragmentAttributes input) : SV_Target
{
	return float4(color, 1.0);
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