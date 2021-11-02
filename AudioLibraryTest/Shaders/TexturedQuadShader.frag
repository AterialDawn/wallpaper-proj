/*
    This fragment shader is used in rendering the final texture to the screen from Render-To-Texture
*/

uniform sampler2D tex;

void main (void)  
{
    vec4 color = texture2D(tex, gl_TexCoord[0].st);
    gl_FragColor = color;
} 