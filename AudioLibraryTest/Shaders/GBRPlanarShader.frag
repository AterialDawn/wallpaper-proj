uniform sampler2D greenTex;
uniform sampler2D blueTex;
uniform sampler2D redTex;

void main (void)  
{
    vec2 sampleLoc = gl_TexCoord[0];
    vec4 redSample = texture2D(redTex, sampleLoc);
    vec4 greenSample = texture2D(greenTex, sampleLoc);
    vec4 blueSample = texture2D(blueTex, sampleLoc);
    gl_FragColor = vec4(redSample.r, greenSample.r, blueSample.r, 1.0);
} 