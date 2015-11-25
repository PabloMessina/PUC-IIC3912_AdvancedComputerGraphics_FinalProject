#version 330

uniform vec3 cameraPosition;

uniform sampler2D topTex;
uniform sampler2D bottomTex;
uniform sampler2D leftTex;
uniform sampler2D rightTex;
uniform sampler2D frontTex;
uniform sampler2D backTex;

in vec3 fragPosition;

out vec4 outFragColor;

void main(void)
{  
	vec3 dir = normalize(fragPosition - cameraPosition);
	float rx = dir.x;
	float ry = dir.y;
	float rz = dir.z;
    float sc = 0;
    float tc = 0;
    float ma = 0;
    float u = 0;
    float v = 0;

	float _rx = abs(rx);
	float _ry = abs(ry);
	float _rz = abs(rz);


	if (_rx > _ry && _rx > _rz) {

		if (rx > 0) 
		{
			sc = -rz;
            tc = -ry;
            ma = _rx;
			u = (sc / ma + 1) / 2;
			v = (tc / ma + 1) / 2;
			outFragColor = texture(rightTex, vec2(u,v));
		}
		else 
		{
			sc = rz;
            tc = -ry;
            ma = _rx;
			u = (sc / ma + 1) / 2;
			v = (tc / ma + 1) / 2;
			outFragColor = texture(leftTex, vec2(u,v));
		}
	}
	else if (_ry > _rx && _ry > _rz) 
	{
		if (ry > 0)
        {
			sc = rx;
			tc = rz;

            ma = _ry;
			u = (sc / ma + 1) / 2;
			v = (tc / ma + 1) / 2;
			outFragColor = texture(topTex, vec2(u,v));
        }
        else
        {
			sc = rx;
			tc = -rz;

            ma = _ry;
			u = (sc / ma + 1) / 2;
			v = (tc / ma + 1) / 2;
			outFragColor = texture(bottomTex, vec2(u,v));
        }
	}
	else 
	{
		 if (rz > 0)
        {
            sc = rx;
            tc = -ry;
            ma = _rz;
			u = (sc / ma + 1) / 2;
			v = (tc / ma + 1) / 2;
			outFragColor = texture(frontTex, vec2(u,v));
        }
        else
        {
            sc = -rx;
            tc = -ry;
            ma = _rz;
			u = (sc / ma + 1) / 2;
			v = (tc / ma + 1) / 2;
			outFragColor = texture(backTex, vec2(u,v));
        }
	}




}