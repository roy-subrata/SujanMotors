(() => {
  const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
  if (!SpeechRecognition) {
    console.warn("Auto voice mode is not supported in this browser.");
    return;
  }

  const recognition = new SpeechRecognition();
  recognition.lang = "bn-BD";
  recognition.continuous = true;
  recognition.interimResults = false;
  recognition.maxAlternatives = 1;

  let running = false;
  let manuallyStopped = false;

  function setInputValue(input, value) {
    const prototype = Object.getPrototypeOf(input);
    const descriptor = Object.getOwnPropertyDescriptor(prototype, "value");
    if (descriptor && descriptor.set) {
      descriptor.set.call(input, value);
    } else {
      input.value = value;
    }
    input.dispatchEvent(new Event("input", { bubbles: true }));
  }

  function submitMessage(text) {
    const message = (text || "").trim();
    if (!message) return;

    const textarea = document.querySelector("textarea");
    if (textarea) {
      textarea.focus();
      setInputValue(textarea, message);
      textarea.dispatchEvent(
        new KeyboardEvent("keydown", {
          bubbles: true,
          cancelable: true,
          key: "Enter",
          code: "Enter"
        })
      );
      return;
    }

    const editable = document.querySelector('[contenteditable="true"]');
    if (editable) {
      editable.focus();
      editable.textContent = message;
      editable.dispatchEvent(new Event("input", { bubbles: true }));
      editable.dispatchEvent(
        new KeyboardEvent("keydown", {
          bubbles: true,
          cancelable: true,
          key: "Enter",
          code: "Enter"
        })
      );
    }
  }

  recognition.onresult = (event) => {
    for (let i = event.resultIndex; i < event.results.length; i += 1) {
      const result = event.results[i];
      if (result.isFinal && result[0] && result[0].transcript) {
        submitMessage(result[0].transcript);
      }
    }
  };

  recognition.onerror = (event) => {
    // Ignore no-speech and aborted; auto-restart handles it.
    if (event.error !== "no-speech" && event.error !== "aborted") {
      console.warn("Auto voice recognition error:", event.error);
    }
  };

  recognition.onend = () => {
    running = false;
    if (!manuallyStopped) {
      startRecognition();
    }
  };

  function startRecognition() {
    if (running || manuallyStopped) return;
    try {
      recognition.start();
      running = true;
    } catch (err) {
      // Retry quietly on transient start errors.
      setTimeout(() => {
        if (!manuallyStopped) startRecognition();
      }, 600);
    }
  }

  function stopRecognition() {
    manuallyStopped = true;
    if (running) {
      recognition.stop();
    }
  }

  // Start automatically after first user interaction to satisfy browser autoplay/security policy.
  const armAutoStart = () => {
    manuallyStopped = false;
    startRecognition();
    window.removeEventListener("click", armAutoStart, true);
    window.removeEventListener("keydown", armAutoStart, true);
    window.removeEventListener("touchstart", armAutoStart, true);
  };

  window.addEventListener("click", armAutoStart, true);
  window.addEventListener("keydown", armAutoStart, true);
  window.addEventListener("touchstart", armAutoStart, true);

  // Optional keyboard controls: Ctrl+Shift+M to toggle auto voice.
  window.addEventListener("keydown", (event) => {
    if (event.ctrlKey && event.shiftKey && event.key.toLowerCase() === "m") {
      if (manuallyStopped) {
        manuallyStopped = false;
        startRecognition();
      } else {
        stopRecognition();
      }
    }
  });
})();
