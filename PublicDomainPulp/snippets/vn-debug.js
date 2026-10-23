function getSpeakerStuff(speakerElem, increment) {
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
  const url= `url(images/${expressionNew}.${imageExt})`;
  const matchArr2 = expressionNew.match(regex);
  const clipboard = ";e:" + matchArr2[2] + "=" + (matchArr2[7] ?? "") + ";i=" + (matchArr2[9] ?? "");
  return [url, clipboard];
}
function getSpeakerElem() {
  return document.querySelector("#pulp:nth-child(2) .speaker-back");
}
function updateDummy(increment = 1) {
  const speakerElem = getSpeakerElem();
  if (speakerElem) {
    const [url, _] = getSpeakerStuff(speakerElem, increment);
    document.getElementById('dummy').style.backgroundImage = url;
  }
}
document.addEventListener("keydown", e => {
  if (e.key === "w" || e.key === "s") {
    const speakerElem = getSpeakerElem();
    if (speakerElem) {
      const increment = e.key === "w" ? -1 : 1;
      const [url, clipboard] = getSpeakerStuff(speakerElem, increment);
      speakerElem.style.backgroundImage = url;
      navigator.clipboard.writeText(clipboard);
      updateDummy(increment);
    }
  } else if (e.key === "j") {
    document.getElementById("vn-header").scrollIntoView(true);
  }
});
toggleEditorializing();
window.addEventListener("posUpdate", e => {
  updateDummy();
});