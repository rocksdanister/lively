const container = document.getElementById("container");
let clock = new THREE.Clock();
const gui = new dat.GUI();
gui.hide();

//custom events
let sceneLoaded = false;
const sceneLoadedEvent = new Event("sceneLoaded");
const sceneChanged = new Event("sceneChanged");

let isDebug = false;
let isPaused = false;
let currentScene = null;
let scene, camera, renderer, material;
let settings = { fps: 24, scale: 1, parallaxVal: 1 };
//required: u_mouse, u_time, u_brightness, u_resolution, u_tex0_resolution,
let shaders = [
  {
    name: "rain",
    uniform: {
      u_tex0: { type: "t" },
      u_time: { value: 0, type: "f" },
      u_blur: { value: false, type: "b" },
      u_intensity: { value: 0.4, type: "f" },
      u_speed: { value: 0.25, type: "f" },
      u_brightness: { value: 0.75, type: "f" },
      u_normal: { value: 0.5, type: "f" },
      u_zoom: { value: 2.61, type: "f" },
      u_panning: { value: false, type: "b" },
      u_post_processing: { value: true, type: "b" },
      u_lightning: { value: false, type: "b" },
      u_mouse: { value: new THREE.Vector4(), type: "v4" },
      u_resolution: { value: new THREE.Vector2(window.innerWidth, window.innerHeight), type: "v2" },
      u_tex0_resolution: { value: new THREE.Vector2(window.innerWidth, window.innerHeight), type: "v2" },
    },
    fragmentShaderPath: "shaders/rain.frag",
    scale: 1,
  },
  {
    name: "snow",
    uniform: {
      u_tex0: { type: "t" },
      u_time: { value: 0, type: "f" },
      u_depth: { value: 1.0, type: "f" },
      u_width: { value: 0.3, type: "f" },
      u_speed: { value: 0.6, type: "f" },
      u_layers: { value: 25, type: "i" },
      u_blur: { value: false, type: "b" },
      u_brightness: { value: 0.75, type: "f" },
      u_post_processing: { value: true, type: "b" },
      u_mouse: { value: new THREE.Vector4(), type: "v4" },
      u_resolution: { value: new THREE.Vector2(window.innerWidth, window.innerHeight), type: "v2" },
      u_tex0_resolution: { value: new THREE.Vector2(window.innerWidth, window.innerHeight), type: "v2" },
    },
    fragmentShaderPath: "shaders/snow.frag",
    scale: 0.75,
  },
  {
    name: "clouds",
    uniform: {
      u_time: { value: 0, type: "f" },
      u_fog: { value: true, type: "b" },
      u_speed: { value: 0.25, type: "f" },
      u_scale: { value: 0.61, type: "f" },
      u_color1: { value: new THREE.Color("#87b0b7"), type: "c" },
      u_fog_color: { value: new THREE.Color("#0f1c1c"), type: "c" },
      u_brightness: { value: 0.75, type: "f" },
      u_mouse: { value: new THREE.Vector4(), type: "v4" },
      u_resolution: { value: new THREE.Vector2(window.innerWidth, window.innerHeight), type: "v2" },
    },
    fragmentShaderPath: "shaders/clouds.frag",
    scale: 0.25,
  },
  {
    name: "synthwave",
    uniform: {
      u_time: { value: 0, type: "f" },
      u_brightness: { value: 0.75, type: "f" },
      u_crt_effect: { value: false, type: "b" },
      u_draw: { value: 1, type: "f" },
      u_sun: { value: 0.5, type: "f" },
      u_plane: { value: 0.7, type: "f" },
      u_mouse: { value: new THREE.Vector4(), type: "v4" },
      u_resolution: { value: new THREE.Vector2(window.innerWidth, window.innerHeight), type: "v2" },
    },
    fragmentShaderPath: "shaders/synthwave.frag",
    scale: 0.75,
  },
  {
    name: "impulse",
    uniform: {
      u_time: { value: 0, type: "f" },
      u_brightness: { value: 0.75, type: "f" },
      u_mouse: { value: new THREE.Vector4(), type: "v4" },
      u_resolution: { value: new THREE.Vector2(window.innerWidth, window.innerHeight), type: "v2" },
    },
    fragmentShaderPath: "shaders/impulse.frag",
    scale: 1,
  },
];
const quad = new THREE.Mesh(new THREE.PlaneGeometry(2, 2, 1, 1));
let videoElement;

