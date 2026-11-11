const DB_NAME = "linguaflow-learning";
const DB_VERSION = 2;
let opening;

function requestValue(request) {
  return new Promise((resolve, reject) => {
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

function complete(transaction) {
  return new Promise((resolve, reject) => {
    transaction.oncomplete = () => resolve();
    transaction.onabort = () => reject(transaction.error || new Error("IndexedDB transaction aborted"));
    transaction.onerror = () => reject(transaction.error || new Error("IndexedDB transaction failed"));
  });
}

export function openStore() {
  if (!globalThis.indexedDB) return Promise.reject(new Error("IndexedDB is unavailable in this browser"));
  if (!opening) opening = new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION);
    request.onupgradeneeded = () => {
      const db = request.result;
      // v1 records remain intact. v2 adds atomic recovery snapshots and backup metadata.
      if (!db.objectStoreNames.contains("records")) {
        const store = db.createObjectStore("records", { keyPath: "key" });
        store.createIndex("courseId", "courseId", { unique: false });
      }
      if (!db.objectStoreNames.contains("snapshots")) db.createObjectStore("snapshots", { keyPath: "courseId" });
      if (!db.objectStoreNames.contains("metadata")) db.createObjectStore("metadata", { keyPath: "key" });
    };
    request.onsuccess = () => { const db = request.result; db.onversionchange = () => db.close(); resolve(db); };
    request.onerror = () => { opening = undefined; reject(request.error); };
    request.onblocked = () => { opening = undefined; reject(new Error("Close other LinguaFlow tabs to upgrade storage")); };
  });
  return opening;
}

export async function getCourse(courseId) {
  const db = await openStore();
  const tx = db.transaction("records", "readonly");
  const done = complete(tx);
  const result = await requestValue(tx.objectStore("records").index("courseId").getAll(courseId));
  await done;
  return result;
}

export async function getAllRecords() {
  const db = await openStore();
  const tx = db.transaction("records", "readonly");
  const done = complete(tx);
  const result = await requestValue(tx.objectStore("records").getAll());
  await done;
  return result;
}

export async function putRecord(record) {
  const db = await openStore();
  const tx = db.transaction("records", "readwrite");
  tx.objectStore("records").put(record);
  await complete(tx);
}

export async function putBatch(records) {
  const db = await openStore();
  const tx = db.transaction("records", "readwrite");
  const done = complete(tx);
  const store = tx.objectStore("records");
  try { for (const record of records) store.put(record); await done; }
  catch (error) { try { tx.abort(); } catch {} await done.catch(() => {}); throw error; }
}

export async function mutateBatch(records, keysToDelete) {
  const db = await openStore();
  const tx = db.transaction("records", "readwrite");
  const done = complete(tx);
  const store = tx.objectStore("records");
  try {
    for (const key of keysToDelete) store.delete(key);
    for (const record of records) store.put(record);
    await done;
  } catch (error) { try { tx.abort(); } catch {} await done.catch(() => {}); throw error; }
}

export async function deleteRecord(key) {
  const db = await openStore();
  const tx = db.transaction("records", "readwrite");
  tx.objectStore("records").delete(key);
  await complete(tx);
}

function deleteCourseInTransaction(store, courseId, tx) {
  return new Promise((resolve, reject) => {
    const cursor = store.index("courseId").openKeyCursor(IDBKeyRange.only(courseId));
    cursor.onsuccess = () => {
      if (!cursor.result) { resolve(); return; }
      store.delete(cursor.result.primaryKey);
      cursor.result.continue();
    };
    cursor.onerror = () => { tx.abort(); reject(cursor.error); };
  });
}

export async function replaceCourse(courseId, records, snapshot, importedAt) {
  const db = await openStore();
  const tx = db.transaction(["records", "snapshots", "metadata"], "readwrite");
  const done = complete(tx);
  const store = tx.objectStore("records");
  // Snapshot, deletion and replacement share one transaction. An interruption rolls all back.
  try {
    tx.objectStore("snapshots").put({ courseId, records: snapshot, createdAt: new Date().toISOString() });
    await deleteCourseInTransaction(store, courseId, tx);
    for (const record of records) store.put(record);
    tx.objectStore("metadata").put({ key: `backup:${courseId}`, lastBackupAt: importedAt });
    await done;
  } catch (error) { try { tx.abort(); } catch {} await done.catch(() => {}); throw error; }
}

export async function getSnapshot(courseId) {
  const db = await openStore();
  const tx = db.transaction("snapshots", "readonly");
  const done = complete(tx);
  const result = await requestValue(tx.objectStore("snapshots").get(courseId));
  await done;
  return result || null;
}

export async function resetCourse(courseId) {
  const db = await openStore();
  const tx = db.transaction(["records", "snapshots", "metadata"], "readwrite");
  const done = complete(tx);
  const store = tx.objectStore("records");
  try {
    const current = await requestValue(store.index("courseId").getAll(courseId));
    tx.objectStore("snapshots").put({ courseId, records: current, createdAt: new Date().toISOString() });
    await deleteCourseInTransaction(store, courseId, tx);
    tx.objectStore("metadata").delete(`backup:${courseId}`);
    await done;
  } catch (error) { try { tx.abort(); } catch {} await done.catch(() => {}); throw error; }
}

export async function resetAll() {
  const db = await openStore();
  const tx = db.transaction(["records", "snapshots", "metadata"], "readwrite");
  tx.objectStore("records").clear();
  tx.objectStore("snapshots").clear();
  tx.objectStore("metadata").clear();
  await complete(tx);
}

export async function setBackupDate(courseId, timestamp) {
  const db = await openStore();
  const tx = db.transaction("metadata", "readwrite");
  tx.objectStore("metadata").put({ key: `backup:${courseId}`, lastBackupAt: timestamp });
  await complete(tx);
}

export async function getBackupDate(courseId) {
  const db = await openStore();
  const tx = db.transaction("metadata", "readonly");
  const done = complete(tx);
  const value = await requestValue(tx.objectStore("metadata").get(`backup:${courseId}`));
  await done;
  return value?.lastBackupAt || null;
}

export async function estimateUsage() {
  if (!navigator.storage?.estimate) return null;
  const result = await navigator.storage.estimate();
  return { usage: result.usage || 0, quota: result.quota || 0 };
}

export function downloadJson(filename, content) {
  const blob = new Blob([content], { type: "application/json;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 60000);
}
