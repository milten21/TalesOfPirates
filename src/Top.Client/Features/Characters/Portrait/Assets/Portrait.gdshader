shader_type canvas_item;
render_mode blend_premul_alpha;

uniform float saturation : hint_range(0.0, 1.0) = 1.0;
uniform float brightness : hint_range(0.0, 1.0) = 1.0;

void fragment() {
	vec4 color = texture(TEXTURE, UV);
	float grey = dot(color.rgb, vec3(0.2126, 0.7152, 0.0722));
	float from_centre = length(UV - vec2(0.5));
	float edge = fwidth(from_centre);

	color.rgb = mix(vec3(grey), color.rgb, saturation) * brightness;

	COLOR = color * (1.0 - smoothstep(0.5 - edge, 0.5, from_centre));
}
