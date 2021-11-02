uniform float FillPercentage;
uniform vec4 FillColor;

varying vec2 quadCoord;

float HPI = 1.5707963269;
float PI = 3.1415926538;
float PI2 = 6.2831853076;
float radius = 0.45;
float innerRadius = 0.35;

void main(void)
{
    float u = 0.5 - quadCoord.x;
    float v = 0.5 - quadCoord.y;
    vec2 uv = vec2(u, v);
    vec2 nuv = normalize(uv);
    vec2 up = normalize(vec2(radius, 0.0));
    float l = length(uv);
    float _dot = up.x*nuv.x + up.y*nuv.y;
    float _det = up.x*nuv.y - up.y*nuv.x;
    float angle = atan(_dot, _det);
    if(angle < 0.0) angle = PI + (PI + angle);
    angle /= PI2;
    
    gl_FragColor = vec4(0);
    if(l < radius && l > innerRadius){
        if(angle < FillPercentage) {
            vec4 color = FillColor;
            gl_FragColor = color;
        }
    }
}