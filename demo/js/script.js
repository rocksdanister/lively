var root = {
  wavecolor: {  
  	r: 51,
    g: 86,
    b: 251
	},
    amplitude: 0.3,
    rainbowSpeed: 0.5,
    rainbow: false, 
};

(function () {

  var pi = Math.PI;
  var pi2 = 2 * Math.PI;

  this.Waves = function (holder, options) {
    var Waves = this;

    Waves.options = extend(options || {}, {
      resize: false,
      rotation: 45,
      waves: 5,
      width: 100,
      hue: [0, 360],
      amplitude: root.amplitude,
      background: true,
      preload: true,
      speed: [0.004, 0.008],
      debug: false,
      fps: false,
    });

    Waves.waves = [];

    Waves.holder = document.querySelector(holder);
    Waves.canvas = document.createElement('canvas');
    Waves.ctx = Waves.canvas.getContext('2d');
    Waves.holder.appendChild(Waves.canvas);

    Waves.hue = Waves.options.hue[0];
    Waves.hueFw = true;
    Waves.stats = new Stats();

    Waves.resize();
    Waves.init(Waves.options.preload);

    if (Waves.options.resize)
      window.addEventListener('resize', function () {
        Waves.resize();
      }, false);

  };

  Waves.prototype.init = function (preload) {
    var Waves = this;
    var options = Waves.options;

    for (var i = 0; i < options.waves; i++)
      Waves.waves[i] = new Wave(Waves);

    if (preload) Waves.preload();
  };

  Waves.prototype.preload = function () {
    var Waves = this;
    var options = Waves.options;

    for (var i = 0; i < options.waves; i++) {
      Waves.updateColor();
      for (var j = 0; j < options.width; j++) {
        Waves.waves[i].update();
      }
    }
  };

  Waves.prototype.render = function () {
    var Waves = this;
    var ctx = Waves.ctx;
    var options = Waves.options;

    Waves.updateColor();
    Waves.clear();

    if (Waves.options.debug) {
      ctx.beginPath();
      ctx.strokeStyle = '#f00';
      ctx.arc(Waves.centerX, Waves.centerY, Waves.radius, 0, pi2);
      ctx.stroke();
    }

    if (Waves.options.background) {
      Waves.background();
    }

    each(Waves.waves, function (wave, i) {
      wave.update();
      wave.draw();
    });
  };

  Waves.prototype.animate = function () {
    var Waves = this;

    Waves.render();
    /*
    if (Waves.options.fps) {
      Waves.stats.log();
      Waves.ctx.font = '12px Arial';
      Waves.ctx.fillStyle = '#fff';
      Waves.ctx.fillText(Waves.stats.fps() + ' FPS', 10, 22);
    }
	*/
    window.requestAnimationFrame(Waves.animate.bind(Waves));
  };

  Waves.prototype.clear = function () {
    var Waves = this;
    Waves.ctx.clearRect(0, 0, Waves.width, Waves.height);
  };

  Waves.prototype.background = function () {
    var Waves = this;
    var ctx = Waves.ctx;

    var gradient = Waves.ctx.createLinearGradient(0, 0, 0, Waves.height);
    gradient.addColorStop(0, '#000');
    gradient.addColorStop(1, Waves.color);

    ctx.fillStyle = gradient;
    ctx.fillRect(0, 0, Waves.width, Waves.height);
  };

  Waves.prototype.resize = function () {
    var Waves = this;
    var width = Waves.holder.offsetWidth;
    var height = Waves.holder.offsetHeight;
    Waves.scale = window.devicePixelRatio || 1;
    Waves.width = width * Waves.scale;
    Waves.height = height * Waves.scale;
    Waves.canvas.width = Waves.width;
    Waves.canvas.height = Waves.height;
    Waves.canvas.style.width = width + 'px';
    Waves.canvas.style.height = height + 'px';
    Waves.radius = Math.sqrt(Math.pow(Waves.width, 2) + Math.pow(Waves.height, 2)) / 2;
    Waves.centerX = Waves.width / 2;
    Waves.centerY = Waves.height / 2;
    //Waves.radius /= 2; // REMOVE FOR FULLSREEN
  };

  Waves.prototype.updateColor = function () {
    var Waves = this;

    Waves.hue += (Waves.hueFw) ? 0.01 : -0.01;

    if (Waves.hue > Waves.options.hue[1] && Waves.hueFw) {
      Waves.hue = Waves.options.hue[1];
      Waves.Waves = false;
    } else if (Waves.hue < Waves.options.hue[0] && !Waves.hueFw) {
      Waves.hue = Waves.options.hue[0];
      Waves.Waves = true;
    }

 	var a = Math.floor(127 * Math.sin(root.rainbowSpeed * Waves.hue + 0) + 128);
    var b = Math.floor(127 * Math.sin(root.rainbowSpeed * Waves.hue + 2) + 128);
    var c = Math.floor(127 * Math.sin(root.rainbowSpeed * Waves.hue + 4) + 128);

	if (root.rainbow) {
		Waves.color = 'rgba(' + a + ',' + b + ',' + c + ', 0.1)';
	} else {
		Waves.color = 'rgba(' + root.wavecolor.r + ',' + root.wavecolor.g + ',' + root.wavecolor.b + ', 0.1)';
	}
  };

  function Wave(Waves) {
    var Wave = this;
    var speed = Waves.options.speed;

    Wave.Waves = Waves;
    Wave.Lines = [];

    Wave.angle = [
      rnd(pi2),
      rnd(pi2),
      rnd(pi2),
      rnd(pi2)
    ];

    Wave.speed = [
      rnd(speed[0], speed[1]) * rnd_sign(),
      rnd(speed[0], speed[1]) * rnd_sign(),
      rnd(speed[0], speed[1]) * rnd_sign(),
      rnd(speed[0], speed[1]) * rnd_sign(),
    ];

    return Wave;
  }

  Wave.prototype.update = function () {
    var Wave = this;
    var Lines = Wave.Lines;
    var color = Wave.Waves.color;

    Lines.push(new Line(Wave, color));

    if (Lines.length > Wave.Waves.options.width) {
      Lines.shift();
    }
  };

  Wave.prototype.draw = function () {
    var Wave = this;
    var Waves = Wave.Waves;

    var ctx = Waves.ctx;
    var radius = Waves.radius;
    var radius3 = radius / 3;
    var x = Waves.centerX;
    var y = Waves.centerY;
    var rotation = dtr(Waves.options.rotation);
    var amplitude = root.amplitude;
    var debug = Waves.options.debug;

    var Lines = Wave.Lines;

    each(Lines, function (line, i) {
      if (debug && i > 0) return;

      var angle = line.angle;

      var x1 = x - radius * Math.cos(angle[0] * amplitude + rotation);
      var y1 = y - radius * Math.sin(angle[0] * amplitude + rotation);
      var x2 = x + radius * Math.cos(angle[3] * amplitude + rotation);
      var y2 = y + radius * Math.sin(angle[3] * amplitude + rotation);
      var cpx1 = x - radius3 * Math.cos(angle[1] * amplitude * 2);
      var cpy1 = y - radius3 * Math.sin(angle[1] * amplitude * 2);
      var cpx2 = x + radius3 * Math.cos(angle[2] * amplitude * 2);
      var cpy2 = y + radius3 * Math.sin(angle[2] * amplitude * 2);

      ctx.strokeStyle = (debug) ? '#fff' : line.color;

      ctx.beginPath();
      ctx.moveTo(x1, y1);
      ctx.bezierCurveTo(cpx1, cpy1, cpx2, cpy2, x2, y2);
      ctx.stroke();

      if (debug) {
        ctx.strokeStyle = '#fff';
        ctx.globalAlpha = 0.3;

        ctx.beginPath();
        ctx.moveTo(x1, y1);
        ctx.lineTo(cpx1, cpy1);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(x2, y2);
        ctx.lineTo(cpx2, cpy2);
        ctx.stroke();

        ctx.globalAlpha = 1;
      }
    });
  };

  function Line(Wave, color) {
    var Line = this;

    var angle = Wave.angle;
    var speed = Wave.speed;

    Line.angle = [
      Math.sin(angle[0] += speed[0]),
      Math.sin(angle[1] += speed[1]),
      Math.sin(angle[2] += speed[2]),
      Math.sin(angle[3] += speed[3])
    ];

    Line.color = color;
  }

  function Stats() {
    this.data = [];
  }

  Stats.prototype.time = function () {
    return (performance || Date)
      .now();
  };

  Stats.prototype.log = function () {
    if (!this.last) {
      this.last = this.time();
      return 0;
    }

    this.new = this.time();
    this.delta = this.new - this.last;
    this.last = this.new;

    this.data.push(this.delta);
    if (this.data.length > 10)
      this.data.shift();
  };

  Stats.prototype.fps = function () {
    var fps = 0;
    each(this.data, function (data, i) {
      fps += data;
    });

    return Math.round(1000 / (fps / this.data.length));
  };

  function each(items, callback) {
    for (var i = 0; i < items.length; i++) {
      callback(items[i], i);
    }
  }

  function extend(options, defaults) {
    for (var key in options)
      if (defaults.hasOwnProperty(key))
        defaults[key] = options[key];
    return defaults;
  }

  function dtr(deg) {
    return deg * pi / 180;
  }

  function rtd(rad) {
    return rad * 180 / pi;
  }

  function diagonal_angle(w, h) {
    var a = Math.atan2(h, w) * 1.27325;
    return a;
  }

  function rnd(a, b) {
    if (arguments.length == 1)
      return Math.random() * a;
    return a + Math.random() * (b - a);
  }

  function rnd_sign() {
    return (Math.random() > 0.5) ? 1 : -1;
  }

})();

var waves = new Waves('#holder', {
  fps: true,
  waves: 3,
  width: 200,
});

waves.animate();

