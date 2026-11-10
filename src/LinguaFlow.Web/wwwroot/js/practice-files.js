// An ordinary ChatGPT conversation URL. No account, Project, or conversation is created by LinguaFlow.
export const chatUrl = "https://chatgpt.com/";
export function downloadMarkdown(filename, content) {
  if (!/^linguaflow-[a-z0-9-]+\.md$/.test(filename)) throw new Error("Invalid filename");
  const blob = new Blob([content], { type: "text/markdown;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  try { const a = document.createElement("a"); a.href = url; a.download = filename; document.body.appendChild(a); a.click(); a.remove(); }
  finally { setTimeout(() => URL.revokeObjectURL(url), 60000); }
}
export async function copyInstructions(content) {
  if (!navigator.clipboard?.writeText) throw new Error("Clipboard is unavailable in this browser context.");
  await navigator.clipboard.writeText(content);
}
export function openChat() { const tab = window.open(chatUrl, "_blank"); if (!tab) return false; tab.opener = null; return true; }
