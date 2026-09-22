// Result and leaderboard overlay. Unity calls window.sproutFarmShowResult(result)
// when a game ends (WebBridge.ShowResult); the page shows the score breakdown, lets
// the player submit a name to /api/scores, and lists the top 10.
// Point values mirror GameResult.cs; the server recomputes the score it stores.
(function () {
  var POINTS = { animal: 100, berry: 20, clearBonus: 1000, remainingMinute: 2, stamina: 5 };
  var NAME_KEY = "sproutfarm:name";

  var overlay, face, title, total, breakdown, list, message, form, input, submitButton, current;

  function el(tag, className, text) {
    var node = document.createElement(tag);
    if (className) node.className = className;
    if (text != null) node.textContent = text;
    return node;
  }

  function readSavedName() {
    try {
      return localStorage.getItem(NAME_KEY) || "";
    } catch (error) {
      return "";
    }
  }

  function saveName(name) {
    try {
      localStorage.setItem(NAME_KEY, name);
    } catch (error) {
      // Private mode or blocked storage: the name just isn't remembered.
    }
  }

  function formatMinutes(minutes) {
    var hours = Math.floor(minutes / 60);
    var rest = minutes % 60;
    return (hours ? hours + "시간 " : "") + rest + "분";
  }

  function breakdownRows(result) {
    var rows = [
      ["울타리에 넣은 동물", result.animals + " / " + result.totalAnimals + "마리", result.animals * POINTS.animal],
      ["먹은 열매", result.berries + "개", result.berries * POINTS.berry],
    ];
    if (result.victory) {
      rows.push(["클리어 보너스", "", POINTS.clearBonus]);
      rows.push(["남은 시간", formatMinutes(result.remainingMinutes), result.remainingMinutes * POINTS.remainingMinute]);
      rows.push(["남은 체력", result.stamina + "%", result.stamina * POINTS.stamina]);
    }
    return rows;
  }

  function renderTop(top, highlightRank) {
    list.textContent = "";
    if (!top.length) {
      list.appendChild(el("li", "result-empty", "아직 기록이 없어. 첫 번째 주인공이 돼 봐!"));
      return;
    }
    top.forEach(function (entry) {
      var item = el("li", entry.rank === highlightRank ? "is-mine" : "");
      var medal = el("span", "");
      if (entry.rank <= 3) {
        var star = el("img", "result-star");
        star.src = "TemplateData/ui/star.png";
        star.alt = "";
        medal.appendChild(star);
      }
      item.appendChild(medal);
      item.appendChild(el("span", "result-rank", entry.rank + "위"));
      item.appendChild(el("span", "result-name", entry.name));
      item.appendChild(el("span", "result-badge", entry.victory ? "클리어" : ""));
      item.appendChild(el("span", "result-score", entry.score.toLocaleString("ko-KR") + "점"));
      list.appendChild(item);
    });
  }

  function setMessage(text) {
    message.textContent = text;
  }

  function loadTop() {
    fetch("api/scores")
      .then(function (response) {
        if (response.status === 503) throw new Error("not_configured");
        if (!response.ok) throw new Error("failed");
        return response.json();
      })
      .then(function (data) {
        renderTop(data.top, null);
      })
      .catch(function (error) {
        submitButton.disabled = true;
        list.textContent = "";
        setMessage(error.message === "not_configured"
          ? "랭킹 서버가 아직 준비 중이야. 점수는 여기서만 볼 수 있어!"
          : "랭킹을 불러오지 못했어. 잠시 뒤에 다시 해 봐!");
      });
  }

  function submit(event) {
    event.preventDefault();
    var name = input.value.trim();
    if (!name) {
      setMessage("이름을 먼저 적어 줘!");
      input.focus();
      return;
    }
    saveName(name);
    submitButton.disabled = true;
    setMessage("올리는 중...");
    fetch("api/scores", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: name,
        victory: current.victory,
        animals: current.animals,
        berries: current.berries,
        remainingMinutes: current.remainingMinutes,
        stamina: current.stamina,
      }),
    })
      .then(function (response) {
        if (response.status === 429) throw new Error("too_many_requests");
        if (!response.ok) throw new Error("failed");
        return response.json();
      })
      .then(function (data) {
        renderTop(data.top, data.rank);
        setMessage(data.rank + "위에 올랐어! 화면을 닫고 누르면 다시 시작할 수 있어.");
        input.disabled = true;
      })
      .catch(function (error) {
        submitButton.disabled = false;
        setMessage(error.message === "too_many_requests"
          ? "조금만 기다렸다가 다시 올려 줘!"
          : "기록을 올리지 못했어. 잠시 뒤에 다시 해 봐!");
      });
  }

  function build() {
    overlay = el("div", "");
    overlay.id = "result-overlay";
    var panel = el("div", "result-panel");
    panel.setAttribute("role", "dialog");
    panel.setAttribute("aria-labelledby", "result-title");

    // Portrait and speech bubble, laid out like the in-game dialogue
    var head = el("div", "result-head");
    var portrait = el("div", "result-portrait");
    face = el("img", "result-face");
    face.alt = "";
    portrait.appendChild(face);
    head.appendChild(portrait);
    var bubble = el("div", "result-bubble");
    title = el("h2", "");
    title.id = "result-title";
    total = el("p", "result-total");
    bubble.appendChild(title);
    bubble.appendChild(total);
    head.appendChild(bubble);
    panel.appendChild(head);

    var scoreBox = el("div", "result-box");
    breakdown = el("dl", "result-breakdown");
    scoreBox.appendChild(breakdown);
    panel.appendChild(scoreBox);

    form = el("form", "result-form");
    input = el("input", "result-field");
    input.type = "text";
    input.maxLength = 10;
    input.placeholder = "이름 (10자까지)";
    input.setAttribute("aria-label", "랭킹에 올릴 이름");
    submitButton = el("button", "result-button", "랭킹에 올리기");
    submitButton.type = "submit";
    form.appendChild(input);
    form.appendChild(submitButton);
    form.addEventListener("submit", submit);
    panel.appendChild(form);

    message = el("p", "result-message");
    message.setAttribute("aria-live", "polite");
    panel.appendChild(message);

    var board = el("div", "result-box");
    board.appendChild(el("h3", "", "랭킹 TOP 10"));
    list = el("ol", "result-list");
    board.appendChild(list);
    panel.appendChild(board);

    var close = el("button", "result-button result-close", "닫기");
    close.type = "button";
    close.addEventListener("click", function () {
      overlay.hidden = true;
    });
    panel.appendChild(close);

    overlay.appendChild(panel);
    document.getElementById("unity-container").appendChild(overlay);
  }

  window.sproutFarmShowResult = function (result) {
    if (!overlay) build();
    current = result;

    face.src = result.victory ? "TemplateData/ui/face-clear.png" : "TemplateData/ui/face-gameover.png";
    title.textContent = result.victory ? "클리어! 동물을 모두 찾았어" : "게임 오버... 다음엔 꼭 잡자!";
    total.textContent = "총점 " + result.score.toLocaleString("ko-KR") + "점";
    breakdown.textContent = "";
    breakdownRows(result).forEach(function (row) {
      var line = el("div", "result-row");
      line.appendChild(el("dt", "", row[0]));
      line.appendChild(el("dd", "result-detail", row[1]));
      line.appendChild(el("dd", "result-points", "+" + row[2].toLocaleString("ko-KR")));
      breakdown.appendChild(line);
    });

    input.value = readSavedName();
    input.disabled = false;
    submitButton.disabled = false;
    setMessage("");
    list.textContent = "";
    overlay.hidden = false;
    input.focus();
    loadTop();
  };
})();
