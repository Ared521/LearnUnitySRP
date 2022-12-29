#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

struct BRDF {
	float3 diffuse;
	float3 specular;
	float roughness;
};

// ����ʵ�й�ͬ��Ҳ��ӵ���ʱ��淴�����Ӷ�ʹ���ǲ����߹⡣�ǽ��������ʲ�����ͬ����ƽ��ֵΪ0.04
#define MIN_REFLECTIVITY 0.04

float OneMinusReflectivity (float metallic) {
	float range = 1.0 - MIN_REFLECTIVITY;
	return (range - metallic * range);
}

BRDF GetBRDF (Surface surface, bool applyAlphaToDiffuse = false) {
	BRDF brdf;
	float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
	brdf.diffuse = surface.color * oneMinusReflectivity;
	// �������ǰ�͸��ʱ��������Ҫ��alpha��������Ϊblend��src����Ϊone�ˡ�
	if (applyAlphaToDiffuse) {
		brdf.diffuse *= surface.alpha;
	}
	
	// �����غ㣺�������������ܳ���������������
	// ������Ӱ�쾵�淴�����ɫ�����Ƿǽ������ᡣ�ǽ�������ķ�����ɫӦ���ǰ�ɫ��
	brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
	
	// roughness���൱��1 - smoothness�����ǲ���CommonMaterial.hlsl�����ת��������PerceptualSmoothnessToPerceptualRoughness(real smoothness)��
	// �µı�������real������commom.hlsl������ݲ�ͬ��ƽ̨����float��half��
	// PerceptualSmoothnessToPerceptualRoughness()Դ������ʵ���� 1 - roughness
	// �����õ��ǵ�ʿ�����ģ�ͣ�ת��Ϊƽ��������֮���ڲ��ʱ༭��ʱ�����ֱ�ۡ�
	float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
	brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	return brdf;
}

// ���ǹ۲쵽�ľ��淴��ǿ��ȡ�������ǵĹ۲췽�����������䷽���ƥ��̶ȡ����ǽ�ʹ�ú�URPһ���Ĺ�ʽ�����Ǽ�CookTorrance�ı��塣
// ����˵���Ǽ���߹�ǿ�ȡ�
float SpecularStrength (Surface surface, BRDF brdf, Light light) {
	float3 h = SafeNormalize(light.direction + surface.viewDirection);
	float nh2 = Square(saturate(dot(surface.normal, h)));
	float lh2 = Square(saturate(dot(light.direction, h)));
	float r2 = Square(brdf.roughness);
	float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
	float normalization = brdf.roughness * 4.0 + 2.0;
	return r2 / (d2 * max(0.1, lh2) * normalization);
}

// �������棬�ƹ⣬BRDFͨ��ֱ��������õ���ɫ������ǽ��Ϊ���淴����ɫ���Ծ��淴��ǿ�ȼ��������䡣
float3 DirectBRDF (Surface surface, BRDF brdf, Light light) {
	return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

#endif