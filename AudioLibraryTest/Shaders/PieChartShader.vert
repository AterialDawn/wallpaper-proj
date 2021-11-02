varying vec2 quadCoord;

void main(void)
{
    gl_Position = ftransform();
    quadCoord = gl_MultiTexCoord0;
}