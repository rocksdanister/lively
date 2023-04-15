//CC0 - Impulse backdrop by mrange
//https://www.shadertoy.com/view/wl2yRm

#ifdef GL_ES
precision highp float;
#endif

uniform float u_time;
uniform vec2 u_resolution;
uniform float u_brightness;

#define PI               3.141592654
#define TAU             (2.0*PI)
#define SCA(a)          vec2(sin(a), cos(a))
 
#define LESS(a,b,c)     mix(a,b,step(0.,c))
#define SABS(x,k)       LESS((.5/k)*x*x+k*.5,abs(x),abs(x)-k)

const vec2 sca0 = SCA(0.0);

float hash(in vec2 co) {
  return fract(sin(dot(co.xy ,vec2(12.9898,58.233))) * 13758.5453);
}

float psin(float a) {
  return 0.5 + 0.5*sin(a);
}

float circle(vec2 p, float r) {
  return length(p) - r;
}

float box(vec2 p, vec2 b) {
  vec2 d = abs(p)-b;
  return length(max(d,0.0)) + min(max(d.x,d.y),0.0);
}

float horseshoe(vec2 p, vec2 c, float r, vec2 w) {
  p.x = abs(p.x);
  float l = length(p);
  p = mat2(-c.x, c.y, c.y, c.x)*p;
  p = vec2((p.y>0.0)?p.x:l*sign(-c.x),(p.x>0.0)?p.y:l);
  p = vec2(p.x,abs(p.y-r))-w;
  return length(max(p,0.0)) + min(0.0,max(p.x,p.y));
}

void rot(inout vec2 p, float a) {
  float c = cos(a);
  float s = sin(a);
  p = vec2(c*p.x + s*p.y, -s*p.x + c*p.y);
}

float onoise(vec2 x) {
  x *= 0.5;
  float a = sin(x.x);
  float b = sin(x.y);
  float c = mix(a, b, psin(TAU/1.25*(a*b+a+b)));
  
  return c;
}

float cell0(vec2 p) {
  float d1 = length(p+vec2(0.5)) - 0.5;
  float d2 = length(p-vec2(0.5)) - 0.5;
  float d = min(d1, d2);
  return d;
}

float cell1(vec2 p) {
  float d1 = abs(p.x);
  float d2 = abs(p.y);
  float d3 = length(p) - 0.25;
  float d = min(d1, d2);
  d = min(d, d3);
  return d;
}

float tnoise(vec2 p) {
  p *= 0.125;
  p += 0.5;
  vec2 nn = floor(p);
  vec2 pp = fract(p) - 0.5;
  float r = hash(nn);
  vec2 rot = -1.0 + 2.0*vec2(step(fract(r*13.0), 0.5), step(fract(r*23.0), 0.5));
  pp *= rot;
  const float w = 0.1;
  float d = 1E6;
  if(r < 0.75) {
    d = cell0(pp);
  } else  {
    d = cell1(pp);
  }
  d = abs(d) - w;
  float h = smoothstep(0.0, w, -d);  
  return h;
}

float vnoise(vec2 x) {
  vec2 i = floor(x);
  vec2 w = fract(x);
    
#if 1
  // quintic interpolation
  vec2 u = w*w*w*(w*(w*6.0-15.0)+10.0);
#else
  // cubic interpolation
  vec2 u = w*w*(3.0-2.0*w);
#endif    

  float a = hash(i+vec2(0.0,0.0));
  float b = hash(i+vec2(1.0,0.0));
  float c = hash(i+vec2(0.0,1.0));
  float d = hash(i+vec2(1.0,1.0));
    
  float k0 =   a;
  float k1 =   b - a;
  float k2 =   c - a;
  float k3 =   d - c + a - b;

  float aa = mix(a, b, u.x);
  float bb = mix(c, d, u.x);
  float cc = mix(aa, bb, u.y);
  
  return k0 + k1*u.x + k2*u.y + k3*u.x*u.y;
}

