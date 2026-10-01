shader_type spatial;
render_mode blend_mix, cull_back, depth_draw_never, unshaded;

#include "res://Rendering/Assets/Color.gdshaderinc"

uniform sampler2DArray frames : filter_linear_mipmap, repeat_enable;
uniform float frame_count = 30.0;
uniform float frames_per_second = 30.0;

uniform sampler2D frame_color : hint_screen_texture, filter_linear;

void fragment() {
	float frame = mod(floor(TIME * frames_per_second), frame_count);
	vec3 water = texture(frames, vec3(UV, frame)).rgb * COLOR.rgb;
	vec3 behind = linear_to_srgb(clamp(texture(frame_color, SCREEN_UV).rgb, 0.0, 1.0));

	ALBEDO = srgb_to_linear(mix(behind, water, COLOR.a));
	ALPHA = 1.0;
}
