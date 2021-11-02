/*
    This vertex shader is used in TextRenderer.cs
*/


void main( void )
{
    gl_TexCoord[0]  = gl_MultiTexCoord0;
	gl_Position = ftransform();
    gl_FrontColor = gl_Color;
}