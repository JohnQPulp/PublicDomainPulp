document.addEventListener("keydown", e => {
  if (e.key === "w" || e.key === "s") {
    const increment = e.key === "w" ? -1 : 1;
    const speakerElem = document.querySelector("#pulp:nth-child(2) .speaker-back");
    if (speakerElem) {
      const regex = /(c-([^-\.]+)(-a[^-\.]+)?((-x[^-\.]+)+)?)(-e([^-\.]+))?(-s?([123])?)?/;
      const matchArr = speakerElem.style.backgroundImage.match(regex);
      const expressionsArr = expressions[matchArr[1]];
      let expressionOld = matchArr[0];
      let expressionNew;
      let index = expressionsArr.indexOf(expressionOld) + expressionsArr.length;
      do {
        index += increment;
        expressionNew = expressionsArr[index % expressionsArr.length];
      } while (!!expressionOld.match(/-s[123]?$/) !== !!expressionNew.match(/-s[123]?$/));
      speakerElem.outerHTML = `<div class="speaker-back" style="background-image: url(images/${expressionNew}.${imageExt})"></div>`;
      const matchArr2 = expressionNew.match(regex);
      navigator.clipboard.writeText(";e:" + matchArr2[2] + "=" + (matchArr2[7] ?? "") + ";i=" + (matchArr2[9] ?? ""));
    }
  }
});
toggleEditorializing();