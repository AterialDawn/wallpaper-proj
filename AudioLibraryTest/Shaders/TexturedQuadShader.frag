/*
    This fragment shader is used in rendering the final texture to the screen from Render-To-Texture
*/

uniform sampler2D tex;
uniform float opacity;

void main (void)  
{
    vec4 color = texture2D(tex, gl_TexCoord[0].st);
    color.a *= opacity;
    gl_FragColor = color;
} 