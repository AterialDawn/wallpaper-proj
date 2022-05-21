/*
    This fragment shader is used in rendering the final texture to the screen from Render-To-Texture
*/

uniform sampler2D tex;
uniform float opacity;

varying vec4 vertColor;
varying vec2 texUV;

void main (void)  
{
    vec4 color = texture2D(tex, texUV) * vertColor;
    gl_FragColor = color * opacity;
} 