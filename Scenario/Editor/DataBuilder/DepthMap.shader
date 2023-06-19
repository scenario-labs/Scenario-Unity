Shader "Custom/DepthMap"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float depth : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.depth = o.vertex.z / o.vertex.w;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float depth = i.depth;
                
                fixed4 color = float4(depth, depth, depth, 1.0);
                
                return color;
            }
            ENDCG
        }
    }
}
