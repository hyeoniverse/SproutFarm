mergeInto(LibraryManager.library, {
  // WebBridge.ShowResult (Assets/Scripts/WebBridge.cs) -> window.sproutFarmShowResult in index.html
  SproutFarm_ShowResult: function (jsonPointer) {
    var json = UTF8ToString(jsonPointer);
    if (typeof window.sproutFarmShowResult === "function") {
      window.sproutFarmShowResult(JSON.parse(json));
    }
  },

  // MobileInput.Enabled (Assets/Scripts/MobileInput.cs): 1 when the main pointer is a finger
  // (phones, tablets), so the game shows its on-screen joystick and buttons. index.html uses
  // the same media query for its touch-only styles.
  SproutFarm_IsTouchDevice: function () {
    return window.matchMedia && window.matchMedia("(pointer: coarse)").matches ? 1 : 0;
  },
});