let vertexShader = `
varying vec2 vUv;        
void main() {
    vUv = uv;
    gl_Position = vec4( position, 1.0 );    
}
`;

async function init() {
  renderer = new THREE.WebGLRenderer({
    antialias: false,
    preserveDrawingBuffer: false,
  });
  renderer.setSize(window.innerWidth, window.innerHeight);
  renderer.setPixelRatio(settings.scale);
  container.appendChild(renderer.domElement);
  scene = new THREE.Scene();
  camera = new THREE.OrthographicCamera(-1, 1, 1, -1, 0, 1);
  scene.add(quad);

  //caching for textureloader
  //ref: https://threejs.org/docs/#api/en/loaders/Cache
  THREE.Cache.enabled = true;

  //preload default shader texture for transition effect
  shaders[1].uniform.u_tex0_resolution.value = new THREE.Vector2(1920, 1080);
  shaders[1].uniform.u_tex0.value = await new THREE.TextureLoader().loadAsync("media/snow_tree.webp");

  await setScene("rain");
  render(); //since init is async

  window.addEventListener("resize", (e) => resize());

  if (isDebug) {
    debugMenu();
    gui.show();
  } else {
    gui.hide();
  }
}

//Example: setProperty("u_intensity", 0.5);
function setProperty(property, value) {
  try {
    if (material.uniforms[property].type == "v3") {
      var rgb = hexToRgb(value);
      material.uniforms[property].value = new THREE.Vector3(rgb.r, rgb.g, rgb.b);
    } else if (material.uniforms[property].type == "c") material.uniforms[property].value = new THREE.Color(value);
    else material.uniforms[property].value = value;
  } catch (ex) {
    console.log(`Failed to update property ${property}=${value}, ${ex}`);
  }
}

function setTexture(texName, value, isBlur = false) {
  showTransition();

  let ext = getExtension(value);
  disposeVideoElement(videoElement);
  material.uniforms[texName].value?.dispose();

  //check if resolution variable of format "texName_resolution" present.
  let texResolutionName = null;
  for (var key in material.uniforms) {
    if (key.includes(texName + "_resolution")) {
      texResolutionName = key;
      break;
    }
  }

  if (ext == "jpg" || ext == "jpeg" || ext == "png" || ext == "webp") {
    new THREE.TextureLoader().load(value, function (tex) {
      material.uniforms[texName].value = tex;
      if (texResolutionName != null)
        material.uniforms[texResolutionName].value = new THREE.Vector2(tex.image.width, tex.image.height);
    });
  } else if (ext == "webm" || ext == "mp4") {
    videoElement = createVideoElement(value);
    let videoTexture = new THREE.VideoTexture(videoElement);
    videoElement.addEventListener(
      "loadedmetadata",
      function (e) {
        if (texResolutionName != null)
          material.uniforms[texResolutionName].value = new THREE.Vector2(
            videoTexture.image.videoWidth,
            videoTexture.image.videoHeight
          );
      },
      false
    );
    material.uniforms.u_tex0.value = videoTexture;
  }
  material.uniforms.u_blur.value = isBlur;
}

//Pause rendering
function setPause(val) {
  isPaused = val;
}

function setScale(value) {
  if (settings.scale == value) return;

  settings.scale = value;
  renderer.setPixelRatio(settings.scale);
  material.uniforms.u_resolution.value = new THREE.Vector2(
    window.innerWidth * settings.scale,
    window.innerHeight * settings.scale
  );
}

function openFilePicker() {
  document.getElementById("filePicker").click();
}

