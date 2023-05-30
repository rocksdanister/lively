//CC0 - Synthwave canyon by mrange
//https://www.shadertoy.com/view/slcXW8

#ifdef GL_ES
precision highp float;
#endif

#define PI 3.141592654

uniform float u_time;
uniform vec2 u_resolution;
uniform float u_brightness;
uniform float u_sun;
uniform float u_plane;
uniform float u_draw;
uniform bool u_crt_effect;
uniform vec4 u_mouse;

float time() {
    return 1000. + u_time / 4.;
}

// License: WTFPL, author: sam hocevar, found: https://stackoverflow.com/a/17897228/418488
const vec4 hsv2rgb_K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
vec3 hsv2rgb(vec3 c) {
    vec3 p = abs(fract(c.xxx + hsv2rgb_K.xyz) * 6.0 - hsv2rgb_K.www);
    return c.z * mix(hsv2rgb_K.xxx, clamp(p - hsv2rgb_K.xxx, 0.0, 1.0), c.y);
}
// License: WTFPL, author: sam hocevar, found: https://stackoverflow.com/a/17897228/418488
//  Macro version of above to enable compile-time constants
#define HSV2RGB(c)  (c.z * mix(hsv2rgb_K.xxx, clamp(abs(fract(c.xxx + hsv2rgb_K.xyz) * 6.0 - hsv2rgb_K.www) - hsv2rgb_K.xxx, 0.0, 1.0), c.y))

// License: Unknown, author: Unknown, found: don't remember
vec4 alphaBlend(vec4 back, vec4 front) {
    float w = front.w + back.w * (1.0 - front.w);
    vec3 xyz = (front.xyz * front.w + back.xyz * back.w * (1.0 - front.w)) / w;
    return w > 0.0 ? vec4(xyz, w) : vec4(0.0);
}

// License: Unknown, author: Unknown, found: don't remember
vec3 alphaBlend(vec3 back, vec4 front) {
    return mix(back, front.xyz, front.w);
}

// License: MIT OR CC-BY-NC-4.0, author: mercury, found: https://mercury.sexy/hg_sdf/
float mod1(inout float p, float size) {
    float halfsize = size * 0.5;
    float c = floor((p + halfsize) / size);
    p = mod(p + halfsize, size) - halfsize;
    return c;
}

float planex(vec2 p, float w) {
    return abs(p.y) - w;
}

float circle(vec2 p, float r) {
    return length(p) - r;
}

// License: MIT, author: Inigo Quilez, found: https://iquilezles.org/articles/smin
float pmin(float a, float b, float k) {
    float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
    return mix(b, a, h) - k * h * (1.0 - h);
}

// License: CC0, author: Mårten Rånge, found: https://github.com/mrange/glsl-snippets
float pmax(float a, float b, float k) {
    return -pmin(-a, -b, k);
}

// License: Unknown, author: Unknown, found: don't remember
float tanh_approx(float x) {
  //  Found this somewhere on the interwebs
  //  return tanh(x);
    float x2 = x * x;
    return clamp(x * (27.0 + x2) / (27.0 + 9.0 * x2), -1.0, 1.0);
}

// License: Unknown, author: Unknown, found: don't remember
float hash(float co) {
    return fract(sin(co * 12.9898) * 13758.5453);
}

// License: Unknown, author: Unknown, found: don't remember
float hash(vec2 p) {
    float a = dot(p, vec2(127.1, 311.7));
    return fract(sin(a) * 43758.5453123);
}

// License: MIT, author: Inigo Quilez, found: https://iquilezles.org/www/index.htm
vec3 postProcess(vec3 col, vec2 q) {
    if(u_crt_effect) {
        col *= 1.5 * smoothstep(-2.0, 1.0, sin(0.5 * PI * q.y * u_resolution.y));
    } else {
        col = clamp(col, 0.0, 1.0);
        col = pow(col, vec3(1.0 / 2.2));
        col = col * 0.6 + 0.4 * col * col * (3.0 - 2.0 * col);
        col = mix(col, vec3(dot(col, vec3(0.33))), -0.4);
        col *= 0.5 + 0.5 * pow(19.0 * q.x * q.y * (1.0 - q.x) * (1.0 - q.y), 0.7);
    }
    return col;
}

