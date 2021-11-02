/*
    This vertex shader is used for rendering the bar visualizer
*/

uniform mat4 texMatrix[2];

varying out vec2 texCoord[2]; //Only 2 textures will be active at any given time

void main( void )
{
	//gl_TexCoord[0]  = gl_TextureMatrix[0] * gl_MultiTexCoord0;
	texCoord[0] = texMatrix[0] * gl_Vertex;
	texCoord[1] = texMatrix[1] * gl_Vertex;
	gl_Position = ftransform();
	gl_FrontColor = vec4(1,1,1,1);
}