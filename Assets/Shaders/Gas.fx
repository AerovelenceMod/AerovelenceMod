float4x4 MatrixTransform;
float2 SampleTexel;
float2 VertexTexel;
float Subdivision;
float CellPixels;
float Additive;
float TileCollision;

texture OpticalTexture;
texture AppearanceTexture;
texture EnvironmentTexture;
texture VertexTexture;

sampler2D OpticalSampler : register(s0) = sampler_state
{
    Texture = <OpticalTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D AppearanceSampler : register(s1) = sampler_state
{
    Texture = <AppearanceTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D LightSampler : register(s2) = sampler_state
{
    Texture = <EnvironmentTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D VertexSampler : register(s3) = sampler_state
{
    Texture = <VertexTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D TerrainSampler : register(s4) = sampler_state
{
    Texture = <EnvironmentTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexInput
{
    float2 Position : POSITION0;
    float2 Local : TEXCOORD0;
    float2 SampleOrigin : TEXCOORD1;
    float2 VertexOrigin : TEXCOORD2;
};

struct VertexOutput
{
    float4 Position : POSITION0;
    float2 Local : TEXCOORD0;
    float2 SampleOrigin : TEXCOORD1;
    float2 VertexOrigin : TEXCOORD2;
};

VertexOutput Transform(VertexInput input)
{
    VertexOutput output;
    output.Position = mul(float4(input.Position, 0.0, 1.0), MatrixTransform);
    output.Local = input.Local;
    output.SampleOrigin = input.SampleOrigin;
    output.VertexOrigin = input.VertexOrigin;
    return output;
}

float4 Weights(float t)
{
    float t2 = t * t;
    float t3 = t2 * t;
    return float4(-7.0 * t3 + 15.0 * t2 - 9.0 * t + 1.0,
        21.0 * t3 - 36.0 * t2 + 16.0,
        -21.0 * t3 + 27.0 * t2 + 9.0 * t + 1.0,
        7.0 * t3 - 6.0 * t2);
}

float4 Row(sampler2D source, float2 uv, float4 weights)
{
    float4 a = tex2Dlod(source, float4(uv, 0.0, 0.0));
    float middle = weights.y + weights.z;
    float4 b = tex2Dlod(source, float4(uv + float2(SampleTexel.x * (1.0 + weights.z / middle), 0.0), 0.0, 0.0));
    float4 d = tex2Dlod(source, float4(uv + float2(SampleTexel.x * 3.0, 0.0), 0.0, 0.0));
    return (a * weights.x + b * middle + d * weights.w) / 18.0;
}

float4 Reconstruct(sampler2D source, float2 uv, float4 wx, float4 wy)
{
    float4 a = Row(source, uv, wx);
    float middle = wy.y + wy.z;
    float4 b = Row(source, uv + float2(0.0, SampleTexel.y * (1.0 + wy.z / middle)), wx);
    float4 d = Row(source, uv + float2(0.0, SampleTexel.y * 3.0), wx);
    return max((a * wy.x + b * middle + d * wy.w) / 18.0, 0.0);
}

float4 Shade(VertexOutput input) : COLOR0
{
    float2 grid = floor(input.Local) / Subdivision + 0.5;
    float2 cell = floor(grid);
    float2 fraction = frac(grid);
    float2 uv = (input.SampleOrigin + cell + 0.5) * SampleTexel;
    float4 wx = Weights(fraction.x), wy = Weights(fraction.y);
    float4 value = Reconstruct(OpticalSampler, uv, wx, wy);
    if (value.a <= 0.003)
        return 0.0;
    float4 appearance = Reconstruct(AppearanceSampler, uv, wx, wy);
    if (appearance.x <= 0.0)
        return 0.0;
    float3 ambient = tex2Dlod(LightSampler, float4((input.SampleOrigin + cell + fraction + 1.5) * SampleTexel, 0.0, 0.0)).rgb;
    float3 pigment = saturate(value.rgb / value.a);
    float3 lighting = lerp(saturate(ambient), 1.0, saturate(appearance.y / value.a));
    float brightness = clamp(appearance.w / value.a, 0.0, 3.0);
    float opacity = 1.0 - exp(-appearance.x * (Additive > 0.5 ? 0.95 : 0.78));
    float4 color;
    if (Additive > 0.5)
    {
        float3 rgb = lerp(pigment, 1.0, opacity * 0.3) * (0.72 + opacity * 0.28) * brightness;
        color = float4(saturate(rgb * lighting * (opacity * 0.72)), 0.0);
    }
    else
        color = float4(saturate(pigment * lighting * opacity * 0.97 * brightness), opacity * 0.76);
    return floor(saturate(color) * 255.0) / 255.0;
}

bool Solid(float2 local, float2 origin)
{
    float2 tile = floor(local / 16.0);
    float2 offset = local - tile * 16.0;
    float shape = floor(tex2Dlod(TerrainSampler, float4((origin + tile + 1.5) * SampleTexel, 0.0, 0.0)).a * 255.0 + 0.5);
    if (shape < 0.5) return false;
    if (shape < 1.5) return true;
    if (shape < 2.5) return offset.y >= 8.0;
    if (shape < 3.5) return offset.y >= 16.0 - offset.x;
    if (shape < 4.5) return offset.y >= offset.x;
    if (shape < 5.5) return offset.y <= offset.x;
    return offset.y <= 16.0 - offset.x;
}

bool Blocked(float2 local, float2 origin)
{
    [branch]
    if (!Solid(local, origin))
        return false;
    return Solid(local + float2(4.0, 0.0), origin) && Solid(local - float2(4.0, 0.0), origin)
        && Solid(local + float2(0.0, 4.0), origin) && Solid(local - float2(0.0, 4.0), origin)
        && Solid(local + float2(2.82842712, 2.82842712), origin) && Solid(local - float2(2.82842712, 2.82842712), origin)
        && Solid(local + float2(2.82842712, -2.82842712), origin) && Solid(local + float2(-2.82842712, 2.82842712), origin);
}

float4 Composite(VertexOutput input) : COLOR0
{
    float2 quad = floor(input.Local);
    float2 fraction = frac(input.Local);
    if (TileCollision > 0.5 && Blocked((quad + 0.5) * (CellPixels / Subdivision), input.SampleOrigin))
        discard;
    float2 uv = (input.VertexOrigin + quad + 0.5) * VertexTexel;
    float4 a = tex2Dlod(VertexSampler, float4(uv, 0.0, 0.0));
    float4 b = tex2Dlod(VertexSampler, float4(uv + float2(VertexTexel.x, 0.0), 0.0, 0.0));
    float4 c = tex2Dlod(VertexSampler, float4(uv + float2(0.0, VertexTexel.y), 0.0, 0.0));
    float4 d = tex2Dlod(VertexSampler, float4(uv + VertexTexel, 0.0, 0.0));
    if (fmod(quad.x + quad.y, 2.0) < 0.5)
    {
        if (fraction.x + fraction.y <= 1.0)
            return a * (1.0 - fraction.x - fraction.y) + b * fraction.x + c * fraction.y;
        return b * (1.0 - fraction.y) + d * (fraction.x + fraction.y - 1.0) + c * (1.0 - fraction.x);
    }
    if (fraction.y <= fraction.x)
        return a * (1.0 - fraction.x) + b * (fraction.x - fraction.y) + d * fraction.y;
    return a * (1.0 - fraction.y) + d * fraction.x + c * (fraction.y - fraction.x);
}

technique GasReconstruct
{
    pass Gas
    {
        VertexShader = compile vs_3_0 Transform();
        PixelShader = compile ps_3_0 Shade();
    }
}

technique GasComposite
{
    pass Gas
    {
        VertexShader = compile vs_3_0 Transform();
        PixelShader = compile ps_3_0 Composite();
    }
}
