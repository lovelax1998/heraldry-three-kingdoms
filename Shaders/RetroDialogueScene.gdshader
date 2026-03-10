shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;
uniform float sepia_strength : hint_range(0.0, 1.0) = 0.32;
uniform float grain_strength : hint_range(0.0, 0.2) = 0.045;
uniform float vignette_strength : hint_range(0.0, 1.0) = 0.24;
uniform float scanline_strength : hint_range(0.0, 0.15) = 0.028;
uniform float posterize_mix : hint_range(0.0, 1.0) = 0.18;
uniform float posterize_steps : hint_range(8.0, 64.0) = 24.0;
uniform float aberration : hint_range(0.0, 0.01) = 0.0014;
uniform vec3 warm_tint = vec3(1.05, 1.0, 0.92);

float hash(vec2 value) {
    return fract(sin(dot(value, vec2(127.1, 311.7))) * 43758.5453123);
}

vec3 apply_sepia(vec3 color) {
    return vec3(
        dot(color, vec3(0.393, 0.769, 0.189)),
        dot(color, vec3(0.349, 0.686, 0.168)),
        dot(color, vec3(0.272, 0.534, 0.131))
    );
}

void fragment() {
    vec2 uv = SCREEN_UV;
    vec2 viewport_size = 1.0 / SCREEN_PIXEL_SIZE;
    vec2 offset = vec2(aberration, 0.0);

    vec3 sampled = vec3(
        texture(screen_texture, uv + offset).r,
        texture(screen_texture, uv).g,
        texture(screen_texture, uv - offset).b
    );

    vec3 posterized = floor(sampled * posterize_steps) / posterize_steps;
    vec3 color = mix(sampled, posterized, posterize_mix);
    color = mix(color, apply_sepia(color), sepia_strength);
    color *= warm_tint;

    float grain = hash(floor(uv * viewport_size * 0.65) + floor(TIME * 36.0)) - 0.5;
    color += grain * grain_strength;

    float scanline = sin((uv.y * viewport_size.y * 0.72) + TIME * 2.1) * 0.5 + 0.5;
    color -= scanline * scanline_strength;

    vec2 centered = uv - vec2(0.5);
    float vignette = 1.0 - smoothstep(0.24, 0.78, length(centered * vec2(1.0, 1.2)));
    color *= mix(1.0, vignette, vignette_strength);

    COLOR = vec4(clamp(color, 0.0, 1.0), 1.0);
}