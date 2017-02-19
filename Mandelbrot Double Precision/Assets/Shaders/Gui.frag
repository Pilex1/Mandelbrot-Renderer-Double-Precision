#version 410 core

void main(void) {
	vec3 colour = gl_FragColor.rgb;
	colour = vec3(1, 1, 1) - colour;
	gl_FragColor.rgb = colour;
}