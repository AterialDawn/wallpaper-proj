/*
    This fragment shader is used for rendering the bar visualizer
*/
#define CATMULL_ROM_INTERP 1
#define BICUBIC_INTERP 2
#define BILINEAR_INTERP 3
#define BSPLINE_INTERP 4

uniform sampler2D tex;
uniform sampler2D tex2;

uniform float saturation;
uniform float blendValue;
uniform int primaryInterpolationType;
uniform int secondaryInterpolationType;
uniform bool texturing;
uniform bool blending;
uniform vec2 texResolutions[2];

in vec2 texCoord[2]; //Only 2 textures will be active at any given time

float BSpline( float x )
{
	float f = x;
	if( f < 0.0 )
	{
		f = -f;
	}

	if( f >= 0.0 && f <= 1.0 )
	{
		return ( 2.0 / 3.0 ) + ( 0.5 ) * ( f* f * f ) - (f*f);
	}
	else if( f > 1.0 && f <= 2.0 )
	{
		return 1.0 / 6.0 * pow( ( 2.0 - f  ), 3.0 );
	}
	return 1.0;
}
vec4 BSpline_Interp( sampler2D textureSampler, vec2 TexCoord, vec2 resolution )
{
    float texelSizeX = 1.0 / resolution.x; //size of one texel 
	float texelSizeY = 1.0 / resolution.y; //size of one texel 
    vec4 nSum = vec4( 0.0, 0.0, 0.0, 0.0 );
    vec4 nDenom = vec4( 0.0, 0.0, 0.0, 0.0 );
    float a = fract( TexCoord.x * resolution.x ); // get the decimal part
    float b = fract( TexCoord.y * resolution.y ); // get the decimal part
	int nX = int(TexCoord.x * resolution.x);
	int nY = int(TexCoord.y * resolution.y);
	vec2 TexCoord1 = vec2( float(nX) / resolution.x + 0.5 / resolution.x,
					       float(nY) / resolution.y + 0.5 / resolution.y );

    for( int m = -1; m <=2; m++ )
    {
        for( int n =-1; n<= 2; n++)
        {
			vec4 vecData = texture2D(textureSampler, TexCoord1 + vec2(texelSizeX * float( m ), texelSizeY * float( n )));
			float f  = BSpline( float( m ) - a );
			vec4 vecCooef1 = vec4( f,f,f,f );
			float f1 = BSpline( -( float( n ) - b ) );
			vec4 vecCoeef2 = vec4( f1, f1, f1, f1 );
            nSum = nSum + ( vecData * vecCoeef2 * vecCooef1  );
            nDenom = nDenom + (( vecCoeef2 * vecCooef1 ));
        }
    }
    return nSum / nDenom;
}

vec4 BiLinear_Interp( sampler2D textureSampler_i, vec2 texCoord_i, vec2 resolution )
{
	float texelSizeX = 1.0 / resolution.x; //size of one texel 
	float texelSizeY = 1.0 / resolution.y; //size of one texel 

	int nX = int( texCoord_i.x * resolution.x );
	int nY = int( texCoord_i.y * resolution.y );
	vec2 texCoord_New = vec2( ( float( nX ) + 0.5 ) / resolution.x,
							  ( float( nY ) + 0.5 ) / resolution.y );
	// Take nearest two data in current row.
    vec4 p0q0 = texture2D(textureSampler_i, texCoord_New);
    vec4 p1q0 = texture2D(textureSampler_i, texCoord_New + vec2(texelSizeX, 0));

	// Take nearest two data in bottom row.
    vec4 p0q1 = texture2D(textureSampler_i, texCoord_New + vec2(0, texelSizeY));
    vec4 p1q1 = texture2D(textureSampler_i, texCoord_New + vec2(texelSizeX , texelSizeY));

    float a = fract( texCoord_i.x * resolution.x ); // Get Interpolation factor for X direction.
											 // Fraction near to valid data.

	// Interpolation in X direction.
    vec4 pInterp_q0 = mix( p0q0, p1q0, a ); // Interpolates top row in X direction.
    vec4 pInterp_q1 = mix( p0q1, p1q1, a ); // Interpolates bottom row in X direction.

    float b = fract( texCoord_i.y * resolution.y ); // Get Interpolation factor for Y direction.
    return mix( pInterp_q0, pInterp_q1, b ); // Interpolate in Y direction.
}

