#define MAX_DISCO_LIGHTS 25

/**************************************************************************************/
/* Variables comunes */
/**************************************************************************************/

//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))

//Matrices de proyeccion del efecto mirrorball
float4x4 matViewProjMirrorBall[MAX_DISCO_LIGHTS];
float4 mirrorBallPosition;
float mirrorBallAttenuation = 0.5;
float mirrorBallIntensity = 5; 

//Textura que se proyectara
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

//parametros de luces
float4 eyePosition; //Posicion de la camara
float3 lightColor[4]; //Color RGB de las 4 luces
float4 lightPosition[4]; //Posicion de las 4 luces
float lightIntensity[4]; //Intensidad de las 4 luces
float lightAttenuation[4]; //Factor de atenuacion de las 4 luces
float4 spotLightDir[4]; //Direccion del cono de luz
float spotLightAngleCos[4]; //Angulo de apertura del cono de luz (en radianes)
float spotLightExponent[4]; //Exponente de atenuacion dentro del cono de luz



//Resultado de computo de lighting
struct LightingResult
{
    float3 ambientLight;
    float3 diffuseLight;
    float3 specularLight;
};

//Calcular colores para una luz
LightingResult calcularDiffuse(int i, float3 Nn, float3 viewVector, float3 worldPosition)
{
	float3 Ln = normalize(lightPosition[i].xyz - worldPosition);
	float3 Hn = normalize(viewVector + lightPosition[i].xyz - worldPosition);
	LightingResult res;
	
	//Calcular intensidad de luz, con atenuacion por distancia
	float distAtten = length(lightPosition[i].xyz - worldPosition) * lightAttenuation[i];
	float intensity = lightIntensity[i] / distAtten;

	//Ambient
	res.ambientLight = intensity * lightColor[i] * materialAmbientColor;
	
	//Diffuse (N dot L)
	float3 n_dot_l = dot(Nn, Ln);
	res.diffuseLight = intensity * lightColor[i] * materialDiffuseColor.rgb * max(0.0, n_dot_l);
	
	float3 n_dot_h = dot(Nn, Hn);
	res.specularLight = n_dot_l <= 0.0
			? float3(0.0, 0.0, 0.0)
			: (
				intensity * lightColor[i] * materialSpecularColor 
				* pow(max( 0.0, n_dot_h), materialSpecularExp)
			);
	
			
	return res;
}

//Calcular colores para una luz
LightingResult calcularSpot(int i, float3 Nn, float3 viewVector, float3 worldPosition)
{
	float3 Ln = normalize(lightPosition[i].xyz - worldPosition);
	float3 Hn = normalize(viewVector + lightPosition[i].xyz - worldPosition);
	LightingResult res;
	
	//Calcular intensidad de luz, con atenuacion por distancia
	float distAtten =length(lightPosition[i].xyz - worldPosition) * lightAttenuation[i];

	float spotAtten = dot(-spotLightDir[i].xyz, Ln);
		spotAtten = (spotAtten > spotLightAngleCos[i]) 
						? pow(spotAtten, spotLightExponent[i])
						: 0.0;

	float intensity = lightIntensity[i] * spotAtten / distAtten;

	//Ambient
	res.ambientLight = (dot(spotLightDir[i].xyz, Nn) <= 0.0?1:0) * intensity * lightColor[i] * materialAmbientColor;
	
	//Diffuse (N dot L)
	float3 n_dot_l = dot(Nn, Ln);
	res.diffuseLight = intensity * lightColor[i] * materialDiffuseColor.rgb * max(0.0, n_dot_l);
	
	float3 n_dot_h = dot(Nn, Hn);
	res.specularLight = n_dot_l <= 0.0
			? float3(0.0, 0.0, 0.0)
			: (
				intensity * lightColor[i] * materialSpecularColor 
				* pow(max( 0.0, n_dot_h), materialSpecularExp)
			);
			
	return res;
}

