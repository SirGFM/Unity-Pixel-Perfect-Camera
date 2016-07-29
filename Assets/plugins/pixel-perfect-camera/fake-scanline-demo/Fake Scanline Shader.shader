Shader "Hidden/Fake Scanline Shader" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		apply ("Is scaled", Float) = 0.0
		scale ("Scaling factor", Int) = 1
		height ("Scaled height", Int) = 240
	}
	SubShader {
		Cull Off ZWrite Off ZTest Off

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v) {
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			unsigned int scale;
			unsigned int height;
			float apply;

			fixed4 frag (v2f i) : SV_Target {
				fixed4 col, scanCol;
				int y;
				col = tex2D(_MainTex, i.uv);
				// i.uv.y == 1 -> top, i.uv.y == 0 -> bottom
				y = (int)(height * i.uv.y);
				// Every _scale lines, set the color of the last to 0
				scanCol = col * (float)(1 - (1 + y % scale) / scale);
				// select between base color and scanline color
				// _apply is set by script when _scale > 1
				return col * (1.0f - apply) + scanCol * apply;
			}
			ENDCG
		}
	}
}
