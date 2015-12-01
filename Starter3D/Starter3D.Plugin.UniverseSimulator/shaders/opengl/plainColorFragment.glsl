#version 330

uniform vec3 color;

in vec3 fragPosition;
in vec3 fragNormal;
in vec3 fragTextureCoords;

out vec4 outFragColor;

void main(void)
{  
	outFragColor = vec4(color,1);
}