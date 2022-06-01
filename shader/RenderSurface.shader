// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Sprites/RenderSurface"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_WaterColor ("Water Color", Color) = (1,1,1,1)
		_EdgeColor("Edge Color", Color) = (1,1,1,1)
		_YSize("YSize", float) = 5
		_XSize("XSize", float) = 5
		_SurfaceRippleSize("Surface Ripple Size", float) = 1
		_SurfaceRippleSpeed("Surface Ripple Speed", float) = 1
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
		}

		Cull Off
		Lighting Off
		ZWrite Off

		GrabPass
		{
			"_FullTexture"
		}

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ PIXELSNAP_ON
			#include "UnityCG.cginc"
			
			struct appdata_t 
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
				float4 grabPos : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
			};

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				//IN.vertex.x += 0.01;
				IN.vertex.y += 0.0625;
				//IN.vertex.x = IN.vertex.x - fmod(IN.vertex.x, 0.125);
				//IN.vertex.y = IN.vertex.y - fmod(IN.vertex.y, 0.125);
				IN.vertex.x = floor(IN.vertex.x * 8) / 8;
				IN.vertex.y = floor(IN.vertex.y * 8) / 8;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color;
				OUT.grabPos = ComputeGrabScreenPos(OUT.vertex);
				OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex);

				return OUT;
			}

			sampler2D _MainTex;
			sampler2D _AlphaTex;
			sampler2D _FullTexture;
			float _AlphaSplitEnabled;

			float _YSize;
			float _XSize;

			float _SurfaceRippleSize;
			float _SurfaceRippleSpeed;

			fixed4 _WaterColor;
			fixed4 _EdgeColor;

			fixed4 SampleSpriteTexture (float2 uv)
			{
				fixed4 color = tex2D (_MainTex, float2(uv.x, 0.5));

				return color;
			}

			float4 SnapToTexel(float4 uv) 
			{
				uv.xy = floor(uv.xy * float2(_XSize, _YSize)) / (float2(_XSize, _YSize));
				return uv;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				float texLevel = SampleSpriteTexture(IN.texcoord).a;

				float dif = texLevel - IN.texcoord.y;

				clip(dif);
				float texr = SampleSpriteTexture(IN.texcoord + float2(1 / _XSize, 0)).a;
				float texl = SampleSpriteTexture(IN.texcoord - float2(1 / _XSize, 0)).a;
				float difr = texr - IN.texcoord.y;
				float difl = texl - IN.texcoord.y;

				float difu = texLevel - (IN.texcoord.y + 1 / _YSize);
				float ripple = saturate((sin(_Time.x * _SurfaceRippleSpeed + IN.texcoord.x * _SurfaceRippleSize) + 1) / 1.8);
				float dif2u = texLevel - (IN.texcoord.y + 2 / _YSize) * ripple;
				float r = sign(difu) + sign(difl) + sign(difr) + sign(dif2u) - 3 * sign(dif);
				r *= -1;
				r = saturate(r);

				fixed4 c = _WaterColor * (1 - r) + _EdgeColor * r;
				fixed4 bgc = tex2Dproj(_FullTexture, IN.grabPos + SnapToTexel(float4(sin((_Time.y * 2) + IN.worldPos.y) / _XSize * 1.5, 0  , 0, 0)));
				c = bgc * (1 - c.a) + c * c.a;

				return c;
			}
		ENDCG
		}
	}
}