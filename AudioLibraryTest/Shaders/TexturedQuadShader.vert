/*
    This vertex shader is used in rendering the final texture to the screen from Render-To-Texture
*/


void main( void )
{
	gl_TexCoord[0]  = gl_MultiTexCoord0;
	gl_Position = ftransform();
}