/*
    This vertex shader is used in rendering the final texture to the screen from Render-To-Texture
*/

varying vec4 vertColor;
varying vec2 texUV;

void main( void )
{
	texUV  = gl_MultiTexCoord0;
	gl_Position = ftransform();
	vertColor = gl_Color;
}