float4 calcularBola(float4 finalColor, float3 worldPosition, float3 worldNormal){

	float3 ln = normalize(mirrorBallPosition.xyz - worldPosition);
        
    float ballAngle = dot(ln, worldNormal);
        
    //Calculamos la atenuación de la proyección
    float distAttenMirrorBall = length(mirrorBallPosition.xyz - worldPosition) * mirrorBallAttenuation;
    float finalIntensity = (ballAngle <= 0.0?0:1) * (mirrorBallIntensity / distAttenMirrorBall); //Dividimos intensidad sobre distancia (lo hacemos lineal pero tambien podria ser i/d^2)
	
	for (float i = 0; i < MAX_DISCO_LIGHTS ; i++) {

		float4 finalProjection = mul(worldPosition, matViewProjMirrorBall[i]);
		
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
/* VERTEX_COLOR */
/**************************************************************************************/

//Input del Vertex Shader
struct VS_INPUT_VERTEX_COLOR 
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL0;
	float4 Color : COLOR;
	float2 Texcoord : TEXCOORD0;
};

//Output del Vertex Shader
struct VS_OUTPUT_VERTEX_COLOR
{
	float4 Position : POSITION0;
	float2 Texcoord : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
	float3 WorldNormal : TEXCOORD2;	
	float4 Color : COLOR;
	
};


//Vertex Shader
VS_OUTPUT_VERTEX_COLOR vs_VertexColor(VS_INPUT_VERTEX_COLOR input)
{
	VS_OUTPUT_VERTEX_COLOR output;

	//Proyectar posicion
	output.Position = mul(input.Position, matWorldViewProj);

	//Enviar color directamente
	output.Color = input.Color;

	//Enviar Texcoord directamente
	output.Texcoord = input.Texcoord;

	//Posicion pasada a World-Space (necesaria para atenuacior distancia)
	output.WorldPosition = mul(input.Position, matWorld);

	/* Pasar normal a World-Space 
	Solo queremos rotarla, no trasladarla ni escalarla.
	Por eso usamos matInverseTransposeWorld en vez de matWorld */
	output.WorldNormal = mul(input.Normal, matInverseTransposeWorld).xyz;

	
	
	return output;
}

//Input del Pixel Shader
struct PS_INPUT_VERTEX_COLOR 
{
	float4 Color : COLOR0; 
	float2 Texcoord : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
	float3 WorldNormal : TEXCOORD2;
	
};

//Pixel Shader
float4 ps_3SpotYEspejos(PS_INPUT_VERTEX_COLOR input) : COLOR0
{      
	
	float3 Nn = normalize(input.WorldNormal);
	float3 viewVector = eyePosition.xyz - input.WorldPosition;
	LightingResult res0 =  calcularSpot(0, Nn, viewVector, input.WorldPosition);
	LightingResult res1 =  calcularSpot(1, Nn, viewVector, input.WorldPosition);
	LightingResult res2 =  calcularSpot(2, Nn, viewVector, input.WorldPosition);
	
	//Obtener texel de la textura
	float4 texelColor = tex2D(diffuseMap, input.Texcoord);

	//Color final: modular (Emissive + Ambient + Diffuse) por el color de la textura, y luego sumar Specular.
	//El color Alpha sale del diffuse material
	float4 finalColor = float4(
		saturate(
			materialEmissiveColor + 
			(res0.ambientLight + res1.ambientLight + res2.ambientLight) + 
			(res0.diffuseLight + res1.diffuseLight + res2.diffuseLight))
		* texelColor  + 
		(res0.specularLight + res1.specularLight + res2.specularLight) , 
	materialDiffuseColor.a);
	

	//BOLA DE ESPEJOS
	finalColor = calcularBola(finalColor, input.WorldPosition, input.WorldNormal);
	

	return finalColor;
}

//Pixel Shader
float4 ps_2SpotDiffuseYEspejos(PS_INPUT_VERTEX_COLOR input) : COLOR0
{      
	
	float3 Nn = normalize(input.WorldNormal);
	float3 viewVector = eyePosition.xyz - input.WorldPosition;
	LightingResult res0 =  calcularSpot(0, Nn, viewVector, input.WorldPosition);
	LightingResult res1 =  calcularSpot(1, Nn, viewVector, input.WorldPosition);
	LightingResult res2 =  calcularDiffuse(2, Nn, viewVector, input.WorldPosition);
	
	//Obtener texel de la textura
	float4 texelColor = tex2D(diffuseMap, input.Texcoord);

	//Color final: modular (Emissive + Ambient + Diffuse) por el color de la textura, y luego sumar Specular.
	//El color Alpha sale del diffuse material
	float4 finalColor = float4(
		saturate(
			materialEmissiveColor + 
			(res0.ambientLight + res1.ambientLight + res2.ambientLight) + 
			(res0.diffuseLight + res1.diffuseLight + res2.diffuseLight))
		* texelColor  + 
		(res0.specularLight + res1.specularLight + res2.specularLight) , 
	materialDiffuseColor.a);
	

	//BOLA DE ESPEJOS
	finalColor = calcularBola(finalColor, input.WorldPosition, input.WorldNormal);

	return finalColor;
}

//Pixel Shader
float4 ps_Spot2DiffuseYEspejos(PS_INPUT_VERTEX_COLOR input) : COLOR0
{      
	
	float3 Nn = normalize(input.WorldNormal);
	float3 viewVector = eyePosition.xyz - input.WorldPosition;
	LightingResult res0 =  calcularSpot(0, Nn, viewVector, input.WorldPosition);
	LightingResult res1 =  calcularDiffuse(1, Nn, viewVector, input.WorldPosition);
	LightingResult res2 =  calcularDiffuse(2, Nn, viewVector, input.WorldPosition);
	
	//Obtener texel de la textura
	float4 texelColor = tex2D(diffuseMap, input.Texcoord);

	//Color final: modular (Emissive + Ambient + Diffuse) por el color de la textura, y luego sumar Specular.
	//El color Alpha sale del diffuse material
	float4 finalColor = float4(
		saturate(
			materialEmissiveColor + 
			(res0.ambientLight + res1.ambientLight + res2.ambientLight) + 
			(res0.diffuseLight + res1.diffuseLight + res2.diffuseLight))
		* texelColor  + 
		(res0.specularLight + res1.specularLight + res2.specularLight) , 
	materialDiffuseColor.a);
	

	//BOLA DE ESPEJOS
	finalColor = calcularBola(finalColor, input.WorldPosition, input.WorldNormal);
	
	return finalColor;
}

//Pixel Shader
float4 ps_3DiffuseYEspejos(PS_INPUT_VERTEX_COLOR input) : COLOR0
{      
	
	float3 Nn = normalize(input.WorldNormal);
	float3 viewVector = eyePosition.xyz - input.WorldPosition;
	LightingResult res0 =  calcularDiffuse(0, Nn, viewVector, input.WorldPosition);
	LightingResult res1 =  calcularDiffuse(1, Nn, viewVector, input.WorldPosition);
	LightingResult res2 =  calcularDiffuse(2, Nn, viewVector, input.WorldPosition);
	
	//Obtener texel de la textura
	float4 texelColor = tex2D(diffuseMap, input.Texcoord);

	//Color final: modular (Emissive + Ambient + Diffuse) por el color de la textura, y luego sumar Specular.
	//El color Alpha sale del diffuse material
	float4 finalColor = float4(
		saturate(
			materialEmissiveColor + 
			(res0.ambientLight + res1.ambientLight + res2.ambientLight) + 
			(res0.diffuseLight + res1.diffuseLight + res2.diffuseLight))
		* texelColor  + 
		(res0.specularLight + res1.specularLight + res2.specularLight) , 
	materialDiffuseColor.a);
	

	//BOLA DE ESPEJOS
	finalColor = calcularBola(finalColor, input.WorldPosition, input.WorldNormal);

	return finalColor;
}




Technique TRES_SPOT_Y_BOLA
{
   pass Pass_0
   {
	  VertexShader = compile vs_3_0 vs_VertexColor();
	  PixelShader = compile ps_3_0 ps_3SpotYEspejos();
   }
 }
 Technique DOS_SPOT_DIFFUSE_Y_BOLA
{
   pass Pass_0
   {
	  VertexShader = compile vs_3_0 vs_VertexColor();
	  PixelShader = compile ps_3_0 ps_2SpotDiffuseYEspejos();
   }
 }
 Technique SPOT_DOS_DIFFUSE_Y_BOLA
{
   pass Pass_0
   {
	  VertexShader = compile vs_3_0 vs_VertexColor();
	  PixelShader = compile ps_3_0 ps_Spot2DiffuseYEspejos();
   }
 }
 Technique TRES_DIFFUSE_Y_BOLA
{
   pass Pass_0
   {
	  VertexShader = compile vs_3_0 vs_VertexColor();
	  PixelShader = compile ps_3_0 ps_3DiffuseYEspejos();
   }
 }
  Technique TRES_DIFFUSE_Y_SPOT
{
   pass Pass_0
   {
	  VertexShader = compile vs_3_0 vs_VertexColor();
	  PixelShader = compile ps_3_0 ps_3DiffuseYEspejos();
   }
 }