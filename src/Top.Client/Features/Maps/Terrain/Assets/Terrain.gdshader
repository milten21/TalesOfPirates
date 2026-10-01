shader_type spatial;
render_mode cull_back, depth_draw_opaque, ambient_light_disabled, specular_disabled;

#include "res://Rendering/Assets/Color.gdshaderinc"

uniform sampler2DArray terrain_textures : filter_linear_mipmap, repeat_enable;
uniform sampler2D mask_atlas : filter_linear, repeat_disable;

global uniform vec3 ambient_sky_color;

varying vec4 mask23_uv;
varying flat vec4 slices;

void vertex() {
	mask23_uv = CUSTOM0;
	slices = CUSTOM1;
}

vec3 blend_detail(vec3 color, vec2 uv, vec2 mask_uv, float slice) {
	float mask = texture(mask_atlas, mask_uv).a * step(0.5, slice);
	vec3 detail = texture(terrain_textures, vec3(uv, slice)).rgb;

	return mix(color, detail, mask);
}

void fragment() {
	vec3 color = texture(terrain_textures, vec3(UV, slices.x)).rgb;

	color = blend_detail(color, UV, UV2, slices.y);
	color = blend_detail(color, UV, mask23_uv.xy, slices.z);
	color = blend_detail(color, UV, mask23_uv.zw, slices.w);

	ALBEDO = srgb_to_linear(color * COLOR.rgb);
	EMISSION = ALBEDO * srgb_to_linear(ambient_sky_color);
}

void light() {
	if (LIGHT_IS_DIRECTIONAL) {
		DIFFUSE_LIGHT += srgb_to_linear(mix(ambient_sky_color, vec3(1.0), ATTENUATION))
				- srgb_to_linear(ambient_sky_color);
	}
}
