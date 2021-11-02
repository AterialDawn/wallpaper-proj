varying vec2 fragCoord;

void main( void )
{
	gl_TexCoord[0]  = gl_MultiTexCoord0;
	fragCoord = gl_Position = ftransform();
}