// https://www.shadertoy.com/view/ldsGDn 
// Copyright (c) 2013 Andrew Baldwin (twitter: baldand, www: http://thndl.com)
// License = Attribution-NonCommercial-ShareAlike (http://creativecommons.org/licenses/by-nc-sa/3.0/deed.en_US)

#ifdef GL_ES
precision highp float;
#endif

varying vec2 vUv;
uniform bool u_blur;
uniform float u_time;
uniform vec4 u_mouse;
uniform vec2 u_resolution;
uniform sampler2D u_tex0;
uniform vec2 u_tex0_resolution;
uniform int u_layers;
uniform float u_depth;
uniform float u_width;
uniform float u_speed;
uniform bool u_post_processing;
uniform float u_brightness;

// #ifdef LIGHT_SNOW
// 	#define LAYERS 50
// 	#define DEPTH .5
// 	#define WIDTH .3
// 	#define SPEED .6
// #else // BLIZZARD
// 	#define LAYERS 200
// 	#define DEPTH .1
// 	#define WIDTH .8
// 	#define SPEED 1.5
// #endif

float rand(vec2 co) {
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

void main() {
    const mat3 p = mat3(13.323122, 23.5112, 21.71123, 21.1212, 28.7312, 11.9312, 21.8112, 14.7212, 61.3934);
    vec2 uv = u_mouse.xy / u_resolution.xy + vec2(1., u_resolution.y / u_resolution.x) * gl_FragCoord.xy / u_resolution.xy;
    vec3 acc = vec3(0.0);
    float dof = 5. * sin(u_time * .1);
    for(int i = 0; i < u_layers; i++) {
        float fi = float(i);
        vec2 q = uv * (1. + fi * u_depth);
        q += vec2(q.y * (u_width * mod(fi * 7.238917, 1.) - u_width * .5), u_speed * u_time / (1. + fi * u_depth * .03));
        vec3 n = vec3(floor(q), 31.189 + fi);
        vec3 m = floor(n) * .00001 + fract(n);
        vec3 mp = (31415.9 + m) / fract(p * m);
        vec3 r = fract(mp);
        vec2 s = abs(mod(q, 1.) - .5 + .9 * r.xy - .45);
        s += .01 * abs(2. * fract(10. * q.yx) - 1.);
        float d = .6 * max(s.x - s.y, s.x + s.y) + max(s.x, s.y) - .01;
        float edge = .005 + .05 * min(.5 * abs(fi - 5. - dof), 1.);
        acc += vec3(smoothstep(edge, -edge, d) * (r.x / (1. + .02 * fi * u_depth)));
    }

    //uniform texture scaling
    vec2 UV = gl_FragCoord.xy / u_resolution.xy;
    float screenAspect = u_resolution.x / u_resolution.y;
    float textureAspect = u_tex0_resolution.x / u_tex0_resolution.y;
    float scaleX = 1., scaleY = 1.;
    if(textureAspect > screenAspect)
        scaleX = screenAspect / textureAspect;
    else
        scaleY = textureAspect / screenAspect;
    UV = vec2(scaleX, scaleY) * (UV - 0.5) + 0.5;
    float blurIntensity = u_blur ? 0.5/100. : 0.;
    vec3 col = texture2D(u_tex0, UV + blurIntensity * (rand(UV) - 0.5)).rgb;

    if(u_post_processing) {
        col *= mix(vec3(1.), vec3(.8, .9, 1.3), 0.5); //subtle color shift
    }

    gl_FragColor = vec4((vec3(acc) + col) * u_brightness, 1.0);
}