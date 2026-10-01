// Body shared by every model shader variant. ModelShaderBuilder writes the
// shader_type line, the render_mode line and the defines below around it.
//
//   TOP_ALPHA_TEST   cut fragments whose texture alpha is under alpha_cutoff
//   TOP_OPAQUE       the variant stays in Godot's opaque pipeline
//   TOP_UNLIT        the variant declares unshaded, so lighting is skipped
//   TOP_SRC_ONE      the source colour enters the blend unscaled
//   TOP_SRC_COLOR    the source colour enters the blend scaled by itself
//   TOP_SRGB_ADD     the variant adds to the frame, and the sum runs in sRGB
//   TOP_UV_ANIMATED  uv_transform and uv_offset carry a texture transform

#include "res://Rendering/Assets/Color.gdshaderinc"

uniform sampler2D base_map : filter_linear_mipmap, repeat_enable;
uniform float alpha_cutoff = 0.5;
uniform float opacity : hint_range(0.0, 1.0) = 1.0;
uniform vec4 uv_transform = vec4(1.0, 0.0, 0.0, 1.0);
uniform vec2 uv_offset = vec2(0.0, 0.0);

#ifndef TOP_UNLIT
global uniform vec3 ambient_sky_color;
global uniform vec3 ambient_horizon_color;
global uniform vec3 ambient_ground_color;

instance uniform vec3 ambient_tint = vec3(1.0);

varying vec3 world_normal;
#endif

#ifdef TOP_SRGB_ADD
uniform sampler2D frame_color : hint_screen_texture, filter_linear;

#ifndef TOP_UNLIT
varying vec3 add_source;
varying vec3 add_ambient;
#endif

vec3 add_in_srgb(vec2 frame_uv, vec3 source) {
	vec3 frame = clamp(texture(frame_color, frame_uv).rgb, 0.0, 1.0);

	return srgb_to_linear(min(linear_to_srgb(frame) + source, vec3(1.0))) - frame;
}
#endif

void vertex() {
#ifndef TOP_UNLIT
	world_normal = MODEL_NORMAL_MATRIX * NORMAL;
#endif

#ifdef TOP_UV_ANIMATED
	// The legacy transform is [u v 1] * M, written row by row. glTF and Godot
	// both put the UV origin top left, so it applies to UV as it stands.
	UV = vec2(
			UV.x * uv_transform.x + UV.y * uv_transform.z + uv_offset.x,
			UV.x * uv_transform.y + UV.y * uv_transform.w + uv_offset.y);
#endif
}

#ifndef TOP_UNLIT
vec3 mix_ambient(vec3 normal) {
	vec3 ambient = normal.y > 0.0
			? mix(ambient_horizon_color, ambient_sky_color, normal.y)
			: mix(ambient_horizon_color, ambient_ground_color, -normal.y);

	return ambient * ambient_tint;
}

vec3 shade(vec3 normal, vec3 sun) {
	return srgb_to_linear(mix_ambient(normal) + sun);
}
#endif

void fragment() {
	vec4 base = texture(base_map, UV) * COLOR;

#ifdef TOP_ALPHA_TEST
	if (base.a < alpha_cutoff) {
		discard;
	}
#endif

	vec3 rgb = base.rgb;

#ifndef TOP_OPAQUE
	float a = base.a * opacity;

#ifdef TOP_SRC_ONE
	a = 1.0;
#endif

#ifdef TOP_SRC_COLOR
	rgb *= rgb;
	a = 1.0;
#endif

	ALPHA = a;
#endif

#ifdef TOP_SRGB_ADD
	ALPHA = 1.0;

#ifdef TOP_UNLIT
	ALBEDO = add_in_srgb(SCREEN_UV, rgb * a);
#else
	vec3 ambient_step = add_in_srgb(SCREEN_UV, rgb * a * mix_ambient(normalize(world_normal)));

	add_source = rgb * a;
	add_ambient = ambient_step;
	ALBEDO = vec3(1.0);
	EMISSION = ambient_step;
#endif
#else
	ALBEDO = srgb_to_linear(rgb);

#ifndef TOP_UNLIT
	EMISSION = ALBEDO * shade(normalize(world_normal), vec3(0.0));
#endif
#endif
}

#ifndef TOP_UNLIT
void light() {
	if (LIGHT_IS_DIRECTIONAL) {
		vec3 normal = normalize(world_normal);
		vec3 sun = linear_to_srgb(LIGHT_COLOR / PI) * (max(dot(NORMAL, LIGHT), 0.0) * ATTENUATION);

#ifdef TOP_SRGB_ADD
		DIFFUSE_LIGHT += add_in_srgb(SCREEN_UV, add_source * (mix_ambient(normal) + sun)) - add_ambient;
#else
		DIFFUSE_LIGHT += shade(normal, sun) - shade(normal, vec3(0.0));
#endif
	}
}
#endif
