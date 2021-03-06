#define MAX_DISCO_LIGHTS 9

/**************************************************************************************/
/* Variables comunes */
/**************************************************************************************/

//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))

//Matrices de proyecci�n del efecto mirrorball
float4x4 matViewProjMirrorBall[MAX_DISCO_LIGHTS];
float4 mirrorBallPosition;
float mirrorBallAttenuation = 0.5;
float mirrorBallIntensity = 5; 

//Textura que se proyectar�
texture mirrorBallTexture;

static const float PI_HALF = 1.5707963268f;

sampler2D mirrorBallTextureSampled = sampler_state
{
   Texture = (mirrorBallTexture);
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};

//Textura para DiffuseMap
texture texDiffuseMap;
sampler2D diffuseMap = sampler_state
{
	Texture = (texDiffuseMap);
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};

//Textura para Lightmap
texture texLightMap;
sampler2D lightMap = sampler_state
{
   Texture = (texLightMap);
};

//Material del mesh
float3 materialEmissiveColor; //Color RGB
float3 materialAmbientColor; //Color RGB
float4 materialDiffuseColor; //Color ARGB (tiene canal Alpha)
float3 materialSpecularColor; //Color RGB
float materialSpecularExp; //Exponente de specular

//Parametros de la Luz
float3 lightColor; //Color RGB de la luz
float4 lightPosition; //Posicion de la luz
float4 eyePosition; //Posicion de la camara
float lightIntensity; //Intensidad de la luz
float lightAttenuation; //Factor de atenuacion de la luz

//parametros del SpotLight
float3 spotLightColor; //Color RGB de la luz
float4 spotLightPosition; //Posicion de la luz
float spotLightIntensity; //Intensidad de la luz
float spotLightAttenuation; //Factor de atenuacion de la luz
float3 spotLightDir; //Direccion del cono de luz
float spotLightAngleCos; //Angulo de apertura del cono de luz (en radianes)
float spotLightExponent; //Exponente de atenuacion dentro del cono de luz


/**************************************************************************************/
/* DIFFUSE_MAP */
/**************************************************************************************/

//Input del Vertex Shader
struct VS_INPUT_DIFFUSE_MAP
{
   float4 Position : POSITION0;
   float3 Normal : NORMAL0;
   float4 Color : COLOR;
   float2 Texcoord : TEXCOORD0;
};

//Output del Vertex Shader
struct VS_OUTPUT_DIFFUSE_MAP
{
	float4 Position : POSITION0;
	float2 Texcoord : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
	float3 WorldNormal : TEXCOORD2;
	float3 LightVec	: TEXCOORD3;
	float3 HalfAngleVec	: TEXCOORD4;
	float4 projectedVector : TEXCOORD5;
};


//Vertex Shader
VS_OUTPUT_DIFFUSE_MAP vs_DiffuseMap(VS_INPUT_DIFFUSE_MAP input)
{ 
	VS_OUTPUT_DIFFUSE_MAP output;

	//Proyectar posicion
	output.Position = mul(input.Position, matWorldViewProj);

	//Enviar Texcoord directamente
	output.Texcoord = input.Texcoord;

	//Posicion pasada a World-Space (necesaria para atenuaci�n por distancia)
	output.WorldPosition = mul(input.Position, matWorld);

	/* Pasar normal a World-Space 
	Solo queremos rotarla, no trasladarla ni escalarla.
	Por eso usamos matInverseTransposeWorld en vez de matWorld */
	output.WorldNormal = mul(input.Normal, matInverseTransposeWorld).xyz;

	//LightVec (L): vector que va desde el vertice hacia la luz. Usado en Diffuse y Specular
	output.LightVec = lightPosition.xyz - output.WorldPosition;

	//ViewVec (V): vector que va desde el vertice hacia la camara.
	float3 viewVector = eyePosition.xyz - output.WorldPosition;

	//HalfAngleVec (H): vector de reflexion simplificado de Phong-Blinn (H = |V + L|). Usado en Specular
	output.HalfAngleVec = viewVector + output.LightVec;
	
	//Ubicaci�n del v�rtice en funci�n de la fuente de la textura proyectada
	output.projectedVector = mul(input.Position, matWorld);


	return output;
}


