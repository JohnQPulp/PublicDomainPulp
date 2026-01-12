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
  document.getElementById("linecount").innerText = "Line " + pos + "/" + (htmlArr.length - 1);
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
function homeClick() {
  window.location.href = '/';
}