﻿/*
    This fragment shader is used for rendering the Glsl Tunnel
*/

uniform vec2 resolution;
uniform float time;
uniform float beat;

//Object A (tunnel)
float oa(vec3 q)
{
 return cos(q.x)+cos(q.y*0.5)+cos(0.2*q.z)+fract(q.y*1.)*.05;
}



//Scene
float o(vec3 q)
{
 return min(oa(q),1.);
}

//Get Normal
vec3 gn(vec3 q)
{
 vec3 f=vec3(.01,0,0);
 return normalize(vec3(o(q+f.xyy),o(q+f.yxy),o(q+f.yyx)));
}

//MainLoop
void main(void)
{
 vec2 p = -1.0 + 2.0 * gl_FragCoord.xy / resolution.xy;
 p.x *= resolution.x/resolution.y;
 vec4 c=vec4(1.0);
 vec3 org=vec3(sin(time)*.5,cos(time*.5)*.25+.25,time + beat * 1.5),dir=normalize(vec3(p.x*1.6,p.y,1.0)),q=org,pp;
 float d=.0;

 //First raymarching
 for(int i=0;i<64;i++)
 {
  d=o(q);
  q+=d*dir;
 }
 pp=q;
 float f=length(q-org)*0.02;

 //Second raymarching (reflection)
 dir=reflect(dir,gn(q));
 q+=dir;
 for(int i=0;i<64;i++)
 {
 d=o(q);
 q+=d*dir;
 }
 c=max(dot(gn(q),vec3(.1,.1,.0)),.0)+vec4(.3,cos(time*.5)*.5+.5,sin(time*.5)*.5+.5,1.)*min(length(q-org)*.04,1.);

 //Ribbon Color
 

 //Final Color
 gl_FragColor = ((c+vec4(f))+(1.-min(pp.y+1.9,1.))*vec4(1.,.8,.7,1.))*min(time*.3,1.);
}