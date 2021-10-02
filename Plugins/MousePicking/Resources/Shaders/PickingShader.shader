Shader "Unlit/PickingShader"
{
    Properties
    {
        _GameObjectID ("Game Object ID", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="SRPDefaultUnlit" }
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
            };

            float _GameObjectID;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float frag() : COLOR
            {
                return _GameObjectID;
            }
            ENDCG
        }
    }
}