async function setScene(name, geometry = quad) {
  if (name == currentScene) return;
  currentScene = name;

  showTransition(); //start async transition

  material?.uniforms?.u_tex0?.value?.dispose();
  material?.dispose();
  disposeVideoElement(videoElement);

  switch (name) {
    case "rain":
      {
        material = new THREE.ShaderMaterial({
          uniforms: shaders[0].uniform,
          vertexShader: vertexShader,
          fragmentShader: await (await fetch(shaders[0].fragmentShaderPath)).text(),
        });
        setScale(shaders[0].scale);
        material.uniforms.u_tex0_resolution.value = new THREE.Vector2(1920, 1080);
        material.uniforms.u_tex0.value = await new THREE.TextureLoader().loadAsync("media/rain_mountain.webp");
      }
      break;
    case "snow":
      {
        material = new THREE.ShaderMaterial({
          uniforms: shaders[1].uniform,
          vertexShader: vertexShader,
          fragmentShader: await (await fetch(shaders[1].fragmentShaderPath)).text(),
        });
        setScale(shaders[1].scale);
        material.uniforms.u_tex0_resolution.value = new THREE.Vector2(1920, 1080);
        material.uniforms.u_tex0.value = await new THREE.TextureLoader().loadAsync("media/snow_tree.webp");
      }
      break;
    case "clouds":
      {
        material = new THREE.ShaderMaterial({
          uniforms: shaders[2].uniform,
          vertexShader: vertexShader,
          fragmentShader: await (await fetch(shaders[2].fragmentShaderPath)).text(),
        });
        setScale(shaders[2].scale); //performance
      }
      break;
    case "synthwave":
      {
        material = new THREE.ShaderMaterial({
          uniforms: shaders[3].uniform,
          vertexShader: vertexShader,
          fragmentShader: await (await fetch(shaders[3].fragmentShaderPath)).text(),
        });
        setScale(shaders[3].scale);
      }
      break;
    case "impulse":
      {
        material = new THREE.ShaderMaterial({
          uniforms: shaders[4].uniform,
          vertexShader: vertexShader,
          fragmentShader: await (await fetch(shaders[4].fragmentShaderPath)).text(),
        });
        setScale(shaders[4].scale);
      }
      break;
  }
  geometry.material = material;
  resize(); //update view

  if (!sceneLoaded) {
    sceneLoaded = true;
    document.dispatchEvent(sceneLoadedEvent);
    setMouse();
  }
  document.dispatchEvent(sceneChanged);
}

function setMouse() {
  //mouse drag
  let startX,
    startY,
    delta = 10,
    isDrag = false;

  //mouse paralax, relative function to the location of the mouse first on the page
  let originX = -1,
    originY = -1;

  this.onmousedown = mouseDown;
  this.onmousemove = mouseMove;
  this.onmouseup = mouseUp;
  function mouseUp(e) {
    isDrag = false;
  }
  function mouseDown(e) {
    if (e.target.id == "page-home") {
      isDrag = true;
      startX = e.pageX;
      startY = e.pageY;
    }
  }
  function mouseMove(e) {
    //parallax
    if (settings.parallaxVal > 0) {
      if (originX == -1 && originY == -1) {
        originX = e.pageX;
        originY = e.pageY;
      }

      //relative parallax by changing window.innerWidth and window.innerHeight to originX and originY
      const x = (originX - e.pageX * settings.parallaxVal) / 90;
      const y = (originY - e.pageY * settings.parallaxVal) / 90;

      container.style.transform = `translateX(${x}px) translateY(${y}px) scale(1.09)`;
    }

    //drag
    if (!(Math.abs(e.pageX - startX) < delta && Math.abs(e.pageY - startY) < delta) && isDrag) {
      //mouse pixel coords. xy: current (if MLB down), zw: click
      material.uniforms.u_mouse.value.x = e.pageX * settings.scale;
      material.uniforms.u_mouse.value.y = e.pageY * settings.scale;
      material.uniforms.u_mouse.value.z = 0;
      material.uniforms.u_mouse.value.w = 0;
    }
  }
}

