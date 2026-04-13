document.addEventListener("keydown", e => {
  if (e.key === "," || e.key === ".") {
    const increment = e.key === "," ? -1 : 1;
    const speakerElem = document.querySelector("#pulp:nth-child(2) .speaker-back")
    const matchArr = speakerElem.style.backgroundImage.match(/(c-[^-\.]+(-a[^-\.]+)?((-x[^-\.]+)+)?)(-e[^-\.]+)?(-s?[123]?)?/)
    const expressionsArr = expressions[matchArr[1]]
    const expressionNew = expressionsArr[(expressionsArr.indexOf(matchArr[0]) + increment + expressionsArr.length) % expressionsArr.length]
    console.log(expressionNew)
    speakerElem.outerHTML = `<div class="speaker-back" style="background-image: url(images/${expressionNew}.${imageExt})"></div>`
  }
});