//Input del Pixel Shader
struct PS_DIFFUSE_MAP
{
	float4 Position : POSITION0;
	float2 Texcoord : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
	float3 WorldNormal : TEXCOORD2;
	float3 LightVec	: TEXCOORD3;
	float3 HalfAngleVec	: TEXCOORD4;
	float4 projectedVector : TEXCOORD5;
};

//Pixel Shader
float4 ps_DiffuseMap(PS_DIFFUSE_MAP input) : COLOR0
{
	//Normalizar vectores
	float3 Nn = normalize(input.WorldNormal);
	float3 Ln = normalize(input.LightVec);
	float3 Hn = normalize(input.HalfAngleVec);
	
	//Calcular intensidad de luz, con atenuacion por distancia
	float distAtten = length(lightPosition.xyz - input.WorldPosition) * lightAttenuation;
	float intensity = lightIntensity / distAtten; //Dividimos intensidad sobre distancia (lo hacemos lineal pero tambien podria ser i/d^2)
	
	//Obtener texel de la textura
	float4 texelColor = tex2D(diffuseMap, input.Texcoord);
	
	//Componente Ambient
	float3 ambientLight = intensity * lightColor * materialAmbientColor;
	
	//Componente Diffuse: N dot L
	float3 n_dot_l = dot(Nn, Ln);
	float3 diffuseLight = intensity * lightColor * materialDiffuseColor.rgb * max(0.0, n_dot_l); //Controlamos que no de negativo
	
	//Componente Specular: (N dot H)^exp
	float3 n_dot_h = dot(Nn, Hn);
	float3 specularLight = n_dot_l <= 0.0
			? float3(0.0, 0.0, 0.0)
			: (intensity * lightColor * materialSpecularColor * pow(max( 0.0, n_dot_h), materialSpecularExp));
	
	/* Color final: modular (Emissive + Ambient + Diffuse) por el color de la textura, y luego sumar Specular.
	   El color Alpha sale del diffuse material */
	float4 finalColor = float4(saturate(materialEmissiveColor + ambientLight + diffuseLight) * texelColor + specularLight, materialDiffuseColor.a);
	
	float3 ballVector = mirrorBallPosition.xyz - input.WorldPosition;
	
	float ballAngle = acos(dot(normalize(ballVector), input.WorldNormal));
	
	//Calculamos la atenuaci�n de la proyecci�n
	float distAttenMirrorBall = length(ballVector) * mirrorBallAttenuation;
	float finalIntensity = (ballAngle <= PI_HALF?1:0) * (mirrorBallIntensity / distAttenMirrorBall); //Dividimos intensidad sobre distancia (lo hacemos lineal pero tambien podria ser i/d^2)
	
	for (float i = 0; i < MAX_DISCO_LIGHTS ; i++) {

		float4 finalProjection = mul(input.projectedVector, matViewProjMirrorBall[i]);
		
		float2 projectTexCoord;

		projectTexCoord.x =  finalProjection.x / finalProjection.w / 2.0f + 0.5f;
		projectTexCoord.y = -finalProjection.y / finalProjection.w / 2.0f + 0.5f;
		


		// Determine if the projected coordinates are in the 0 to 1 range.  If it is then this pixel is inside the projected view port.
		if((saturate(projectTexCoord.x) == projectTexCoord.x) && (saturate(projectTexCoord.y) == projectTexCoord.y))
		{
			// Sample the color value from the projection texture using the sampler at the projected texture coordinate location.
			float4 projectionColor = tex2D(mirrorBallTextureSampled, projectTexCoord);

			// Set the output color of this pixel to the projection texture overriding the regular color value.
			finalColor = finalColor + (projectionColor * projectionColor.a * finalIntensity); 
		}
	}
	

	return finalColor;
}



/**************************************************************************************/
/* SPOTLIGHT */
/**************************************************************************************/

//Input del Vertex Shader
struct VS_INPUT_SL_DIFFUSE_MAP
{
   float4 Position : POSITION0;
   float3 Normal : NORMAL0;
   float4 Color : COLOR;
   float2 Texcoord : TEXCOORD0;
};

//Output del Vertex Shader
struct VS_OUTPUT_SL_DIFFUSE_MAP
{
	float4 Position : POSITION0;
	float2 Texcoord : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
	float3 WorldNormal : TEXCOORD2;
	float3 SpotLightVec	: TEXCOORD3;
	float3 SpotHalfAngleVec	: TEXCOORD4;
};


