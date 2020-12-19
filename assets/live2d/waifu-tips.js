/*
 * Live2D Widget
 * https://github.com/stevenjoezhang/live2d-widget
 */
let messageG;

function loadWidget(config) {
	let { waifuPath, apiPath, cdnPath } = config;
	let useCDN = false, modelList;
	if (typeof cdnPath === "string") {
		useCDN = true;
		if (!cdnPath.endsWith("/")) cdnPath += "/";
	} else if (typeof apiPath === "string") {
		if (!apiPath.endsWith("/")) apiPath += "/";
	} else {
		console.error("Invalid initWidget argument!");
		return;
	}
	localStorage.removeItem("waifu-display");
	sessionStorage.removeItem("waifu-text");
	document.body.insertAdjacentHTML("beforeend", `<div id="waifu">
			<div id="waifu-tips"></div>
			<canvas id="live2d" width="800" height="800"></canvas>
			<div id="waifu-tool">
				<span class="fa fa-lg fa-user-circle"></span>
				<span class="fa fa-lg fa-street-view"></span>
				<span class="fa fa-lg fa-camera-retro"></span>
				<span class="fa fa-lg fa-info-circle"></span>
				<span class="fa fa-lg fa-times"></span>
			</div>
		</div>`);
	// https://stackoverflow.com/questions/24148403/trigger-css-transition-on-appended-element
	setTimeout(() => {
		document.getElementById("waifu").style.bottom = 0;
	}, 0);

	function randomSelection(obj) {
		return Array.isArray(obj) ? obj[Math.floor(Math.random() * obj.length)] : obj;
	}
	// Detect user activity status and display a message when idle
	let userAction = false,
		userActionTimer,
		messageTimer,
		messageArray = ["Long time no see, life is going so fast...", "Big damn! How long have you been neglecting people?", "Hi, come and play with me!... D note: gross"];
	window.addEventListener("mousemove", () => userAction = true);
	window.addEventListener("keydown", () => userAction = true);
	setInterval(() => {
		if (userAction) {
			userAction = false;
			clearInterval(userActionTimer);
			userActionTimer = null;
		} else if (!userActionTimer) {
			userActionTimer = setInterval(() => {
				showMessage(randomSelection(messageArray), 6000, 9);
			}, 20000);
		}
	}, 1000);

	(function registerEventListener() {
		//document.querySelector("#waifu-tool .fa-comment").addEventListener("click", showHitokoto);
		/*document.querySelector("#waifu-tool .fa-paper-plane").addEventListener("click", () => {
			if (window.Asteroids) {
				if (!window.ASTEROIDSPLAYERS) window.ASTEROIDSPLAYERS = [];
				window.ASTEROIDSPLAYERS.push(new Asteroids());
			} else {
				const script = document.createElement("script");
				script.src = "https://cdn.jsdelivr.net/gh/stevenjoezhang/asteroids/asteroids.js";
				document.head.appendChild(script);
			}
		});*/
		document.querySelector("#waifu-tool .fa-user-circle").addEventListener("click", loadOtherModel);
		document.querySelector("#waifu-tool .fa-street-view").addEventListener("click", loadRandModel);
		document.querySelector("#waifu-tool .fa-camera-retro").addEventListener("click", () => {
			showMessage("Let me see the picture, is it cute?", 6000, 9);
			Live2D.captureName = "photo.png";
			Live2D.captureFrame = true;
		});
		document.querySelector("#waifu-tool .fa-info-circle").addEventListener("click", () => {
			open("https://github.com/stevenjoezhang/live2d-widget");
		});
		document.querySelector("#waifu-tool .fa-times").addEventListener("click", () => {
			localStorage.setItem("waifu-display", Date.now());
			showMessage("May you meet an important person one day.", 2000, 11);
			document.getElementById("waifu").style.bottom = "-500px";
			setTimeout(() => {
				document.getElementById("waifu").style.display = "none";
				document.getElementById("waifu-toggle").classList.add("waifu-toggle-active");
			}, 3000);
		});
		const devtools = () => {};
		console.log("%c", devtools);
		devtools.toString = () => {
			showMessage("Haha, you opened the console, do you want to see my little secrets?", 6000, 9);
		};
		window.addEventListener("copy", () => {
			showMessage("What have you copied? Remember to add the source when sharing!", 6000, 9);
		});
		window.addEventListener("visibilitychange", () => {
			if (!document.hidden) showMessage("Wow, you are finally back~", 6000, 9);
		});
	})();

	(function welcomeMessage() {
		let text;
		if (location.pathname === "/") { // If it is the homepage
			const now = new Date().getHours();
			if (now> 5 && now <= 7) text = "Good morning! The plan for a day is in the morning, and a good day is about to begin.";
			else if (now> 7 && now <= 11) text = "Good morning! Work is going well, don't sit for a long time, get up and move around!";
			else if (now> 11 && now <= 13) text = "It's noon, I worked all morning, and it's lunch time!";
			else if (now> 13 && now <= 17) text = "It's easy to get sleepy in the afternoon. Have you completed today's exercise goal?";
			else if (now> 17 && now <= 19) text = "It's evening! The sunset outside the window is very beautiful, the most beautiful but the sunset is red~";
			else if (now> 19 && now <= 21) text = "Good evening, how is your day?";
			else if (now> 21 && now <= 23) text = ["It's already so late, rest early, good night~", "Take care of your eyes late at night!"];
			else text = "Are you a night owl? You have not gone to bed so late, will you get up tomorrow?";
		} else if (document.referrer !== "") {
			const referrer = new URL(document.referrer),
				domain = referrer.hostname.split(".")[1];
				if (location.hostname === referrer.hostname) text = `Enjoy our <span>「${document.title.split("-")[0]}」</span> page`;
				else if (domain === "baidu") text = `Hello! Friends from Baidu Search<br>Did you find me when searching for <span>${referrer.search.split("&wd=")[1].split("&")[0]}</span>? `;
				else if (domain === "so") text = `Hello! Friends from 360 Search<br>Did you find me by searching for <span>${referrer.search.split("&q=")[1].split("&")[0]}</span>? `;
				else if (domain === "google") text = `Hello! Friends from Google Search<br>Enjoy your stay at <span>"${document.title.split("-")[0]}"</span>`;
				else text = `Hello! Friends from <span>${referrer.hostname}</span>`;
		} else {
			text = `Enjoy <span>「${document.title.split(" - ")[0]}」</span>`;
		}
		showMessage(text, 7000, 8);
	})();

	// function showHitokoto() {
	// 	// Add hitokoto.cn API
	// 	fetch("https://v1.hitokoto.cn")
	// 		.then(response => response.json())
	// 		.then(result => {
	// 			const text = `This sentence comes from <span>"${result.from}"</span>, which was submitted by <span>${result.creator}</span> on hitokoto.cn. `;
	// 			showMessage(result.hitokoto, 6000, 9);
	// 			setTimeout(() => {
	// 				showMessage(text, 4000, 9);
	// 			}, 6000);
	// 		});
	// }

	function showMessage(text, timeout, priority) {
		if (!text || (sessionStorage.getItem("waifu-text") && sessionStorage.getItem("waifu-text") > priority)) return;
		if (messageTimer) {
			clearTimeout(messageTimer);
			messageTimer = null;
		}
		text = randomSelection(text);
		sessionStorage.setItem("waifu-text", priority);
		const tips = document.getElementById("waifu-tips");
		tips.innerHTML = text;
		tips.classList.add("waifu-tips-active");
		messageTimer = setTimeout(() => {
			sessionStorage.removeItem("waifu-text");
			tips.classList.remove("waifu-tips-active");
		}, timeout);
	}
	messageG = (m,t,p) => showMessage(m,t,p);

	(function initModel() {
		let modelId = localStorage.getItem("modelId"),
			modelTexturesId = localStorage.getItem("modelTexturesId");
		if (modelId === null) {
			// First visit to load the specified material of the specified model
			modelId = 1; // Model ID
			modelTexturesId = 53; // Material ID
		}
		loadModel(modelId, modelTexturesId);
		fetch(waifuPath)
			.then(response => response.json())
			.then(result => {
				window.addEventListener("mouseover", event => {
					for (let { selector, text } of result.mouseover) {
						if (!event.target.matches(selector)) continue;
						text = randomSelection(text);
						text = text.replace("{text}", event.target.innerText);
						showMessage(text, 4000, 8);
						return;
					}
				});
				window.addEventListener("click", event => {
					for (let { selector, text } of result.click) {
						if (!event.target.matches(selector)) continue;
						text = randomSelection(text);
						text = text.replace("{text}", event.target.innerText);
						showMessage(text, 4000, 8);
						return;
					}
				});
				result.seasons.forEach(({ date, text }) => {
					const now = new Date(),
						after = date.split("-")[0],
						before = date.split("-")[1] || after;
					if ((after.split("/")[0] <= now.getMonth() + 1 && now.getMonth() + 1 <= before.split("/")[0]) && (after.split("/")[1] <= now.getDate() && now.getDate() <= before.split("/")[1])) {
						text = randomSelection(text);
						text = text.replace("{year}", now.getFullYear());
						//showMessage(text, 7000, true);
						messageArray.push(text);
					}
				});
			});
	})();

	async function loadModelList() {
		const response = await fetch(`${cdnPath}model_list.json`);
		modelList = await response.json();
		console.log(modelList);
	}

	async function loadModel(modelId, modelTexturesId, message) {
		localStorage.setItem("modelId", modelId);
		localStorage.setItem("modelTexturesId", modelTexturesId);
		showMessage(message, 4000, 10);
		if (useCDN) {
			if (!modelList) await loadModelList();
			const target = randomSelection(modelList.models[modelId]);
			loadlive2d("live2d", `${cdnPath}model/${target}/index.json`);
		} else {
			loadlive2d("live2d", `${apiPath}get/?id=${modelId}-${modelTexturesId}`);
			console.log(`Live2D model ${modelId}-${modelTexturesId} loaded complete`);
		}
	}

	async function loadRandModel() {
		const modelId = localStorage.getItem("modelId"),
			modelTexturesId = localStorage.getItem("modelTexturesId");
		if (useCDN) {
			if (!modelList) await loadModelList();
			const target = randomSelection(modelList.models[modelId]);
			loadlive2d("live2d", `${cdnPath}model/${target}/index.json`);
			showMessage("How do you like my new dress?", 4000, 10);
		} else {
			// Optional "rand" (random), "switch" (sequence)
			fetch(`${apiPath}rand_textures/?id=${modelId}-${modelTexturesId}`)
				.then(response => response.json())
				.then(result => {
					if (result.textures.id === 1 && (modelTexturesId === 1 || modelTexturesId === 0)) showMessage("I have no other clothes yet!", 4000, 10);
					else loadModel(modelId, result.textures.id, "How do you like my new dress?");
				});
		}
	}

	async function loadOtherModel() {
		translatedMessages=["Potion Maker - Pio ~","Come on Potion Maker - Tia ~","Vilibili Live- like 22 ~","The theory of the coming Bilibili Live 33","Shizuku Talk! This is Shizuku ~","Nep! Nep! Hyperdimension Neptunia: Kaiohsei Series","Kantai Collection / Murakumo"]
		let modelId = localStorage.getItem("modelId");
		if (useCDN) {
			if (!modelList) await loadModelList();
			const index = (++modelId >= modelList.models.length) ? 0 : modelId;
			loadModel(index, 0, translatedMessages[index]);
		} else {
			fetch(`${apiPath}switch/?id=${modelId}`)
				.then(response => response.json())
				.then(result => {
					loadModel(result.model.id, 0, result.model.message);
				});
		}
	}
}

function initWidget(config, apiPath) {
	if (typeof config === "string") {
		config = {
			waifuPath: config,
			apiPath
		};
	}
	document.body.insertAdjacentHTML("beforeend", `<div id="waifu-toggle">
			<span>Kanban girl</span>
		</div>`);
	const toggle = document.getElementById("waifu-toggle");
	toggle.addEventListener("click", () => {
		toggle.classList.remove("waifu-toggle-active");
		if (toggle.getAttribute("first-time")) {
			loadWidget(config);
			toggle.removeAttribute("first-time");
		} else {
			localStorage.removeItem("waifu-display");
			document.getElementById("waifu").style.display = "";
			setTimeout(() => {
				document.getElementById("waifu").style.bottom = 0;
			}, 0);
		}
	});
	if (localStorage.getItem("waifu-display") && Date.now() - localStorage.getItem("waifu-display") <= 86400000) {
		toggle.setAttribute("first-time", true);
		setTimeout(() => {
			toggle.classList.add("waifu-toggle-active");
		}, 0);
	} else {
		loadWidget(config);
	}
}