float fbm(vec2 p) {
  const int mid = 3;
  const int mx = 7;  
  const float aa = 0.4;
  const float pp = 2.3;
  const vec2 oo = -vec2(1.23, 1.5);
  const float rr = 1.2;
  
  vec2 op = p;
  
  float h = 0.0;
  float d = 0.0;
  float a = 1.0;
  
  for (int i = 0; i < mid; ++i) {
    h += a*onoise(p);
    d += (a);
    a *= aa;
    p += oo;
    p *= pp;
    rot(p, rr);
  }

  for (int i = mid; i < mx; ++i) {
    h += a*tnoise(p);
    d += (a);
    a *= aa;
    p += oo;
    p *= pp;
    rot(p, rr);
  }
  
  return 0.5*mix(-1.0, 1.0, smoothstep(0.0, 1.2, (vnoise(0.50*op))))*(h/d);
}

float warp(vec2 p) {
  vec2 v = vec2(fbm(p), fbm(p+10.7*vec2(1.0, 1.0)));
  rot(v, 1.0+u_time*0.125);
  return mix(0., 1.0, v.x - v.y);
}

float height(vec2 p) {
  float a = 0.005*u_time;
  p += 5.0*vec2(cos(a), sin(sqrt(0.5)*a));
  p *= 2.0;
  p += 13.0;
  float h = warp(p);
  float rs = 3.0;
//  return 0.75*tanh(rs*h)/rs;
  return 0.75*h;
}

vec3 normal(vec2 p) {
  // As suggested by IQ, thanks!
  vec2 eps = -vec2(2.0/u_resolution.y, 0.0);
  
  vec3 n;
  
  n.x = height(p + eps.xy) - height(p - eps.xy);
  n.y = 2.0*eps.x;
  n.z = height(p + eps.yx) - height(p - eps.yx);
  
  
  return normalize(n);
}

vec3 postProcess(vec3 col, vec2 q)  {
  col=pow(clamp(col,0.0,1.0),vec3(0.75)); 
  col=col*0.6+0.4*col*col*(3.0-2.0*col);  // contrast
  col=mix(col, vec3(dot(col, vec3(0.33))), -0.4);  // saturation
  col*=0.5+0.5*pow(19.0*q.x*q.y*(1.0-q.x)*(1.0-q.y),0.7);  // vigneting
  return col;
}

void main()  {
  vec2 q = gl_FragCoord.xy / u_resolution.xy;
  vec2 p = -1. + 2. * q;
  p.x *= u_resolution.x/u_resolution.y;
 
  const vec3 lp1 = vec3(0.8, -0.75, 0.8);
  const vec3 lp2 = vec3(-0., -1.5, -1.0);

  float aa = 10.0/u_resolution.y;
  
  vec3 col = vec3(0.0);
  float h = height(p);
  vec3 pp = vec3(p.x, h, p.y);
  vec3 ld1 = normalize(lp1 - pp);
  vec3 ld2 = normalize(lp2 - pp);
 
  vec3 n = normal(p);
  float diff1 = max(dot(ld1, n), 0.0);
  float diff2 = max(dot(ld2, n), 0.0);
 
  const vec3 baseCol1 = vec3(0.6, 0.8, 1.0);
  const vec3 baseCol2 = sqrt(baseCol1.zyx);
  
  col += baseCol1*pow(diff1, 16.0);
  col += 0.1*baseCol1*pow(diff1, 4.0);
  col += 0.15*baseCol2*pow(diff2,8.0);
  col += 0.015*baseCol2*pow(diff2, 2.0);

  col = clamp(col, 0.0, 1.0);
  col = mix(0.05*baseCol1, col, 1.0 - (1.0 - 0.5*diff1)*exp(- 2.0*smoothstep(-.1, 0.05, (h))));
  
  float shd = pow(psin(-0.25*u_time+(p.x-p.y)*1.5), 4.0);
  col = clamp(col, 0.0, 1.0);

  col = postProcess(col, q);

  const float fadeIn = 3.0;
  col *= smoothstep(0.0, fadeIn*fadeIn, u_time*u_time);
  
  gl_FragColor = vec4(col * u_brightness, 1.0);
}