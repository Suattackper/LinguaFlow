function worker() {
  if (!('serviceWorker' in navigator) || !('caches' in window)) throw new Error('Offline downloads are unavailable in this browser.');
  return navigator.serviceWorker.ready.then(registration => {
    if (!registration.active) throw new Error('Service worker is not active yet. Reload after installation.');
    return registration.active;
  });
}

function send(message, dotnetRef) {
  return worker().then(active => new Promise((resolve, reject) => {
    const channel = new MessageChannel();
    const timeout = setTimeout(() => { channel.port1.close(); reject(new Error('Offline request timed out.')); }, 120000);
    channel.port1.onmessage = event => {
      const data = event.data;
      if (data.event === 'stage') { globalThis.__linguaFlowOfflineStage = data.stage; return; }
      if (data.event === 'progress') { globalThis.__linguaFlowOfflineProgress = [data.done, data.total]; dotnetRef?.invokeMethodAsync('OfflineProgress', data.done, data.total); return; }
      clearTimeout(timeout); channel.port1.close();
      if (data.event === 'error') reject(new Error(data.message));
      else resolve(data.result);
    };
    active.postMessage(message, [channel.port2]);
  }));
}

export function status(courseId) { return send({ action: 'status', courseId }); }
export function download(courseId, languageDirectory, manifestUrl, expectedFingerprint, allowChanged, dotnetRef) {
  return send({ action: 'download', courseId, languageDirectory, manifestUrl, expectedFingerprint, allowChanged }, dotnetRef);
}
export function remove(courseId) { return send({ action: 'remove', courseId }); }
export function networkHint() { return navigator.onLine; }
export function observeNetwork(dotnetRef) {
  const listener = () => dotnetRef.invokeMethodAsync('NetworkChanged', navigator.onLine);
  window.addEventListener('online', listener); window.addEventListener('offline', listener);
  return { dispose: () => { window.removeEventListener('online', listener); window.removeEventListener('offline', listener); } };
}