//Vertex Shader
VS_OUTPUT_SL_DIFFUSE_MAP vs_SpotLightDiffuseMap(VS_INPUT_SL_DIFFUSE_MAP input)
{ 
	VS_OUTPUT_SL_DIFFUSE_MAP output;

	//Proyectar posicion
	output.Position = mul(input.Position, matWorldViewProj);

	//Enviar Texcoord directamente
	output.Texcoord = input.Texcoord;

	//Posicion pasada a World-Space (necesaria para atenuaci�n por distancia)
	output.WorldPosition = mul(input.Position, matWorld);

	/* Pasar normal a World-Space 
	Solo queremos rotarla, no trasladarla ni escalarla.
	Por eso usamos matInverseTransposeWorld en vez de matWorld */
	output.WorldNormal = mul(input.Normal, matInverseTransposeWorld).xyz;

	//LightVec (L): vector que va desde el vertice hacia la luz. Usado en Diffuse y Specular
	output.SpotLightVec = spotLightPosition.xyz - output.WorldPosition;

	//ViewVec (V): vector que va desde el vertice hacia la camara.
	float3 viewVector = eyePosition.xyz - output.WorldPosition;

	//HalfAngleVec (H): vector de reflexion simplificado de Phong-Blinn (H = |V + L|). Usado en Specular
	output.SpotHalfAngleVec = viewVector + output.SpotLightVec;

	return output;
}


//Input del Pixel Shader
struct PS_SL_DIFFUSE_MAP
{
	float2 Texcoord : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
	float3 WorldNormal : TEXCOORD2;
	float3 SpotLightVec	: TEXCOORD3;
	float3 SpotHalfAngleVec	: TEXCOORD4;
};

//Pixel Shader
float4 ps_SpotLightDiffuseMap(PS_SL_DIFFUSE_MAP input) : COLOR0
{
	//Normalizar vectores
	float3 Nn = normalize(input.WorldNormal);
	float3 Ln = normalize(input.SpotLightVec);
	float3 Hn = normalize(input.SpotHalfAngleVec);
	
	//Calcular atenuacion por distancia
	float distAtten = length(spotLightPosition.xyz - input.WorldPosition) * spotLightAttenuation;
	
	//Calcular atenuacion por Spot Light. Si esta fuera del angulo del cono tiene 0 intensidad.
	float spotAtten = dot(-spotLightDir, Ln);
	spotAtten = (spotAtten > spotLightAngleCos) 
					? pow(spotAtten, spotLightExponent)
					: 0.0;
	
	//Calcular intensidad de la luz segun la atenuacion por distancia y si esta adentro o fuera del cono de luz
	float intensity = spotLightIntensity * spotAtten / distAtten;
	
	//Obtener texel de la textura
	float4 texelColor = tex2D(diffuseMap, input.Texcoord);
	
	//Componente Ambient
	float3 ambientLight = intensity * spotLightColor * materialAmbientColor;
	
	//Componente Diffuse: N dot L
	float3 n_dot_l = dot(Nn, Ln);
	float3 diffuseLight = intensity * spotLightColor * materialDiffuseColor.rgb * max(0.0, n_dot_l); //Controlamos que no de negativo
	
	//Componente Specular: (N dot H)^exp
	float3 n_dot_h = dot(Nn, Hn);
	float3 specularLight = n_dot_l <= 0.0
			? float3(0.0, 0.0, 0.0)
			: (intensity * spotLightColor * materialSpecularColor * pow(max( 0.0, n_dot_h), materialSpecularExp));
	
	/* Color final: modular (Emissive + Ambient + Diffuse) por el color de la textura, y luego sumar Specular.
	   El color Alpha sale del diffuse material */
	float4 finalColor = float4(saturate(materialEmissiveColor + ambientLight + diffuseLight) * texelColor + specularLight, materialDiffuseColor.a);
	
	
	return finalColor;
}




/*
* Technique MIRROR_BALL_MAP
*/
technique MIRROR_BALL_MAP
{
   pass Pass_0
   {
	  VertexShader = compile vs_3_0 vs_DiffuseMap();
	  PixelShader = compile ps_3_0 ps_DiffuseMap();
   }
   pass Pass_1
   {
		AlphaBlendEnable=True;
		SrcBlend=One;
        DestBlend=One;	
     VertexShader = compile vs_3_0 vs_SpotLightDiffuseMap();
	 PixelShader = compile ps_3_0 ps_SpotLightDiffuseMap();
   }
}
