uniform sampler2D yTex;
uniform sampler2D uTex;
uniform sampler2D vTex;
uniform bool limitedToFullRangeConvert;

void main() {
    vec2 sampleLoc = gl_TexCoord[0];
    float r, g, b, y, u, v;

    y = texture2D(yTex, sampleLoc).r;
    u = texture2D(uTex, sampleLoc).r - 0.5;
    v = texture2D(vTex, sampleLoc).r - 0.5;

    r = y + 1.13983*v;
    g = y - 0.39465*u - 0.58060*v;
    b = y + 2.03211*u;

    if(limitedToFullRangeConvert)
    {
        gl_FragColor =  ((vec4(r,g,b,1.0) - (16.0 / 255.0)) * (255.0 / 219.0));
    }
    else
    {
        gl_FragColor =  vec4(r,g,b,1.0);
    }
} 