Shader "Custom/ObjectSpaceTexture"
{
    Properties
    {
        _Tint ("Color", Color) = (1,1,1,1)
		[NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
		_Tiling ("Tiling", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
			CGPROGRAM

			#pragma vertex myVertexProgram
			#pragma fragment myFragmentProgram

			#include "UnityStandardBRDF.cginc"

			sampler2D _MainTex;
			float4 _Tint, _Tiling;

			struct VertextData {
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct Interpolators 
			{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : TEXCOORD1;
			};

			Interpolators myVertexProgram(VertextData v)
			{
                Interpolators i;
                float3 n = abs(normalize(v.normal)); 
                float2 uvScale;

                if (n.x >= n.y && n.x >= n.z)
                {
                    uvScale = float2(_Tiling.z, _Tiling.y);
                }
                else if (n.y >= n.x && n.y >= n.z)
                {
                    uvScale = float2(_Tiling.x, _Tiling.z);
                }
                else
                {
                    uvScale = float2(_Tiling.x, _Tiling.y);
                }

                i.uv = v.uv * uvScale;
                i.normal = v.normal;
                i.position = UnityObjectToClipPos(v.position);
                return i;
			}

			float4 myFragmentProgram(Interpolators i) : SV_TARGET
			{
				float3 albedo = tex2D(_MainTex, i.uv).rgb * _Tint.rgb;
				return float4(albedo,1);
			}

			ENDCG
        }

        Pass {
			Tags {
				"LightMode" = "ShadowCaster"
			}

			CGPROGRAM

			#pragma target 3.0

			#pragma vertex MyShadowVertexProgram
			#pragma fragment MyShadowFragmentProgram

			struct VertexData {
				float4 position : POSITION;
			};
			
			float4 MyShadowVertexProgram (VertexData v) : SV_POSITION {
				return UnityObjectToClipPos(v.position);
			}

			half4 MyShadowFragmentProgram () : SV_TARGET {
				return 0;
			}

			ENDCG
		}

    }
}