// function setMouseParallax() {
//   this.onmousemove = mouseMove;
//   //makes the paralax be a relative function to the location of the mouse first on the page
//   let originX = -1,
//     originY = -1;
//   function mouseMove(event) {
//     if (settings.parallaxVal == 0) return;

//     if (originX == -1 && originY == -1) {
//       originX = event.pageX;
//       originY = event.pageY;
//     }

//     //relative parallax by changing window.innerWidth and window.innerHeight to originX and originY
//     const x = (originX - event.pageX * settings.parallaxVal) / 90;
//     const y = (originY - event.pageY * settings.parallaxVal) / 90;

//     container.style.transform = `translateX(${x}px) translateY(${y}px) scale(1.09)`;
//   }
// }

// function setMouseDrag() {
//   let startX,
//     startY,
//     delta = 10,
//     isDrag = false;
//   this.onmousedown = mouseDown;
//   this.onmousemove = mouseMove;
//   this.onmouseup = mouseUp;
//   function mouseUp(e) {
//     isDrag = false;
//   }
//   function mouseDown(e) {
//     if (e.target.id == "page-home") {
//       isDrag = true;
//       startX = e.pageX;
//       startY = e.pageY;
//     }
//   }
//   function mouseMove(e) {
//     if ((Math.abs(e.pageX - startX) < delta && Math.abs(e.pageY - startY) < delta) || !isDrag) {
//       return;
//     }

//     //mouse pixel coords. xy: current (if MLB down), zw: click
//     material.uniforms.u_mouse.value.x = e.pageX * settings.scale;
//     material.uniforms.u_mouse.value.y = e.pageY * settings.scale;
//     material.uniforms.u_mouse.value.z = 0;
//     material.uniforms.u_mouse.value.w = 0;
//   }
// }

// function setMouseMove() {
//   this.onmousemove = mouseMove;
//   function mouseMove(e) {
//     if (e.target.id != "page-home") {
//       return;
//     }

//     //mouse pixel coords. xy: current (if MLB down), zw: click
//     material.uniforms.u_mouse.value.x = e.pageX * settings.scale;
//     material.uniforms.u_mouse.value.y = e.pageY * settings.scale;
//     material.uniforms.u_mouse.value.z = 0;
//     material.uniforms.u_mouse.value.w = 0;
//   }
// }

// function resetMouse() {
//   this.onmousedown = this.onmousemove = this.onmouseup = null;
// }

async function showTransition() {
  if (material == null) return;

  renderer.render(scene, camera); //WebGLRenderer.preserveDrawingBuffer is false.
  const quad = new THREE.Mesh(new THREE.PlaneGeometry(2, 2, 1, 1));
  let screenShot = renderer.domElement.toDataURL();
  const texture = new THREE.TextureLoader().load(screenShot);
  quad.material = new THREE.MeshBasicMaterial({ map: texture, transparent: true, opacity: 1.0 });
  scene.add(quad);

  for (let val = 1; val >= 0; val -= 0.1) {
    quad.material.opacity = val;
    await new Promise((r) => setTimeout(r, 75));
  }

  texture.dispose();
  scene.remove(quad);
  URL.revokeObjectURL(screenShot);
}

function resize() {
  renderer.setSize(window.innerWidth, window.innerHeight);
  material.uniforms.u_resolution.value = new THREE.Vector2(
    window.innerWidth * settings.scale,
    window.innerHeight * settings.scale
  );
}

function render() {
  setTimeout(function () {
    requestAnimationFrame(render);
  }, 1000 / settings.fps);

  //reset every 6hr
  if (clock.getElapsedTime() > 21600) clock = new THREE.Clock();
  material.uniforms.u_time.value = clock.getElapsedTime();

  if (!isPaused) renderer.render(scene, camera);
}

init();

