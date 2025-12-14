function createLineButtons(i) {
  return `<button onclick="setPos(${i})">Line ${i}</button><button onclick="copyClick(${i})">🔗</button>`;
}
let menuOpen = false;
function menuClick() {
  if (menuOpen) {
    document.getElementById("menu").classList.toggle("visible", false);
    document.getElementById("menuBtn").classList.toggle("menuOpen", false);
    menuOpen = false;
    scrollEnabled = true;
  } else {
    document.getElementById("menu").classList.toggle("visible", true);
    document.getElementById("menuBtn").classList.toggle("menuOpen", true);
    menuOpen = true;
    scrollEnabled = false;
  }
}
let bookmarks = [];
const bookmarksString = localStorage.getItem("b");
if (bookmarksString) {
  bookmarks = JSON.parse(bookmarksString);
}
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
  localStorage.setItem("b", JSON.stringify(bookmarks));
}
function handlePosUpdate() {
  updateBookmarkBtn();
}
window.addEventListener("load", e => {
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
function copyClick(i) {
  const copytext = document.getElementById('copytext');
  copytext.classList.toggle('visible', true);
  setTimeout(() => copytext.classList.toggle('visible', false), 1000);

  const url = new URL(window.location.href);
  url.search = "";
  url.searchParams.set("l", i.toString());
  navigator.clipboard.writeText(url.toString());
}
function homeClick() {
  window.location.href = '/';
}