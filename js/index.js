//app ui
var ul = document.getElementById("wallpapers");
ul.onclick = function (event) {
  var img = event.target;
  if (img.tagName != "IMG") {
    return;
  }

  //clear selection
  let lis = ul.getElementsByTagName("IMG");
  for (let i = 0; i <= lis.length - 1; i++) {
    lis[i].style.outline = "";
  }

  //update selection
  img.style.outline = "2.5px solid #a425a0";

  //update customise controls
  $("*[id*=ui-app-customise-controls-]").css("display", "none");
  switch (img.parentElement.id) {
    case "rain":
      setScene("rain");
      $("#ui-app-customise-controls-rain").css("display", "inline");
      break;
    case "clouds":
      setScene("clouds");
      $("#ui-app-customise-controls-clouds").css("display", "inline");
      break;
    case "snow":
      setScene("snow");
      $("#ui-app-customise-controls-snow").css("display", "inline");
      break;
    case "synthwave":
      setScene("synthwave");
      $("#ui-app-customise-controls-synthwave").css("display", "inline");
      break;
  }
};

//app ui (scrolling)
$(window).scroll(function () {
  var requiredOffset = 100;

  // Between 0 and 1 (inclusive)
  var percentage = Math.min(1, $(window).scrollTop() / requiredOffset);

  // Starts at requiredOffset and goes down to 0
  var marginTop = requiredOffset * (1 - percentage);

  // Opacity of frame
  var alpha = 1 - percentage;

  $(".heading").css("opacity", alpha);
  $(".ui-app").css("margin-top", marginTop);
  //$("#ui-app-library").css("filter", `brightness(${1 - (1 - alpha) / 4})`);
  $(".ui-app-overlay").css("background-color", `rgba(0, 0, 0, ${(1 - alpha) / 4})`);
  $("#ui-app-customize-heading").css("opacity", 1 - alpha);
  $("#ui-app-customise").css("opacity", 1 - alpha);
  setProperty("u_brightness", 0.75 + (1 - alpha) / 8);

  if (alpha == 0) {
    $("#ui-app-library").css("pointer-events", "none");
    $("#ui-app-customise").css("pointer-events", "auto");
  } else {
    $("#ui-app-library").css("pointer-events", "auto");
    $("#ui-app-customise").css("pointer-events", "none");
  }
});

//listen to element visibility change
//ref: https://stackoverflow.com/questions/27462306/css3-animate-elements-if-visible-in-viewport-page-scroll/
function callbackFunc(entries, observer) {
  entries.forEach((entry) => {
    switch (entry.target.id) {
      case "page-home":
        setPause(!entry.isIntersecting); //pause threejs
        break;
      case "page-features":
        if (entry.isIntersecting) {
          // $(".cards").addClass("fade-in-start-2s");
        }
        break;
      case "page-gallery":
        if (entry.isIntersecting) {
          // $(".gallery-container").addClass("fade-in-start-2s");
        }
        break;
      case "page-download":
        if (entry.isIntersecting) {
          // $(".download-options").addClass("fade-in-start-2s");
        }
        break;
      case "footer":
        //todo
        break;
    }
  });
}

let observer = new IntersectionObserver(
  callbackFunc,
  (options = {
    root: null,
    rootMargin: "0px",
    threshold: 0.0,
  })
);
observer.observe($("#page-home")[0]);
observer.observe($("#page-features")[0]);
observer.observe($("#page-gallery")[0]);
observer.observe($("#page-download")[0]);
observer.observe($("#footer")[0]);

//threejs scene first run
document.addEventListener("sceneLoaded", () => {
  if (container.style.opacity == 0) setVisible(container);
  $(".indeterminate-progress-bar").css("display", "none");

  $(".item").each(function () {
    $(this).css("background-image", $(this).data("delayedsrc"));
  });
});

//helpers
async function setVisible(element) {
  for (let val = 0; val < 1; val += 0.1) {
    element.style.opacity = val;
    await new Promise((r) => setTimeout(r, 75));
  }
}

function setPallete() {
  document.body.style.setProperty("--color1", "#415074");
  document.body.style.setProperty("--color2", "#6072A1");
  document.body.style.setProperty("--color3", "#57658F");
}
//setPallete();

function hasClass(element, className) {
  return (" " + element.className + " ").indexOf(" " + className + " ") > -1;
}

function scrollToElement(id) {
  document.getElementById(id).scrollIntoView({ behavior: "smooth", block: "center", inline: "nearest" });
}