// Value noise: https://iquilezles.org/articles/morenoise
float vnoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);

    vec2 u = f * f * (3.0 - 2.0 * f);
//  vec2 u = f;

    float a = hash(i + vec2(0.0, 0.0));
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));

    float m0 = mix(a, b, u.x);
    float m1 = mix(c, d, u.x);
    float m2 = mix(m0, m1, u.y);

    return m2;
}

// FBM: https://iquilezles.org/articles/fbm
float fbm(vec2 p) {
    const float aa = 0.35;
    const float pp = 2.2 - 0.4;

    float sum = 0.0;
    float a = 1.0;

    for(int i = 0; i < 3; ++i) {
        sum += a * vnoise(p);
        a *= aa;
        p *= pp;
    }

    return sum;
}

float height(vec2 p) {
    return fbm(p) * smoothstep(0.0, 1.25 + 0.25 * sin(0.5 * p.y), abs(p.x)) - 0.35;
}

vec3 offset(float z) {
    float a = z;
    vec2 p = -0.05 * (vec2(cos(a), sin(a * sqrt(2.0))) + vec2(cos(a * sqrt(0.75)), sin(a * sqrt(0.5))));
    return vec3(p, z);
}

vec3 doffset(float z) {
    float eps = 0.1;
    return 0.5 * (offset(z + eps) - offset(z - eps)) / eps;
}

vec3 ddoffset(float z) {
    float eps = 0.1;
    return 0.5 * (doffset(z + eps) - doffset(z - eps)) / eps;
}

vec4 plane(vec3 ro, vec3 rd, vec3 pp, vec3 off, float aa, float n) {
    float h = hash(n);
    float s = mix(0.05, 0.25, h);

    vec3 hn;
    vec2 p = (pp - off * 2.0 * vec3(1.0, 1.0, 0.0)).xy;

    float he = height(vec2(p.x, pp.z));

    float d = p.y - he;
    float t = smoothstep(aa, -aa, d);

    vec3 hsv = vec3(fract(u_plane + 0.125 * sin(0.6 * pp.z)), 0.5, smoothstep(aa, -aa, abs(d) - aa));
    float g = exp(-90. * max(abs(d), 0.0));
    hsv.z += g;
    hsv.z += (he * he - pp.y - 0.125) * 0.5;
    vec3 col = hsv2rgb(hsv);

    return vec4(col, tanh_approx(t + g));
}

float sun(vec2 p) {
    const float ch = 0.0125;
    vec2 sp = p;
    vec2 cp = p;
    mod1(cp.y, ch * 6.0);
    float d0 = circle(sp, 0.5);
    float d1 = planex(cp, ch);
    float d2 = p.y + ch * 3.0;
    float d = d0;
    d = pmax(d, -max(d1, d2), ch * 2.0);
    return d;
}

float df(vec2 p) {
    const vec2 off = vec2(0.0, -10.0 + 0.5);
    const vec2 coff = vec2(0);
    const float si = 5.0;
    const float sc = 25.0;
    float ds = sun(p / sc) * sc;
    float d = ds;
    return d;
}

vec3 skyColor(vec3 ro, vec3 rd) {
    float aa = 2.0 / u_resolution.y;

    vec2 p = rd.xy * 2.0;
    p.y -= 0.25;
    vec3 sunCol = mix(vec3(1.0, 1.0, 0.0), vec3(1.0, 0.0, u_sun), clamp((0.85 - p.y) * 0.75, 0.0, 1.0));
    vec3 glareCol = sqrt(sunCol);
    float ss = smoothstep(-1.05, 0.0, p.y);
    vec3 glow = mix(vec3(1.0, 0.7, 0.6).zyx, glareCol, ss);

    float s = 15.0;
    float d = df(p * s) / s;
    float db = abs(d) - 0.0025;

    vec3 col = vec3(1.0, 0.0, 1.0) * 0.125;
    vec3 corona = 0.65 * glow * exp(-2.5 * d) * ss;
    col += corona;
    col = mix(col, sunCol * ss, smoothstep(-aa, aa, -d));
    col = mix(col, glow * 1.55, smoothstep(-aa, aa, -db));

    return col;
}