document.getElementById("filePicker").addEventListener("change", function () {
  if (this.files[0] === undefined) return;
  let file = this.files[0];
  if (file.type == "image/jpg" || file.type == "image/jpeg" || file.type == "image/png") {
    disposeVideoElement(videoElement);
    material.uniforms.u_tex0.value?.dispose();

    new THREE.TextureLoader().load(URL.createObjectURL(file), function (tex) {
      material.uniforms.u_tex0.value = tex;
      material.uniforms.u_tex0_resolution.value = new THREE.Vector2(tex.image.width, tex.image.height);
    });
  } else if (file.type == "video/mp4" || file.type == "video/webm") {
    disposeVideoElement(videoElement);
    material.uniforms.u_tex0.value?.dispose();

    videoElement = createVideoElement(URL.createObjectURL(file));
    let videoTexture = new THREE.VideoTexture(videoElement);
    videoElement.addEventListener(
      "loadedmetadata",
      function (e) {
        material.uniforms.u_tex0_resolution.value = new THREE.Vector2(
          videoTexture.image.videoWidth,
          videoTexture.image.videoHeight
        );
      },
      false
    );
    material.uniforms.u_tex0.value = videoTexture;
  }
  //always blur user content
  material.uniforms.u_blur.value = true;

  //https://stackoverflow.com/questions/4109276/how-to-detect-input-type-file-change-for-the-same-file
  this.value = null;
});

//helpers
function hexToRgb(hex) {
  var result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
  return result
    ? {
        r: parseInt(result[1], 16),
        g: parseInt(result[2], 16),
        b: parseInt(result[3], 16),
      }
    : null;
}

function getExtension(filePath) {
  return filePath.substring(filePath.lastIndexOf(".") + 1, filePath.length) || filePath;
}

function hasClass(element, className) {
  return (" " + element.className + " ").indexOf(" " + className + " ") > -1;
}

function createVideoElement(src) {
  let htmlVideo = document.createElement("video");
  htmlVideo.src = src;
  htmlVideo.muted = true;
  htmlVideo.loop = true;
  htmlVideo.play();
  return htmlVideo;
}

//ref: https://stackoverflow.com/questions/3258587/how-to-properly-unload-destroy-a-video-element
function disposeVideoElement(video) {
  if (video != null && video.hasAttribute("src")) {
    video.pause();
    video.removeAttribute("src"); // empty source
    video.load();
  }
}

//datgui threejs color menu
function addColor(ui, property, displayName) {
  var conf = { color: property.value.getHex() };
  ui.addColor(conf, "color")
    .onChange(function (val) {
      property.value = new THREE.Color(val);
    })
    .name(displayName);
}

//debug
function debugMenu() {
  try {
    debugScale();
    //debugSnow();
    //debugSynthwave();
    //debugCloud();
  } catch (ex) {
    console.log(ex);
  }
}

function debugScale() {
  gui
    .add(settings, "scale", 0.1, 2, 0.01)
    .name("Display Scale")
    .onChange(function () {
      setScale(settings.scale);
    });
}

function debugSnow() {
  gui.add(material.uniforms.u_layers, "value", 0, 200, 1).name("Layers");
  gui.add(material.uniforms.u_depth, "value", 0, 10, 0.01).name("Depth");
  gui.add(material.uniforms.u_width, "value", 0, 10, 0.01).name("Width");
  gui.add(material.uniforms.u_speed, "value", 0, 10, 0.01).name("Speed");
}

function debugSynthwave() {
  gui.add(material.uniforms.u_sun, "value", 0, 1, 0.01).name("Sun");
  gui.add(material.uniforms.u_draw, "value", 0, 2, 0.01).name("Draw");
  gui.add(material.uniforms.u_plane, "value", 0, 1, 0.01).name("Plane");
  gui.add(material.uniforms.u_crt_effect, "value", 0, 2, 0.01).name("CRT");
}

function debugCloud() {
  gui.add(material.uniforms.u_fog, "value").name("Fog");
  gui.add(material.uniforms.u_scale, "value", 0, 2, 0.01).name("Size1");
  addColor(gui, material.uniforms.u_color1, "Density color");
}
