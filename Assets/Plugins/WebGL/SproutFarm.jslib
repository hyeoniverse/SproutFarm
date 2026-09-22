mergeInto(LibraryManager.library, {
  // WebBridge.ShowResult (Assets/Scripts/WebBridge.cs) -> window.sproutFarmShowResult in index.html
  SproutFarm_ShowResult: function (jsonPointer) {
    var json = UTF8ToString(jsonPointer);
    if (typeof window.sproutFarmShowResult === "function") {
      window.sproutFarmShowResult(JSON.parse(json));
    }
  },
});
