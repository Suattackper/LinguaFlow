let active = null;

export function supported() {
  return typeof window !== "undefined" && "speechSynthesis" in window && "SpeechSynthesisUtterance" in window;
}

export async function voices(timeoutMs = 1500) {
  if (!supported()) return [];
  let list = speechSynthesis.getVoices();
  if (list.length === 0) {
    list = await new Promise(resolve => {
      let finished = false;
      let timer;
      const finish = () => {
        if (finished) return;
        finished = true;
        speechSynthesis.removeEventListener("voiceschanged", changed);
        clearTimeout(timer);
        resolve(speechSynthesis.getVoices());
      };
      const changed = () => finish();
      speechSynthesis.addEventListener("voiceschanged", changed);
      timer = setTimeout(finish, timeoutMs);
    });
  }
  return list.map(v => ({ voiceUri: v.voiceURI, name: v.name, lang: v.lang, localService: v.localService }));
}

export function speak(text, language, voiceUri, rate) {
  if (!supported()) return Promise.reject(new Error("Text-to-speech is unavailable"));
  if (!text || !text.trim()) return Promise.reject(new Error("Nothing to speak"));
  stop();
  return new Promise((resolve, reject) => {
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = language;
    utterance.rate = Math.max(0.6, Math.min(1.4, rate));
    const voice = speechSynthesis.getVoices().find(v => v.voiceURI === voiceUri);
    if (voice) utterance.voice = voice;
    let settled = false;
    const finish = error => {
      if (settled) return;
      settled = true;
      utterance.onend = null;
      utterance.onerror = null;
      if (active?.utterance === utterance) active = null;
      error ? reject(error) : resolve();
    };
    utterance.onend = () => finish(null);
    utterance.onerror = event => finish(new Error(event.error || "Speech playback failed"));
    active = { utterance, cancel: () => finish(new Error("Speech stopped")) };
    speechSynthesis.speak(utterance);
  });
}

export function stop() {
  if (!supported()) return;
  if (active) active.cancel();
  speechSynthesis.cancel();
}
