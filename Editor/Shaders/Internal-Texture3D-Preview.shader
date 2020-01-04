// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Unlit shader. Simplest possible textured shader.
// - no lighting
// - no lightmap support
// - no per-material color

// Modified to work with a Texture3D in the Texture3DAtlasInspector window.

Shader "Hidden/Internal-Texture3D-Preview" {
Properties {
    _MainTex ("Base (RGB)", 3D) = "" {}
    _Depth("Depth", Range(0, 1)) = 0
    _Mip ("Mip", float) = 0
    _ColorMask("Color Mask", Color) = (1,1,1,1)
}

SubShader {
    Tags
    {
        "Queue" = "Transparent"
        "IgnoreProjector" = "True"
        "RenderType" = "Transparent"
        "PreviewType" = "Plane"
        "CanUseSpriteAtlas" = "True"
    }

    Cull Off
    Lighting Off
    ZWrite Off
    Blend SrcAlpha OneMinusSrcAlpha
    //ColorMask RGBA

    Pass {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 3.5
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

			UNITY_DECLARE_TEX3D(_MainTex);
            float4 _MainTex_ST;
            float _Depth;
            float _Mip;
            float4 _ColorMask;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.texcoord.z = _Depth;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = UNITY_SAMPLE_TEX3D_LOD(_MainTex, i.texcoord, _Mip);

                bool alphaOnly = (dot(_ColorMask.rgb, _ColorMask.rgb) <= 0.5) && _ColorMask.a > 0.5;
                if (alphaOnly)
                    col = float4(col.a, col.a, col.a, 1);
                else
                    col *= _ColorMask;

                UNITY_OPAQUE_ALPHA(col.a);
                return col;
            }
        ENDCG
    }
}

}
