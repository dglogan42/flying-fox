/**
 * Flying Fox — Firefox extension background (MV3 event page).
 * Toolbar click (or Alt+Shift+F) opens the game in a dedicated tab.
 */

const GAME_PATH = "index.html";

async function openGame() {
  const url = browser.runtime.getURL(GAME_PATH);
  const tabs = await browser.tabs.query({ url });

  if (tabs.length > 0) {
    const tab = tabs[0];
    await browser.tabs.update(tab.id, { active: true });
    if (tab.windowId != null) {
      await browser.windows.update(tab.windowId, { focused: true });
    }
    return;
  }

  await browser.tabs.create({ url });
}

browser.action.onClicked.addListener(() => {
  openGame().catch((err) => console.error("Flying Fox: failed to open game", err));
});

browser.runtime.onInstalled.addListener((details) => {
  if (details.reason === "install") {
    openGame().catch(() => {});
  }
});
