/* Redesigning the spider elements using SVG for a premium, integrated look */

const spiderHeadSVG = `
<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">
    <!-- Spider Body/Head -->
    <defs>
        <radialGradient id="headGrad" cx="50%" cy="50%" r="50%">
            <stop offset="0%" style="stop-color:#222" />
            <stop offset="100%" style="stop-color:#000" />
        </radialGradient>
        <filter id="eyeGlow">
            <feGaussianBlur stdDeviation="2" result="blur" />
            <feComposite in="SourceGraphic" in2="blur" operator="over" />
        </filter>
    </defs>
    <ellipse cx="50" cy="50" rx="35" ry="40" fill="url(#headGrad)" stroke="#111" stroke-width="1" />
    <!-- Eyes -->
    <circle cx="40" cy="40" r="4" fill="#ff6600" filter="url(#eyeGlow)" />
    <circle cx="60" cy="40" r="4" fill="#ff6600" filter="url(#eyeGlow)" />
    <circle cx="45" cy="30" r="2" fill="#ff3300" filter="url(#eyeGlow)" />
    <circle cx="55" cy="30" r="2" fill="#ff3300" filter="url(#eyeGlow)" />
    <!-- Mandibles -->
    <path d="M40 70 Q45 85 50 75 Q55 85 60 70" fill="none" stroke="#222" stroke-width="3" />
</svg>
`;

const legSVG = (length, width, color) => `
<svg viewBox="0 0 20 ${length}" xmlns="http://www.w3.org/2000/svg">
    <path d="M10 0 L10 ${length}" stroke="${color}" stroke-width="${width}" stroke-linecap="round" />
    <path d="M10 0 L5 ${length/3} L10 ${length/1.5} L15 ${length}" fill="none" stroke="${color}" stroke-width="${width/2}" opacity="0.5" />
</svg>
`;
