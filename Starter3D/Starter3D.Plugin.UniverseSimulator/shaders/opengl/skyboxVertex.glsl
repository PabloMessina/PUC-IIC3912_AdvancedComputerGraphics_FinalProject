#version 330

precision highp float;
uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;
uniform mat4 modelMatrix;

in vec3 inPosition;

out vec3 fragPosition;

void main(void)
{
  vec4 worldPosition = modelMatrix * vec4(inPosition,1);
  fragPosition = worldPosition.xyz;
  gl_Position = projectionMatrix * viewMatrix * worldPosition;
}