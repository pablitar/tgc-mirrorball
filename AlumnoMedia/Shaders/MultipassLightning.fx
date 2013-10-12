//Light
float3 lightColor;
float4 lightPosition;
float lightAttenuation;
float lightIntensity;

//Material
float3 materialEmissiveColor = float3(0,0,0); //Color RGB
float3 materialAmbientColor; //Color RGB
float4 materialDiffuseColor; //Color ARGB (tiene canal Alpha)
float3 materialSpecularColor; //Color RGB
float materialSpecularExp; //Exponente de specular

//SpotLight
float3 spotLightDirection;
float spotLightAngle;
float spotLightExponent;

//Camera
float4 eyePosition;

//Material Texture
texture texDiffuseMap;
sampler2D textureSampled = sampler_state
{
	Texture = (texDiffuseMap);
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
	MINFILTER = ANISOTROPIC;
	MAGFILTER = ANISOTROPIC;
	MIPFILTER = LINEAR;
	MAXANISOTROPY = 16;
};

float4x4 matWorld; //Matriz de transformacion World
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))

struct VS_INPUT
{
   float4 Position : POSITION0;
   float3 Normal : NORMAL0;
   float4 Color : COLOR;
   float2 Texcoord : TEXCOORD0;
};

struct VS_OUTPUT
{
	float4 Position : POSITION0;
	float2 Texcoord : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
	float3 WorldNormal : TEXCOORD2;
	float3 Normal : NORMAL0;
};

struct LightParams 
{
	float3 N;
	float3 L;
	float3 V;
	float3 Ln;
	float3 Hn;
	float3 n_dot_l;
	float3 n_dot_h;
	float intensity;
};

VS_OUTPUT vs_Common(VS_INPUT input)
{
	VS_OUTPUT output;

	//Proyectar posicion
	output.Position = mul(input.Position, matWorldViewProj);

	//Enviar Texcoord directamente
	output.Texcoord = input.Texcoord;

	//Posicion pasada a World-Space (necesaria para atenuación por distancia)
	output.WorldPosition = input.Position.xyz;
	
	output.Normal = input.Normal;
	
	output.WorldNormal = mul(input.Normal, matInverseTransposeWorld).xyz;

	return output;
}

float4 ps_Ambient(VS_OUTPUT input) : COLOR0 
{
	return float4(saturate(materialAmbientColor + materialEmissiveColor),1)  * tex2D(textureSampled, input.Texcoord);
}

float3 computeSpecularComponent(LightParams params) 
{
	//Componente Specular (Phong Blinn): (N dot H)^exp
	return params.n_dot_l <= 0.0
			? float3(0.0, 0.0, 0.0)
			: (params.intensity * lightColor * materialSpecularColor * pow(max( 0.0, params.n_dot_h), materialSpecularExp));
}

//Funcion para calcular color RGB de Diffuse de un Pixel
float3 computeDiffuseComponent(LightParams params)
{	
	return params.intensity * lightColor * materialDiffuseColor.rgb * max(0.0, params.n_dot_l);
}

//Calcula los parámetros comunes para las luces
LightParams computeLightParams(float3 normal, float3 position)
{
	LightParams result;
	result.N = normalize(normal);
	result.L = normalize(lightPosition.xyz - position);
	result.Ln = normalize(result.L);
	result.V = eyePosition.xyz - position;
	result.Hn = normalize(result.V + result.L);
	result.n_dot_l = dot(result.N, result.Ln);
	result.n_dot_h = dot(result.N, result.Hn);
	result.intensity = lightIntensity / (length(result.L) * lightAttenuation);
	
	return result;
}

float4 texel(float3 light, float2 texCoord)
{
	float4 texelColor = tex2D(textureSampled, texCoord);
	texelColor.rgb *= light;

	return texelColor;
}

float4 ps_Point(VS_OUTPUT input) : COLOR0
{
	LightParams params = computeLightParams(input.Normal, input.WorldPosition);

	float3 diffuse = computeDiffuseComponent(params);
	float3 specular = computeSpecularComponent(params);
	
	return texel(diffuse + specular, input.Texcoord);
}


float4 ps_Spot(VS_OUTPUT input) : COLOR0
{

	LightParams params = computeLightParams(input.Normal, input.WorldPosition);
	
	float3 diffuse = computeDiffuseComponent(params);
	float3 specular = computeSpecularComponent(params);

	float spotAngle = acos(dot(normalize(-spotLightDirection), params.Ln));
	float spotScale = (spotLightAngle > spotAngle) 
					? pow(spotLightAngle - spotAngle, spotLightExponent)
					: 0.0;

    return texel((diffuse + specular) * spotScale, input.Texcoord);
}


technique MultiPassLight
{
    pass Pass_0
    {
        VertexShader = compile vs_3_0 vs_Common();
        PixelShader = compile ps_3_0 ps_Ambient();
    }
    pass Pass_1
    {
		AlphaBlendEnable=True;
		SrcBlend=One;
        DestBlend=One;	
		PixelShader = compile ps_3_0 ps_Point();
    }
    
	pass Pass_2
    {
		AlphaBlendEnable=True;
		SrcBlend=One;
        DestBlend=One;	
        PixelShader = compile ps_3_0 ps_Spot();
    }
	/*
	pass MirrorBall
	{
		VertexShader = compile vs_3_0 vs_MirrorBall();
		PixelShader = compile ps_3_0 ps_MirrorBall();
	}*/
}
