function createLineButtons(i) {
  return `<button onclick="setPos(${i})">Line ${i}</button><button class='copyLinkBtn' onclick="copyClick(event, ${i})"></button>`;
}
let bookmarks = [];
function populateBookmarks() {
  const bookmarksList = document.querySelector("#menu-bookmarks > ul");
  bookmarksList.innerHTML = bookmarks.map(p => {
    const line = htmlArr[p].replace(/<p class='e'>.*?<\/p>/g, "").replace(/<b class='speaker'>.*?<\/b>/g, "").replace(/<.*?>/g, "");
    return `<li><span>${createLineButtons(p)}: ${line}</span></li>`
  }).join("\n");
}
function updateBookmarkBtn() {
  const bookmarkBtn = document.getElementById("bookmarkBtn");
  bookmarkBtn.classList.toggle("bookmarked", bookmarks.indexOf(pos) !== -1);
}
function bookmarkClick() {
  const index = bookmarks.indexOf(pos);
  if (index === -1) {
    bookmarks.push(pos);
  } else {
    bookmarks.splice(index, 1);
  }
  bookmarks.sort((a, b) => a - b);
  populateBookmarks();
  updateBookmarkBtn();
  localStorage.setItem(getLocalStorageKey('b'), JSON.stringify(bookmarks));
}
window["handlePosUpdate"] = function() {
  updateBookmarkBtn();
  document.getElementById("linecount").innerHTML = "<b>Line:</b> " + pos + "/" + (htmlArr.length - 1);
  document.getElementById("progress").value = pos / (htmlArr.length - 1);
}
window.addEventListener("load", e => {
  const bookmarksString = localStorage.getItem(getLocalStorageKey('b'));
  if (bookmarksString) {
    bookmarks = JSON.parse(bookmarksString);
  }
  populateBookmarks();
  const tocList = document.querySelector("#menu-toc > ul");
  let level = 1;
  let ulHtml = "<ul>";
  headers.forEach(headerStr => {
    headerArr = headerStr.split('|');
    let line = parseInt(headerArr[0]);
    let hLevel = parseInt(headerArr[1]);
    let headerText = headerArr[2];
    if (hLevel > level) {
      ulHtml += "<ul>";
    } else if (hLevel < level) {
      ulHtml += "</ul>";
    }
    level = hLevel;
    ulHtml += `<li><span><b>${headerText}</b> ${createLineButtons(line)}</span></li>`;
  });
  ulHtml += "</ul>";
  tocList.innerHTML = ulHtml;
});
function copyClick(e, i) {
  e.target.classList.toggle('copied', true);
  setTimeout(() => e.target.classList.toggle('copied', false), 1000);

  const url = new URL(window.location.href);
  url.search = "";
  url.searchParams.set("l", i.toString());
  navigator.clipboard.writeText(url.toString());
}
let isFullscreen = false;
function toggleFullscreen() {
  if (isFullscreen) {
    document.exitFullscreen().catch(exitFullscreen);
    document.getElementById("fullscreenToggle").classList.remove("btnActive");
  } else {
    document.documentElement.requestFullscreen().catch(enterFullscreen);
    document.getElementById("fullscreenToggle").classList.add("btnActive");
  }
  isFullscreen = !isFullscreen;
}
document.addEventListener("keydown", function (e) {
  if (e.key === "f" || e.key === "F") {
    toggleFullscreen();
  } else if (e.key === "e" || e.key === "E") {
    toggleEditor();
  } else if (e.key === "h" || e.key === "H") {
    bookmarkClick();
  } else if (e.key === "p" || e.key === "P") {
    toggleAuto();
  }
});
addEventListener("fullscreenchange", (event) => {
  if (document.fullscreenElement) {
    enterFullscreen();
  } else {
    exitFullscreen();
  }
});
function enterFullscreen() {
  setWindowProps("1vw", "1vh");
  document.getElementById("app").style.borderRadius = "0";
  document.getElementById("app").scrollIntoView({behavior: "instant"});
}
function exitFullscreen() {
  setWindowProps("min(0.8vw, 1.6vh)", "min(0.45vw, 0.9vh)");
  document.getElementById("app").style.borderRadius = "12px";
}
function setWindowProps(vwUnit, vhUnit) {
  document.getElementsByTagName("main")[0].style.setProperty("--vwUnit", vwUnit);
  document.getElementsByTagName("main")[0].style.setProperty("--vhUnit", vhUnit);
}
let autoplayCounter = 0;
let autoplay = false;
let autoplayDial = 100;
function toggleAuto() {
  autoplay = !autoplay;
  if (autoplay) {
    document.getElementById("autoToggle").classList.add("btnActive");
    autoCallback(++autoplayCounter);
  } else {
    document.getElementById("autoToggle").classList.remove("btnActive");
  }
}
function autoCallback(counter) {
  setTimeout(() => {
    if (autoplay && counter === autoplayCounter) {
      nextPulp();
      autoCallback(counter);
    }
  }, (50 + htmlArr[pos].length * 2) * Math.pow(10, 2 - autoplayDial / 100));
}
let notesVisible = true;
function toggleEditor() {
  notesVisible = !notesVisible;
  if (notesVisible) {
    document.getElementById("editorToggle").classList.add("btnActive");
    document.getElementById("app").style.setProperty("--editorDisplay", "block");
  } else {
    document.getElementById("editorToggle").classList.remove("btnActive");
    document.getElementById("app").style.setProperty("--editorDisplay", "none");
  }
}
function setFontSize(fontNumber) {
  document.getElementById("app").style.setProperty('--vnFontSize', Math.floor(fontNumber / 100) + '.' + (Math.floor(fontNumber / 10) % 10) + (fontNumber % 10));
}
function setFontSizeReally(fontNumber) {
  document.getElementById("app").style.setProperty('--vnFontSizeReally', Math.floor(fontNumber / 100) + '.' + (Math.floor(fontNumber / 10) % 10) + (fontNumber % 10) + "em");
}
function setDialogueFontWeight(fontWeight) {
  document.getElementById("app").style.setProperty('--vnDialogueFontWeight', fontWeight);
}
function setLineHeight(lineHeight) {
  document.getElementById("app").style.setProperty('--vnLineHeight', Math.floor(lineHeight / 100) + '.' + (Math.floor(lineHeight / 10) % 10) + (lineHeight % 10));
}
function setAutoSpeed(speed) {
  autoplayDial = speed;
}