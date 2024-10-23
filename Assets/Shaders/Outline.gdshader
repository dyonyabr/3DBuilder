shader_type spatial;

uniform sampler2D screen_texture: hint_screen_texture, repeat_disable, filter_nearest;

void fragment() {
	vec3 tex = textureLod(screen_texture, SCREEN_UV, 0.0).rgb;
	tex.b = tex.b + .15f;
	tex.g = tex.g + .075f;
	
	ALBEDO = tex;
}

