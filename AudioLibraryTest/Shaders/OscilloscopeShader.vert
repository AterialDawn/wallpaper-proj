/*
    This vertex shader is used in rendering the oscilloscope visualizer
*/

void main( void )
{
	gl_Position = ftransform();
    gl_FrontColor = gl_Color;
}