vec3 color(vec3 ww, vec3 uu, vec3 vv, vec3 ro, vec2 p) {
    float lp = length(p);
    vec2 np = p + 1.0 / u_resolution.xy;
//  float rdd = (2.0-1.0*tanh_approx(lp));  // Playing around with rdd can give interesting distortions
    float rdd = 2.0;
    vec3 rd = normalize(p.x * uu + p.y * vv + rdd * ww);
    vec3 nrd = normalize(np.x * uu + np.y * vv + rdd * ww);

    const float planeDist = 1.0 - 0.5;
    float furthest = 24. * u_draw;
    float fadeFrom = max(furthest - 2., 0.);

    float fadeDist = planeDist * float(furthest - fadeFrom);
    float nz = floor(ro.z / planeDist);

    vec3 skyCol = skyColor(ro, rd);

    vec4 acol = vec4(0.0);
    const float cutOff = 0.95;
    bool cutOut = false;

  // Steps from nearest to furthest plane and accumulates the color 
    for(float i = 1.; i <= furthest; ++i) {
        float pz = planeDist * nz + planeDist * float(i);

        float pd = (pz - ro.z) / rd.z;

        vec3 pp = ro + rd * pd;

        if(pp.y < 1.25 && pd > 0.0 && acol.w < cutOff) {
            vec3 npp = ro + nrd * pd;

            float aa = 3.0 * length(pp - npp);

            vec3 off = offset(pp.z);

            vec4 pcol = plane(ro, rd, pp, off, aa, nz + float(i));

            float nz = pp.z - ro.z;
            float fadeIn = smoothstep(planeDist * float(furthest), planeDist * float(fadeFrom), nz);
            float fadeOut = smoothstep(0.0, planeDist * 0.1, nz);
            pcol.xyz = mix(skyCol, pcol.xyz, fadeIn);
            pcol.w *= fadeOut;
            pcol = clamp(pcol, 0.0, 1.0);

            acol = alphaBlend(pcol, acol);
        } else {
            cutOut = true;
            acol.w = acol.w > cutOff ? 1.0 : acol.w;
            break;
        }

    }

    vec3 col = alphaBlend(skyCol, acol);
// To debug cutouts due to transparency  
//  col += cutOut ? vec3(1.0, -1.0, 0.0) : vec3(0.0);
    return col;
}

vec3 effect(vec2 p, vec2 q) {
    float tm = time() * 0.25;
    vec3 ro = offset(tm);
    vec3 dro = doffset(tm);
    vec3 ddro = ddoffset(tm);

    vec3 ww = normalize(dro);
    vec3 uu = normalize(cross(normalize(vec3(0.0, 1.0, 0.0) + ddro), ww));
    vec3 vv = normalize(cross(ww, uu));

    vec3 col = color(ww, uu, vv, ro, p);

    return col;
}

void main() {
    vec2 q = gl_FragCoord.xy / u_resolution.xy;
    vec2 p = -1. + 2. * q;
    p.x *= u_resolution.x / u_resolution.y;
    vec3 col = vec3(0.0);
    vec2 M = u_mouse.xy / u_resolution.xy;
    float zoom = M.x + M.y > 0. ? M.x + M.y : 1.;
    col = effect(p * zoom, q);
    col *= smoothstep(0.0, 4.0, time());
    col = postProcess(col, q);
    gl_FragColor = vec4(col * u_brightness, 1.0);
}