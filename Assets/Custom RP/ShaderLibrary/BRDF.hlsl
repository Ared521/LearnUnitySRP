#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

struct BRDF {
	float3 diffuse;
	float3 specular;
	float roughness;
};

// 在现实中光同样也会从电介质表面反弹，从而使它们产生高光。非金属反射率不尽相同，但平均值为0.04
#define MIN_REFLECTIVITY 0.04

float OneMinusReflectivity (float metallic) {
	float range = 1.0 - MIN_REFLECTIVITY;
	return (range - metallic * range);
}

BRDF GetBRDF (Surface surface, bool applyAlphaToDiffuse = false) {
	BRDF brdf;
	float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
	brdf.diffuse = surface.color * oneMinusReflectivity;
	// 当材质是半透明时，漫反射要乘alpha，这是因为blend的src设置为one了。
	if (applyAlphaToDiffuse) {
		brdf.diffuse *= surface.alpha;
	}
	
	// 能量守恒：出射光的数量不能超过入射光的数量。
	// 金属会影响镜面反射的颜色，但是非金属不会。非金属表面的反射颜色应该是白色。
	brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
	
	// roughness就相当于1 - smoothness，我们采用CommonMaterial.hlsl库里的转换函数：PerceptualSmoothnessToPerceptualRoughness(real smoothness)。
	// 新的变量类型real定义在commom.hlsl库里，根据不同的平台编译float或half。
	// PerceptualSmoothnessToPerceptualRoughness()源码里其实就是 1 - roughness
	// 这里用的是迪士尼光照模型，转化为平方，这样之后在材质编辑的时候更加直观。
	float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
	brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	return brdf;
}

// 我们观察到的镜面反射强度取决于我们的观察方向与完美反射方向的匹配程度。我们将使用和URP一样的公式，它是简化CookTorrance的变体。
// 简单来说就是计算高光强度。
float SpecularStrength (Surface surface, BRDF brdf, Light light) {
	float3 h = SafeNormalize(light.direction + surface.viewDirection);
	float nh2 = Square(saturate(dot(surface.normal, h)));
	float lh2 = Square(saturate(dot(light.direction, h)));
	float r2 = Square(brdf.roughness);
	float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
	float normalization = brdf.roughness * 4.0 + 2.0;
	return r2 / (d2 * max(0.1, lh2) * normalization);
}

// 给定曲面，灯光，BRDF通过直接照明获得的颜色。结果是结果为镜面反射颜色乘以镜面反射强度加上漫反射。
float3 DirectBRDF (Surface surface, BRDF brdf, Light light) {
	return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

#endif