float Triangular( float f )
{
	f = f / 2.0;
	if( f < 0.0 )
	{
		return ( f + 1.0 );
	}
	else
	{
		return ( 1.0 - f );
	}
	return 0.0;
}
vec4 BiCubic_Interp( sampler2D textureSampler, vec2 TexCoord, vec2 resolution  )
{
	float texelSizeX = 1.0 / resolution.x; //size of one texel 
	float texelSizeY = 1.0 / resolution.y; //size of one texel 
    vec4 nSum = vec4( 0.0, 0.0, 0.0, 0.0 );
    vec4 nDenom = vec4( 0.0, 0.0, 0.0, 0.0 );
    float a = fract( TexCoord.x * resolution.x ); // get the decimal part
    float b = fract( TexCoord.y * resolution.y ); // get the decimal part

	int nX = int(TexCoord.x * resolution.x);
	int nY = int(TexCoord.y * resolution.y);
	vec2 TexCoord1 = vec2( float(nX) / resolution.x + 0.5 / resolution.x,
					       float(nY) / resolution.y + 0.5 / resolution.y );

    for( int m = -1; m <=2; m++ )
    {
        for( int n =-1; n<= 2; n++)
        {
			vec4 vecData = texture2D(textureSampler, TexCoord1 + vec2(texelSizeX * float( m ), texelSizeY * float( n )));
			float f  = Triangular( float( m ) - a );
			vec4 vecCooef1 = vec4( f,f,f,f );
			float f1 = Triangular( -( float( n ) - b ) );
			vec4 vecCoeef2 = vec4( f1, f1, f1, f1 );
            nSum = nSum + ( vecData * vecCoeef2 * vecCooef1  );
            nDenom = nDenom + (( vecCoeef2 * vecCooef1 ));
        }
    }
    return nSum / nDenom;
}

float CatMullRom( float x )
{
    float B = 0.0;
    float C = 0.5;
    float f = x;
    if( f < 0.0 )
    {
        f = -f;
    }
    if( f < 1.0 )
    {
        return ( ( 12.0 - 9.0 * B - 6.0 * C ) * ( f * f * f ) +
            ( -18.0 + 12.0 * B + 6.0 * C ) * ( f * f ) +
            ( 6.0 - 2.0 * B ) ) / 6.0;
    }
    else if( f >= 1.0 && f < 2.0 )
    {
        return ( ( -B - 6 * C ) * ( f * f * f )
            + ( 6 * B + 30 * C ) * ( f *f ) +
            ( - ( 12 * B ) - 48 * C  ) * f +
            8 * B + 24 * C)/ 6.0;
    }
    else
    {
        return 0.0;
    }
}


vec4 BiCubic_Catmull( sampler2D textureSampler, vec2 TexCoord, vec2 resolution )
{
	float texelSizeX = 1.0 / resolution.x; //size of one texel 
	float texelSizeY = 1.0 / resolution.y; //size of one texel 
    vec4 nSum = vec4( 0.0, 0.0, 0.0, 0.0 );
    vec4 nDenom = vec4( 0.0, 0.0, 0.0, 0.0 );
    float a = fract( TexCoord.x * resolution.x ); // get the decimal part
    float b = fract( TexCoord.y * resolution.y ); // get the decimal part

	int nX = int(TexCoord.x * resolution.x);
	int nY = int(TexCoord.y * resolution.y);
	vec2 TexCoord1 = vec2( float(nX) / resolution.x + 0.5 / resolution.x,
					       float(nY) / resolution.y + 0.5 / resolution.y );

    for( int m = -1; m <=2; m++ )
    {
        for( int n =-1; n<= 2; n++)
        {
			vec4 vecData = texture2D(textureSampler, TexCoord1 + vec2(texelSizeX * float( m ), texelSizeY * float( n )));
			float f  = CatMullRom( float( m ) - a );
			vec4 vecCooef1 = vec4( f,f,f,f );
			float f1 = CatMullRom( -( float( n ) - b ) );
			vec4 vecCoeef2 = vec4( f1, f1, f1, f1 );
            nSum = nSum + ( vecData * vecCoeef2 * vecCooef1  );
            nDenom = nDenom + (( vecCoeef2 * vecCooef1 ));
        }
    }
    return nSum / nDenom;
}

vec3 czm_saturation(vec3 rgb, float adjustment)
{
    // Algorithm from Chapter 16 of OpenGL Shading Language
    const vec3 W = vec3(0.2125, 0.7154, 0.0721);
    vec3 intensity = vec3(dot(rgb, W));
    return mix(intensity, rgb, adjustment);
}

vec4 interpolate(sampler2D image, vec2 loc, vec2 res, int interpolation)
{
	switch(interpolation)
	{
		case CATMULL_ROM_INTERP: return BiCubic_Catmull(image, loc, res);
		case BICUBIC_INTERP : return BiCubic_Interp(image, loc, res);
		case BILINEAR_INTERP : return BiLinear_Interp(image, loc, res);
		case BSPLINE_INTERP : BSpline_Interp(image, loc, res);
		default: return texture2D(image, loc);
	}
}

void main (void)  
{
    vec4 color;
    
    if(texturing)
    {
		vec2 sampleLoc = vec2(texCoord[0].s, 1.0 - texCoord[0].t);
		color = interpolate(tex, sampleLoc, texResolutions[0], primaryInterpolationType);
		if(blending)
		{
			vec2 secondSampleLoc = vec2(texCoord[1].s, 1.0 - texCoord[1].t);
			vec4 color2 = interpolate(tex2, secondSampleLoc, texResolutions[1], secondaryInterpolationType);
			color = mix(color2, color, blendValue);
		}
        if(saturation != 1.0)
        {
            color = vec4(czm_saturation(color.rgb, saturation), 0.75);
        }
    }
    else
    {
        color = gl_Color;
    }
    gl_FragColor = color;
} 