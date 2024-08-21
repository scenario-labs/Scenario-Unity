Shader "Scenario/Projected Debug"
{
   Properties
    {
        _MainTexFront ("Front Texture", 2D) = "white" {}
        _MainTexBack ("Back Texture", 2D) = "white" {}
        _MainTexLeft ("Left Texture", 2D) = "white" {}
        _MainTexRight ("Right Texture", 2D) = "white" {}
        _MainTexTop ("Top Texture", 2D) = "white" {}
        _MainTexBottom ("Bottom Texture", 2D) = "white" {}
        _MainTexOther ("Other Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct MeshData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv: TEXCOORD0;
                float2 uv2 : TEXCOORD2;
            };

            struct v2f
            {
                float2 uv2 : TEXCOORD11;
                float2 uv_MainTexFront : TEXCOORD0;
                float2 uv_MainTexBack : TEXCOORD1;
                float2 uv_MainTexLeft : TEXCOORD2;
                float2 uv_MainTexRight : TEXCOORD3;
                float2 uv_MainTexTop : TEXCOORD4;
                float2 uv_MainTexBottom : TEXCOORD5;
                float2 uv_MainTexOther : TEXCOORD6;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            sampler2D _MainTexFront;
            float4 _MainTexFront_ST;

            sampler2D _MainTexBack;
            float4 _MainTexBack_ST;

            sampler2D _MainTexLeft;
            float4 _MainTexLeft_ST;

            sampler2D _MainTexRight;
            float4 _MainTexRight_ST;

            sampler2D _MainTexTop;
            float4 _MainTexTop_ST;

            sampler2D _MainTexBottom;
            float4 _MainTexBottom_ST;

            sampler2D _MainTexOther;
            float4 _MainTexOther_ST;

            v2f vert(MeshData v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                o.uv2 = v.uv2;

                o.uv_MainTexFront = TRANSFORM_TEX(v.uv, _MainTexFront);
                o.uv_MainTexBack = TRANSFORM_TEX(v.uv, _MainTexBack);
                o.uv_MainTexLeft = TRANSFORM_TEX(v.uv, _MainTexLeft);
                o.uv_MainTexRight = TRANSFORM_TEX(v.uv, _MainTexRight);
                o.uv_MainTexTop = TRANSFORM_TEX(v.uv, _MainTexTop);
                o.uv_MainTexBottom = TRANSFORM_TEX(v.uv, _MainTexBottom);
                o.uv_MainTexOther = TRANSFORM_TEX(v.uv, _MainTexOther);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 finalColor;

                // Sample UV2 coordinate to determine face
                float2 uv2Coord = i.uv2;

                fixed4 front = tex2D(_MainTexFront, i.uv_MainTexFront);
                fixed4 back = tex2D(_MainTexBack, i.uv_MainTexBack);
                fixed4 left = tex2D(_MainTexLeft, i.uv_MainTexLeft);
                fixed4 right = tex2D(_MainTexRight, i.uv_MainTexRight);
                fixed4 top = tex2D(_MainTexTop, i.uv_MainTexTop);
                fixed4 bottom = tex2D(_MainTexBottom, i.uv_MainTexBottom);
                fixed4 other = tex2D(_MainTexOther, i.uv_MainTexOther);
                
                // Determine which face the fragment belongs to based on UV2 coordinate
                if (uv2Coord.x < 0.333 && uv2Coord.y < 0.333)
                    finalColor = top;
                else if (uv2Coord.x < 0.666 && uv2Coord.y < 0.333)
                    finalColor = front;
                else if (uv2Coord.x < 1.0 && uv2Coord.y < 0.333)
                    finalColor = left;
                else if (uv2Coord.x < 0.333 && uv2Coord.y < 0.666)
                    finalColor = bottom;
                else if (uv2Coord.x < 0.666 && uv2Coord.y < 0.666)
                    finalColor = back;
                else if (uv2Coord.x < 1.0 && uv2Coord.y < 0.666)
                    finalColor = right;
                /*else if (uv2Coord.x < 0.333 && uv2Coord.y < 1.0)
                    finalColor = float4(1, 0, 1, 0);
                else if (uv2Coord.x < 0.666 && uv2Coord.y < 1.0)
                    finalColor = float4(1, 1, 1, 0);*/
                else
                    finalColor = other;

                return finalColor;
}
            ENDCG
        }
    }
    FallBack "Diffuse"
}
