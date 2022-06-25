#extension GL_EXT_gpu_shader4 : enable

varying vec2 fragCoord;

uniform sampler2D image;
uniform vec2 resolution;
uniform bool blur;
uniform bool colorOverride;
uniform vec4 color;


const int samples = 30,
          LOD = 2,         // gaussian done on MIPmap at scale LOD
          sLOD = 1 << LOD; // tile size = 2^LOD
const float sigma = float(samples) * .25;

float gaussian(vec2 i) {
    return exp( -.5* dot(i/=sigma,i) ) / ( 6.28 * sigma*sigma );
}

vec4 blurFunc(sampler2D sp, vec2 U, vec2 scale) {
    vec4 O = vec4(0);  
    int s = samples/sLOD;
    
    for ( int i = 0; i < s*s; i++ ) {
        vec2 d = vec2(i%s, i/s)*float(sLOD) - float(samples)/2.;
        O += gaussian(d) * texture2D( sp, U + scale * d);
    }
    
    return O / O.a;
}

vec3 adjustExposure(vec3 color, float value) {
  return (1.0 + value) * color;
}


void main(void)
{
    if(colorOverride)
    {
        gl_FragColor = color;
        return;
    }
	vec2 sampleLoc = gl_TexCoord[0].st;
    vec4 output;
    if(blur)
    {
	    output = blurFunc(image, sampleLoc, 1. / resolution.xy);
	    output = vec4(adjustExposure(output.rgb, -.31), 1);
    }
    else
    {
        output = texture2D(image, sampleLoc);
    }
	gl_FragColor = output;
}