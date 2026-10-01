// The legacy engine did its color maths on sRGB values, the way D3D9 did.
// Godot renders in linear space, so the ported shaders hold the blend and the
// lighting in sRGB and convert with these two.

vec3 srgb_to_linear(vec3 color) {
	return mix(pow((color + vec3(0.055)) / 1.055, vec3(2.4)), color / 12.92, lessThan(color, vec3(0.04045)));
}

vec3 linear_to_srgb(vec3 color) {
	return mix(1.055 * pow(color, vec3(1.0 / 2.4)) - vec3(0.055), color * 12.92, lessThan(color, vec3(0.0031308)));
}
